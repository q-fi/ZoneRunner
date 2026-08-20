using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BattleLootOverflowUI : MonoBehaviour
{
    private const float PendingCardWidth = 170f;
    private const float PendingCardHeight = 116f;
    private const float PendingDoubleClickSeconds = 0.4f;

    private sealed class PendingVisualChunk
    {
        public long Id { get; }
        public string EntryId { get; }
        public int Count { get; set; }

        public PendingVisualChunk(long id, string entryId, int count)
        {
            Id = id;
            EntryId = entryId;
            Count = count;
        }
    }

    [SerializeField] private BattleController battleController;
    [SerializeField] private GameObject overflowPanel;
    [SerializeField] private TMP_Text pendingLootText;

    [Header("Pending Item Icons")]
    [SerializeField] private RectTransform pendingItemsContainer;
    [SerializeField] private GameObject itemIconPrefab;
    [SerializeField] private ExpeditionBackpackGridUI
        runtimeBackpackGrid;

    [Header("Actions")]
    [SerializeField] private Button leaveAllButton;
    [SerializeField] private Button manualReplaceButton;
    [SerializeField] private Button autoReplaceButton;
    [SerializeField] private Button confirmLootButton;
    [SerializeField] private Button splitPendingButton;
    [SerializeField] private StackQuantityPickerUI quantityPicker;

    private string selectedPendingEntryId;
    private long selectedPendingChunkId = -1;
    private int selectedPendingCount;
    private readonly Dictionary<string, List<PendingVisualChunk>>
        pendingVisualChunks = new(StringComparer.Ordinal);
    private string pendingVisualChunksSessionId;
    private long nextPendingVisualChunkId = 1;
    private readonly HashSet<string> selectedBackpackItemIds = new();
    private string lastPendingClickEntryId;
    private long lastPendingClickChunkId = -1;
    private float lastPendingClickTime = -10f;
    private string interactionMessage;
    private bool pendingMutationInProgress;
    private TMP_Text manualReplaceButtonLabel;
    private TMP_Text splitPendingButtonLabel;
    private bool placementModeActive;
    private int placementTargetX = -1;
    private int placementTargetY = -1;
    private bool placementTargetValid;
    private string placementMergeTargetInstanceId;

    private void Awake()
    {
        if (battleController == null)
            battleController = GetComponent<BattleController>();

        if (runtimeBackpackGrid == null)
        {
            runtimeBackpackGrid = GetComponentInChildren<
                ExpeditionBackpackGridUI
            >(true);
        }

        if (manualReplaceButton != null)
        {
            manualReplaceButtonLabel =
                manualReplaceButton.GetComponentInChildren<TMP_Text>(true);
        }

        if (splitPendingButton != null)
        {
            splitPendingButtonLabel =
                splitPendingButton.GetComponentInChildren<TMP_Text>(true);
        }
    }

    private void OnEnable()
    {
        if (battleController != null)
        {
            battleController.OnBattleStateChanged -= Refresh;
            battleController.OnBattleStateChanged += Refresh;
        }

        if (leaveAllButton != null)
        {
            leaveAllButton.onClick.RemoveListener(LeaveAllPending);
            leaveAllButton.onClick.AddListener(LeaveAllPending);
        }


        if (manualReplaceButton != null)
        {
            manualReplaceButton.onClick.RemoveListener(
                ReplaceSelected
            );
            manualReplaceButton.onClick.AddListener(
                ReplaceSelected
            );
        }

        if (confirmLootButton != null)
        {
            confirmLootButton.onClick.RemoveListener(
                ConfirmLoot
            );
            confirmLootButton.onClick.AddListener(
                ConfirmLoot
            );
        }

        if (splitPendingButton != null)
        {
            splitPendingButton.onClick.RemoveListener(
                HandleSplitPendingAction
            );
            splitPendingButton.onClick.AddListener(
                HandleSplitPendingAction
            );
        }

        if (runtimeBackpackGrid != null)
        {
            runtimeBackpackGrid.OnItemClicked -=
                ToggleBackpackItemSelection;
            runtimeBackpackGrid.OnItemClicked +=
                ToggleBackpackItemSelection;
            runtimeBackpackGrid.OnPlacementCellClicked -=
                SelectPlacementCell;
            runtimeBackpackGrid.OnPlacementCellClicked +=
                SelectPlacementCell;
        }

        Refresh();
    }

    private void OnDisable()
    {
        if (battleController != null)
            battleController.OnBattleStateChanged -= Refresh;

        if (leaveAllButton != null)
            leaveAllButton.onClick.RemoveListener(LeaveAllPending);

        if (manualReplaceButton != null)
        {
            manualReplaceButton.onClick.RemoveListener(
                ReplaceSelected
            );
        }

        if (confirmLootButton != null)
        {
            confirmLootButton.onClick.RemoveListener(
                ConfirmLoot
            );
        }

        if (splitPendingButton != null)
        {
            splitPendingButton.onClick.RemoveListener(
                HandleSplitPendingAction
            );
        }

        if (runtimeBackpackGrid != null)
        {
            runtimeBackpackGrid.OnItemClicked -=
                ToggleBackpackItemSelection;
            runtimeBackpackGrid.OnPlacementCellClicked -=
                SelectPlacementCell;
        }

        if (overflowPanel != null)
            overflowPanel.SetActive(false);

        quantityPicker?.Close();
        ExitPlacementMode();
        ResetPendingClickTracker();
    }

    private void LeaveAllPending()
    {
        ResetPendingClickTracker();
        quantityPicker?.Close();
        ExitPlacementMode();
        battleController?.LeaveAllPendingLoot();
    }

    private void ConfirmLoot()
    {
        ResetPendingClickTracker();
        quantityPicker?.Close();
        ExitPlacementMode();
        battleController?.AcknowledgeLootResolution();
    }

    private void SelectPendingReward(
        string entryId,
        long chunkId
    )
    {
        PendingVisualChunk chunk = FindPendingVisualChunk(
            entryId,
            chunkId
        );

        if (chunk == null)
        {
            ClearPendingSelection();
            selectedBackpackItemIds.Clear();
            interactionMessage =
                "Pending loot changed. Select the stack again.";
            Refresh();
            return;
        }

        bool isDoubleClick =
            lastPendingClickEntryId == entryId &&
            lastPendingClickChunkId == chunkId &&
            Time.unscaledTime - lastPendingClickTime <=
                PendingDoubleClickSeconds;

        lastPendingClickEntryId = entryId;
        lastPendingClickChunkId = chunkId;
        lastPendingClickTime = Time.unscaledTime;

        if (isDoubleClick)
        {
            ResetPendingClickTracker();
            quantityPicker?.Close();
            AutoStorePendingChunk(
                entryId,
                chunkId
            );
            return;
        }

        bool isAlreadySelected =
            selectedPendingEntryId == entryId &&
            selectedPendingChunkId == chunkId;

        if (isAlreadySelected)
        {
            ClearPendingSelection();
        }
        else
        {
            ExitPlacementMode();
            selectedPendingEntryId = entryId;
            selectedPendingChunkId = chunkId;
            selectedPendingCount = chunk.Count;
            BeginPlacementMode();
        }

        quantityPicker?.Close();
        interactionMessage = null;
        Refresh();
    }

    private void AutoStorePendingChunk(
        string entryId,
        long chunkId
    )
    {
        if (battleController == null)
            return;

        PendingVisualChunk chunk = FindPendingVisualChunk(
            entryId,
            chunkId
        );

        if (chunk == null || chunk.Count <= 0)
            return;

        int requestedCount = chunk.Count;

        BattleLootReward reward =
            battleController.LootSession?.FindReward(entryId);
        int pendingBefore = reward?.PendingCount ?? 0;
        string itemName = reward?.Item != null
            ? reward.Item.itemName
            : "item";
        int mergedCount;
        pendingMutationInProgress = true;

        try
        {
            mergedCount = battleController.AutoStorePendingLoot(
                entryId,
                requestedCount
            );
        }
        finally
        {
            pendingMutationInProgress = false;
        }

        int pendingAfter = reward?.PendingCount ?? 0;
        bool modelUpdated =
            mergedCount >= 0 &&
            pendingBefore - pendingAfter == mergedCount &&
            (mergedCount == 0 || ConsumePendingVisualChunk(
                entryId,
                chunkId,
                mergedCount
            ));

        if (!modelUpdated)
            pendingVisualChunks.Remove(entryId);

        if (mergedCount > 0)
        {
            interactionMessage =
                $"Stored {itemName} x{mergedCount} in available " +
                "stack or backpack space.";
            ClearPendingSelection();
            selectedBackpackItemIds.Clear();
        }
        else
        {
            interactionMessage =
                $"No stack or backpack space for {itemName}.";
        }

        Refresh();
    }

    private void ToggleBackpackItemSelection(ItemInstance item)
    {
        ResetPendingClickTracker();

        if (battleController == null || item?.Data == null)
            return;

        if (selectedBackpackItemIds.Contains(item.InstanceId))
        {
            selectedBackpackItemIds.Remove(item.InstanceId);
            placementMergeTargetInstanceId = null;
            RevalidatePlacementTarget();
            interactionMessage =
                $"{item.Data.itemName} is no longer marked for discard.";
            Refresh();
            return;
        }

        if (TrySelectCompatibleStackTarget(item))
            return;

        if (!battleController
            .CanSelectBackpackItemForLootReplacement(item))
        {
            if (placementModeActive &&
                TravelManager.Instance != null &&
                TravelManager.Instance.TryGetSelectedBackpackItemPosition(
                    item,
                    out int blockedX,
                    out int blockedY
                ))
            {
                placementMergeTargetInstanceId = null;
                placementTargetX = blockedX;
                placementTargetY = blockedY;
                RevalidatePlacementTarget();
                interactionMessage =
                    $"{item.Data.itemName} cannot be removed, and " +
                    "the selected loot cannot be placed over it.";
                Refresh();
            }

            return;
        }

        placementMergeTargetInstanceId = null;

        selectedBackpackItemIds.Add(item.InstanceId);

        if (placementModeActive &&
            TravelManager.Instance != null &&
            TravelManager.Instance.TryGetSelectedBackpackItemPosition(
                item,
                out int itemX,
                out int itemY
            ))
        {
            placementTargetX = itemX;
            placementTargetY = itemY;
            RevalidatePlacementTarget();
            interactionMessage = placementTargetValid
                ? $"{item.Data.itemName} will be discarded. " +
                    "Its top-left cell is ready for placement."
                : $"{item.Data.itemName} will be discarded, but " +
                    "the selected loot does not fit at its top-left cell.";
        }
        else
        {
            interactionMessage =
                $"{item.Data.itemName} is marked for discard.";
        }

        RevalidatePlacementTarget();
        Refresh();
    }

    private bool TrySelectCompatibleStackTarget(ItemInstance item)
    {
        if (!placementModeActive ||
            battleController == null ||
            item?.Data == null ||
            !item.Data.isStackable)
        {
            return false;
        }

        BattleLootReward reward = battleController.LootSession
            ?.FindReward(selectedPendingEntryId);

        if (reward?.Item != item.Data)
            return false;

        placementMergeTargetInstanceId = item.InstanceId;
        selectedBackpackItemIds.Clear();

        if (TravelManager.Instance != null &&
            TravelManager.Instance.TryGetSelectedBackpackItemPosition(
                item,
                out int itemX,
                out int itemY
            ))
        {
            placementTargetX = itemX;
            placementTargetY = itemY;
        }

        RevalidatePlacementTarget();
        int maximumStackSize = Mathf.Max(1, item.Data.maxStackSize);
        int freeSpace = Mathf.Max(
            0,
            maximumStackSize - item.StackCount
        );
        interactionMessage = placementTargetValid
            ? $"Stack {reward.Item.itemName} x{selectedPendingCount} " +
                $"into x{item.StackCount}. Press STACK HERE."
            : $"This stack has space for x{freeSpace}. " +
                "Split the pending stack to that amount or choose another cell.";
        Refresh();
        return true;
    }

    private void ReplaceSelected()
    {
        ResetPendingClickTracker();
        quantityPicker?.Close();

        if (!placementModeActive)
        {
            DiscardSelectedBackpackItems();
            return;
        }

        if (!placementTargetValid)
            return;

        PlaceSelectedAtTarget();
    }

    private void DiscardSelectedBackpackItems()
    {
        if (battleController == null ||
            selectedBackpackItemIds.Count == 0)
        {
            return;
        }

        var selectedIds = new List<string>(selectedBackpackItemIds);
        bool discarded;
        pendingMutationInProgress = true;

        try
        {
            discarded = battleController
                .TryDiscardSelectedBackpackLoot(selectedIds);
        }
        finally
        {
            pendingMutationInProgress = false;
        }

        if (discarded)
        {
            selectedBackpackItemIds.Clear();
            interactionMessage =
                $"Discarded {selectedIds.Count} backpack stack(s).";
        }
        else
        {
            interactionMessage =
                "The selected backpack items could not be discarded.";
        }

        Refresh();
    }

    private void BeginPlacementMode()
    {
        if (battleController == null ||
            string.IsNullOrEmpty(selectedPendingEntryId) ||
            selectedPendingCount <= 0)
        {
            return;
        }

        PendingVisualChunk chunk = FindPendingVisualChunk(
            selectedPendingEntryId,
            selectedPendingChunkId
        );
        BattleLootReward reward = battleController.LootSession
            ?.FindReward(selectedPendingEntryId);

        if (chunk == null ||
            chunk.Count != selectedPendingCount ||
            reward?.Item == null)
        {
            return;
        }

        placementModeActive = true;
        placementTargetX = -1;
        placementTargetY = -1;
        placementTargetValid = false;
        placementMergeTargetInstanceId = null;
        interactionMessage =
            "Tap a backpack cell to choose the new stack position.";
    }

    private void SelectPlacementCell(int x, int y)
    {
        if (!placementModeActive)
            return;

        placementMergeTargetInstanceId = null;
        placementTargetX = x;
        placementTargetY = y;
        RevalidatePlacementTarget();
        interactionMessage = placementTargetValid
            ? $"Cell ({x + 1}, {y + 1}) is valid. Press PLACE HERE."
            : $"Item cannot fit at cell ({x + 1}, {y + 1}).";
        Refresh();
    }

    private void RevalidatePlacementTarget()
    {
        bool hasValidSelection =
            placementModeActive &&
            placementTargetX >= 0 &&
            placementTargetY >= 0 &&
            battleController != null &&
            !string.IsNullOrEmpty(selectedPendingEntryId) &&
            selectedPendingCount > 0;

        if (!hasValidSelection)
        {
            placementTargetValid = false;
            return;
        }

        placementTargetValid = !string.IsNullOrEmpty(
            placementMergeTargetInstanceId
        )
            ? battleController.CanMergePendingLootInto(
                selectedPendingEntryId,
                selectedPendingCount,
                placementMergeTargetInstanceId
            )
            : battleController.CanPlacePendingLootAt(
                selectedPendingEntryId,
                selectedPendingCount,
                placementTargetX,
                placementTargetY,
                selectedBackpackItemIds
            );
    }

    private void PlaceSelectedAtTarget()
    {
        if (!string.IsNullOrEmpty(placementMergeTargetInstanceId))
        {
            MergeSelectedIntoTargetStack();
            return;
        }

        PendingVisualChunk chunk = FindPendingVisualChunk(
            selectedPendingEntryId,
            selectedPendingChunkId
        );

        if (chunk == null ||
            chunk.Count != selectedPendingCount ||
            !placementTargetValid)
        {
            RevalidatePlacementTarget();
            Refresh();
            return;
        }

        var selectedIds = new List<string>(
            selectedBackpackItemIds
        );
        string entryId = selectedPendingEntryId;
        long chunkId = selectedPendingChunkId;
        int chunkCount = selectedPendingCount;
        int targetX = placementTargetX;
        int targetY = placementTargetY;
        BattleLootReward reward = battleController.LootSession
            ?.FindReward(entryId);
        string itemName = reward?.Item != null
            ? reward.Item.itemName
            : "item";
        int pendingBefore = reward?.PendingCount ?? 0;
        bool placed;
        pendingMutationInProgress = true;

        try
        {
            placed = battleController.TryPlacePendingLootAt(
                entryId,
                chunkCount,
                targetX,
                targetY,
                selectedIds
            );
        }
        finally
        {
            pendingMutationInProgress = false;
        }

        int pendingAfter = reward?.PendingCount ?? 0;

        if (!placed)
        {
            if (pendingAfter != pendingBefore)
                pendingVisualChunks.Remove(entryId);

            RevalidatePlacementTarget();
            interactionMessage =
                "Placement changed or is no longer valid. Choose a cell again.";
            Refresh();
            return;
        }

        if (pendingBefore - pendingAfter != chunkCount ||
            !ConsumePendingVisualChunk(entryId, chunkId, chunkCount))
        {
            pendingVisualChunks.Remove(entryId);
        }
        ExitPlacementMode();
        ClearPendingSelection();
        selectedBackpackItemIds.Clear();
        interactionMessage =
            $"Placed {itemName} x{chunkCount} at cell " +
            $"({targetX + 1}, {targetY + 1}) as a separate stack.";
        Refresh();
    }

    private void MergeSelectedIntoTargetStack()
    {
        PendingVisualChunk chunk = FindPendingVisualChunk(
            selectedPendingEntryId,
            selectedPendingChunkId
        );

        if (chunk == null ||
            chunk.Count != selectedPendingCount ||
            !placementTargetValid ||
            string.IsNullOrEmpty(placementMergeTargetInstanceId))
        {
            RevalidatePlacementTarget();
            Refresh();
            return;
        }

        string entryId = selectedPendingEntryId;
        long chunkId = selectedPendingChunkId;
        int chunkCount = selectedPendingCount;
        string targetInstanceId = placementMergeTargetInstanceId;
        BattleLootReward reward = battleController.LootSession
            ?.FindReward(entryId);
        string itemName = reward?.Item != null
            ? reward.Item.itemName
            : "item";
        int pendingBefore = reward?.PendingCount ?? 0;
        bool merged;
        pendingMutationInProgress = true;

        try
        {
            merged = battleController.TryMergePendingLootInto(
                entryId,
                chunkCount,
                targetInstanceId
            );
        }
        finally
        {
            pendingMutationInProgress = false;
        }

        int pendingAfter = reward?.PendingCount ?? 0;

        if (!merged)
        {
            if (pendingAfter != pendingBefore)
                pendingVisualChunks.Remove(entryId);

            RevalidatePlacementTarget();
            interactionMessage =
                "That stack changed or no longer has enough space.";
            Refresh();
            return;
        }

        if (pendingBefore - pendingAfter != chunkCount ||
            !ConsumePendingVisualChunk(entryId, chunkId, chunkCount))
        {
            pendingVisualChunks.Remove(entryId);
        }

        ExitPlacementMode();
        ClearPendingSelection();
        selectedBackpackItemIds.Clear();
        interactionMessage =
            $"Stacked {itemName} x{chunkCount} into the selected stack.";
        Refresh();
    }

    private void ExitPlacementMode()
    {
        placementModeActive = false;
        placementTargetX = -1;
        placementTargetY = -1;
        placementTargetValid = false;
        placementMergeTargetInstanceId = null;
        runtimeBackpackGrid?.SetPlacementState(
            false,
            null,
            -1,
            -1,
            false
        );
    }

    private void Refresh()
    {
        if (pendingMutationInProgress)
            return;

        BattleLootResolutionSession session =
            battleController?.LootSession;

        bool shouldShow =
            battleController != null &&
            battleController.CurrentPhase == BattlePhase.Victory &&
            session != null &&
            session.RequiresPlayerReview &&
            !session.IsAcknowledged;

        if (overflowPanel != null)
            overflowPanel.SetActive(shouldShow);

        if (!shouldShow)
        {
            ClearPendingSelection();
            selectedBackpackItemIds.Clear();
            interactionMessage = null;
            quantityPicker?.Close();
            ExitPlacementMode();
            ClearPendingVisualChunks();
            ResetPendingClickTracker();
            return;
        }

        EnsurePendingVisualChunks(session);
        ValidateSelection(session);

        if (!placementModeActive &&
            !string.IsNullOrEmpty(selectedPendingEntryId) &&
            selectedPendingCount > 0)
        {
            BeginPlacementMode();
        }

        RevalidatePlacementTarget();
        RefreshRuntimeBackpackSelection();
        RefreshRuntimePlacementState(session);

        if (leaveAllButton != null)
            leaveAllButton.interactable = session.HasPendingLoot;

        if (manualReplaceButton != null)
        {
            manualReplaceButton.interactable = placementModeActive
                ? placementTargetValid
                : selectedBackpackItemIds.Count > 0;

            if (manualReplaceButtonLabel != null)
            {
                manualReplaceButtonLabel.text = placementModeActive
                    ? (!string.IsNullOrEmpty(
                        placementMergeTargetInstanceId
                    )
                        ? "STACK HERE"
                        : "PLACE HERE")
                    : "DISCARD SELECTED";
            }
        }

        if (autoReplaceButton != null)
            autoReplaceButton.interactable = false;

        if (confirmLootButton != null)
            confirmLootButton.interactable = session.IsResolved;

        if (splitPendingButton != null)
        {
            splitPendingButton.interactable =
                session.HasPendingLoot &&
                !string.IsNullOrEmpty(selectedPendingEntryId) &&
                selectedPendingCount > 1;

            if (splitPendingButtonLabel != null)
                splitPendingButtonLabel.text = "SPLIT";
        }

        if (pendingLootText == null)
        {
            RefreshPendingItemIcons(session);
            return;
        }

        var text = new StringBuilder();

        if (session.HasPendingLoot)
            text.AppendLine("NO SPACE FOR THESE ITEMS");
        else
            text.AppendLine("LOOT READY");

        text.Append("Already stored: ");
        text.Append(session.TotalStoredCount);
        text.Append(" / ");
        text.Append(session.TotalGeneratedCount);
        text.AppendLine();

        if (session.HasPendingLoot)
        {
            text.AppendLine(
                "Tap PENDING, then tap LOOT/NEW to discard (× undoes)."
            );
            text.Append(
                "Double-click PENDING to add it automatically " +
                "to the backpack."
            );
            text.AppendLine();
            text.Append(
                "Tap a cell for a separate stack; green fits, red is blocked. " +
                "Press PLACE HERE to confirm."
            );
            text.AppendLine();
            text.AppendLine(
                "Tap the same stackable item and press STACK HERE to merge."
            );
            text.Append(
                "Or select LOOT/NEW without PENDING and press DISCARD SELECTED."
            );
        }
        else
        {
            text.AppendLine(
                "Select LOOT/NEW and press DISCARD SELECTED if needed."
            );
            text.Append(
                "Review the backpack, then press CONFIRM LOOT."
            );
        }

        BattleLootReward selectedReward =
            session.FindReward(selectedPendingEntryId);

        if (selectedReward != null)
        {
            text.AppendLine();
            text.Append("Selected: ");
            text.Append(selectedReward.Item.itemName);
            text.Append(" x");
            text.Append(selectedPendingCount);
            text.Append(" | total pending x");
            text.Append(selectedReward.PendingCount);
            text.Append(" | backpack stacks: ");
            text.Append(selectedBackpackItemIds.Count);
        }
        else if (selectedBackpackItemIds.Count > 0)
        {
            text.AppendLine();
            text.Append("Selected backpack stacks to discard: ");
            text.Append(selectedBackpackItemIds.Count);
        }

        if (!string.IsNullOrEmpty(interactionMessage))
        {
            text.AppendLine();
            text.Append(interactionMessage);
        }

        pendingLootText.text = text.ToString();
        RefreshPendingItemIcons(session);

    }

    private void ValidateSelection(
        BattleLootResolutionSession session
    )
    {
        bool hasPendingSelection =
            !string.IsNullOrEmpty(selectedPendingEntryId) &&
            selectedPendingChunkId > 0 &&
            selectedPendingCount > 0;

        if (hasPendingSelection)
        {
            BattleLootReward selectedReward =
                session.FindReward(selectedPendingEntryId);
            PendingVisualChunk selectedChunk = FindPendingVisualChunk(
                selectedPendingEntryId,
                selectedPendingChunkId
            );

            if (selectedReward == null ||
                selectedReward.PendingCount <= 0 ||
                selectedChunk == null ||
                selectedChunk.Count <= 0 ||
                selectedChunk.Count != selectedPendingCount)
            {
                quantityPicker?.Close();
                ClearPendingSelection();
                selectedBackpackItemIds.Clear();
                return;
            }
        }
        else
        {
            selectedPendingEntryId = null;
            selectedPendingChunkId = -1;
            selectedPendingCount = 0;

            if (placementModeActive)
                ExitPlacementMode();
        }

        TravelManager travel = TravelManager.Instance;

        if (travel == null)
        {
            selectedBackpackItemIds.Clear();
            return;
        }

        var validIds = new HashSet<string>();

        foreach (var item in travel.SelectedBackpackItems)
        {
            if (battleController != null &&
                battleController
                    .CanSelectBackpackItemForLootReplacement(item))
            {
                validIds.Add(item.InstanceId);
            }
        }

        selectedBackpackItemIds.RemoveWhere(
            instanceId => !validIds.Contains(instanceId)
        );
    }

    private void RefreshRuntimeBackpackSelection()
    {
        if (runtimeBackpackGrid == null)
            return;

        var selectableIds = new List<string>();
        var newLootIds = new List<string>();
        TravelManager travel = TravelManager.Instance;

        if (travel != null && battleController != null)
        {
            foreach (var item in travel.SelectedBackpackItems)
            {
                if (battleController
                    .CanSelectBackpackItemForLootReplacement(item))
                {
                    selectableIds.Add(item.InstanceId);
                }

                if (battleController.IsCurrentBattleLoot(item))
                    newLootIds.Add(item.InstanceId);
            }
        }

        runtimeBackpackGrid.SetSelectionState(
            selectableIds,
            selectedBackpackItemIds,
            newLootIds
        );
    }

    private void RefreshRuntimePlacementState(
        BattleLootResolutionSession session
    )
    {
        if (runtimeBackpackGrid == null)
            return;

        BattleLootReward reward = placementModeActive
            ? session.FindReward(selectedPendingEntryId)
            : null;

        runtimeBackpackGrid.SetPlacementState(
            placementModeActive && reward?.Item != null,
            reward?.Item,
            placementTargetX,
            placementTargetY,
            placementTargetValid
        );
    }

    private void RefreshPendingItemIcons(
        BattleLootResolutionSession session
    )
    {
        if (pendingItemsContainer == null || itemIconPrefab == null)
            return;

        PreparePendingItemsContainer();

        for (int index = pendingItemsContainer.childCount - 1;
            index >= 0;
            index--)
        {
            GameObject oldIcon =
                pendingItemsContainer.GetChild(index).gameObject;
            oldIcon.SetActive(false);
            Destroy(oldIcon);
        }

        foreach (var reward in session.Rewards)
        {
            if (reward?.Item == null || reward.PendingCount <= 0)
                continue;

            List<PendingVisualChunk> chunks;

            if (!pendingVisualChunks.TryGetValue(
                reward.EntryId,
                out chunks
            ))
            {
                continue;
            }

            foreach (PendingVisualChunk chunk in chunks)
            {
                CreatePendingItemIcon(reward, chunk);
            }
        }

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(
            pendingItemsContainer
        );
    }

    private void CreatePendingItemIcon(
        BattleLootReward reward,
        PendingVisualChunk chunk
    )
    {
        string entryId = reward.EntryId;
        long chunkId = chunk.Id;
        int chunkCount = chunk.Count;

        GameObject icon = Instantiate(
            itemIconPrefab,
            pendingItemsContainer
        );

        RectTransform rect = icon.GetComponent<RectTransform>();

        if (rect != null)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.localScale = Vector3.one;
            rect.sizeDelta = new Vector2(
                PendingCardWidth,
                PendingCardHeight
            );
        }

        ExpeditionItemIconView view =
            icon.GetComponent<ExpeditionItemIconView>();

        if (view == null)
            view = icon.AddComponent<ExpeditionItemIconView>();

        view.Setup(
            reward.Item,
            chunkCount,
            "PENDING",
            true,
            () => SelectPendingReward(
                entryId,
                chunkId
            )
        );
        view.SetSelected(
            entryId == selectedPendingEntryId &&
            chunkId == selectedPendingChunkId
        );
    }

    private static int GetPendingMaximumStackSize(ItemData item)
    {
        return item != null && item.isStackable
            ? Mathf.Max(1, item.maxStackSize)
            : 1;
    }

    private void ClearPendingSelection()
    {
        if (placementModeActive)
            ExitPlacementMode();

        selectedPendingEntryId = null;
        selectedPendingChunkId = -1;
        selectedPendingCount = 0;
    }

    private void OpenQuantityPicker()
    {
        if (quantityPicker == null ||
            battleController == null ||
            string.IsNullOrEmpty(selectedPendingEntryId) ||
            selectedPendingCount <= 1)
        {
            return;
        }

        BattleLootReward reward = battleController.LootSession
            ?.FindReward(selectedPendingEntryId);

        PendingVisualChunk chunk = FindPendingVisualChunk(
            selectedPendingEntryId,
            selectedPendingChunkId
        );

        if (reward?.Item == null || chunk == null || chunk.Count <= 1)
            return;

        ResetPendingClickTracker();
        quantityPicker.Open(
            reward.Item.itemName,
            chunk.Count,
            Mathf.CeilToInt(chunk.Count * 0.5f),
            ApplySelectedPendingAmount
        );
    }

    private void HandleSplitPendingAction()
    {
        OpenQuantityPicker();
    }

    private void ApplySelectedPendingAmount(int amount)
    {
        BattleLootReward reward = battleController?.LootSession
            ?.FindReward(selectedPendingEntryId);
        List<PendingVisualChunk> chunks;
        int chunkIndex;
        PendingVisualChunk chunk;

        if (reward == null ||
            !TryFindPendingVisualChunk(
                selectedPendingEntryId,
                selectedPendingChunkId,
                out chunks,
                out chunkIndex,
                out chunk
            ))
        {
            quantityPicker?.Close();
            ClearPendingSelection();
            selectedBackpackItemIds.Clear();
            interactionMessage =
                "Pending loot changed. Select the stack again.";
            Refresh();
            return;
        }

        if (amount <= 0 || amount >= chunk.Count)
        {
            interactionMessage =
                $"Choose an amount from 1 to {chunk.Count - 1}.";
            Refresh();
            return;
        }

        int originalCount = chunk.Count;
        chunk.Count = amount;
        chunks.Insert(
            chunkIndex + 1,
            new PendingVisualChunk(
                CreatePendingVisualChunkId(),
                selectedPendingEntryId,
                originalCount - amount
            )
        );
        selectedPendingCount = chunk.Count;
        interactionMessage =
            $"Split x{originalCount}: x{amount} + " +
            $"x{originalCount - amount}.";
        ResetPendingClickTracker();
        Refresh();
    }

    private void ResetPendingClickTracker()
    {
        lastPendingClickEntryId = null;
        lastPendingClickChunkId = -1;
        lastPendingClickTime = -10f;
    }

    private void EnsurePendingVisualChunks(
        BattleLootResolutionSession session
    )
    {
        if (!string.Equals(
            pendingVisualChunksSessionId,
            session.SessionId,
            StringComparison.Ordinal
        ))
        {
            pendingVisualChunks.Clear();
            pendingVisualChunksSessionId = session.SessionId;
            nextPendingVisualChunkId = 1;
            ClearPendingSelection();
            selectedBackpackItemIds.Clear();
            quantityPicker?.Close();
            ResetPendingClickTracker();
            interactionMessage = null;
        }

        var liveEntryIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (BattleLootReward reward in session.Rewards)
        {
            if (reward?.Item == null ||
                string.IsNullOrEmpty(reward.EntryId) ||
                reward.PendingCount <= 0)
            {
                continue;
            }

            liveEntryIds.Add(reward.EntryId);
            List<PendingVisualChunk> chunks;

            if (!pendingVisualChunks.TryGetValue(
                    reward.EntryId,
                    out chunks
                ) ||
                !ArePendingVisualChunksValid(reward, chunks))
            {
                RebuildPendingVisualChunks(reward);

                if (selectedPendingEntryId == reward.EntryId)
                {
                    quantityPicker?.Close();
                    ClearPendingSelection();
                    selectedBackpackItemIds.Clear();
                    ResetPendingClickTracker();
                }
            }
        }

        var staleEntryIds = new List<string>();

        foreach (string entryId in pendingVisualChunks.Keys)
        {
            if (!liveEntryIds.Contains(entryId))
                staleEntryIds.Add(entryId);
        }

        foreach (string entryId in staleEntryIds)
        {
            pendingVisualChunks.Remove(entryId);

            if (selectedPendingEntryId == entryId)
            {
                quantityPicker?.Close();
                ClearPendingSelection();
                selectedBackpackItemIds.Clear();
                ResetPendingClickTracker();
            }
        }
    }

    private bool ArePendingVisualChunksValid(
        BattleLootReward reward,
        List<PendingVisualChunk> chunks
    )
    {
        if (chunks == null || chunks.Count == 0)
            return false;

        int maximumStackSize = GetPendingMaximumStackSize(reward.Item);
        long total = 0;
        var chunkIds = new HashSet<long>();

        foreach (PendingVisualChunk chunk in chunks)
        {
            if (chunk == null ||
                chunk.Id <= 0 ||
                !chunkIds.Add(chunk.Id) ||
                chunk.EntryId != reward.EntryId ||
                chunk.Count <= 0 ||
                chunk.Count > maximumStackSize)
            {
                return false;
            }

            total += chunk.Count;
        }

        return total == reward.PendingCount;
    }

    private void RebuildPendingVisualChunks(BattleLootReward reward)
    {
        var chunks = new List<PendingVisualChunk>();
        int maximumStackSize = GetPendingMaximumStackSize(reward.Item);
        int remainingCount = reward.PendingCount;

        while (remainingCount > 0)
        {
            int chunkCount = Mathf.Min(maximumStackSize, remainingCount);
            chunks.Add(new PendingVisualChunk(
                CreatePendingVisualChunkId(),
                reward.EntryId,
                chunkCount
            ));
            remainingCount -= chunkCount;
        }

        pendingVisualChunks[reward.EntryId] = chunks;
    }

    private PendingVisualChunk FindPendingVisualChunk(
        string entryId,
        long chunkId
    )
    {
        List<PendingVisualChunk> chunks;
        int index;
        PendingVisualChunk chunk;

        return TryFindPendingVisualChunk(
            entryId,
            chunkId,
            out chunks,
            out index,
            out chunk
        )
            ? chunk
            : null;
    }

    private bool TryFindPendingVisualChunk(
        string entryId,
        long chunkId,
        out List<PendingVisualChunk> chunks,
        out int index,
        out PendingVisualChunk chunk
    )
    {
        index = -1;
        chunk = null;

        if (string.IsNullOrEmpty(entryId) ||
            chunkId <= 0 ||
            !pendingVisualChunks.TryGetValue(entryId, out chunks))
        {
            chunks = null;
            return false;
        }

        for (int currentIndex = 0;
            currentIndex < chunks.Count;
            currentIndex++)
        {
            if (chunks[currentIndex].Id != chunkId)
                continue;

            index = currentIndex;
            chunk = chunks[currentIndex];
            return true;
        }

        return false;
    }

    private bool ConsumePendingVisualChunk(
        string entryId,
        long chunkId,
        int consumedCount
    )
    {
        List<PendingVisualChunk> chunks;
        int index;
        PendingVisualChunk chunk;

        if (consumedCount <= 0 ||
            !TryFindPendingVisualChunk(
                entryId,
                chunkId,
                out chunks,
                out index,
                out chunk
            ) ||
            consumedCount > chunk.Count)
        {
            return false;
        }

        chunk.Count -= consumedCount;

        if (chunk.Count == 0)
            chunks.RemoveAt(index);

        if (chunks.Count == 0)
            pendingVisualChunks.Remove(entryId);

        return true;
    }

    private long CreatePendingVisualChunkId()
    {
        if (nextPendingVisualChunkId <= 0 ||
            nextPendingVisualChunkId == long.MaxValue)
        {
            nextPendingVisualChunkId = 1;
        }

        return nextPendingVisualChunkId++;
    }

    private void ClearPendingVisualChunks()
    {
        pendingVisualChunks.Clear();
        pendingVisualChunksSessionId = null;
        nextPendingVisualChunkId = 1;
    }

    private void PreparePendingItemsContainer()
    {
        // The horizontal Content must have exactly the Viewport height.
        // Otherwise a MiddleLeft layout can center cards outside the
        // visible area and leave only their top badge on screen.
        pendingItemsContainer.anchorMin = new Vector2(0f, 0f);
        pendingItemsContainer.anchorMax = new Vector2(0f, 1f);
        pendingItemsContainer.pivot = new Vector2(0f, 0.5f);

        Vector2 anchoredPosition =
            pendingItemsContainer.anchoredPosition;
        anchoredPosition.y = 0f;
        pendingItemsContainer.anchoredPosition = anchoredPosition;

        Vector2 containerSize = pendingItemsContainer.sizeDelta;
        containerSize.y = 0f;
        pendingItemsContainer.sizeDelta = containerSize;

        HorizontalLayoutGroup layout =
            pendingItemsContainer.GetComponent<HorizontalLayoutGroup>();

        if (layout == null)
            return;

        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
    }
}
