using UnityEngine;

public class CampPanelUIController : MonoBehaviour
{
    [SerializeField] private GameObject actionButtons;
    [SerializeField] private GameObject[] campPanels;

    private GameObject currentPanel;

    private void Awake()
    {
        actionButtons.SetActive(true);

        foreach (GameObject panel in campPanels)
        {
            if (panel != null)
                panel.SetActive(false);
        }
    }

    public void OpenPanel(GameObject panel)
    {
        if (panel == null)
            return;

        foreach (GameObject campPanel in campPanels)
        {
            if (campPanel != null)
                campPanel.SetActive(campPanel == panel);
        }

        currentPanel = panel;
        actionButtons.SetActive(false);
    }

    public void CloseCurrentPanel()
    {
        if (currentPanel != null)
            currentPanel.SetActive(false);

        currentPanel = null;
        actionButtons.SetActive(true);
    }
}