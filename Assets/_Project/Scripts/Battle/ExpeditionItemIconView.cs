using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ExpeditionItemIconView :
    MonoBehaviour,
    IPointerClickHandler
{
    private TMP_Text itemNameText;
    private TMP_Text countText;
    private TMP_Text badgeText;
    private Outline selectionOutline;
    private Image rootImage;
    private GameObject selectionUndoObject;
    private Action clickAction;
    private bool clickThroughWhenSelected;

    public void Setup(
        ItemData item,
        int count,
        string badge = null,
        bool alwaysShowCount = false,
        Action onClick = null,
        bool allowCellClickWhenSelected = false
    )
    {
        DisableInventoryInteraction();
        clickAction = onClick;
        clickThroughWhenSelected = allowCellClickWhenSelected;

        rootImage = GetComponent<Image>();

        if (rootImage != null)
        {
            rootImage.raycastTarget = clickAction != null;

            if (item != null && item.icon != null)
            {
                rootImage.sprite = item.icon;
                rootImage.preserveAspect = true;
            }
        }

        EnsureLabels();

        itemNameText.text = item != null
            ? item.itemName
            : "Missing Item";

        countText.text = count > 1 || alwaysShowCount
            ? $"x{count}"
            : string.Empty;

        badgeText.text = badge ?? string.Empty;
        badgeText.gameObject.SetActive(
            !string.IsNullOrEmpty(badge)
        );

        SetSelected(false);
    }

    public void SetSelected(bool selected)
    {
        EnsureSelectionOutline();
        selectionOutline.enabled = selected;

        bool showUndo = selected &&
            clickThroughWhenSelected &&
            clickAction != null;

        if (rootImage == null)
            rootImage = GetComponent<Image>();

        if (rootImage != null)
        {
            rootImage.raycastTarget =
                clickAction != null && !showUndo;
        }

        if (showUndo)
            EnsureSelectionUndo();

        if (selectionUndoObject != null)
            selectionUndoObject.SetActive(showUndo);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        clickAction?.Invoke();
    }

    private void DisableInventoryInteraction()
    {
        ItemIconUI inventoryIcon = GetComponent<ItemIconUI>();
        Button button = GetComponent<Button>();

        if (inventoryIcon != null)
            inventoryIcon.enabled = false;

        if (button != null)
            button.enabled = false;
    }

    private void EnsureLabels()
    {
        if (itemNameText == null)
        {
            itemNameText = CreateLabel(
                "ExpeditionItemName",
                new Vector2(0.06f, 0.18f),
                new Vector2(0.94f, 0.82f),
                14f,
                TextAlignmentOptions.Center,
                Color.black
            );
        }

        if (countText == null)
        {
            countText = CreateLabel(
                "ExpeditionItemCount",
                new Vector2(0.38f, 0f),
                new Vector2(0.96f, 0.32f),
                16f,
                TextAlignmentOptions.BottomRight,
                Color.black
            );
        }

        if (badgeText == null)
        {
            badgeText = CreateLabel(
                "ExpeditionItemBadge",
                new Vector2(0.02f, 0.72f),
                new Vector2(0.98f, 0.98f),
                12f,
                TextAlignmentOptions.TopLeft,
                new Color(0.65f, 0.08f, 0.08f, 1f)
            );
        }
    }

    private void EnsureSelectionOutline()
    {
        if (selectionOutline != null)
            return;

        selectionOutline = gameObject.AddComponent<Outline>();
        selectionOutline.effectColor =
            new Color(1f, 0.78f, 0f, 1f);
        selectionOutline.effectDistance = new Vector2(4f, -4f);
        selectionOutline.useGraphicAlpha = false;
        selectionOutline.enabled = false;
    }

    private void EnsureSelectionUndo()
    {
        if (selectionUndoObject != null)
            return;

        selectionUndoObject = new GameObject(
            "UndoRemovalSelection",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image)
        );
        selectionUndoObject.transform.SetParent(transform, false);

        RectTransform rect =
            selectionUndoObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.sizeDelta = new Vector2(46f, 46f);
        rect.anchoredPosition = Vector2.zero;

        Image background = selectionUndoObject.GetComponent<Image>();
        background.color = new Color(0.72f, 0.08f, 0.08f, 0.96f);
        background.raycastTarget = true;

        TMP_Text label = CreateLabel(
            "UndoRemovalLabel",
            Vector2.zero,
            Vector2.one,
            30f,
            TextAlignmentOptions.Center,
            Color.white
        );
        label.transform.SetParent(selectionUndoObject.transform, false);
        label.text = "×";
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.overflowMode = TextOverflowModes.Overflow;
    }

    private TMP_Text CreateLabel(
        string objectName,
        Vector2 anchorMin,
        Vector2 anchorMax,
        float fontSize,
        TextAlignmentOptions alignment,
        Color color
    )
    {
        var labelObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI)
        );

        labelObject.transform.SetParent(transform, false);

        RectTransform rect =
            labelObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        TextMeshProUGUI label =
            labelObject.GetComponent<TextMeshProUGUI>();
        label.fontSize = fontSize;
        label.alignment = alignment;
        label.color = color;
        label.textWrappingMode = TextWrappingModes.Normal;
        label.raycastTarget = false;
        label.overflowMode = TextOverflowModes.Ellipsis;

        return label;
    }
}
