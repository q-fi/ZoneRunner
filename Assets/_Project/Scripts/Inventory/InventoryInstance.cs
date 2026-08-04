using System;

public class ItemInstance
{
    public readonly string InstanceId;
    public readonly ItemData Data;
    public int StackCount;

    public ItemInstance(ItemData data, int stackCount = 1)
    {
        InstanceId = Guid.NewGuid().ToString();
        Data = data;
        StackCount = stackCount;
    }
}