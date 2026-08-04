using UnityEngine;
using UnityEngine.UI;

public enum EquipmentSlotType
{
    PrimaryWeapon,
    SecondaryWeapon,
    Armor,
    Detector
}

public class EquipmentSlotUI : MonoBehaviour
{
    [SerializeField] private EquipmentSlotType slotType;
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
        ItemData equippedItem = slotType switch
        {
            EquipmentSlotType.PrimaryWeapon => InventoryManager.Instance.EquippedPrimaryWeapon,
            EquipmentSlotType.SecondaryWeapon => InventoryManager.Instance.EquippedSecondaryWeapon,
            EquipmentSlotType.Armor => InventoryManager.Instance.EquippedArmor,
            EquipmentSlotType.Detector => InventoryManager.Instance.EquippedDetector,
            _ => null
        };

        if (equippedItem != null && equippedItem.icon != null)
        {
            iconImage.sprite = equippedItem.icon;
            iconImage.color = Color.white;
        }
        else
        {
            iconImage.sprite = emptySlotSprite;
            iconImage.color = new Color(1f, 1f, 1f, 0.3f); // напівпрозорий, показує "порожньо"
        }
    }
}