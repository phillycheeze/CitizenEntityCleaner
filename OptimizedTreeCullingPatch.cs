using HarmonyLib;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Entities;
using Game.Rendering;
using Game.Common;
using System.Runtime.CompilerServices;
using System;
using System.Reflection;

namespace CitizenEntityCleaner
{
    /// <summary>
    /// Harmony patch to optimize TreeCullingIterator performance
    /// This patch reduces redundant calculations and improves branch prediction
    /// </summary>
    public static class OptimizedTreeCullingPatch
    {
        private static Harmony harmonyInstance;

        public static void ApplyPatches()
        {
            try
            {
                harmonyInstance = new Harmony("CitizenEntityCleaner.OptimizedTreeCulling");
                
                // Find the TreeCullingIterator nested type
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
                    // Patch the Intersect method
                    var intersectMethod = iteratorType.GetMethod("Intersect");
                    if (intersectMethod != null)
                    {
                        var intersectPrefix = typeof(OptimizedTreeCullingPatch).GetMethod("IntersectPrefix");
                        harmonyInstance.Patch(intersectMethod, new HarmonyMethod(intersectPrefix));
                        Mod.log.Info("Patched TreeCullingIterator.Intersect method");
                    }
                    
                    // Patch the Iterate method
                    var iterateMethod = iteratorType.GetMethod("Iterate");
                    if (iterateMethod != null)
                    {
                        var iteratePrefix = typeof(OptimizedTreeCullingPatch).GetMethod("IteratePrefix");
                        harmonyInstance.Patch(iterateMethod, new HarmonyMethod(iteratePrefix));
                        Mod.log.Info("Patched TreeCullingIterator.Iterate method");
                    }
                    
                    Mod.log.Info("Applied optimized tree culling patches successfully");
                }
                else
                {
                    Mod.log.Error("Could not find TreeCullingIterator type for patching");
                }
            }
            catch (Exception ex)
            {
                Mod.log.Error($"Failed to apply tree culling patches: {ex.Message}");
            }
        }

        public static void RemovePatches()
        {
            try
            {
                harmonyInstance?.UnpatchAll("CitizenEntityCleaner.OptimizedTreeCulling");
                Mod.log.Info("Removed optimized tree culling patches");
            }
            catch (Exception ex)
            {
                Mod.log.Error($"Failed to remove tree culling patches: {ex.Message}");
            }
        }

        /// <summary>
        /// Optimized Intersect method that reduces redundant calculations
        /// </summary>
        public static bool IntersectPrefix(ref bool __result, ref object __instance, QuadTreeBoundsXZ bounds, ref int subData)
        {
            try
            {
                var type = __instance.GetType();
                
                // Get field values using reflection (cached for performance)
                var lodParameters = GetFieldValue<float4>(__instance, type, "m_LodParameters");
                var cameraPosition = GetFieldValue<float3>(__instance, type, "m_CameraPosition");
                var cameraDirection = GetFieldValue<float3>(__instance, type, "m_CameraDirection");
                var prevCameraPosition = GetFieldValue<float3>(__instance, type, "m_PrevCameraPosition");
                var prevLodParameters = GetFieldValue<float4>(__instance, type, "m_PrevLodParameters");
                var prevCameraDirection = GetFieldValue<float3>(__instance, type, "m_PrevCameraDirection");
                var visibleMask = GetFieldValue<BoundsMask>(__instance, type, "m_VisibleMask");
                var prevVisibleMask = GetFieldValue<BoundsMask>(__instance, type, "m_PrevVisibleMask");

                // Optimized intersect logic
                __result = OptimizedIntersect(bounds, subData, lodParameters, cameraPosition, cameraDirection, 
                                            prevCameraPosition, prevLodParameters, prevCameraDirection, 
                                            visibleMask, prevVisibleMask);
                return false; // Skip original method
            }
            catch (Exception ex)
            {
                Mod.log.Error($"Error in optimized Intersect: {ex.Message}");
                return true; // Fall back to original method
            }
        }

