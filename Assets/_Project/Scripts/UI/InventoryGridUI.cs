using UnityEngine;
using UnityEngine.UI;

public class InventoryGridUI : MonoBehaviour
{
    [SerializeField] private RectTransform cellsContainer;
    [SerializeField] private RectTransform itemsContainer;
    [SerializeField] private RectTransform content;
    [SerializeField] private RectTransform equipmentPanel;
    [SerializeField] private RectTransform viewport;
    [SerializeField] private GameObject cellPrefab;
    [SerializeField] private GameObject itemIconPrefab;
    [SerializeField] private InventoryViewMode viewMode = InventoryViewMode.Inventory;

    private float cellSize;
    private float gridOffsetY;

    private void Start()
    {
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(viewport);

        var grid = InventoryManager.Instance.Grid;

        float availableWidth = viewport.rect.width;
        cellSize = availableWidth / grid.Width;

        gridOffsetY = equipmentPanel.rect.height;

        BuildBackgroundGrid();

        Debug.Log($"cellSize={cellSize}, grid.Width={grid.Width}, grid.Height={grid.Height}, gridOffsetY={gridOffsetY}, content.sizeDelta={content.sizeDelta}");
        
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
        content.sizeDelta = new Vector2(0, gridOffsetY + grid.Height * cellSize);

        cellsContainer.anchoredPosition = new Vector2(0, -gridOffsetY);
        itemsContainer.anchoredPosition = new Vector2(0, -gridOffsetY);

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
                rt.anchoredPosition = new Vector2(x * cellSize, -y * cellSize);
            }
        }
    }

    private InventoryGrid GetCurrentGrid()
{
    switch (viewMode)
    {
        case InventoryViewMode.Inventory:
            return InventoryManager.Instance.Grid;

        case InventoryViewMode.BackpackPreset:
            // Поки що пресети ще не використовують InventoryGrid,
            // тому тимчасово показуємо звичайний інвентар.
            return InventoryManager.Instance.Grid;

        default:
            return InventoryManager.Instance.Grid;
    }
}   

    private void Redraw()
{
    foreach (Transform child in itemsContainer)
        Destroy(child.gameObject);

    var grid = GetCurrentGrid();

    foreach (var instance in grid.GetAllItems())
    {
        var pos = grid.GetPosition(instance);
        if (pos == null) continue;

        GameObject iconObj = Instantiate(itemIconPrefab, itemsContainer);
        RectTransform rt = iconObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.sizeDelta = new Vector2(instance.Data.gridWidth * cellSize, instance.Data.gridHeight * cellSize);
        rt.anchoredPosition = new Vector2(pos.Value.x * cellSize, -pos.Value.y * cellSize);

        iconObj.GetComponent<ItemIconUI>().Setup(instance, ItemContext.Inventory);
    }
}
}