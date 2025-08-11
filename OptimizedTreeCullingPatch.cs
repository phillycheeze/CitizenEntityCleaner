using HarmonyLib;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using Game.Rendering;
using Game.Common;
using Game.Buildings;
using System.Runtime.CompilerServices;
using System;
using System.Reflection;
using System.Collections.Generic;

namespace CitizenEntityCleaner
{
    /// <summary>
    /// Simplified building occlusion patch for tree culling
    /// Only handles occlusion culling without complex LOD optimizations
    /// </summary>
    public static class OptimizedTreeCullingPatch
    {
        private static Harmony harmonyInstance;
        private static Dictionary<int, BuildingOcclusionData> buildingCache = new Dictionary<int, BuildingOcclusionData>();
        private static float lastCacheUpdate = 0f;
        private static float3 lastCameraPos = float3.zero;

        private struct BuildingOcclusionData
        {
            public float3 position;
            public float3 size;
            public float2 boundsMin;
            public float2 boundsMax;
        }

        public static void ApplyPatches()
        {
            try
            {
                harmonyInstance = new Harmony("CitizenEntityCleaner.BuildingOcclusion");
                
                // Find and patch only the TreeCullingIterator.Intersect method
                var preCullingSystemType = typeof(PreCullingSystem);
                var nestedTypes = preCullingSystemType.GetNestedTypes(BindingFlags.NonPublic | BindingFlags.Public);
                
                Type iteratorType = null;
                foreach (var nestedType in nestedTypes)
                {
                    if (nestedType.Name.Contains("TreeCullingIterator"))
                    {
                        iteratorType = nestedType;
                        break;
                    }
                }
                
                if (iteratorType != null)
                {
                    var intersectMethod = iteratorType.GetMethod("Intersect");
                    if (intersectMethod != null)
                    {
                        var prefix = typeof(OptimizedTreeCullingPatch).GetMethod("IntersectPrefix");
                        harmonyInstance.Patch(intersectMethod, new HarmonyMethod(prefix));
                        Mod.log.Info("Applied building occlusion patch to TreeCullingIterator.Intersect");
                    }
                }
                else
                {
                    Mod.log.Error("Could not find TreeCullingIterator for occlusion patching");
                }
            }
            catch (Exception ex)
            {
                Mod.log.Error($"Failed to apply building occlusion patch: {ex.Message}");
            }
        }

        public static void RemovePatches()
        {
            try
            {
                harmonyInstance?.UnpatchAll("CitizenEntityCleaner.BuildingOcclusion");
                Mod.log.Info("Removed building occlusion patches");
            }
            catch (Exception ex)
            {
                Mod.log.Error($"Failed to remove building occlusion patches: {ex.Message}");
            }
        }

