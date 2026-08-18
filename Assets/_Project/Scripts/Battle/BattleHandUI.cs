using System.Collections.Generic;
using UnityEngine;

public class BattleHandUI : MonoBehaviour
{
    [SerializeField] private BattleController battleController;
    [SerializeField] private Transform handContainer;
    [SerializeField] private BattleCardButtonUI cardButtonPrefab;

    private readonly List<BattleCardButtonUI> spawnedButtons = new();
    private readonly List<BattleCardRuntime> displayedHand = new();

    private void Awake()
    {
        if (battleController == null)
            battleController = GetComponent<BattleController>();
    }

    private void OnEnable()
    {
        if (battleController != null)
        {
            battleController.OnBattleStateChanged -= Refresh;
            battleController.OnBattleStateChanged += Refresh;
        }

        Refresh();
    }

    private void OnDisable()
    {
        if (battleController != null)
            battleController.OnBattleStateChanged -= Refresh;

        ClearButtons();
    }

    private void Refresh()
    {
        if (battleController == null ||
            handContainer == null ||
            cardButtonPrefab == null)
        {
            return;
        }

        var hand = battleController.Hand;

        if (MatchesDisplayedHand(hand))
            return;

        ClearButtons();

        if (hand == null)
            return;

        foreach (var runtimeCard in hand)
        {
            if (runtimeCard?.Data == null)
                continue;

            var button = Instantiate(
                cardButtonPrefab,
                handContainer
            );

            button.Setup(battleController, runtimeCard);
            spawnedButtons.Add(button);
            displayedHand.Add(runtimeCard);
        }
    }

    private bool MatchesDisplayedHand(
        IReadOnlyList<BattleCardRuntime> hand
    )
    {
        if (hand == null)
            return displayedHand.Count == 0;

        if (hand.Count != displayedHand.Count)
            return false;

        for (int i = 0; i < hand.Count; i++)
        {
            if (hand[i] != displayedHand[i])
                return false;
        }

        return true;
    }

    private void ClearButtons()
    {
        foreach (var button in spawnedButtons)
        {
            if (button != null)
                Destroy(button.gameObject);
        }

        spawnedButtons.Clear();
        displayedHand.Clear();
    }
}
