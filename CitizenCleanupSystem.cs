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
        
        // Cached queries for reuse
        private EntityQuery m_householdMemberQuery;
        private EntityQuery m_householdQuery;
        private EntityQuery m_propertyRenterQuery;
        
        protected override void OnCreate()
        {
            base.OnCreate();
            
            // Initialize cached queries
            m_householdMemberQuery = GetEntityQuery(ComponentType.ReadOnly<Game.Citizens.HouseholdMember>());
            m_householdQuery = GetEntityQuery(ComponentType.ReadOnly<Game.Citizens.Household>());
            m_propertyRenterQuery = GetEntityQuery(ComponentType.ReadOnly<Game.Buildings.PropertyRenter>());
            
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
        /// Gets current entity statistics without performing cleanup
        /// </summary>
        public (int totalCitizens, int totalHouseholds, int brokenHouseholds) GetEntityStatistics()
        {
            try
            {
                int totalCitizens = m_householdMemberQuery.CalculateEntityCount();
                int totalHouseholds = m_householdQuery.CalculateEntityCount();
                int householdsWithProperty = m_propertyRenterQuery.CalculateEntityCount();
                int brokenHouseholds = totalHouseholds - householdsWithProperty;
                
                return (totalCitizens, totalHouseholds, brokenHouseholds);
            }
            catch (System.Exception ex)
            {
                s_log.Warn($"Error getting entity statistics: {ex.Message}");
                return (0, 0, 0);
            }
        }

        /// <summary>
        /// Gets entities that would be affected by cleanup (for preview/counting)
        /// </summary>
        public int GetCleanupCandidateCount()
        {
            try
            {
                var householdMembers = m_householdMemberQuery.ToComponentDataArray<Game.Citizens.HouseholdMember>(Allocator.TempJob);
                var entities = m_householdMemberQuery.ToEntityArray(Allocator.TempJob);
                
                int candidateCount = 0;
                
                try
                {
                    for (int i = 0; i < householdMembers.Length; i++)
                    {
                        var householdMember = householdMembers[i];
                        Entity householdEntity = householdMember.m_Household;
                        
                        if (EntityManager.Exists(householdEntity) && 
                            !EntityManager.HasComponent<Game.Buildings.PropertyRenter>(householdEntity))
                        {
                            if (EntityManager.HasBuffer<Game.Citizens.HouseholdCitizen>(householdEntity))
                            {
                                DynamicBuffer<Game.Citizens.HouseholdCitizen> hhCitizens = SystemAPI.GetBuffer<Game.Citizens.HouseholdCitizen>(householdEntity);
                                candidateCount += hhCitizens.Length;
                            }
                        }
                    }
                }
                finally
                {
                    householdMembers.Dispose();
                    entities.Dispose();
                }
                
                return candidateCount;
            }
            catch (System.Exception ex)
            {
                s_log.Warn($"Error counting cleanup candidates: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// This is where you'll put your existing entity deletion query code
        /// </summary>
        private void RunEntityCleanup()
        {
            // Use the cached query for consistency
            var householdMembers = m_householdMemberQuery.ToComponentDataArray<Game.Citizens.HouseholdMember>(Allocator.TempJob);
            var entities = m_householdMemberQuery.ToEntityArray(Allocator.TempJob);
            
            int deletedCount = 0;
            
            try
            {
                for (int i = 0; i < householdMembers.Length; i++)
                {
                    var householdMember = householdMembers[i];
                    var entity = entities[i];
                    
                    Entity householdEntity = householdMember.m_Household;
                    
                    // Validate household entity still exists and doesn't have PropertyRenter
                    if (EntityManager.Exists(householdEntity) && 
                        !EntityManager.HasComponent<Game.Buildings.PropertyRenter>(householdEntity))
                    {
                        // Check if the household still has the citizen buffer
                        if (EntityManager.HasBuffer<Game.Citizens.HouseholdCitizen>(householdEntity))
                        {
                            DynamicBuffer<Game.Citizens.HouseholdCitizen> hhCitizens = SystemAPI.GetBuffer<Game.Citizens.HouseholdCitizen>(householdEntity);
                            
                            // Create a copy of the buffer to avoid iteration issues if buffer changes
                            var citizensCopy = hhCitizens.ToNativeArray(Allocator.Temp);
                            
                            foreach (var householdCitizen in citizensCopy)
                            {
                                Entity citizenEntity = householdCitizen.m_Citizen;
                                
                                // Validate citizen entity exists and doesn't already have Deleted component
                                if (EntityManager.Exists(citizenEntity) && 
                                    !EntityManager.HasComponent<Deleted>(citizenEntity))
                                {
                                    EntityManager.AddComponent<Deleted>(citizenEntity);
                                    deletedCount++;
                                }
                            }
                            
                            citizensCopy.Dispose();
                        }
                    }
                }
            }
            finally
            {
                householdMembers.Dispose();
                entities.Dispose();
            }

            s_log.Info($"Entity cleanup completed. Marked {deletedCount} citizens for deletion.");
        }

        protected override void OnDestroy()
        {
            s_log.Info("CitizenCleanupSystem destroyed");
            base.OnDestroy();
        }
    }
} 