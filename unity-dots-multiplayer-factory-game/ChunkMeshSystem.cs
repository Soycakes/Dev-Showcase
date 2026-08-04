using FactoryGame.Mixed;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering;

namespace FactoryGame.Client
{
    public struct Vertex
    {
        public float3 Position;
        public float4 Color;
        public float2 UV;
    }

    /// <summary>
    /// Rebuilds 2D chunk mesh geometry using jobs and unsafe pointers.
    /// </summary>
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [WorldSystemFilter(
        WorldSystemFilterFlags.LocalSimulation
            | WorldSystemFilterFlags.ClientSimulation
    )]
    public partial class ChunkMeshSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            if (
                !SystemAPI.TryGetSingleton<RegistryReference>(
                    out var registryRef
                )
            )
                return;

            var matQuery = GetEntityQuery(typeof(TilemapMaterialComponent));
            if (matQuery.IsEmpty)
                return;
            var matComp = matQuery.GetSingleton<TilemapMaterialComponent>();

            var material = matComp.Material;
            if (material == null)
                return;

            var defaultFilterSettings = Unity
                .Entities
                .Graphics
                .RenderFilterSettings
                .Default;

            using var chunksToInitialize = new NativeList<Entity>(
                Allocator.Temp
            );
            foreach (
                var (chunk, entity) in SystemAPI
                    .Query<RefRW<Chunk>>()
                    .WithNone<ChunkMeshState>()
                    .WithEntityAccess()
            )
            {
                chunksToInitialize.Add(entity);
            }

            if (chunksToInitialize.Length > 0)
            {
                // Same material/desc for every chunk initialized this frame
                // build once instead of reallocating a 1 element Material[] per chunk
                // and batch the structural changes (AddComponentData + RenderMeshUtility.AddComponents) 
                // through one ECB playback instead of a direct EntityManager call per chunk.
                var materials = new Material[] { material };
                var desc = new Unity.Rendering.RenderMeshDescription
                {
                    FilterSettings = defaultFilterSettings,
                    LightProbeUsage = LightProbeUsage.Off,
                };
                var ecb = new EntityCommandBuffer(Allocator.Temp);

                for (int i = 0; i < chunksToInitialize.Length; i++)
                {
                    Entity entity = chunksToInitialize[i];
                    // A Mesh pool would cut GC pressure here
                    // its return side belongs in the chunk despawn system.
                    var mesh = new Mesh();
                    mesh.MarkDynamic();
                    ecb.AddComponent(
                        entity,
                        new ChunkMeshState
                        {
                            Mesh = mesh,
                            RenderedVersionWall = 0,
                            RenderedVersionLogistics = 0,
                            RenderedVersionMain = 0,
                            RenderedVersionFluid = 0,
                            RenderedVersionOverlay = 0,
                        }
                    );

                    var renderMeshArray = new Unity.Rendering.RenderMeshArray(
                        materials,
                        new Mesh[] { mesh }
                    );
                    var materialMeshInfo =
                        Unity.Rendering.MaterialMeshInfo.FromRenderMeshArrayIndices(
                            0,
                            0
                        );
                    Unity.Rendering.RenderMeshUtility.AddComponents(
                        entity,
                        ecb,
                        desc,
                        renderMeshArray,
                        materialMeshInfo
                    );
                }

                ecb.Playback(EntityManager);
                ecb.Dispose();
            }

            // Complete and apply finished jobs. 
            // Querying the state component directly avoids manual component checks.
            foreach (
                var (chunk, meshState, entity) in SystemAPI
                    .Query<RefRW<Chunk>, ChunkMeshState>()
                    .WithEntityAccess()
            )
            {
                if (
                    meshState.HasPendingJob
                    && meshState.MeshJobHandle.IsCompleted
                )
                {
                    meshState.MeshJobHandle.Complete();

                    Mesh.ApplyAndDisposeWritableMeshData(
                        meshState.PendingMeshData,
                        meshState.Mesh
                    );
                    meshState.Mesh.RecalculateBounds();

                    var bounds = meshState.Mesh.bounds;
                    var aabb = new AABB
                    {
                        Center = bounds.center,
                        Extents = bounds.extents,
                    };
                    EntityManager.SetComponentData(
                        entity,
                        new Unity.Rendering.RenderBounds { Value = aabb }
                    );

                    meshState.HasPendingJob = false;
                }
            }

