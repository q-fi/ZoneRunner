using UnityEngine;

[CreateAssetMenu(fileName = "NewWeapon", menuName = "ZoneRunner/Items/Weapon")]
public class WeaponData : ItemData
{
    [Header("Weapon Stats")]
    public int damage;
    public float range;
    public int magazineSize;
    public float durability = 100f;
    public string ammoTypeId;
}