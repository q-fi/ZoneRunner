using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ExpeditionBackpackGridUI : MonoBehaviour
{
    public event Action<ItemInstance> OnItemClicked;
    public event Action<int, int> OnPlacementCellClicked;

    [Header("Containers")]
    [SerializeField] private RectTransform panelRoot;
    [SerializeField] private RectTransform cellsContainer;
    [SerializeField] private RectTransform itemsContainer;

    [Header("Prefabs")]
    [SerializeField] private GameObject cellPrefab;
    [SerializeField] private GameObject itemIconPrefab;

    private int builtWidth = -1;
    private int builtHeight = -1;
    private float builtCellSize = -1f;
    private readonly HashSet<string> selectedInstanceIds = new();
    private readonly HashSet<string> selectableInstanceIds = new();
    private readonly HashSet<string> newLootInstanceIds = new();
    private GameObject placementLayer;
    private RectTransform placementLayerRect;
    private RectTransform placementPreviewRect;
    private Image placementPreviewImage;
    private bool placementModeActive;
    private ItemData placementItem;
    private int placementTargetX = -1;
    private int placementTargetY = -1;
    private bool placementTargetValid;
    private int currentGridWidth;
    private int currentGridHeight;
    private float currentCellSize;

    private void OnEnable()
    {
        if (TravelManager.Instance != null)
        {
            TravelManager.Instance.OnSelectedBackpackChanged -= Refresh;
            TravelManager.Instance.OnSelectedBackpackChanged += Refresh;
        }

        Refresh();
    }

    private void OnDisable()
    {
        if (TravelManager.Instance != null)
            TravelManager.Instance.OnSelectedBackpackChanged -= Refresh;
    }

    public void Refresh()
    {
        TravelManager travel = TravelManager.Instance;

        if (travel == null ||
            panelRoot == null ||
            cellsContainer == null ||
            itemsContainer == null ||
            cellPrefab == null ||
            itemIconPrefab == null)
        {
            return;
        }

        int width = travel.SelectedBackpackGridWidth;
        int height = travel.SelectedBackpackGridHeight;

        if (width <= 0 || height <= 0)
            return;

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(panelRoot);

        float cellSize = panelRoot.rect.width / width;

        if (cellSize <= 0f)
            return;

        if (builtWidth != width ||
            builtHeight != height ||
            !Mathf.Approximately(builtCellSize, cellSize))
        {
            BuildBackground(width, height, cellSize);
        }

        RedrawItems(travel, cellSize);
        currentGridWidth = width;
        currentGridHeight = height;
        currentCellSize = cellSize;
        RefreshPlacementLayer();
    }

    public void SetPlacementState(
        bool active,
        ItemData item,
        int targetX,
        int targetY,
        bool targetValid
    )
    {
        bool nextActive = active && item != null;
        bool interactionChanged = placementModeActive != nextActive;
        placementModeActive = nextActive;
        placementItem = placementModeActive ? item : null;
        placementTargetX = placementModeActive ? targetX : -1;
        placementTargetY = placementModeActive ? targetY : -1;
        placementTargetValid = placementModeActive && targetValid;
        RefreshPlacementLayer();

        if (interactionChanged)
            Refresh();
    }

    public void SetSelectionState(
        IEnumerable<string> selectableIds,
        IEnumerable<string> selectedIds,
        IEnumerable<string> newLootIds
    )
    {
        selectableInstanceIds.Clear();
        selectedInstanceIds.Clear();
        newLootInstanceIds.Clear();

        if (selectableIds != null)
        {
            foreach (string instanceId in selectableIds)
            {
                if (!string.IsNullOrEmpty(instanceId))
                    selectableInstanceIds.Add(instanceId);
            }
        }

        if (selectedIds != null)
        {
            foreach (string instanceId in selectedIds)
            {
                if (!string.IsNullOrEmpty(instanceId) &&
                    selectableInstanceIds.Contains(instanceId))
                {
                    selectedInstanceIds.Add(instanceId);
                }
            }
        }

        if (newLootIds != null)
        {
            foreach (string instanceId in newLootIds)
            {
                if (!string.IsNullOrEmpty(instanceId))
                    newLootInstanceIds.Add(instanceId);
            }
        }

        Refresh();
    }

    public static bool CanSelectForBattleLootReplacement(
        ItemInstance item
    )
    {
        return item != null &&
            item.Data != null &&
            item.StackCount > 0 &&
            item.ProtectedCount == 0 &&
            (item.Origins & ItemInstanceOrigin.ExpeditionLoot) != 0;
    }

    private void BuildBackground(
        int width,
        int height,
        float cellSize
    )
    {
        ClearChildren(cellsContainer);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                GameObject cell = Instantiate(
                    cellPrefab,
                    cellsContainer
                );
                int cellX = x;
                int cellY = y;
                ExpeditionPlacementInputLayer cellInput =
                    cell.GetComponent<ExpeditionPlacementInputLayer>();

                if (cellInput == null)
                {
                    cellInput = cell.AddComponent<
                        ExpeditionPlacementInputLayer
                    >();
                }

                cellInput.Configure(
                    () => HandlePlacementCellClick(cellX, cellY)
                );
                RectTransform rect =
                    cell.GetComponent<RectTransform>();

                SetGridRect(
                    rect,
                    x,
                    y,
                    1,
                    1,
                    cellSize
                );
            }
        }

        Vector2 size = panelRoot.sizeDelta;
        size.y = height * cellSize;
        panelRoot.sizeDelta = size;

        builtWidth = width;
        builtHeight = height;
        builtCellSize = cellSize;
    }

    private void RefreshPlacementLayer()
    {
        if (panelRoot == null ||
            currentGridWidth <= 0 ||
            currentGridHeight <= 0 ||
            currentCellSize <= 0f)
        {
            return;
        }

        EnsurePlacementLayer();

        if (placementLayer == null)
            return;

        placementLayer.SetActive(placementModeActive);

        if (!placementModeActive)
            return;

        placementLayer.transform.SetAsLastSibling();
        bool hasTarget = placementTargetX >= 0 &&
            placementTargetY >= 0 &&
            placementItem != null;
        placementPreviewRect.gameObject.SetActive(hasTarget);

        if (!hasTarget)
            return;

        placementPreviewRect.sizeDelta = new Vector2(
            Mathf.Max(1, placementItem.gridWidth) * currentCellSize,
            Mathf.Max(1, placementItem.gridHeight) * currentCellSize
        );
        placementPreviewRect.anchoredPosition = new Vector2(
            placementTargetX * currentCellSize,
            -placementTargetY * currentCellSize
        );
        placementPreviewImage.color = placementTargetValid
            ? new Color(0.1f, 0.85f, 0.25f, 0.48f)
            : new Color(0.9f, 0.12f, 0.12f, 0.5f);
    }

    private void EnsurePlacementLayer()
    {
        if (placementLayer != null)
            return;

        placementLayer = new GameObject(
            "PlacementInputLayer",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(RectMask2D)
        );
        placementLayer.transform.SetParent(panelRoot, false);
        placementLayer.layer = panelRoot.gameObject.layer;
        placementLayerRect =
            placementLayer.GetComponent<RectTransform>();
        placementLayerRect.anchorMin = Vector2.zero;
        placementLayerRect.anchorMax = Vector2.one;
        placementLayerRect.pivot = new Vector2(0f, 1f);
        placementLayerRect.offsetMin = Vector2.zero;
        placementLayerRect.offsetMax = Vector2.zero;

        Image inputImage = placementLayer.GetComponent<Image>();
        inputImage.color = Color.clear;
        inputImage.raycastTarget = false;

        var previewObject = new GameObject(
            "PlacementFootprintPreview",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Outline)
        );
        previewObject.transform.SetParent(placementLayer.transform, false);
        previewObject.layer = placementLayer.layer;
        placementPreviewRect =
            previewObject.GetComponent<RectTransform>();
        placementPreviewRect.anchorMin = new Vector2(0f, 1f);
        placementPreviewRect.anchorMax = new Vector2(0f, 1f);
        placementPreviewRect.pivot = new Vector2(0f, 1f);
        placementPreviewImage = previewObject.GetComponent<Image>();
        placementPreviewImage.raycastTarget = false;

        Outline outline = previewObject.GetComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(3f, -3f);
        outline.useGraphicAlpha = false;
        placementLayer.SetActive(false);
    }

    private void HandlePlacementCellClick(int x, int y)
    {
        if (!placementModeActive)
            return;

        OnPlacementCellClicked?.Invoke(x, y);
    }

    private void RedrawItems(
        TravelManager travel,
        float cellSize
    )
    {
        ClearChildren(itemsContainer);

        foreach (var item in travel.SelectedBackpackItems)
        {
            if (item?.Data == null ||
                !travel.TryGetSelectedBackpackItemPosition(
                    item,
                    out int x,
                    out int y
                ))
            {
                continue;
            }

            GameObject icon = Instantiate(
                itemIconPrefab,
                itemsContainer
            );
            RectTransform rect = icon.GetComponent<RectTransform>();

            SetGridRect(
                rect,
                x,
                y,
                item.Data.gridWidth,
                item.Data.gridHeight,
                cellSize
            );

            ExpeditionItemIconView view =
                icon.GetComponent<ExpeditionItemIconView>();

            if (view == null)
                view = icon.AddComponent<ExpeditionItemIconView>();

            bool selectable =
                selectableInstanceIds.Contains(item.InstanceId) &&
                CanSelectForBattleLootReplacement(item);

            string badge;

            if (item.ProtectedCount > 0)
                badge = $"LOCK {item.ProtectedCount}";
            else if (newLootInstanceIds.Contains(item.InstanceId))
                badge = "NEW";
            else
                badge = selectable ? "LOOT" : "NEW";

            ItemInstance currentItem = item;
            Action onClick = placementModeActive || selectable
                ? () => OnItemClicked?.Invoke(currentItem)
                : null;

            view.Setup(
                item.Data,
                item.StackCount,
                badge,
                false,
                onClick,
                selectable
            );
            view.SetSelected(
                selectedInstanceIds.Contains(item.InstanceId)
            );
        }
    }

    private static void SetGridRect(
        RectTransform rect,
        int x,
        int y,
        int width,
        int height,
        float cellSize
    )
    {
        if (rect == null)
            return;

        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.sizeDelta = new Vector2(
            width * cellSize,
            height * cellSize
        );
        rect.anchoredPosition = new Vector2(
            x * cellSize,
            -y * cellSize
        );
    }

    private static void ClearChildren(RectTransform container)
    {
        for (int index = container.childCount - 1;
            index >= 0;
            index--)
        {
            Destroy(container.GetChild(index).gameObject);
        }
    }
}
