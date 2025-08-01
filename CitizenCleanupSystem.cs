using Unity.Entities;
using Unity.Collections;
using Colossal.Logging;
using Game.Common;
using System.Linq;

namespace CitizenEntityCleaner
{
    /// <summary>
    /// ECS System that handles cleanup of citizen entities when triggered via settings UI
    /// </summary>
    public partial class CitizenCleanupSystem : SystemBase
    {
        private static ILog s_log = LogManager.GetLogger($"{nameof(CitizenEntityCleaner)}.{nameof(CitizenCleanupSystem)}");
        
        // Flag to trigger the cleanup operation
        private bool m_shouldRunCleanup = false;
        
        protected override void OnCreate()
        {
            base.OnCreate();
            s_log.Info("CitizenCleanupSystem created");
        }

        protected override void OnUpdate()
        {
            // Only run the cleanup when explicitly triggered
            if (!m_shouldRunCleanup)
                return;

            m_shouldRunCleanup = false;
            
            s_log.Info("Starting citizen entity cleanup...");
            
            // Your entity deletion query will go here
            RunEntityCleanup();
        }

        /// <summary>
        /// Triggers the cleanup operation to run on the next update
        /// </summary>
        public void TriggerCleanup()
        {
            s_log.Info("Cleanup operation triggered from settings");
            m_shouldRunCleanup = true;
        }

        /// <summary>
        /// This is where you'll put your existing entity deletion query code
        /// </summary>
        private void RunEntityCleanup()
        {
            // Temporary deleting of all broken citizen and household entities
            var query = SystemAPI.QueryBuilder().WithAll<Game.Citizens.HouseholdMember>().Build();
            foreach (var (householdMember, entity) in query.ToComponentDataArray<Game.Citizens.HouseholdMember>(Allocator.Temp).Zip(query.ToEntityArray(Allocator.Temp), (h, e) => (h, e)))
            {
                Entity householdEntity = householdMember.m_Household;
                if (!EntityManager.HasComponent<Game.Buildings.PropertyRenter>(householdEntity))
                {
                    DynamicBuffer<Game.Citizens.HouseholdCitizen> hhCitizens = SystemAPI.GetBuffer<Game.Citizens.HouseholdCitizen>(householdEntity);
                    foreach (var householdCitizen in hhCitizens)
                    {
                        Entity citizenEntity = householdCitizen.m_Citizen;
                        EntityManager.AddComponent<Deleted>(citizenEntity);
                    }
                }
            }

            s_log.Info("Entity cleanup completed (placeholder - add your query here)");
        }

        protected override void OnDestroy()
        {
            s_log.Info("CitizenCleanupSystem destroyed");
            base.OnDestroy();
        }
    }
} 