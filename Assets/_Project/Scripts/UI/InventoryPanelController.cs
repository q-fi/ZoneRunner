using UnityEngine;

public class InventoryPanelController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject equipmentPanel;
    [SerializeField] private GameObject backpackPresetPanel;

    private void Start()
    {
        ShowEquipmentPanel();
    }

    public void ShowEquipmentPanel()
    {
        equipmentPanel.SetActive(true);
        backpackPresetPanel.SetActive(false);
    }

    public void ShowBackpackPresets()
    {
        equipmentPanel.SetActive(false);
        backpackPresetPanel.SetActive(true);
    }
}