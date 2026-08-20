using System;
using System.Collections.Generic;

public enum BattleLootResolutionState
{
    Resolved,
    AwaitingOverflowChoice
}

public sealed class BattleLootReward
{
    private readonly Dictionary<string, int> storedAllocations = new(
        StringComparer.Ordinal
    );

    public string EntryId { get; }
    public ItemData Item { get; }
    public int GeneratedCount { get; }
    public int StoredCount { get; private set; }
    public int PendingCount { get; private set; }
    public int LeftBehindCount { get; private set; }
    public int ReplacedOutCount { get; private set; }
    internal IReadOnlyDictionary<string, int> StoredAllocations =>
        storedAllocations;

    public BattleLootReward(
        ItemData item,
        int generatedCount,
        int storedCount,
        IEnumerable<ExpeditionBackpackAllocation> allocations = null
    )
    {
        EntryId = Guid.NewGuid().ToString();
        Item = item;
        GeneratedCount = Math.Max(0, generatedCount);
        StoredCount = Math.Min(
            Math.Max(0, storedCount),
            GeneratedCount
        );
        PendingCount = GeneratedCount - StoredCount;

        int remainingTrackedCount = StoredCount;

        if (allocations == null)
            return;

        foreach (var allocation in allocations)
        {
            if (remainingTrackedCount == 0)
                break;

            if (allocation == null ||
                allocation.Item != Item ||
                string.IsNullOrEmpty(allocation.InstanceId) ||
                allocation.Count <= 0)
            {
                continue;
            }

            int trackedCount = Math.Min(
                allocation.Count,
                remainingTrackedCount
            );

            AddStoredAllocation(
                allocation.InstanceId,
                trackedCount
            );
            remainingTrackedCount -= trackedCount;
        }
    }

    internal int LeavePending(int requestedCount)
    {
        int leftBehind = Math.Min(
            Math.Max(0, requestedCount),
            PendingCount
        );

        PendingCount -= leftBehind;
        LeftBehindCount += leftBehind;
        return leftBehind;
    }

    internal bool CanStorePending(
        int requestedCount,
        IEnumerable<ExpeditionBackpackAllocation> allocations
    )
    {
        if (requestedCount <= 0 || requestedCount > PendingCount)
            return false;

        long allocationTotal = 0;

        if (allocations == null)
            return false;

        foreach (var allocation in allocations)
        {
            if (allocation == null ||
                allocation.Item != Item ||
                string.IsNullOrEmpty(allocation.InstanceId) ||
                allocation.Count <= 0)
            {
                return false;
            }

            allocationTotal += allocation.Count;

            if (allocationTotal > requestedCount)
                return false;
        }

        return allocationTotal == requestedCount;
    }

    internal int StorePending(
        int requestedCount,
        IEnumerable<ExpeditionBackpackAllocation> allocations
    )
    {
        if (!CanStorePending(requestedCount, allocations))
            return 0;

        foreach (var allocation in allocations)
        {
            AddStoredAllocation(
                allocation.InstanceId,
                allocation.Count
            );
        }

        PendingCount -= requestedCount;
        StoredCount += requestedCount;
        return requestedCount;
    }

    internal int GetStoredAllocation(string instanceId)
    {
        return !string.IsNullOrEmpty(instanceId) &&
            storedAllocations.TryGetValue(instanceId, out int count)
                ? count
                : 0;
    }

    internal int ReplaceOutStoredAllocation(string instanceId)
    {
        int removed = GetStoredAllocation(instanceId);

        if (removed <= 0)
            return 0;

        storedAllocations.Remove(instanceId);
        StoredCount -= removed;
        ReplacedOutCount += removed;
        return removed;
    }

    private void AddStoredAllocation(string instanceId, int count)
    {
        if (string.IsNullOrEmpty(instanceId) || count <= 0)
            return;

        if (storedAllocations.TryGetValue(
            instanceId,
            out int existingCount
        ))
        {
            storedAllocations[instanceId] = (int)Math.Min(
                int.MaxValue,
                (long)existingCount + count
            );
            return;
        }

        storedAllocations.Add(instanceId, count);
    }
}

public sealed class BattleLootRemovedBackpackItem
{
    public ItemData Item { get; }
    public int Count { get; private set; }

    public BattleLootRemovedBackpackItem(ItemData item, int count)
    {
        Item = item;
        Count = Math.Max(0, count);
    }

    internal void AddCount(int count)
    {
        Count = (int)Math.Min(
            int.MaxValue,
            (long)Count + Math.Max(0, count)
        );
    }
}

public sealed class BattleLootResolutionSession
{
    private readonly List<BattleLootReward> rewards = new();
    private readonly List<BattleLootRemovedBackpackItem>
        removedBackpackItems = new();
    private readonly Dictionary<string, int>
        replaceableBackpackAllowances = new(
            StringComparer.Ordinal
        );

