using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ItemIconUI : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private Image iconImage;

    private ItemInstance item;

    public void Setup(ItemInstance itemInstance)
    {
        item = itemInstance;
        if (iconImage != null && itemInstance.Data.icon != null)
        {
            iconImage.sprite = itemInstance.Data.icon;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        ItemDetailPopupUI.Instance.Show(item);
    }
}