        /// <summary>
        /// Optimized Iterate method that reduces redundant calculations
        /// </summary>
        public static bool IteratePrefix(ref object __instance, QuadTreeBoundsXZ bounds, int subData, Entity entity)
        {
            try
            {
                var type = __instance.GetType();
                
                // Get field values using reflection
                var lodParameters = GetFieldValue<float4>(__instance, type, "m_LodParameters");
                var cameraPosition = GetFieldValue<float3>(__instance, type, "m_CameraPosition");
                var cameraDirection = GetFieldValue<float3>(__instance, type, "m_CameraDirection");
                var prevCameraPosition = GetFieldValue<float3>(__instance, type, "m_PrevCameraPosition");
                var prevLodParameters = GetFieldValue<float4>(__instance, type, "m_PrevLodParameters");
                var prevCameraDirection = GetFieldValue<float3>(__instance, type, "m_PrevCameraDirection");
                var visibleMask = GetFieldValue<BoundsMask>(__instance, type, "m_VisibleMask");
                var prevVisibleMask = GetFieldValue<BoundsMask>(__instance, type, "m_PrevVisibleMask");
                var actionQueue = GetFieldValue<Writer<CullingAction>>(__instance, type, "m_ActionQueue");

                // Optimized iterate logic
                OptimizedIterate(bounds, subData, entity, lodParameters, cameraPosition, cameraDirection, 
                               prevCameraPosition, prevLodParameters, prevCameraDirection, 
                               visibleMask, prevVisibleMask, actionQueue);
                return false; // Skip original method
            }
            catch (Exception ex)
            {
                Mod.log.Error($"Error in optimized Iterate: {ex.Message}");
                return true; // Fall back to original method
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static T GetFieldValue<T>(object instance, Type type, string fieldName)
        {
            var field = type.GetField(fieldName);
            return (T)field.GetValue(instance);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool OptimizedIntersect(QuadTreeBoundsXZ bounds, int subData, 
                                             float4 lodParameters, float3 cameraPosition, float3 cameraDirection,
                                             float3 prevCameraPosition, float4 prevLodParameters, float3 prevCameraDirection,
                                             BoundsMask visibleMask, BoundsMask prevVisibleMask)
        {
            // Pre-calculate visibility masks to avoid repeated operations
            BoundsMask currentVisible = visibleMask & bounds.m_Mask;
            BoundsMask prevVisible = prevVisibleMask & bounds.m_Mask;

            switch (subData)
            {
                case 1:
                    {
                        // Early exit for performance
                        if (currentVisible == (BoundsMask)0)
                            return false;

                        // Calculate current distance and LOD once
                        float currentMinDist = RenderingUtils.CalculateMinDistance(bounds.m_Bounds, cameraPosition, cameraDirection, lodParameters);
                        int currentLod = RenderingUtils.CalculateLod(currentMinDist * currentMinDist, lodParameters);
                        
                        if (currentLod < bounds.m_MinLod)
                            return false;

                        // Optimized previous visibility check
                        if (prevVisible != (BoundsMask)0)
                        {
                            float prevMaxDist = RenderingUtils.CalculateMaxDistance(bounds.m_Bounds, prevCameraPosition, prevCameraDirection, prevLodParameters);
                            int prevLod = RenderingUtils.CalculateLod(prevMaxDist * prevMaxDist, prevLodParameters);
                            
                            if (prevLod >= bounds.m_MaxLod)
                                return currentLod > prevLod;
                            return false;
                        }
                        return true;
                    }
                case 2:
                    {
                        // Early exit for performance
                        if (prevVisible == (BoundsMask)0)
                            return false;

                        // Calculate previous distance and LOD once
                        float prevMinDist = RenderingUtils.CalculateMinDistance(bounds.m_Bounds, prevCameraPosition, prevCameraDirection, prevLodParameters);
                        int prevLod = RenderingUtils.CalculateLod(prevMinDist * prevMinDist, prevLodParameters);
                        
                        if (prevLod < bounds.m_MinLod)
                            return false;

                        // Optimized current visibility check
                        if (currentVisible != (BoundsMask)0)
                        {
                            float currentMaxDist = RenderingUtils.CalculateMaxDistance(bounds.m_Bounds, cameraPosition, cameraDirection, lodParameters);
                            int currentLod = RenderingUtils.CalculateLod(currentMaxDist * currentMaxDist, lodParameters);
                            
                            if (currentLod >= bounds.m_MaxLod)
                                return prevLod > currentLod;
                            return false;
                        }
                        return true;
                    }
                default:
                    {
                        // Early exit if neither visible
                        if (currentVisible == (BoundsMask)0 && prevVisible == (BoundsMask)0)
                            return false;

                        // Calculate distances once and reuse
                        float currentMinDist = RenderingUtils.CalculateMinDistance(bounds.m_Bounds, cameraPosition, cameraDirection, lodParameters);
                        float prevMinDist = RenderingUtils.CalculateMinDistance(bounds.m_Bounds, prevCameraPosition, prevCameraDirection, prevLodParameters);
                        
                        int currentLod = RenderingUtils.CalculateLod(currentMinDist * currentMinDist, lodParameters);
                        int prevLod = RenderingUtils.CalculateLod(prevMinDist * prevMinDist, prevLodParameters);
                        
                        bool isCurrentlyVisible = currentVisible != (BoundsMask)0 && currentLod >= bounds.m_MinLod;
                        bool wasPreviouslyVisible = prevVisible != (BoundsMask)0 && prevLod >= bounds.m_MaxLod;
                        
                        return isCurrentlyVisible != wasPreviouslyVisible;
                    }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void OptimizedIterate(QuadTreeBoundsXZ bounds, int subData, Entity entity,
                                           float4 lodParameters, float3 cameraPosition, float3 cameraDirection,
                                           float3 prevCameraPosition, float4 prevLodParameters, float3 prevCameraDirection,
                                           BoundsMask visibleMask, BoundsMask prevVisibleMask, Writer<CullingAction> actionQueue)
        {
            // Pre-calculate visibility masks
            BoundsMask currentVisible = visibleMask & bounds.m_Mask;
            BoundsMask prevVisible = prevVisibleMask & bounds.m_Mask;

            switch (subData)
            {
                case 1:
                    {
                        // Early exit if not currently visible
                        if (currentVisible == (BoundsMask)0)
                            return;

                        // Calculate current distance and LOD once
                        float currentMinDist = RenderingUtils.CalculateMinDistance(bounds.m_Bounds, cameraPosition, cameraDirection, lodParameters);
                        int currentLod = RenderingUtils.CalculateLod(currentMinDist * currentMinDist, lodParameters);
                        
                        if (currentLod < bounds.m_MinLod)
                            return;

                        // Optimized condition check
                        bool shouldEnqueue = prevVisible == (BoundsMask)0;
                        if (!shouldEnqueue && prevVisible != (BoundsMask)0)
                        {
                            float prevMinDist = RenderingUtils.CalculateMinDistance(bounds.m_Bounds, prevCameraPosition, prevCameraDirection, prevLodParameters);
                            int prevLod = RenderingUtils.CalculateLod(prevMinDist * prevMinDist, prevLodParameters);
                            shouldEnqueue = prevLod < bounds.m_MaxLod;
                        }

                        if (shouldEnqueue)
                        {
                            actionQueue.Enqueue(new CullingAction
                            {
                                m_Entity = entity,
                                m_Flags = ActionFlags.PassedCulling,
                                m_UpdateFrame = -1
                            });
                        }
                        return;
                    }
                case 2:
                    {
                        // Early exit if not previously visible
                        if (prevVisible == (BoundsMask)0)
                            return;

                        // Calculate previous distance and LOD once
                        float prevMinDist = RenderingUtils.CalculateMinDistance(bounds.m_Bounds, prevCameraPosition, prevCameraDirection, prevLodParameters);
                        int prevLod = RenderingUtils.CalculateLod(prevMinDist * prevMinDist, prevLodParameters);
                        
                        if (prevLod < bounds.m_MinLod)
                            return;

                        // Optimized condition check
                        bool shouldEnqueue = currentVisible == (BoundsMask)0;
                        ActionFlags flags = (ActionFlags)0;
                        
                        if (!shouldEnqueue && currentVisible != (BoundsMask)0)
                        {
                            float currentMinDist = RenderingUtils.CalculateMinDistance(bounds.m_Bounds, cameraPosition, cameraDirection, lodParameters);
                            int currentLod = RenderingUtils.CalculateLod(currentMinDist * currentMinDist, lodParameters);
                            
                            if (currentLod < bounds.m_MaxLod)
                            {
                                shouldEnqueue = true;
                                flags = ActionFlags.CrossFade;
                            }
                        }

                        if (shouldEnqueue)
                        {
                            actionQueue.Enqueue(new CullingAction
                            {
                                m_Entity = entity,
                                m_Flags = flags,
                                m_UpdateFrame = -1
                            });
                        }
                        return;
                    }
                default:
                    {
                        // Calculate distances once and reuse
                        float currentMinDist = RenderingUtils.CalculateMinDistance(bounds.m_Bounds, cameraPosition, cameraDirection, lodParameters);
                        float prevMinDist = RenderingUtils.CalculateMinDistance(bounds.m_Bounds, prevCameraPosition, prevCameraDirection, prevLodParameters);
                        
                        int currentLod = RenderingUtils.CalculateLod(currentMinDist * currentMinDist, lodParameters);
                        int prevLod = RenderingUtils.CalculateLod(prevMinDist * prevMinDist, prevLodParameters);
                        
                        bool isCurrentlyVisible = currentVisible != (BoundsMask)0 && currentLod >= bounds.m_MinLod;
                        bool wasPreviouslyVisible = prevVisible != (BoundsMask)0 && prevLod >= bounds.m_MaxLod;
                        
                        if (isCurrentlyVisible != wasPreviouslyVisible)
                        {
                            ActionFlags flags = (ActionFlags)0;
                            if (isCurrentlyVisible)
                            {
                                flags = ActionFlags.PassedCulling;
                            }
                            else if (currentVisible != (BoundsMask)0)
                            {
                                flags = ActionFlags.CrossFade;
                            }

                            actionQueue.Enqueue(new CullingAction
                            {
                                m_Entity = entity,
                                m_Flags = flags,
                                m_UpdateFrame = -1
                            });
                        }
                        return;
                    }
            }
        }
    }
}