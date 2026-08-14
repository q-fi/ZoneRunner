using UnityEngine;
using UnityEngine.UI;

public class EquipmentSlotUI : MonoBehaviour
{
    [SerializeField] private SlotCategory category;
    [SerializeField] private int slotIndex; 
    [SerializeField] private Image iconImage;
    [SerializeField] private Sprite emptySlotSprite;

    private void Start()
    {
        InventoryManager.Instance.OnEquipmentChanged += Refresh;
        Refresh();
    }

    private void OnDestroy()
    {
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.OnEquipmentChanged -= Refresh;
    }

    private void Refresh()
{
    var instance = InventoryManager.Instance.GetEquipped(category, slotIndex);

    if (instance != null && instance.Data.icon != null)
    {
        iconImage.sprite = instance.Data.icon;
        iconImage.color = Color.white;
    }
    else
    {
        iconImage.sprite = emptySlotSprite;
        iconImage.color = new Color(1f, 1f, 1f, 0.3f);
    }
}
}