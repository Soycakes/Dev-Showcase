using System.Collections.Generic;
using System.IO;
using FactoryGame.Mixed;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

namespace FactoryGame.Server
{
    /// <summary>
    /// Chat feed for save/load status messages. Kept separate from
    /// SaveWorldSerializer so the serializer isn't coupled to chat UI concerns.
    /// </summary>
    public static class SaveNotifications
    {
        public static readonly System.Collections.Concurrent.ConcurrentQueue<string> Pending =
            new System.Collections.Concurrent.ConcurrentQueue<string>();

        public static void NotifySuccess(string message) =>
            Pending.Enqueue(ChatConfig.FormatServerInfo(message));

        public static void NotifyError(string message) =>
            Pending.Enqueue(ChatConfig.FormatServerError(message));
    }

    /// <summary>
    /// Saves and loads the server world state using binary serialization.
    /// </summary>
    public static class SaveWorldSerializer
    {
        // Change if the file layout changes. Rejects invalid versions on load.
        private const int CurrentSaveVersion = 4;

        // Fixed slot count for machine inventories (ex. chests).
        private const int MachineInventorySlotCount = 40;

        // Volatile backing field ensures cross thread visibility.
        private static volatile bool s_IsSaving;
        public static bool IsSaving => s_IsSaving;

        public struct InventorySlotData
        {
            public uint ItemId;
            public int Count;
        }

        public struct PlayerData
        {
            // Known limitation
            // keyed by NetworkId, which is reassigned on reconnect.
            // A persistent player identity (ex. platform account ID) is needed for
            // saves to survive reconnects correctly.
            public int NetworkId;
            public double3 Position;
            public int SelectedHotbarSlot;
            public List<InventorySlotData> Inventory;
        }

        // A single RLE compressed span.
        private struct RleSpan
        {
            public ushort Count;
            public uint BlockId;
            public byte Metadata;
        }

        /// <summary>
        /// Returns true if a world.dat file exists for the active world.
        /// </summary>
        public static bool WorldFileExists() => WorldSaveInfo.WorldFileExists();

