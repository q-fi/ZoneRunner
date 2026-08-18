using System;
using System.Collections.Generic;
using UnityEngine;

public enum BattleCardTargetType
{
    SingleEnemy,
    Self,
    AllEnemies,
    SelectedEnemyAndAdjacent
}

public enum BattleCardEffectType
{
    Damage,
    Heal,
    RestoreStamina
}

[Serializable]
public class BattleCardEffect
{
    public BattleCardEffectType effectType;
    public float value;
}

[CreateAssetMenu(
    fileName = "BattleCardData",
    menuName = "ZoneRunner/Battle/Card Data"
)]
public class BattleCardData : ScriptableObject
{
    [Header("Identity")]
    public string cardId;
    public string displayName = "CARD";
    public Sprite icon;

    [TextArea(2, 4)]
    public string description;

    [Header("Use")]
    [Min(0f)]
    public float staminaCost = 1f;

    public BattleCardTargetType targetType =
        BattleCardTargetType.SingleEnemy;

    [Range(0f, 100f)]
    public float hitChancePercent = 100f;

    [Header("Backpack Cost")]
    public ItemData requiredBackpackItem;

    [Min(0)]
    public int backpackItemCost;

    [Header("Effects")]
    [SerializeField] private List<BattleCardEffect> effects = new();

    public IReadOnlyList<BattleCardEffect> Effects => effects;
}
