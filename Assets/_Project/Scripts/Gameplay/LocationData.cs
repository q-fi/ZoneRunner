using UnityEngine;

[CreateAssetMenu(
    fileName = "LocationData",
    menuName = "ZoneRunner/Map/Location Data"
)]
public class LocationData : ScriptableObject
{
    [Header("Identity")]
    public string locationId;
    public string displayName = "LOCATION";

    [TextArea(3, 6)]
    public string description;

    [Header("Intel")]
    public string threatLevel = "—";
    public string possibleEnemies = "—";
    public string hazards = "—";
    public string possibleEvents = "—";

    [Header("Rewards")]
    public string loot = "—";
    public string stashes = "—";
    public string artifacts = "—";
    public bool legendaryArtifactPossible;

    [Header("Travel")]
    [Min(1f)]
    public float travelDurationSeconds = 10f;
    
}