    public string SessionId { get; }
    public string EncounterId { get; }
    public int DefeatedEnemyCount { get; }
    public IReadOnlyList<BattleLootReward> Rewards => rewards;
    public IReadOnlyList<BattleLootRemovedBackpackItem>
        RemovedBackpackItems => removedBackpackItems;
    public BattleLootResolutionState State { get; private set; }
    public bool RequiresPlayerReview { get; }
    public bool IsAcknowledged { get; private set; }

    public int TotalGeneratedCount => GetTotal(
        reward => reward.GeneratedCount
    );
    public int TotalStoredCount => GetTotal(
        reward => reward.StoredCount
    );
    public int TotalPendingCount => GetTotal(
        reward => reward.PendingCount
    );
    public int TotalLeftBehindCount => GetTotal(
        reward => reward.LeftBehindCount
    );
    public int TotalReplacedOutCount => GetTotal(
        reward => reward.ReplacedOutCount
    );
    public int TotalRemovedBackpackCount
    {
        get
        {
            int total = 0;

            foreach (var item in removedBackpackItems)
                total = SaturatingAdd(total, item.Count);

            return total;
        }
    }

    public bool HasPendingLoot => TotalPendingCount > 0;
    public bool IsResolved =>
        State == BattleLootResolutionState.Resolved;

    public BattleLootResolutionSession(
        string encounterId,
        int defeatedEnemyCount,
        IEnumerable<BattleLootReward> resolvedRewards,
        IReadOnlyDictionary<string, int>
            initialReplaceableBackpackAllowances = null
    )
    {
        SessionId = Guid.NewGuid().ToString();
        EncounterId = encounterId ?? string.Empty;
        DefeatedEnemyCount = Math.Max(0, defeatedEnemyCount);

        if (resolvedRewards != null)
        {
            foreach (var reward in resolvedRewards)
            {
                if (reward?.Item == null ||
                    reward.GeneratedCount <= 0)
                {
                    continue;
                }

                rewards.Add(reward);
            }
        }

        if (initialReplaceableBackpackAllowances != null)
        {
            foreach (var pair in initialReplaceableBackpackAllowances)
            {
                if (!string.IsNullOrEmpty(pair.Key) && pair.Value > 0)
                    replaceableBackpackAllowances[pair.Key] = pair.Value;
            }
        }

        RefreshState();
        RequiresPlayerReview = HasPendingLoot;
        IsAcknowledged = !RequiresPlayerReview;
    }

    internal bool Acknowledge()
    {
        if (!IsResolved || IsAcknowledged)
            return false;

        IsAcknowledged = true;
        return true;
    }

    internal int GetReplaceableBackpackCount(string instanceId)
    {
        if (string.IsNullOrEmpty(instanceId))
            return 0;

        int total = replaceableBackpackAllowances.TryGetValue(
            instanceId,
            out int oldLootCount
        )
            ? oldLootCount
            : 0;

        foreach (var reward in rewards)
        {
            total = SaturatingAdd(
                total,
                reward.GetStoredAllocation(instanceId)
            );
        }

        return total;
    }

    internal int GetCurrentBattleStoredCount(string instanceId)
    {
        if (string.IsNullOrEmpty(instanceId))
            return 0;

        int total = 0;

        foreach (var reward in rewards)
        {
            total = SaturatingAdd(
                total,
                reward.GetStoredAllocation(instanceId)
            );
        }

        return total;
    }

    internal IReadOnlyDictionary<string, int>
        CreateReplaceableBackpackAllowances()
    {
        var result = new Dictionary<string, int>(
            replaceableBackpackAllowances,
            StringComparer.Ordinal
        );

        foreach (var reward in rewards)
        {
            foreach (var allocation in reward.StoredAllocations)
            {
                int existingCount = result.TryGetValue(
                    allocation.Key,
                    out int count
                )
                    ? count
                    : 0;

                result[allocation.Key] = SaturatingAdd(
                    existingCount,
                    allocation.Value
                );
            }
        }

        return result;
    }

    internal int LeavePending(
        string entryId,
        int requestedCount
    )
    {
        if (string.IsNullOrEmpty(entryId))
            return 0;

        foreach (var reward in rewards)
        {
            if (!string.Equals(
                reward.EntryId,
                entryId,
                StringComparison.Ordinal
            ))
            {
                continue;
            }

            int leftBehind = reward.LeavePending(requestedCount);
            RefreshState();
            return leftBehind;
        }

        return 0;
    }

    internal BattleLootReward FindReward(string entryId)
    {
        if (string.IsNullOrEmpty(entryId))
            return null;

        foreach (var reward in rewards)
        {
            if (string.Equals(
                reward.EntryId,
                entryId,
                StringComparison.Ordinal
            ))
            {
                return reward;
            }
        }

        return null;
    }

