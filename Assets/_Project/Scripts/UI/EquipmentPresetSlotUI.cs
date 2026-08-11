using UnityEngine;
using UnityEngine.UI;

public class EquipmentPresetSlotUI : MonoBehaviour
{
    [SerializeField] private SlotCategory category;
    [SerializeField] private int slotIndex;

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

    public void Refresh()
    {
        if (InventoryManager.Instance == null)
            return;

        var preset =
            InventoryManager.Instance.EquipmentPresets.CurrentPreset;

        if (preset == null)
        {
            ShowEmpty();
            return;
        }

        PresetItem presetItem =
            preset.GetSlot(category, slotIndex);

        if (presetItem != null &&
            presetItem.Data != null &&
            presetItem.Data.icon != null)
        {
            iconImage.sprite = presetItem.Data.icon;
            iconImage.color = Color.white;
        }
        else
        {
            ShowEmpty();
        }
    }

    private void ShowEmpty()
    {
        iconImage.sprite = emptySlotSprite;
        iconImage.color = new Color(1f, 1f, 1f, 0.3f);
    }

    public void RemoveFromPreset()
    {
        if (InventoryManager.Instance == null)
            return;

        InventoryManager.Instance.RemoveItemFromEquipmentPreset(
            category,
            slotIndex
        );
    }
}