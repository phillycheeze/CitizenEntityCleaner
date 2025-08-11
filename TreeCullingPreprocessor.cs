using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Game.Common;
using Game.Objects;
using Game.Rendering;
using Game.Prefabs;
using System.Runtime.CompilerServices;
using Colossal.Mathematics;
using Game.Tools;
using System;
using Unity.Burst.Intrinsics;
using Unity.Collections.LowLevel.Unsafe;
using Colossal.Logging;

namespace CitizenEntityCleaner
{
    /// <summary>
    /// Pre-processing system that optimizes tree entities before the tree culling jobs run.
    /// This approach is much simpler than Harmony patches and can provide significant performance improvements.
    /// </summary>
    public partial class TreeCullingPreprocessor : SystemBase
    {
        private static ILog s_log = Mod.log;
        private EntityQuery m_TreeQuery;
        private float3 m_LastCameraPosition;
        private float m_CameraMovementThreshold = 5.0f; // Only update when camera moves significantly

        // System dependencies for camera access
        private CameraUpdateSystem m_CameraUpdateSystem;

        // Safe state tracking without modifying vanilla components
        private NativeParallelHashMap<Entity, OptimizationState> m_OptimizationStates;

        // Performance optimization caches
        private NativeParallelHashMap<Entity, CachedTreeData> m_TreeCache;
        private NativeParallelHashMap<int, float> m_DistanceCache; // Hash(position) -> distance

        protected override void OnCreate()
        {
            m_TreeQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[]
                {
                    ComponentType.ReadWrite<CullingInfo>(),
                    ComponentType.ReadOnly<Tree>(),
                    ComponentType.ReadOnly<Transform>(),
                    ComponentType.ReadOnly<PrefabRef>()
                },
                None = new ComponentType[]
                {
                    ComponentType.ReadOnly<Deleted>(),
                    ComponentType.ReadOnly<Temp>()
                }
            });

            m_CameraUpdateSystem = World.GetExistingSystemManaged<CameraUpdateSystem>();

            s_log.Info("OnCreate, past m_CameraUpdateSystem fetch");
            m_OptimizationStates = new NativeParallelHashMap<Entity, OptimizationState>(128, Allocator.Persistent);
            m_TreeCache = new NativeParallelHashMap<Entity, CachedTreeData>(128, Allocator.Persistent);
            m_DistanceCache = new NativeParallelHashMap<int, float>(128, Allocator.Persistent);

            RequireForUpdate(m_TreeQuery);
            base.OnCreate();

