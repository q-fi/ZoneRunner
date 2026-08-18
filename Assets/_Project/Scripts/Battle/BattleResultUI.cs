using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BattleResultUI : MonoBehaviour
{
    [SerializeField] private BattleController battleController;
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private Button continueButton;

    private void Awake()
    {
        if (battleController == null)
            battleController = GetComponent<BattleController>();
    }

    private void OnEnable()
    {
        if (continueButton != null)
        {
            continueButton.onClick.RemoveListener(Continue);
            continueButton.onClick.AddListener(Continue);
        }

        if (battleController != null)
        {
            battleController.OnBattleStateChanged -= Refresh;
            battleController.OnBattleStateChanged += Refresh;
        }

        Refresh();
    }

    private void OnDisable()
    {
        if (continueButton != null)
            continueButton.onClick.RemoveListener(Continue);

        if (battleController != null)
            battleController.OnBattleStateChanged -= Refresh;

        if (resultPanel != null)
            resultPanel.SetActive(false);
    }

    private void Continue()
    {
        if (battleController != null)
            battleController.ContinueAfterVictory();
    }

    private void Refresh()
    {
        if (resultPanel == null || battleController == null)
            return;

        bool isVictory =
            battleController.CurrentPhase == BattlePhase.Victory;
        bool isDefeat =
            battleController.CurrentPhase == BattlePhase.Defeat;

        resultPanel.SetActive(isVictory || isDefeat);

        if (!isVictory && !isDefeat)
            return;

        if (continueButton != null)
            continueButton.interactable = isVictory;

        if (resultText == null || battleController.Player == null)
            return;

        var player = battleController.Player;
        string title = isVictory ? "VICTORY" : "DEFEAT";

        resultText.text =
            $"{title}\n\n" +
            $"Enemies: {battleController.Enemies.Count}\n" +
            $"Player HP: {player.CurrentHealth:0.#}/{player.MaxHealth:0.#}\n" +
            $"Stamina: {player.CurrentStamina:0.#}/{player.MaxStamina:0.#}";
    }
}
