using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Game.Rendering;
using Unity.Jobs;
using Colossal.Collections;
using Game.Common;
using Game.Objects;
using Colossal.Logging;

namespace CitizenEntityCleaner
{
    [UpdateAfter(typeof(SearchSystem))]
    public partial class OcclusionCullingSystem : SystemBase
    {
        private static ILog s_log = Mod.log;
        private CameraUpdateSystem m_CameraSystem;
        private float3 m_LastCameraPos;
        private float3 m_LastCameraDir;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_CameraSystem = World.GetExistingSystemManaged<CameraUpdateSystem>();
            m_LastCameraPos = float3.zero;
            m_LastCameraDir = float3.zero;
        }

        protected override void OnUpdate()
        {
            // Bail if the camera system isn't ready
            if (!m_CameraSystem.TryGetLODParameters(out var lodParams))
            {
                s_log.Info("Camera system not ready");
                return;
            }

            float3 camPos = lodParams.cameraPosition;
            float3 camDir = m_CameraSystem.activeViewer.forward;

            s_log.Info($"...Received camera system: {camPos} and {camDir}");

            // Movement/rotation thresholds
            float moveDist = math.distance(camPos, m_LastCameraPos);
            float rotAngle = math.degrees(math.acos(math.clamp(math.dot(camDir, m_LastCameraDir), -1f, 1f)));
            bool camMoved = moveDist > 2f || rotAngle > 1f;
            if (!camMoved)
            {
                s_log.Info($"Cam hasn't moved,skipping....");
                return;
            }

            // Get the existing static search tree and wait for its build
            var searchSystem = World.GetExistingSystemManaged<SearchSystem>();
            var tree = searchSystem.GetStaticSearchTree(readOnly: true, out var treeDeps);
            s_log.Info($"Static tree: created:{tree.IsCreated}");
            // Chain dependency on the SearchSystem's build job
            Dependency = JobHandle.CombineDependencies(Dependency, treeDeps);
            // Get the CullingInfo lookup (no need to manually update dependencies)
            var lookup = GetComponentLookup<CullingInfo>(false);
            s_log.Info("Starting to build occlusion job");
            
            // Create node buffers like PreCullingSystem does
            var nodeBuffer = new NativeArray<int>(1536, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            var subDataBuffer = new NativeArray<int>(1536, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            
            // Schedule the occlusion filtering job after the tree is built
            var occlusionJob = new OcclusionJob
            {
                tree = tree,
                nodeBuffer = nodeBuffer,
                subDataBuffer = subDataBuffer,
                cullingInfoLookup = lookup,
                cameraPosition = camPos,
                cameraDirection = camDir
            };
            var occlHandle = occlusionJob.Schedule(nodeBuffer.Length, 1, Dependency);
            // Register our read with the SearchSystem so future builds wait for us
            searchSystem.AddStaticSearchTreeReader(occlHandle);
            Dependency = occlHandle;
            s_log.Info("Occlusion job scheduled");

            // Cache camera state until next significant move
            m_LastCameraPos = camPos;
            m_LastCameraDir = camDir;
        }

        // Use the same pattern as PreCullingSystem - parallel job with node buffers
        [BurstCompile]
        private struct OcclusionJob : IJobParallelFor
        {
            [ReadOnly] public NativeQuadTree<Entity, QuadTreeBoundsXZ> tree;
            [ReadOnly] public NativeArray<int> nodeBuffer;
            [ReadOnly] public NativeArray<int> subDataBuffer;
            [ReadOnly] public float3 cameraPosition;
            [ReadOnly] public float3 cameraDirection;
            [NativeDisableParallelForRestriction] public ComponentLookup<CullingInfo> cullingInfoLookup;

            public void Execute(int index)
            {
                // Use the same iterator pattern as PreCullingSystem
                var iterator = new OcclusionIterator
                {
                    cullingInfoLookup = cullingInfoLookup,
                    cameraPosition = cameraPosition,
                    cameraDirection = cameraDirection
                };
                
                // Iterate safely using node buffers like PreCullingSystem
                tree.Iterate(ref iterator, subDataBuffer[index], nodeBuffer[index]);
            }

            // Simple iterator that performs occlusion testing directly like PreCullingSystem
            private struct OcclusionIterator : INativeQuadTreeIterator<Entity, QuadTreeBoundsXZ>
            {
                [NativeDisableParallelForRestriction] public ComponentLookup<CullingInfo> cullingInfoLookup;
                [ReadOnly] public float3 cameraPosition;
                [ReadOnly] public float3 cameraDirection;

                public bool Intersect(QuadTreeBoundsXZ bounds)
                {
                    // Basic distance culling like PreCullingSystem does
                    var center = (bounds.m_Bounds.min + bounds.m_Bounds.max) * 0.5f;
                    var distance = math.distance(center, cameraPosition);
                    return distance <= 200f; // Only process nearby entities
                }

                public void Iterate(QuadTreeBoundsXZ bounds, Entity entity)
                {
                    // Safety check
                    if (!cullingInfoLookup.HasComponent(entity))
                        return;

                    // Simple distance and direction-based culling
                    var center = (bounds.m_Bounds.min + bounds.m_Bounds.max) * 0.5f;
                    var toEntity = center - cameraPosition;
                    var distance = math.length(toEntity);
                    
                    bool shouldPass = distance < 150f; // Simple distance test
                    
                    ref var info = ref cullingInfoLookup.GetRefRW(entity).ValueRW;
                    info.m_PassedCulling = (byte)(shouldPass ? 1 : 0);
                }
            }
        }
    }
}
