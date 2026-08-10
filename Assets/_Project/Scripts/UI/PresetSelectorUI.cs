using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PresetSelectorUI : MonoBehaviour
{
    [SerializeField] private bool equipmentPresetMode;

    [Header("Header")]
    [SerializeField] private TextMeshProUGUI currentPresetLabel;
    [SerializeField] private Button currentPresetButton;
    [SerializeField] private Button renameButton;

    [Header("Rename")]
    [SerializeField] private GameObject renameInputRoot;
    [SerializeField] private TMP_InputField renameInputField;

    [Header("Selection List")]
    [SerializeField] private GameObject selectionListRoot;
    [SerializeField] private Transform selectionListContainer;
    [SerializeField] private GameObject presetListItemPrefab; // Button + TextMeshProUGUI всередині

    private void Start()
    {
        currentPresetButton.onClick.AddListener(ToggleSelectionList);
        renameButton.onClick.AddListener(OpenRename);

        if (equipmentPresetMode)
        {
            InventoryManager.Instance.OnCurrentEquipmentPresetChanged += Refresh;
        }
        else
        {
            InventoryManager.Instance.OnCurrentPresetChanged += Refresh;
        }
        Refresh();

        selectionListRoot.SetActive(false);
        renameInputRoot.SetActive(false);
    }

    private void OnDestroy()
    {
       if (InventoryManager.Instance != null)
        {
            if (equipmentPresetMode)
            {
                InventoryManager.Instance.OnCurrentEquipmentPresetChanged -= Refresh;
            }
            else
            {
                InventoryManager.Instance.OnCurrentPresetChanged -= Refresh;
            }
        }
    }

    private void Refresh()
    {
        if (equipmentPresetMode)
        {
            currentPresetLabel.text =
                InventoryManager.Instance
                    .EquipmentPresets
                    .CurrentPreset
                    .PresetName + " ▼";
        }
        else
        {
            currentPresetLabel.text =
                InventoryManager.Instance
                    .BackpackPresets
                    .CurrentPreset
                    .PresetName + " ▼";
        }
    }

    private void ToggleSelectionList()
    {
        bool willShow = !selectionListRoot.activeSelf;
        selectionListRoot.SetActive(willShow);

        if (willShow)
            selectionListRoot.transform.SetAsLastSibling();
            PopulateList();
    }

    private void PopulateList()
    {
        foreach (Transform child in selectionListContainer)
            Destroy(child.gameObject);

        if (equipmentPresetMode)
        {
            var presets =
                InventoryManager.Instance.EquipmentPresets.Presets;

            for (int i = 0; i < presets.Count; i++)
            {
                int index = i;

                GameObject itemObj =
                    Instantiate(
                        presetListItemPrefab,
                        selectionListContainer
                    );

                itemObj
                    .GetComponentInChildren<TextMeshProUGUI>()
                    .text = presets[i].PresetName;

                itemObj
                    .GetComponent<Button>()
                    .onClick
                    .AddListener(
                        () => SelectPreset(index)
                    );
            }
        }
        else
        {
            var presets =
                InventoryManager.Instance.BackpackPresets.Presets;

            for (int i = 0; i < presets.Count; i++)
            {
                int index = i;

                GameObject itemObj =
                    Instantiate(
                        presetListItemPrefab,
                        selectionListContainer
                    );

                itemObj
                    .GetComponentInChildren<TextMeshProUGUI>()
                    .text = presets[i].PresetName;

                itemObj
                    .GetComponent<Button>()
                    .onClick
                    .AddListener(
                        () => SelectPreset(index)
                    );
            }
        }
    }


    private void SelectPreset(int index)
    {
        if (equipmentPresetMode)
        {
            InventoryManager.Instance.SelectEquipmentPreset(index);
        }
        else
        {
            InventoryManager.Instance.SelectPreset(index);
        }

        selectionListRoot.SetActive(false);
    }

    private void OpenRename()
    {
        renameInputRoot.SetActive(true);

        if (equipmentPresetMode)
        {
            renameInputField.text =
                InventoryManager.Instance
                    .EquipmentPresets
                    .CurrentPreset
                    .PresetName;
        }
        else
        {
            renameInputField.text =
                InventoryManager.Instance
                    .BackpackPresets
                    .CurrentPreset
                    .PresetName;
        }

        renameInputField.onEndEdit.RemoveAllListeners();
        renameInputField.onEndEdit.AddListener(ConfirmRename);
    }

    private void ConfirmRename(string newName)
    {
        renameInputRoot.SetActive(false);

        if (string.IsNullOrWhiteSpace(newName))
            return;

        if (equipmentPresetMode)
        {
            InventoryManager.Instance
                .RenameCurrentEquipmentPreset(newName);
        }
        else
        {
            InventoryManager.Instance
                .RenameCurrentPreset(newName);
        }
    }
}