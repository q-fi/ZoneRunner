using UnityEngine;

public enum ConsumableEffect
{
    Heal,
    RemoveRadiation,
    RestoreStamina,
    Grenade
}

[CreateAssetMenu(fileName = "NewConsumable", menuName = "ZoneRunner/Items/Consumable")]
public class ConsumableData : ItemData
{
    [Header("Effect")]
    public ConsumableEffect effect;
    public float effectValue;
}