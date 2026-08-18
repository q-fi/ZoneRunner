using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TravelManager : MonoBehaviour
{
    public static TravelManager Instance { get; private set; }

    public bool IsTraveling { get; private set; }
    public bool IsPausedForBattle { get; private set; }
    public float TimeRemaining { get; private set; }
    public string CurrentRegion { get; private set; }

    public RegionData CurrentRegionData { get; private set; }
    public LocationData CurrentLocation { get; private set; }
    public EncounterData CurrentEncounter { get; private set; }
    public int SelectedEquipmentPresetIndex { get; private set; } = -1;
    public int SelectedBackpackPresetIndex { get; private set; } = -1;
    public IReadOnlyList<ItemInstance> SelectedEquipmentItems =>
        selectedEquipmentItems;
    public IReadOnlyList<ItemInstance> SelectedBackpackItems =>
        selectedBackpackItems;

    private readonly List<ItemInstance> selectedEquipmentItems = new();
    private readonly List<ItemInstance> selectedBackpackItems = new();

    private bool ambushScheduled;
    private float ambushTriggerTimeRemaining;

    public event Action OnTravelStarted;
    public event Action<float> OnTravelTick;
    public event Action OnTravelEnded;

    [SerializeField] private float travelDuration = 10f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void StartTravel(string destinationName)
    {
        CurrentRegionData = null;
        CurrentLocation = null;
        CurrentEncounter = null;
        SelectedEquipmentPresetIndex = -1;
        SelectedBackpackPresetIndex = -1;
        selectedEquipmentItems.Clear();
        selectedBackpackItems.Clear();

        StartTravelInternal(destinationName, travelDuration);
    }

    public void StartTravel(
        RegionData regionData,
        LocationData locationData,
        int equipmentPresetIndex,
        int backpackPresetIndex
    )
    {
        if (regionData == null || locationData == null)
            return;

        CurrentRegionData = regionData;
        CurrentLocation = locationData;
        CurrentEncounter = null;
        SelectedEquipmentPresetIndex = equipmentPresetIndex;
        SelectedBackpackPresetIndex = backpackPresetIndex;

        CaptureSelectedEquipment();
        CaptureSelectedBackpack();

        Debug.Log(
            $"Travel context: {CurrentRegionData.RegionName} -> " +
            $"{CurrentLocation.displayName}"
        );

        StartTravelInternal(
            locationData.displayName,
            locationData.travelDurationSeconds
        );
    }

    private void CaptureSelectedEquipment()
    {
        selectedEquipmentItems.Clear();

        if (InventoryManager.Instance == null)
        {
            Debug.LogError(
                "TravelManager: не вдалося зчитати Equipment."
            );
            return;
        }

        if (SelectedEquipmentPresetIndex >= 0)
        {
            CaptureEquipmentPreset();
            return;
        }

        foreach (var item in InventoryManager.Instance.GetAllEquippedItems())
        {
            if (item == null || item.Data == null)
                continue;

            selectedEquipmentItems.Add(item);
            Debug.Log($"Current Equipment: {item.Data.itemName}");
        }

        Debug.Log(
            $"Current Equipment captured: {selectedEquipmentItems.Count} item(s)."
        );
    }

    private void CaptureEquipmentPreset()
    {
        var presets = InventoryManager.Instance.EquipmentPresets.Presets;

        if (SelectedEquipmentPresetIndex >= presets.Count)
        {
            Debug.LogError(
                $"TravelManager: Equipment Preset index " +
                $"{SelectedEquipmentPresetIndex} is invalid."
            );
            return;
        }

        var preset = presets[SelectedEquipmentPresetIndex];

        foreach (var category in EquipmentPreset.SupportedCategories)
        {
            for (int slotIndex = 0;
                slotIndex < preset.SlotCount(category);
                slotIndex++)
            {
                var presetItem = preset.GetSlot(category, slotIndex);

                if (presetItem == null || presetItem.Data == null)
                    continue;

                var sourceItem = FindOwnedItem(presetItem.SourceInstanceId);

                if (sourceItem == null)
                {
                    Debug.LogWarning(
                        $"Equipment Preset '{preset.PresetName}': " +
                        $"missing item {presetItem.Data.itemName}."
                    );
                    continue;
                }

                selectedEquipmentItems.Add(sourceItem);
                Debug.Log(
                    $"Equipment Preset '{preset.PresetName}': " +
                    $"{sourceItem.Data.itemName}"
                );
            }
        }

        Debug.Log(
            $"Equipment Preset '{preset.PresetName}' captured: " +
            $"{selectedEquipmentItems.Count} item(s)."
        );
    }

    private ItemInstance FindOwnedItem(string instanceId)
    {
        if (string.IsNullOrEmpty(instanceId))
            return null;

        foreach (var item in InventoryManager.Instance.GetAllItems())
        {
            if (item != null && item.InstanceId == instanceId)
                return item;
        }

        foreach (var item in InventoryManager.Instance.GetAllEquippedItems())
        {
            if (item != null && item.InstanceId == instanceId)
                return item;
        }

        return null;
    }

    private void CaptureSelectedBackpack()
    {
        selectedBackpackItems.Clear();

        if (InventoryManager.Instance == null)
        {
            Debug.LogError(
                "TravelManager: не вдалося зчитати Backpack Preset."
            );
            return;
        }

        var presets = InventoryManager.Instance.BackpackPresets.Presets;

        if (SelectedBackpackPresetIndex < 0 ||
            SelectedBackpackPresetIndex >= presets.Count)
        {
            Debug.LogError(
                $"TravelManager: Backpack Preset index " +
                $"{SelectedBackpackPresetIndex} is invalid."
            );
            return;
        }

        var preset = presets[SelectedBackpackPresetIndex];

        foreach (var presetItem in preset.Grid.GetAllItems())
        {
            if (presetItem == null || presetItem.Data == null)
                continue;

            var expeditionItem = new ItemInstance(
                presetItem.Data,
                presetItem.StackCount
            );

            selectedBackpackItems.Add(expeditionItem);
            Debug.Log(
                $"Backpack Preset '{preset.PresetName}': " +
                $"{expeditionItem.Data.itemName} x{expeditionItem.StackCount}"
            );
        }

        Debug.Log(
            $"Backpack Preset '{preset.PresetName}' captured: " +
            $"{selectedBackpackItems.Count} stack(s)."
        );
    }

    public int GetSelectedBackpackItemCount(ItemData itemData)
    {
        if (itemData == null)
            return 0;

        int total = 0;

        foreach (var item in selectedBackpackItems)
        {
            if (item != null && item.Data == itemData)
                total += Mathf.Max(0, item.StackCount);
        }

        return total;
    }

    public bool TryConsumeSelectedBackpackItem(
        ItemData itemData,
        int count
    )
    {
        count = Mathf.Max(0, count);

        if (count == 0)
            return true;

        if (itemData == null ||
            GetSelectedBackpackItemCount(itemData) < count)
        {
            return false;
        }

        int remainingToConsume = count;

        foreach (var item in selectedBackpackItems)
        {
            if (item == null ||
                item.Data != itemData ||
                item.StackCount <= 0)
            {
                continue;
            }

            int consumed = Mathf.Min(
                item.StackCount,
                remainingToConsume
            );

            item.StackCount -= consumed;
            remainingToConsume -= consumed;

            if (remainingToConsume == 0)
                break;
        }

        Debug.Log(
            $"Expedition backpack consumed: {itemData.itemName} x{count}. " +
            $"Remaining: {GetSelectedBackpackItemCount(itemData)}."
        );

        return true;
    }

    private void StartTravelInternal(
        string destinationName,
        float duration
    )
    {
        if (IsTraveling)
            return;

        CurrentRegion = destinationName;
        IsPausedForBattle = false;
        PlanAmbush(duration);

        StartCoroutine(
            TravelRoutine(Mathf.Max(1f, duration))
        );
    }

    private IEnumerator TravelRoutine(float duration)
    {
        IsTraveling = true;
        TimeRemaining = duration;

        OnTravelStarted?.Invoke();

        while (TimeRemaining > 0f)
        {
            yield return null;

            TimeRemaining -= Time.deltaTime;

            OnTravelTick?.Invoke(
                Mathf.Max(TimeRemaining, 0f)
            );

            if (ambushScheduled &&
                TimeRemaining <= ambushTriggerTimeRemaining)
            {
                PauseForAmbush();
                yield break;
            }
        }

        IsTraveling = false;
        OnTravelEnded?.Invoke();

        ResolveArrival();
    }

    private void ResolveArrival()
    {
        Debug.Log("Подорож без пригод. Прибуття на локацію.");
        GameManager.Instance.ChangeState(GameState.Search);
    }

    private void PlanAmbush(float duration)
    {
        float locationChance = CurrentLocation != null
            ? CurrentLocation.outboundAmbushChancePercent
            : 0f;

        float regionMultiplier = CurrentRegionData != null
            ? CurrentRegionData.AmbushChanceMultiplier
            : 1f;

        float finalChance = Mathf.Clamp(
            locationChance * regionMultiplier,
            0f,
            100f
        );

        float roll = UnityEngine.Random.Range(0f, 100f);
        ambushScheduled =
            finalChance >= 100f ||
            (finalChance > 0f && roll < finalChance);

        Debug.Log(
            $"Ambush check: {locationChance:0.#}% x " +
            $"{regionMultiplier:0.##} = {finalChance:0.#}% | " +
            $"roll {roll:0.#}"
        );

        if (!ambushScheduled)
        {
            ambushTriggerTimeRemaining = -1f;
            return;
        }

        CurrentEncounter = SelectEncounter();

        if (CurrentEncounter == null)
        {
            ambushScheduled = false;
            ambushTriggerTimeRemaining = -1f;

            Debug.LogWarning(
                "TravelManager: для локації немає доступного encounter."
            );
            return;
        }

        Debug.Log(
            $"Selected encounter: {CurrentEncounter.displayName} " +
            $"({CurrentEncounter.difficulty}, " +
            $"{CurrentEncounter.Enemies.Count} enemy slots)."
        );

        float triggerProgress = UnityEngine.Random.Range(0.2f, 0.8f);
        ambushTriggerTimeRemaining = duration * (1f - triggerProgress);

        Debug.Log(
            $"Ambush scheduled at {triggerProgress * 100f:0.#}% " +
            $"of the route."
        );
    }

    private EncounterData SelectEncounter()
    {
        if (CurrentLocation == null ||
            CurrentLocation.PossibleBattleEncounters == null)
        {
            return null;
        }

        float totalWeight = 0f;

        foreach (var entry in CurrentLocation.PossibleBattleEncounters)
        {
            if (entry?.encounter != null && entry.weight > 0f)
                totalWeight += entry.weight;
        }

        if (totalWeight <= 0f)
            return null;

        float roll = UnityEngine.Random.Range(0f, totalWeight);
        EncounterData fallback = null;

        foreach (var entry in CurrentLocation.PossibleBattleEncounters)
        {
            if (entry?.encounter == null || entry.weight <= 0f)
                continue;

            fallback = entry.encounter;
            roll -= entry.weight;

            if (roll <= 0f)
                return entry.encounter;
        }

        return fallback;
    }

    private void PauseForAmbush()
    {
        ambushScheduled = false;
        IsPausedForBattle = true;
        TimeRemaining = Mathf.Max(TimeRemaining, 0f);

        Debug.Log(
            $"Контекстна засідка: подорож призупинена, " +
            $"залишилось {TimeRemaining:0.0} с."
        );

        GameManager.Instance.ChangeState(GameState.Battle);
    }

    [ContextMenu("Resume Travel After Battle")]
    public void ResumeTravelAfterBattle()
    {
        if (!IsTraveling || !IsPausedForBattle)
        {
            Debug.LogWarning(
                "TravelManager: немає призупиненої подорожі."
            );
            return;
        }

        IsPausedForBattle = false;

        Debug.Log(
            $"Подорож відновлено. Залишилось {TimeRemaining:0.0} с."
        );

        GameManager.Instance.ChangeState(GameState.Travel);
        StartCoroutine(TravelRoutine(TimeRemaining));
    }
}
