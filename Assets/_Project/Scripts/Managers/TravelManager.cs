using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class ExpeditionRemovedBackpackItem
{
    public string InstanceId { get; }
    public ItemData Item { get; }
    public int Count { get; }

    public ExpeditionRemovedBackpackItem(
        string instanceId,
        ItemData item,
        int count
    )
    {
        InstanceId = instanceId ?? string.Empty;
        Item = item;
        Count = Math.Max(0, count);
    }
}

public sealed class ExpeditionBackpackAllocation
{
    public string InstanceId { get; }
    public ItemData Item { get; }
    public int Count { get; }

    public ExpeditionBackpackAllocation(
        string instanceId,
        ItemData item,
        int count
    )
    {
        InstanceId = instanceId ?? string.Empty;
        Item = item;
        Count = Math.Max(0, count);
    }
}

public sealed class ExpeditionBackpackAddResult
{
    private readonly List<ExpeditionBackpackAllocation> allocations;

    public int RequestedCount { get; }
    public int AddedCount { get; }
    public IReadOnlyList<ExpeditionBackpackAllocation>
        Allocations => allocations;

    public ExpeditionBackpackAddResult(
        int requestedCount,
        int addedCount,
        IEnumerable<ExpeditionBackpackAllocation>
            addedAllocations
    )
    {
        RequestedCount = Math.Max(0, requestedCount);
        AddedCount = Math.Min(
            Math.Max(0, addedCount),
            RequestedCount
        );
        allocations = addedAllocations != null
            ? new List<ExpeditionBackpackAllocation>(
                addedAllocations
            )
            : new List<ExpeditionBackpackAllocation>();
    }
}

public sealed class ExpeditionBackpackReplacementResult
{
    private readonly List<ExpeditionRemovedBackpackItem>
        removedItems;
    private readonly List<ExpeditionBackpackAllocation>
        addedAllocations;

    public int AddedCount { get; }
    public IReadOnlyList<ExpeditionRemovedBackpackItem>
        RemovedItems => removedItems;
    public IReadOnlyList<ExpeditionBackpackAllocation>
        AddedAllocations => addedAllocations;

