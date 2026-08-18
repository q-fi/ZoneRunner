using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class LocationEncounterEntry
{
    public EncounterData encounter;

    [Min(0f)]
    public float weight = 1f;
}

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

    [Header("Battle Encounters")]
    [Range(0f, 100f)]
    public float outboundAmbushChancePercent = 35f;

    [Range(0f, 100f)]
    public float returnAmbushChancePercent = 5f;

    [SerializeField]
    private List<LocationEncounterEntry> possibleBattleEncounters = new();

    public IReadOnlyList<LocationEncounterEntry> PossibleBattleEncounters =>
        possibleBattleEncounters;
}
