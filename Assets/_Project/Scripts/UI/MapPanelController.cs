using UnityEngine;

public class MapPanelController : MonoBehaviour
{
    [SerializeField] private GameObject worldMapView;
    [SerializeField] private GameObject regionMapView;

    private void OnEnable()
    {
        ShowWorldMap();
    }

    public void OpenRegion()
    {
        worldMapView.SetActive(false);
        regionMapView.SetActive(true);
    }

    public void ShowWorldMap()
    {
        worldMapView.SetActive(true);
        regionMapView.SetActive(false);
    }
}