    internal bool TryStorePending(
        string entryId,
        ExpeditionBackpackAddResult addResult
    )
    {
        BattleLootReward reward = FindReward(entryId);

        if (reward == null ||
            addResult == null ||
            !reward.CanStorePending(
                addResult.AddedCount,
                addResult.Allocations
            ))
        {
            return false;
        }

        int stored = reward.StorePending(
            addResult.AddedCount,
            addResult.Allocations
        );

        if (stored != addResult.AddedCount)
            return false;

        RefreshState();
        return true;
    }

    internal bool TryApplyManualReplacement(
        string incomingEntryId,
        ExpeditionBackpackReplacementResult result
    )
    {
        BattleLootReward incomingReward =
            FindReward(incomingEntryId);

        if (incomingReward == null ||
            result == null ||
            !incomingReward.CanStorePending(
                result.AddedCount,
                result.AddedAllocations
            ) ||
            !CanApplyRemovedBackpackItems(result.RemovedItems))
        {
            return false;
        }

        ApplyRemovedBackpackItems(result.RemovedItems);

        int stored = incomingReward.StorePending(
            result.AddedCount,
            result.AddedAllocations
        );

        if (stored != result.AddedCount)
            return false;

        RefreshState();
        return true;
    }

    internal bool TryApplyBackpackDiscard(
        ExpeditionBackpackReplacementResult result
    )
    {
        if (result == null ||
            result.AddedCount != 0 ||
            result.AddedAllocations.Count != 0 ||
            result.RemovedItems.Count == 0 ||
            !CanApplyRemovedBackpackItems(result.RemovedItems))
        {
            return false;
        }

        ApplyRemovedBackpackItems(result.RemovedItems);
        RefreshState();
        return true;
    }

    private bool CanApplyRemovedBackpackItems(
        IReadOnlyList<ExpeditionRemovedBackpackItem> removedItems
    )
    {
        if (removedItems == null)
            return false;

        var removedInstanceIds = new HashSet<string>(
            StringComparer.Ordinal
        );

        foreach (var removed in removedItems)
        {
            if (removed?.Item == null ||
                removed.Count <= 0 ||
                string.IsNullOrEmpty(removed.InstanceId) ||
                !removedInstanceIds.Add(removed.InstanceId))
            {
                return false;
            }

            int currentBattleCount = 0;

            foreach (var reward in rewards)
            {
                currentBattleCount = SaturatingAdd(
                    currentBattleCount,
                    reward.GetStoredAllocation(removed.InstanceId)
                );
            }

            int oldLootCount =
                replaceableBackpackAllowances.TryGetValue(
                    removed.InstanceId,
                    out int allowedCount
                )
                    ? allowedCount
                    : 0;

            if ((long)currentBattleCount + oldLootCount !=
                removed.Count)
            {
                return false;
            }
        }

        return true;
    }

    private void ApplyRemovedBackpackItems(
        IReadOnlyList<ExpeditionRemovedBackpackItem> removedItems
    )
    {
        foreach (var removed in removedItems)
        {
            int currentBattleCount = 0;

            foreach (var reward in rewards)
            {
                currentBattleCount = SaturatingAdd(
                    currentBattleCount,
                    reward.ReplaceOutStoredAllocation(
                        removed.InstanceId
                    )
                );
            }

            int oldLootCount = Math.Max(
                0,
                removed.Count - currentBattleCount
            );

            replaceableBackpackAllowances.Remove(
                removed.InstanceId
            );

            AddRemovedBackpackItem(
                removed.Item,
                oldLootCount
            );
        }
    }

    private void AddRemovedBackpackItem(ItemData item, int count)
    {
        if (item == null || count <= 0)
            return;

        foreach (var existing in removedBackpackItems)
        {
            if (existing.Item != item)
                continue;

            existing.AddCount(count);
            return;
        }

        removedBackpackItems.Add(
            new BattleLootRemovedBackpackItem(item, count)
        );
    }

    internal int LeaveAllPending()
    {
        int totalLeftBehind = 0;

        foreach (var reward in rewards)
        {
            totalLeftBehind = SaturatingAdd(
                totalLeftBehind,
                reward.LeavePending(reward.PendingCount)
            );
        }

        RefreshState();
        return totalLeftBehind;
    }

    private void RefreshState()
    {
        State = HasPendingLoot
            ? BattleLootResolutionState.AwaitingOverflowChoice
            : BattleLootResolutionState.Resolved;
    }

    private int GetTotal(Func<BattleLootReward, int> selector)
    {
        int total = 0;

        foreach (var reward in rewards)
            total = SaturatingAdd(total, selector(reward));

        return total;
    }

    private static int SaturatingAdd(int first, int second)
    {
        return (int)Math.Min(
            int.MaxValue,
            (long)Math.Max(0, first) + Math.Max(0, second)
        );
    }
}
