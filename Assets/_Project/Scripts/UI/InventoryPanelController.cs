using System;
using UnityEngine;
using UnityEngine.UI;

public class InventoryPanelController : MonoBehaviour
{
    public static InventoryPanelController Instance { get; private set; }

    [Header("Panels")]
    [SerializeField] private GameObject equipmentPanel;
    [SerializeField] private GameObject backpackPresetPanel;
    [SerializeField] private GameObject equipmentPresetPanel;

    [Header("UI")]
    [SerializeField] private BackpackPresetGridUI presetGridUI;
    [SerializeField] private InventoryGridUI inventoryGridUI;
    [SerializeField] private ScrollRect scrollRect;

    public bool IsPresetPanelActive { get; private set; }

    public InventoryViewMode CurrentViewMode { get; private set; }

    public event Action<InventoryViewMode> OnViewModeChanged;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        ShowEquipmentPanel();
    }

    private void SetViewMode(InventoryViewMode mode)
    {
        CurrentViewMode = mode;
        OnViewModeChanged?.Invoke(mode);
    }

    public void ShowEquipmentPanel()
    {
        equipmentPanel.SetActive(true);
        backpackPresetPanel.SetActive(false);

        if (equipmentPresetPanel != null)
            equipmentPresetPanel.SetActive(false);

        IsPresetPanelActive = false;

        SetViewMode(InventoryViewMode.Inventory);

        Refresh(equipmentPanel.GetComponent<RectTransform>());
    }

    public void ShowBackpackPresets()
    {
        equipmentPanel.SetActive(false);
        backpackPresetPanel.SetActive(true);

        if (equipmentPresetPanel != null)
            equipmentPresetPanel.SetActive(false);

        IsPresetPanelActive = true;

        SetViewMode(InventoryViewMode.BackpackPreset);

        presetGridUI.SetPreset(
            InventoryManager.Instance.BackpackPresets.CurrentPreset
        );

        Refresh(backpackPresetPanel.GetComponent<RectTransform>());
    }

    public void ShowEquipmentPresets()
    {
        if (equipmentPresetPanel == null)
        {
            Debug.LogWarning(
                "Equipment Preset Panel ще не призначений в InventoryPanelController."
            );

            return;
        }

        equipmentPanel.SetActive(false);
        backpackPresetPanel.SetActive(false);
        equipmentPresetPanel.SetActive(true);

        IsPresetPanelActive = true;

        SetViewMode(InventoryViewMode.EquipmentPreset);

        Refresh(
            equipmentPresetPanel.GetComponent<RectTransform>()
        );
    }

    private void Refresh(RectTransform panel)
    {
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(panel);

        inventoryGridUI.RecalculateOffset(panel);

        scrollRect.verticalNormalizedPosition = 1f;
    }
}