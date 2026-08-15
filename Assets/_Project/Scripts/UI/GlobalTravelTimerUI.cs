using TMPro;
using UnityEngine;

public class GlobalTravelTimerUI : MonoBehaviour
{
    [SerializeField] private TMP_Text timerText;

    private void OnEnable()
    {
        Subscribe();
        Refresh();
    }

    private void Start()
    {
        Subscribe();
        Refresh();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void Subscribe()
    {
        if (TravelManager.Instance == null)
            return;

        TravelManager.Instance.OnTravelStarted -= Refresh;
        TravelManager.Instance.OnTravelStarted += Refresh;

        TravelManager.Instance.OnTravelTick -= UpdateTimer;
        TravelManager.Instance.OnTravelTick += UpdateTimer;

        TravelManager.Instance.OnTravelEnded -= Refresh;
        TravelManager.Instance.OnTravelEnded += Refresh;
    }

    private void Unsubscribe()
    {
        if (TravelManager.Instance == null)
            return;

        TravelManager.Instance.OnTravelStarted -= Refresh;
        TravelManager.Instance.OnTravelTick -= UpdateTimer;
        TravelManager.Instance.OnTravelEnded -= Refresh;
    }

    private void Refresh()
    {
        if (TravelManager.Instance == null ||
            !TravelManager.Instance.IsTraveling)
        {
            timerText.gameObject.SetActive(false);
            return;
        }

        UpdateTimer(TravelManager.Instance.TimeRemaining);
    }

    private void UpdateTimer(float timeRemaining)
    {
        timerText.gameObject.SetActive(true);

        timerText.text =
            $"{TravelManager.Instance.CurrentRegion}: " +
            $"{Mathf.CeilToInt(timeRemaining)}s";
    }
}