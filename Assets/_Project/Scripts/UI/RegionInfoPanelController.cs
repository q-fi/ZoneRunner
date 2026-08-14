using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RegionInfoPanelController : MonoBehaviour
{
    [SerializeField] private GameObject regionInfoPanel;
    [SerializeField] private Button toggleButton;
    [SerializeField] private TMP_Text toggleText;

    private bool isExpanded;

    private void Awake()
    {
        toggleButton.onClick.AddListener(Toggle);
        SetExpanded(false);
    }

    private void Toggle()
    {
        SetExpanded(!isExpanded);
    }

    private void SetExpanded(bool expanded)
    {
        isExpanded = expanded;
        regionInfoPanel.SetActive(expanded);

        toggleText.text = expanded
            ? "REGION INFO ▲"
            : "REGION INFO ▼";
    }
}