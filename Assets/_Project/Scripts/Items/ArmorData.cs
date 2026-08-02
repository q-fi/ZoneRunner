using UnityEngine;

[CreateAssetMenu(fileName = "NewArmor", menuName = "ZoneRunner/Items/Armor")]
public class ArmorData : ItemData
{
    [Header("Armor Stats")]
    public int defense;
    [Range(0f, 1f)] public float radiationResistance;
    public float durability = 100f;
}