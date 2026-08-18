using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "Region_01",
    menuName = "ZoneRunner/Map/Region Data"
)]
public class RegionData : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string regionId = "region_01";
    [SerializeField] private string regionName = "Region_01";

    [Header("Description")]
    [TextArea(2, 4)]
    [SerializeField] private string shortDescription;

    [TextArea(4, 10)]
    [SerializeField] private string fullDescription;

    [Header("Danger")]
    [Range(1, 10)]
    [SerializeField] private int threatLevel = 1;

    [Range(0f, 2f)]
    [SerializeField] private float ambushChanceMultiplier = 1f;

    [SerializeField] private List<string> possibleThreats = new();
    [SerializeField] private List<string> possibleEvents = new();
    [SerializeField] private List<string> possiblePhenomena = new();
    [SerializeField] private List<string> inhabitants = new();

    [Header("Dynamic Content Ranges")]
    [SerializeField] private Vector2Int lootAmountRange =
        new Vector2Int(3, 8);

    [SerializeField] private Vector2Int stashAmountRange =
        new Vector2Int(0, 3);

    [SerializeField] private Vector2Int artifactAmountRange =
        new Vector2Int(0, 2);

    [SerializeField] private bool legendaryArtifactPossible;

    [Header("Refresh")]
    [Min(1)]
    [SerializeField] private int refreshIntervalMinutes = 60;

    [Header("Locations")]
    [SerializeField] private List<LocationData> locations = new();

    [Header("Map View")]
    [SerializeField] private GameObject regionMapPrefab;

    public GameObject RegionMapPrefab => regionMapPrefab;

    public string RegionId => regionId;
    public string RegionName => regionName;

    public string ShortDescription => shortDescription;
    public string FullDescription => fullDescription;

    public int ThreatLevel => threatLevel;
    public float AmbushChanceMultiplier => ambushChanceMultiplier;

    public IReadOnlyList<string> PossibleThreats =>
        possibleThreats;

    public IReadOnlyList<string> PossibleEvents =>
        possibleEvents;

    public IReadOnlyList<string> PossiblePhenomena =>
        possiblePhenomena;

    public IReadOnlyList<string> Inhabitants =>
        inhabitants;

    public Vector2Int LootAmountRange =>
        lootAmountRange;

    public Vector2Int StashAmountRange =>
        stashAmountRange;

    public Vector2Int ArtifactAmountRange =>
        artifactAmountRange;

    public bool LegendaryArtifactPossible =>
        legendaryArtifactPossible;

    public int RefreshIntervalMinutes =>
        refreshIntervalMinutes;

    public IReadOnlyList<LocationData> Locations =>
        locations;

    private void OnValidate()
    {
        threatLevel = Mathf.Clamp(threatLevel, 1, 10);
        ambushChanceMultiplier =
            Mathf.Clamp(ambushChanceMultiplier, 0f, 2f);
        refreshIntervalMinutes =
            Mathf.Max(1, refreshIntervalMinutes);

        lootAmountRange =
            NormalizeRange(lootAmountRange);

        stashAmountRange =
            NormalizeRange(stashAmountRange);

        artifactAmountRange =
            NormalizeRange(artifactAmountRange);
    }

    private Vector2Int NormalizeRange(Vector2Int range)
    {
        int minimum = Mathf.Max(0, range.x);
        int maximum = Mathf.Max(minimum, range.y);

        return new Vector2Int(minimum, maximum);
    }
}
