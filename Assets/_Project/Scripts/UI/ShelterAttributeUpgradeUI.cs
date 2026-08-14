using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShelterAttributeUpgradeUI : MonoBehaviour
{
    [Header("Value Texts")]
    [SerializeField] private TMP_Text healthValueText;
    [SerializeField] private TMP_Text staminaValueText;
    [SerializeField] private TMP_Text enduranceValueText;
    [SerializeField] private TMP_Text luckValueText;

    [Header("Upgrade Buttons")]
    [SerializeField] private Button healthUpgradeButton;
    [SerializeField] private Button staminaUpgradeButton;
    [SerializeField] private Button enduranceUpgradeButton;
    [SerializeField] private Button luckUpgradeButton;

    private PlayerStats playerStats;
    private PlayerProgression progression;
    private bool isSubscribed;

    private void Awake()
    {
        healthUpgradeButton.onClick.AddListener(
            () => TryUpgrade(PlayerStatType.Health)
        );

        staminaUpgradeButton.onClick.AddListener(
            () => TryUpgrade(PlayerStatType.Stamina)
        );

        enduranceUpgradeButton.onClick.AddListener(
            () => TryUpgrade(PlayerStatType.Endurance)
        );

        luckUpgradeButton.onClick.AddListener(
            () => TryUpgrade(PlayerStatType.Luck)
        );

        SetUpgradeButtonsInteractable(false);
    }

    private void OnEnable()
    {
        SubscribeAndRefresh();
    }

    private void Start()
    {
        SubscribeAndRefresh();
    }

    private void OnDisable()
    {
        if (!isSubscribed)
            return;

        playerStats.OnStatsChanged -= RefreshUI;
        progression.OnProgressChanged -= RefreshUI;
        isSubscribed = false;
    }

    private void SubscribeAndRefresh()
    {
        playerStats ??= PlayerStats.Instance;
        progression ??= PlayerProgression.Instance;

        if (playerStats == null || progression == null)
            return;

        if (!isSubscribed)
        {
            playerStats.OnStatsChanged += RefreshUI;
            progression.OnProgressChanged += RefreshUI;
            isSubscribed = true;
        }

        RefreshUI();
    }

    private void TryUpgrade(PlayerStatType stat)
    {
        if (playerStats == null || progression == null)
            return;

        if (progression.AvailableSkillPoints <= 0)
            return;

        if (!playerStats.TryUpgradeBaseStat(stat))
            return;

        progression.TrySpendSkillPoint();
    }

    private void RefreshUI()
    {
        healthValueText.text =
            FormatValue(playerStats.GetBaseStat(PlayerStatType.Health));

        staminaValueText.text =
            FormatValue(playerStats.GetBaseStat(PlayerStatType.Stamina));

        enduranceValueText.text =
            FormatValue(playerStats.GetBaseStat(PlayerStatType.Endurance));

        luckValueText.text =
            FormatValue(playerStats.GetBaseStat(PlayerStatType.Luck));

        SetUpgradeButtonsInteractable(
            progression.AvailableSkillPoints > 0
        );
    }

    private void SetUpgradeButtonsInteractable(bool value)
    {
        healthUpgradeButton.interactable = value;
        staminaUpgradeButton.interactable = value;
        enduranceUpgradeButton.interactable = value;
        luckUpgradeButton.interactable = value;
    }

    private string FormatValue(float value)
    {
        return value % 1f == 0f
            ? value.ToString("0")
            : value.ToString("0.##");
    }
}