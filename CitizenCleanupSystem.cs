using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
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
        
        // ---- constants ----
        private const int CLEANUP_CHUNK_SIZE = 2000;
        private const Game.Creatures.HumanFlags HomelessFlag = Game.Creatures.HumanFlags.Homeless;
        
        // ---- fields ----
        // Flag to trigger the cleanup operation
        private bool m_shouldRunCleanup = false;
        
        // Chunked cleanup state
        private NativeList<Entity> m_entitiesToCleanup;
        private int m_cleanupIndex = 0;
        private bool m_isChunkedCleanupInProgress = false;

        // Cached query for reuse
        private EntityQuery m_householdMemberQuery;
        
        // Reference to settings
        private Setting m_settings;
        
        // Callback for when cleanup is in progress and completed
        // event instead of public delegate prevents external code accidental overwrite delegate list
        public event System.Action<float> OnCleanupProgress;
        public event System.Action OnCleanupCompleted;
        
       
        protected override void OnCreate()
        {   
            m_householdMemberQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[] { ComponentType.ReadOnly<Game.Citizens.HouseholdMember>() },
                None = new ComponentType[] { ComponentType.ReadOnly<Deleted>() }
            });
            
            base.OnCreate();

            s_log.Info("CitizenCleanupSystem created");
        }

        // non-blocking OnUpdate() starts run and lets next frame finish it.
        protected override void OnUpdate()
        {
         // If nothing to do, bail out quickly
         if (!m_isChunkedCleanupInProgress && !m_shouldRunCleanup)
            return;
            
            // Handle chunked cleanup if in progress, otherwise fallback to boolean flag
            if (m_isChunkedCleanupInProgress)
            {
                ProcessCleanupChunk();
                return;
            }

            // Start a new cleanup run
            m_shouldRunCleanup = false;
            s_log.Info("Starting citizen entity cleanup...");
            StartChunkedCleanup();
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
                // Create a hash set with capacity equal to number of household members, but never less than 1
                  using var processedHouseholds =
                      new NativeHashSet<Entity>(math.max(1, householdMembers.Length), Allocator.TempJob);

                
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
                            
                            if (EntityManager.Exists(citizenEntity) && !EntityManager.HasComponent<Deleted>(citizenEntity))
                            {
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
            if (settings == null) return true;
            
            // Check if citizen is a commuter and filter is disabled
            if (EntityManager.HasComponent<Game.Citizens.Citizen>(citizenEntity))
            {
                var citizen = EntityManager.GetComponentData<Game.Citizens.Citizen>(citizenEntity);
                if ((citizen.m_State & Game.Citizens.CitizenFlags.Commuter) != 0 && !settings.IncludeCommuters)
                {
                    return false;
                }
            }
            
            // Check if citizen is homeless and filter is disabled  
            
            if (EntityManager.HasComponent<Game.Citizens.CurrentTransport>(citizenEntity))
            {
                var transport = EntityManager.GetComponentData<Game.Citizens.CurrentTransport>(citizenEntity);
                var human = transport.m_CurrentTransport;

                // add guard to avoid exceptions by checking if Game.Creatures.Human exists.
                if (EntityManager.Exists(human) && EntityManager.HasComponent<Game.Creatures.Human>(human))
                {
                    var humanData = EntityManager.GetComponentData<Game.Creatures.Human>(human);
                    if ((humanData.m_Flags & HomelessFlag) != 0 && !settings.IncludeHomeless)
                    return false;
                }
                }
            }
            
            return true;
        }

        /// <summary>
        /// Starts the chunked cleanup process
        /// </summary>
        private void StartChunkedCleanup()
        {
            m_entitiesToCleanup = GetCorruptedCitizenEntities(Allocator.Persistent);
            m_cleanupIndex = 0;
            m_isChunkedCleanupInProgress = true;
            
            s_log.Info($"Starting chunked cleanup of {m_entitiesToCleanup.Length} citizens in chunks of {CLEANUP_CHUNK_SIZE}");
        }
        
        /// <summary>
        /// Processes one chunk of entities per frame
        /// </summary>
        private void ProcessCleanupChunk()
        {
            if (!m_entitiesToCleanup.IsCreated || m_cleanupIndex >= m_entitiesToCleanup.Length)
            {
                FinishChunkedCleanup();
                return;
            }
            
            int remainingEntities = m_entitiesToCleanup.Length - m_cleanupIndex;
            int chunkSize = math.min(CLEANUP_CHUNK_SIZE, remainingEntities);
            
            var chunk = m_entitiesToCleanup.AsArray().GetSubArray(m_cleanupIndex, chunkSize);
            EntityManager.AddComponent<Deleted>(chunk);
            
            m_cleanupIndex += chunkSize;
            
            float progress = (float)m_cleanupIndex / m_entitiesToCleanup.Length;
            OnCleanupProgress?.Invoke(progress);
            
            s_log.Info($"Processed chunk: {m_cleanupIndex}/{m_entitiesToCleanup.Length} citizens ({progress:P0})");
        }
        
        /// <summary>
        /// Finishes the chunked cleanup process
        /// </summary>
        private void FinishChunkedCleanup()
        {
            if (m_entitiesToCleanup.IsCreated)
            {
                s_log.Info($"Entity cleanup completed. Marked {m_entitiesToCleanup.Length} citizens for deletion.");
                m_entitiesToCleanup.Dispose();
            }
            
            m_isChunkedCleanupInProgress = false;
            m_cleanupIndex = 0;
            
            // Notify settings that cleanup is complete
            OnCleanupCompleted?.Invoke();
        }

        protected override void OnDestroy()
        {
            if (m_entitiesToCleanup.IsCreated)
            {
                m_entitiesToCleanup.Dispose();
            }
            
            s_log.Info("CitizenCleanupSystem destroyed");
            base.OnDestroy();
        }
    }
} 
