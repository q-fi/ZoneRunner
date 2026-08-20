using System;

[Flags]
public enum ItemInstanceOrigin
{
    Unknown = 0,
    DepartureBackpack = 1 << 0,
    ExpeditionLoot = 1 << 1
}

public class ItemInstance
{
    public readonly string InstanceId;
    public readonly ItemData Data;
    public int StackCount { get; private set; }
    public ItemInstanceOrigin Origins { get; private set; }
    public int ProtectedCount { get; private set; }

    public int UnprotectedCount =>
        Math.Max(0, StackCount - ProtectedCount);

    public bool IsProtectedForBattleLootResolution =>
        ProtectedCount > 0;

    public ItemInstance(
        ItemData data,
        int stackCount = 1,
        ItemInstanceOrigin origin = ItemInstanceOrigin.Unknown,
        int protectedCount = 0
    )
    {
        InstanceId = Guid.NewGuid().ToString();
        Data = data;
        StackCount = Math.Max(0, stackCount);
        Origins = origin;
        ProtectedCount = Math.Min(
            Math.Max(0, protectedCount),
            StackCount
        );
    }

    public int AddUnits(
        int count,
        ItemInstanceOrigin origin = ItemInstanceOrigin.Unknown,
        int protectedCount = 0
    )
    {
        int added = Math.Max(0, count);

        if (added == 0)
            return 0;

        StackCount += added;
        Origins |= origin;
        ProtectedCount += Math.Min(
            Math.Max(0, protectedCount),
            added
        );

        return added;
    }

    public int ConsumeUnits(int count)
    {
        int consumed = Math.Min(
            Math.Max(0, count),
            StackCount
        );

        if (consumed == 0)
            return 0;

        int consumedProtected = Math.Min(
            ProtectedCount,
            consumed
        );

        StackCount -= consumed;
        ProtectedCount -= consumedProtected;

        return consumed;
    }
}
