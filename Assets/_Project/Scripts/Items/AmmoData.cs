using UnityEngine;

[CreateAssetMenu(fileName = "NewAmmo", menuName = "ZoneRunner/Items/Ammo")]
public class AmmoData : ItemData
{
    [Header("Ammo Info")]
    public string ammoTypeId;
    public int damageModifier;
}