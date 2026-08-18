using System.Text;
using TMPro;
using UnityEngine;

public class BattleStatusUI : MonoBehaviour
{
    [SerializeField] private BattleController battleController;
    [SerializeField] private TMP_Text statusText;

    private void Awake()
    {
        if (battleController == null)
            battleController = GetComponent<BattleController>();
    }

    private void OnEnable()
    {
        if (battleController == null)
            return;

        battleController.OnBattleStateChanged -= Refresh;
        battleController.OnBattleStateChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        if (battleController != null)
            battleController.OnBattleStateChanged -= Refresh;
    }

    private void Refresh()
    {
        if (statusText == null ||
            battleController == null ||
            battleController.Player == null)
        {
            return;
        }

        var player = battleController.Player;
        var builder = new StringBuilder();

        builder.AppendLine($"Phase: {battleController.CurrentPhase}");
        builder.AppendLine(
            $"Player HP: {player.CurrentHealth:0.#}/{player.MaxHealth:0.#}"
        );
        builder.AppendLine(
            $"Stamina: {player.CurrentStamina:0.#}/{player.MaxStamina:0.#}"
        );
        builder.AppendLine($"Defense: {player.Defense:0.#}");
        builder.AppendLine(
            $"Cards: {battleController.CardsPlayedThisTurn}/" +
            $"{battleController.CardsPerTurnLimit}"
        );
        builder.AppendLine(
            $"Selected Card: " +
            $"{(battleController.SelectedCard?.Data != null ? battleController.SelectedCard.Data.displayName : "—")}"
        );

        if (battleController.Hand != null)
        {
            builder.Append("Hand: ");

            for (int i = 0; i < battleController.Hand.Count; i++)
            {
                if (i > 0)
                    builder.Append(", ");

                builder.Append(
                    battleController.Hand[i].Data.displayName
                );
            }

            builder.AppendLine();
        }

        foreach (var enemy in battleController.Enemies)
        {
            builder.AppendLine(
                $"[{enemy.FormationSlot}] {enemy.Data.displayName}: " +
                $"{enemy.CurrentHealth:0.#}/{enemy.MaxHealth:0.#} HP"
            );
        }

        statusText.text = builder.ToString();
    }
}
