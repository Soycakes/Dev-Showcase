using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

namespace FactoryGame.Mixed
{
    /// <summary>
    /// Predicts and reconciles client inventory actions.
    /// </summary>
    internal static class LocalPlayerLookup
    {
        public static EntityQuery CreateQuery(EntityManager em) =>
            em.CreateEntityQuery(typeof(PlayerInput), typeof(GhostOwnerIsLocal));

        public static Entity Find(EntityQuery query) =>
            query.IsEmpty ? Entity.Null : query.GetSingletonEntity();
    }

    internal static class ClientPredictedTransactionApplier
    {
        // Applies predicted transaction logic based on transaction type.
        public static void Apply(
            in PredictedInventoryTransaction txn,
            DynamicBuffer<InventoryElement> playerInv,
            ref PlayerHeldItem heldItem,
            DynamicBuffer<PredictedContainerElement> predictedContainer,
            bool hasOpenContainer,
            MachineRuleType containerRuleType
        )
        {
            var containerAsInventory =
                predictedContainer.Reinterpret<InventoryElement>();

            DynamicBuffer<InventoryElement> targetInv =
                txn.InvType == InventoryType.Container
                    ? containerAsInventory
                    : playerInv;
            IInventoryValidator validator =
                txn.InvType == InventoryType.Container
                    ? (
                        containerRuleType == MachineRuleType.Furnace
                            ? (IInventoryValidator)InventoryValidators.Furnace
                            : InventoryValidators.Chest
                      )
                    : InventoryValidators.Chest;

            switch (txn.Kind)
            {
                case PredictedTransactionKind.Click:
                    if (txn.SlotIndex < 0 || txn.SlotIndex >= targetInv.Length)
                        return;
                    InventoryClickSolver.SolveClick(
                        targetInv,
                        ref heldItem,
                        txn.SlotIndex,
                        txn.ClickType,
                        validator
                    );
                    break;

                case PredictedTransactionKind.ShiftClick:
                {
                    if (txn.SlotIndex < 0 || txn.SlotIndex >= targetInv.Length)
                        return;
                    int count = targetInv[txn.SlotIndex].Count;
                    if (txn.ClickType == InventoryClickType.ShiftRightClick)
                    {
                        count = (count + 1) / 2;
                    }

                    InventoryShiftClickRouter.ResolveRoute(
                        txn.InvType,
                        txn.SlotIndex,
                        hasOpenContainer,
                        playerInv.Length,
                        containerAsInventory.Length,
                        out bool targetIsContainer,
                        out int destStart,
                        out int destEnd
                    );

                    DynamicBuffer<InventoryElement> destInv = targetIsContainer
                        ? containerAsInventory
                        : playerInv;
                    IInventoryValidator destValidator =
                        InventoryShiftClickRouter.ResolveDestValidator(
                            targetIsContainer,
                            containerRuleType
                        );

                    InventoryClickSolver.SolveShiftClick(
                        targetInv,
                        destInv,
                        txn.SlotIndex,
                        destStart,
                        destEnd,
                        destValidator,
                        count
                    );
                    break;
                }

                case PredictedTransactionKind.Drag:
                    InventoryClickSolver.SolveDragPlacement(
                        targetInv,
                        ref heldItem,
                        txn.SlotIndices,
                        validator
                    );
                    break;
            }
        }
    }

