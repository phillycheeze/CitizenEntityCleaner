using HarmonyLib;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Game.Objects;
using Colossal.Collections;
using Unity.Mathematics;
using UnityEngine;
using Game.Common;
using Colossal.Mathematics;
using Colossal.Logging;
using Game.Rendering;
using Colossal.Entities;

namespace CitizenEntityCleaner
{
    [HarmonyPatch(typeof(SearchSystem), "GetStaticSearchTree")]
    public static class OptimizedTreeCullingPatch
    {
        private static ILog m_Log = Mod.log;
        private static CameraUpdateSystem m_CameraUpdateSystem;
        private static int frameCounter = 0;
        [HarmonyPostfix]
        public static NativeQuadTree<Entity, QuadTreeBoundsXZ> GetStaticSearchTree_Postfix(NativeQuadTree<Entity, QuadTreeBoundsXZ> __result, ref JobHandle dependencies)
        {
            if (__result.IsCreated)
            { 
                frameCounter++;
                
                if (frameCounter % 120 == 0)
                {
                    if (m_CameraUpdateSystem == null)
                    {
                        m_CameraUpdateSystem = World.DefaultGameObjectInjectionWorld?.GetExistingSystemManaged<CameraUpdateSystem>();
                    }

                    if (m_CameraUpdateSystem != null && m_CameraUpdateSystem.TryGetLODParameters(out var lodParameters))
                    {
                        var cameraPosition = lodParameters.cameraPosition;
                        var cameraDirection = m_CameraUpdateSystem.activeViewer.forward;

                        if (cameraPosition.Equals(float3.zero) || cameraDirection.Equals(float3.zero) )
                        {
                            m_Log.Info("Camera position or direction is float 0");
                            return __result;
                        }

                        m_Log.Info("Culling: running frame loop iteration");
                        try
                        {
                            var filteredTree = PerformShadowBasedCulling(__result, cameraPosition, cameraDirection);
                            return filteredTree;
                        }
                        catch (System.Exception ex)
                        {
                            m_Log.Info($"OptimizedTreeCulling Error: {ex.Message}");
                            return __result;
                        }
                    }
                    else
                    {
                        m_Log.Info("Camera system not ready");
                    }
                }
            }
            
            return __result;
        }

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
            float maxDistance = 50f)
        {
            // FAST APPROACH - ACCURACY NOT CRITICAL:
            // 1. Create ShadowCasterCollector struct (implements tree iterator)
            // 2. Create camera-centered search bounds (cameraPos + max distance)
            // 3. Use filtering in Intersect() method: return Intersects(cameraBounds)
            var collector = new ShadowCasterCollector(cameraPosition, 300f, 10);
            quadTree.Iterate(ref collector, 0);
            m_Log.Info($"Number of shadow casters found: {collector.m_Bounds.Length}");
            return collector.m_Bounds;
        }

