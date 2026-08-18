using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class BattleDeckCardEntry
{
    public BattleCardData card;

    [Range(1, 2)]
    public int copies = 1;
}

[CreateAssetMenu(
    fileName = "BattleDeckData",
    menuName = "ZoneRunner/Battle/Deck Data"
)]
public class BattleDeckData : ScriptableObject
{
    public const int TargetDeckSize = 20;
    public const int MaxCopiesPerCard = 2;

    [Header("Identity")]
    public string deckId;
    public string displayName = "DECK";

    [Header("Cards")]
    [SerializeField] private List<BattleDeckCardEntry> cards = new();

    public IReadOnlyList<BattleDeckCardEntry> Cards => cards;

    public int TotalCardCount
    {
        get
        {
            int total = 0;

            foreach (var entry in cards)
            {
                if (entry?.card != null)
                    total += Mathf.Clamp(
                        entry.copies,
                        1,
                        MaxCopiesPerCard
                    );
            }

            return total;
        }
    }

    public bool HasTargetDeckSize => TotalCardCount == TargetDeckSize;

    private void OnValidate()
    {
        foreach (var entry in cards)
        {
            if (entry == null)
                continue;

            entry.copies = Mathf.Clamp(
                entry.copies,
                1,
                MaxCopiesPerCard
            );
        }
    }
}
