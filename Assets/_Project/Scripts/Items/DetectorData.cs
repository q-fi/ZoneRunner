using UnityEngine;

[CreateAssetMenu(fileName = "NewDetector", menuName = "ZoneRunner/Items/Detector")]
public class DetectorData : ItemData
{
    [Header("Detector Stats")]
    public float detectionRadius;
    public bool showsArtifactType;

    public override SlotCategory? EquipCategory => SlotCategory.Detector;
}