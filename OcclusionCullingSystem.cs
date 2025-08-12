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
    [UpdateAfter(typeof(Game.Rendering.PreCullingSystem))]
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
                return;
            }

            float3 camPos = lodParams.cameraPosition;
            float3 camDir = m_CameraSystem.activeViewer.forward;

            // Movement/rotation thresholds
            float moveDist = math.distance(camPos, m_LastCameraPos);
            float dot = math.clamp(math.dot(math.normalize(new float3(camDir.x, 0f, camDir.z)), math.normalize(new float3(m_LastCameraDir.x, 0f, m_LastCameraDir.z))), -1f, 1f);
            float rotAngle = math.degrees(math.acos(dot));
            bool camMoved = moveDist > 2f || rotAngle > 1f || m_LastCameraDir.Equals(float3.zero);
            if (!camMoved)
            {
                return;
            }

            // Get the existing static search tree and wait for its build
            var searchSystem = World.GetExistingSystemManaged<SearchSystem>();
            var tree = searchSystem.GetStaticSearchTree(readOnly: true, out var treeDeps);

            // Chain dependency on the SearchSystem's build job
            Dependency = JobHandle.CombineDependencies(Dependency, treeDeps);

            // Get the CullingInfo lookup
            var lookup = GetComponentLookup<CullingInfo>(false);

            // Schedule the occlusion filtering job after the tree is built
            var occlusionJob = new OcclusionJob
            {
                tree = tree,
                cullingInfoLookup = lookup,
                cameraPosition = camPos,
                cameraDirection = camDir,
                maxDistance = 150f
            };
            var occlHandle = occlusionJob.Schedule(Dependency);

            // Register our read with the SearchSystem so future builds wait for us
            searchSystem.AddStaticSearchTreeReader(occlHandle);
            Dependency = occlHandle;

            // Cache camera state until next significant move
            m_LastCameraPos = camPos;
            m_LastCameraDir = camDir;
        }

        // Iterate the tree once in a single job to avoid data races with PreCulling
        [BurstCompile]
        private struct OcclusionJob : IJob
        {
            [ReadOnly] public NativeQuadTree<Entity, QuadTreeBoundsXZ> tree;
            [ReadOnly] public float3 cameraPosition;
            [ReadOnly] public float3 cameraDirection;
            [ReadOnly] public float maxDistance;
            [NativeDisableParallelForRestriction] public ComponentLookup<CullingInfo> cullingInfoLookup;

            public void Execute()
            {
                var iterator = new OcclusionIterator
                {
                    cullingInfoLookup = cullingInfoLookup,
                    cameraPosition = cameraPosition,
                    cameraDirection = cameraDirection,
                    maxDistance = maxDistance
                };

                tree.Iterate(ref iterator, 0);
            }

            private struct OcclusionIterator : INativeQuadTreeIterator<Entity, QuadTreeBoundsXZ>
            {
                [NativeDisableParallelForRestriction] public ComponentLookup<CullingInfo> cullingInfoLookup;
                [ReadOnly] public float3 cameraPosition;
                [ReadOnly] public float3 cameraDirection;
                [ReadOnly] public float maxDistance;

                public bool Intersect(QuadTreeBoundsXZ bounds)
                {
                    var center = (bounds.m_Bounds.min + bounds.m_Bounds.max) * 0.5f;
                    var distance = math.distance(center, cameraPosition);
                    return distance <= maxDistance;
                }

                public void Iterate(QuadTreeBoundsXZ bounds, Entity entity)
                {
                    if (!cullingInfoLookup.HasComponent(entity))
                        return;

                    var center = (bounds.m_Bounds.min + bounds.m_Bounds.max) * 0.5f;
                    var toEntity = center - cameraPosition;
                    var distance = math.length(toEntity);

                    bool shouldPass = distance < maxDistance;

                    ref var info = ref cullingInfoLookup.GetRefRW(entity).ValueRW;
                    info.m_PassedCulling = (byte)(shouldPass ? 1 : 0);
                }
            }
        }
    }
}
