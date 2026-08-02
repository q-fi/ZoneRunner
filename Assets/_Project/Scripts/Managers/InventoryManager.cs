using System;
using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    [Header("Grid Size")]
    [SerializeField] private int gridWidth = 6;
    [SerializeField] private int gridHeight = 8;

    public InventoryGrid Grid { get; private set; }

    [Header("Equipped Items")]
    public WeaponData EquippedPrimaryWeapon { get; private set; }
    public WeaponData EquippedSecondaryWeapon { get; private set; }
    public ArmorData EquippedArmor { get; private set; }
    public ItemData EquippedDetector { get; private set; }

    public event Action OnInventoryChanged;
    public event Action OnEquipmentChanged;

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

    public bool AddItem(ItemData item)
    {
        bool success = Grid.TryAddItem(item);
        if (success)
        {
            Debug.Log($"Додано в інвентар: {item.itemName}");
            OnInventoryChanged?.Invoke();
        }
        else
        {
            Debug.Log($"Немає місця для: {item.itemName}");
        }
        return success;
    }

    public void RemoveItem(ItemData item)
    {
        Grid.RemoveItem(item);
        OnInventoryChanged?.Invoke();
    }

    public IEnumerable<ItemData> GetAllItems() => Grid.GetAllItems();

    // ---------- Equipment ----------

    public void EquipWeapon(WeaponData weapon, bool isPrimary = true)
    {
        if (isPrimary)
            EquippedPrimaryWeapon = weapon;
        else
            EquippedSecondaryWeapon = weapon;

        OnEquipmentChanged?.Invoke();
    }

    public void EquipArmor(ArmorData armor)
    {
        EquippedArmor = armor;
        OnEquipmentChanged?.Invoke();
    }

    public void EquipDetector(ItemData detector)
    {
        EquippedDetector = detector;
        OnEquipmentChanged?.Invoke();
    }
}