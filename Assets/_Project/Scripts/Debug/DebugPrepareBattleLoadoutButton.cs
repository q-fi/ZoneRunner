using UnityEngine;

public class DebugPrepareBattleLoadoutButton : MonoBehaviour
{
    private const int BackpackPresetOneIndex = 0;

    [Header("Test Loadout")]
    [SerializeField] private WeaponData weapon;
    [SerializeField] private AmmoData ammo;
    [SerializeField] private ConsumableData grenade;

    [Header("Backpack Preset 1 Amounts")]
    [Min(1)]
    [SerializeField] private int ammoCount = 10;

    [Min(1)]
    [SerializeField] private int grenadeCount = 2;

    [Header("Manual Replace Test")]
    [SerializeField] private ItemData replaceableRuntimeLoot;

    [Min(1)]
    [SerializeField] private int replaceableRuntimeLootCount = 1;

    public void PrepareBattleLoadout()
    {
        InventoryManager inventory = InventoryManager.Instance;

        if (inventory == null)
        {
            Debug.LogError(
                "DebugPrepareBattleLoadoutButton: " +
                "InventoryManager is missing."
            );
            return;
        }

        if (weapon == null || ammo == null || grenade == null)
        {
            Debug.LogError(
                "DebugPrepareBattleLoadoutButton: assign Weapon, " +
                "Ammo and Grenade in the Inspector."
            );
            return;
        }

        bool equipped = inventory.TryEquipItem(weapon);

        inventory.SelectPreset(BackpackPresetOneIndex);

        bool ammoAdded = inventory.SaveItemToPreset(
            new ItemInstance(ammo, Mathf.Max(1, ammoCount))
        );

        bool grenadesAdded = inventory.SaveItemToPreset(
            new ItemInstance(grenade, Mathf.Max(1, grenadeCount))
        );

        Debug.Log(
            "DEBUG battle loadout: " +
            $"weapon {(equipped ? "equipped" : "failed")}, " +
            $"ammo x{ammoCount} " +
            $"{(ammoAdded ? "added" : "failed")}, " +
            $"grenades x{grenadeCount} " +
            $"{(grenadesAdded ? "added" : "failed")} " +
            "to Backpack Preset 1."
        );
    }

    public void AddReplaceableRuntimeLoot()
    {
        TravelManager travel = TravelManager.Instance;

        if (travel == null)
        {
            Debug.LogError(
                "DEBUG runtime loot: TravelManager is missing."
            );
            return;
        }

        if (!travel.IsTraveling || !travel.IsPausedForBattle)
        {
            Debug.LogError(
                "DEBUG runtime loot: press this button during an " +
                "active battle, before defeating the final enemy."
            );
            return;
        }

        if (replaceableRuntimeLoot == null)
        {
            Debug.LogError(
                "DEBUG runtime loot: assign Replaceable Runtime " +
                "Loot in the Inspector."
            );
            return;
        }

        int requestedCount = Mathf.Max(
            1,
            replaceableRuntimeLootCount
        );
        int addedCount = travel.AddSelectedBackpackItem(
            replaceableRuntimeLoot,
            requestedCount
        );

        if (addedCount != requestedCount)
        {
            Debug.LogError(
                $"DEBUG runtime loot: only added " +
                $"{replaceableRuntimeLoot.itemName} " +
                $"x{addedCount}/{requestedCount}."
            );
            return;
        }

        Debug.Log(
            $"DEBUG pre-victory LOOT added: " +
            $"{replaceableRuntimeLoot.itemName} x{addedCount}. " +
            "It can be replaced by pending victory loot."
        );
    }
}
