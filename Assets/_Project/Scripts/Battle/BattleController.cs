using System;
using System.Collections.Generic;
using UnityEngine;

public enum BattlePhase
{
    None,
    Setup,
    PlayerTurn,
    EnemyTurn,
    Victory,
    Defeat
}

public class BattleController : MonoBehaviour
{
    private enum VictoryLootResolutionProgress
    {
        NotStarted,
        Resolving,
        Completed,
        Failed
    }

    private const int MaxCardsPerTurn = 2;
    private const int TargetHandSize = 5;

    [Header("Temporary Test")]
    [SerializeField] private BattleCardData testCard;
    [SerializeField] private BattleDeckData testDeck;

    public BattlePhase CurrentPhase { get; private set; } = BattlePhase.None;
    public BattlePlayerState Player { get; private set; }
    public BattleCardRuntime SelectedCard { get; private set; }
    public IReadOnlyList<BattleCardRuntime> Hand => runtimeDeck?.Hand;
    public IReadOnlyList<BattleEnemyState> Enemies => enemies;
    public int CardsPlayedThisTurn { get; private set; }
    public int CardsPerTurnLimit => MaxCardsPerTurn;
    public BattleLootResolutionSession LootSession { get; private set; }
    public bool CanContinueAfterVictory =>
        CurrentPhase == BattlePhase.Victory &&
        lootResolutionProgress ==
            VictoryLootResolutionProgress.Completed &&
        LootSession != null &&
        LootSession.IsResolved &&
        LootSession.IsAcknowledged;

    public event Action OnBattleStateChanged;
    public event Action<BattleEnemyState, float> OnEnemyDamaged;
    public event Action<BattleLootResolutionSession>
        OnVictoryLootResolved;

    private readonly List<BattleEnemyState> enemies = new();
    private BattleDeckRuntime runtimeDeck;
    private VictoryLootResolutionProgress lootResolutionProgress;

    private void OnEnable()
    {
        if (GameManager.Instance != null &&
            GameManager.Instance.CurrentState == GameState.Battle)
        {
            if (CurrentPhase == BattlePhase.None)
                InitializeBattle();
        }
    }

    private void ResetBattleRuntime()
    {
        CurrentPhase = BattlePhase.None;
        Player = null;
        SelectedCard = null;
        runtimeDeck = null;
        enemies.Clear();
        CardsPlayedThisTurn = 0;
        LootSession = null;
        lootResolutionProgress =
            VictoryLootResolutionProgress.NotStarted;
    }

    private void InitializeBattle()
    {
        ResetBattleRuntime();
        CurrentPhase = BattlePhase.Setup;

        if (PlayerStats.Instance == null)
        {
            Debug.LogError("BattleController: PlayerStats is missing.");
            CurrentPhase = BattlePhase.None;
            return;
        }

        if (TravelManager.Instance == null ||
            TravelManager.Instance.CurrentEncounter == null)
        {
            Debug.LogError("BattleController: EncounterData is missing.");
            CurrentPhase = BattlePhase.None;
            return;
        }

        Player = new BattlePlayerState(
            PlayerStats.Instance,
            TravelManager.Instance.SelectedEquipmentItems
        );

        runtimeDeck = new BattleDeckRuntime(testDeck);
        runtimeDeck.DrawToHand(TargetHandSize);

        foreach (var slot in TravelManager.Instance.CurrentEncounter.Enemies)
        {
            if (slot?.enemy == null)
                continue;

            enemies.Add(
                new BattleEnemyState(
                    slot.enemy,
                    slot.formationSlot
                )
            );
        }

        if (enemies.Count == 0)
        {
            Debug.LogError("BattleController: Encounter has no enemies.");
            CurrentPhase = BattlePhase.None;
            return;
        }

        Debug.Log(
            $"Battle initialized: Player HP {Player.CurrentHealth:0.#}/" +
            $"{Player.MaxHealth:0.#}, Stamina {Player.CurrentStamina:0.#}/" +
            $"{Player.MaxStamina:0.#}, Defense {Player.Defense:0.#}."
        );

        Debug.Log(
            $"Battle hand: {runtimeDeck.Hand.Count}, " +
            $"draw pile: {runtimeDeck.DrawPileCount}, " +
            $"discard pile: {runtimeDeck.DiscardPileCount}."
        );

        foreach (var enemy in enemies)
        {
            Debug.Log(
                $"Enemy [{enemy.FormationSlot}]: {enemy.Data.displayName}, " +
                $"HP {enemy.CurrentHealth:0.#}/{enemy.MaxHealth:0.#}, " +
                $"Defense {enemy.Defense:0.#}."
            );
        }

        CurrentPhase = BattlePhase.PlayerTurn;
        Debug.Log("Battle phase: PlayerTurn.");
        OnBattleStateChanged?.Invoke();
    }

