using UnityEngine;
using DeviceScreen = UnityEngine.Device.Screen;

[RequireComponent(typeof(RectTransform))]
public class AdaptiveTopBarUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform content;
    [SerializeField] private RectTransform moneyText;
    [SerializeField] private RectTransform timerText;

    [Header("Layout")]
    [SerializeField] private float baseBarHeight = 100f;
    [SerializeField] private float horizontalPadding = 30f;
    [SerializeField] private float verticalPadding = 15f;
    [SerializeField] private float cutoutPadding = 20f;
    [SerializeField] private float elementSpacing = 20f;

    [Header("Element Sizes")]
    [SerializeField] private float moneyWidth = 220f;
    [SerializeField] private float timerWidth = 400f;
    [SerializeField] private float elementHeight = 70f;

    [Header("Background")]
    [SerializeField] private float backgroundBleed = 4f;

    private RectTransform barRect;
    private RectTransform canvasRect;

    private Rect lastSafeArea;
    private Vector2Int lastScreenSize;

    private void Awake()
    {
        barRect = GetComponent<RectTransform>();

        Canvas canvas = GetComponentInParent<Canvas>();

        if (canvas != null)
            canvasRect = canvas.GetComponent<RectTransform>();

        ApplyLayout();
    }

    private void OnEnable()
    {
        ApplyLayout();
    }

    private void Update()
    {
        if (DeviceScreen.safeArea != lastSafeArea ||
            DeviceScreen.width != lastScreenSize.x ||
            DeviceScreen.height != lastScreenSize.y)
        {
            ApplyLayout();
        }
    }

    private void ApplyLayout()
    {
        if (canvasRect == null ||
            content == null ||
            moneyText == null ||
            timerText == null ||
            DeviceScreen.width <= 0 ||
            DeviceScreen.height <= 0)
        {
            return;
        }

        Canvas.ForceUpdateCanvases();

        Rect safeArea = DeviceScreen.safeArea;

        lastSafeArea = safeArea;
        lastScreenSize = new Vector2Int(
            DeviceScreen.width,
            DeviceScreen.height
        );

        float canvasWidth = canvasRect.rect.width;
        float canvasHeight = canvasRect.rect.height;

        float scaleX =
            canvasWidth / DeviceScreen.width;

        float scaleY =
            canvasHeight / DeviceScreen.height;

        float leftLimit =
            safeArea.xMin * scaleX +
            horizontalPadding;

        float rightLimit =
            safeArea.xMax * scaleX -
            horizontalPadding;

        float moneyX = leftLimit;
        float timerX = rightLimit - timerWidth;

        float cutoutLeft = float.MaxValue;
        float cutoutRight = float.MinValue;
        float deepestCutout = 0f;

        bool hasTopCutout = false;

        foreach (Rect cutout in DeviceScreen.cutouts)
        {
            bool isTopCutout =
                cutout.yMax >=
                DeviceScreen.height * 0.75f;

            if (!isTopCutout)
                continue;

            hasTopCutout = true;

            cutoutLeft = Mathf.Min(
                cutoutLeft,
                cutout.xMin * scaleX - cutoutPadding
            );

            cutoutRight = Mathf.Max(
                cutoutRight,
                cutout.xMax * scaleX + cutoutPadding
            );

            float cutoutDepth =
                (DeviceScreen.height - cutout.yMin) *
                scaleY;

            deepestCutout = Mathf.Max(
                deepestCutout,
                cutoutDepth
            );
        }

        bool placedBesideCutout = false;
        bool movedBelowCutout = false;

        float groupWidth =
            moneyWidth +
            elementSpacing +
            timerWidth;

        if (hasTopCutout)
        {
            float leftSpace =
                cutoutLeft - leftLimit;

            float rightSpace =
                rightLimit - cutoutRight;

            bool moneyFitsLeft =
                leftSpace >= moneyWidth;

            bool timerFitsRight =
                rightSpace >= timerWidth;

            if (moneyFitsLeft && timerFitsRight)
            {
                moneyX =
                    leftLimit +
                    (leftSpace - moneyWidth) * 0.5f;

                timerX =
                    cutoutRight +
                    (rightSpace - timerWidth) * 0.5f;

                placedBesideCutout = true;
            }
            else if (rightSpace >= groupWidth)
            {
                float groupStart =
                    cutoutRight +
                    (rightSpace - groupWidth) * 0.5f;

                moneyX = groupStart;

                timerX =
                    groupStart +
                    moneyWidth +
                    elementSpacing;

                placedBesideCutout = true;
            }
            else if (leftSpace >= groupWidth)
            {
                float groupStart =
                    leftLimit +
                    (leftSpace - groupWidth) * 0.5f;

                moneyX = groupStart;

                timerX =
                    groupStart +
                    moneyWidth +
                    elementSpacing;

                placedBesideCutout = true;
            }
        }

        if (!hasTopCutout)
        {
            PlaceInTwoColumns(
                leftLimit,
                rightLimit,
                out moneyX,
                out timerX
            );
        }
        else if (!placedBesideCutout)
        {
            float availableWidth =
                rightLimit - leftLimit;

            float groupStart =
                leftLimit +
                (availableWidth - groupWidth) * 0.5f;

            moneyX = groupStart;

            timerX =
                groupStart +
                moneyWidth +
                elementSpacing;

            movedBelowCutout = true;
        }

        float contentHeight;

        if (movedBelowCutout)
        {
            contentHeight =
                deepestCutout +
                verticalPadding +
                elementHeight +
                verticalPadding;
        }
        else
        {
            contentHeight = Mathf.Max(
                baseBarHeight,
                deepestCutout,
                elementHeight + verticalPadding * 2f
            );
        }

        float rowTop = movedBelowCutout
            ? deepestCutout + verticalPadding
            : (contentHeight - elementHeight) * 0.5f;

        ConfigureBar(contentHeight + backgroundBleed);

        PlaceElement(
            moneyText,
            moneyX,
            rowTop,
            moneyWidth
        );

        PlaceElement(
            timerText,
            timerX,
            rowTop,
            timerWidth
        );
    }

    private void PlaceInTwoColumns(
        float left,
        float right,
        out float moneyX,
        out float timerX
    )
    {
        float middle = (left + right) * 0.5f;

        float leftColumnWidth = middle - left;
        float rightColumnWidth = right - middle;

        moneyX =
            left +
            (leftColumnWidth - moneyWidth) * 0.5f;

        timerX =
            middle +
            (rightColumnWidth - timerWidth) * 0.5f;
    }

    private void ConfigureBar(float height)
    {
        barRect.anchorMin = new Vector2(0f, 1f);
        barRect.anchorMax = new Vector2(1f, 1f);
        barRect.pivot = new Vector2(0.5f, 1f);
        barRect.anchoredPosition = Vector2.zero;

        barRect.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Vertical,
            height
        );

        content.anchorMin = Vector2.zero;
        content.anchorMax = Vector2.one;
        content.offsetMin = Vector2.zero;
        content.offsetMax = Vector2.zero;
    }

    private void PlaceElement(
        RectTransform element,
        float x,
        float top,
        float width
    )
    {
        element.anchorMin = new Vector2(0f, 1f);
        element.anchorMax = new Vector2(0f, 1f);
        element.pivot = new Vector2(0f, 1f);

        element.anchoredPosition = new Vector2(
            x,
            -top
        );

        element.sizeDelta = new Vector2(
            width,
            elementHeight
        );
    }
}