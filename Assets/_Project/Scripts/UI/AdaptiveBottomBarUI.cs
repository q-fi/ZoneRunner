using UnityEngine;
using DeviceScreen = UnityEngine.Device.Screen;

[RequireComponent(typeof(RectTransform))]
public class AdaptiveBottomBarUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform content;

    [Header("Layout")]
    [SerializeField] private float contentHeight = 120f;
    [SerializeField] private float backgroundBleed = 12f;

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

        float canvasHeight = canvasRect.rect.height;

        float scaleY =
            canvasHeight / DeviceScreen.height;

        float bottomInset =
            safeArea.yMin * scaleY;

        float totalHeight =
            bottomInset +
            contentHeight +
            backgroundBleed;

        ConfigureBar(totalHeight);
        ConfigureContent(bottomInset);
    }

    private void ConfigureBar(float height)
    {
        barRect.anchorMin = new Vector2(0f, 0f);
        barRect.anchorMax = new Vector2(1f, 0f);
        barRect.pivot = new Vector2(0.5f, 0f);
        barRect.anchoredPosition = Vector2.zero;

        barRect.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Vertical,
            height
        );
    }

    private void ConfigureContent(float bottomInset)
    {
        content.anchorMin = new Vector2(0f, 0f);
        content.anchorMax = new Vector2(1f, 0f);
        content.pivot = new Vector2(0.5f, 0f);

        content.anchoredPosition = new Vector2(
            0f,
            bottomInset
        );

        content.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Vertical,
            contentHeight
        );
    }
}