    [ContextMenu("Play Test Card On First Enemy")]
    private void PlayTestCardOnFirstEnemy()
    {
        if (testCard == null)
        {
            Debug.LogWarning("BattleController: Test Card is missing.");
            return;
        }

        int targetIndex = enemies.FindIndex(enemy => enemy.IsAlive);

        if (targetIndex < 0)
        {
            Debug.LogWarning("BattleController: no living enemy target.");
            return;
        }

        TryPlayCard(testCard, targetIndex);
    }

    public bool TryPlayCard(BattleCardData card, int targetIndex)
    {
        if (CurrentPhase != BattlePhase.PlayerTurn ||
            card == null ||
            CardsPlayedThisTurn >= MaxCardsPerTurn ||
            targetIndex < 0 ||
            targetIndex >= enemies.Count ||
            !enemies[targetIndex].IsAlive)
        {
            return false;
        }

        if (!HasRequiredBackpackItem(card))
        {
            Debug.LogWarning(
                $"Missing backpack item for {card.displayName}."
            );
            return false;
        }

        if (!HasCompatibleEquippedWeapon(card))
        {
            Debug.LogWarning(
                $"No equipped weapon compatible with " +
                $"{card.requiredBackpackItem.itemName}."
            );
            return false;
        }

        if (!Player.TrySpendStamina(card.staminaCost))
        {
            Debug.LogWarning(
                $"Not enough Stamina for {card.displayName}."
            );
            return false;
        }

        if (!TryConsumeRequiredBackpackItem(card))
        {
            Debug.LogWarning(
                $"Missing backpack item for {card.displayName}."
            );
            return false;
        }

        CardsPlayedThisTurn++;

        float hitRoll = UnityEngine.Random.Range(0f, 100f);
        bool hit =
            card.hitChancePercent >= 100f ||
            (card.hitChancePercent > 0f &&
             hitRoll < card.hitChancePercent);

        var target = enemies[targetIndex];

        if (hit)
        {
            ApplyCardEffects(card, targetIndex);
        }
        else
        {
            Debug.Log(
                $"{card.displayName} missed {target.Data.displayName} " +
                $"(roll {hitRoll:0.#})."
            );
        }

        Debug.Log(
            $"Cards: {CardsPlayedThisTurn}/{MaxCardsPerTurn}, " +
            $"Stamina: {Player.CurrentStamina:0.#}/{Player.MaxStamina:0.#}."
        );

        if (!HasLivingEnemies())
        {
            CompleteVictory();
            return true;
        }

        if (CardsPlayedThisTurn >= MaxCardsPerTurn)
        {
            RunEnemyTurn();
        }
        else
        {
            OnBattleStateChanged?.Invoke();
        }

        return true;
    }

    public bool SelectCard(BattleCardRuntime runtimeCard)
    {
        if (!CanSelectCard(runtimeCard))
            return false;

        SelectedCard = runtimeCard;
        Debug.Log(
            $"Selected card: {SelectedCard.Data.displayName} " +
            $"(instance {SelectedCard.InstanceId})."
        );
        OnBattleStateChanged?.Invoke();
        return true;
    }

    public bool TryPlaySelectedCard(int targetIndex)
    {
        if (SelectedCard == null)
            return false;

        BattleCardRuntime runtimeCard = SelectedCard;
        BattleCardData card = runtimeCard.Data;
        SelectedCard = null;

        if (runtimeDeck == null ||
            !runtimeDeck.MoveFromHandToDiscard(runtimeCard))
        {
            SelectedCard = runtimeCard;
            return false;
        }

        bool played = TryPlayCard(card, targetIndex);

        if (!played)
        {
            runtimeDeck.ReturnFromDiscardToHand(runtimeCard);
            SelectedCard = runtimeCard;
        }

        OnBattleStateChanged?.Invoke();
        return played;
    }

    public bool IsCardInHand(BattleCardRuntime card)
    {
        return runtimeDeck != null && runtimeDeck.ContainsInHand(card);
    }

    public bool CanSelectCard(BattleCardRuntime runtimeCard)
    {
        BattleCardData card = runtimeCard?.Data;

        return
            CurrentPhase == BattlePhase.PlayerTurn &&
            Player != null &&
            runtimeCard != null &&
            card != null &&
            CardsPlayedThisTurn < MaxCardsPerTurn &&
            Player.CurrentStamina >= card.staminaCost &&
            IsCardInHand(runtimeCard) &&
            HasRequiredBackpackItem(card) &&
            HasCompatibleEquippedWeapon(card);
    }