        /// <summary>
        /// Ultra-fast shadow box calculation using simple 2D ground projection
        /// Trades accuracy for massive performance gains
        /// </summary>
        /// <param name="casterBounds">The bounds of the object casting the shadow</param>
        /// <param name="cameraPosition">Camera position acting as light source</param>
        /// <param name="fixedShadowDistance">Fixed shadow length (100-200m)</param>
        /// <returns>Simple rectangular shadow volume on ground plane</returns>
        private static QuadTreeBoundsXZ CalculateShadowBox(
            QuadTreeBoundsXZ casterBounds,
            float3 cameraPosition,
            float3 cameraDirection,
            float fixedShadowDistance = 20f)
        {

            // ULTRA-SIMPLE APPROACH - NO COMPLEX RAY CASTING:
            // 1. Get object center point: (bounds.min + bounds.max) / 2
            // 2. Calculate shadow direction: normalize(objectCenter - cameraPosition)
            // 3. Project shadow on XZ plane only (ignore Y-axis complexity)
            // 4. Shadow end point = objectCenter + (shadowDirection * fixedShadowDistance)
            // 5. Create rectangular shadow box from objectCenter to shadowEnd
            // 6. Expand box by object size on X/Z axes for rough shadow width
            // 7. Y-axis: shadow from ground (0) to object height only
            // 8. NO complex geometry - just basic vector math and box creation
            // 9. Result: Fast approximate ground shadow that's "good enough" for culling
            float3 direction = math.normalize(cameraDirection);

            var objectCenter = (casterBounds.m_Bounds.min + casterBounds.m_Bounds.max) * 0.5f;
            var objectSize = (casterBounds.m_Bounds.max - casterBounds.m_Bounds.min);

            var shadowEnd = objectCenter + (direction * fixedShadowDistance);

            m_Log.Info($"Shadow debug: Object at {objectCenter}, shadow direction {direction}, shadow end {shadowEnd}, distanceFromCamera {math.distance(cameraPosition, objectCenter)}");

            var shadowMin = new float3(
                math.min(objectCenter.x, shadowEnd.x) - objectSize.x * 0.5f,
                casterBounds.m_Bounds.min.y,
                math.min(objectCenter.z, shadowEnd.z) - objectSize.z * 0.5f
            );
            var shadowMax = new float3(
                math.max(objectCenter.x, shadowEnd.x) + objectSize.x * 0.5f,
                casterBounds.m_Bounds.max.y,
                math.max(objectCenter.z, shadowEnd.z) - objectSize.z * 0.5f
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
            // SUPER-FAST OCCLUSION TEST:
            // 1. Calculate candidate's distance from camera once
            // 2. For each shadow box (only 5-10 iterations max):
            //    a. Simple bounds.Intersects() check (very fast)
            //    b. If intersects: depth test (candidateDistance > shadowCasterDistance + buffer)
            //    c. If both true: CULL (return true immediately)
            // 3. If no shadows intersect: DON'T CULL (return false)
            // 4. No complex geometry, no expensive spatial queries
            // 5. Total cost per object: ~10 simple math operations
            const float depthBuffer = 10f;

            if (shadowBoxes.Length == 0) return false;

            var candidateCenter = (candidateBounds.m_Bounds.min + candidateBounds.m_Bounds.max) * 0.5f;
            var candidateDistance = math.distance(cameraPosition, candidateCenter);

            for (int i = 0; i < shadowBoxes.Length; i++)
            {
                var shadowBox = shadowBoxes[i];
                var casterDistance = shadowCasterDistances[i];

                m_Log.Info($"Culling: isobjectoccluded, checking if candidate distance ({candidateDistance}) is less than casterDistance + depth ({casterDistance + depthBuffer})");

                if (candidateDistance <= casterDistance + depthBuffer) continue;

                m_Log.Info($"Culling: isobjectoccluded, candidate distance: {candidateDistance} ; caster distance: {casterDistance}");

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
        private static NativeQuadTree<Entity, QuadTreeBoundsXZ> PerformShadowBasedCulling(
            NativeQuadTree<Entity, QuadTreeBoundsXZ> quadTree,
            float3 cameraPosition,
            float3 cameraDirection)
        {
            // PERFORMANCE-OPTIMIZED APPROACH:
            // 1. Check cache: if camera hasn't moved much, use cached shadow boxes
            // 2. If cache miss: FindShadowCasters() -> get 5-10 largest nearby buildings
            // 3. For each shadow caster: CalculateShadowBox() -> simple ground projection
            // 4. Cache shadow boxes + caster distances for next 3-5 frames
            // 5. Create new filtered QuadTree with same spatial parameters
            // 6. SINGLE ITERATION through original QuadTree:
            //    a. For each object: call IsObjectOccluded() inline
            //    b. If NOT occluded: add to filtered tree
            //    c. If occluded: skip (cull it)
            // 7. Dispose original tree, return filtered tree
            // 8. Expected result: 10-30% fewer objects for TreeCullingJobs to process
            // 9. Performance cost: ~5-15% overhead, but saves much more in culling jobs

            var shadowCasters = FindShadowCasters(quadTree, cameraPosition);
            if (shadowCasters.Length == 0)
            {
                shadowCasters.Dispose();
                return quadTree;
            }

            var shadowBoxes = new NativeList<QuadTreeBoundsXZ>(shadowCasters.Length, Allocator.Temp);
            var casterDistances = new NativeList<float>(shadowCasters.Length, Allocator.Temp);

            for (int i = 0; i < shadowCasters.Length; i++)
            {
                var caster = shadowCasters[i];
                var shadowBox = CalculateShadowBox(caster.bounds, cameraPosition, cameraDirection, 300f);
                var distance = math.distance(cameraPosition, (caster.bounds.m_Bounds.min + caster.bounds.m_Bounds.max) * 0.5f);

                m_Log.Info($"Calculated a shadow box: bounds({shadowBox.m_Bounds.min}, {shadowBox.m_Bounds.max}); casterDistance({distance}); cameraPosition({cameraPosition}); cameraDirection({cameraDirection}); casterCenterPoint({(caster.bounds.m_Bounds.min + caster.bounds.m_Bounds.max) * 0.5f})");

                shadowBoxes.Add(shadowBox);
                casterDistances.Add(distance);
            }

            var filteredTree = new NativeQuadTree<Entity, QuadTreeBoundsXZ>(1f, Allocator.TempJob);

            var filteredCollector = new FilteringCollector
            {
                shadowBoxes = shadowBoxes,
                casterDistances = casterDistances,
                cameraPosition = cameraPosition,
                filteredTree = filteredTree,
                maxProcessingDistance = 1000f
            };

            quadTree.Iterate(ref  filteredCollector, 0);

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

        public struct FilteringCollector : INativeQuadTreeIterator<Entity, QuadTreeBoundsXZ>
        {
            public NativeList<QuadTreeBoundsXZ> shadowBoxes;
            public NativeList<float> casterDistances;
            public float3 cameraPosition;
            public NativeQuadTree<Entity, QuadTreeBoundsXZ> filteredTree;
            public float maxProcessingDistance;

            public bool Intersect(QuadTreeBoundsXZ bounds)
            {
                var boundsCenter = (bounds.m_Bounds.min + bounds.m_Bounds.max) * 0.5f;
                var distance = math.distance(cameraPosition, boundsCenter);

                if(distance <= 50f)
                {
                    m_Log.Info($"Filter culling debug: entity found close at {boundsCenter}, distance {distance}.");
                }

                return distance <= maxProcessingDistance;
            }

            public void Iterate(QuadTreeBoundsXZ bounds, Entity entity)
            {
                bool isOccluded = IsObjectOccluded(entity, bounds, shadowBoxes, casterDistances, cameraPosition);
                if (!isOccluded)
                {
                    filteredTree.Add(entity, bounds);
                }
                else
                {
                    m_Log.Info($"Culling: removing an entity from the tree because it is in a shadow: {entity}");
                }
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

        public ShadowCasterCollector(float3 cameraPos, float searchRadius, int maxShadowCasters)
        {
            cameraPosition = cameraPos;
            maxDistance = searchRadius;
            maxCount = maxShadowCasters;
            count = 0;
            m_Bounds = new NativeList<(Entity, QuadTreeBoundsXZ)>(maxShadowCasters, Allocator.Temp);
        }

        public bool Intersect(QuadTreeBoundsXZ bounds)
        {
            if (count >= maxCount)
            {
                return false;
            }

            // Leverage QuadTreeBounds more efficient Intersect methods
            var searchBounds = new QuadTreeBoundsXZ(
                new Bounds3((cameraPosition - maxDistance), (cameraPosition + maxDistance)),
                BoundsMask.AllLayers,
                0
            );

            return bounds.Intersect(searchBounds);
        }

        public void Iterate(QuadTreeBoundsXZ bounds, Entity item)
        {
            if (count >= maxCount) return;

            m_Bounds.Add((item, bounds));
            count++;
        }
    }
}
