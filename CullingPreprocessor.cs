using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Game.Common;
using Game.Objects;
using Game.Rendering;
using Game.Prefabs;
using Game.Simulation;
using Colossal.Logging;

namespace CitizenEntityCleaner
{
    /// <summary>
    /// Comprehensive culling preprocessor that optimizes ALL entities before vanilla culling runs.
    /// Implements Phase 1 optimizations: camera movement thresholds, distance caching, early rejection.
    /// </summary>
    public partial class CullingPreprocessor : SystemBase
    {
        private static ILog s_log = Mod.log;
        
        private CameraUpdateSystem m_CameraUpdateSystem;
        private float3 m_LastCameraPosition;
        private float3 m_LastCameraDirection;
        private float m_CameraMovementThreshold = 2.0f;
        private float m_CameraRotationThreshold = 0.1f;

        private EntityQuery m_AllCullableQuery;
        private EntityQuery m_StaticObjectQuery;
        private EntityQuery m_DynamicObjectQuery;

        private NativeParallelHashMap<int, float> m_DistanceCache;
        private int m_FrameCounter;
        private int m_CacheCleanupInterval = 600;
        
        // Track whether static objects are currently hidden
        private bool m_StaticObjectsHidden = false;
        
        private NativeParallelHashMap<Entity, BoundsMask> m_OriginalMasks;
        
        // Dynamic object optimization tracking
        private int m_DynamicOptimizationFrame = 0;

        protected override void OnCreate()
        {
            m_AllCullableQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[]
                {
                    ComponentType.ReadWrite<CullingInfo>(),
                    ComponentType.ReadOnly<Transform>()
                },
                None = new ComponentType[]
                {
                    ComponentType.ReadOnly<Deleted>(),
                    ComponentType.ReadOnly<Temp>()
                }
            });