    public bool TryGetCardDisplayedDamage(
        BattleCardData card,
        out float damage
    )
    {
        damage = 0f;

        if (card == null)
            return false;

        if (card.requiredBackpackItem is AmmoData ammo)
        {
            WeaponData weapon = GetCompatibleEquippedWeapon(card);

            if (weapon == null)
                return false;

            damage = Mathf.Max(
                0f,
                weapon.damage + ammo.damageModifier
            );

            return true;
        }

        bool hasDamageEffect = false;

        foreach (var effect in card.Effects)
        {
            if (effect == null ||
                effect.effectType != BattleCardEffectType.Damage)
            {
                continue;
            }

            hasDamageEffect = true;
            damage += Mathf.Max(0f, effect.value);
        }

        return hasDamageEffect;
    }

    public bool TryGetCardWeapon(
        BattleCardData card,
        out WeaponData weapon
    )
    {
        weapon = GetCompatibleEquippedWeapon(card);
        return weapon != null;
    }

    public bool TryGetSelectedCardDamageRange(
        BattleEnemyState target,
        out float minimumDamage,
        out float maximumDamage
    )
    {
        minimumDamage = 0f;
        maximumDamage = 0f;

        if (CurrentPhase != BattlePhase.PlayerTurn ||
            SelectedCard?.Data == null ||
            target == null ||
            !target.IsAlive)
        {
            return false;
        }

        bool hasDamageEffect = false;

        foreach (var effect in SelectedCard.Data.Effects)
        {
            if (effect == null ||
                effect.effectType != BattleCardEffectType.Damage ||
                !TryGetRawDamageRange(
                    SelectedCard.Data,
                    effect.value,
                    out float rawMinimum,
                    out float rawMaximum
                ))
            {
                continue;
            }

            hasDamageEffect = true;
            bool usesAmmo =
                SelectedCard.Data.requiredBackpackItem is AmmoData;
            float resolvedMinimum = usesAmmo
                ? Mathf.Round(rawMinimum)
                : rawMinimum;
            float resolvedMaximum = usesAmmo
                ? Mathf.Round(rawMaximum)
                : rawMaximum;

            minimumDamage += Mathf.Max(
                0f,
                resolvedMinimum - target.Defense
            );
            maximumDamage += Mathf.Max(
                0f,
                resolvedMaximum - target.Defense
            );
        }

        return hasDamageEffect;
    }

    private bool HasRequiredBackpackItem(BattleCardData card)
    {
        if (card == null ||
            card.requiredBackpackItem == null ||
            card.backpackItemCost <= 0)
        {
            return true;
        }

        return TravelManager.Instance != null &&
            TravelManager.Instance.GetSelectedBackpackItemCount(
                card.requiredBackpackItem
            ) >= card.backpackItemCost;
    }

    private bool TryConsumeRequiredBackpackItem(BattleCardData card)
    {
        if (card == null ||
            card.requiredBackpackItem == null ||
            card.backpackItemCost <= 0)
        {
            return true;
        }

        return TravelManager.Instance != null &&
            TravelManager.Instance.TryConsumeSelectedBackpackItem(
                card.requiredBackpackItem,
                card.backpackItemCost
            );
    }

    private bool HasCompatibleEquippedWeapon(BattleCardData card)
    {
        if (card?.requiredBackpackItem is not AmmoData)
            return true;

        return GetCompatibleEquippedWeapon(card) != null;
    }

    private WeaponData GetCompatibleEquippedWeapon(BattleCardData card)
    {
        if (card?.requiredBackpackItem is not AmmoData ammo ||
            TravelManager.Instance == null ||
            string.IsNullOrWhiteSpace(ammo.ammoTypeId))
        {
            return null;
        }

        WeaponData secondaryWeapon = null;

        foreach (var item in TravelManager.Instance.SelectedEquipmentItems)
        {
            if (item?.Data is not WeaponData weapon ||
                !string.Equals(
                    weapon.ammoTypeId,
                    ammo.ammoTypeId,
                    StringComparison.OrdinalIgnoreCase
                ))
            {
                continue;
            }

            if (weapon.weaponSlot == WeaponSlotType.Primary)
                return weapon;

            secondaryWeapon ??= weapon;
        }

        return secondaryWeapon;
    }

