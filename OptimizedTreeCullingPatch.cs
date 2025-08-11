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
            public Bounds3 bounds3D; // Full 3D bounds for building
            public float height;
            public float distanceToCamera; // Distance from camera for sorting/filtering
        }

        private struct ShadowVolume
        {
            public Bounds3 shadowBounds; // The 3D shadow volume cast by the building
            public float3 buildingPosition;
            public float shadowLength; // How far the shadow extends
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
                var cameraDirection = GetFieldValue<float3>(__instance, type, "m_CameraDirection");
                
                // For trees, use a position that's more representative of what the camera sees
                // If camera is tilted up, check higher parts of the tree
                float3 objectPosition = CalculateVisibleObjectPosition(bounds.m_Bounds, cameraPosition, cameraDirection);
                
                // Check if object is occluded by buildings with proper 3D logic
                if (IsOccludedByBuildings(cameraPosition, cameraDirection, objectPosition, bounds.m_Bounds))
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
        /// Calculate the position on the object that the camera is most likely looking at
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float3 CalculateVisibleObjectPosition(Bounds3 objectBounds, float3 cameraPosition, float3 cameraDirection)
        {
            float3 objectCenter = new float3(
                (objectBounds.min.x + objectBounds.max.x) * 0.5f,
                (objectBounds.min.y + objectBounds.max.y) * 0.5f,
                (objectBounds.min.z + objectBounds.max.z) * 0.5f
            );

            // If camera is looking down, use lower part of object
            // If camera is looking up, use higher part of object
            float heightOffset = cameraDirection.y * (objectBounds.max.y - objectBounds.min.y) * 0.3f;
            
            return new float3(
                objectCenter.x,
                math.clamp(objectCenter.y + heightOffset, objectBounds.min.y, objectBounds.max.y),
                objectCenter.z
            );
        }

        /// <summary>
        /// Main occlusion check using shadow volume approach
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsOccludedByBuildings(float3 cameraPosition, float3 cameraDirection, float3 objectPosition, Bounds3 objectBounds)
        {
            try
            {
                // Only check occlusion for distant objects
                float objectDistance = math.distance(cameraPosition, objectPosition);
                if (objectDistance < 60f) return false;

                // Update building cache periodically
                if (math.distance(cameraPosition, lastCameraPos) > 20f || 
                    UnityEngine.Time.time - lastCacheUpdate > 2f)
                {
                    UpdateBuildingCache(cameraPosition);
                    lastCameraPos = cameraPosition;
                    lastCacheUpdate = UnityEngine.Time.time;
                }

                // Step 1: Get nearby buildings (closer than the object we're checking)
                var nearbyBuildings = GetNearbyBuildingsForShadows(cameraPosition, objectDistance);
                
                // Step 2 & 3: Check if object is in any building's shadow volume
                foreach (var building in nearbyBuildings)
                {
                    var shadowVolume = CalculateShadowVolume(cameraPosition, building, objectDistance);
                    
                    if (IsObjectInShadowVolume(objectPosition, objectBounds, shadowVolume))
                    {
                        // Log occasionally to verify occlusion is working
                        if (UnityEngine.Time.frameCount % 120 == 0) // Every 2 seconds at 60fps
                        {
                            Mod.log.Info($"Occlusion: Tree in shadow of building at {building.distanceToCamera:F0}m (object at {objectDistance:F0}m)");
                        }
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

                    float distanceToCamera = math.distance(cameraPosition, buildingPos);
                    
                    buildingCache[entity.Index] = new BuildingOcclusionData
                    {
                        position = buildingPos,
                        size = size,
                        bounds3D = bounds,
                        height = size.y,
                        distanceToCamera = distanceToCamera
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
        /// Step 1: Get buildings that are close enough to camera and closer than the object to cast shadows
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static List<BuildingOcclusionData> GetNearbyBuildingsForShadows(float3 cameraPosition, float objectDistance)
        {
            var nearbyBuildings = new List<BuildingOcclusionData>();
            
            foreach (var building in buildingCache.Values)
            {
                // Building must be closer than the object to cast a shadow on it
                if (building.distanceToCamera >= objectDistance)
                    continue;
                    
                // Only consider buildings within reasonable shadow-casting range
                if (building.distanceToCamera > 150f)
                    continue;
                    
                nearbyBuildings.Add(building);
            }
            
            return nearbyBuildings;
        }

        /// <summary>
        /// Step 2: Calculate the 3D shadow volume cast by a building from camera position
        /// Think of camera as sun, building blocks light, creates shadow volume extending past building
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ShadowVolume CalculateShadowVolume(float3 cameraPosition, BuildingOcclusionData building, float maxShadowDistance)
        {
            // Calculate shadow projection direction (from camera through building)
            float3 shadowDirection = math.normalize(building.position - cameraPosition);
            
            // How far should the shadow extend? Use remaining distance to object
            float shadowLength = maxShadowDistance - building.distanceToCamera + 20f; // +20f for safety margin
            
            // Calculate shadow volume bounds by projecting building bounds away from camera
            Bounds3 buildingBounds = building.bounds3D;
            
            // Expand building bounds slightly for better occlusion
            float3 expansion = new float3(5f, 0f, 5f); // Expand XZ but not Y
            buildingBounds.min -= expansion;
            buildingBounds.max += expansion;
            
            // Project building corners to create shadow volume
            float3 shadowEndPos = building.position + shadowDirection * shadowLength;
            
            // Create shadow bounds that encompass both building and shadow projection
            Bounds3 shadowBounds = new Bounds3
            {
                min = math.min(buildingBounds.min, shadowEndPos - building.size * 0.5f),
                max = math.max(buildingBounds.max, shadowEndPos + building.size * 0.5f)
            };
            
            return new ShadowVolume
            {
                shadowBounds = shadowBounds,
                buildingPosition = building.position,
                shadowLength = shadowLength
            };
        }

        /// <summary>
        /// Step 3: Check if an object is inside the shadow volume
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsObjectInShadowVolume(float3 objectPosition, Bounds3 objectBounds, ShadowVolume shadowVolume)
        {
            // Simple bounds check - is object center or any corner inside shadow volume?
            Bounds3 shadow = shadowVolume.shadowBounds;
            
            // Check object center
            if (IsPointInBounds(objectPosition, shadow))
                return true;
                
            // Check object corners for more accurate intersection
            float3[] objectCorners = new float3[]
            {
                new float3(objectBounds.min.x, objectBounds.min.y, objectBounds.min.z),
                new float3(objectBounds.max.x, objectBounds.min.y, objectBounds.min.z),
                new float3(objectBounds.min.x, objectBounds.max.y, objectBounds.min.z),
                new float3(objectBounds.max.x, objectBounds.max.y, objectBounds.min.z),
                new float3(objectBounds.min.x, objectBounds.min.y, objectBounds.max.z),
                new float3(objectBounds.max.x, objectBounds.min.y, objectBounds.max.z),
                new float3(objectBounds.min.x, objectBounds.max.y, objectBounds.max.z),
                new float3(objectBounds.max.x, objectBounds.max.y, objectBounds.max.z)
            };
            
            // If any corner is in shadow, object is occluded
            foreach (var corner in objectCorners)
            {
                if (IsPointInBounds(corner, shadow))
                    return true;
            }
            
            return false;
        }

        /// <summary>
        /// Simple 3D point-in-bounds check
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsPointInBounds(float3 point, Bounds3 bounds)
        {
            return point.x >= bounds.min.x && point.x <= bounds.max.x &&
                   point.y >= bounds.min.y && point.y <= bounds.max.y &&
                   point.z >= bounds.min.z && point.z <= bounds.max.z;
        }


    }
}