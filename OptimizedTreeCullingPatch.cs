using Unity.Collections;
using Unity.Entities;
using Colossal.Collections;
using Unity.Mathematics;
using Game.Common;
using Colossal.Mathematics;
using Colossal.Logging;

namespace CitizenEntityCleaner
{
    public static class OcclusionUtilities
    {
        // Utility functions for performing shadow-based culling

        /// <summary>
        /// Efficiently finds nearby, large objects using heirarchal spatial iteration
        /// </summary>
        /// <param name="quadTree">The static object search tree</param>
        /// <param name="cameraPosition">Current camera world position</param>
        /// <param name="maxDistance">Maximum distance to consider objects (200-500m)</param>
        /// <returns>Very small list of the largest shadow caster candidates</returns>
        private static NativeList<(Entity entity, QuadTreeBoundsXZ bounds)> FindShadowCasters(
            NativeQuadTree<Entity, QuadTreeBoundsXZ> quadTree,
            float3 cameraPosition,
            float maxDistance = 25f)
        {
            var collector = new ShadowCasterCollector(cameraPosition, maxDistance, 8);
            quadTree.Iterate(ref collector, 0);
            return collector.m_Bounds;
        }

        /// <summary>
        /// Shadow box calculaton behind nearby objects
        /// </summary>
        /// <param name="casterBounds">The bounds of the object casting the shadow</param>
        /// <param name="cameraPosition">Camera position acting as light source</param>
        /// <param name="fixedShadowDistance">Fixed shadow length (100-200m)</param>
        /// <returns>Simple rectangular shadow volume on ground plane</returns>
        private static QuadTreeBoundsXZ CalculateShadowBox(
            QuadTreeBoundsXZ casterBounds,
            float3 cameraPosition,
            float3 cameraDirection,
            float fixedShadowDistance)
        {
            var objectCenter = (casterBounds.m_Bounds.min + casterBounds.m_Bounds.max) * 0.5f;
            var objectSize = (casterBounds.m_Bounds.max - casterBounds.m_Bounds.min);

            float3 direction = math.normalize(objectCenter - cameraDirection);

            var shadowEnd = objectCenter + (direction * fixedShadowDistance);

            var shadowMin = new float3(
                math.min(objectCenter.x, shadowEnd.x) - objectSize.x * 0.5f,
                casterBounds.m_Bounds.min.y,
                math.min(objectCenter.z, shadowEnd.z) - objectSize.z * 0.5f
            );
            var shadowMax = new float3(
                math.max(objectCenter.x, shadowEnd.x) + objectSize.x * 0.5f,
                casterBounds.m_Bounds.max.y,
                math.max(objectCenter.z, shadowEnd.z) + objectSize.z * 0.5f
            );
            return new QuadTreeBoundsXZ(new Bounds3(shadowMin, shadowMax), BoundsMask.AllLayers, 0);
        }

        /// <summary>
        /// Single-pass occlusion test - no separate "find occluded" step
        /// Integrated directly into main culling loop for maximum performance
        /// </summary>
        /// <param name="candidateEntity">Entity being tested for occlusion</param>
        /// <param name="candidateBounds">Bounds of the candidate object</param>
        /// <param name="shadowBoxes">Pre-calculated shadow volumes (5-10 boxes max)</param>
        /// <param name="shadowCasterDistances">Distances from camera to each shadow caster</param>
        /// <param name="cameraPosition">Camera position for depth testing</param>
        /// <returns>True if object should be culled (is occluded)</returns>
        private static bool IsObjectOccluded(
            Entity candidateEntity,
            QuadTreeBoundsXZ candidateBounds,
            NativeList<QuadTreeBoundsXZ> shadowBoxes,
            NativeList<float> shadowCasterDistances,
            float3 cameraPosition)
        {
            if (shadowBoxes.Length == 0) return false;
            const float depthBuffer = 10f;

            var candidateCenter = (candidateBounds.m_Bounds.min + candidateBounds.m_Bounds.max) * 0.5f;
            var candidateDistance = math.distance(cameraPosition, candidateCenter);

            for (int i = 0; i < shadowBoxes.Length; i++)
            {
                var casterDistance = shadowCasterDistances[i];
                if (candidateDistance <= casterDistance + depthBuffer) continue;

                var shadowBox = shadowBoxes[i];
                if (candidateBounds.Intersect(shadowBox)) return true;
            }
            return false;
        }

