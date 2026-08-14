[System.Serializable]
public class StatModifier
{
    public PlayerStatType stat;
    public float value;

    public StatModifier(PlayerStatType stat, float value)
    {
        this.stat = stat;
        this.value = value;
    }
}