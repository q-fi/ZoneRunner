using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class RegionButtonUI : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private RegionData regionData;

    [Header("References")]
    [SerializeField] private MapPanelController mapPanelController;
    [SerializeField] private TMP_Text regionNameText;

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();

        if (mapPanelController == null)
        {
            mapPanelController =
                GetComponentInParent<MapPanelController>();
        }

        button.onClick.AddListener(OpenRegion);
        RefreshText();
    }

    private void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(OpenRegion);
    }

    private void OpenRegion()
    {
        if (regionData == null)
        {
            Debug.LogError(
                $"{name}: RegionData is not assigned."
            );

            return;
        }

        if (mapPanelController == null)
        {
            Debug.LogError(
                $"{name}: MapPanelController is not assigned."
            );

            return;
        }

        mapPanelController.OpenRegion(regionData);
    }

    private void RefreshText()
    {
        if (regionNameText == null ||
            regionData == null)
        {
            return;
        }

        regionNameText.text = regionData.RegionName;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        RefreshText();
    }
#endif
}