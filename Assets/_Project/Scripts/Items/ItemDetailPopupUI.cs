using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ItemDetailPopupUI : MonoBehaviour
{
    public static ItemDetailPopupUI Instance { get; private set; }

    [Header("Root")]
    [SerializeField] private GameObject popupRoot;

    [Header("Content")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI statsText;

    [Header("Buttons")]
    [SerializeField] private Button equipButton;
    [SerializeField] private Button useButton;
    [SerializeField] private Button discardButton;
    [SerializeField] private Button closeButton;

    private ItemInstance currentItem;

    private void Awake()
    {
        Instance = this;
        popupRoot.SetActive(false);

        closeButton.onClick.AddListener(Close);
        equipButton.onClick.AddListener(OnEquipClicked);
        useButton.onClick.AddListener(OnUseClicked);
        discardButton.onClick.AddListener(OnDiscardClicked);
    }

    public void Show(ItemInstance instance)
    {
        currentItem = instance;
        var data = instance.Data;

        iconImage.sprite = data.icon;
        nameText.text = data.isStackable && instance.StackCount > 1
            ? $"{data.itemName} x{instance.StackCount}"
            : data.itemName;
        descriptionText.text = data.description;
        statsText.text = BuildStatsText(data);

        bool canEquip = data.EquipCategory != null;
        equipButton.gameObject.SetActive(canEquip);

        bool canUse = data.itemType == ItemType.Consumable;
        useButton.gameObject.SetActive(canUse);

        popupRoot.SetActive(true);
    }

    public void Close()
    {
        currentItem = null;
        popupRoot.SetActive(false);
    }

    private string BuildStatsText(ItemData data)
    {
        switch (data)
        {
            case WeaponData w:
                return $"Урон: {w.damage}\nДальність: {w.range}\nМагазин: {w.magazineSize}";
            case ArmorData a:
                return $"Захист: {a.defense}\nРадіостійкість: {a.radiationResistance:P0}";
            case DetectorData d:
                return $"Радіус виявлення: {d.detectionRadius}\nПоказує тип артефакту: {(d.showsArtifactType ? "Так" : "Ні")}";
            case ConsumableData c:
                return $"Ефект: {c.effect} ({c.effectValue})";
            case ArtifactData art:
                return $"Рідкість: {art.rarity}\nРадіація: {art.radiationLevel}\nЦіна: {art.basePrice}";
            case AmmoData ammo:
                return $"Модифікатор урону: {ammo.damageModifier}";
            default:
                return string.Empty;
        }
    }

    private void OnEquipClicked()
    {
        if (currentItem == null) return;

        if (InventoryManager.Instance.TryEquipItem(currentItem))
        {
            InventoryManager.Instance.RemoveItem(currentItem);
        }

        Close();
    }

    private void OnUseClicked()
    {
        if (currentItem == null) return;

        InventoryManager.Instance.UseItem(currentItem);
        Close();
    }

    private void OnDiscardClicked()
    {
        if (currentItem == null) return;

        InventoryManager.Instance.DiscardItem(currentItem);
        Close();
    }
}