    private float CalculateRawDamage(
        BattleCardData card,
        float cardDamage
    )
    {
        if (!TryGetRawDamageRange(
            card,
            cardDamage,
            out float minimumDamage,
            out float maximumDamage
        ))
        {
            return 0f;
        }

        float rawDamage = maximumDamage;

        if (card.requiredBackpackItem is AmmoData ammo)
        {
            rawDamage = Mathf.Approximately(
                minimumDamage,
                maximumDamage
            )
                ? Mathf.Round(maximumDamage)
                : Mathf.Round(
                    UnityEngine.Random.Range(
                        minimumDamage,
                        maximumDamage
                    )
                );

            WeaponData weapon = GetCompatibleEquippedWeapon(card);

            Debug.Log(
                $"Damage source: {weapon.itemName} + {ammo.itemName} | " +
                $"range {minimumDamage:0.#}-{maximumDamage:0.#}, " +
                $"roll {rawDamage:0.#}."
            );
        }

        return Mathf.Max(0f, rawDamage);
    }

    private bool TryGetRawDamageRange(
        BattleCardData card,
        float cardDamage,
        out float minimumDamage,
        out float maximumDamage
    )
    {
        minimumDamage = 0f;
        maximumDamage = 0f;

        if (card == null)
            return false;

        if (card.requiredBackpackItem is not AmmoData ammo)
        {
            minimumDamage = Mathf.Max(0f, cardDamage);
            maximumDamage = minimumDamage;
            return true;
        }

        WeaponData weapon = GetCompatibleEquippedWeapon(card);

        if (weapon == null)
            return false;

        maximumDamage = Mathf.Max(
            0f,
            weapon.damage + ammo.damageModifier
        );

        float spread = Mathf.Clamp01(
            weapon.damageSpreadPercent / 100f
        );

        minimumDamage = maximumDamage * (1f - spread);
        return true;
    }

    [ContextMenu("End Player Turn")]
    public void EndPlayerTurn()
    {
        if (CurrentPhase != BattlePhase.PlayerTurn)
        {
            Debug.LogWarning(
                "BattleController: зараз не хід гравця."
            );
            return;
        }

        SelectedCard = null;

        Debug.Log(
            $"Player ended turn after " +
            $"{CardsPlayedThisTurn}/{MaxCardsPerTurn} cards."
        );

        RunEnemyTurn();
    }

    [ContextMenu("Continue After Victory")]
    public void ContinueAfterVictory()
    {
        if (CurrentPhase != BattlePhase.Victory)
        {
            Debug.LogWarning(
                "BattleController: battle has not been won yet."
            );
            return;
        }

        if (!CanContinueAfterVictory)
        {
            Debug.LogWarning(
                "BattleController: pending battle loot must be " +
                "resolved before continuing."
            );
            return;
        }

        if (TravelManager.Instance == null)
        {
            Debug.LogError("BattleController: TravelManager is missing.");
            return;
        }

        Debug.Log("Battle victory confirmed. Continuing travel.");
        if (!TravelManager.Instance.ResumeTravelAfterBattle())
            return;

        ResetBattleRuntime();
    }

    public bool LeaveAllPendingLoot()
    {
        if (CurrentPhase != BattlePhase.Victory ||
            LootSession == null ||
            !LootSession.HasPendingLoot)
        {
            return false;
        }

        int leftBehind = LootSession.LeaveAllPending();

        Debug.Log(
            $"Battle loot left behind: x{leftBehind}. " +
            $"Session state: {LootSession.State}."
        );

        OnBattleStateChanged?.Invoke();
        return leftBehind > 0;
    }

    public bool LeavePendingLoot(string entryId, int count)
    {
        if (CurrentPhase != BattlePhase.Victory ||
            LootSession == null ||
            !LootSession.HasPendingLoot)
        {
            return false;
        }

        int leftBehind = LootSession.LeavePending(entryId, count);

        if (leftBehind <= 0)
            return false;

        Debug.Log(
            $"Battle loot entry left behind: x{leftBehind}. " +
            $"Pending x{LootSession.TotalPendingCount}."
        );

        OnBattleStateChanged?.Invoke();
        return true;
    }

    public bool AcknowledgeLootResolution()
    {
        if (CurrentPhase != BattlePhase.Victory ||
            LootSession == null ||
            !LootSession.Acknowledge())
        {
            return false;
        }

        Debug.Log("Battle loot review confirmed.");
        OnBattleStateChanged?.Invoke();
        return true;
    }

    public bool CanReplacePendingLoot(
        string entryId,
        int requestedCount,
        IReadOnlyCollection<string> removableInstanceIds
    )
    {
        if (CurrentPhase != BattlePhase.Victory ||
            LootSession == null ||
            TravelManager.Instance == null)
        {
            return false;
        }

        BattleLootReward reward = LootSession.FindReward(entryId);

        return reward != null &&
            reward.Item != null &&
            requestedCount > 0 &&
            requestedCount <= reward.PendingCount &&
            TravelManager.Instance.CanReplaceSelectedBackpackLoot(
                reward.Item,
                requestedCount,
                removableInstanceIds,
                LootSession.CreateReplaceableBackpackAllowances()
            );
    }

