using UnityEngine;

public class BackpackPresetGridUI : MonoBehaviour
{
    [SerializeField] private RectTransform panelRoot; // контейнер, під ширину якого підганяється розмір клітинки
    [SerializeField] private RectTransform cellsContainer;
    [SerializeField] private RectTransform itemsContainer;

    [SerializeField] private GameObject cellPrefab;
    [SerializeField] private GameObject itemIconPrefab;

    private float cellSize;
    private bool backgroundBuilt;
    private BackpackPreset currentPreset;

    private void Start()
    {
        InventoryManager.Instance.OnPresetChanged += HandlePresetChanged;
        InventoryManager.Instance.OnCurrentPresetChanged += HandleCurrentPresetChanged;
    }

    private void OnDestroy()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnPresetChanged -= HandlePresetChanged;
            InventoryManager.Instance.OnCurrentPresetChanged -= HandleCurrentPresetChanged;
        }
    }

    private void HandlePresetChanged()
    {
        if (currentPreset == InventoryManager.Instance.BackpackPresets.CurrentPreset)
            Redraw();
    }

    private void HandleCurrentPresetChanged()
    {
        SetPreset(InventoryManager.Instance.BackpackPresets.CurrentPreset);
    }

    public void SetPreset(BackpackPreset preset)
    {
        currentPreset = preset;

        if (!backgroundBuilt)
            BuildBackgroundGrid();

        Redraw();
    }

    private void BuildBackgroundGrid()
    {
    var grid = currentPreset.Grid;

    Canvas.ForceUpdateCanvases();
    cellSize = panelRoot.rect.width / grid.Width;

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

    // NEW: підганяємо реальну висоту панелі під розмір сітки, щоб не було зайвого простору знизу
    Vector2 size = panelRoot.sizeDelta;
    size.y = grid.Height * cellSize;
    panelRoot.sizeDelta = size;

    backgroundBuilt = true;
    }

    private void Redraw()
    {
        if (currentPreset == null)
            return;

        foreach (Transform child in itemsContainer)
            Destroy(child.gameObject);

        var grid = currentPreset.Grid;

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

            iconObj.GetComponent<ItemIconUI>().Setup(instance, ItemContext.Preset);
        }
    }
}