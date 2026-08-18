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

    public event Action OnBattleStateChanged;

    private readonly List<BattleEnemyState> enemies = new();
    private BattleDeckRuntime runtimeDeck;

    private void OnEnable()
    {
        if (GameManager.Instance != null &&
            GameManager.Instance.CurrentState == GameState.Battle)
        {
            InitializeBattle();
        }
    }

    private void OnDisable()
    {
        CurrentPhase = BattlePhase.None;
        Player = null;
        SelectedCard = null;
        runtimeDeck = null;
        enemies.Clear();
        CardsPlayedThisTurn = 0;
    }

    private void InitializeBattle()
    {
        CurrentPhase = BattlePhase.Setup;
        SelectedCard = null;
        runtimeDeck = null;
        enemies.Clear();
        CardsPlayedThisTurn = 0;

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
            CurrentPhase = BattlePhase.Victory;
            Debug.Log("Battle phase: Victory.");
            OnBattleStateChanged?.Invoke();
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
        if (card?.requiredBackpackItem is not AmmoData ammo)
            return Mathf.Max(0f, cardDamage);

        WeaponData weapon = GetCompatibleEquippedWeapon(card);

        if (weapon == null)
            return 0f;

        float maximumDamage = Mathf.Max(
            0f,
            weapon.damage + ammo.damageModifier
        );

        float spread = Mathf.Clamp01(
            weapon.damageSpreadPercent / 100f
        );

        float minimumDamage = maximumDamage * (1f - spread);
        float rawDamage = Mathf.Round(
            UnityEngine.Random.Range(minimumDamage, maximumDamage)
        );

        Debug.Log(
            $"Damage source: {weapon.itemName} + {ammo.itemName} | " +
            $"range {minimumDamage:0.#}-{maximumDamage:0.#}, " +
            $"roll {rawDamage:0.#}."
        );

        return Mathf.Max(0f, rawDamage);
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

        if (TravelManager.Instance == null)
        {
            Debug.LogError("BattleController: TravelManager is missing.");
            return;
        }

        Debug.Log("Battle victory confirmed. Continuing travel.");
        TravelManager.Instance.ResumeTravelAfterBattle();
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
