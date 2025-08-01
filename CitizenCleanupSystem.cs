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
        private static ILog s_log = Mod.log;
        
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
        /// Gets detailed entity statistics for display
        /// </summary>
        public (int totalCitizens, int totalHouseholds, int corruptedCitizens, int corruptedHouseholds) GetDetailedEntityStatistics()
        {
            try
            {
                int totalCitizens = m_householdMemberQuery.CalculateEntityCount();
                int totalHouseholds = m_householdQuery.CalculateEntityCount();
                int householdsWithProperty = m_propertyRenterQuery.CalculateEntityCount();
                int corruptedHouseholds = totalHouseholds - householdsWithProperty;
                
                // Count corrupted citizens (those in households without PropertyRenter)
                int corruptedCitizens = GetCorruptedCitizenCount();
                
                return (totalCitizens, totalHouseholds, corruptedCitizens, corruptedHouseholds);
            }
            catch (System.Exception ex)
            {
                s_log.Warn($"Error getting detailed entity statistics: {ex.Message}");
                return (0, 0, 0, 0);
            }
        }

        /// <summary>
        /// Gets all corrupted citizen entities (those in households without PropertyRenter)
        /// </summary>
        private NativeList<Entity> GetCorruptedCitizenEntities(Allocator allocator)
        {
            var corruptedCitizens = new NativeList<Entity>(allocator);
            
            try
            {
                var householdMembers = m_householdMemberQuery.ToComponentDataArray<Game.Citizens.HouseholdMember>(Allocator.TempJob);
                var processedHouseholds = new NativeHashSet<Entity>(householdMembers.Length, Allocator.TempJob);
                
                try
                {
                    for (int i = 0; i < householdMembers.Length; i++)
                    {
                        var householdMember = householdMembers[i];
                        Entity householdEntity = householdMember.m_Household;
                        
                        // Skip if we've already processed this household
                        if (processedHouseholds.Contains(householdEntity))
                            continue;
                            
                        processedHouseholds.Add(householdEntity);
                        
                        if (EntityManager.Exists(householdEntity) && 
                            !EntityManager.HasComponent<Game.Buildings.PropertyRenter>(householdEntity))
                        {
                            if (EntityManager.HasBuffer<Game.Citizens.HouseholdCitizen>(householdEntity))
                            {
                                DynamicBuffer<Game.Citizens.HouseholdCitizen> hhCitizens = SystemAPI.GetBuffer<Game.Citizens.HouseholdCitizen>(householdEntity);
                                
                                foreach (var householdCitizen in hhCitizens)
                                {
                                    Entity citizenEntity = householdCitizen.m_Citizen;
                                    
                                    // Validate citizen entity exists and doesn't already have Deleted component
                                    if (EntityManager.Exists(citizenEntity) && 
                                        !EntityManager.HasComponent<Deleted>(citizenEntity))
                                    {
                                        corruptedCitizens.Add(citizenEntity);
                                    }
                                }
                            }
                        }
                    }
                }
                finally
                {
                    householdMembers.Dispose();
                    processedHouseholds.Dispose();
                }
            }
            catch (System.Exception ex)
            {
                s_log.Warn($"Error getting corrupted citizen entities: {ex.Message}");
            }
            
            return corruptedCitizens;
        }

        /// <summary>
        /// Gets count of corrupted citizens
        /// </summary>
        private int GetCorruptedCitizenCount()
        {
            var corruptedCitizens = GetCorruptedCitizenEntities(Allocator.TempJob);
            int count = corruptedCitizens.Length;
            corruptedCitizens.Dispose();
            return count;
        }

        /// <summary>
        /// This is where you'll put your existing entity deletion query code
        /// </summary>
        private void RunEntityCleanup()
        {
            // Get all corrupted citizen entities using shared query logic
            var corruptedCitizens = GetCorruptedCitizenEntities(Allocator.TempJob);
            
            int deletedCount = 0;
            
            try
            {
                // Mark all corrupted citizens for deletion
                for (int i = 0; i < corruptedCitizens.Length; i++)
                {
                    Entity citizenEntity = corruptedCitizens[i];
                    EntityManager.AddComponent<Deleted>(citizenEntity);
                    deletedCount++;
                }
            }
            finally
            {
                corruptedCitizens.Dispose();
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