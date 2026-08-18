using System;
using System.Collections.Generic;
using UnityEngine;

public enum EncounterDifficulty
{
    Easy,
    Medium,
    Hard
}

public enum BattleFormationSlot
{
    FrontLeft,
    FrontCenter,
    FrontRight,
    BackLeft,
    BackRight
}

[Serializable]
public class EncounterEnemySlot
{
    public EnemyData enemy;
    public BattleFormationSlot formationSlot;
}

[CreateAssetMenu(
    fileName = "EncounterData",
    menuName = "ZoneRunner/Battle/Encounter Data"
)]
public class EncounterData : ScriptableObject
{
    public const int MaxEnemyCount = 5;

    [Header("Identity")]
    public string encounterId;
    public string displayName = "ENCOUNTER";

    [Header("Difficulty")]
    public EncounterDifficulty difficulty = EncounterDifficulty.Easy;

    [Header("Enemies (maximum 5)")]
    [SerializeField] private List<EncounterEnemySlot> enemies = new();

    public IReadOnlyList<EncounterEnemySlot> Enemies => enemies;

    private void OnValidate()
    {
        if (enemies.Count > MaxEnemyCount)
        {
            enemies.RemoveRange(
                MaxEnemyCount,
                enemies.Count - MaxEnemyCount
            );
        }
    }
}
