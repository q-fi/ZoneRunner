using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class BattleEnemyButtonUI : MonoBehaviour
{
    [SerializeField] private BattleController battleController;
    [SerializeField] private BattleFormationSlot formationSlot;
    [SerializeField] private TMP_Text label;

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();

        if (battleController == null)
            battleController = GetComponentInParent<BattleController>();

        if (label == null)
            label = GetComponentInChildren<TMP_Text>();
    }

    private void OnEnable()
    {
        button.onClick.RemoveListener(SelectEnemy);
        button.onClick.AddListener(SelectEnemy);

        if (battleController != null)
        {
            battleController.OnBattleStateChanged -= Refresh;
            battleController.OnBattleStateChanged += Refresh;
        }

        Refresh();
    }

    private void OnDisable()
    {
        if (button != null)
            button.onClick.RemoveListener(SelectEnemy);

        if (battleController != null)
            battleController.OnBattleStateChanged -= Refresh;
    }

    private void SelectEnemy()
    {
        int enemyIndex = FindEnemyIndex();

        if (enemyIndex >= 0)
            battleController.TryPlaySelectedCard(enemyIndex);
    }

    private void Refresh()
    {
        int enemyIndex = FindEnemyIndex();
        BattleEnemyState enemy = enemyIndex >= 0
            ? battleController.Enemies[enemyIndex]
            : null;

        if (label != null)
        {
            label.text = enemy != null
                ? $"{enemy.Data.displayName}\n" +
                  $"{enemy.CurrentHealth:0.#}/{enemy.MaxHealth:0.#} HP"
                : $"{formationSlot}\nEMPTY";
        }

        if (button == null)
            return;

        button.interactable =
            enemy != null &&
            enemy.IsAlive &&
            battleController != null &&
            battleController.CurrentPhase == BattlePhase.PlayerTurn &&
            battleController.SelectedCard != null;
    }

    private int FindEnemyIndex()
    {
        if (battleController == null)
            return -1;

        for (int i = 0; i < battleController.Enemies.Count; i++)
        {
            if (battleController.Enemies[i].FormationSlot == formationSlot)
                return i;
        }

        return -1;
    }
}