        /// <summary>
        /// Simple prefix patch that adds building occlusion check before original intersect logic
        /// </summary>
        public static bool IntersectPrefix(ref bool __result, ref object __instance, QuadTreeBoundsXZ bounds, ref int subData)
        {
            try
            {
                var type = __instance.GetType();
                var cameraPosition = GetFieldValue<float3>(__instance, type, "m_CameraPosition");
                
                // Calculate object center
                float3 objectCenter = new float3(
                    (bounds.m_Bounds.min.x + bounds.m_Bounds.max.x) * 0.5f,
                    (bounds.m_Bounds.min.y + bounds.m_Bounds.max.y) * 0.5f,
                    (bounds.m_Bounds.min.z + bounds.m_Bounds.max.z) * 0.5f
                );
                
                // Check if object is occluded by buildings
                if (IsOccludedByBuildings(cameraPosition, objectCenter))
                {
                    __result = false;
                    return false; // Skip original method - object is occluded
                }
                
                return true; // Continue with original method
            }
            catch (Exception ex)
            {
                Mod.log.Error($"Error in building occlusion check: {ex.Message}");
                return true; // Fall back to original method
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static T GetFieldValue<T>(object instance, Type type, string fieldName)
        {
            var field = type.GetField(fieldName);
            return (T)field.GetValue(instance);
        }

        /// <summary>
        /// Main occlusion check - simplified version
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsOccludedByBuildings(float3 cameraPosition, float3 objectCenter)
        {
            try
            {
                // Only check occlusion for distant objects
                float distance = math.distance(cameraPosition, objectCenter);
                if (distance < 80f) return false;

                // Update building cache periodically
                if (math.distance(cameraPosition, lastCameraPos) > 20f || 
                    UnityEngine.Time.time - lastCacheUpdate > 2f)
                {
                    UpdateBuildingCache(cameraPosition);
                    lastCameraPos = cameraPosition;
                    lastCacheUpdate = UnityEngine.Time.time;
                }

                // Simple occlusion test against cached buildings
                foreach (var building in buildingCache.Values)
                {
                    if (IsObjectOccluded(cameraPosition, objectCenter, building))
                    {
                        return true;
                    }
                }

                return false;
            }
            catch
            {
                return false; // Safe fallback
            }
        }

        /// <summary>
        /// Simplified building cache update using proper ECS patterns
        /// </summary>
        private static void UpdateBuildingCache(float3 cameraPosition)
        {
            try
            {
                buildingCache.Clear();

                // Get the default world - this is the correct way to access it
                var world = Unity.Entities.World.DefaultGameObjectInjectionWorld;
                if (world == null) return;

                var entityManager = world.EntityManager;
                
                // Create EntityQuery using EntityQueryDesc pattern like CitizenCleanupSystem
                var queryDesc = new Unity.Entities.EntityQueryDesc
                {
                    All = new Unity.Entities.ComponentType[]
                    {
                        Unity.Entities.ComponentType.ReadOnly<Game.Buildings.Building>(),
                        Unity.Entities.ComponentType.ReadOnly<Unity.Transforms.LocalToWorld>(),
                        Unity.Entities.ComponentType.ReadOnly<CullingInfo>()
                    },
                    None = new Unity.Entities.ComponentType[]
                    {
                        Unity.Entities.ComponentType.ReadOnly<Game.Common.Deleted>() // Exclude deleted buildings
                    }
                };

                using var query = entityManager.CreateEntityQuery(queryDesc);
                
                if (query.IsEmpty) return;

                // Use proper allocation patterns like CitizenCleanupSystem
                using var entities = query.ToEntityArray(Unity.Collections.Allocator.TempJob);
                using var transforms = query.ToComponentDataArray<Unity.Transforms.LocalToWorld>(Unity.Collections.Allocator.TempJob);
                using var cullingInfos = query.ToComponentDataArray<CullingInfo>(Unity.Collections.Allocator.TempJob);

                int count = 0;
                float maxRange = 200f;
                float maxRangeSq = maxRange * maxRange;

                for (int i = 0; i < entities.Length && count < 30; i++)
                {
                    var entity = entities[i];
                    var transform = transforms[i];
                    var cullingInfo = cullingInfos[i];
                    
                    float3 buildingPos = transform.Position;
                    
                    // Use squared distance for better performance
                    if (math.distancesq(cameraPosition, buildingPos) > maxRangeSq) continue;

                    var bounds = cullingInfo.m_Bounds;
                    float3 size = bounds.max - bounds.min;
                    
                    // Only consider large buildings that can provide meaningful occlusion
                    if (size.x < 15f || size.z < 15f || size.y < 8f) continue;

                    // Verify entity still exists (following CitizenCleanupSystem pattern)
                    if (!entityManager.Exists(entity) || entityManager.HasComponent<Game.Common.Deleted>(entity))
                        continue;

                    buildingCache[entity.Index] = new BuildingOcclusionData
                    {
                        position = buildingPos,
                        size = size,
                        boundsMin = new float2(bounds.min.x, bounds.min.z),
                        boundsMax = new float2(bounds.max.x, bounds.max.z)
                    };
                    count++;
                }
            }
            catch (Exception ex)
            {
                Mod.log.Error($"Error updating building cache: {ex.Message}");
            }
        }

        /// <summary>
        /// Simple 2D occlusion test
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsObjectOccluded(float3 cameraPos, float3 objectPos, BuildingOcclusionData building)
        {
            // Check if building is between camera and object
            float3 cameraToBuilding = building.position - cameraPos;
            float3 cameraToObject = objectPos - cameraPos;
            
            if (math.dot(cameraToBuilding, cameraToObject) < 0 || 
                math.length(cameraToBuilding) > math.length(cameraToObject))
                return false;

            // Simple 2D line intersection with building bounds
            float2 camPos2D = cameraPos.xz;
            float2 objPos2D = objectPos.xz;
            
            return LineIntersectsRect(camPos2D, objPos2D, building.boundsMin, building.boundsMax);
        }

        /// <summary>
        /// Basic line-rectangle intersection test
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool LineIntersectsRect(float2 lineStart, float2 lineEnd, float2 rectMin, float2 rectMax)
        {
            // Expand rect slightly for tolerance
            rectMin -= 3f;
            rectMax += 3f;

            // Check if line endpoints are inside rect
            if (IsPointInRect(lineStart, rectMin, rectMax) || IsPointInRect(lineEnd, rectMin, rectMax))
                return true;

            // Check line intersection with rect edges
            return LineIntersectsLine(lineStart, lineEnd, new float2(rectMin.x, rectMin.y), new float2(rectMax.x, rectMin.y)) ||
                   LineIntersectsLine(lineStart, lineEnd, new float2(rectMax.x, rectMin.y), new float2(rectMax.x, rectMax.y)) ||
                   LineIntersectsLine(lineStart, lineEnd, new float2(rectMax.x, rectMax.y), new float2(rectMin.x, rectMax.y)) ||
                   LineIntersectsLine(lineStart, lineEnd, new float2(rectMin.x, rectMax.y), new float2(rectMin.x, rectMin.y));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsPointInRect(float2 point, float2 rectMin, float2 rectMax)
        {
            return point.x >= rectMin.x && point.x <= rectMax.x && 
                   point.y >= rectMin.y && point.y <= rectMax.y;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool LineIntersectsLine(float2 p1, float2 p2, float2 p3, float2 p4)
        {
            float2 s1 = p2 - p1;
            float2 s2 = p4 - p3;

            float cross = s1.x * s2.y - s1.y * s2.x;
            if (math.abs(cross) < 1e-6f) return false;

            float t = ((p3.x - p1.x) * s2.y - (p3.y - p1.y) * s2.x) / cross;
            float u = ((p3.x - p1.x) * s1.y - (p3.y - p1.y) * s1.x) / cross;

            return t >= 0 && t <= 1 && u >= 0 && u <= 1;
        }
    }
}