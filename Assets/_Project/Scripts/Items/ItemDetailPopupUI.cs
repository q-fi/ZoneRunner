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
    [SerializeField] private Button saveToPresetButton;
    [SerializeField] private Button removeFromPresetButton;
    [SerializeField] private Button closeButton;

    private ItemInstance currentItem;
    private ItemContext currentContext;

    private void Awake()
    {
        Instance = this;
        popupRoot.SetActive(false);

        closeButton.onClick.AddListener(Close);
        equipButton.onClick.AddListener(OnEquipClicked);
        useButton.onClick.AddListener(OnUseClicked);
        discardButton.onClick.AddListener(OnDiscardClicked);
        saveToPresetButton.onClick.AddListener(OnSaveToPresetClicked);
        removeFromPresetButton.onClick.AddListener(OnRemoveFromPresetClicked);
    }

    public void Show(ItemInstance instance, ItemContext context = ItemContext.Inventory)
{
    currentItem = instance;
    currentContext = context;
    var data = instance.Data;

    iconImage.sprite = data.icon;
    nameText.text = data.isStackable && instance.StackCount > 1
        ? $"{data.itemName} x{instance.StackCount}"
        : data.itemName;
    descriptionText.text = data.description;
    statsText.text = BuildStatsText(data);

    bool presetModeActive = InventoryPanelController.Instance != null
                          && InventoryPanelController.Instance.IsPresetPanelActive;

    if (context == ItemContext.Preset)
    {
        // Клік по предмету всередині сітки пресета — тільки "Прибрати з пресета"
        equipButton.gameObject.SetActive(false);
        useButton.gameObject.SetActive(false);
        discardButton.gameObject.SetActive(false);
        saveToPresetButton.gameObject.SetActive(false);
        removeFromPresetButton.gameObject.SetActive(true);
    }
    else if (presetModeActive)
    {
        // Клік по реальному інвентарю, поки відкрита панель пресетів — тільки "Зберегти в пресет"
        equipButton.gameObject.SetActive(false);
        useButton.gameObject.SetActive(false);
        discardButton.gameObject.SetActive(false);
        saveToPresetButton.gameObject.SetActive(true);
        removeFromPresetButton.gameObject.SetActive(false);
    }
    else
    {
        // Звичайний режим екіпіровки — стандартні кнопки
        equipButton.gameObject.SetActive(data.EquipCategory != null);
        useButton.gameObject.SetActive(data.itemType == ItemType.Consumable);
        discardButton.gameObject.SetActive(true);
        saveToPresetButton.gameObject.SetActive(false);
        removeFromPresetButton.gameObject.SetActive(false);
    }

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

    private void OnSaveToPresetClicked()
    {
        if (currentItem == null) return;

        InventoryManager.Instance.SaveItemToPreset(currentItem);
        Close();
    }

    private void OnRemoveFromPresetClicked()
    {
        if (currentItem == null) return;

        InventoryManager.Instance.RemoveItemFromPreset(currentItem);
        Close();
    }
}