        // Captures all world data on the main thread, then writes to disk in a background thread.
        // Returns true if saving successfully started, false if already saving.
        public static bool Save(World world)
        {
            if (IsSaving)
            {
                SaveNotifications.NotifyError("Save is already in progress.");
                return false;
            }
            s_IsSaving = true;
            var sw = System.Diagnostics.Stopwatch.StartNew();
            string saveDir = WorldSaveInfo.GetSaveDirectory();
            string playersDir = Path.Combine(saveDir, "players");

            Directory.CreateDirectory(saveDir);
            Directory.CreateDirectory(playersDir);

            EntityManager em = world.EntityManager;

            // Snapshot active chunks.
            List<ChunkSaveData> chunkSnapshots = new List<ChunkSaveData>();
            var activeCoords = new HashSet<int2>();
            EntityQuery chunkQuery = em.CreateEntityQuery(typeof(Chunk));

            using (
                var chunkEntities = chunkQuery.ToEntityArray(
                    Unity.Collections.Allocator.TempJob
                )
            )
            {
                foreach (Entity chunkEntity in chunkEntities)
                {
                    Chunk chunkComponent = em.GetComponentData<Chunk>(
                        chunkEntity
                    );
                    chunkSnapshots.Add(
                        ChunkSnapshotUtility.SnapshotChunk(
                            em,
                            chunkEntity,
                            chunkComponent.Coords
                        )
                    );
                    activeCoords.Add(chunkComponent.Coords);
                }
            }

            // Merge modified chunks from the save cache.
            EntityQuery saveCacheQuery = em.CreateEntityQuery(
                typeof(WorldSaveCache)
            );
            if (saveCacheQuery.CalculateEntityCount() > 0)
            {
                var saveCache = em.GetComponentObject<WorldSaveCache>(
                    saveCacheQuery.GetSingletonEntity()
                );
                foreach (var cached in saveCache.Cache)
                {
                    if (activeCoords.Contains(cached.Key))
                        continue;
                    chunkSnapshots.Add(cached.Value);
                }
            }

            // Snapshot active machines.
            List<MachineSaveData> machineSnapshots =
                new List<MachineSaveData>();
            EntityQuery machineQuery = em.CreateEntityQuery(typeof(Machine));

            using (
                var machineEntities = machineQuery.ToEntityArray(
                    Unity.Collections.Allocator.TempJob
                )
            )
            {
                foreach (Entity machineEntity in machineEntities)
                {
                    Machine machineComponent = em.GetComponentData<Machine>(
                        machineEntity
                    );

                    var invSnap = new List<InventorySlotSaveData>();
                    if (em.HasBuffer<InventoryElement>(machineEntity))
                    {
                        var invBuffer = em.GetBuffer<InventoryElement>(
                            machineEntity
                        );
                        for (int i = 0; i < invBuffer.Length; i++)
                        {
                            invSnap.Add(
                                new InventorySlotSaveData
                                {
                                    ItemId = invBuffer[i].ItemId,
                                    Count = invBuffer[i].Count,
                                }
                            );
                        }
                    }

                    machineSnapshots.Add(
                        new MachineSaveData
                        {
                            GridPosition = machineComponent.GridPosition,
                            ItemId = machineComponent.ItemId,
                            Inventory = invSnap,
                        }
                    );
                }
            }

            // Snapshot player data.
            List<PlayerData> playerSnapshots = new List<PlayerData>();
            var activeNetworkIds = new HashSet<int>();
            EntityQuery playerQuery = em.CreateEntityQuery(
                ComponentType.ReadOnly<Mixed.PlayerData>(),
                ComponentType.ReadOnly<ConnectionOwner>()
            );

            using (
                var playerEntities = playerQuery.ToEntityArray(
                    Unity.Collections.Allocator.TempJob
                )
            )
            {
                foreach (Entity playerEntity in playerEntities)
                {
                    ConnectionOwner owner =
                        em.GetComponentData<ConnectionOwner>(playerEntity);
                    if (!em.HasComponent<NetworkId>(owner.Entity))
                        continue;

                    NetworkId netId = em.GetComponentData<NetworkId>(
                        owner.Entity
                    );
                    LocalTransform transform =
                        em.GetComponentData<LocalTransform>(playerEntity);
                    Mixed.PlayerData playerData =
                        em.GetComponentData<Mixed.PlayerData>(playerEntity);

                    var invBuffer = em.GetBuffer<InventoryElement>(
                        playerEntity
                    );
                    List<InventorySlotData> invSnap =
                        new List<InventorySlotData>();

                    for (int i = 0; i < invBuffer.Length; i++)
                    {
                        invSnap.Add(
                            new InventorySlotData
                            {
                                ItemId = invBuffer[i].ItemId,
                                Count = invBuffer[i].Count,
                            }
                        );
                    }

                    playerSnapshots.Add(
                        new PlayerData
                        {
                            NetworkId = netId.Value,
                            Position = transform.Position,
                            SelectedHotbarSlot = playerData.SelectedHotbarSlot,
                            Inventory = invSnap,
                        }
                    );
                    activeNetworkIds.Add(netId.Value);
                }
            }

            // Merge disconnected players from the cache.
            EntityQuery playerCacheQuery = em.CreateEntityQuery(
                typeof(PlayerSaveCache)
            );
            if (playerCacheQuery.CalculateEntityCount() > 0)
            {
                var playerCache = em.GetComponentObject<PlayerSaveCache>(
                    playerCacheQuery.GetSingletonEntity()
                );
                foreach (var cached in playerCache.Cache)
                {
                    if (activeNetworkIds.Contains(cached.Key))
                        continue;
                    playerSnapshots.Add(cached.Value);
                }
            }

            // Write data to disk in background thread
            // Capture paths on the main thread since Unity APIs are not thread safe.
            string capturedWorldPath = WorldSaveInfo.GetWorldFilePath();
            var capturedPlayerPaths = new Dictionary<int, string>();
            foreach (var playerSnap in playerSnapshots)
                capturedPlayerPaths[playerSnap.NetworkId] =
                    WorldSaveInfo.GetPlayerFilePath(playerSnap.NetworkId);

            System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    string worldPath = capturedWorldPath;
                    WriteWorldFile(worldPath, chunkSnapshots, machineSnapshots);
                    foreach (var playerSnap in playerSnapshots)
                    {
                        WritePlayerFile(
                            capturedPlayerPaths[playerSnap.NetworkId],
                            playerSnap
                        );
                    }
                    sw.Stop();
                    long worldSize = new FileInfo(worldPath).Length;
                    double worldSizeKb = worldSize / 1024.0;

                    string msg = $"World saved successfully! ({sw.ElapsedMilliseconds}ms, {worldSizeKb:F1} KB)";
                    SaveNotifications.NotifySuccess(msg);
                    Debug.Log($"[SaveWorldSerializer] {msg}");
                }
                catch (System.Exception ex)
                {
                    string errMsg = $"Error during save execution: {ex.Message}";
                    SaveNotifications.NotifyError(errMsg);
                    Debug.LogError($"[SaveWorldSerializer] {errMsg}");
                }
                finally
                {
                    s_IsSaving = false;
                }
            });

            return true;
        }

        // Loads saved world data into cache at startup.
        public static void LoadAtStartup(World world)
        {
            string worldPath = WorldSaveInfo.GetWorldFilePath();
            if (!File.Exists(worldPath))
            {
                Debug.LogWarning(
                    "[SaveWorldSerializer] No world.dat file found. Skipping terrain loading."
                );
                return;
            }

            EntityManager em = world.EntityManager;

            EntityQuery activeChunksQuery = em.CreateEntityQuery(
                typeof(ActiveChunks)
            );
            WorldSaveCache saveCache = null;
            if (activeChunksQuery.HasSingleton<ActiveChunks>())
            {
                Entity cacheHolderEntity =
                    activeChunksQuery.GetSingletonEntity();
                if (em.HasComponent<WorldSaveCache>(cacheHolderEntity))
                {
                    saveCache = em.GetComponentObject<WorldSaveCache>(
                        cacheHolderEntity
                    );
                }
            }

            try
            {
                using (
                    BinaryReader reader = new BinaryReader(
                        File.Open(worldPath, FileMode.Open)
                    )
                )
                {
                    int fileVersion = reader.ReadInt32();
                    if (fileVersion != CurrentSaveVersion)
                    {
                        Debug.LogError(
                            $"[SaveWorldSerializer] Cannot load world.dat: save version {fileVersion} does not match current version {CurrentSaveVersion}."
                        );
                        return;
                    }
                    WorldSaveInfo.ActiveSeed = reader.ReadInt32();
                    int chunkCount = reader.ReadInt32();

                    if (saveCache == null && chunkCount > 0)
                    {
                        Debug.LogError(
                            $"[SaveWorldSerializer] No WorldSaveCache found on the ActiveChunks singleton: "
                                + $"{chunkCount} saved chunks will be decoded but discarded instead of cached "
                                + "for lazy loading."
                        );
                    }

                    for (int i = 0; i < chunkCount; i++)
                    {
                        int2 coords = new int2(
                            reader.ReadInt32(),
                            reader.ReadInt32()
                        );

                        var chunkData = new ChunkSaveData
                        {
                            Coords = coords,
                            HasWall = reader.ReadBoolean(),
                            HasLogistics = reader.ReadBoolean(),
                            HasMain = reader.ReadBoolean(),
                            HasFluid = reader.ReadBoolean(),
                            HasOverlay = reader.ReadBoolean(),
                        };

                        if (chunkData.HasWall)
                            chunkData.WallTiles = DecompressLayer(reader);
                        if (chunkData.HasLogistics)
                            chunkData.LogisticsTiles = DecompressLayer(reader);
                        if (chunkData.HasMain)
                            chunkData.MainTiles = DecompressLayer(reader);
                        if (chunkData.HasFluid)
                            chunkData.FluidTiles = DecompressLayer(reader);
                        if (chunkData.HasOverlay)
                            chunkData.OverlayTiles = DecompressLayer(reader);

                        chunkData.Items = ReadItems(reader);

                        if (saveCache != null)
                        {
                            saveCache.Cache[coords] = chunkData;
                        }
                    }

                    // Recreate machines and register their footprints.
                    List<MachineSaveData> machines = ReadMachines(reader);
                    EntityQuery machineLookupQuery = em.CreateEntityQuery(
                        typeof(ServerMachineLookup)
                    );
                    if (machineLookupQuery.HasSingleton<ServerMachineLookup>())
                    {
                        var machineLookupMap = machineLookupQuery
                            .GetSingleton<ServerMachineLookup>()
                            .Map;

                        foreach (var machineData in machines)
                        {
                            uint itemId = machineData.ItemId;

                            // Ignore items that are no longer registered as machines.
                            if (!ItemDatabase.IsMachine(itemId))
                                continue;

                            Entity machineEntity = em.CreateEntity();
                            em.AddComponentData(
                                machineEntity,
                                new Machine
                                {
                                    GridPosition = machineData.GridPosition,
                                    ItemId = itemId,
                                }
                            );
                            em.AddComponent<InWorldGameplayEntity>(
                                machineEntity
                            );

                            if (
                                ItemDatabase.GetMachineType(itemId)
                                == MachineFunction.Storage
                            )
                            {
                                em.AddComponent<Chest>(machineEntity);
                                var chestInv = em.AddBuffer<InventoryElement>(
                                    machineEntity
                                );
                                chestInv.ResizeUninitialized(MachineInventorySlotCount);
                                for (int s = 0; s < MachineInventorySlotCount; s++)
                                {
                                    chestInv[s] =
                                        s < machineData.Inventory.Count
                                            ? new InventoryElement
                                            {
                                                ItemId = machineData
                                                    .Inventory[s]
                                                    .ItemId,
                                                Count = machineData
                                                    .Inventory[s]
                                                    .Count,
                                            }
                                            : new InventoryElement
                                            {
                                                ItemId = 0,
                                                Count = 0,
                                            };
                                }
                            }
                            else if (machineData.Inventory.Count > 0)
                            {
                                // Restore inventory for other machines.
                                var machineInv = em.AddBuffer<InventoryElement>(
                                    machineEntity
                                );
                                foreach (var slot in machineData.Inventory)
                                {
                                    machineInv.Add(
                                        new InventoryElement
                                        {
                                            ItemId = slot.ItemId,
                                            Count = slot.Count,
                                        }
                                    );
                                }
                            }

                            ServerMachineLookup.RegisterFootprint(
                                ref machineLookupMap,
                                machineData.GridPosition,
                                ItemDatabase.GetBlockWidth(itemId),
                                ItemDatabase.GetBlockHeight(itemId),
                                machineEntity
                            );
                        }
                    }

                    Debug.Log(
                        $"[SaveWorldSerializer] Loaded {chunkCount} chunks and {machines.Count} machines successfully."
                    );
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError(
                    $"[SaveWorldSerializer] Error during startup load: {ex.Message}"
                );
            }
        }

        // Loads a player profile from disk. See PlayerData.NetworkId for the
        // known limitation around player identity.
        public static bool TryLoadPlayer(
            int networkId,
            out double3 position,
            out int selectedSlot,
            out List<InventorySlotData> inventory
        )
        {
            position = default;
            selectedSlot = 0;
            inventory = new List<InventorySlotData>();

            string filePath = WorldSaveInfo.GetPlayerFilePath(networkId);
            if (!File.Exists(filePath))
            {
                return false;
            }

            try
            {
                using (
                    BinaryReader reader = new BinaryReader(
                        File.Open(filePath, FileMode.Open)
                    )
                )
                {
                    position.x = reader.ReadDouble();
                    position.y = reader.ReadDouble();
                    position.z = reader.ReadDouble();
                    selectedSlot = reader.ReadInt32();
                    int itemCount = reader.ReadInt32();

                    for (int i = 0; i < itemCount; i++)
                    {
                        inventory.Add(
                            new InventorySlotData
                            {
                                ItemId = reader.ReadUInt32(),
                                Count = reader.ReadInt32(),
                            }
                        );
                    }
                }
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError(
                    $"[SaveWorldSerializer] Error reading player {networkId} file: {ex.Message}"
                );
                return false;
            }
        }

        #region Serialization Helpers

        private static void WriteWorldFile(
            string filePath,
            List<ChunkSaveData> chunks,
            List<MachineSaveData> machines
        )
        {
            using (
                BinaryWriter writer = new BinaryWriter(
                    File.Open(filePath, FileMode.Create)
                )
            )
            {
                writer.Write(CurrentSaveVersion);
                writer.Write(WorldSaveInfo.ActiveSeed);
                writer.Write(chunks.Count);

                foreach (var chunk in chunks)
                {
                    writer.Write(chunk.Coords.x);
                    writer.Write(chunk.Coords.y);

                    writer.Write(chunk.HasWall);
                    writer.Write(chunk.HasLogistics);
                    writer.Write(chunk.HasMain);
                    writer.Write(chunk.HasFluid);
                    writer.Write(chunk.HasOverlay);

                    if (chunk.HasWall)
                        WriteLayer(writer, chunk.WallTiles);
                    if (chunk.HasLogistics)
                        WriteLayer(writer, chunk.LogisticsTiles);
                    if (chunk.HasMain)
                        WriteLayer(writer, chunk.MainTiles);
                    if (chunk.HasFluid)
                        WriteLayer(writer, chunk.FluidTiles);
                    if (chunk.HasOverlay)
                        WriteLayer(writer, chunk.OverlayTiles);

                    writer.Write(chunk.Items.Count);
                    foreach (var item in chunk.Items)
                    {
                        writer.Write(item.LocalPosition.x);
                        writer.Write(item.LocalPosition.y);
                        writer.Write(item.ItemId);
                        writer.Write(item.Count);
                    }
                }

                // Write machine data.
                writer.Write(machines.Count);
                foreach (var machine in machines)
                {
                    writer.Write(machine.GridPosition.x);
                    writer.Write(machine.GridPosition.y);
                    writer.Write(machine.ItemId);
                    writer.Write(machine.Inventory.Count);
                    foreach (var slot in machine.Inventory)
                    {
                        writer.Write(slot.ItemId);
                        writer.Write(slot.Count);
                    }
                }
            }
        }

        private static void WritePlayerFile(string filePath, PlayerData player)
        {
            using (
                BinaryWriter writer = new BinaryWriter(
                    File.Open(filePath, FileMode.Create)
                )
            )
            {
                writer.Write(player.Position.x);
                writer.Write(player.Position.y);
                writer.Write(player.Position.z);
                writer.Write(player.SelectedHotbarSlot);
                writer.Write(player.Inventory.Count);

                foreach (var item in player.Inventory)
                {
                    writer.Write(item.ItemId);
                    writer.Write(item.Count);
                }
            }
        }

        private static void WriteLayer(
            BinaryWriter writer,
            TileSaveData[] tiles
        )
        {
            List<RleSpan> spans = CompressLayer(tiles);
            writer.Write(spans.Count);
            foreach (var span in spans)
            {
                writer.Write(span.Count);
                writer.Write(span.BlockId);
                writer.Write(span.Metadata);
            }
        }

        private static List<RleSpan> CompressLayer(TileSaveData[] tiles)
        {
            List<RleSpan> spans = new List<RleSpan>();
            if (tiles == null || tiles.Length == 0)
                return spans;

            uint currentBlockId = tiles[0].BlockId;
            byte currentMetadata = tiles[0].Metadata;
            ushort currentRun = 1;

            for (int i = 1; i < tiles.Length; i++)
            {
                if (
                    tiles[i].BlockId == currentBlockId
                    && tiles[i].Metadata == currentMetadata
                )
                {
                    currentRun++;
                }
                else
                {
                    spans.Add(
                        new RleSpan
                        {
                            Count = currentRun,
                            BlockId = currentBlockId,
                            Metadata = currentMetadata,
                        }
                    );
                    currentBlockId = tiles[i].BlockId;
                    currentMetadata = tiles[i].Metadata;
                    currentRun = 1;
                }
            }
            spans.Add(
                new RleSpan
                {
                    Count = currentRun,
                    BlockId = currentBlockId,
                    Metadata = currentMetadata,
                }
            );
            return spans;
        }

        // Decompresses a single layer.
        private static TileSaveData[] DecompressLayer(BinaryReader reader)
        {
            var tiles = new TileSaveData[ChunkLookup.ChunkArea];
            int spanCount = reader.ReadInt32();
            int index = 0;

            for (int i = 0; i < spanCount; i++)
            {
                ushort count = reader.ReadUInt16();
                uint blockId = reader.ReadUInt32();
                byte metadata = reader.ReadByte();

                for (int c = 0; c < count; c++)
                {
                    if (index < ChunkLookup.ChunkArea)
                    {
                        tiles[index] = new TileSaveData
                        {
                            BlockId = blockId,
                            Metadata = metadata,
                        };
                        index++;
                    }
                }
            }
            return tiles;
        }

        #endregion

        // Reads machine data from the file.
        private static List<MachineSaveData> ReadMachines(BinaryReader reader)
        {
            int machineCount = reader.ReadInt32();
            var result = new List<MachineSaveData>(machineCount);

            for (int i = 0; i < machineCount; i++)
            {
                int2 gridPos = new int2(reader.ReadInt32(), reader.ReadInt32());
                uint itemId = reader.ReadUInt32();
                int invCount = reader.ReadInt32();
                var invSlots = new List<InventorySlotSaveData>(invCount);
                for (int s = 0; s < invCount; s++)
                {
                    invSlots.Add(
                        new InventorySlotSaveData
                        {
                            ItemId = reader.ReadUInt32(),
                            Count = reader.ReadInt32(),
                        }
                    );
                }

                result.Add(
                    new MachineSaveData
                    {
                        GridPosition = gridPos,
                        ItemId = itemId,
                        Inventory = invSlots,
                    }
                );
            }

            return result;
        }

        // Reads item drops from the file.
        private static List<ItemDropSaveData> ReadItems(BinaryReader reader)
        {
            int itemCount = reader.ReadInt32();
            var result = new List<ItemDropSaveData>(itemCount);

            for (int i = 0; i < itemCount; i++)
            {
                int2 localPos = new int2(
                    reader.ReadInt32(),
                    reader.ReadInt32()
                );
                uint itemId = reader.ReadUInt32();
                int count = reader.ReadInt32();

                result.Add(
                    new ItemDropSaveData
                    {
                        LocalPosition = localPos,
                        ItemId = itemId,
                        Count = count,
                    }
                );
            }

            return result;
        }
    }
}
