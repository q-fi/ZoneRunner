using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PresetSelectorUI : MonoBehaviour
{
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

        InventoryManager.Instance.OnCurrentPresetChanged += Refresh;
        Refresh();

        selectionListRoot.SetActive(false);
        renameInputRoot.SetActive(false);
    }

    private void OnDestroy()
    {
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.OnCurrentPresetChanged -= Refresh;
    }

    private void Refresh()
    {
        currentPresetLabel.text = InventoryManager.Instance.BackpackPresets.CurrentPreset.PresetName + " ▼";
    }

    private void ToggleSelectionList()
    {
        bool willShow = !selectionListRoot.activeSelf;
        selectionListRoot.SetActive(willShow);

        if (willShow)
            PopulateList();
    }

    private void PopulateList()
    {
        foreach (Transform child in selectionListContainer)
            Destroy(child.gameObject);

        var presets = InventoryManager.Instance.BackpackPresets.Presets;

        for (int i = 0; i < presets.Count; i++)
        {
            int index = i; // локальна копія для замикання, щоб кожна кнопка "запам'ятала" свій індекс
            GameObject itemObj = Instantiate(presetListItemPrefab, selectionListContainer);
            itemObj.GetComponentInChildren<TextMeshProUGUI>().text = presets[i].PresetName;
            itemObj.GetComponent<Button>().onClick.AddListener(() => SelectPreset(index));
        }
    }

    private void SelectPreset(int index)
    {
        InventoryManager.Instance.SelectPreset(index);
        selectionListRoot.SetActive(false);
    }

    private void OpenRename()
    {
        renameInputRoot.SetActive(true);
        renameInputField.text = InventoryManager.Instance.BackpackPresets.CurrentPreset.PresetName;
        renameInputField.onEndEdit.RemoveAllListeners();
        renameInputField.onEndEdit.AddListener(ConfirmRename);
    }

    private void ConfirmRename(string newName)
    {
        renameInputRoot.SetActive(false);

        if (string.IsNullOrWhiteSpace(newName))
            return;

        InventoryManager.Instance.RenameCurrentPreset(newName);
    }
}