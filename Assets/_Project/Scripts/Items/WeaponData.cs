using UnityEngine;

[CreateAssetMenu(fileName = "NewWeapon", menuName = "ZoneRunner/Items/Weapon")]
public class WeaponData : ItemData
{
    [Header("Slot")]
    public WeaponSlotType weaponSlot = WeaponSlotType.Primary;

    [Header("Weapon Stats")]
    public int damage;
    public float range;
    public int magazineSize;
    public float durability = 100f;
    public string ammoTypeId;
    
    public override SlotCategory? EquipCategory => SlotCategory.Weapon;
    public override int PreferredSlotIndex => (int)weaponSlot; // Primary=0, Secondary=1
}