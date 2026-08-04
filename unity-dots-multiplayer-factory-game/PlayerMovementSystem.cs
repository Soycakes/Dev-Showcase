using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

namespace FactoryGame.Mixed
{
    /// <summary>
    /// Handles player movement and collisions in 2D.
    /// </summary>
    [UpdateInGroup(typeof(FactoryGameInputSystemGroup))]
    [AlwaysSynchronizeSystem]
    public partial class GatherPlayerInputSystem : SystemBase
    {
        private GameInputControls m_InputControls;

        protected override void OnCreate()
        {
            RequireForUpdate<EnableSpawnPlayer>();
            RequireForUpdate<PlayerInput>();
            RequireForUpdate<NetworkStreamInGame>();

            m_InputControls = new GameInputControls();
            m_InputControls.Enable();
        }

        protected override void OnDestroy()
        {
            if (m_InputControls != null)
            {
                m_InputControls.Disable();
            }
        }

        protected override void OnUpdate()
        {
            if (UIInputBlocker.IsGameInputBlocked)
            {
                foreach (
                    var input in SystemAPI
                        .Query<RefRW<PlayerInput>>()
                        .WithAll<GhostOwnerIsLocal>()
                )
                {
                    input.ValueRW.MovementInput = float2.zero;
                    input.ValueRW.PlaceClicked = default;
                    input.ValueRW.BreakHeld = false;
                    input.ValueRW.JumpPressed = false;
                    input.ValueRW.DownPressed = false;
                }
                return;
            }

            UnityEngine.Vector2 moveVal =
                m_InputControls.Player.Move.ReadValue<UnityEngine.Vector2>();
            float2 movementInput = new float2(moveVal.x, moveVal.y);

            bool breakHeld = m_InputControls.Player.Break.IsPressed();
            bool placeClicked =
                m_InputControls.Player.Place.WasPressedThisFrame();

            bool jumpPressed = m_InputControls.Player.Jump.IsPressed();
            bool downPressed = m_InputControls.Player.Down.IsPressed();

            int targetSlot = -1;
            // Eventually hotbar swap needs a proper handler especially when cooldowns or tool usage time is implemented
            // For now, just allow hotbar swapping to be done
            if (m_InputControls.Player.HotbarSlot1.WasPressedThisFrame())
                targetSlot = 0;
            else if (m_InputControls.Player.HotbarSlot2.WasPressedThisFrame())
                targetSlot = 1;
            else if (m_InputControls.Player.HotbarSlot3.WasPressedThisFrame())
                targetSlot = 2;
            else if (m_InputControls.Player.HotbarSlot4.WasPressedThisFrame())
                targetSlot = 3;
            else if (m_InputControls.Player.HotbarSlot5.WasPressedThisFrame())
                targetSlot = 4;
            else if (m_InputControls.Player.HotbarSlot6.WasPressedThisFrame())
                targetSlot = 5;
            else if (m_InputControls.Player.HotbarSlot7.WasPressedThisFrame())
                targetSlot = 6;
            else if (m_InputControls.Player.HotbarSlot8.WasPressedThisFrame())
                targetSlot = 7;
            else if (m_InputControls.Player.HotbarSlot9.WasPressedThisFrame())
                targetSlot = 8;
            else if (m_InputControls.Player.HotbarSlot10.WasPressedThisFrame())
                targetSlot = 9;

            float scroll = m_InputControls
                .Player.Scroll.ReadValue<UnityEngine.Vector2>()
                .y;

            if (targetSlot != -1 || scroll != 0f)
            {
                foreach (
                    var playerData in SystemAPI
                        .Query<RefRW<PlayerData>>()
                        .WithAll<GhostOwnerIsLocal>()
                )
                {
                    if (targetSlot != -1)
                    {
                        playerData.ValueRW.SelectedHotbarSlot = targetSlot;
                    }
                    else if (scroll > 0f)
                    {
                        playerData.ValueRW.SelectedHotbarSlot =
                            (playerData.ValueRO.SelectedHotbarSlot + 9) % 10;
                    }
                    else if (scroll < 0f)
                    {
                        playerData.ValueRW.SelectedHotbarSlot =
                            (playerData.ValueRO.SelectedHotbarSlot + 1) % 10;
                    }
                }
            }

            Dependency = new GatherPlayerInputJob()
            {
                movementInput = movementInput,
                breakHeld = breakHeld,
                placeClicked = placeClicked,
                jumpPressed = jumpPressed,
                downPressed = downPressed,
            }.Schedule(Dependency); // Run on single thread since there is only one local player.
        }