    public bool CanPlacePendingLootAt(
        string entryId,
        int requestedCount,
        int targetX,
        int targetY,
        IReadOnlyCollection<string> removableInstanceIds
    )
    {
        if (CurrentPhase != BattlePhase.Victory ||
            LootSession == null ||
            TravelManager.Instance == null)
        {
            return false;
        }

        BattleLootReward reward = LootSession.FindReward(entryId);

        return reward != null &&
            reward.Item != null &&
            requestedCount > 0 &&
            requestedCount <= reward.PendingCount &&
            TravelManager.Instance.CanPlaceSelectedBackpackLootAt(
                reward.Item,
                requestedCount,
                targetX,
                targetY,
                removableInstanceIds,
                LootSession.CreateReplaceableBackpackAllowances()
            );
    }

    public bool CanMergePendingLootInto(
        string entryId,
        int requestedCount,
        string targetInstanceId
    )
    {
        if (CurrentPhase != BattlePhase.Victory ||
            LootSession == null ||
            TravelManager.Instance == null)
        {
            return false;
        }

        BattleLootReward reward = LootSession.FindReward(entryId);

        return reward != null &&
            reward.Item != null &&
            requestedCount > 0 &&
            requestedCount <= reward.PendingCount &&
            TravelManager.Instance.CanMergeSelectedBackpackLootInto(
                reward.Item,
                requestedCount,
                targetInstanceId
            );
    }

    public bool CanSelectBackpackItemForLootReplacement(
        ItemInstance item
    )
    {
        return CurrentPhase == BattlePhase.Victory &&
            LootSession != null &&
            item != null &&
            item.Data != null &&
            item.StackCount > 0 &&
            item.ProtectedCount == 0 &&
            (item.Origins & ItemInstanceOrigin.ExpeditionLoot) != 0 &&
            LootSession.GetReplaceableBackpackCount(
                item.InstanceId
            ) >= item.StackCount;
    }

    public bool IsCurrentBattleLoot(ItemInstance item)
    {
        return LootSession != null &&
            item != null &&
            LootSession.GetCurrentBattleStoredCount(
                item.InstanceId
            ) > 0;
    }

    public bool TryDiscardSelectedBackpackLoot(
        IReadOnlyCollection<string> removableInstanceIds
    )
    {
        if (CurrentPhase != BattlePhase.Victory ||
            LootSession == null ||
            TravelManager.Instance == null ||
            removableInstanceIds == null ||
            removableInstanceIds.Count == 0)
        {
            return false;
        }

        IReadOnlyDictionary<string, int> replaceableAllowances =
            LootSession.CreateReplaceableBackpackAllowances();

        if (!TravelManager.Instance.TryDiscardSelectedBackpackLoot(
            removableInstanceIds,
            replaceableAllowances,
            out ExpeditionBackpackReplacementResult result
        ))
        {
            return false;
        }

        if (!LootSession.TryApplyBackpackDiscard(result))
        {
            Debug.LogError(
                "BattleController: runtime backpack discard " +
                "succeeded, but its loot ledger could not be applied."
            );
            return false;
        }

        Debug.Log(
            $"Battle backpack discarded {result.RemovedItems.Count} " +
            $"stack(s); old loot removed " +
            $"x{LootSession.TotalRemovedBackpackCount}; new loot " +
            $"discarded x{LootSession.TotalReplacedOutCount}."
        );

        OnBattleStateChanged?.Invoke();
        return true;
    }

    public bool TryReplacePendingLoot(
        string entryId,
        int requestedCount,
        IReadOnlyCollection<string> removableInstanceIds
    )
    {
        if (CurrentPhase != BattlePhase.Victory ||
            LootSession == null ||
            TravelManager.Instance == null)
        {
            return false;
        }

        BattleLootReward reward = LootSession.FindReward(entryId);

        if (reward == null ||
            reward.Item == null ||
            requestedCount <= 0 ||
            requestedCount > reward.PendingCount)
        {
            return false;
        }

        IReadOnlyDictionary<string, int> replaceableAllowances =
            LootSession.CreateReplaceableBackpackAllowances();

        if (!TravelManager.Instance.TryReplaceSelectedBackpackLoot(
            reward.Item,
            requestedCount,
            removableInstanceIds,
            replaceableAllowances,
            out ExpeditionBackpackReplacementResult result
        ))
        {
            return false;
        }

        if (!LootSession.TryApplyManualReplacement(entryId, result))
        {
            Debug.LogError(
                "BattleController: runtime backpack replacement " +
                "succeeded, but its loot ledger could not be applied."
            );
            return false;
        }

        Debug.Log(
            $"Battle loot manually replaced: {reward.Item.itemName} " +
            $"x{result.AddedCount}; old backpack loot removed " +
            $"x{LootSession.TotalRemovedBackpackCount}; " +
            $"new loot replaced out " +
            $"x{LootSession.TotalReplacedOutCount}; pending " +
            $"x{LootSession.TotalPendingCount}."
        );

        OnBattleStateChanged?.Invoke();
        return true;
    }

