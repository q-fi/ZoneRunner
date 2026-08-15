using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class LocationButtonUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private LocationPopupUI locationPopup;
    [SerializeField] private TMP_Text locationNameText;

    [Header("Current Data")]
    [SerializeField] private LocationData locationData;

    private Button locationButton;

    public LocationData LocationData => locationData;

    private void Awake()
    {
        locationButton = GetComponent<Button>();

        if (locationNameText == null)
            locationNameText = GetComponentInChildren<TMP_Text>();

        if (locationPopup == null)
        {
            MapPanelController mapPanelController =
                GetComponentInParent<MapPanelController>();

            if (mapPanelController != null)
            {
                locationPopup =
                    mapPanelController.GetComponentInChildren<LocationPopupUI>(
                        true
                    );
            }
        }

        locationButton.onClick.AddListener(OpenLocation);
        RefreshView();
    }

    private void OnDestroy()
    {
        if (locationButton != null)
            locationButton.onClick.RemoveListener(OpenLocation);
    }

    public void Setup(LocationData data)
    {
        locationData = data;

        bool hasLocation = locationData != null;
        gameObject.SetActive(hasLocation);

        if (hasLocation)
            RefreshView();
    }

    private void RefreshView()
    {
        if (locationData == null)
            return;

        if (locationNameText != null)
            locationNameText.text = locationData.displayName;

    }

    private void OpenLocation()
    {
        if (locationPopup == null)
        {
            Debug.LogError($"{name}: LocationPopupUI is not assigned.");
            return;
        }

        if (locationData == null)
        {
            Debug.LogError($"{name}: LocationData is not assigned.");
            return;
        }

        locationPopup.OpenLocation(locationData);
    }
}