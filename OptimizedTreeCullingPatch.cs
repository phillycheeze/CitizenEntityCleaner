using HarmonyLib;
using Unity.Mathematics;
using Colossal.Mathematics;
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
                
                Mod.log.Info($"DEBUG: Found {nestedTypes.Length} nested types in PreCullingSystem");
                foreach (var nestedType in nestedTypes)
                {
                    Mod.log.Info($"DEBUG: Found nested type: {nestedType.Name}");
                }
                
                Type iteratorType = null;
                foreach (var nestedType in nestedTypes)
                {
                    if (nestedType.Name.Contains("TreeCullingIterator"))
                    {
                        iteratorType = nestedType;
                        Mod.log.Info($"DEBUG: Found TreeCullingIterator type: {iteratorType.FullName}");
                        break;
                    }
                }
                
                if (iteratorType != null)
                {
                    var methods = iteratorType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    Mod.log.Info($"DEBUG: TreeCullingIterator has {methods.Length} methods:");
                    foreach (var method in methods)
                    {
                        Mod.log.Info($"DEBUG: Method: {method.Name}");
                    }
                    
                    var intersectMethod = iteratorType.GetMethod("Intersect");
                    if (intersectMethod != null)
                    {
                        Mod.log.Info($"DEBUG: Found Intersect method with signature: {intersectMethod}");
                        var parameters = intersectMethod.GetParameters();
                        Mod.log.Info($"DEBUG: Intersect method has {parameters.Length} parameters:");
                        foreach (var param in parameters)
                        {
                            Mod.log.Info($"DEBUG: Parameter: {param.ParameterType.Name} {param.Name}");
                        }
                        
                        var prefix = typeof(OptimizedTreeCullingPatch).GetMethod("IntersectPrefix");
                        if (prefix != null)
                        {
                            var patchResult = harmonyInstance.Patch(intersectMethod, new HarmonyMethod(prefix));
                            Mod.log.Info($"SUCCESS: Applied building occlusion patch to {iteratorType.Name}.Intersect");
                            Mod.log.Info($"DEBUG: Patch result: {patchResult}");
                            
                            // Verify the patch was applied
                            var patches = Harmony.GetPatchInfo(intersectMethod);
                            if (patches != null)
                            {
                                Mod.log.Info($"DEBUG: Method has {patches.Prefixes.Count} prefixes, {patches.Postfixes.Count} postfixes");
                            }
                        }
                        else
                        {
                            Mod.log.Error("Could not find IntersectPrefix method for patching");
                        }
                    }
                    else
                    {
                        Mod.log.Error("Could not find Intersect method in TreeCullingIterator");
                    }
                    
                    // Also try patching the Iterate method as a backup
                    var iterateMethod = iteratorType.GetMethod("Iterate");
                    if (iterateMethod != null)
                    {
                        Mod.log.Info($"DEBUG: Found Iterate method with signature: {iterateMethod}");
                        var parameters = iterateMethod.GetParameters();
                        Mod.log.Info($"DEBUG: Iterate method has {parameters.Length} parameters:");
                        foreach (var param in parameters)
                        {
                            Mod.log.Info($"DEBUG: Parameter: {param.ParameterType.Name} {param.Name}");
                        }
                        
                        var iteratePrefix = typeof(OptimizedTreeCullingPatch).GetMethod("IteratePrefix");
                        if (iteratePrefix != null)
                        {
                            var patchResult = harmonyInstance.Patch(iterateMethod, new HarmonyMethod(iteratePrefix));
                            Mod.log.Info($"SUCCESS: Also applied patch to {iteratorType.Name}.Iterate");
                        }
                    }
                    
                    // Try patching ALL public methods to see which ones are called
                    Mod.log.Info("DEBUG: Attempting to patch all public methods for testing...");
                    foreach (var method in methods)
                    {
                        if (method.IsPublic && !method.IsSpecialName && method.DeclaringType == iteratorType)
                        {
                            try
                            {
                                var testPrefix = typeof(OptimizedTreeCullingPatch).GetMethod("TestPrefix");
                                harmonyInstance.Patch(method, new HarmonyMethod(testPrefix));
                                Mod.log.Info($"DEBUG: Applied test patch to {method.Name}");
                            }
                            catch (Exception ex)
                            {
                                Mod.log.Info($"DEBUG: Could not patch {method.Name}: {ex.Message}");
                            }
                        }
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
                Mod.log.Error($"Stack trace: {ex.StackTrace}");
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
                // Debug: Log that patch is being called
                if (UnityEngine.Time.frameCount % 600 == 0) // Every 10 seconds at 60fps
                {
                    Mod.log.Info($"DEBUG: TreeCullingIterator.Intersect patch called (subData: {subData})");
                }

                var type = __instance.GetType();
                var cameraPosition = GetFieldValue<float3>(__instance, type, "m_CameraPosition");
                var cameraDirection = GetFieldValue<float3>(__instance, type, "m_CameraDirection");
                
                if (UnityEngine.Time.frameCount % 600 == 0)
                {
                    Mod.log.Info($"DEBUG: Camera at {cameraPosition}, direction {cameraDirection}");
                }
                
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
                Mod.log.Error($"ERROR in TreeCullingIterator.Intersect patch: {ex.Message}");
                Mod.log.Error($"Stack trace: {ex.StackTrace}");
                return true; // Fall back to original method
            }
        }

        /// <summary>
        /// Alternative patch target for testing - Iterate method
        /// </summary>
        public static bool IteratePrefix(ref object __instance)
        {
            try
            {
                // Debug: Log that Iterate patch is being called
                if (UnityEngine.Time.frameCount % 300 == 0) // Every 5 seconds at 60fps
                {
                    Mod.log.Info($"DEBUG: TreeCullingIterator.Iterate patch called!");
                }
                
                return true; // Continue with original method
            }
            catch (Exception ex)
            {
                Mod.log.Error($"ERROR in TreeCullingIterator.Iterate patch: {ex.Message}");
                return true; // Fall back to original method
            }
        }

        /// <summary>
        /// Test prefix to see which methods are actually being called
        /// </summary>
        public static bool TestPrefix()
        {
            try
            {
                // Log which method is being called
                var stackTrace = new System.Diagnostics.StackTrace();
                var callingMethod = stackTrace.GetFrame(1)?.GetMethod();
                if (callingMethod != null && UnityEngine.Time.frameCount % 300 == 0)
                {
                    Mod.log.Info($"DEBUG: Method called: {callingMethod.DeclaringType?.Name}.{callingMethod.Name}");
                }
                
                return true; // Continue with original method
            }
            catch
            {
                return true;
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
                // Debug: Log when occlusion check is called
                if (UnityEngine.Time.frameCount % 300 == 0) // Every 5 seconds at 60fps
                {
                    Mod.log.Info($"DEBUG: Occlusion check called for object at distance {math.distance(cameraPosition, objectPosition):F0}m");
                }

                // Only check occlusion for distant objects
                float objectDistance = math.distance(cameraPosition, objectPosition);
                if (objectDistance < 60f) 
                {
                    if (UnityEngine.Time.frameCount % 300 == 0)
                    {
                        Mod.log.Info($"DEBUG: Object too close ({objectDistance:F0}m < 60m), skipping occlusion");
                    }
                    return false;
                }

                // Update building cache periodically
                if (math.distance(cameraPosition, lastCameraPos) > 20f || 
                    UnityEngine.Time.time - lastCacheUpdate > 2f)
                {
                    UpdateBuildingCache(cameraPosition);
                    lastCameraPos = cameraPosition;
                    lastCacheUpdate = UnityEngine.Time.time;
                    
                    Mod.log.Info($"DEBUG: Updated building cache, found {buildingCache.Count} buildings");
                }

                // Step 1: Get nearby buildings (closer than the object we're checking)
                var nearbyBuildings = GetNearbyBuildingsForShadows(cameraPosition, objectDistance);
                
                if (UnityEngine.Time.frameCount % 300 == 0)
                {
                    Mod.log.Info($"DEBUG: Found {nearbyBuildings.Count} nearby buildings for shadow casting (total cached: {buildingCache.Count})");
                }
                
                // Step 2 & 3: Check if object is in any building's shadow volume
                foreach (var building in nearbyBuildings)
                {
                    var shadowVolume = CalculateShadowVolume(cameraPosition, building, objectDistance);
                    
                    if (IsObjectInShadowVolume(objectPosition, objectBounds, shadowVolume))
                    {
                        // Log immediately when occlusion is found
                        Mod.log.Info($"SUCCESS: Tree occluded by building at {building.distanceToCamera:F0}m (object at {objectDistance:F0}m)");
                        return true;
                    }
                }

                if (UnityEngine.Time.frameCount % 300 == 0 && nearbyBuildings.Count > 0)
                {
                    Mod.log.Info($"DEBUG: No occlusion found after checking {nearbyBuildings.Count} buildings");
                }

                return false;
            }
            catch (System.Exception ex)
            {
                Mod.log.Error($"ERROR in occlusion check: {ex.Message}");
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
                        Unity.Entities.ComponentType.ReadOnly<Game.Objects.Transform>(),
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
                using var transforms = query.ToComponentDataArray<Game.Objects.Transform>(Unity.Collections.Allocator.TempJob);
                using var cullingInfos = query.ToComponentDataArray<CullingInfo>(Unity.Collections.Allocator.TempJob);

                int count = 0;
                float maxRange = 200f;
                float maxRangeSq = maxRange * maxRange;

                for (int i = 0; i < entities.Length && count < 30; i++)
                {
                    var entity = entities[i];
                    var transform = transforms[i];
                    var cullingInfo = cullingInfos[i];
                    
                    float3 buildingPos = transform.m_Position;
                    
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
}