    public bool TryPlacePendingLootAt(
        string entryId,
        int requestedCount,
        int targetX,
        int targetY,
        IReadOnlyCollection<string> removableInstanceIds
    )
    {
        if (CurrentPhase != BattlePhase.Victory ||
            LootSession == null ||
            TravelManager.Instance == null)
        {
            return false;
        }

        BattleLootReward reward = LootSession.FindReward(entryId);

        if (reward == null ||
            reward.Item == null ||
            requestedCount <= 0 ||
            requestedCount > reward.PendingCount)
        {
            return false;
        }

        IReadOnlyDictionary<string, int> replaceableAllowances =
            LootSession.CreateReplaceableBackpackAllowances();

        if (!TravelManager.Instance.TryPlaceSelectedBackpackLootAt(
            reward.Item,
            requestedCount,
            targetX,
            targetY,
            removableInstanceIds,
            replaceableAllowances,
            out ExpeditionBackpackReplacementResult result
        ))
        {
            return false;
        }

        if (!LootSession.TryApplyManualReplacement(entryId, result))
        {
            Debug.LogError(
                "BattleController: exact runtime backpack placement " +
                "succeeded, but its loot ledger could not be applied."
            );
            return false;
        }

        Debug.Log(
            $"Battle loot placed exactly: {reward.Item.itemName} " +
            $"x{result.AddedCount} at ({targetX}, {targetY}); " +
            $"pending x{LootSession.TotalPendingCount}."
        );

        OnBattleStateChanged?.Invoke();
        return true;
    }

    public bool TryMergePendingLootInto(
        string entryId,
        int requestedCount,
        string targetInstanceId
    )
    {
        if (CurrentPhase != BattlePhase.Victory ||
            LootSession == null ||
            TravelManager.Instance == null)
        {
            return false;
        }

        BattleLootReward reward = LootSession.FindReward(entryId);

        if (reward == null ||
            reward.Item == null ||
            requestedCount <= 0 ||
            requestedCount > reward.PendingCount)
        {
            return false;
        }

        if (!TravelManager.Instance.TryMergeSelectedBackpackLootInto(
            reward.Item,
            requestedCount,
            targetInstanceId,
            out ExpeditionBackpackAddResult result
        ))
        {
            return false;
        }

        if (!LootSession.TryStorePending(entryId, result))
        {
            Debug.LogError(
                "BattleController: exact stack merge succeeded, " +
                "but its loot ledger could not be applied."
            );
            return false;
        }

        Debug.Log(
            $"Battle loot stacked exactly: {reward.Item.itemName} " +
            $"x{result.AddedCount}; pending " +
            $"x{LootSession.TotalPendingCount}."
        );

        OnBattleStateChanged?.Invoke();
        return true;
    }

    public int AutoStorePendingLoot(
        string entryId,
        int requestedCount
    )
    {
        if (CurrentPhase != BattlePhase.Victory ||
            LootSession == null ||
            TravelManager.Instance == null)
        {
            return 0;
        }

        BattleLootReward reward = LootSession.FindReward(entryId);

        if (reward == null ||
            reward.Item == null ||
            requestedCount <= 0 ||
            reward.PendingCount <= 0)
        {
            return 0;
        }

        int amountToTry = Mathf.Min(
            requestedCount,
            reward.PendingCount
        );
        ExpeditionBackpackAddResult addResult =
            TravelManager.Instance
                .AddSelectedBackpackItemWithReceipt(
                    reward.Item,
                    amountToTry
                );

        if (addResult.AddedCount <= 0)
            return 0;

        if (!LootSession.TryStorePending(entryId, addResult))
        {
            Debug.LogError(
                "BattleController: pending auto-store succeeded, " +
                "but its loot ledger could not be applied."
            );
            return 0;
        }

        Debug.Log(
            $"Battle pending loot auto-stored: " +
            $"{reward.Item.itemName} " +
            $"x{addResult.AddedCount}; pending " +
            $"x{reward.PendingCount}."
        );

        OnBattleStateChanged?.Invoke();
        return addResult.AddedCount;
    }

    private void CompleteVictory()
    {
        if (CurrentPhase == BattlePhase.Victory)
            return;

        CurrentPhase = BattlePhase.Victory;
        ResolveVictoryLootOnce();

        Debug.Log("Battle phase: Victory.");
        OnBattleStateChanged?.Invoke();
    }

