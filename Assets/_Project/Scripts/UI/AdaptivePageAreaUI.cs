using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class AdaptivePageAreaUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform topBar;
    [SerializeField] private RectTransform bottomBar;

    [Header("Layout")]
    [SerializeField] private float overlap = 12f;

    private RectTransform pageArea;
    private RectTransform parentRect;

    private readonly Vector3[] topBarCorners = new Vector3[4];
    private readonly Vector3[] bottomBarCorners = new Vector3[4];

    private void Awake()
    {
        pageArea = GetComponent<RectTransform>();
        parentRect = pageArea.parent as RectTransform;
    }

    private void OnEnable()
    {
        Canvas.ForceUpdateCanvases();
        ApplyLayout();
    }

    private void LateUpdate()
    {
        ApplyLayout();
    }

    private void ApplyLayout()
    {
        if (pageArea == null ||
            parentRect == null ||
            topBar == null ||
            bottomBar == null)
        {
            return;
        }

        topBar.GetWorldCorners(topBarCorners);
        bottomBar.GetWorldCorners(bottomBarCorners);

        float topBarBottom =
            parentRect.InverseTransformPoint(topBarCorners[0]).y;

        float bottomBarTop =
            parentRect.InverseTransformPoint(bottomBarCorners[1]).y;

        float desiredTop = topBarBottom + overlap;
        float desiredBottom = bottomBarTop - overlap;

        if (desiredTop <= desiredBottom)
            return;

        pageArea.anchorMin = Vector2.zero;
        pageArea.anchorMax = Vector2.one;
        pageArea.pivot = new Vector2(0.5f, 0.5f);

        pageArea.offsetMin = new Vector2(
            0f,
            desiredBottom - parentRect.rect.yMin
        );

        pageArea.offsetMax = new Vector2(
            0f,
            desiredTop - parentRect.rect.yMax
        );
    }
}