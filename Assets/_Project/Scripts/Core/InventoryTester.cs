using UnityEngine;

public class InventoryTester : MonoBehaviour
{
    [SerializeField] private ItemData[] testItems;

    public void AddAllTestItems()
    {
        foreach (var item in testItems)
        {
            InventoryManager.Instance.AddItem(item);
        }
    }
}