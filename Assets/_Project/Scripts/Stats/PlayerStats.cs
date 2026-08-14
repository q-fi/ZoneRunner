using System.Collections.Generic;
using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    public static PlayerStats Instance { get; private set; }

    [Header("Base Stats")]
    [SerializeField] private float baseHealth = 20f;
    [SerializeField] private float baseStamina = 15f;
    [SerializeField] private float baseEndurance = 10f;
    [SerializeField] private float baseLuck = 1f;

    private readonly List<StatModifier> modifiers = new();

    public event System.Action OnStatsChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public float GetBaseStat(PlayerStatType stat)
    {
        return stat switch
        {
            PlayerStatType.Health => baseHealth,
            PlayerStatType.Stamina => baseStamina,
            PlayerStatType.Endurance => baseEndurance,
            PlayerStatType.Luck => baseLuck,
            PlayerStatType.Defense => 0f,
            _ => 0f
        };
    }

    public float GetFinalStat(PlayerStatType stat)
    {
        float value = GetBaseStat(stat);

        foreach (var modifier in modifiers)
        {
            if (modifier != null && modifier.stat == stat)
                value += modifier.value;
        }

        return value;
    }

    public void RebuildFromEquipment()
    {
        modifiers.Clear();

        Debug.Log("========== PLAYER STATS REBUILD ==========");

        if (InventoryManager.Instance == null)
        {
            Debug.LogError("PlayerStats: InventoryManager.Instance == NULL");
            OnStatsChanged?.Invoke();
            return;
        }

        foreach (var item in InventoryManager.Instance.GetAllEquippedItems())
        {
            if (item == null || item.Data == null)
                continue;

            Debug.Log($"EQUIPPED: {item.Data.itemName}");

            if (item.Data.StatModifiers != null)
            {
                foreach (var modifier in item.Data.StatModifiers)
                {
                    if (modifier == null)
                        continue;

                    modifiers.Add(modifier);

                    Debug.Log(
                        $"  MODIFIER: {modifier.stat} " +
                        $"{modifier.value:+0.##;-0.##;0}"
                    );
                }
            }

            if (item.Data is ArmorData armor && armor.defense != 0)
            {
                modifiers.Add(
                    new StatModifier(
                        PlayerStatType.Defense,
                        armor.defense
                    )
                );

                Debug.Log($"  ARMOR DEFENSE: +{armor.defense}");
            }
        }

        Debug.Log("---------- FINAL STATS ----------");

        foreach (PlayerStatType stat in System.Enum.GetValues(typeof(PlayerStatType)))
        {
            Debug.Log($"FINAL {stat}: {GetFinalStat(stat)}");
        }

        Debug.Log("==========================================");

        OnStatsChanged?.Invoke();
    }

    public void AddModifier(StatModifier modifier)
    {
        if (modifier == null)
            return;

        modifiers.Add(modifier);
        OnStatsChanged?.Invoke();
    }

    public void RemoveModifier(StatModifier modifier)
    {
        if (modifier == null)
            return;

        modifiers.Remove(modifier);
        OnStatsChanged?.Invoke();
    }

    public void ClearModifiers()
    {
        modifiers.Clear();
        OnStatsChanged?.Invoke();
    }

    public bool TryUpgradeBaseStat(PlayerStatType stat)
    {
        switch (stat)
        {
            case PlayerStatType.Health:
                baseHealth += 1f;
                break;

            case PlayerStatType.Stamina:
                baseStamina += 1f;
                break;

            case PlayerStatType.Endurance:
                baseEndurance += 1f;
                break;

            case PlayerStatType.Luck:
                baseLuck += 1f;
                break;

            default:
                return false;
        }

        OnStatsChanged?.Invoke();
        return true;
    }
}