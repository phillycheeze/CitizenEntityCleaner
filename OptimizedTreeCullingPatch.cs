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
using System.Reflection.Emit;
using System.Linq;

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
                
                // Use transpiler approach to patch TreeCullingJob Execute methods
                var preCullingSystemType = typeof(PreCullingSystem);
                var nestedTypes = preCullingSystemType.GetNestedTypes(BindingFlags.NonPublic | BindingFlags.Public);
                
                Mod.log.Info($"DEBUG: Found {nestedTypes.Length} nested types in PreCullingSystem");
                foreach (var nestedType in nestedTypes)
                {
                    Mod.log.Info($"DEBUG: Found nested type: {nestedType.Name}");
                }
                
                // Find TreeCullingJob1 and TreeCullingJob2
                Type job1Type = null;
                Type job2Type = null;
                foreach (var nestedType in nestedTypes)
                {
                    if (nestedType.Name.Contains("TreeCullingJob1"))
                    {
                        job1Type = nestedType;
                        Mod.log.Info($"DEBUG: Found TreeCullingJob1 type: {job1Type.FullName}");
                    }
                    else if (nestedType.Name.Contains("TreeCullingJob2"))
                    {
                        job2Type = nestedType;
                        Mod.log.Info($"DEBUG: Found TreeCullingJob2 type: {job2Type.FullName}");
                    }
                }
                
                // Apply transpiler patches to both job types
                if (job1Type != null)
                {
                    var executeMethod = job1Type.GetMethod("Execute", new Type[] { typeof(int) });
                    if (executeMethod != null)
                    {
                        try
                        {
                            var transpiler = typeof(OptimizedTreeCullingPatch).GetMethod("TreeCullingJob1Transpiler", BindingFlags.Static | BindingFlags.NonPublic);
                            harmonyInstance.Patch(executeMethod, transpiler: new HarmonyMethod(transpiler));
                            Mod.log.Info($"SUCCESS: Applied transpiler patch to TreeCullingJob1.Execute");
                        }
                        catch (Exception ex)
                        {
                            Mod.log.Error($"Failed to apply transpiler to TreeCullingJob1: {ex.Message}");
                        }
                    }
                    else
                    {
                        Mod.log.Error("Could not find Execute method in TreeCullingJob1");
                    }
                }
                
                if (job2Type != null)
                {
                    var executeMethod = job2Type.GetMethod("Execute", new Type[] { typeof(int) });
                    if (executeMethod != null)
                    {
                        try
                        {
                            var transpiler = typeof(OptimizedTreeCullingPatch).GetMethod("TreeCullingJob2Transpiler", BindingFlags.Static | BindingFlags.NonPublic);
                            harmonyInstance.Patch(executeMethod, transpiler: new HarmonyMethod(transpiler));
                            Mod.log.Info($"SUCCESS: Applied transpiler patch to TreeCullingJob2.Execute");
                        }
                        catch (Exception ex)
                        {
                            Mod.log.Error($"Failed to apply transpiler to TreeCullingJob2: {ex.Message}");
                        }
                    }
                    else
                    {
                        Mod.log.Error("Could not find Execute method in TreeCullingJob2");
                    }
                }
                
                if (job1Type == null && job2Type == null)
                {
                    Mod.log.Error("Could not find TreeCullingJob1 or TreeCullingJob2 for transpiler patching");
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
        /// Signature must match: Boolean Intersect(Game.Common.QuadTreeBoundsXZ, Int32 ByRef)
        /// </summary>
        public static bool IntersectPrefix(QuadTreeBoundsXZ bounds, ref int subData)
        {
            try
            {
                intersectCallCount++;
                
                // Log first few calls immediately, then periodically
                if (intersectCallCount <= 3 || intersectCallCount % 50 == 0)
                {
                    Mod.log.Info($"DEBUG: IntersectPrefix called #{intersectCallCount} (subData: {subData})");
                }

                // For now, just test that the patch is working - we'll add occlusion logic later
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
        /// Postfix version of the patch - called after the original method
        /// </summary>
        public static void IntersectPostfix(QuadTreeBoundsXZ bounds, ref int subData, bool __result)
        {
            try
            {
                intersectCallCount++;
                
                // Log first few calls immediately, then periodically
                if (intersectCallCount <= 3 || intersectCallCount % 50 == 0)
                {
                    Mod.log.Info($"DEBUG: IntersectPostfix called #{intersectCallCount} (subData: {subData}, result: {__result})");
                }
            }
            catch (Exception ex)
            {
                Mod.log.Error($"ERROR in TreeCullingIterator.Intersect postfix: {ex.Message}");
            }
        }

        /// <summary>
        /// Transpiler for TreeCullingJob1.Execute() - intercepts iterator usage
        /// </summary>
        private static IEnumerable<CodeInstruction> TreeCullingJob1Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            
            try
            {
                // Look for the line: m_StaticObjectSearchTree.Iterate<TreeCullingIterator, int>(ref treeCullingIterator2, 3, ...)
                for (int i = 0; i < codes.Count - 2; i++)
                {
                    if (codes[i].opcode == OpCodes.Ldarg_0 && // Load 'this'
                        codes[i + 1].opcode == OpCodes.Ldfld && // Load m_StaticObjectSearchTree field
                        i + 3 < codes.Count &&
                        codes[i + 3].operand?.ToString().Contains("Iterate") == true)
                    {
                        // Insert our custom call before the Iterate call
                        var customCall = new CodeInstruction(OpCodes.Call, typeof(OptimizedTreeCullingPatch).GetMethod("InterceptIteratorCall"));
                        codes.Insert(i + 3, customCall);
                        
                        Mod.log.Info("DEBUG: Transpiler found and patched iterator call in TreeCullingJob1");
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Mod.log.Error($"Error in TreeCullingJob1 transpiler: {ex.Message}");
            }
            
            return codes;
        }

        /// <summary>
        /// Transpiler for TreeCullingJob2.Execute() - intercepts iterator usage
        /// </summary>
        private static IEnumerable<CodeInstruction> TreeCullingJob2Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            
            try
            {
                // Similar logic to Job1 but for Job2's single entity iteration
                for (int i = 0; i < codes.Count - 2; i++)
                {
                    if (codes[i].opcode == OpCodes.Ldarg_0 && // Load 'this'
                        codes[i + 1].opcode == OpCodes.Ldfld && // Load m_StaticObjectSearchTree field
                        i + 3 < codes.Count &&
                        codes[i + 3].operand?.ToString().Contains("Iterate") == true)
                    {
                        // Insert our custom call before the Iterate call
                        var customCall = new CodeInstruction(OpCodes.Call, typeof(OptimizedTreeCullingPatch).GetMethod("InterceptIteratorCall"));
                        codes.Insert(i + 3, customCall);
                        
                        Mod.log.Info("DEBUG: Transpiler found and patched iterator call in TreeCullingJob2");
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Mod.log.Error($"Error in TreeCullingJob2 transpiler: {ex.Message}");
            }
            
            return codes;
        }

        /// <summary>
        /// Custom method called by transpiler to intercept iterator usage
        /// </summary>
        public static void InterceptIteratorCall()
        {
            try
            {
                iterateCallCount++;
                
                // Log first few calls to confirm transpiler is working
                if (iterateCallCount <= 5 || iterateCallCount % 100 == 0)
                {
                    Mod.log.Info($"SUCCESS: Transpiler intercepted iterator call #{iterateCallCount}");
                }
            }
            catch (Exception ex)
            {
                Mod.log.Error($"Error in InterceptIteratorCall: {ex.Message}");
            }
        }

        /// <summary>
        /// Simple test prefix to confirm method is being called
        /// </summary>
        public static bool SimpleIteratePrefix()
        {
            try
            {
                iterateCallCount++;
                
                // Always log the first call immediately
                if (firstIterateCall)
                {
                    Mod.log.Info($"FIRST CALL: SimpleIteratePrefix called! This confirms the patch is working.");
                    firstIterateCall = false;
                }
                
                // Log periodically
                if (iterateCallCount <= 10 || iterateCallCount % 50 == 0)
                {
                    Mod.log.Info($"SIMPLE: IteratePrefix called #{iterateCallCount}");
                }
                
                return true;
            }
            catch (Exception ex)
            {
                Mod.log.Error($"ERROR in SimpleIteratePrefix: {ex.Message}");
                return true;
            }
        }

        /// <summary>
        /// MAIN occlusion patch - Iterate method is what actually gets called for tree culling
        /// Signature must match: Void Iterate(Game.Common.QuadTreeBoundsXZ, Int32, Unity.Entities.Entity)
        /// </summary>
        public static bool IteratePrefix(QuadTreeBoundsXZ bounds, int subData, Unity.Entities.Entity entity)
        {
            try
            {
                iterateCallCount++;
                
                // Log first few calls immediately, then periodically  
                if (iterateCallCount <= 3 || iterateCallCount % 100 == 0)
                {
                    Mod.log.Info($"DEBUG: IteratePrefix called #{iterateCallCount} (subData: {subData}, entity: {entity})");
                }
                
                // We need camera position for occlusion check, but we can't access __instance
                // For now, let's get it from the World/ECS system
                var world = Unity.Entities.World.DefaultGameObjectInjectionWorld;
                if (world != null)
                {
                    // Try to get camera position from rendering system
                    // This is a simplified approach - we'll improve it once we confirm it works
                    var cameraPosition = GetCameraPositionFromWorld();
                    var cameraDirection = GetCameraDirectionFromWorld();
                    
                    if (!cameraPosition.Equals(float3.zero))
                    {
                        // Calculate object position
                        float3 objectPosition = CalculateVisibleObjectPosition(bounds.m_Bounds, cameraPosition, cameraDirection);
                        
                        // Check if object is occluded by buildings
                        if (IsOccludedByBuildings(cameraPosition, cameraDirection, objectPosition, bounds.m_Bounds))
                        {
                            // Skip rendering this entity - it's occluded
                            if (iterateCallCount % 100 == 0)
                            {
                                Mod.log.Info($"SUCCESS: Skipped rendering entity {entity} - occluded by building");
                            }
                            return false; // Skip original method
                        }
                    }
                }
                
                return true; // Continue with original method
            }
            catch (Exception ex)
            {
                Mod.log.Error($"ERROR in TreeCullingIterator.Iterate patch: {ex.Message}");
                return true; // Fall back to original method
            }
        }

        // Static counters for method calls
        private static int methodCallCount = 0;
        private static int intersectCallCount = 0;
        private static int iterateCallCount = 0;
        private static bool firstIterateCall = true;

        /// <summary>
        /// Test prefix to see which methods are actually being called
        /// </summary>
        public static bool TestPrefix()
        {
            try
            {
                methodCallCount++;
                
                // Log first few calls immediately, then periodically
                if (methodCallCount <= 5 || methodCallCount % 100 == 0)
                {
                    var stackTrace = new System.Diagnostics.StackTrace();
                    var callingMethod = stackTrace.GetFrame(1)?.GetMethod();
                    if (callingMethod != null)
                    {
                        Mod.log.Info($"DEBUG: TreeCullingIterator method called #{methodCallCount}: {callingMethod.DeclaringType?.Name}.{callingMethod.Name}");
                    }
                }
                
                return true; // Continue with original method
            }
            catch (Exception ex)
            {
                Mod.log.Error($"Error in TestPrefix: {ex.Message}");
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
        /// Get camera position from game world - simplified version
        /// </summary>
        private static float3 GetCameraPositionFromWorld()
        {
            try
            {
                // Simple approach: use camera position if available
                var camera = UnityEngine.Camera.main;
                if (camera != null)
                {
                    var pos = camera.transform.position;
                    return new float3(pos.x, pos.y, pos.z);
                }
                return float3.zero;
            }
            catch
            {
                return float3.zero;
            }
        }

        /// <summary>
        /// Get camera direction from game world - simplified version
        /// </summary>
        private static float3 GetCameraDirectionFromWorld()
        {
            try
            {
                var camera = UnityEngine.Camera.main;
                if (camera != null)
                {
                    var dir = camera.transform.forward;
                    return new float3(dir.x, dir.y, dir.z);
                }
                return new float3(0, 0, 1); // Default forward
            }
            catch
            {
                return new float3(0, 0, 1);
            }
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
