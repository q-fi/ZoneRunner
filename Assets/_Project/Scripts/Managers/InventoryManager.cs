using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    [Header("Grid Size")]
    [SerializeField] private int gridWidth = 6;
    [SerializeField] private int gridHeight = 8;
    [SerializeField] private int presetGridHeight = 4; 
    public InventoryGrid Grid { get; private set; }
    public BackpackPresetCollection BackpackPresets { get; private set; }
    public EquipmentPresetCollection EquipmentPresets { get; private set; }

    private readonly Dictionary<SlotCategory, ItemInstance[]> equipment = new()
    {
        { SlotCategory.Weapon,   new ItemInstance[2] },
        { SlotCategory.Armor,    new ItemInstance[1] },
        { SlotCategory.Detector, new ItemInstance[1] },
        { SlotCategory.Artifact, new ItemInstance[3] },
        { SlotCategory.Medicine, new ItemInstance[2] },
    };

    public event Action OnInventoryChanged;
    public event Action OnEquipmentChanged;
    public event Action<string> OnInventoryMessage; 
    public event Action OnPresetChanged;
    public event Action OnCurrentPresetChanged;
    public event Action OnEquipmentPresetChanged;
    public event Action OnCurrentEquipmentPresetChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        Grid = new InventoryGrid(gridWidth, gridHeight);
        BackpackPresets = new BackpackPresetCollection(gridWidth, presetGridHeight); // NEW: менша висота
        EquipmentPresets = new EquipmentPresetCollection();
    }

    /// <summary>Додає новий фізичний екземпляр предмета .</summary>
    public bool AddItem(ItemData template, int count = 1)
    {
        if (template.isStackable)
        {
            var existing = Grid.FindStackableInstance(template);
            if (existing != null)
            {
                int freeSpace = template.maxStackSize - existing.StackCount;
                int toAdd = Mathf.Min(freeSpace, count);
                existing.AddUnits(toAdd);
                count -= toAdd;
                OnInventoryChanged?.Invoke();

                if (count <= 0)
                {
                    Debug.Log($"Додано в стак: {template.itemName} (+{toAdd})");
                    return true;
                }
            }
        }

        var instance = new ItemInstance(template, count);
        bool success = Grid.TryAddItem(instance);

        if (success)
        {
            Debug.Log($"Додано в інвентар: {template.itemName}");
            OnInventoryChanged?.Invoke();
        }
        else
        {
            Debug.Log($"Немає місця для: {template.itemName}");
            OnInventoryMessage?.Invoke($"Немає місця для: {template.itemName}");
        }
        return success;
    }

    public void RemoveItem(ItemInstance instance)
    {
        Grid.RemoveItem(instance);
        OnInventoryChanged?.Invoke();
    }

    public IEnumerable<ItemInstance> GetAllItems() => Grid.GetAllItems();

    // ---------- Equipment ----------

    public ItemInstance GetEquipped(SlotCategory category, int slotIndex) => equipment[category][slotIndex];

    public IEnumerable<ItemInstance> GetAllEquippedItems()
    {
        foreach (var slots in equipment.Values)
        {
            foreach (var item in slots)
            {
                if (item != null)
                    yield return item;
            }
        }
    }

    public bool TryEquipItem(ItemInstance instance)
    {
        var category = instance.Data.EquipCategory;
        if (category == null)
        {
            Debug.Log($"{instance.Data.itemName} не можна екіпірувати.");
            return false;
        }

        var slots = equipment[category.Value];
        int slotIndex = instance.Data.PreferredSlotIndex;

        if (category.Value == SlotCategory.Artifact || category.Value == SlotCategory.Medicine)
        {
            int freeIndex = Array.IndexOf(slots, null);
            slotIndex = freeIndex != -1 ? freeIndex : 0;
        }

        slots[slotIndex] = instance;

        Debug.Log(
            $"Екіпіровано {instance.Data.itemName} у {category.Value}[{slotIndex}]"
        );

        OnEquipmentChanged?.Invoke();

        if (PlayerStats.Instance == null)
        {
            Debug.LogError("PLAYER STATS INSTANCE == NULL!");
        }
        else
        {
            Debug.Log("PLAYER STATS FOUND → REBUILDING...");
            PlayerStats.Instance.RebuildFromEquipment();
        }

        return true;
    }

    /// <summary>Швидке екіп напряму з ItemData (для тестів, без проходження через сітку).</summary>
    public bool TryEquipItem(ItemData template) => TryEquipItem(new ItemInstance(template));

    public void UnequipSlot(SlotCategory category, int slotIndex)
    {
        equipment[category][slotIndex] = null;
        OnEquipmentChanged?.Invoke();
        PlayerStats.Instance?.RebuildFromEquipment();
    }

    public void DiscardItem(ItemInstance instance)
    {
        Debug.Log($"Викинуто: {instance.Data.itemName}");
        RemoveItem(instance);
    }

    public void UseItem(ItemInstance instance)
    {
        if (instance.Data is not ConsumableData consumable)
            return;

        Debug.Log($"Використано {instance.Data.itemName}: ефект {consumable.effect} ({consumable.effectValue})");
        // TODO: PlayerStats.ApplyEffect(consumable.effect, consumable.effectValue) — коли з'явиться система здоров'я

        instance.ConsumeUnits(1);
        if (instance.StackCount <= 0)
            RemoveItem(instance);
        else
            OnInventoryChanged?.Invoke();
    }

    /// <summary>Сортує: зброя  броня  набої інше.</summary>
    public void SortInventory()
    {
        var items = Grid.GetAllItems().ToList();

        items.Sort((a, b) =>
        {
            int priorityA = GetSortPriority(a.Data);
            int priorityB = GetSortPriority(b.Data);
            if (priorityA != priorityB)
                return priorityA.CompareTo(priorityB);

            int areaA = a.Data.gridWidth * a.Data.gridHeight;
            int areaB = b.Data.gridWidth * b.Data.gridHeight;
            return areaB.CompareTo(areaA);
        });

        Grid.Clear();
        foreach (var item in items)
            Grid.TryAddItem(item);

        Debug.Log("Інвентар відсортовано.");
        OnInventoryChanged?.Invoke();
    }

    private int GetSortPriority(ItemData data)
    {
        return data.itemType switch
        {
            ItemType.Weapon => 0,
            ItemType.Armor => 1,
            ItemType.Ammo => 2,
            _ => 3
        };
    }

    public bool SaveItemToPreset(ItemInstance sourceInstance)
    {
        if (sourceInstance == null)
            return false;

        if (!CanSaveItemToBackpackPreset(sourceInstance))
        {
            Debug.Log(
                $"{sourceInstance.Data.itemName} не можна додати до Backpack Preset."
            );

            OnInventoryMessage?.Invoke(
                $"{sourceInstance.Data.itemName} не можна додати до Backpack Preset."
            );

            return false;
        }

        var preset = BackpackPresets.CurrentPreset;

        // Створюємо віртуальну копію
        var virtualCopy = new ItemInstance(
            sourceInstance.Data,
            sourceInstance.StackCount
        );

        bool success = preset.Grid.TryAddItem(virtualCopy);

        if (success)
        {
            preset.IsDirty = true;

            Debug.Log(
                $"Додано в Backpack Preset '{preset.PresetName}': " +
                sourceInstance.Data.itemName
            );

            OnPresetChanged?.Invoke();
        }
        else
        {
            Debug.Log(
                $"Немає місця в Backpack Preset для: " +
                sourceInstance.Data.itemName
            );

            OnInventoryMessage?.Invoke(
                $"Немає місця в пресеті для: {sourceInstance.Data.itemName}"
            );
        }

        return success;
    }

    private bool CanSaveItemToBackpackPreset(ItemInstance instance)
    {
        if (instance == null || instance.Data == null)
            return false;

        ItemData data = instance.Data;

        // Боєприпаси
        if (data.itemType == ItemType.Ammo)
            return true;

        // Гранати та їжа/напої
        if (data is ConsumableData consumable)
        {
            return consumable.effect == ConsumableEffect.Grenade
                || consumable.effect == ConsumableEffect.RestoreStamina;
        }

        return false;
    }

