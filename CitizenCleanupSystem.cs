using Unity.Entities;
using Unity.Collections;
using Colossal.Logging;
using Game.Common;

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
        
        // Cached query for reuse
        private EntityQuery m_householdMemberQuery;
        
        // Reference to settings
        private Setting m_settings;
        
        protected override void OnCreate()
        {
            base.OnCreate();
            
            // Initialize cached query
            m_householdMemberQuery = GetEntityQuery(ComponentType.ReadOnly<Game.Citizens.HouseholdMember>());
            
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
        /// Sets the settings reference for filtering
        /// </summary>
        public void SetSettings(Setting settings)
        {
            m_settings = settings;
        }

        /// <summary>
        /// Gets the current settings for filtering
        /// </summary>
        public Setting GetSettings()
        {
            return m_settings;
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
        /// Gets citizen statistics for display
        /// </summary>
        public (int totalCitizens, int corruptedCitizens) GetCitizenStatistics()
        {
            try
            {
                int totalCitizens = m_householdMemberQuery.CalculateEntityCount();
                int corruptedCitizens = GetCorruptedCitizenCount();
                
                return (totalCitizens, corruptedCitizens);
            }
            catch (System.Exception ex)
            {
                s_log.Warn($"Error getting citizen statistics: {ex.Message}");
                return (0, 0);
            }
        }

        /// <summary>
        /// Gets all corrupted citizen entities (those in households without PropertyRenter)
        /// Applies additional filtering based on settings for homeless and commuters
        /// </summary>
        private NativeList<Entity> GetCorruptedCitizenEntities(Allocator allocator)
        {
            var corruptedCitizens = new NativeList<Entity>(allocator);
            
            try
            {
                using var householdMembers = m_householdMemberQuery.ToComponentDataArray<Game.Citizens.HouseholdMember>(Allocator.TempJob);
                using var processedHouseholds = new NativeHashSet<Entity>(householdMembers.Length, Allocator.TempJob);
                
                foreach (var householdMember in householdMembers)
                {
                    Entity householdEntity = householdMember.m_Household;
                    
                    // Skip if already processed or doesn't exist
                    if (processedHouseholds.Contains(householdEntity) || !EntityManager.Exists(householdEntity))
                        continue;
                        
                    processedHouseholds.Add(householdEntity);
                    
                    // Check if household is corrupted (no PropertyRenter component)
                    if (!EntityManager.HasComponent<Game.Buildings.PropertyRenter>(householdEntity) &&
                        EntityManager.HasBuffer<Game.Citizens.HouseholdCitizen>(householdEntity))
                    {
                        var householdCitizens = SystemAPI.GetBuffer<Game.Citizens.HouseholdCitizen>(householdEntity);
                        
                        foreach (var householdCitizen in householdCitizens)
                        {
                            Entity citizenEntity = householdCitizen.m_Citizen;
                            
                            // Add valid, non-deleted citizens to cleanup list
                            if (EntityManager.Exists(citizenEntity) && !EntityManager.HasComponent<Deleted>(citizenEntity))
                            {
                                // Apply additional filtering based on settings
                                if (ShouldIncludeCitizen(citizenEntity))
                                {
                                    corruptedCitizens.Add(citizenEntity);
                                }
                            }
                        }
                    }
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
            using var corruptedCitizens = GetCorruptedCitizenEntities(Allocator.TempJob);
            return corruptedCitizens.Length;
        }

        /// <summary>
        /// Determines whether a citizen should be included in cleanup based on filter settings
        /// </summary>
        private bool ShouldIncludeCitizen(Entity citizenEntity)
        {
            var settings = m_settings;
            if (settings == null) return true; // Include all if settings unavailable
            
            // Check if citizen is a commuter and filter is disabled
            if (EntityManager.HasComponent<Game.Citizens.Citizen>(citizenEntity))
            {
                var citizen = EntityManager.GetComponentData<Game.Citizens.Citizen>(citizenEntity);
                if ((citizen.m_State & Game.Citizens.CitizenFlags.Commuter) != 0 && !settings.IncludeCommuters)
                {
                    return false; // Skip commuters when filter is disabled
                }
            }
            
            // Check if citizen is homeless and filter is disabled  
            if (EntityManager.HasComponent<Game.Creatures.Human>(citizenEntity))
            {
                var human = EntityManager.GetComponentData<Game.Creatures.Human>(citizenEntity);
                if ((human.m_Flags & (Game.Creatures.HumanFlags)0x40u) != 0 && !settings.IncludeHomeless)
                {
                    return false; // Skip homeless when filter is disabled
                }
            }
            
            return true; // Include by default
        }

        /// <summary>
        /// Performs the actual citizen entity cleanup by marking corrupted citizens for deletion
        /// </summary>
        private void RunEntityCleanup()
        {
            using var corruptedCitizens = GetCorruptedCitizenEntities(Allocator.TempJob);
            
            // Mark all corrupted citizens for deletion
            foreach (var citizenEntity in corruptedCitizens)
            {
                EntityManager.AddComponent<Deleted>(citizenEntity);
            }

            s_log.Info($"Entity cleanup completed. Marked {corruptedCitizens.Length} citizens for deletion.");
        }

        protected override void OnDestroy()
        {
            s_log.Info("CitizenCleanupSystem destroyed");
            base.OnDestroy();
        }
    }
} 