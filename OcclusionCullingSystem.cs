using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Game.Rendering;
using Game.Objects;
using CitizenEntityCleaner;

namespace CitizenEntityCleaner
{
    // Runs before SearchSystem to tag and cull occluded entities
    [UpdateBefore(typeof(SearchSystem))]
    public partial class OcclusionCullingSystem : SystemBase
    {
        private CameraUpdateSystem m_CameraSystem;
        private SearchSystem m_SearchSystem;
        private BeginSimulationEntityCommandBufferSystem m_EcbSystem;
        private float3 m_LastCameraPos;
        private float3 m_LastCameraDir;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_CameraSystem = World.GetExistingSystemManaged<CameraUpdateSystem>();
            m_SearchSystem = World.GetExistingSystemManaged<SearchSystem>();
            m_EcbSystem    = World.GetOrCreateSystemManaged<BeginSimulationEntityCommandBufferSystem>();
            m_LastCameraPos = float3.zero;
            m_LastCameraDir = float3.zero;
        }

        protected override void OnUpdate()
        {
            // Bail if the camera system isn't ready
            if (!m_CameraSystem.TryGetLODParameters(out var lodParams))
                return;

            float3 camPos = lodParams.cameraPosition;
            float3 camDir = m_CameraSystem.activeViewer.forward;

            // Movement/rotation thresholds
            float moveDist = math.distance(camPos, m_LastCameraPos);
            float rotAngle = math.degrees(math.acos(math.clamp(math.dot(camDir, m_LastCameraDir), -1f, 1f)));
            bool camMoved = moveDist > 2f || rotAngle > 1f;
            if (!camMoved)
                return;

            // Get the existing static search tree and wait for its build
            var tree = m_SearchSystem.GetStaticSearchTree(readOnly: true, out var treeDeps);
            Dependency = treeDeps;

            // Schedule the occlusion filtering job
            var occlusionJob = new OcclusionJob
            {
                tree = tree,
                cullingInfoLookup = GetComponentLookup<CullingInfo>(false),
                occludedTagLookup = GetComponentLookup<OccludedTag>(true),
                ecb = m_EcbSystem.CreateCommandBuffer().AsParallelWriter(),
                cameraPosition = camPos,
                cameraDirection = camDir
            };
            var jobHandle = occlusionJob.Schedule(Dependency);
            m_EcbSystem.AddJobHandleForProducer(jobHandle);
            Dependency = jobHandle;

            // Cache camera state until next significant move
            m_LastCameraPos = camPos;
            m_LastCameraDir = camDir;
        }

        // Collector to grab entities from quad tree
        private struct EntityCollector : INativeQuadTreeIterator<Entity, QuadTreeBoundsXZ>
        {
            public NativeList<Entity> list;
            public bool Intersect(QuadTreeBoundsXZ bounds) => true;
            public void Iterate(QuadTreeBoundsXZ bounds, Entity entity) => list.Add(entity);
        }
        // Burst job that filters occlusion and tags entities
        [BurstCompile]
        private struct OcclusionJob : IJob
        {
            [ReadOnly] public NativeQuadTree<Entity, QuadTreeBoundsXZ> tree;
            [NativeDisableContainerSafetyRestriction] public ComponentLookup<CullingInfo> cullingInfoLookup;
            [ReadOnly] public ComponentLookup<OccludedTag> occludedTagLookup;
            public EntityCommandBuffer.ParallelWriter ecb;
            public float3 cameraPosition;
            public float3 cameraDirection;

            public void Execute()
            {
                // Perform shadow-based filtering
                var filtered = OcclusionUtilities.PerformShadowBasedCulling(tree, cameraPosition, cameraDirection);

                // Collect survivors
                var survivors = new NativeParallelHashSet<Entity>(8, Allocator.Temp);
                var collector = new Collector { survivors = survivors };
                filtered.Iterate(ref collector, 0);
                filtered.Dispose();

                // Process original tree: tag occluded and update CullingInfo
                var processor = new Processor { survivors = survivors, cullingInfoLookup = cullingInfoLookup, ecb = ecb };
                tree.Iterate(ref processor, 0);
                survivors.Dispose();
            }

            private struct Collector : INativeQuadTreeIterator<Entity, QuadTreeBoundsXZ>
            {
                public NativeParallelHashSet<Entity> survivors;
                public bool Intersect(QuadTreeBoundsXZ bounds) => true;
                public void Iterate(QuadTreeBoundsXZ bounds, Entity entity) => survivors.Add(entity);
            }

            private struct Processor : INativeQuadTreeIterator<Entity, QuadTreeBoundsXZ>
            {
                [NativeDisableParallelForRestriction] public NativeParallelHashSet<Entity> survivors;
                [NativeDisableContainerSafetyRestriction] public ComponentLookup<CullingInfo> cullingInfoLookup;
                [ReadOnly] public ComponentLookup<OccludedTag> occludedTagLookup;
                public EntityCommandBuffer.ParallelWriter ecb;

                public bool Intersect(QuadTreeBoundsXZ bounds) => true;
                public void Iterate(QuadTreeBoundsXZ bounds, Entity entity)
                {
                    bool passed = survivors.Contains(entity);
                    ref var info = ref cullingInfoLookup.GetRefRW(entity).ValueRW;
                    info.m_PassedCulling = (byte)(passed ? 1 : 0);
                    bool hasTag = occludedTagLookup.HasComponent(entity);
                    if (!passed)
                    {
                        if (hasTag)
                        {
                            ecb.SetComponentEnabled<OccludedTag>(0, entity, true);
                        }
                        else
                        {
                            ecb.AddComponent<OccludedTag>(0, entity);
                        }
                    }
                    else if (hasTag)
                    {
                        ecb.SetComponentEnabled<OccludedTag>(0, entity, false);
                    }
                }
            }
        }
    }
}
