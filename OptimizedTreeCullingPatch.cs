using HarmonyLib;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Game.Objects;
using Colossal.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace CitizenEntityCleaner
{
    [HarmonyPatch(typeof(SearchSystem), "GetStaticSearchTree")]
    public static class OptimizedTreeCullingPatch
    {
        [HarmonyPostfix]
        public static NativeQuadTree<Entity, QuadTreeBoundsXZ> GetStaticSearchTree_Postfix(NativeQuadTree<Entity, QuadTreeBoundsXZ> __result, ref JobHandle dependencies)
        {
            if (__result.IsCreated)
            {
                Debug.Log($"[OptimizedTreeCullingPatch] We are being called");
                
                // Simple test: get first element using minimal iterator
                var firstFinder = new FirstElementFinder();
                __result.Iterate(ref firstFinder, 0); // Start from node 0
                
                if (firstFinder.found)
                {
                    Debug.Log($"[OptimizedTreeCullingPatch] First entity: {firstFinder.entity}");
                }
                
                // Return original tree unchanged for now
            }
            
            return __result;
        }
        
        private static (Entity entity, QuadTreeBoundsXZ bounds) ProcessQuadTreeItem(Entity entity, QuadTreeBoundsXZ bounds)
        {
            // Parse the item - for now just log some info and return unchanged
            Debug.Log($"[OptimizedTreeCullingPatch] Processing entity: {entity}, bounds: {bounds.m_Bounds.min} to {bounds.m_Bounds.max}");
            
            // Future filtering logic will go here
            // For now, return the item unchanged
            return (entity, bounds);
        }

        /// <summary>
        /// Fast approximation to find 5-10 largest objects near camera for shadow casting
        /// Optimized for speed over accuracy
        /// </summary>
        /// <param name="quadTree">The static object search tree</param>
        /// <param name="cameraPosition">Current camera world position</param>
        /// <param name="maxDistance">Maximum distance to consider objects (200-500m)</param>
        /// <returns>Very small list of the largest shadow caster candidates</returns>
        private static NativeList<(Entity entity, QuadTreeBoundsXZ bounds)> FindShadowCasters(
            NativeQuadTree<Entity, QuadTreeBoundsXZ> quadTree,
            float3 cameraPosition,
            float maxDistance = 300f)
        {
            // FAST APPROACH - ACCURACY NOT CRITICAL:
            // 1. Create simple sphere search area around camera (radius = maxDistance)
            // 2. Quick size filter: skip any object with bounds volume < minShadowCasterSize (buildings only, not trees/props)
            // 3. Distance filter: only objects within 200-300m of camera
            // 4. Calculate rough object volume: (max - min).x * (max - min).y * (max - min).z
            // 5. Keep only top 5-10 largest objects by volume (use simple insertion sort)
            // 6. IMPORTANT: Cache results for 3-5 frames to avoid recalculating every frame
            // 7. Only recalculate when camera moves more than 50m or rotates more than 30 degrees
            return default;
        }

        /// <summary>
        /// Ultra-fast shadow box calculation using simple 2D ground projection
        /// Trades accuracy for massive performance gains
        /// </summary>
        /// <param name="casterBounds">The bounds of the object casting the shadow</param>
        /// <param name="cameraPosition">Camera position acting as light source</param>
        /// <param name="fixedShadowDistance">Fixed shadow length (100-200m)</param>
        /// <returns>Simple rectangular shadow volume on ground plane</returns>
        private static Bounds3 CalculateShadowBox(
            QuadTreeBoundsXZ casterBounds,
            float3 cameraPosition,
            float fixedShadowDistance = 150f)
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
            return default;
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
            NativeList<Bounds3> shadowBoxes,
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
            float3 cameraPosition)
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
            return default;
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
    }

    /// <summary>
    /// Minimal iterator to find the first entity for testing
    /// </summary>
    public struct FirstElementFinder : INativeQuadTreeIterator<Entity, QuadTreeBoundsXZ>
    {
        public bool found;
        public Entity entity;

        public bool Intersect(QuadTreeBoundsXZ bounds)
        {
            return !found; // Stop after finding first element
        }

        public void Iterate(QuadTreeBoundsXZ bounds, Entity item)
        {
            if (!found)
            {
                entity = item;
                found = true;
            }
        }
    }
}
