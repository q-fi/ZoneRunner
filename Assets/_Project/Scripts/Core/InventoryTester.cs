using UnityEngine;

public class InventoryTester : MonoBehaviour
{
    [SerializeField] private ItemData[] testItems;
    [SerializeField] private WeaponData testWeapon;
    [SerializeField] private ArmorData testArmor;

    public void AddAllTestItems()
    {
        foreach (var item in testItems)
        {
            InventoryManager.Instance.AddItem(item);
        }
    }

    public void EquipTestWeapon()
    {
        InventoryManager.Instance.EquipWeapon(testWeapon, isPrimary: true);
    }

    public void EquipTestArmor()
    {
        InventoryManager.Instance.EquipArmor(testArmor);
    }
}