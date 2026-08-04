using UnityEngine;
using UnityEngine.UI;

public class ItemIconUI : MonoBehaviour
{
    [SerializeField] private Image iconImage;

    private ItemData item;

    public void Setup(ItemData itemData)
    {
        item = itemData;
        if (iconImage != null && itemData.icon != null)
        {
            iconImage.sprite = itemData.icon;
        }
    }

    public void OnClicked()
    {
        Debug.Log($"Клікнуто на предмет: {item.itemName}");
        // Наступного разу тут відкриємо попап деталей предмета
    }
}