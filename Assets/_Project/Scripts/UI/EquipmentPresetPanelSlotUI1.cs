using UnityEngine;
using UnityEngine.UI;

public class EquipmentPresetPanelSlotUI : MonoBehaviour
{
    [Header("Slot")]
    [SerializeField] private SlotCategory category;
    [SerializeField] private int slotIndex;

    [Header("UI")]
    [SerializeField] private Image iconImage;
    [SerializeField] private Sprite emptySlotSprite;

    private void Start()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnEquipmentPresetChanged += Refresh;
            InventoryManager.Instance.OnCurrentEquipmentPresetChanged += Refresh;
        }

        Refresh();
    }

    private void OnDestroy()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnEquipmentPresetChanged -= Refresh;
            InventoryManager.Instance.OnCurrentEquipmentPresetChanged -= Refresh;
        }
    }

    private void Refresh()
    {
        if (InventoryManager.Instance == null)
            return;

        var presetCollection = InventoryManager.Instance.EquipmentPresets;

        if (presetCollection == null ||
            presetCollection.CurrentPreset == null)
        {
            SetEmpty();
            return;
        }

        var preset = presetCollection.CurrentPreset;

        PresetItem presetItem =
            preset.GetSlot(category, slotIndex);

        if (presetItem == null ||
            presetItem.Data == null ||
            presetItem.Data.icon == null)
        {
            SetEmpty();
            return;
        }

        iconImage.sprite = presetItem.Data.icon;
        iconImage.color = Color.white;
    }

    private void SetEmpty()
    {
        if (emptySlotSprite != null)
            iconImage.sprite = emptySlotSprite;

        iconImage.color = new Color(1f, 1f, 1f, 0.3f);
    }
}