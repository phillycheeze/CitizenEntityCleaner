using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Game.Rendering;
using Unity.Jobs;
using Colossal.Collections;

namespace CitizenEntityCleaner
{
    // Runs before SearchSystem to tag and cull occluded entities
    [UpdateBefore(typeof(SearchSystem))]
    public class OcclusionCullingSystem : SystemBase
    {
        private CameraUpdateSystem m_CameraSystem;
        // Removed cached SearchSystem; will fetch on-demand
        // No longer using command buffers for tagging
        private float3 m_LastCameraPos;
        private float3 m_LastCameraDir;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_CameraSystem = World.GetExistingSystemManaged<CameraUpdateSystem>();
            // No longer caching SearchSystem
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
            var searchSystem = World.GetExistingSystemManaged<SearchSystem>();
            var tree = searchSystem.GetStaticSearchTree(readOnly: true, out var treeDeps);
            Dependency = treeDeps;

            // Schedule the occlusion filtering job
            var occlusionJob = new OcclusionJob
            {
                tree = tree,
                cullingInfoLookup = GetComponentLookup<CullingInfo>(false),
                cameraPosition = camPos,
                cameraDirection = camDir
            };
            Dependency = occlusionJob.Schedule(Dependency);

            // Cache camera state until next significant move
            m_LastCameraPos = camPos;
            m_LastCameraDir = camDir;
        }

        // EntityCollector removed: no longer used
        // Burst job that filters occlusion and tags entities
        [BurstCompile]
        private struct OcclusionJob : IJob
        {
            [NativeDisableContainerSafetyRestriction] public ComponentLookup<CullingInfo> cullingInfoLookup;
            public NativeQuadTree<Entity, QuadTreeBoundsXZ> tree;
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
                var processor = new Processor { survivors = survivors, cullingInfoLookup = cullingInfoLookup };
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

                public bool Intersect(QuadTreeBoundsXZ bounds) => true;
                public void Iterate(QuadTreeBoundsXZ bounds, Entity entity)
                {
                    bool passed = survivors.Contains(entity);
                    ref var info = ref cullingInfoLookup.GetRefRW(entity).ValueRW;
                    // Simply update the passed culling flag
                    info.m_PassedCulling = (byte)(passed ? 1 : 0);
                }
            }
        }
    }
}
