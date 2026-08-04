using UnityEngine;

public class BackpackPresetGridUI : MonoBehaviour
{
    [SerializeField] private RectTransform cellsContainer;
    [SerializeField] private RectTransform itemsContainer;

    [SerializeField] private GameObject cellPrefab;
    [SerializeField] private GameObject itemIconPrefab;

    [SerializeField] private float cellSize = 64f;

    private BackpackPreset currentPreset;

    public void SetPreset(BackpackPreset preset)
    {
        currentPreset = preset;
        Redraw();
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

            iconObj.GetComponent<ItemIconUI>().Setup(instance);
        }
    }
}