using System.Text;
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
        {
            continueButton.interactable =
                isVictory &&
                battleController.CanContinueAfterVictory;
        }

        if (resultText == null || battleController.Player == null)
            return;

        var player = battleController.Player;
        string title = isVictory ? "VICTORY" : "DEFEAT";
        string lootSummary = string.Empty;

        if (isVictory && battleController.LootSession != null)
            lootSummary = BuildLootSummary(
                battleController.LootSession
            );

        resultText.text =
            $"{title}\n\n" +
            $"Enemies: {battleController.Enemies.Count}\n" +
            $"Player HP: {player.CurrentHealth:0.#}/{player.MaxHealth:0.#}\n" +
            $"Stamina: {player.CurrentStamina:0.#}/{player.MaxStamina:0.#}" +
            lootSummary;
    }

    private static string BuildLootSummary(
        BattleLootResolutionSession loot
    )
    {
        var text = new StringBuilder();
        text.AppendLine();
        text.AppendLine();
        text.AppendLine("LOOT SUMMARY");
        text.Append("Generated: ");
        text.Append(loot.TotalGeneratedCount);
        text.Append(" | Kept: ");
        text.AppendLine(loot.TotalStoredCount.ToString());

        foreach (var reward in loot.Rewards)
        {
            if (reward?.Item == null || reward.StoredCount <= 0)
                continue;

            text.Append("+ ");
            text.Append(reward.Item.itemName);
            text.Append(" x");
            text.AppendLine(reward.StoredCount.ToString());
        }

        text.Append("Not picked up: ");
        text.AppendLine(loot.TotalLeftBehindCount.ToString());

        foreach (var reward in loot.Rewards)
        {
            if (reward?.Item == null || reward.LeftBehindCount <= 0)
                continue;

            text.Append("- ");
            text.Append(reward.Item.itemName);
            text.Append(" x");
            text.AppendLine(reward.LeftBehindCount.ToString());
        }

        text.Append("Discarded/replaced new loot: ");
        text.AppendLine(loot.TotalReplacedOutCount.ToString());

        foreach (var reward in loot.Rewards)
        {
            if (reward?.Item == null || reward.ReplacedOutCount <= 0)
                continue;

            text.Append("- ");
            text.Append(reward.Item.itemName);
            text.Append(" x");
            text.AppendLine(reward.ReplacedOutCount.ToString());
        }

        text.Append("Discarded from previous backpack loot: ");
        text.AppendLine(loot.TotalRemovedBackpackCount.ToString());

        foreach (var removed in loot.RemovedBackpackItems)
        {
            if (removed?.Item == null || removed.Count <= 0)
                continue;

            text.Append("- ");
            text.Append(removed.Item.itemName);
            text.Append(" x");
            text.AppendLine(removed.Count.ToString());
        }

        return text.ToString().TrimEnd();
    }
}
