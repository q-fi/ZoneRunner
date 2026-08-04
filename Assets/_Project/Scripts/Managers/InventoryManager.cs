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

    public InventoryGrid Grid { get; private set; }

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
    public event Action<string> OnInventoryMessage; // для сповіщень типу "Немає місця"

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
    }

    /// <summary>Додає новий фізичний екземпляр предмета (зі стаканням, якщо предмет стакається).</summary>
    public bool AddItem(ItemData template, int count = 1)
    {
        if (template.isStackable)
        {
            var existing = Grid.FindStackableInstance(template);
            if (existing != null)
            {
                int freeSpace = template.maxStackSize - existing.StackCount;
                int toAdd = Mathf.Min(freeSpace, count);
                existing.StackCount += toAdd;
                count -= toAdd;
                OnInventoryChanged?.Invoke();

                if (count <= 0)
                {
                    Debug.Log($"Додано в стак: {template.itemName} (+{toAdd})");
                    return true;
                }
                // стак заповнився, а лишок є — створюємо ще один екземпляр нижче
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
        Debug.Log($"Екіпіровано {instance.Data.itemName} у {category.Value}[{slotIndex}]");
        OnEquipmentChanged?.Invoke();
        return true;
    }

    /// <summary>Швидке екіпірування напряму з ItemData (для тестів, без проходження через сітку).</summary>
    public bool TryEquipItem(ItemData template) => TryEquipItem(new ItemInstance(template));

    public void UnequipSlot(SlotCategory category, int slotIndex)
    {
        equipment[category][slotIndex] = null;
        OnEquipmentChanged?.Invoke();
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

        instance.StackCount--;
        if (instance.StackCount <= 0)
            RemoveItem(instance);
        else
            OnInventoryChanged?.Invoke();
    }

    /// <summary>Сортує інвентар: зброя → броня → набої → все інше.</summary>
    public void SortInventory()
    {
        var items = Grid.GetAllItems().ToList();

        items.Sort((a, b) =>
        {
            int priorityA = GetSortPriority(a.Data);
            int priorityB = GetSortPriority(b.Data);
            if (priorityA != priorityB)
                return priorityA.CompareTo(priorityB);

            // всередині однакового пріоритету — більші предмети спочатку (щільніша упаковка)
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
}