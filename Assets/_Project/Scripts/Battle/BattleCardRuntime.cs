public sealed class BattleCardRuntime
{
    public int InstanceId { get; }
    public BattleCardData Data { get; }

    public BattleCardRuntime(
        int instanceId,
        BattleCardData data
    )
    {
        InstanceId = instanceId;
        Data = data;
    }
}
