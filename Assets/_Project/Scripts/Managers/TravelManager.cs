using System;
using System.Collections;
using UnityEngine;

public class TravelManager : MonoBehaviour
{
    public static TravelManager Instance { get; private set; }

    public bool IsTraveling { get; private set; }
    public float TimeRemaining { get; private set; }
    public string CurrentRegion { get; private set; }

    public LocationData CurrentLocation { get; private set; }
    public int SelectedEquipmentPresetIndex { get; private set; } = -1;
    public int SelectedBackpackPresetIndex { get; private set; } = -1;

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

    public void StartTravel(string destinationName)
    {
        CurrentLocation = null;
        SelectedEquipmentPresetIndex = -1;
        SelectedBackpackPresetIndex = -1;

        StartTravelInternal(destinationName, travelDuration);
    }

    public void StartTravel(
        LocationData locationData,
        int equipmentPresetIndex,
        int backpackPresetIndex
    )
    {
        if (locationData == null)
            return;

        CurrentLocation = locationData;
        SelectedEquipmentPresetIndex = equipmentPresetIndex;
        SelectedBackpackPresetIndex = backpackPresetIndex;

        StartTravelInternal(
            locationData.displayName,
            locationData.travelDurationSeconds
        );
    }

    private void StartTravelInternal(
        string destinationName,
        float duration
    )
    {
        if (IsTraveling)
            return;

        CurrentRegion = destinationName;

        StartCoroutine(
            TravelRoutine(Mathf.Max(1f, duration))
        );
    }

    private IEnumerator TravelRoutine(float duration)
    {
        IsTraveling = true;
        TimeRemaining = duration;

        OnTravelStarted?.Invoke();

        while (TimeRemaining > 0f)
        {
            yield return null;

            TimeRemaining -= Time.deltaTime;

            OnTravelTick?.Invoke(
                Mathf.Max(TimeRemaining, 0f)
            );
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