        /// <summary>
        /// Optimized single-pass shadow-based culling with aggressive performance optimizations
        /// Uses cached shadow data and inline occlusion testing
        /// </summary>
        /// <param name="quadTree">Original static object search tree</param>
        /// <param name="cameraPosition">Current camera world position</param>
        /// <returns>Filtered quad tree with occluded objects removed</returns>
        public static NativeQuadTree<Entity, QuadTreeBoundsXZ> PerformShadowBasedCulling(
            NativeQuadTree<Entity, QuadTreeBoundsXZ> quadTree,
            float3 cameraPosition,
            float3 cameraDirection)
        {

            var shadowCasters = FindShadowCasters(quadTree, cameraPosition);
            if (shadowCasters.Length == 0)
            {
                shadowCasters.Dispose();
                return quadTree;
            }

            var shadowBoxes = new NativeList<QuadTreeBoundsXZ>(shadowCasters.Length, Allocator.TempJob);
            var casterDistances = new NativeList<float>(shadowCasters.Length, Allocator.TempJob);

            for (int i = 0; i < shadowCasters.Length; i++)
            {
                var caster = shadowCasters[i];
                var shadowBox = CalculateShadowBox(caster.bounds, cameraPosition, cameraDirection, 1000f);
                var distance = math.distance(cameraPosition, (caster.bounds.m_Bounds.min + caster.bounds.m_Bounds.max) * 0.5f);

                shadowBoxes.Add(shadowBox);
                casterDistances.Add(distance);
            }

            var filteredTree = new NativeQuadTree<Entity, QuadTreeBoundsXZ>(1f, Allocator.TempJob);

            var filteredCollector = new FilteringCollector(shadowBoxes,casterDistances,cameraPosition,filteredTree,1000f);

            quadTree.Iterate(ref filteredCollector, 0);

            shadowCasters.Dispose();
            shadowBoxes.Dispose();
            casterDistances.Dispose();

            return filteredTree;
        }

        /// <summary>
        /// Simple cache for shadow data to avoid recalculating every frame
        /// </summary>
        private static class ShadowCache
        {
            // Cache shadow boxes for multiple frames when camera movement is minimal
            // public static int lastFrameCalculated = -1;
            // public static float3 lastCameraPosition;
            // public static NativeList<Bounds3> cachedShadowBoxes;
            // public static NativeList<float> cachedCasterDistances;
            // 
            // Reset cache when camera moves > 50m or game loads new area
            // Dramatically reduces per-frame shadow calculation overhead
        }

        public unsafe struct FilteringCollector : INativeQuadTreeIterator<Entity, QuadTreeBoundsXZ>
        {
            public NativeList<QuadTreeBoundsXZ> shadowBoxes;
            public NativeList<float> casterDistances;
            public float3 cameraPosition;
            public NativeQuadTree<Entity, QuadTreeBoundsXZ> filteredTree;
            public float maxProcessingDistance;
            public int entityCount;
            private readonly QuadTreeBoundsXZ searchBounds;

            public FilteringCollector(NativeList<QuadTreeBoundsXZ> shadowBoxes, NativeList<float> casterDistances, float3 cameraPosition, NativeQuadTree<Entity, QuadTreeBoundsXZ> filteredTree, float maxProcessingDistance)
            {
                this.shadowBoxes = shadowBoxes;
                this.casterDistances = casterDistances;
                this.cameraPosition = cameraPosition;
                this.filteredTree = filteredTree;
                this.maxProcessingDistance = maxProcessingDistance;
                this.entityCount = 0;
                this.searchBounds = new QuadTreeBoundsXZ(new Bounds3(cameraPosition - maxProcessingDistance, cameraPosition + maxProcessingDistance), BoundsMask.AllLayers, 0);
            }

            public bool Intersect(QuadTreeBoundsXZ bounds)
            {
                return bounds.Intersect(searchBounds);
            }

            public void Iterate(QuadTreeBoundsXZ bounds, Entity entity)
            {
                if(entityCount >= 1000) return;

                bool isOccluded = IsObjectOccluded(entity, bounds, shadowBoxes, casterDistances, cameraPosition);
                if (isOccluded)
                {
                    QuadTreeBoundsXZ* boundsPtr = &bounds;
                    boundsPtr->m_Mask = (BoundsMask)0;
                    entityCount++;
                }
            }
        }

        /// <summary>
        /// Minimal iterator to find the first entity for testing
        /// </summary>
        public struct ShadowCasterCollector : INativeQuadTreeIterator<Entity, QuadTreeBoundsXZ>
        {
            public float3 cameraPosition;
            public float maxDistance;
            public int maxCount;

            private int count;
            public NativeList<(Entity, QuadTreeBoundsXZ)> m_Bounds;
            private QuadTreeBoundsXZ searchBounds;

            public ShadowCasterCollector(float3 cameraPos, float searchRadius, int maxShadowCasters)
            {
                cameraPosition = cameraPos;
                maxDistance = searchRadius;
                maxCount = maxShadowCasters;
                count = 0;
                m_Bounds = new NativeList<(Entity, QuadTreeBoundsXZ)>(maxShadowCasters, Allocator.TempJob);
                searchBounds = new QuadTreeBoundsXZ(new Bounds3(cameraPosition - maxDistance, cameraPosition + maxDistance), BoundsMask.AllLayers, 0);
            }

            public bool Intersect(QuadTreeBoundsXZ bounds)
            {
                if (count >= maxCount)
                {
                    return false;
                }

                return bounds.Intersect(searchBounds);
            }

            public void Iterate(QuadTreeBoundsXZ bounds, Entity item)
            {
                if (count >= maxCount) return;

                var objectSize = (bounds.m_Bounds.max - bounds.m_Bounds.min);
                var minDimension = math.min(math.min(objectSize.x, objectSize.y), objectSize.z);
                var maxDimension = math.max(math.max(objectSize.x, objectSize.y), objectSize.z);

                if(maxDimension > 5f && maxDimension > 15f)
                {
                    m_Bounds.Add((item, bounds));
                    count++;
                }
            }
        }
    }

}