    // Reseeds local shadow inventories and replays pending transactions.
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(PredictedSimulationSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    public partial class ClientInventoryReconcileSystem : SystemBase
    {
        private EntityQuery m_LocalPlayerQuery;

        protected override void OnCreate()
        {
            m_LocalPlayerQuery = LocalPlayerLookup.CreateQuery(EntityManager);
        }

        protected override void OnUpdate()
        {
            Entity localPlayerEntity = LocalPlayerLookup.Find(m_LocalPlayerQuery);
            if (localPlayerEntity == Entity.Null)
                return;
            if (!SystemAPI.HasBuffer<PredictedInventoryTransaction>(localPlayerEntity))
                return;

            var invState = SystemAPI.GetComponent<PlayerInventoryState>(
                localPlayerEntity
            );
            var pendingTxns = SystemAPI.GetBuffer<PredictedInventoryTransaction>(
                localPlayerEntity
            );

            uint watermark = invState.LastProcessedTransactionId;

            // Remove resolved transactions.
            int keepFrom = 0;
            while (
                keepFrom < pendingTxns.Length
                && pendingTxns[keepFrom].TransactionId <= watermark
            )
            {
                keepFrom++;
            }
            if (keepFrom > 0)
            {
                pendingTxns.RemoveRange(0, keepFrom);
            }

            // Copy authoritative player inventory to shadow.
            var authoritativePlayerInv = SystemAPI.GetBuffer<InventoryElement>(
                localPlayerEntity
            );
            var predictedPlayerInv =
                SystemAPI.GetBuffer<PredictedPlayerInventoryElement>(
                    localPlayerEntity
                );
            predictedPlayerInv.ResizeUninitialized(
                authoritativePlayerInv.Length
            );
            predictedPlayerInv
                .Reinterpret<InventoryElement>()
                .CopyFrom(authoritativePlayerInv);

            // Copy authoritative container inventory to shadow.
            var containerProxy = SystemAPI.GetBuffer<ContainerInventoryElement>(
                localPlayerEntity
            );
            var predictedContainer = SystemAPI.GetBuffer<PredictedContainerElement>(
                localPlayerEntity
            );
            predictedContainer.ResizeUninitialized(containerProxy.Length);
            predictedContainer
                .Reinterpret<ContainerInventoryElement>()
                .CopyFrom(containerProxy);

            // Copy authoritative held item to shadow.
            var authoritativeHeld = SystemAPI.GetComponent<PlayerHeldItem>(
                localPlayerEntity
            );
            var heldItem = new PlayerHeldItem
            {
                ItemId = authoritativeHeld.ItemId,
                Count = authoritativeHeld.Count,
            };

            bool hasOpenContainer = SystemAPI.HasComponent<OpenedContainerClientInfo>(
                localPlayerEntity
            );
            MachineRuleType ruleType = hasOpenContainer
                ? SystemAPI
                    .GetComponent<OpenedContainerClientInfo>(localPlayerEntity)
                    .RuleType
                : MachineRuleType.GenericChest;

            var predictedPlayerInvAsElements =
                predictedPlayerInv.Reinterpret<InventoryElement>();
            for (int i = 0; i < pendingTxns.Length; i++)
            {
                ClientPredictedTransactionApplier.Apply(
                    pendingTxns[i],
                    predictedPlayerInvAsElements,
                    ref heldItem,
                    predictedContainer,
                    hasOpenContainer,
                    ruleType
                );
            }

            // Save final predictions.
            SystemAPI.SetComponent(
                localPlayerEntity,
                new PredictedHeldItem
                {
                    ItemId = heldItem.ItemId,
                    Count = heldItem.Count,
                }
            );
        }
    }

    // Processes new click and drag inputs and sends them to the server.
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(ClientInventoryReconcileSystem))]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    public partial class ClientPredictInventorySystem : SystemBase
    {
        private const int MaxPendingTransactions = 12;

        private EntityQuery m_LocalPlayerQuery;

        protected override void OnCreate()
        {
            m_LocalPlayerQuery = LocalPlayerLookup.CreateQuery(EntityManager);
        }

        protected override void OnUpdate()
        {
            Entity localPlayerEntity = LocalPlayerLookup.Find(m_LocalPlayerQuery);
            if (localPlayerEntity == Entity.Null)
                return;
            if (!SystemAPI.HasBuffer<InventoryClickIntent>(localPlayerEntity))
                return;

            // Exit if server connection is not established.
            if (
                !SystemAPI.TryGetSingletonEntity<NetworkStreamConnection>(
                    out Entity serverConnectionEntity
                )
            )
                return;

            var pendingTxns = SystemAPI.GetBuffer<PredictedInventoryTransaction>(
                localPlayerEntity
            );
            var clickIntents = SystemAPI.GetBuffer<InventoryClickIntent>(
                localPlayerEntity
            );
            var dragIntents = SystemAPI.GetBuffer<InventoryDragIntent>(
                localPlayerEntity
            );

            if (clickIntents.Length == 0 && dragIntents.Length == 0)
                return;

            var predictedPlayerInvBuffer =
                SystemAPI.GetBuffer<PredictedPlayerInventoryElement>(
                    localPlayerEntity
                );
            var playerInv =
                predictedPlayerInvBuffer.Reinterpret<InventoryElement>();
            var predictedContainer = SystemAPI.GetBuffer<PredictedContainerElement>(
                localPlayerEntity
            );
            var predictedHeld = SystemAPI.GetComponent<PredictedHeldItem>(
                localPlayerEntity
            );
            var heldItem = new PlayerHeldItem
            {
                ItemId = predictedHeld.ItemId,
                Count = predictedHeld.Count,
            };
            var counter = SystemAPI.GetComponent<ClientInventoryTransactionCounter>(
                localPlayerEntity
            );

            bool hasOpenContainer = SystemAPI.HasComponent<OpenedContainerClientInfo>(
                localPlayerEntity
            );
            MachineRuleType ruleType = hasOpenContainer
                ? SystemAPI
                    .GetComponent<OpenedContainerClientInfo>(localPlayerEntity)
                    .RuleType
                : MachineRuleType.GenericChest;

            var ecb = new EntityCommandBuffer(Allocator.Temp);

            int clickIdx = 0;
            int dragIdx = 0;

            // Process intents until queues are empty or window is full.
            while (
                pendingTxns.Length < MaxPendingTransactions
                && (
                    clickIdx < clickIntents.Length
                    || dragIdx < dragIntents.Length
                )
            )
            {
                if (clickIdx < clickIntents.Length)
                {
                    var intent = clickIntents[clickIdx];
                    clickIdx++;

                    bool isShift =
                        intent.ClickType == InventoryClickType.ShiftLeftClick
                        || intent.ClickType
                            == InventoryClickType.ShiftRightClick;

                    var containerAsInventory =
                        predictedContainer.Reinterpret<InventoryElement>();
                    DynamicBuffer<InventoryElement> sourceInv =
                        intent.InvType == InventoryType.Container
                            ? containerAsInventory
                            : playerInv;
                    if (
                        intent.SlotIndex < 0
                        || intent.SlotIndex >= sourceInv.Length
                    )
                        continue;

                    var expectedSlot = sourceInv[intent.SlotIndex];
                    uint expectedHeldItemId = heldItem.ItemId;
                    int expectedHeldCount = heldItem.Count;
                    uint txnId = counter.NextTransactionId++;

                    var txn = new PredictedInventoryTransaction
                    {
                        TransactionId = txnId,
                        InvType = intent.InvType,
                        Kind = isShift
                            ? PredictedTransactionKind.ShiftClick
                            : PredictedTransactionKind.Click,
                        SlotIndex = intent.SlotIndex,
                        ClickType = intent.ClickType,
                    };

                    ClientPredictedTransactionApplier.Apply(
                        txn,
                        playerInv,
                        ref heldItem,
                        predictedContainer,
                        hasOpenContainer,
                        ruleType
                    );
                    pendingTxns.Add(txn);

                    ecb.SendRpc(
                        new ClickInventorySlotRequest
                        {
                            InvType = intent.InvType,
                            SlotIndex = intent.SlotIndex,
                            ExpectedSlotItemId = expectedSlot.ItemId,
                            ExpectedSlotCount = expectedSlot.Count,
                            ExpectedHeldItemId = expectedHeldItemId,
                            ExpectedHeldCount = expectedHeldCount,
                            ClickType = intent.ClickType,
                            TransactionId = txnId,
                        },
                        serverConnectionEntity
                    );
                }
                else
                {
                    var intent = dragIntents[dragIdx];
                    dragIdx++;

                    uint expectedHeldItemId = heldItem.ItemId;
                    int expectedHeldCount = heldItem.Count;
                    uint txnId = counter.NextTransactionId++;

                    var txn = new PredictedInventoryTransaction
                    {
                        TransactionId = txnId,
                        InvType = intent.InvType,
                        Kind = PredictedTransactionKind.Drag,
                        SlotIndices = intent.SlotIndices,
                    };

                    ClientPredictedTransactionApplier.Apply(
                        txn,
                        playerInv,
                        ref heldItem,
                        predictedContainer,
                        hasOpenContainer,
                        ruleType
                    );
                    pendingTxns.Add(txn);

                    ecb.SendRpc(
                        new DragInventorySlotRequest
                        {
                            InvType = intent.InvType,
                            SlotIndices = intent.SlotIndices,
                            ExpectedHeldItemId = expectedHeldItemId,
                            ExpectedHeldCount = expectedHeldCount,
                            TransactionId = txnId,
                        },
                        serverConnectionEntity
                    );
                }
            }

            // Remove processed intents and leave overflow.
            if (clickIdx > 0)
                clickIntents.RemoveRange(0, clickIdx);
            if (dragIdx > 0)
                dragIntents.RemoveRange(0, dragIdx);

            SystemAPI.SetComponent(
                localPlayerEntity,
                new PredictedHeldItem
                {
                    ItemId = heldItem.ItemId,
                    Count = heldItem.Count,
                }
            );
            SystemAPI.SetComponent(localPlayerEntity, counter);

            ecb.Playback(EntityManager);
            ecb.Dispose();
        }
    }
}
