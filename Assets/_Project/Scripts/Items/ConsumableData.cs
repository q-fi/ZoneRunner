using UnityEngine;

public enum ConsumableEffect
{
    Heal,            // аптечка
    StopBleeding,    // бинт
    RemoveRadiation, // антирад
    RestoreStamina,  // їжа/напій
    Grenade          // граната
}

[CreateAssetMenu(fileName = "NewConsumable", menuName = "ZoneRunner/Items/Consumable")]
public class ConsumableData : ItemData
{
    [Header("Effect")]
    public ConsumableEffect effect;
    public float effectValue;

    public override SlotCategory? EquipCategory =>
        effect == ConsumableEffect.Heal
        || effect == ConsumableEffect.StopBleeding
        || effect == ConsumableEffect.RemoveRadiation
            ? SlotCategory.Medicine
            : null; // Grenade і RestoreStamina — без слота, тільки "Використати"
}