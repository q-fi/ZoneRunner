using UnityEngine;

[CreateAssetMenu(
    fileName = "EnemyData",
    menuName = "ZoneRunner/Battle/Enemy Data"
)]
public class EnemyData : ScriptableObject
{
    [Header("Identity")]
    public string enemyId;
    public string displayName = "ENEMY";
    public Sprite icon;

    [Header("Base Combat Stats")]
    [Min(1f)]
    public float maxHealth = 10f;

    [Min(0f)]
    public float defense;

    [Min(0f)]
    public float baseDamage = 2f;
}