            m_StaticObjectQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[]
                {
                    ComponentType.ReadWrite<CullingInfo>(),
                    ComponentType.ReadOnly<Transform>()
                },
                Any = new ComponentType[]
                {
                    ComponentType.ReadOnly<Tree>(),
                    ComponentType.ReadOnly<Plant>(),
                    ComponentType.ReadOnly<Building>(),
                    ComponentType.ReadOnly<Game.Objects.Static>()
                },
                None = new ComponentType[]
                {
                    ComponentType.ReadOnly<Deleted>(),
                    ComponentType.ReadOnly<Temp>(),
                    ComponentType.ReadOnly<Moving>()
                }
            });

            m_DynamicObjectQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[]
                {
                    ComponentType.ReadWrite<CullingInfo>(),
                    ComponentType.ReadOnly<Transform>()
                },
                Any = new ComponentType[]
                {
                    ComponentType.ReadOnly<Moving>(),
                    ComponentType.ReadOnly<Updated>()
                },
                None = new ComponentType[]
                {
                    ComponentType.ReadOnly<Deleted>(),
                    ComponentType.ReadOnly<Temp>()
                }
            });

            m_CameraUpdateSystem = World.GetExistingSystemManaged<CameraUpdateSystem>();
            m_DistanceCache = new NativeParallelHashMap<int, float>(1024, Allocator.Persistent);
            m_OriginalMasks = new NativeParallelHashMap<Entity, BoundsMask>(1024, Allocator.Persistent);

            RequireForUpdate(m_AllCullableQuery, m_StaticObjectQuery, m_DynamicObjectQuery);
            
            s_log.Info("CullingPreprocessor created - ready to optimize vanilla culling performance");
        }

        protected override void OnDestroy()
        {
            if (m_DistanceCache.IsCreated)
                m_DistanceCache.Dispose();
            if (m_OriginalMasks.IsCreated)
                m_OriginalMasks.Dispose();

        }

        protected override void OnUpdate()
        {
            if (m_CameraUpdateSystem == null) return;

            float3 currentCameraPos = m_CameraUpdateSystem.position;
            float3 currentCameraDir = m_CameraUpdateSystem.direction;

            bool cameraMovedSignificantly = HasCameraMovedSignificantly(currentCameraPos, currentCameraDir);
            
            if (!cameraMovedSignificantly)
            {
                // Camera hasn't moved - hide static objects from TreeCullingJobs if not already hidden
                if (!m_StaticObjectsHidden)
                {
                    HideStaticObjects();
                }
            }
            else
            {
                // Camera moved significantly - restore static objects if they were hidden
                if (m_StaticObjectsHidden)
                {
                    RestoreStaticObjects();
                }

                m_LastCameraPosition = currentCameraPos;
                m_LastCameraDirection = currentCameraDir;
            }

            // Always optimize dynamic objects regardless of camera movement
            // Dynamic objects (vehicles, citizens) move independently of camera
            OptimizeDynamicObjects(currentCameraPos);

            m_FrameCounter++;
            if (m_FrameCounter % m_CacheCleanupInterval == 0)
            {
                CleanupDistanceCache();
            }
        }

        private bool HasCameraMovedSignificantly(float3 currentPos, float3 currentDir)
        {
            float positionDelta = math.distance(currentPos, m_LastCameraPosition);
            float rotationDelta = math.distance(currentDir, m_LastCameraDirection);

            return positionDelta > m_CameraMovementThreshold || rotationDelta > m_CameraRotationThreshold;
        }

        private void HideStaticObjects()
        {
            var hideJob = new HideStaticObjectsJob
            {
                CullingInfoTypeHandle = GetComponentTypeHandle<CullingInfo>(),
                EntityTypeHandle = GetEntityTypeHandle(),
                OriginalMasks = m_OriginalMasks.AsParallelWriter()
            };

            Dependency = hideJob.ScheduleParallel(m_StaticObjectQuery, Dependency);
            m_StaticObjectsHidden = true;
            
            s_log.Info($"Hidden {m_StaticObjectQuery.CalculateEntityCount()} static objects from TreeCullingJobs");
        }

        private void RestoreStaticObjects()
        {
            var restoreJob = new RestoreStaticObjectsJob
            {
                CullingInfoTypeHandle = GetComponentTypeHandle<CullingInfo>(),
                EntityTypeHandle = GetEntityTypeHandle(),
                OriginalMasks = m_OriginalMasks
            };

            Dependency = restoreJob.ScheduleParallel(m_StaticObjectQuery, Dependency);
            m_StaticObjectsHidden = false;
            
            s_log.Info($"Restored {m_StaticObjectQuery.CalculateEntityCount()} static objects for TreeCullingJobs");
        }

        private void OptimizeDynamicObjects(float3 cameraPosition)
        {
            m_DynamicOptimizationFrame++;
            
            // Single efficient job that handles both restore and hide logic
            var optimizeJob = new OptimizeDynamicObjectsJob
            {
                CullingInfoTypeHandle = GetComponentTypeHandle<CullingInfo>(),
                TransformTypeHandle = GetComponentTypeHandle<Transform>(true),
                EntityTypeHandle = GetEntityTypeHandle(),
                CameraPosition = cameraPosition,
                CurrentFrame = m_DynamicOptimizationFrame,
                
                // Distance thresholds for update frequency
                NearDistance = 50f,   // Update every frame
                MidDistance = 200f,   // Update every 2nd frame  
                FarDistance = 500f,   // Update every 3rd frame
                VeryFarDistance = 1000f // Update every 4th frame
            };

            Dependency = optimizeJob.ScheduleParallel(m_DynamicObjectQuery, Dependency);
        }

        private void CleanupDistanceCache()
        {
            // Simple cleanup - clear cache periodically to prevent unbounded growth
            m_DistanceCache.Clear();
            m_OriginalMasks.Clear();

        }
    }



    /// <summary>
    /// Efficient single-pass dynamic object optimization with smart mask management
    /// </summary>
    [BurstCompile]
    public struct OptimizeDynamicObjectsJob : IJobChunk
    {
        public ComponentTypeHandle<CullingInfo> CullingInfoTypeHandle;
        
        [ReadOnly]
        public ComponentTypeHandle<Transform> TransformTypeHandle;
        
        [ReadOnly]
        public EntityTypeHandle EntityTypeHandle;
        
        [ReadOnly]
        public float3 CameraPosition;
        
        [ReadOnly]
        public int CurrentFrame;
        
        [ReadOnly] public float NearDistance;
        [ReadOnly] public float MidDistance;
        [ReadOnly] public float FarDistance;
        [ReadOnly] public float VeryFarDistance;

        // Custom hidden mask for dynamic objects
        private const BoundsMask DYNAMIC_HIDDEN_MASK = (BoundsMask)0x4000;

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            var cullingInfoArray = chunk.GetNativeArray(ref CullingInfoTypeHandle);
            var transformArray = chunk.GetNativeArray(ref TransformTypeHandle);
            
            for (int i = 0; i < chunk.Count; i++)
            {
                var transform = transformArray[i];
                var cullingInfo = cullingInfoArray[i];
                
                float distance = math.distance(transform.m_Position, CameraPosition);
                byte distanceBucket = GetDistanceBucket(distance);
                
                // Determine if this object should be processed this frame
                bool shouldProcess = ShouldUpdateThisFrame(distanceBucket);
                bool isCurrentlyHidden = cullingInfo.m_Bounds.m_Mask == DYNAMIC_HIDDEN_MASK;
                
                if (shouldProcess && isCurrentlyHidden)
                {
                    // Restore to normal processing - use default normal layers
                    // PHIL TODO: Is it always normal layers?
                    cullingInfo.m_Bounds.m_Mask = BoundsMask.NormalLayers;
                    cullingInfoArray[i] = cullingInfo;
                }
                else if (!shouldProcess && !isCurrentlyHidden)
                {
                    // Hide from processing this frame
                    cullingInfo.m_Bounds.m_Mask = DYNAMIC_HIDDEN_MASK;
                    cullingInfoArray[i] = cullingInfo;
                }
                // If shouldProcess == !isCurrentlyHidden, no change needed (already in correct state)
            }
        }

        private byte GetDistanceBucket(float distance)
        {
            if (distance < NearDistance) return 0;
            if (distance < MidDistance) return 1;
            if (distance < FarDistance) return 2;
            return 3;
        }

        private bool ShouldUpdateThisFrame(byte distanceBucket)
        {
            // Always update near objects
            if (distanceBucket == 0) return true;
            
            // For distant objects, check update frequency
            int updateInterval = distanceBucket + 1; // 1, 2, 3, 4 frame intervals
            
            return (CurrentFrame % updateInterval) == 0;
        }
    }

    /// <summary>
    /// Hides static objects from TreeCullingJobs by setting their BoundsMask to a hidden layer
    /// </summary>
    [BurstCompile]
    public struct HideStaticObjectsJob : IJobChunk
    {
        public ComponentTypeHandle<CullingInfo> CullingInfoTypeHandle;
        
        [ReadOnly]
        public EntityTypeHandle EntityTypeHandle;
        
        public NativeParallelHashMap<Entity, BoundsMask>.ParallelWriter OriginalMasks;
        
        // Custom hidden mask that won't be in vanilla m_VisibleMask
        private const BoundsMask HIDDEN_MASK = (BoundsMask)0x8000; // High bit - won't conflict with vanilla layers

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            var cullingInfoArray = chunk.GetNativeArray(ref CullingInfoTypeHandle);
            var entityArray = chunk.GetNativeArray(EntityTypeHandle);
            
            for (int i = 0; i < chunk.Count; i++)
            {
                var entity = entityArray[i];
                var cullingInfo = cullingInfoArray[i];
                
                // Store original mask for restoration
                OriginalMasks.TryAdd(entity, cullingInfo.m_Bounds.m_Mask);
                
                // Replace with hidden mask - TreeCullingJobs will skip these
                cullingInfo.m_Bounds.m_Mask = HIDDEN_MASK;
                cullingInfoArray[i] = cullingInfo;
            }
        }
    }

    /// <summary>
    /// Restores static objects' original BoundsMask so TreeCullingJobs can process them again
    /// </summary>
    [BurstCompile]
    public struct RestoreStaticObjectsJob : IJobChunk
    {
        public ComponentTypeHandle<CullingInfo> CullingInfoTypeHandle;
        
        [ReadOnly]
        public EntityTypeHandle EntityTypeHandle;
        
        [ReadOnly]
        public NativeParallelHashMap<Entity, BoundsMask> OriginalMasks;

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            var cullingInfoArray = chunk.GetNativeArray(ref CullingInfoTypeHandle);
            var entityArray = chunk.GetNativeArray(EntityTypeHandle);
            
            for (int i = 0; i < chunk.Count; i++)
            {
                var entity = entityArray[i];
                var cullingInfo = cullingInfoArray[i];
                
                // Restore original mask if we have it stored
                if (OriginalMasks.TryGetValue(entity, out BoundsMask originalMask))
                {
                    cullingInfo.m_Bounds.m_Mask = originalMask;
                    cullingInfoArray[i] = cullingInfo;
                }
            }
        }
    }
}
