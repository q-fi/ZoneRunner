using System;
using UnityEngine;

public class MapPanelController : MonoBehaviour
{
    [Header("Views")]
    [SerializeField] private GameObject worldMapView;
    [SerializeField] private GameObject regionMapView;

    [Header("Region Map")]
    [SerializeField] private Transform regionMapRoot;

    private GameObject currentRegionMapInstance;

    public RegionData CurrentRegion { get; private set; }

    public event Action<RegionData> OnRegionOpened;

    private void OnEnable()
    {
        ShowWorldMap();
    }

    public void OpenRegion(RegionData region)
    {
        if (region == null)
        {
            Debug.LogError(
                "MapPanelController: RegionData is missing."
            );

            return;
        }

        CurrentRegion = region;

        worldMapView.SetActive(false);
        regionMapView.SetActive(true);

        LoadRegionMap(region);

        OnRegionOpened?.Invoke(CurrentRegion);

        Debug.Log($"Opened region: {CurrentRegion.RegionName}");
    }

    public void ShowWorldMap()
    {
        worldMapView.SetActive(true);
        regionMapView.SetActive(false);

        CurrentRegion = null;

        ClearRegionMap();
    }

    private void LoadRegionMap(RegionData region)
    {
        ClearRegionMap();

        if (regionMapRoot == null)
        {
            Debug.LogError(
                "MapPanelController: RegionMapRoot is not assigned."
            );

            return;
        }

        if (region.RegionMapPrefab == null)
        {
            Debug.LogWarning(
                $"{region.RegionName} has no Region Map Prefab."
            );

            return;
        }

        currentRegionMapInstance = Instantiate(
            region.RegionMapPrefab,
            regionMapRoot
        );

        RectTransform instanceRect =
            currentRegionMapInstance.GetComponent<RectTransform>();

        if (instanceRect != null)
        {
            instanceRect.anchorMin = Vector2.zero;
            instanceRect.anchorMax = Vector2.one;
            instanceRect.offsetMin = Vector2.zero;
            instanceRect.offsetMax = Vector2.zero;
            instanceRect.localScale = Vector3.one;
        }
    }

    private void ClearRegionMap()
    {
        if (regionMapRoot == null)
            return;

        for (int i = regionMapRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(regionMapRoot.GetChild(i).gameObject);
        }

        currentRegionMapInstance = null;
    }
}