    private void ResolveVictoryLootOnce()
    {
        if (lootResolutionProgress !=
            VictoryLootResolutionProgress.NotStarted)
        {
            return;
        }

        lootResolutionProgress =
            VictoryLootResolutionProgress.Resolving;

        try
        {
            ResolveVictoryLoot();
            lootResolutionProgress =
                VictoryLootResolutionProgress.Completed;
        }
        catch (Exception exception)
        {
            lootResolutionProgress =
                VictoryLootResolutionProgress.Failed;
            Debug.LogException(exception);
        }

        if (lootResolutionProgress ==
            VictoryLootResolutionProgress.Completed)
        {
            try
            {
                OnVictoryLootResolved?.Invoke(LootSession);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }
    }

    private void ResolveVictoryLoot()
    {
        IReadOnlyDictionary<string, int> replaceableSnapshot =
            TravelManager.Instance != null
                ? TravelManager.Instance
                    .CreateSelectedBackpackReplaceableSnapshot()
                : new Dictionary<string, int>();

        var generatedByItem = new Dictionary<ItemData, int>();
        var itemOrder = new List<ItemData>();
        int defeatedEnemyCount = 0;

        foreach (var enemy in enemies)
        {
            if (enemy == null || enemy.IsAlive || enemy.Data == null)
                continue;

            defeatedEnemyCount++;

            if (enemy.Data.LootTable == null)
                continue;

            foreach (var entry in enemy.Data.LootTable)
            {
                if (entry?.item == null)
                {
                    Debug.LogWarning(
                        $"Battle loot: {enemy.Data.displayName} has " +
                        "an entry without ItemData."
                    );
                    continue;
                }

                float chance = Mathf.Clamp(
                    entry.dropChancePercent,
                    0f,
                    100f
                );
                float roll = UnityEngine.Random.Range(0f, 100f);
                bool dropped =
                    chance >= 100f ||
                    (chance > 0f && roll < chance);

                if (!dropped)
                {
                    Debug.Log(
                        $"Loot roll: {enemy.Data.displayName} -> " +
                        $"{entry.item.itemName} | {chance:0.#}% | " +
                        $"roll {roll:0.#} | no drop."
                    );
                    continue;
                }

                int minimumCount = Mathf.Max(1, entry.minCount);
                int maximumCount = Mathf.Max(
                    minimumCount,
                    entry.maxCount
                );
                int generatedCount = minimumCount == maximumCount
                    ? minimumCount
                    : UnityEngine.Random.Range(
                        minimumCount,
                        maximumCount + 1
                    );

                if (!generatedByItem.ContainsKey(entry.item))
                {
                    generatedByItem.Add(entry.item, 0);
                    itemOrder.Add(entry.item);
                }

                long aggregatedCount =
                    (long)generatedByItem[entry.item] + generatedCount;
                generatedByItem[entry.item] = (int)Math.Min(
                    int.MaxValue,
                    aggregatedCount
                );

                Debug.Log(
                    $"Loot roll: {enemy.Data.displayName} -> " +
                    $"{entry.item.itemName} | {chance:0.#}% | " +
                    $"roll {roll:0.#} | x{generatedCount}."
                );
            }
        }

        var rewards = new List<BattleLootReward>();

        foreach (var item in itemOrder)
        {
            int generatedCount = generatedByItem[item];
            ExpeditionBackpackAddResult addResult =
                TravelManager.Instance != null
                ? TravelManager.Instance
                    .AddSelectedBackpackItemWithReceipt(
                    item,
                    generatedCount
                )
                : new ExpeditionBackpackAddResult(
                    generatedCount,
                    0,
                    null
                );

            rewards.Add(
                new BattleLootReward(
                    item,
                    generatedCount,
                    addResult.AddedCount,
                    addResult.Allocations
                )
            );
        }

        string encounterId =
            TravelManager.Instance?.CurrentEncounter?.encounterId;

        LootSession = new BattleLootResolutionSession(
            encounterId,
            defeatedEnemyCount,
            rewards,
            replaceableSnapshot
        );

        Debug.Log(
            $"Battle loot session {LootSession.SessionId}: " +
            $"encounter '{LootSession.EncounterId}', " +
            $"defeated {LootSession.DefeatedEnemyCount}, " +
            $"generated x{LootSession.TotalGeneratedCount}, " +
            $"stored x{LootSession.TotalStoredCount}, " +
            $"pending x{LootSession.TotalPendingCount}, " +
            $"state {LootSession.State}."
        );

        foreach (var reward in LootSession.Rewards)
        {
            Debug.Log(
                $"Battle loot: {reward.Item.itemName} | " +
                $"generated x{reward.GeneratedCount}, " +
                $"stored x{reward.StoredCount}, " +
                $"pending x{reward.PendingCount}."
            );
        }
    }

    private void ApplyCardEffects(
        BattleCardData card,
        int selectedTargetIndex
    )
    {
        var targets = GetEnemyTargets(card, selectedTargetIndex);

        foreach (var effect in card.Effects)
        {
            if (effect == null)
                continue;

            if (effect.effectType != BattleCardEffectType.Damage)
                continue;

            float rawDamage = CalculateRawDamage(card, effect.value);

            foreach (var target in targets)
            {
                float damage = target.TakeDamage(rawDamage);
                OnEnemyDamaged?.Invoke(target, damage);

                Debug.Log(
                    $"{card.displayName} -> " +
                    $"{target.Data.displayName} [{target.FormationSlot}]: " +
                    $"{damage:0.#} damage, HP " +
                    $"{target.CurrentHealth:0.#}/{target.MaxHealth:0.#}."
                );
            }
        }
    }

    private List<BattleEnemyState> GetEnemyTargets(
        BattleCardData card,
        int selectedTargetIndex
    )
    {
        var targets = new List<BattleEnemyState>();

        if (selectedTargetIndex < 0 ||
            selectedTargetIndex >= enemies.Count)
        {
            return targets;
        }

        BattleEnemyState selectedTarget = enemies[selectedTargetIndex];

        if (card.targetType == BattleCardTargetType.AllEnemies)
        {
            foreach (var enemy in enemies)
            {
                if (enemy.IsAlive)
                    targets.Add(enemy);
            }

            return targets;
        }

        if (selectedTarget.IsAlive)
            targets.Add(selectedTarget);

        if (card.targetType !=
            BattleCardTargetType.SelectedEnemyAndAdjacent)
        {
            return targets;
        }

        foreach (var enemy in enemies)
        {
            if (!enemy.IsAlive || enemy == selectedTarget)
                continue;

            if (AreFormationSlotsAdjacent(
                selectedTarget.FormationSlot,
                enemy.FormationSlot
            ))
            {
                targets.Add(enemy);
            }
        }

        return targets;
    }

    private bool AreFormationSlotsAdjacent(
        BattleFormationSlot first,
        BattleFormationSlot second
    )
    {
        return first switch
        {
            BattleFormationSlot.FrontLeft =>
                second == BattleFormationSlot.FrontCenter ||
                second == BattleFormationSlot.BackLeft,

            BattleFormationSlot.FrontCenter =>
                second == BattleFormationSlot.FrontLeft ||
                second == BattleFormationSlot.FrontRight ||
                second == BattleFormationSlot.BackLeft ||
                second == BattleFormationSlot.BackRight,

            BattleFormationSlot.FrontRight =>
                second == BattleFormationSlot.FrontCenter ||
                second == BattleFormationSlot.BackRight,

            BattleFormationSlot.BackLeft =>
                second == BattleFormationSlot.FrontLeft ||
                second == BattleFormationSlot.FrontCenter ||
                second == BattleFormationSlot.BackRight,

            BattleFormationSlot.BackRight =>
                second == BattleFormationSlot.FrontCenter ||
                second == BattleFormationSlot.FrontRight ||
                second == BattleFormationSlot.BackLeft,

            _ => false
        };
    }

    private void RunEnemyTurn()
    {
        SelectedCard = null;
        CurrentPhase = BattlePhase.EnemyTurn;
        Debug.Log("Battle phase: EnemyTurn.");

        foreach (var enemy in enemies)
        {
            if (!enemy.IsAlive)
                continue;

            float damage = Player.TakeDamage(enemy.BaseDamage);

            Debug.Log(
                $"{enemy.Data.displayName} -> Player: " +
                $"{damage:0.#} damage, HP " +
                $"{Player.CurrentHealth:0.#}/{Player.MaxHealth:0.#}."
            );

            if (!Player.IsAlive)
            {
                CurrentPhase = BattlePhase.Defeat;
                Debug.Log("Battle phase: Defeat.");
                OnBattleStateChanged?.Invoke();
                return;
            }
        }

        CardsPlayedThisTurn = 0;
        runtimeDeck?.DrawToHand(TargetHandSize);
        CurrentPhase = BattlePhase.PlayerTurn;
        Debug.Log("Battle phase: PlayerTurn.");
        OnBattleStateChanged?.Invoke();
    }

    private bool HasLivingEnemies()
    {
        foreach (var enemy in enemies)
        {
            if (enemy.IsAlive)
                return true;
        }

        return false;
    }
}
