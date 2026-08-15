using UnityEngine;
using DeviceScreen = UnityEngine.Device.Screen;

[RequireComponent(typeof(RectTransform))]
public class SafeAreaUI : MonoBehaviour
{
    private RectTransform rectTransform;

    private Rect lastSafeArea;
    private Vector2Int lastScreenSize;
    private ScreenOrientation lastOrientation;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        ApplySafeArea();
    }

    private void OnEnable()
    {
        ApplySafeArea();
    }

    private void Update()
    {
        if (DeviceScreen.safeArea != lastSafeArea ||
            DeviceScreen.width != lastScreenSize.x ||
            DeviceScreen.height != lastScreenSize.y ||
            DeviceScreen.orientation != lastOrientation)
        {
            ApplySafeArea();
        }
    }

    private void ApplySafeArea()
    {
        if (rectTransform == null ||
            DeviceScreen.width <= 0 ||
            DeviceScreen.height <= 0)
        {
            return;
        }

        Rect safeArea = DeviceScreen.safeArea;

        lastSafeArea = safeArea;
        lastScreenSize = new Vector2Int(
            DeviceScreen.width,
            DeviceScreen.height
        );
        lastOrientation = DeviceScreen.orientation;

        Vector2 anchorMin = safeArea.position;
        Vector2 anchorMax =
            safeArea.position + safeArea.size;

        anchorMin.x /= DeviceScreen.width;
        anchorMin.y /= DeviceScreen.height;

        anchorMax.x /= DeviceScreen.width;
        anchorMax.y /= DeviceScreen.height;

        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;

        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }
}