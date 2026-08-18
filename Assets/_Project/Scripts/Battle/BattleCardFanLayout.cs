using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class BattleCardFanLayout : MonoBehaviour
{
    [Header("Fan Shape")]
    [Min(0f)]
    [SerializeField] private float cardSpacing = 115f;

    [Min(0f)]
    [SerializeField] private float arcHeight = 35f;

    [Range(0f, 45f)]
    [SerializeField] private float maximumRotation = 12f;

    [Header("Position")]
    [SerializeField] private float bottomOffset;

    private RectTransform container;

    private void Awake()
    {
        container = GetComponent<RectTransform>();
    }

    private void OnEnable()
    {
        EnsureContainer();
        LayoutCards();
    }

    private void OnTransformChildrenChanged()
    {
        EnsureContainer();
        LayoutCards();
    }

    private void OnRectTransformDimensionsChange()
    {
        EnsureContainer();
        LayoutCards();
    }

    private void OnValidate()
    {
        EnsureContainer();
        LayoutCards();
    }

    [ContextMenu("Layout Cards")]
    public void LayoutCards()
    {
        if (container == null)
            return;

        int cardCount = container.childCount;

        if (cardCount == 0)
            return;

        float spacing = CalculateSpacing(cardCount);
        float centerIndex = (cardCount - 1) * 0.5f;
        float halfRange = Mathf.Max(centerIndex, 1f);

        for (int index = 0; index < cardCount; index++)
        {
            if (container.GetChild(index) is not RectTransform card)
                continue;

            float offsetFromCenter = index - centerIndex;
            float normalized = offsetFromCenter / halfRange;
            float heightFactor = 1f - normalized * normalized;
            Vector2 position = new(
                offsetFromCenter * spacing,
                bottomOffset + arcHeight * heightFactor
            );

            Quaternion rotation = Quaternion.Euler(
                0f,
                0f,
                -normalized * maximumRotation
            );

            card.anchorMin = new Vector2(0.5f, 0f);
            card.anchorMax = new Vector2(0.5f, 0f);
            card.pivot = new Vector2(0.5f, 0f);

            if (card.TryGetComponent(
                out BattleCardButtonUI cardButton
            ))
            {
                cardButton.SetFanRestingPose(position, rotation);
            }
            else
            {
                card.anchoredPosition = position;
                card.localRotation = rotation;
                card.localScale = Vector3.one;
            }
        }
    }

    private float CalculateSpacing(int cardCount)
    {
        if (cardCount <= 1)
            return 0f;

        float cardWidth = 0f;

        if (container.GetChild(0) is RectTransform firstCard)
            cardWidth = firstCard.rect.width;

        float availableForSpacing = Mathf.Max(
            0f,
            container.rect.width - cardWidth
        );

        float maximumFittingSpacing =
            availableForSpacing / (cardCount - 1);

        return Mathf.Min(cardSpacing, maximumFittingSpacing);
    }

    private void EnsureContainer()
    {
        if (container == null)
            container = GetComponent<RectTransform>();
    }
}
