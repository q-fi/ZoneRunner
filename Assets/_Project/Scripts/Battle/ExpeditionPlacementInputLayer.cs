using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class ExpeditionPlacementInputLayer :
    MonoBehaviour,
    IPointerClickHandler
{
    private RectTransform rectTransform;
    private Action<Vector2> clickAction;
    private Action directClickAction;

    public void Configure(Action<Vector2> onClicked)
    {
        rectTransform = GetComponent<RectTransform>();
        clickAction = onClicked;
        directClickAction = null;
    }

    public void Configure(Action onClicked)
    {
        rectTransform = null;
        clickAction = null;
        directClickAction = onClicked;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        if (directClickAction != null)
        {
            directClickAction.Invoke();
            return;
        }

        if (rectTransform == null || clickAction == null)
            return;

        Camera eventCamera = eventData.pressEventCamera ??
            eventData.enterEventCamera;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform,
            eventData.position,
            eventCamera,
            out Vector2 localPoint
        ))
        {
            return;
        }

        clickAction.Invoke(localPoint);
    }
}
