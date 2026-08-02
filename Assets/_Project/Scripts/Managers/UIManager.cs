using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("State Panels")]
    [SerializeField] private GameObject campPanel;
    [SerializeField] private GameObject mapPanel;
    [SerializeField] private GameObject battlePanel;
    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] private GameObject searchPanel;
    [SerializeField] private GameObject leaderboardPanel;
    [SerializeField] private GameObject pausePanel;

    [Header("Overlays (not tied to GameState)")]
    [SerializeField] private GameObject rewardPanel;
    [SerializeField] private GameObject settingsPanel;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        GameManager.Instance.OnStateChanged += HandleStateChanged;

        rewardPanel.SetActive(false);
        settingsPanel.SetActive(false);
        HandleStateChanged(GameManager.Instance.CurrentState, GameManager.Instance.CurrentState);
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnStateChanged -= HandleStateChanged;
    }

    private void HandleStateChanged(GameState previous, GameState next)
    {
        campPanel.SetActive(next == GameState.Camp);
        mapPanel.SetActive(next == GameState.Travel);
        battlePanel.SetActive(next == GameState.Battle);
        inventoryPanel.SetActive(next == GameState.Inventory);
        searchPanel.SetActive(next == GameState.Search);
        leaderboardPanel.SetActive(next == GameState.Leaderboard);
        pausePanel.SetActive(next == GameState.Pause);
    }

    // ---------- Overlays ----------

    public void ShowReward() => rewardPanel.SetActive(true);
    public void HideReward() => rewardPanel.SetActive(false);

    public void OpenSettings() => settingsPanel.SetActive(true);
    public void CloseSettings() => settingsPanel.SetActive(false);
    public void ToggleSettings() => settingsPanel.SetActive(!settingsPanel.activeSelf);
}