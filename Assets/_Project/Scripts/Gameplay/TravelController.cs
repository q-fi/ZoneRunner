using UnityEngine;
using TMPro;

public class TravelController : MonoBehaviour
{
    [SerializeField] private GameObject regionSelectView;
    [SerializeField] private GameObject travelingView;
    [SerializeField] private TMP_Text timerText;

    private void OnEnable()
    {
        TravelManager.Instance.OnTravelTick += UpdateTimerText;
        RefreshView();
    }

    private void OnDisable()
    {
        if (TravelManager.Instance != null)
            TravelManager.Instance.OnTravelTick -= UpdateTimerText;
    }

    public void OnRegionSelected(string regionName)
    {
        TravelManager.Instance.StartTravel(regionName);
        RefreshView();
    }

    private void RefreshView()
    {
        bool traveling = TravelManager.Instance.IsTraveling;
        regionSelectView.SetActive(!traveling);
        travelingView.SetActive(traveling);

        if (traveling)
            UpdateTimerText(TravelManager.Instance.TimeRemaining);
    }

    private void UpdateTimerText(float timeRemaining)
    {
        if (timerText != null)
            timerText.text = $"В дорозі... {Mathf.CeilToInt(timeRemaining)} сек";
    }
}