/// <summary>Прибирає віртуальну копію предмета з поточного пресета (на реальний інвентар не впливає).</summary>
    public void RemoveItemFromPreset(ItemInstance presetInstance)
    {
        var preset = BackpackPresets.CurrentPreset;
        preset.Grid.RemoveItem(presetInstance);
        preset.IsDirty = true;
        Debug.Log($"Прибрано з пресета: {presetInstance.Data.itemName}");
        OnPresetChanged?.Invoke();
    }

    public void SelectPreset(int index)
    {
        BackpackPresets.SelectPreset(index);
        OnCurrentPresetChanged?.Invoke();
    }

    public void RenameCurrentPreset(string newName)
    {
        int index = BackpackPresets.Presets.IndexOf(BackpackPresets.CurrentPreset);
        BackpackPresets.RenamePreset(index, newName);
        OnCurrentPresetChanged?.Invoke();
    }

        // ---------- Equipment Presets ----------

    public bool SaveItemToEquipmentPreset(ItemInstance sourceInstance)
    {
        if (sourceInstance == null)
            return false;

        var category = sourceInstance.Data.EquipCategory;

        Debug.Log(
            $"EQUIPMENT PRESET DEBUG | " +
            $"Item: {sourceInstance.Data.itemName} | " +
            $"Type: {sourceInstance.Data.itemType} | " +
            $"Category: {category}"
        );

        // Предмет взагалі не є екіпіровкою
        if (category == null)
        {
            Debug.Log(
                $"{sourceInstance.Data.itemName} не є предметом екіпіровки."
            );

            OnInventoryMessage?.Invoke(
                $"{sourceInstance.Data.itemName} не можна додати до Equipment Preset."
            );

            return false;
        }

        // Backpack/Equipment preset працює тільки з цими категоріями
        if (Array.IndexOf(
                EquipmentPreset.SupportedCategories,
                category.Value) == -1)
        {
            return false;
        }

        var preset = EquipmentPresets.CurrentPreset;

        // Один конкретний ItemInstance не можна додати двічі
        if (preset.Contains(sourceInstance))
        {
            OnInventoryMessage?.Invoke(
                $"{sourceInstance.Data.itemName} вже є в Equipment Preset."
            );

            return false;
        }

        int slotIndex;

        // -------------------------------------------------
        // Weapon
        // -------------------------------------------------

        if (category.Value == SlotCategory.Weapon)
        {
            slotIndex = sourceInstance.Data.PreferredSlotIndex;

            if (slotIndex < 0 ||
                slotIndex >= preset.SlotCount(SlotCategory.Weapon))
            {
                return false;
            }

            if (preset.GetSlot(SlotCategory.Weapon, slotIndex) != null)
            {
                OnInventoryMessage?.Invoke(
                    "Цей слот зброї в Equipment Preset вже зайнятий."
                );

                return false;
            }
        }

        // -------------------------------------------------
        // Armor
        // -------------------------------------------------

        else if (category.Value == SlotCategory.Armor)
        {
            slotIndex = 0;

            if (preset.GetSlot(SlotCategory.Armor, slotIndex) != null)
            {
                OnInventoryMessage?.Invoke(
                    "Слот броні в Equipment Preset вже зайнятий."
                );

                return false;
            }
        }

        // -------------------------------------------------
        // Detector
        // -------------------------------------------------

        else if (category.Value == SlotCategory.Detector)
        {
            slotIndex = 0;

            if (preset.GetSlot(SlotCategory.Detector, slotIndex) != null)
            {
                OnInventoryMessage?.Invoke(
                    "Слот детектора в Equipment Preset вже зайнятий."
                );

                return false;
            }
        }

        // -------------------------------------------------
        // Artifact
        // -------------------------------------------------

        else if (category.Value == SlotCategory.Artifact)
        {
            slotIndex = -1;

            for (int i = 0;
                i < preset.SlotCount(SlotCategory.Artifact);
                i++)
            {
                if (preset.GetSlot(SlotCategory.Artifact, i) == null)
                {
                    slotIndex = i;
                    break;
                }
            }

            if (slotIndex == -1)
            {
                OnInventoryMessage?.Invoke(
                    "У Equipment Preset немає вільного слота для артефакту."
                );

                return false;
            }
        }

        // -------------------------------------------------
        // Medicine
        // -------------------------------------------------

        else if (category.Value == SlotCategory.Medicine)
        {
            slotIndex = -1;

            // У нас 2 медичних слоти
            for (int i = 0;
                i < preset.SlotCount(SlotCategory.Medicine);
                i++)
            {
                if (preset.GetSlot(SlotCategory.Medicine, i) == null)
                {
                    slotIndex = i;
                    break;
                }
            }

            if (slotIndex == -1)
            {
                OnInventoryMessage?.Invoke(
                    "У Equipment Preset немає вільного слота для медицини."
                );

                return false;
            }
        }

        else
        {
            return false;
        }

        // Додаємо саме конкретний ItemInstance
       var presetItem = new PresetItem(sourceInstance);

        preset.SetSlot(
            category.Value,
            slotIndex,
            presetItem
        );

        preset.IsDirty = true;

        Debug.Log(
            $"Додано '{sourceInstance.Data.itemName}' " +
            $"до Equipment Preset '{preset.PresetName}' " +
            $"у {category.Value}[{slotIndex}]"
        );

        OnEquipmentPresetChanged?.Invoke();

        return true;
    }

    public bool RemoveItemFromEquipmentPreset(
    SlotCategory category,
    int slotIndex)
    {
    var preset = EquipmentPresets.CurrentPreset;

    if (preset.GetSlot(category, slotIndex) == null)
        return false;

    var item = preset.GetSlot(category, slotIndex);

    preset.SetSlot(category, slotIndex, null);
    preset.IsDirty = true;

    Debug.Log(
        $"Прибрано '{item.Data.itemName}' " +
        $"з Equipment Preset '{preset.PresetName}'"
    );

    OnEquipmentPresetChanged?.Invoke();

    return true;
    }


    public void SelectEquipmentPreset(int index)
    {
    EquipmentPresets.SelectPreset(index);

    OnCurrentEquipmentPresetChanged?.Invoke();
    }

    public void RenameCurrentEquipmentPreset(string newName)
    {
        int index =
            EquipmentPresets.Presets.IndexOf(
                EquipmentPresets.CurrentPreset
            );

        EquipmentPresets.RenamePreset(index, newName);

        OnCurrentEquipmentPresetChanged?.Invoke();
    }
}
