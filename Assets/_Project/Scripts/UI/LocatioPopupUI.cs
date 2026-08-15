using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LocationPopupUI : MonoBehaviour
{
    [Header("Popup")]
    [SerializeField] private GameObject locationDetailsPopup;
    [SerializeField] private Button closeLocationPopupButton;

    [Header("Text References")]
    [SerializeField] private TMP_Text locationTitle;
    [SerializeField] private TMP_Text locationDescription;
    [SerializeField] private TMP_Text locationIntelText;
    [SerializeField] private TMP_Text locationLootText;
    [SerializeField] private TMP_Text travelTimeText;

    [Header("Preset Dropdowns")]
    [SerializeField] private TMP_Dropdown equipmentPresetDropdown;
    [SerializeField] private TMP_Dropdown backpackPresetDropdown;

    [SerializeField] private Button departButton;

    public LocationData SelectedLocation { get; private set; }

    public int SelectedEquipmentPresetIndex =>
        equipmentPresetDropdown != null
            ? equipmentPresetDropdown.value
            : -1;

    public int SelectedBackpackPresetIndex =>
        backpackPresetDropdown != null
            ? backpackPresetDropdown.value
            : -1;

    private void Awake()
    {
        closeLocationPopupButton.onClick.AddListener(ClosePopup);

        departButton.onClick.AddListener(Depart);
    }

    private void OnEnable()
    {
        ClosePopup();
    }

    public void OpenLocation(LocationData locationData)
    {
        if (locationData == null)
            return;

        SelectedLocation = locationData;

        locationTitle.text = locationData.displayName;
        locationDescription.text = locationData.description;

        locationIntelText.text =
            $"Threat Level: {locationData.threatLevel}\n" +
            $"Possible Enemies: {locationData.possibleEnemies}\n" +
            $"Hazards: {locationData.hazards}\n" +
            $"Possible Events: {locationData.possibleEvents}";

        locationLootText.text =
            $"Loot: {locationData.loot}\n" +
            $"Stashes: {locationData.stashes}\n" +
            $"Artifacts: {locationData.artifacts}\n" +
            $"Legendary Artifact: " +
            $"{(locationData.legendaryArtifactPossible ? "?" : "—")}";

        travelTimeText.text =
            $"Travel Time: {FormatTravelTime(locationData.travelDurationSeconds)}";

        PopulatePresetDropdowns();
        locationDetailsPopup.SetActive(true);
    }

    private void Depart()
    {
        if (SelectedLocation == null || TravelManager.Instance == null)
            return;

        TravelManager.Instance.StartTravel(
            SelectedLocation,
            SelectedEquipmentPresetIndex,
            SelectedBackpackPresetIndex
        );

        ClosePopup();
    }

    public void ClosePopup()
    {
        locationDetailsPopup.SetActive(false);
    }

    private void PopulatePresetDropdowns()
    {
        if (InventoryManager.Instance == null)
            return;

        PopulateEquipmentDropdown();
        PopulateBackpackDropdown();
    }

    private void PopulateEquipmentDropdown()
    {
        equipmentPresetDropdown.ClearOptions();

        var presets = InventoryManager.Instance.EquipmentPresets.Presets;

        foreach (var preset in presets)
            equipmentPresetDropdown.options.Add(
                new TMP_Dropdown.OptionData(preset.PresetName)
            );

        int currentIndex = presets.IndexOf(
            InventoryManager.Instance.EquipmentPresets.CurrentPreset
        );

        equipmentPresetDropdown.SetValueWithoutNotify(
            Mathf.Max(0, currentIndex)
        );

        equipmentPresetDropdown.RefreshShownValue();
    }

    private void PopulateBackpackDropdown()
    {
        backpackPresetDropdown.ClearOptions();

        var presets = InventoryManager.Instance.BackpackPresets.Presets;

        foreach (var preset in presets)
            backpackPresetDropdown.options.Add(
                new TMP_Dropdown.OptionData(preset.PresetName)
            );

        int currentIndex = presets.IndexOf(
            InventoryManager.Instance.BackpackPresets.CurrentPreset
        );

        backpackPresetDropdown.SetValueWithoutNotify(
            Mathf.Max(0, currentIndex)
        );

        backpackPresetDropdown.RefreshShownValue();
    }

    private string FormatTravelTime(float seconds)
    {
        int totalSeconds = Mathf.CeilToInt(seconds);

        if (totalSeconds < 60)
            return $"{totalSeconds}s";

        int minutes = totalSeconds / 60;
        int remainingSeconds = totalSeconds % 60;

        return $"{minutes}m {remainingSeconds}s";
    }
}