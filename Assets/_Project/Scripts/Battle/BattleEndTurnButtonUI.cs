using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class BattleEndTurnButtonUI : MonoBehaviour
{
    [SerializeField] private BattleController battleController;

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();

        if (battleController == null)
            battleController = GetComponentInParent<BattleController>();
    }

    private void OnEnable()
    {
        button.onClick.RemoveListener(EndTurn);
        button.onClick.AddListener(EndTurn);

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
            button.onClick.RemoveListener(EndTurn);

        if (battleController != null)
            battleController.OnBattleStateChanged -= Refresh;
    }

    private void EndTurn()
    {
        if (battleController != null)
            battleController.EndPlayerTurn();
    }

    private void Refresh()
    {
        if (button == null)
            return;

        button.interactable =
            battleController != null &&
            battleController.CurrentPhase == BattlePhase.PlayerTurn;
    }
}