        [WithAll(typeof(GhostOwnerIsLocal))]
        partial struct GatherPlayerInputJob : IJobEntity
        {
            public float2 movementInput;
            public bool breakHeld;
            public bool placeClicked;
            public bool jumpPressed;
            public bool downPressed;

            public void Execute(ref PlayerInput inputData)
            {
                inputData.MovementInput = movementInput;
                inputData.BreakHeld = breakHeld;
                inputData.PlaceClicked = default; // Clear input event
                if (placeClicked)
                {
                    inputData.PlaceClicked.Set();
                }
                inputData.JumpPressed = jumpPressed;
                inputData.DownPressed = downPressed;
            }
        }
    }

    // Process movement for the local player on the client.
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(ClientMovementRestoreSystem))]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    public partial class ProcessPlayerMovementSystem : SystemBase
    {
        protected override void OnCreate()
        {
            RequireForUpdate<EnableSpawnPlayer>();
            RequireForUpdate<PlayerInput>();
            RequireForUpdate<ActiveChunks>();
            RequireForUpdate<RegistryReference>();
        }

        protected override void OnUpdate()
        {
            var activeChunks = SystemAPI.GetSingleton<ActiveChunks>();
            var registryRef = SystemAPI.GetSingleton<RegistryReference>();

            Dependency = new ProcessPlayerMovementJob()
            {
                DeltaTime = SystemAPI.Time.DeltaTime,
                Grid = activeChunks.Map,
                MainTileLookup = SystemAPI.GetBufferLookup<MainTile>(true),
                Registry = registryRef.Reference,
            }.Schedule(Dependency); // Run on single thread since there is only one local player.
        }

        // Filters by local player archetype.
        [BurstCompile]
        [WithAll(typeof(GhostOwnerIsLocal))]
        partial struct ProcessPlayerMovementJob : IJobEntity
        {
            private const float GravityAcceleration = 28f;
            private const float TerminalVelocityY = -16f;
            private const float SubstepVelocityThreshold = 0.4f;
            private const float CollisionEpsilon = 0.0001f;
            private const byte CollisionNone = 0;
            private const byte CollisionSolid = 1;
            private const byte CollisionOneWay = 2;

            public float DeltaTime;

            [ReadOnly]
            public NativeParallelHashMap<int2, Entity> Grid;

            [ReadOnly]
            public BufferLookup<MainTile> MainTileLookup;

            [ReadOnly]
            public BlobAssetReference<RegistryBlob> Registry;

            public void Execute(
                ref LocalTransform trans,
                ref PlayerMovement movement,
                ref PlayerData playerData,
                in PlayerStats playerStats,
                in PlayerInput input,
                ref ClientMovementState clientState
            )
            {
                // Apply gravity, clamped to terminal velocity.
                movement.Velocity.y = math.max(
                    movement.Velocity.y - (GravityAcceleration * DeltaTime),
                    TerminalVelocityY
                );

                // Jump logic
                if (movement.IsGrounded && input.JumpPressed)
                {
                    movement.Velocity.y = playerStats.JumpHeight;
                    movement.IsGrounded = false;
                }

                // Horizontal movement
                movement.Velocity.x =
                    input.MovementInput.x * playerStats.MovementSpeed;

                float3 pos = trans.Position;
                float2 halfSize = playerData.HitboxSize * 0.5f;

                // Substepped movement
                int substeps = CalculateSubsteps(
                    movement.Velocity,
                    DeltaTime,
                    SubstepVelocityThreshold
                );
                float subDt = DeltaTime / substeps;
                bool hitGround = false;

                for (int step = 0; step < substeps; step++)
                {
                    float3 stepStartPos = pos;
                    pos.x += movement.Velocity.x * subDt;
                    if (
                        ResolveCollisions(
                            ref pos,
                            stepStartPos,
                            halfSize,
                            true,
                            input.DownPressed
                        )
                    )
                    {
                        movement.Velocity.x = 0f;
                    }

                    stepStartPos = pos;
                    pos.y += movement.Velocity.y * subDt;
                    if (
                        ResolveCollisions(
                            ref pos,
                            stepStartPos,
                            halfSize,
                            false,
                            input.DownPressed
                        )
                    )
                    {
                        if (movement.Velocity.y <= 0f)
                        {
                            hitGround = true;
                        }
                        movement.Velocity.y = 0f;
                    }
                }

                movement.IsGrounded = hitGround;

                // Lock Z position to prevent rendering issues.
                pos.z = 0f;
                trans.Position = pos;

                // Save velocity for smoothing.
                movement.CurrentVelocity = movement.Velocity;

                // Save client state for the next frame.
                clientState.Position = trans.Position;
                clientState.Velocity = movement.Velocity;
                clientState.IsGrounded = movement.IsGrounded;
                clientState.Initialized = true;
            }

            private bool ResolveCollisions(
                ref float3 pos,
                float3 prevPos,
                float2 halfSize,
                bool isXAxis,
                bool downPressed
            )
            {
                bool collided = false;
                float2 min = pos.xy - halfSize;
                float2 max = pos.xy + halfSize;

                int minGridX = (int)math.floor(min.x);
                int maxGridX = (int)math.ceil(max.x);
                int minGridY = (int)math.floor(min.y);
                int maxGridY = (int)math.ceil(max.y);

                for (int x = minGridX; x <= maxGridX; x++)
                {
                    for (int y = minGridY; y <= maxGridY; y++)
                    {
                        int2 coord = new int2(x, y);

                        int2 chunkPos = ChunkLookup.WorldToChunkCoords(
                            new int2(x, y)
                        );

                        byte collisionType = 0;

                        // Treat unloaded chunks as solid blocks.
                        if (!Grid.TryGetValue(chunkPos, out Entity chunkEntity))
                        {
                            collisionType = 1;
                        }
                        else if (!MainTileLookup.HasBuffer(chunkEntity))
                        {
                            collisionType = 1;
                        }
                        else
                        {
                            var tiles = MainTileLookup[chunkEntity];
                            if (tiles.Length < ChunkLookup.ChunkArea)
                            {
                                collisionType = 1;
                            }
                            else
                            {
                                int2 localPos =
                                    coord - chunkPos * ChunkLookup.ChunkSize;
                                int index =
                                    localPos.y * ChunkLookup.ChunkSize
                                    + localPos.x;
                                if (index >= 0 && index < ChunkLookup.ChunkArea)
                                {
                                    var tile = tiles[index];
                                    uint blockId = tile.BlockId;

                                    // Check registry bounds.
                                    if (
                                        blockId
                                        < (uint)Registry.Value.Objects.Length
                                    )
                                    {
                                        var obj = Registry.Value.Objects[
                                            (int)blockId
                                        ];
                                        if (
                                            obj.Id != 0
                                            && (
                                                obj.Category
                                                & ItemCategory.Block
                                            ) != 0
                                        )
                                        {
                                            collisionType =
                                                obj.BlockConfig.CollisionType;
                                        }
                                    }
                                }
                            }
                        }

                        if (collisionType == CollisionNone)
                            continue;

                        float2 blockMin = new float2(
                            coord.x - 0.5f,
                            coord.y - 0.5f
                        );
                        float2 blockMax = new float2(
                            coord.x + 0.5f,
                            coord.y + 0.5f
                        );

                        if (
                            IsOverlapping(
                                prevPos.xy - halfSize,
                                prevPos.xy + halfSize,
                                blockMin,
                                blockMax
                            )
                        )
                            continue;
                        if (!IsOverlapping(min, max, blockMin, blockMax))
                            continue;

                        if (isXAxis)
                        {
                            if (collisionType == CollisionOneWay)
                                continue; // One way platforms have no horizontal collision

                            pos.x =
                                (prevPos.x < coord.x)
                                    ? (blockMin.x - halfSize.x - CollisionEpsilon)
                                    : (blockMax.x + halfSize.x + CollisionEpsilon);
                            collided = true;
                        }
                        else
                        {
                            if (collisionType == CollisionOneWay)
                            {
                                float prevBottom = prevPos.y - halfSize.y;
                                if (
                                    pos.y < prevPos.y
                                    && prevBottom >= blockMax.y - 0.01f
                                    && !downPressed
                                )
                                {
                                    pos.y = blockMax.y + halfSize.y + CollisionEpsilon;
                                    collided = true;
                                }
                            }
                            else
                            {
                                pos.y =
                                    (prevPos.y < coord.y)
                                        ? (blockMin.y - halfSize.y - CollisionEpsilon)
                                        : (blockMax.y + halfSize.y + CollisionEpsilon);
                                collided = true;
                            }
                        }

                        if (collided)
                        {
                            min = pos.xy - halfSize;
                            max = pos.xy + halfSize;
                        }
                    }
                }

                return collided;
            }

            private static bool IsOverlapping(
                float2 minA,
                float2 maxA,
                float2 minB,
                float2 maxB
            )
            {
                return minA.x < maxB.x
                    && maxA.x > minB.x
                    && minA.y < maxB.y
                    && maxA.y > minB.y;
            }

            private static int CalculateSubsteps(
                float2 velocity,
                float deltaTime,
                float maxStep
            )
            {
                float maxMove = math.max(
                    math.abs(velocity.x * deltaTime),
                    math.abs(velocity.y * deltaTime)
                );
                return math.max(1, (int)math.ceil(maxMove / maxStep));
            }
        }
    }

    public struct ClientMovementState : IComponentData
    {
        public float3 Position;
        public float2 Velocity;
        public bool IsGrounded;
        public bool Initialized;
    }

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(PredictedSimulationSystemGroup))]
    [UpdateBefore(typeof(ClientMovementSendSystem))]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    public partial class ClientMovementRestoreSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            foreach (
                var (trans, movement, state) in SystemAPI
                    .Query<
                        RefRW<LocalTransform>,
                        RefRW<PlayerMovement>,
                        RefRO<ClientMovementState>
                    >()
                    .WithAll<GhostOwnerIsLocal>()
            )
            {
                if (state.ValueRO.Initialized)
                {
                    trans.ValueRW.Position = state.ValueRO.Position;
                    movement.ValueRW.Velocity = state.ValueRO.Velocity;
                    movement.ValueRW.IsGrounded = state.ValueRO.IsGrounded;
                }
            }
        }
    }

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(ProcessPlayerMovementSystem))]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    public partial class ClientMovementSendSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            // Exit if server connection is not established.
            if (
                !SystemAPI.TryGetSingletonEntity<NetworkStreamConnection>(
                    out Entity serverConnectionEntity
                )
            )
                return;

            var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (
                var (trans, movement, pData, state, entity) in SystemAPI
                    .Query<
                        RefRO<LocalTransform>,
                        RefRO<PlayerMovement>,
                        RefRO<PlayerData>,
                        RefRW<ClientMovementState>
                    >()
                    .WithAll<GhostOwnerIsLocal>()
                    .WithEntityAccess()
            )
            {
                if (!state.ValueRO.Initialized)
                    continue;

                ecb.SendRpc(
                    new ClientMovementReport
                    {
                        Position = trans.ValueRO.Position,
                        Velocity = movement.ValueRO.Velocity,
                        IsGrounded = movement.ValueRO.IsGrounded,
                        SelectedHotbarSlot = pData.ValueRO.SelectedHotbarSlot,
                    },
                    serverConnectionEntity
                );
            }

            ecb.Playback(EntityManager);
            ecb.Dispose();
        }
    }

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(ClientMovementRestoreSystem))]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    public partial class ClientMovementInitializeSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (
                var (trans, entity) in SystemAPI
                    .Query<RefRO<LocalTransform>>()
                    .WithAll<GhostOwnerIsLocal>()
                    .WithNone<ClientMovementState>()
                    .WithEntityAccess()
            )
            {
                ecb.AddComponent(
                    entity,
                    new ClientMovementState
                    {
                        Position = trans.ValueRO.Position,
                        Velocity = float2.zero,
                        IsGrounded = false,
                        Initialized = false,
                    }
                );
            }

            ecb.Playback(EntityManager);
            ecb.Dispose();
        }
    }
}
