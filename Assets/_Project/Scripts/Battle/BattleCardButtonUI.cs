using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class BattleCardButtonUI : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler
{
    [SerializeField] private BattleController battleController;
    [SerializeField] private TMP_Text label;

    [Header("Interaction Visuals")]
    [Min(0f)]
    [SerializeField] private float raisedHeight = 75f;

    [Min(1f)]
    [SerializeField] private float raisedScale = 1.18f;

    [Min(0.1f)]
    [SerializeField] private float animationSpeed = 14f;

    [SerializeField] private Color highlightColor =
        new(1f, 0.82f, 0.2f, 1f);

    private Button button;
    private RectTransform cardRect;
    private Canvas sortingCanvas;
    private Outline highlightOutline;

    private Vector2 restingPosition;
    private Quaternion restingRotation = Quaternion.identity;
    private Vector2 targetPosition;
    private Quaternion targetRotation = Quaternion.identity;
    private Vector3 targetScale = Vector3.one;

    private bool hasRestingPose;
    private bool pointerInside;
    private BattleCardRuntime runtimeCard;

    public BattleCardData Card => runtimeCard?.Data;

    private void Awake()
    {
        EnsureVisualReferences();
        button = GetComponent<Button>();

        if (battleController == null)
            battleController = GetComponentInParent<BattleController>();

        if (label == null)
            label = GetComponentInChildren<TMP_Text>();
    }

    private void OnEnable()
    {
        button.onClick.RemoveListener(SelectCard);
        button.onClick.AddListener(SelectCard);

        if (battleController != null)
        {
            battleController.OnBattleStateChanged -= Refresh;
            battleController.OnBattleStateChanged += Refresh;
        }

        Refresh();
    }

    private void OnDisable()
    {
        pointerInside = false;

        if (button != null)
            button.onClick.RemoveListener(SelectCard);

        if (battleController != null)
            battleController.OnBattleStateChanged -= Refresh;

        if (highlightOutline != null)
            highlightOutline.enabled = false;
    }

    private void Update()
    {
        if (!hasRestingPose || cardRect == null)
            return;

        float blend = 1f - Mathf.Exp(
            -animationSpeed * Time.unscaledDeltaTime
        );

        cardRect.anchoredPosition = Vector2.Lerp(
            cardRect.anchoredPosition,
            targetPosition,
            blend
        );

        cardRect.localRotation = Quaternion.Slerp(
            cardRect.localRotation,
            targetRotation,
            blend
        );

        cardRect.localScale = Vector3.Lerp(
            cardRect.localScale,
            targetScale,
            blend
        );
    }

    private void SelectCard()
    {
        if (battleController != null && runtimeCard != null)
            battleController.SelectCard(runtimeCard);
    }

    public void Setup(
        BattleController controller,
        BattleCardRuntime cardInstance
    )
    {
        if (battleController != null)
            battleController.OnBattleStateChanged -= Refresh;

        battleController = controller;
        runtimeCard = cardInstance;

        if (isActiveAndEnabled && battleController != null)
        {
            battleController.OnBattleStateChanged -= Refresh;
            battleController.OnBattleStateChanged += Refresh;
        }

        Refresh();
    }

    private void Refresh()
    {
        if (label != null)
        {
            label.text = BuildLabelText();
        }

        if (button == null)
            return;

        button.interactable =
            battleController != null &&
            runtimeCard != null &&
            battleController.CanSelectCard(runtimeCard);

        UpdateInteractionVisual();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        pointerInside = true;
        UpdateInteractionVisual();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        pointerInside = false;
        UpdateInteractionVisual();
    }

    public void SetFanRestingPose(
        Vector2 position,
        Quaternion rotation
    )
    {
        EnsureVisualReferences();

        restingPosition = position;
        restingRotation = rotation;
        hasRestingPose = true;

        UpdateInteractionVisual();

        if (!Application.isPlaying)
            SnapToTargetPose();
    }

    private string BuildLabelText()
    {
        BattleCardData card = Card;

        if (card == null)
            return "CARD NOT ASSIGNED";

        string text = card.displayName;

        if (battleController != null &&
            battleController.TryGetCardDisplayedDamage(
                card,
                out float damage
            ))
        {
            text += $"\nDamage: {damage:0.#}";
        }

        text += $"\n{card.staminaCost:0.#} Stamina";
        return text;
    }

    private void UpdateInteractionVisual()
    {
        EnsureVisualReferences();

        if (!hasRestingPose)
            return;

        bool isSelected =
            battleController != null &&
            runtimeCard != null &&
            battleController.SelectedCard == runtimeCard;

        bool isRaised = pointerInside || isSelected;

        targetPosition = restingPosition +
            Vector2.up * (isRaised ? raisedHeight : 0f);

        targetRotation = isRaised
            ? Quaternion.identity
            : restingRotation;

        targetScale = isRaised
            ? Vector3.one * raisedScale
            : Vector3.one;

        if (sortingCanvas != null)
        {
            sortingCanvas.sortingOrder = pointerInside
                ? 110
                : isSelected ? 100 : 0;
        }

        if (highlightOutline != null)
            highlightOutline.enabled = isRaised;
    }

    private void SnapToTargetPose()
    {
        if (cardRect == null)
            return;

        cardRect.anchoredPosition = targetPosition;
        cardRect.localRotation = targetRotation;
        cardRect.localScale = targetScale;
    }

    private void EnsureVisualReferences()
    {
        if (cardRect == null)
            cardRect = transform as RectTransform;

        if (sortingCanvas == null)
        {
            sortingCanvas = GetComponent<Canvas>();

            if (sortingCanvas == null && Application.isPlaying)
                sortingCanvas = gameObject.AddComponent<Canvas>();

            if (sortingCanvas != null)
                sortingCanvas.overrideSorting = true;
        }

        if (Application.isPlaying &&
            GetComponent<GraphicRaycaster>() == null)
        {
            gameObject.AddComponent<GraphicRaycaster>();
        }

        if (highlightOutline == null)
        {
            highlightOutline = GetComponent<Outline>();

            if (highlightOutline == null && Application.isPlaying)
                highlightOutline = gameObject.AddComponent<Outline>();

            if (highlightOutline != null)
            {
                highlightOutline.effectColor = highlightColor;
                highlightOutline.effectDistance = new Vector2(3f, -3f);
                highlightOutline.useGraphicAlpha = true;
                highlightOutline.enabled = false;
            }
        }
    }
}
