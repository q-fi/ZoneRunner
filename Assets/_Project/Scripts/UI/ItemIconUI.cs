using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ItemIconUI : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private Image iconImage;

    private ItemInstance item;
    private ItemContext context;

    public void Setup(ItemInstance itemInstance, ItemContext itemContext = ItemContext.Inventory)
    {
        item = itemInstance;
        context = itemContext;
        if (iconImage != null && itemInstance.Data.icon != null)
        {
            iconImage.sprite = itemInstance.Data.icon;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        ItemDetailPopupUI.Instance.Show(item, context);
    }
}