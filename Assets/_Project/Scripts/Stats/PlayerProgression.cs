using System;
using UnityEngine;

public class PlayerProgression : MonoBehaviour
{
    public static PlayerProgression Instance { get; private set; }

    [Header("Current Progress")]
    [SerializeField, Min(1)] private int currentLevel = 1;
    [SerializeField, Min(0)] private int currentXp = 0;
    [SerializeField, Min(0)] private int availableSkillPoints = 0;
    [SerializeField, Min(0)] private int totalSkillPointsEarned = 0;

    [Header("XP Curve")]
    [SerializeField, Min(1)] private int baseXpRequired = 100;
    [SerializeField, Min(1f)] private float xpGrowthMultiplier = 1.15f;

    public event Action OnProgressChanged;

    public int CurrentLevel => currentLevel;
    public int CurrentXp => currentXp;
    public int AvailableSkillPoints => availableSkillPoints;
    public int TotalSkillPointsEarned => totalSkillPointsEarned;

    public int XpRequiredForNextLevel =>
        Mathf.RoundToInt(
            baseXpRequired *
            Mathf.Pow(xpGrowthMultiplier, currentLevel - 1)
        );

    public float XpProgress =>
        currentXp / (float)XpRequiredForNextLevel;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void AddExperience(int amount)
    {
        if (amount <= 0)
            return;

        currentXp += amount;

        while (currentXp >= XpRequiredForNextLevel)
        {
            currentXp -= XpRequiredForNextLevel;
            LevelUp();
        }

        OnProgressChanged?.Invoke();
    }

    public bool TrySpendSkillPoint()
    {
        if (availableSkillPoints <= 0)
            return false;

        availableSkillPoints--;
        OnProgressChanged?.Invoke();

        return true;
    }

    private void LevelUp()
    {
        currentLevel++;

        AddSkillPoints(1);

        if (currentLevel % 5 == 0)
        {
            int milestoneBonus = UnityEngine.Random.Range(2, 4);
            AddSkillPoints(milestoneBonus);
        }

        Debug.Log(
            $"LEVEL UP: Level {currentLevel}. " +
            $"SP: {availableSkillPoints}/{totalSkillPointsEarned}"
        );
    }

    private void AddSkillPoints(int amount)
    {
        availableSkillPoints += amount;
        totalSkillPointsEarned += amount;
    }

    [ContextMenu("Debug/Add 25 XP")]
    private void DebugAdd25Xp()
    {
        AddExperience(25);
    }

    [ContextMenu("Debug/Add 500 XP")]
    private void DebugAdd500Xp()
    {
        AddExperience(500);
    }
}