using System.Collections.Generic;
using UnityEngine;

public class BattlePlayerState
{
    public float MaxHealth { get; }
    public float CurrentHealth { get; private set; }
    public float MaxStamina { get; }
    public float CurrentStamina { get; private set; }
    public float Defense { get; }

    public bool IsAlive => CurrentHealth > 0f;

    public BattlePlayerState(
        PlayerStats stats,
        IReadOnlyList<ItemInstance> equipmentItems
    )
    {
        MaxHealth = Mathf.Max(
            1f,
            CalculateStat(
                stats,
                equipmentItems,
                PlayerStatType.Health
            )
        );

        MaxStamina = Mathf.Max(
            0f,
            CalculateStat(
                stats,
                equipmentItems,
                PlayerStatType.Stamina
            )
        );

        Defense = Mathf.Max(
            0f,
            CalculateStat(
                stats,
                equipmentItems,
                PlayerStatType.Defense
            )
        );

        CurrentHealth = MaxHealth;
        CurrentStamina = MaxStamina;
    }

    public bool TrySpendStamina(float amount)
    {
        amount = Mathf.Max(0f, amount);

        if (CurrentStamina < amount)
            return false;

        CurrentStamina -= amount;
        return true;
    }

    public float TakeDamage(float rawDamage)
    {
        float damageAfterDefense = Mathf.Max(0f, rawDamage - Defense);
        float damage = Mathf.Min(CurrentHealth, damageAfterDefense);
        CurrentHealth -= damage;
        return damage;
    }

    private static float CalculateStat(
        PlayerStats stats,
        IReadOnlyList<ItemInstance> equipmentItems,
        PlayerStatType statType
    )
    {
        float value = stats.GetBaseStat(statType);

        if (equipmentItems == null)
            return value;

        foreach (var item in equipmentItems)
        {
            if (item?.Data == null)
                continue;

            if (item.Data.StatModifiers != null)
            {
                foreach (var modifier in item.Data.StatModifiers)
                {
                    if (modifier != null && modifier.stat == statType)
                        value += modifier.value;
                }
            }

            if (statType == PlayerStatType.Defense &&
                item.Data is ArmorData armor)
            {
                value += armor.defense;
            }
        }

        return value;
    }
}

public class BattleEnemyState
{
    public EnemyData Data { get; }
    public BattleFormationSlot FormationSlot { get; }
    public float MaxHealth { get; }
    public float CurrentHealth { get; private set; }
    public float Defense { get; }
    public float BaseDamage { get; }

    public bool IsAlive => CurrentHealth > 0f;

    public BattleEnemyState(
        EnemyData data,
        BattleFormationSlot formationSlot
    )
    {
        Data = data;
        FormationSlot = formationSlot;
        MaxHealth = Mathf.Max(1f, data.maxHealth);
        CurrentHealth = MaxHealth;
        Defense = Mathf.Max(0f, data.defense);
        BaseDamage = Mathf.Max(0f, data.baseDamage);
    }

    public float TakeDamage(float rawDamage)
    {
        float damageAfterDefense = Mathf.Max(0f, rawDamage - Defense);
        float damage = Mathf.Min(CurrentHealth, damageAfterDefense);
        CurrentHealth -= damage;
        return damage;
    }
}