    public ExpeditionBackpackReplacementResult(
        int addedCount,
        IEnumerable<ExpeditionRemovedBackpackItem> removed,
        IEnumerable<ExpeditionBackpackAllocation> allocations
    )
    {
        AddedCount = Math.Max(0, addedCount);
        removedItems = removed != null
            ? new List<ExpeditionRemovedBackpackItem>(removed)
            : new List<ExpeditionRemovedBackpackItem>();
        addedAllocations = allocations != null
            ? new List<ExpeditionBackpackAllocation>(allocations)
            : new List<ExpeditionBackpackAllocation>();
    }
}

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
    public int SelectedBackpackGridWidth =>
        selectedBackpackGrid?.Width ?? 0;
    public int SelectedBackpackGridHeight =>
        selectedBackpackGrid?.Height ?? 0;

    private readonly List<ItemInstance> selectedEquipmentItems = new();
    private readonly List<ItemInstance> selectedBackpackItems = new();
    private InventoryGrid selectedBackpackGrid;

    private bool ambushScheduled;
    private float ambushTriggerTimeRemaining;

    public event Action OnTravelStarted;
    public event Action<float> OnTravelTick;
    public event Action OnTravelEnded;
    public event Action OnSelectedBackpackChanged;

    [Header("Expedition Backpack Runtime")]
    [Min(1)]
    [SerializeField] private int expeditionBackpackWidth = 6;

    [Min(1)]
    [SerializeField] private int expeditionBackpackHeight = 4;

    [Header("Travel")]
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
        selectedBackpackGrid = null;

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
        selectedBackpackGrid = null;

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
        selectedBackpackGrid = new InventoryGrid(
            Mathf.Max(1, expeditionBackpackWidth),
            Mathf.Max(1, expeditionBackpackHeight)
        );

        foreach (var presetItem in preset.Grid.GetAllItems())
        {
            if (presetItem == null || presetItem.Data == null)
                continue;

            var expeditionItem = new ItemInstance(
                presetItem.Data,
                presetItem.StackCount,
                ItemInstanceOrigin.DepartureBackpack,
                presetItem.StackCount
            );

            var position = preset.Grid.GetPosition(presetItem);
            bool added =
                position.HasValue &&
                selectedBackpackGrid.TryAddItem(
                    expeditionItem,
                    position.Value.x,
                    position.Value.y
                );

            if (!added)
                added = selectedBackpackGrid.TryAddItem(expeditionItem);

            if (!added)
            {
                Debug.LogError(
                    $"TravelManager: не вдалося скопіювати " +
                    $"{expeditionItem.Data.itemName} в runtime backpack."
                );
                continue;
            }

            selectedBackpackItems.Add(expeditionItem);
            Debug.Log(
                $"Backpack Preset '{preset.PresetName}': " +
                $"{expeditionItem.Data.itemName} " +
                $"x{expeditionItem.StackCount}, " +
                $"protected x{expeditionItem.ProtectedCount}"
            );
        }

        Debug.Log(
            $"Backpack Preset '{preset.PresetName}' captured: " +
            $"{selectedBackpackItems.Count} stack(s), runtime grid " +
            $"{selectedBackpackGrid.Width}x{selectedBackpackGrid.Height}."
        );

        NotifySelectedBackpackChanged();
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

    public int GetSelectedBackpackProtectedCount(ItemData itemData)
    {
        if (itemData == null)
            return 0;

        int total = 0;

        foreach (var item in selectedBackpackItems)
        {
            if (item != null && item.Data == itemData)
                total += Mathf.Max(0, item.ProtectedCount);
        }

        return total;
    }

    public IReadOnlyDictionary<string, int>
        CreateSelectedBackpackReplaceableSnapshot()
    {
        var snapshot = new Dictionary<string, int>(
            StringComparer.Ordinal
        );

        foreach (var item in selectedBackpackItems)
        {
            if (item == null ||
                item.Data == null ||
                item.UnprotectedCount <= 0 ||
                (item.Origins & ItemInstanceOrigin.ExpeditionLoot) == 0)
            {
                continue;
            }

            snapshot[item.InstanceId] = item.UnprotectedCount;
        }

        return snapshot;
    }

    public bool TryGetSelectedBackpackItemPosition(
        ItemInstance item,
        out int x,
        out int y
    )
    {
        x = 0;
        y = 0;

        if (item == null || selectedBackpackGrid == null)
            return false;

        var position = selectedBackpackGrid.GetPosition(item);

        if (!position.HasValue)
            return false;

        x = position.Value.x;
        y = position.Value.y;
        return true;
    }

    public int AddSelectedBackpackItem(
        ItemData itemData,
        int requestedCount
    )
    {
        return AddSelectedBackpackItemWithReceipt(
            itemData,
            requestedCount
        ).AddedCount;
    }

    public bool CanMergeSelectedBackpackLootInto(
        ItemData itemData,
        int requestedCount,
        string targetInstanceId
    )
    {
        return TryGetSelectedBackpackMergeTarget(
            itemData,
            requestedCount,
            targetInstanceId,
            out _
        );
    }

    public bool TryMergeSelectedBackpackLootInto(
        ItemData itemData,
        int requestedCount,
        string targetInstanceId,
        out ExpeditionBackpackAddResult result
    )
    {
        result = null;

        if (!TryGetSelectedBackpackMergeTarget(
            itemData,
            requestedCount,
            targetInstanceId,
            out ItemInstance target
        ))
        {
            return false;
        }

        int added = target.AddUnits(
            requestedCount,
            ItemInstanceOrigin.ExpeditionLoot
        );

        if (added != requestedCount)
            return false;

        result = new ExpeditionBackpackAddResult(
            requestedCount,
            added,
            new[]
            {
                new ExpeditionBackpackAllocation(
                    target.InstanceId,
                    itemData,
                    added
                )
            }
        );

        Debug.Log(
            $"Expedition backpack exact stack merge: " +
            $"{itemData.itemName} x{added} into " +
            $"{target.InstanceId}; stack is now x{target.StackCount}."
        );
        NotifySelectedBackpackChanged();
        return true;
    }

    private bool TryGetSelectedBackpackMergeTarget(
        ItemData itemData,
        int requestedCount,
        string targetInstanceId,
        out ItemInstance target
    )
    {
        target = null;

        if (itemData == null ||
            !itemData.isStackable ||
            requestedCount <= 0 ||
            string.IsNullOrEmpty(targetInstanceId) ||
            selectedBackpackGrid == null)
        {
            return false;
        }

        foreach (ItemInstance candidate in selectedBackpackItems)
        {
            if (candidate != null &&
                candidate.InstanceId == targetInstanceId)
            {
                target = candidate;
                break;
            }
        }

        if (target == null ||
            target.Data != itemData ||
            target.StackCount <= 0 ||
            !selectedBackpackGrid.GetPosition(target).HasValue)
        {
            target = null;
            return false;
        }

        int maximumStackSize = Mathf.Max(1, itemData.maxStackSize);
        bool canFit = requestedCount <=
            maximumStackSize - target.StackCount;

        if (!canFit)
            target = null;

        return canFit;
    }

    public ExpeditionBackpackAddResult
        AddSelectedBackpackItemWithReceipt(
            ItemData itemData,
            int requestedCount
        )
    {
        requestedCount = Mathf.Max(0, requestedCount);
        var allocations =
            new List<ExpeditionBackpackAllocation>();

        if (itemData == null ||
            requestedCount == 0 ||
            selectedBackpackGrid == null)
        {
            return new ExpeditionBackpackAddResult(
                requestedCount,
                0,
                allocations
            );
        }

        int remaining = requestedCount;
        int added = 0;
        int maximumStackSize = itemData.isStackable
            ? Mathf.Max(1, itemData.maxStackSize)
            : 1;

        if (itemData.isStackable)
        {
            foreach (var existingItem in selectedBackpackItems)
            {
                if (existingItem == null ||
                    existingItem.Data != itemData ||
                    existingItem.StackCount >= maximumStackSize)
                {
                    continue;
                }

                int addedToStack = Mathf.Min(
                    maximumStackSize - existingItem.StackCount,
                    remaining
                );

                existingItem.AddUnits(
                    addedToStack,
                    ItemInstanceOrigin.ExpeditionLoot
                );
                allocations.Add(
                    new ExpeditionBackpackAllocation(
                        existingItem.InstanceId,
                        itemData,
                        addedToStack
                    )
                );
                remaining -= addedToStack;
                added += addedToStack;

                if (remaining == 0)
                    break;
            }
        }

        while (remaining > 0)
        {
            int newStackCount = itemData.isStackable
                ? Mathf.Min(maximumStackSize, remaining)
                : 1;

            var newItem = new ItemInstance(
                itemData,
                newStackCount,
                ItemInstanceOrigin.ExpeditionLoot
            );

            if (!selectedBackpackGrid.TryAddItem(newItem))
                break;

            selectedBackpackItems.Add(newItem);
            allocations.Add(
                new ExpeditionBackpackAllocation(
                    newItem.InstanceId,
                    itemData,
                    newStackCount
                )
            );
            remaining -= newStackCount;
            added += newStackCount;
        }

        if (added > 0)
        {
            Debug.Log(
                $"Expedition backpack added: {itemData.itemName} " +
                $"x{added}/{requestedCount}."
            );
            NotifySelectedBackpackChanged();
        }

        if (remaining > 0)
        {
            Debug.LogWarning(
                $"Expedition backpack has no space for " +
                $"{itemData.itemName} x{remaining}."
            );
        }

        return new ExpeditionBackpackAddResult(
            requestedCount,
            added,
            allocations
        );
    }

    public bool CanReplaceSelectedBackpackLoot(
        ItemData pendingItem,
        int pendingCount,
        IReadOnlyCollection<string> removableInstanceIds,
        IReadOnlyDictionary<string, int> replaceableAllowances
    )
    {
        return TryBuildBackpackReplacementPlan(
            pendingItem,
            pendingCount,
            removableInstanceIds,
            replaceableAllowances,
            out _
        );
    }

    public bool TryReplaceSelectedBackpackLoot(
        ItemData pendingItem,
        int pendingCount,
        IReadOnlyCollection<string> removableInstanceIds,
        IReadOnlyDictionary<string, int> replaceableAllowances,
        out ExpeditionBackpackReplacementResult result
    )
    {
        result = null;

        if (!TryBuildBackpackReplacementPlan(
            pendingItem,
            pendingCount,
            removableInstanceIds,
            replaceableAllowances,
            out BackpackReplacementPlan plan
        ))
        {
            return false;
        }

        return TryCommitBackpackReplacementPlan(
            pendingItem,
            pendingCount,
            plan,
            "manual replacement",
            out result
        );
    }

    public bool CanPlaceSelectedBackpackLootAt(
        ItemData pendingItem,
        int pendingCount,
        int targetX,
        int targetY,
        IReadOnlyCollection<string> removableInstanceIds,
        IReadOnlyDictionary<string, int> replaceableAllowances
    )
    {
        return TryBuildExactBackpackPlacementPlan(
            pendingItem,
            pendingCount,
            targetX,
            targetY,
            removableInstanceIds,
            replaceableAllowances,
            out _
        );
    }

    public bool TryPlaceSelectedBackpackLootAt(
        ItemData pendingItem,
        int pendingCount,
        int targetX,
        int targetY,
        IReadOnlyCollection<string> removableInstanceIds,
        IReadOnlyDictionary<string, int> replaceableAllowances,
        out ExpeditionBackpackReplacementResult result
    )
    {
        result = null;

        if (!TryBuildExactBackpackPlacementPlan(
            pendingItem,
            pendingCount,
            targetX,
            targetY,
            removableInstanceIds,
            replaceableAllowances,
            out BackpackReplacementPlan plan
        ))
        {
            return false;
        }

        return TryCommitBackpackReplacementPlan(
            pendingItem,
            pendingCount,
            plan,
            $"exact placement at ({targetX}, {targetY})",
            out result
        );
    }

    public bool TryDiscardSelectedBackpackLoot(
        IReadOnlyCollection<string> removableInstanceIds,
        IReadOnlyDictionary<string, int> replaceableAllowances,
        out ExpeditionBackpackReplacementResult result
    )
    {
        result = null;

        if (selectedBackpackGrid == null ||
            removableInstanceIds == null ||
            replaceableAllowances == null)
        {
            return false;
        }

        var requestedIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (string instanceId in removableInstanceIds)
        {
            if (!string.IsNullOrEmpty(instanceId))
                requestedIds.Add(instanceId);
        }

        if (requestedIds.Count == 0)
            return false;

        var removableItems = new List<ItemInstance>();
        var removedReceipts =
            new List<ExpeditionRemovedBackpackItem>();

        foreach (ItemInstance item in selectedBackpackItems)
        {
            if (item == null ||
                !requestedIds.Contains(item.InstanceId))
            {
                continue;
            }

            if (!CanRemoveDuringBattleLootResolution(item) ||
                !replaceableAllowances.TryGetValue(
                    item.InstanceId,
                    out int allowedCount
                ) ||
                allowedCount < item.StackCount ||
                !selectedBackpackGrid.GetPosition(item).HasValue)
            {
                return false;
            }

            removableItems.Add(item);
            removedReceipts.Add(
                new ExpeditionRemovedBackpackItem(
                    item.InstanceId,
                    item.Data,
                    item.StackCount
                )
            );
        }

        if (removableItems.Count != requestedIds.Count)
            return false;

        foreach (ItemInstance item in removableItems)
        {
            selectedBackpackGrid.RemoveItem(item);
            selectedBackpackItems.Remove(item);
        }

        result = new ExpeditionBackpackReplacementResult(
            0,
            removedReceipts,
            null
        );

        Debug.Log(
            $"Expedition backpack discarded " +
            $"{removedReceipts.Count} selected stack(s)."
        );
        NotifySelectedBackpackChanged();
        return true;
    }

    private bool TryCommitBackpackReplacementPlan(
        ItemData pendingItem,
        int pendingCount,
        BackpackReplacementPlan plan,
        string operationLabel,
        out ExpeditionBackpackReplacementResult result
    )
    {
        result = null;

        if (pendingItem == null ||
            pendingCount <= 0 ||
            plan == null ||
            selectedBackpackGrid == null)
        {
            return false;
        }

        foreach (var removal in plan.Removals)
            selectedBackpackGrid.RemoveItem(removal.Item);

        var placedAdditions = new List<PlannedAddition>();

        foreach (var addition in plan.Additions)
        {
            if (selectedBackpackGrid.TryAddItem(
                addition.Item,
                addition.X,
                addition.Y
            ))
            {
                placedAdditions.Add(addition);
                continue;
            }

            foreach (var placed in placedAdditions)
                selectedBackpackGrid.RemoveItem(placed.Item);

            bool rollbackSucceeded = true;

            foreach (var removal in plan.Removals)
            {
                if (!selectedBackpackGrid.TryAddItem(
                    removal.Item,
                    removal.X,
                    removal.Y
                ))
                {
                    rollbackSucceeded = false;
                }
            }

            if (!rollbackSucceeded)
            {
                Debug.LogError(
                    "TravelManager: runtime backpack replacement " +
                    "rollback failed."
                );
            }

            return false;
        }

        foreach (var removal in plan.Removals)
            selectedBackpackItems.Remove(removal.Item);

        foreach (var addition in plan.Additions)
            selectedBackpackItems.Add(addition.Item);

        foreach (var merge in plan.Merges)
        {
            merge.Target.AddUnits(
                merge.Count,
                ItemInstanceOrigin.ExpeditionLoot
            );
        }

        var removedItems =
            new List<ExpeditionRemovedBackpackItem>();
        var addedAllocations =
            new List<ExpeditionBackpackAllocation>();

        foreach (var removal in plan.Removals)
        {
            removedItems.Add(
                new ExpeditionRemovedBackpackItem(
                    removal.Item.InstanceId,
                    removal.Item.Data,
                    removal.Item.StackCount
                )
            );
        }

        foreach (var merge in plan.Merges)
        {
            addedAllocations.Add(
                new ExpeditionBackpackAllocation(
                    merge.Target.InstanceId,
                    pendingItem,
                    merge.Count
                )
            );
        }

        foreach (var addition in plan.Additions)
        {
            addedAllocations.Add(
                new ExpeditionBackpackAllocation(
                    addition.Item.InstanceId,
                    pendingItem,
                    addition.Item.StackCount
                )
            );
        }

        result = new ExpeditionBackpackReplacementResult(
            pendingCount,
            removedItems,
            addedAllocations
        );

        Debug.Log(
            $"Expedition backpack {operationLabel}: added " +
            $"{pendingItem.itemName} x{pendingCount}, removed " +
            $"{removedItems.Count} stack(s)."
        );

        NotifySelectedBackpackChanged();
        return true;
    }

    private bool TryBuildExactBackpackPlacementPlan(
        ItemData pendingItem,
        int pendingCount,
        int targetX,
        int targetY,
        IReadOnlyCollection<string> removableInstanceIds,
        IReadOnlyDictionary<string, int> replaceableAllowances,
        out BackpackReplacementPlan plan
    )
    {
        plan = null;
        pendingCount = Mathf.Max(0, pendingCount);

        if (pendingItem == null ||
            pendingCount == 0 ||
            selectedBackpackGrid == null ||
            replaceableAllowances == null)
        {
            return false;
        }

        int maximumStackSize = pendingItem.isStackable
            ? Mathf.Max(1, pendingItem.maxStackSize)
            : 1;

        if (pendingCount > maximumStackSize)
            return false;

        var requestedIds = new HashSet<string>();

        if (removableInstanceIds != null)
        {
            foreach (string instanceId in removableInstanceIds)
            {
                if (!string.IsNullOrEmpty(instanceId))
                    requestedIds.Add(instanceId);
            }
        }

        var removalById = new Dictionary<string, ItemInstance>();

        foreach (ItemInstance item in selectedBackpackItems)
        {
            if (item == null ||
                !requestedIds.Contains(item.InstanceId))
            {
                continue;
            }

            if (!CanRemoveDuringBattleLootResolution(item) ||
                !replaceableAllowances.TryGetValue(
                    item.InstanceId,
                    out int allowedCount
                ) ||
                allowedCount < item.StackCount)
            {
                return false;
            }

            removalById[item.InstanceId] = item;
        }

        if (removalById.Count != requestedIds.Count)
            return false;

        var candidate = new BackpackReplacementPlan();
        var shadowGrid = new InventoryGrid(
            selectedBackpackGrid.Width,
            selectedBackpackGrid.Height
        );

        foreach (ItemInstance item in selectedBackpackItems)
        {
            if (item == null || item.Data == null)
                return false;

            var position = selectedBackpackGrid.GetPosition(item);

            if (!position.HasValue)
                return false;

            if (removalById.ContainsKey(item.InstanceId))
            {
                candidate.Removals.Add(new PlannedRemoval(
                    item,
                    position.Value.x,
                    position.Value.y
                ));
                continue;
            }

            if (!shadowGrid.TryAddItem(
                item,
                position.Value.x,
                position.Value.y
            ))
            {
                return false;
            }
        }

        var newItem = new ItemInstance(
            pendingItem,
            pendingCount,
            ItemInstanceOrigin.ExpeditionLoot
        );

        if (!shadowGrid.TryAddItem(newItem, targetX, targetY))
            return false;

        candidate.Additions.Add(new PlannedAddition(
            newItem,
            targetX,
            targetY
        ));
        plan = candidate;
        return true;
    }

    private bool TryBuildBackpackReplacementPlan(
        ItemData pendingItem,
        int pendingCount,
        IReadOnlyCollection<string> removableInstanceIds,
        IReadOnlyDictionary<string, int> replaceableAllowances,
        out BackpackReplacementPlan plan
    )
    {
        plan = null;
        pendingCount = Mathf.Max(0, pendingCount);

        if (pendingItem == null ||
            pendingCount == 0 ||
            selectedBackpackGrid == null ||
            removableInstanceIds == null ||
            removableInstanceIds.Count == 0 ||
            replaceableAllowances == null)
        {
            return false;
        }

        var requestedIds = new HashSet<string>();

        foreach (string instanceId in removableInstanceIds)
        {
            if (!string.IsNullOrEmpty(instanceId))
                requestedIds.Add(instanceId);
        }

        if (requestedIds.Count == 0)
            return false;

        var removalById = new Dictionary<string, ItemInstance>();

        foreach (var item in selectedBackpackItems)
        {
            if (item == null ||
                !requestedIds.Contains(item.InstanceId))
            {
                continue;
            }

            if (!CanRemoveDuringBattleLootResolution(item) ||
                !replaceableAllowances.TryGetValue(
                    item.InstanceId,
                    out int allowedCount
                ) ||
                allowedCount < item.StackCount)
            {
                return false;
            }

            removalById[item.InstanceId] = item;
        }

        if (removalById.Count != requestedIds.Count)
            return false;

        var candidate = new BackpackReplacementPlan();
        var shadowGrid = new InventoryGrid(
            selectedBackpackGrid.Width,
            selectedBackpackGrid.Height
        );

        foreach (var item in selectedBackpackItems)
        {
            if (item == null || item.Data == null)
                return false;

            var position = selectedBackpackGrid.GetPosition(item);

            if (!position.HasValue)
                return false;

            if (removalById.ContainsKey(item.InstanceId))
            {
                candidate.Removals.Add(
                    new PlannedRemoval(
                        item,
                        position.Value.x,
                        position.Value.y
                    )
                );
                continue;
            }

            if (!shadowGrid.TryAddItem(
                item,
                position.Value.x,
                position.Value.y
            ))
            {
                return false;
            }
        }

        int remaining = pendingCount;
        int maximumStackSize = pendingItem.isStackable
            ? Mathf.Max(1, pendingItem.maxStackSize)
            : 1;

        if (pendingItem.isStackable)
        {
            foreach (var item in selectedBackpackItems)
            {
                if (remaining == 0)
                    break;

                if (item == null ||
                    removalById.ContainsKey(item.InstanceId) ||
                    item.Data != pendingItem ||
                    item.StackCount >= maximumStackSize)
                {
                    continue;
                }

                int mergedCount = Mathf.Min(
                    maximumStackSize - item.StackCount,
                    remaining
                );

                candidate.Merges.Add(
                    new PlannedMerge(item, mergedCount)
                );
                remaining -= mergedCount;
            }
        }

        while (remaining > 0)
        {
            int newStackCount = pendingItem.isStackable
                ? Mathf.Min(maximumStackSize, remaining)
                : 1;

            var newItem = new ItemInstance(
                pendingItem,
                newStackCount,
                ItemInstanceOrigin.ExpeditionLoot
            );

            if (!shadowGrid.TryAddItem(newItem))
                return false;

            var newPosition = shadowGrid.GetPosition(newItem);

            if (!newPosition.HasValue)
                return false;

            candidate.Additions.Add(
                new PlannedAddition(
                    newItem,
                    newPosition.Value.x,
                    newPosition.Value.y
                )
            );
            remaining -= newStackCount;
        }

        plan = candidate;
        return true;
    }

    private static bool CanRemoveDuringBattleLootResolution(
        ItemInstance item
    )
    {
        return item != null &&
            item.Data != null &&
            item.StackCount > 0 &&
            item.ProtectedCount == 0 &&
            (item.Origins & ItemInstanceOrigin.ExpeditionLoot) != 0;
    }

    private sealed class BackpackReplacementPlan
    {
        public readonly List<PlannedRemoval> Removals = new();
        public readonly List<PlannedMerge> Merges = new();
        public readonly List<PlannedAddition> Additions = new();
    }

    private sealed class PlannedRemoval
    {
        public ItemInstance Item { get; }
        public int X { get; }
        public int Y { get; }

        public PlannedRemoval(ItemInstance item, int x, int y)
        {
            Item = item;
            X = x;
            Y = y;
        }
    }

    private sealed class PlannedMerge
    {
        public ItemInstance Target { get; }
        public int Count { get; }

        public PlannedMerge(ItemInstance target, int count)
        {
            Target = target;
            Count = count;
        }
    }

    private sealed class PlannedAddition
    {
        public ItemInstance Item { get; }
        public int X { get; }
        public int Y { get; }

        public PlannedAddition(ItemInstance item, int x, int y)
        {
            Item = item;
            X = x;
            Y = y;
        }
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

        for (int i = 0;
            i < selectedBackpackItems.Count && remainingToConsume > 0;
            i++)
        {
            var item = selectedBackpackItems[i];

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

            item.ConsumeUnits(consumed);
            remainingToConsume -= consumed;

            if (item.StackCount <= 0)
            {
                selectedBackpackGrid?.RemoveItem(item);
                selectedBackpackItems.RemoveAt(i);
                i--;
            }
        }

        Debug.Log(
            $"Expedition backpack consumed: {itemData.itemName} x{count}. " +
            $"Remaining: {GetSelectedBackpackItemCount(itemData)}. " +
            $"Protected remaining: " +
            $"{GetSelectedBackpackProtectedCount(itemData)}."
        );

        NotifySelectedBackpackChanged();

        return true;
    }

    private void NotifySelectedBackpackChanged()
    {
        var handlers = OnSelectedBackpackChanged;

        if (handlers == null)
            return;

        foreach (Delegate callback in handlers.GetInvocationList())
        {
            try
            {
                ((Action)callback).Invoke();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }
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
    private void ResumeTravelAfterBattleFromContextMenu()
    {
        ResumeTravelAfterBattle();
    }

    public bool ResumeTravelAfterBattle()
    {
        if (!IsTraveling || !IsPausedForBattle)
        {
            Debug.LogWarning(
                "TravelManager: немає призупиненої подорожі."
            );
            return false;
        }

        IsPausedForBattle = false;

        Debug.Log(
            $"Подорож відновлено. Залишилось {TimeRemaining:0.0} с."
        );

        GameManager.Instance.ChangeState(GameState.Travel);
        StartCoroutine(TravelRoutine(TimeRemaining));
        return true;
    }
}
