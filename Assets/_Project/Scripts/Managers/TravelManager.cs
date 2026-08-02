using System;
using System.Collections;
using UnityEngine;

public class TravelManager : MonoBehaviour
{
    public static TravelManager Instance { get; private set; }

    public bool IsTraveling { get; private set; }
    public float TimeRemaining { get; private set; }
    public string CurrentRegion { get; private set; }

    public event Action OnTravelStarted;
    public event Action<float> OnTravelTick;
    public event Action OnTravelEnded;

    [SerializeField] private float travelDuration = 10f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void StartTravel(string regionName)
    {
        if (IsTraveling) return;

        CurrentRegion = regionName;
        StartCoroutine(TravelRoutine());
    }

    private IEnumerator TravelRoutine()
    {
        IsTraveling = true;
        TimeRemaining = travelDuration;
        OnTravelStarted?.Invoke();

        while (TimeRemaining > 0f)
        {
            yield return null;
            TimeRemaining -= Time.deltaTime;
            OnTravelTick?.Invoke(Mathf.Max(TimeRemaining, 0f));
        }

        IsTraveling = false;
        OnTravelEnded?.Invoke();
        ResolveArrival();
    }

    private void ResolveArrival()
    {
        bool randomEvent = UnityEngine.Random.value < 0.5f;

        if (randomEvent)
        {
            Debug.Log("Випадкова подія: бій!");
            GameManager.Instance.ChangeState(GameState.Battle);
        }
        else
        {
            Debug.Log("Подорож без пригод. Прибуття на локацію.");
            GameManager.Instance.ChangeState(GameState.Search);
        }
    }
}