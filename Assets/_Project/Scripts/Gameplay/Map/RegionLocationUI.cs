using UnityEngine;

public class RegionLocationsUI : MonoBehaviour
{
    [Header("Controller")]
    [SerializeField] private MapPanelController mapPanelController;

    [Header("Location Slots")]
    [SerializeField] private LocationButtonUI[] locationButtons;

    private bool isSubscribed;

    private void Awake()
    {
        if (mapPanelController == null)
        {
            mapPanelController =
                GetComponentInParent<MapPanelController>();
        }
    }

    private void OnEnable()
    {
        Subscribe();

        if (mapPanelController != null)
        {
            RefreshLocations(
                mapPanelController.CurrentRegion
            );
        }
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void OnDestroy()
    {
        Unsubscribe();
    }

    private void Subscribe()
    {
        if (isSubscribed ||
            mapPanelController == null)
        {
            return;
        }

        mapPanelController.OnRegionOpened +=
            RefreshLocations;

        isSubscribed = true;
    }

    private void Unsubscribe()
    {
        if (!isSubscribed ||
            mapPanelController == null)
        {
            return;
        }

        mapPanelController.OnRegionOpened -=
            RefreshLocations;

        isSubscribed = false;
    }

    private void RefreshLocations(RegionData region)
    {
        if (locationButtons == null)
            return;

        int locationCount =
            region?.Locations?.Count ?? 0;

        for (int i = 0; i < locationButtons.Length; i++)
        {
            LocationButtonUI button =
                locationButtons[i];

            if (button == null)
                continue;

            LocationData location =
                i < locationCount
                    ? region.Locations[i]
                    : null;

            button.Setup(location);
        }

        if (locationCount > locationButtons.Length)
        {
            Debug.LogWarning(
                $"{region.RegionName} contains " +
                $"{locationCount} locations, but only " +
                $"{locationButtons.Length} UI slots exist."
            );
        }
    }
}