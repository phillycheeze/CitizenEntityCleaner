using System.Reflection;
using HarmonyLib;
using Unity.Entities;
using Game.Objects;
using Game.Tools;
using Game.Common;

#if false
namespace CitizenEntityCleaner.Patches
{
    [HarmonyPatch(typeof(SearchSystem))]
    [HarmonyPatch("OnCreate")]
    public static class SearchSystem_OnCreate_Patch
    {
        static readonly FieldInfo updatedQueryField = typeof(SearchSystem).GetField("m_UpdatedStaticsQuery", BindingFlags.NonPublic | BindingFlags.Instance);
        static readonly FieldInfo allQueryField     = typeof(SearchSystem).GetField("m_AllStaticsQuery",    BindingFlags.NonPublic | BindingFlags.Instance);

        static void Postfix(SearchSystem __instance)
        {
            // Exclude all occluded entities from the static search-tree queries
            var occludedType = ComponentType.ReadOnly<OccludedTag>();
            var tempType     = ComponentType.ReadOnly<Temp>();
            // Rebuild the "updated statics" query
            var updatedDesc = (new EntityQueryDesc
            {
                All  = new[] { ComponentType.ReadOnly<Object>(), ComponentType.ReadOnly<Static>() },
                Any  = new[] { ComponentType.ReadOnly<Updated>(), ComponentType.ReadOnly<Deleted>() },
                None = new[] { tempType, occludedType }
            });
            var newUpdated = __instance.EntityManager.CreateEntityQuery(updatedDesc);
            updatedQueryField.SetValue(__instance, newUpdated);

            // Rebuild the "all statics" query
            var allDesc = new EntityQueryDesc
            {
                All  = new[] { ComponentType.ReadOnly<Object>(), ComponentType.ReadOnly<Static>() },
                None = new[] { tempType, occludedType }
            };
            var newAll = __instance.EntityManager.CreateEntityQuery(allDesc);
            allQueryField.SetValue(__instance, newAll);
        }
    }
}
#endif
