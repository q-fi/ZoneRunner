using UnityEngine;
using UnityEngine.UI;

public class InventoryPanelController : MonoBehaviour
{
    public static InventoryPanelController Instance { get; private set; }

    [Header("Panels")]
    [SerializeField] private GameObject equipmentPanel;
    [SerializeField] private GameObject backpackPresetPanel;

    [SerializeField] private BackpackPresetGridUI presetGridUI;
    [SerializeField] private InventoryGridUI inventoryGridUI;

    [SerializeField] private ScrollRect scrollRect;

    public bool IsPresetPanelActive { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        ShowEquipmentPanel();
    }

    public void ShowEquipmentPanel()
    {
        equipmentPanel.SetActive(true);
        backpackPresetPanel.SetActive(false);

        IsPresetPanelActive = false;

        Refresh(equipmentPanel.GetComponent<RectTransform>());
    }

    public void ShowBackpackPresets()
    {
        equipmentPanel.SetActive(false);
        backpackPresetPanel.SetActive(true);

        IsPresetPanelActive = true;

        presetGridUI.SetPreset(
            InventoryManager.Instance.BackpackPresets.CurrentPreset);

        Refresh(backpackPresetPanel.GetComponent<RectTransform>());
    }

    private void Refresh(RectTransform panel)
    {
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(panel);

        inventoryGridUI.RecalculateOffset(panel);

        scrollRect.verticalNormalizedPosition = 1f;
    }
}