            s_log.Info("OnCreate, at the very end");
        }

        protected override void OnDestroy()
        {
            if (m_OptimizationStates.IsCreated)
                m_OptimizationStates.Dispose();
            if (m_TreeCache.IsCreated)
                m_TreeCache.Dispose();
            if (m_DistanceCache.IsCreated)
                m_DistanceCache.Dispose();
            base.OnDestroy();
        }

        protected override void OnUpdate()
        {
            if (m_CameraUpdateSystem == null)
            {
                s_log.Info("OnUpdate, but camera update system is null!!!!!!!");
                return;
            }

            if (m_TreeQuery.IsEmpty)
            {
                s_log.Info("OnUpdate, but tree query is empty!!!!!!");
                return;
            }

            float3 cameraPosition = m_CameraUpdateSystem.position;
            float3 cameraDirection = m_CameraUpdateSystem.direction;

            // Only process if camera moved significantly
            bool shouldUpdate = math.distance(cameraPosition, m_LastCameraPosition) > m_CameraMovementThreshold;

            if (!shouldUpdate) return;

            m_LastCameraPosition = cameraPosition;

            // Schedule the tree preprocessing job
            var preprocessJob = new TreePreprocessJob
            {
                CullingInfoTypeHandle = GetComponentTypeHandle<CullingInfo>(),
                TransformTypeHandle = GetComponentTypeHandle<Transform>(true),
                PrefabRefTypeHandle = GetComponentTypeHandle<PrefabRef>(true),
                TreeTypeHandle = GetComponentTypeHandle<Tree>(true),
                EntityTypeHandle = GetEntityTypeHandle(),

                CameraPosition = cameraPosition,
                CameraDirection = cameraDirection,
                FrameTime = UnityEngine.Time.time,

                NearDistance = 30f,     // Very close trees - always visible, no optimization
                MidDistance = 150f,     // Medium distance - light optimization  
                FarDistance = 400f,     // Far trees - aggressive LOD optimization
                VeryFarDistance = 800f, // Very far trees - maximum culling optimization

                // Caches for performance
                OptimizationStates = m_OptimizationStates,
                TreeCache = m_TreeCache,
                DistanceCache = m_DistanceCache
            };

            Dependency = preprocessJob.Schedule(m_TreeQuery, Dependency);
        }
    }

    /// <summary>
    /// Tracks which optimizations have been applied to avoid permanent modifications
    /// </summary>
    public struct OptimizationState : IEquatable<OptimizationState>
    {
        public byte flags;
        public Bounds3 originalBounds;
        public byte originalMinLod;

        public const byte BOUNDS_OPTIMIZED = 1;
        public const byte LOD_OPTIMIZED = 2;
        public const byte QUANTIZED = 4;
        public const byte DISTANCE_CACHED = 8;

        public bool Equals(OptimizationState other) => flags == other.flags;
        public override int GetHashCode() => flags.GetHashCode();
    }

    /// <summary>
    /// Cached tree data to avoid redundant computations
    /// </summary>
    public struct CachedTreeData : IEquatable<CachedTreeData>
    {
        public float lastDistance;
        public float lastFrameTime;
        public byte distanceBucket;    // 0=near, 1=mid, 2=far, 3=very far

        public bool Equals(CachedTreeData other) =>
            lastDistance == other.lastDistance && distanceBucket == other.distanceBucket;
        public override int GetHashCode() => lastDistance.GetHashCode();
    }

    /// <summary>
    /// Burst-compiled job that preprocesses tree entities for optimal culling performance
    /// </summary>
    [BurstCompile]
    public struct TreePreprocessJob : IJobChunk
    {
        public ComponentTypeHandle<CullingInfo> CullingInfoTypeHandle;

        [ReadOnly]
        public ComponentTypeHandle<Transform> TransformTypeHandle;

        [ReadOnly]
        public ComponentTypeHandle<PrefabRef> PrefabRefTypeHandle;

        [ReadOnly]
        public ComponentTypeHandle<Tree> TreeTypeHandle;

        [ReadOnly]
        public EntityTypeHandle EntityTypeHandle;

        [ReadOnly]
        public float3 CameraPosition;

        [ReadOnly]
        public float3 CameraDirection;

        [ReadOnly]
        public float FrameTime;

        [ReadOnly]
        public float NearDistance;

        [ReadOnly]
        public float MidDistance;

        [ReadOnly]
        public float FarDistance;

        [ReadOnly]
        public float VeryFarDistance;

        public NativeParallelHashMap<Entity, OptimizationState> OptimizationStates;

        public NativeParallelHashMap<Entity, CachedTreeData> TreeCache;

        public NativeParallelHashMap<int, float> DistanceCache;

        public unsafe void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            var cullingInfoArray = chunk.GetNativeArray(ref CullingInfoTypeHandle);
            var transformArray = chunk.GetNativeArray(ref TransformTypeHandle);
            var treeArray = chunk.GetNativeArray(ref TreeTypeHandle);
            var entityArray = chunk.GetNativeArray(EntityTypeHandle);

            // Get raw pointers for Burst-compatible access
            var cullingInfoPtr = (CullingInfo*)cullingInfoArray.GetUnsafePtr();
            var transformPtr = (Transform*)transformArray.GetUnsafeReadOnlyPtr();
            var treePtr = (Tree*)treeArray.GetUnsafeReadOnlyPtr();

            for (int i = 0; i < chunk.Count; i++)
            {
                var entity = entityArray[i];
                var transform = transformPtr[i];
                var tree = treePtr[i];

                float distanceToCamera = GetCachedDistance(entity, transform.m_Position);

                // Apply aggressive optimizations based on distance
                OptimizeCullingInfoAdvanced(ref cullingInfoPtr[i], entity, transform, tree, distanceToCamera);
            }
        }

        /// <summary>
        /// Gets cached distance or calculates and caches new distance
        /// </summary>
        private float GetCachedDistance(Entity entity, float3 position)
        {
            int posHash = ((int)(position.x / 4f) & 0x7FFF) + ((int)(position.z / 4f) & 0x7FFF) * 32768;

            if (DistanceCache.TryGetValue(posHash, out float cachedDistance))
            {
                return cachedDistance;
            }

            float distance = math.distance(position, CameraPosition);
            DistanceCache[posHash] = distance;
            return distance;
        }

        /// <summary>
        /// Advanced optimization with caching and aggressive culling
        /// </summary>
        private void OptimizeCullingInfoAdvanced(ref CullingInfo cullingInfo, Entity entity, Transform transform, Tree tree, float distance)
        {
            TreeState treeState = tree.m_State;
            bool isSmallishTree = (treeState == TreeState.Teen || treeState == TreeState.Stump);

            byte distanceBucket = GetDistanceBucket(distance);

            if (distance < 10f)
            {
                return; // Very close distance should just leave everything to vanilla
            }

            // === AGGRESSIVE CULLING FOR DISTANT TREES ===
            if (distance > VeryFarDistance)
            {
                // Pre-cull extremely distant, small trees by making them essentially invisible
                if (isSmallishTree)
                {
                    cullingInfo.m_MinLod = 255; // Max LOD - won't render at any distance
                    return;
                }
            }

            // === PERFORMANCE OPTIMIZATIONS ===
            ApplyDistanceBasedOptimizations(ref cullingInfo, entity, distance, distanceBucket, treeState);

            var cachedData = new CachedTreeData
            {
                lastDistance = distance,
                lastFrameTime = FrameTime,
                distanceBucket = distanceBucket
            };
            TreeCache[entity] = cachedData;

        }

        private byte GetDistanceBucket(float distance)
        {
            if (distance < NearDistance) return 0;
            if (distance < MidDistance) return 1;
            if (distance < FarDistance) return 2;
            return 3;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ApplyDistanceBasedOptimizations(ref CullingInfo cullingInfo, Entity entity, float distance, byte distanceBucket, TreeState treeState)
        {
            bool hasState = OptimizationStates.TryGetValue(entity, out OptimizationState state);

            if (!hasState)
            {
                state = new OptimizationState
                {
                    flags = 0,
                    originalBounds = cullingInfo.m_Bounds,
                    originalMinLod = cullingInfo.m_MinLod,
                };
            }

            // Restore to original if moving to near distance
            if (distanceBucket == 0)
            {
                RestoreOriginalValues(ref cullingInfo, ref state);
            }
            else
            {
                ApplyOptimizationForBucket(ref cullingInfo, ref state, distanceBucket, distance, treeState);
            }

            OptimizationStates[entity] = state;
        }

        private void RestoreOriginalValues(ref CullingInfo cullingInfo, ref OptimizationState state)
        {
            if (state.flags != 0)
            {
                cullingInfo.m_Bounds = state.originalBounds;
                cullingInfo.m_MinLod = state.originalMinLod;
                state.flags = 0;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ApplyOptimizationForBucket(ref CullingInfo cullingInfo, ref OptimizationState state, byte distanceBucket, float distance, TreeState treeState)
        {
            bool isDeadOrTeen = treeState == TreeState.Dead || treeState == TreeState.Teen;

            switch (distanceBucket)
            {
                case 0:
                    return;

                case 1:
                    if ((state.flags & OptimizationState.QUANTIZED) == 0)
                    {
                        QuantizeBounds(ref cullingInfo, 0.5f);
                        state.flags |= OptimizationState.QUANTIZED;
                    }
                    break;

                case 2:
                case 3:
                    if ((state.flags & OptimizationState.QUANTIZED) == 0)
                    {
                        float gridSize = math.lerp(0.5f, 1.0f, (distanceBucket - 1) / 3f);
                        QuantizeBounds(ref cullingInfo, gridSize);
                        state.flags |= OptimizationState.QUANTIZED;
                    }

                    if ((state.flags & OptimizationState.LOD_OPTIMIZED) == 0 && isDeadOrTeen)
                    {
                        // Increase MinLod to shortcircuit expensive vanilla CalculateLod() calls
                        byte lodIncrease = (byte)(10 + (distanceBucket - 2) * 20); // 15, 35, 55
                        cullingInfo.m_MinLod = (byte)math.min(255, cullingInfo.m_MinLod + lodIncrease);
                        state.flags |= OptimizationState.LOD_OPTIMIZED;
                    }

                    if ((state.flags & OptimizationState.BOUNDS_OPTIMIZED) == 0 && distanceBucket >= 3)
                    {
                        float shrinkFactor = isDeadOrTeen ?
                            math.lerp(0.7f, 0.2f, (distanceBucket - 3) / 1f) :
                            math.lerp(0.8f, 0.5f, (distanceBucket - 3) / 1f);

                        ShrinkBounds(ref cullingInfo, shrinkFactor);
                        state.flags |= OptimizationState.BOUNDS_OPTIMIZED;
                    }
                    break;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void QuantizeBounds(ref CullingInfo cullingInfo, float gridSize)
        {
            cullingInfo.m_Bounds.min = math.floor(cullingInfo.m_Bounds.min / gridSize) * gridSize;
            cullingInfo.m_Bounds.max = math.ceil(cullingInfo.m_Bounds.max / gridSize) * gridSize;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ShrinkBounds(ref CullingInfo cullingInfo, float shrinkFactor)
        {
            float3 center = (cullingInfo.m_Bounds.min + cullingInfo.m_Bounds.max) * 0.5f;
            float3 size = (cullingInfo.m_Bounds.max - cullingInfo.m_Bounds.min) * shrinkFactor;
            cullingInfo.m_Bounds.min = center - size * 0.5f;
            cullingInfo.m_Bounds.max = center + size * 0.5f;
            cullingInfo.m_Radius = math.length(size) * 0.5f;
        }
    }
}


