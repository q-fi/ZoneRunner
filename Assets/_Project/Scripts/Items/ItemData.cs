using UnityEngine;
using System.Collections.Generic;

public abstract class ItemData : ScriptableObject
{
    [Header("Basic Info")]
    public string itemId;
    public string itemName;
    [TextArea] public string description;
    public Sprite icon;
    public ItemType itemType;

    [Header("Grid Size (in cells)")]
    [Min(1)] public int gridWidth = 1;
    [Min(1)] public int gridHeight = 1;

    [Header("Stacking")]
    public bool isStackable = false;
    public int maxStackSize = 1;

    [Header("Stat Modifiers")]
    [SerializeField] private List<StatModifier> statModifiers = new();

    public IReadOnlyList<StatModifier> StatModifiers => statModifiers;

    // До якої категорії слотів підходить предмет. null = взагалі не екіпірується
    public virtual SlotCategory? EquipCategory => null;

    // У який конкретно слот всередині категорії йде предмет (0, якщо не важливо)
    public virtual int PreferredSlotIndex => 0;
}