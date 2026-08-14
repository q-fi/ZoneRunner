using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShelterLevelUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text skillPointsText;
    [SerializeField] private Slider xpProgressBar;
    [SerializeField] private TMP_Text xpText;

    private PlayerProgression progression;
    private bool isSubscribed;

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
        if (progression != null && isSubscribed)
        {
            progression.OnProgressChanged -= RefreshUI;
            isSubscribed = false;
        }
    }

    private void SubscribeAndRefresh()
    {
        progression ??= PlayerProgression.Instance;

        if (progression == null)
            return;

        if (!isSubscribed)
        {
            progression.OnProgressChanged += RefreshUI;
            isSubscribed = true;
        }

        RefreshUI();
    }

    private void RefreshUI()
    {
        levelText.text = $"Level {progression.CurrentLevel}";

        skillPointsText.text =
            $"SP {progression.AvailableSkillPoints}/" +
            $"{progression.TotalSkillPointsEarned}";

        xpProgressBar.value = progression.XpProgress;

        xpText.text =
            $"{progression.CurrentXp}/" +
            $"{progression.XpRequiredForNextLevel} XP";
    }
}