            // Schedule mesh jobs for modified chunks.
            foreach (
                var (chunk, meshState, entity) in SystemAPI
                    .Query<RefRW<Chunk>, ChunkMeshState>()
                    .WithEntityAccess()
            )
            {
                ref var chunkData = ref chunk.ValueRW;

                // Skip if a job is already in flight for this chunk
                if (meshState.HasPendingJob)
                    continue;

                // Rebuild mesh if any of the layers' versions has ticked ahead
                if (
                    chunkData.VersionWall > meshState.RenderedVersionWall
                    || chunkData.VersionLogistics > meshState.RenderedVersionLogistics
                    || chunkData.VersionMain > meshState.RenderedVersionMain
                    || chunkData.VersionFluid > meshState.RenderedVersionFluid
                    || chunkData.VersionOverlay > meshState.RenderedVersionOverlay
                    || meshState.RenderedVersionMain == 0
                ) // Start with initial draw.
                {
                    var wallTiles = EntityManager.GetBuffer<WallTile>(entity);
                    var logisticsTiles = EntityManager.GetBuffer<LogisticsTile>(
                        entity
                    );
                    var mainTiles = EntityManager.GetBuffer<MainTile>(entity);
                    var fluidTiles = EntityManager.GetBuffer<FluidTile>(entity);
                    var overlayTiles = EntityManager.GetBuffer<OverlayTile>(
                        entity
                    );

                    int nonAirCount =
                        CountNonAir(wallTiles)
                        + CountNonAir(logisticsTiles)
                        + CountNonAir(mainTiles)
                        + CountNonAir(fluidTiles)
                        + CountNonAir(overlayTiles);

                    if (nonAirCount == 0)
                    {
                        meshState.Mesh.Clear();
                        meshState.RenderedVersionWall = chunkData.VersionWall;
                        meshState.RenderedVersionLogistics = chunkData.VersionLogistics;
                        meshState.RenderedVersionMain = chunkData.VersionMain;
                        meshState.RenderedVersionFluid = chunkData.VersionFluid;
                        meshState.RenderedVersionOverlay = chunkData.VersionOverlay;
                        continue;
                    }

                    int vertexCount = nonAirCount * 4;
                    int indexCount = nonAirCount * 6;

                    // Copy buffers to avoid race conditions when archetypes change.
                    var wallCopy = CopyOrEmpty(wallTiles);
                    var logisticsCopy = CopyOrEmpty(logisticsTiles);
                    var mainCopy = CopyOrEmpty(mainTiles);
                    var fluidCopy = CopyOrEmpty(fluidTiles);
                    var overlayCopy = CopyOrEmpty(overlayTiles);

                    var meshDataArray = Mesh.AllocateWritableMeshData(1);
                    var job = new BuildChunkMeshJob
                    {
                        WallTiles = wallCopy,
                        LogisticsTiles = logisticsCopy,
                        MainTiles = mainCopy,
                        FluidTiles = fluidCopy,
                        OverlayTiles = overlayCopy,
                        Registry = registryRef.Reference,
                        MeshData = meshDataArray[0],
                        VertexCount = vertexCount,
                        IndexCount = indexCount,
                    };

                    meshState.MeshJobHandle = job.Schedule();
                    wallCopy.Dispose(meshState.MeshJobHandle);
                    logisticsCopy.Dispose(meshState.MeshJobHandle);
                    mainCopy.Dispose(meshState.MeshJobHandle);
                    fluidCopy.Dispose(meshState.MeshJobHandle);
                    overlayCopy.Dispose(meshState.MeshJobHandle);
                    Dependency = JobHandle.CombineDependencies(
                        Dependency,
                        meshState.MeshJobHandle
                    );
                    meshState.PendingMeshData = meshDataArray;
                    meshState.HasPendingJob = true;

                    meshState.RenderedVersionWall = chunkData.VersionWall;
                    meshState.RenderedVersionLogistics = chunkData.VersionLogistics;
                    meshState.RenderedVersionMain = chunkData.VersionMain;
                    meshState.RenderedVersionFluid = chunkData.VersionFluid;
                    meshState.RenderedVersionOverlay = chunkData.VersionOverlay;
                }
            }
        }

        // Assumes every tile layer struct has block ID at offset 0.
        private static int CountNonAir<T>(DynamicBuffer<T> buffer)
            where T : unmanaged
        {
            // Bounded by buffer length to prevent reading past native allocations.
            int tileCount = math.min(buffer.Length, ChunkLookup.ChunkArea);
            if (tileCount == 0)
                return 0;

            int count = 0;
            unsafe
            {
                void* ptr = buffer.GetUnsafeReadOnlyPtr();
                for (int idx = 0; idx < tileCount; idx++)
                {
                    byte* elementPtr =
                        (byte*)ptr + (idx * UnsafeUtility.SizeOf<T>());
                    if (*(uint*)elementPtr != 0)
                        count++;
                }
            }
            return count;
        }

        // Copies a dynamic buffer to a native array.
        private static NativeArray<T> CopyOrEmpty<T>(DynamicBuffer<T> buffer)
            where T : unmanaged
        {
            var copy = new NativeArray<T>(buffer.Length, Allocator.TempJob);
            if (buffer.Length > 0)
                copy.CopyFrom(buffer.AsNativeArray());
            return copy;
        }

        protected override void OnDestroy()
        {
            // Clean up background jobs, native buffers, and meshes when shutting down to prevent leaks.
            var query = EntityManager.CreateEntityQuery(typeof(ChunkMeshState));
            using (var entities = query.ToEntityArray(Allocator.TempJob))
            {
                for (int i = 0; i < entities.Length; i++)
                {
                    var meshState =
                        EntityManager.GetComponentObject<ChunkMeshState>(
                            entities[i]
                        );
                    if (meshState == null)
                        continue;

                    if (meshState.HasPendingJob)
                    {
                        meshState.MeshJobHandle.Complete();
                        meshState.PendingMeshData.Dispose();
                    }

                    if (meshState.Mesh != null)
                    {
                        Object.Destroy(meshState.Mesh);
                    }
                }
            }
        }

        [BurstCompile]
        public struct BuildChunkMeshJob : IJob
        {
            [ReadOnly]
            public NativeArray<WallTile> WallTiles;

            [ReadOnly]
            public NativeArray<LogisticsTile> LogisticsTiles;

            [ReadOnly]
            public NativeArray<MainTile> MainTiles;

            [ReadOnly]
            public NativeArray<FluidTile> FluidTiles;

            [ReadOnly]
            public NativeArray<OverlayTile> OverlayTiles;

            [ReadOnly]
            public BlobAssetReference<RegistryBlob> Registry;
            public Mesh.MeshData MeshData;
            public int VertexCount;
            public int IndexCount;

            public void Execute()
            {
                var attributes = new NativeArray<VertexAttributeDescriptor>(
                    3,
                    Allocator.Temp
                );
                attributes[0] = new VertexAttributeDescriptor(
                    VertexAttribute.Position,
                    VertexAttributeFormat.Float32,
                    3,
                    stream: 0
                );
                attributes[1] = new VertexAttributeDescriptor(
                    VertexAttribute.Color,
                    VertexAttributeFormat.Float32,
                    4,
                    stream: 0
                );
                attributes[2] = new VertexAttributeDescriptor(
                    VertexAttribute.TexCoord0,
                    VertexAttributeFormat.Float32,
                    2,
                    stream: 0
                );
                MeshData.SetVertexBufferParams(VertexCount, attributes);
                MeshData.SetIndexBufferParams(IndexCount, IndexFormat.UInt32);

                NativeArray<Vertex> vertices = MeshData.GetVertexData<Vertex>(
                    0
                );
                NativeArray<uint> indices = MeshData.GetIndexData<uint>();

                int quadIndex = 0;

                // Build each layer matching Z depth heights.
                BuildLayer(
                    ref quadIndex,
                    WallTiles,
                    1.0f,
                    ref vertices,
                    ref indices
                );
                BuildLayer(
                    ref quadIndex,
                    LogisticsTiles,
                    0.2f,
                    ref vertices,
                    ref indices
                );
                BuildLayer(
                    ref quadIndex,
                    MainTiles,
                    0.0f,
                    ref vertices,
                    ref indices
                );
                BuildLayer(
                    ref quadIndex,
                    FluidTiles,
                    0.0f,
                    ref vertices,
                    ref indices
                );
                BuildLayer(
                    ref quadIndex,
                    OverlayTiles,
                    -0.5f,
                    ref vertices,
                    ref indices
                );

                var subMesh = new SubMeshDescriptor(0, IndexCount)
                {
                    firstVertex = 0,
                    vertexCount = VertexCount,
                };
                MeshData.subMeshCount = 1;
                MeshData.SetSubMesh(0, subMesh);
            }

            // Build vertex and index coordinates for active tiles in a layer.
            private void BuildLayer<T>(
                ref int quadIndex,
                NativeArray<T> buffer,
                float zOffset,
                ref NativeArray<Vertex> vertices,
                ref NativeArray<uint> indices
            )
                where T : unmanaged
            {
                // Bounded by buffer length to avoid reading out of bounds.
                int tileCount = math.min(buffer.Length, ChunkLookup.ChunkArea);
                if (tileCount == 0)
                    return;

                ref var objects = ref Registry.Value.Objects;

                for (int ty = 0; ty < ChunkLookup.ChunkSize; ty++)
                {
                    for (int tx = 0; tx < ChunkLookup.ChunkSize; tx++)
                    {
                        int tileIndex = ty * ChunkLookup.ChunkSize + tx;
                        if (tileIndex >= tileCount)
                            continue;

                        // Read block ID and metadata using unsafe pointers.
                        uint blockId = 0;
                        byte tileMetadata = 0;

                        unsafe
                        {
                            void* ptr = buffer.GetUnsafeReadOnlyPtr();
                            // Assumes block ID is at offset 0 and metadata is at offset 4.
                            byte* elementPtr =
                                (byte*)ptr
                                + (tileIndex * UnsafeUtility.SizeOf<T>());
                            blockId = *(uint*)elementPtr;
                            tileMetadata = *(elementPtr + 4);
                        }

                        if (blockId == 0)
                            continue;

                        float uMin = 0f,
                            vMin = 0f,
                            uMax = 0f,
                            vMax = 0f;
                        if (objects.TryGetObject(blockId, out var obj))
                        {
                            if (
                                obj.Id != 0
                                && (obj.Category & ItemCategory.Block) != 0
                            )
                            {
                                uMin = obj.BlockConfig.BlockUvs.UMin;
                                vMin = obj.BlockConfig.BlockUvs.VMin;
                                uMax = obj.BlockConfig.BlockUvs.UMax;
                                vMax = obj.BlockConfig.BlockUvs.VMax;

                                if (
                                    obj.BlockConfig.Width > 1
                                    || obj.BlockConfig.Height > 1
                                )
                                {
                                    int localX = tileMetadata & 3;
                                    int localY = (tileMetadata >> 2) & 3;
                                    float w =
                                        (uMax - uMin) / obj.BlockConfig.Width;
                                    float h =
                                        (vMax - vMin) / obj.BlockConfig.Height;
                                    uMin = uMin + localX * w;
                                    vMin = vMin + localY * h;
                                    uMax = uMin + w;
                                    vMax = vMin + h;
                                }
                            }
                        }

                        int vOffset = quadIndex * 4;

                        vertices[vOffset + 0] = new Vertex
                        {
                            Position = new float3(
                                tx - 0.5f,
                                ty - 0.5f,
                                zOffset
                            ),
                            Color = new float4(1f, 1f, 1f, 1f),
                            UV = new float2(uMin, vMin),
                        };
                        vertices[vOffset + 1] = new Vertex
                        {
                            Position = new float3(
                                tx + 0.5f,
                                ty - 0.5f,
                                zOffset
                            ),
                            Color = new float4(1f, 1f, 1f, 1f),
                            UV = new float2(uMax, vMin),
                        };
                        vertices[vOffset + 2] = new Vertex
                        {
                            Position = new float3(
                                tx + 0.5f,
                                ty + 0.5f,
                                zOffset
                            ),
                            Color = new float4(1f, 1f, 1f, 1f),
                            UV = new float2(uMax, vMax),
                        };
                        vertices[vOffset + 3] = new Vertex
                        {
                            Position = new float3(
                                tx - 0.5f,
                                ty + 0.5f,
                                zOffset
                            ),
                            Color = new float4(1f, 1f, 1f, 1f),
                            UV = new float2(uMin, vMax),
                        };

                        int iOffset = quadIndex * 6;
                        indices[iOffset + 0] = (uint)(vOffset + 0);
                        indices[iOffset + 1] = (uint)(vOffset + 2);
                        indices[iOffset + 2] = (uint)(vOffset + 1);
                        indices[iOffset + 3] = (uint)(vOffset + 0);
                        indices[iOffset + 4] = (uint)(vOffset + 3);
                        indices[iOffset + 5] = (uint)(vOffset + 2);

                        quadIndex++;
                    }
                }
            }
        }
    }
}
