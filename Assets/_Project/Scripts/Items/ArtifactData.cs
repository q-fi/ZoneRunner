using UnityEngine;

public enum Rarity { Common, Uncommon, Rare, Legendary }

[CreateAssetMenu(fileName = "NewArtifact", menuName = "ZoneRunner/Items/Artifact")]
public class ArtifactData : ItemData
{
    [Header("Artifact Stats")]
    public Rarity rarity;
    public float radiationLevel;
    public int basePrice;

    public override SlotCategory? EquipCategory => SlotCategory.Artifact;
}