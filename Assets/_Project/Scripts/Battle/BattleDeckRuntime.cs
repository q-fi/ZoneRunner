using System.Collections.Generic;
using UnityEngine;

public class BattleDeckRuntime
{
    public IReadOnlyList<BattleCardRuntime> Hand => hand;
    public int DrawPileCount => drawPile.Count;
    public int DiscardPileCount => discardPile.Count;

    private readonly List<BattleCardRuntime> drawPile = new();
    private readonly List<BattleCardRuntime> hand = new();
    private readonly List<BattleCardRuntime> discardPile = new();

    public BattleDeckRuntime(BattleDeckData deckData)
    {
        if (deckData == null)
            return;

        int nextInstanceId = 1;

        foreach (var entry in deckData.Cards)
        {
            if (entry?.card == null)
                continue;

            int copies = Mathf.Clamp(
                entry.copies,
                1,
                BattleDeckData.MaxCopiesPerCard
            );

            for (int i = 0; i < copies; i++)
            {
                drawPile.Add(
                    new BattleCardRuntime(
                        nextInstanceId++,
                        entry.card
                    )
                );
            }
        }

        Shuffle(drawPile);
    }

    public void DrawToHand(int targetHandSize)
    {
        targetHandSize = Mathf.Max(0, targetHandSize);

        while (hand.Count < targetHandSize)
        {
            if (drawPile.Count == 0)
            {
                if (discardPile.Count == 0)
                    break;

                drawPile.AddRange(discardPile);
                discardPile.Clear();
                Shuffle(drawPile);
            }

            int lastIndex = drawPile.Count - 1;
            hand.Add(drawPile[lastIndex]);
            drawPile.RemoveAt(lastIndex);
        }
    }

    public bool ContainsInHand(BattleCardRuntime card)
    {
        return card != null && hand.Contains(card);
    }

    public bool MoveFromHandToDiscard(BattleCardRuntime card)
    {
        if (card == null || !hand.Remove(card))
            return false;

        discardPile.Add(card);
        return true;
    }

    public void ReturnFromDiscardToHand(BattleCardRuntime card)
    {
        if (card == null || !discardPile.Remove(card))
            return;

        hand.Add(card);
    }

    private static void Shuffle(List<BattleCardRuntime> cards)
    {
        for (int i = cards.Count - 1; i > 0; i--)
        {
            int randomIndex = UnityEngine.Random.Range(0, i + 1);
            (cards[i], cards[randomIndex]) =
                (cards[randomIndex], cards[i]);
        }
    }
}
