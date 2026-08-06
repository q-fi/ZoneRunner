using UnityEngine;
using UnityEngine.UI;

public class InventoryGridUI : MonoBehaviour
{
    [Header("Containers")]
    [SerializeField] private RectTransform cellsContainer;
    [SerializeField] private RectTransform itemsContainer;
    [SerializeField] private RectTransform content;

    [Header("References")]
    [SerializeField] private RectTransform equipmentPanel;
    [SerializeField] private RectTransform viewport;

    [Header("Prefabs")]
    [SerializeField] private GameObject cellPrefab;
    [SerializeField] private GameObject itemIconPrefab;

    [Header("Layout")]
    [SerializeField] private float topPanelSpacing = 15f;

    private float cellSize;

    private void Start()
    {
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(viewport);

        var grid = InventoryManager.Instance.Grid;

        float availableWidth = viewport.rect.width;
        cellSize = availableWidth / grid.Width;

        BuildBackgroundGrid();

        RecalculateOffset(equipmentPanel);

        InventoryManager.Instance.OnInventoryChanged += Redraw;
        Redraw();
    }

    private void OnDestroy()
    {
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.OnInventoryChanged -= Redraw;
    }

    private void BuildBackgroundGrid()
    {
        var grid = InventoryManager.Instance.Grid;

        for (int y = 0; y < grid.Height; y++)
        {
            for (int x = 0; x < grid.Width; x++)
            {
                GameObject cell = Instantiate(cellPrefab, cellsContainer);

                RectTransform rt = cell.GetComponent<RectTransform>();

                rt.anchorMin = new Vector2(0, 1);
                rt.anchorMax = new Vector2(0, 1);
                rt.pivot = new Vector2(0, 1);

                rt.sizeDelta = new Vector2(cellSize, cellSize);
                rt.anchoredPosition = new Vector2(
                    x * cellSize,
                    -y * cellSize);
            }
        }
    }

    public void RecalculateOffset(RectTransform activeTopPanel)
    {
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(activeTopPanel);

        float lowestY = float.MaxValue;

        foreach (RectTransform child in activeTopPanel.GetComponentsInChildren<RectTransform>(true))
        {
            if (child == activeTopPanel)
                continue;

            Vector3[] corners = new Vector3[4];
            child.GetWorldCorners(corners);

            float y = corners[0].y;

            if (y < lowestY)
                lowestY = y;
        }

        Vector2 localPoint;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            content,
            RectTransformUtility.WorldToScreenPoint(null, new Vector3(0, lowestY, 0)),
            null,
            out localPoint);

        float gridOffsetY = -localPoint.y + topPanelSpacing;

        var grid = InventoryManager.Instance.Grid;

        content.sizeDelta = new Vector2(
            content.sizeDelta.x,
            gridOffsetY + grid.Height * cellSize);

        cellsContainer.anchoredPosition = new Vector2(0, -gridOffsetY);
        itemsContainer.anchoredPosition = new Vector2(0, -gridOffsetY);
    }

    private void Redraw()
    {
        foreach (Transform child in itemsContainer)
            Destroy(child.gameObject);

        var grid = InventoryManager.Instance.Grid;

        foreach (var instance in grid.GetAllItems())
        {
            var pos = grid.GetPosition(instance);

            if (pos == null)
                continue;

            GameObject iconObj = Instantiate(itemIconPrefab, itemsContainer);

            RectTransform rt = iconObj.GetComponent<RectTransform>();

            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);

            rt.sizeDelta = new Vector2(
                instance.Data.gridWidth * cellSize,
                instance.Data.gridHeight * cellSize);

            rt.anchoredPosition = new Vector2(
                pos.Value.x * cellSize,
                -pos.Value.y * cellSize);

            iconObj.GetComponent<ItemIconUI>()
                .Setup(instance, ItemContext.Inventory);
        }
    }
}