using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class EnemyLootEntry
{
    public ItemData item;

    [Range(0f, 100f)]
    public float dropChancePercent = 100f;

    [Min(1)]
    public int minCount = 1;

    [Min(1)]
    public int maxCount = 1;
}

[CreateAssetMenu(
    fileName = "EnemyData",
    menuName = "ZoneRunner/Battle/Enemy Data"
)]
public class EnemyData : ScriptableObject
{
    [Header("Identity")]
    public string enemyId;
    public string displayName = "ENEMY";
    public Sprite icon;

    [Header("Base Combat Stats")]
    [Min(1f)]
    public float maxHealth = 10f;

    [Min(0f)]
    public float defense;

    [Min(0f)]
    public float baseDamage = 2f;

    [Header("Loot Table")]
    [SerializeField]
    private List<EnemyLootEntry> lootTable = new();

    public IReadOnlyList<EnemyLootEntry> LootTable => lootTable;

    private void OnValidate()
    {
        foreach (var entry in lootTable)
        {
            if (entry == null)
                continue;

            entry.dropChancePercent = Mathf.Clamp(
                entry.dropChancePercent,
                0f,
                100f
            );

            entry.minCount = Mathf.Max(1, entry.minCount);
            entry.maxCount = Mathf.Max(
                entry.minCount,
                entry.maxCount
            );
        }
    }
}
