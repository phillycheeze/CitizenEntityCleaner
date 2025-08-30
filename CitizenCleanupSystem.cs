using Colossal.Logging;
using Game.Buildings;   // for PropertyRenter
using Game.Citizens;    // for Citizen, HouseholdMember
using Game.Common;       //for Deleted
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace CitizenEntityCleaner
{
    /// <summary>
    /// ECS System that handles cleanup of citizen entities when triggered via settings UI
    /// </summary>
    public partial class CitizenCleanupSystem : SystemBase
    {
        private static readonly ILog s_log = Mod.log;

        #region Constants
        private const int CLEANUP_CHUNK_SIZE = 2000;    // how many entities to mark per frame
        private const int LOG_BUCKET_PERCENT = 10;    // log once per N% (change to 5 for more frequent 5% logs)
        #endregion

        #region Fields
        // ---- state, queries ----
        private float m_lastProgressNotified = -1f;    // UI progress throttle (~5% steps)
        private int m_lastLoggedBucket = -1;   // -1 means "nothing logged yet" for log throttle state
        private bool m_shouldRunCleanup = false;    // Flag to trigger the cleanup operation

        // Chunked cleanup state
        private NativeList<Entity> m_entitiesToCleanup;
        private int m_cleanupIndex = 0;
        private bool m_isChunkedCleanupInProgress = false;

        // Cached query for reuse
        private EntityQuery m_citizenQuery; // query for all citizens (exclude Deleted) to build commuter snapshot
        private EntityQuery m_householdMemberQuery;

        // settings + snapshots
        private Setting? m_settings; // nullable. Store setting passed from Mod.Onload
        private NativeHashSet<Entity> m_commuterCitizens; // snapshot of commuters (built when commuter checkbox is ON)
        #endregion

        #region Events
        // Callback for when cleanup is in progress and completed
        // event instead of public delegate prevents external code accidental overwrite delegate list
        public event System.Action<float>? OnCleanupProgress;
        public event System.Action? OnCleanupCompleted;
        public event System.Action? OnCleanupNoWork;
        #endregion


        /// <summary>
        /// Setup queries and log creation
        /// </summary>
        protected override void OnCreate()
        {   
            m_householdMemberQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new[] { ComponentType.ReadOnly<HouseholdMember>() },
                None = new[] { ComponentType.ReadOnly<Deleted>() }
            });
            
            m_citizenQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new[] { ComponentType.ReadOnly<Citizen>() },
                None = new[] { ComponentType.ReadOnly<Deleted>() }
            });

            base.OnCreate();

            s_log.Info("CitizenCleanupSystem created");
        }

        // Non-blocking: process one chunk if run is active, otherwise start a run if requested
        protected override void OnUpdate()
        {
         // If nothing active & nothing requested, do nothing this frame
         if (!m_isChunkedCleanupInProgress && !m_shouldRunCleanup)
            return;
            
            // Handle chunked cleanup if in progress, otherwise fallback to boolean flag
            if (m_isChunkedCleanupInProgress)
            {
                ProcessCleanupChunk();
                return;
            }

            // Start new cleanup run: a run requested via TriggerCleanup. Clear request flag, log it, initialize chunked workflow.
            m_shouldRunCleanup = false;
            s_log.Info("Starting citizen entity cleanup...");
            StartChunkedCleanup();
        }

        #region Public API
        /// <summary>
        /// Sets the settings reference for filtering
        /// </summary>
        public void SetSettings(Setting settings) => m_settings = settings;

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
        public (int totalCitizens, int citizensToClean) GetCitizenStatistics()
        {
            try
            {
                int totalCitizens = m_householdMemberQuery.CalculateEntityCount();
                int citizensToClean = GetCitizensToCleanCount();

                return (totalCitizens, citizensToClean);
            }
            catch (System.Exception ex)
            {
                s_log.Warn($"Error getting citizen statistics: {ex.Message}");
                return (0, 0);
            }
        }
        #endregion

        #region Selection Logic (build deletion set)
        /// <summary>
        /// Builds the deletion set based on toggle options == true:
        /// - Corrupt households (no PropertyRenter & not homeless/commuter/tourist)
        /// - HomelessHousehold members when IncludeHomeless == true
        /// - CommuterHousehold members when IncludeCommuters == true
        /// Note: tourists are implicitly safe (hotels provide PropertyRenter); homeless check is Transport-agnostic.
        /// </summary>
        private NativeList<Entity> GetDeletionCandidates(Allocator allocator)
        {
            using var householdMembers = m_householdMemberQuery.ToComponentDataArray<HouseholdMember>(Allocator.TempJob);
            var candidates = new NativeList<Entity>(math.max(1, householdMembers.Length), allocator); // presize capacity - reduce reallocs

            try
            {
                // Create a hash set with capacity equal to number of household members, but never less than 1
                using var processedHouseholds =
                    new NativeHashSet<Entity>(math.max(1, householdMembers.Length), Allocator.TempJob);

                foreach (var householdMember in householdMembers)
                {
                    Entity householdEntity = householdMember.m_Household;

                    // Skip if missing or already processed
                    if (!EntityManager.Exists(householdEntity)) continue;
                    if (!processedHouseholds.Add(householdEntity)) continue;

                    // Must have members buffer
                    if (!EntityManager.HasBuffer<HouseholdCitizen>(householdEntity))
                        continue;

                    // If household has PropertyRenter, it's not corrupt → skip
                    bool hasPropertyRenter = EntityManager.HasComponent<PropertyRenter>(householdEntity);
                    if (hasPropertyRenter)
                        continue;

                    // No PropertyRenter: could be Homeless, Commuter, Tourist, or a truly corrupt resident household
                    bool isHomelessHH = EntityManager.HasComponent<HomelessHousehold>(householdEntity);
                    bool isCommuterHH = EntityManager.HasComponent<CommuterHousehold>(householdEntity);
                    bool isTouristHH = EntityManager.HasComponent<TouristHousehold>(householdEntity);  // Optional guard: tourists generally have PropertyRenter via hotels already

                    // Include when the matching toggle option is ON:
                    // - Corrupt resident households: no PropertyRenter (not homeless/commuter/tourist) → IncludeCorrupt
                    // - Homeless households    → IncludeHomeless
                    // - Commuter households    → IncludeCommuters
                    bool isResidentCorrupt = (!isHomelessHH && !isCommuterHH && !isTouristHH); // resident + no PR ⇒ corrupt

                    bool includeThisHousehold =
                        (isResidentCorrupt && (m_settings?.IncludeCorrupt ?? true))   // include if toggle ON (default ON)
                        || (isHomelessHH && (m_settings?.IncludeHomeless == true))      // include if toggle ON
                        || (isCommuterHH && (m_settings?.IncludeCommuters == true));    // include if toggle ON

                    if (!includeThisHousehold)
                        continue;

                    // Include ALL members of this (already-approved) household
                    var householdCitizens = EntityManager.GetBuffer<HouseholdCitizen>(householdEntity);
                    for (int j = 0; j < householdCitizens.Length; j++)
                    {
                        var citizenEntity = householdCitizens[j].m_Citizen;
                        if (!EntityManager.Exists(citizenEntity)) continue;
                        if (EntityManager.HasComponent<Deleted>(citizenEntity)) continue;

                        candidates.Add(citizenEntity);
                    }
                }
            }
            catch (System.Exception ex)
            {
                s_log.Warn($"Error building deletion candidates: {ex.Message}");
            }
            
            return candidates;
        }

        /// <summary>
        /// Gets the count of citizens to clean based on toggles:
        /// IncludeCorrupt, IncludeHomeless, IncludeCommuters.
        /// Internally counts results of GetDeletionCandidates
        /// </summary>
        private int GetCitizensToCleanCount()
        {
            using var candidates = GetDeletionCandidates(Allocator.TempJob);
            return candidates.Length;
        }
        #endregion

        #region Helpers
        // Build a one-shot snapshot of all commuter citizens
        private void BuildCommuterSet(Allocator alloc)
        {
            if (m_commuterCitizens.IsCreated) m_commuterCitizens.Dispose();

            using var entities = m_citizenQuery.ToEntityArray(Allocator.TempJob);
            using var citizens = m_citizenQuery.ToComponentDataArray<Citizen>(Allocator.TempJob);

            int capacity = math.max(1, entities.Length);
            m_commuterCitizens = new NativeHashSet<Entity>(capacity, alloc);

            for (int i = 0; i < citizens.Length; i++)
            {
                if ((citizens[i].m_State & CitizenFlags.Commuter) != 0)
                    m_commuterCitizens.Add(entities[i]);
            }
        }

        // Merge (set-union) the commuter snapshot into deletion list, de-duplicated
        private void UnionCommutersIntoCleanup()
        {
            if (!m_commuterCitizens.IsCreated) return;

            // De-dupe with a temp set
            var unique = new NativeHashSet<Entity>(math.max(1, m_entitiesToCleanup.Length * 2), Allocator.Temp);

            // Seed with current deletion list
            for (int i = 0; i < m_entitiesToCleanup.Length; i++)
                unique.Add(m_entitiesToCleanup[i]);

            // Add all commuters (skip invalid or already deleted)
            foreach (var e in m_commuterCitizens)
            {
                if (!EntityManager.Exists(e)) continue;
                if (EntityManager.HasComponent<Deleted>(e)) continue;
                unique.Add(e);
            }

            // Rebuild deletion list from the set
            if (m_entitiesToCleanup.IsCreated) m_entitiesToCleanup.Dispose();

            m_entitiesToCleanup = new NativeList<Entity>(unique.Count, Allocator.Persistent);

            foreach (var e in unique)
            {
                m_entitiesToCleanup.Add(e);
            }

            unique.Dispose();

        }
        #endregion

        #region Chunked Cleanup Workflow
        /// <summary>
        /// Starts the chunked cleanup process
        /// </summary>
        private void StartChunkedCleanup()
        {
            // Build the base set according to toggles:
            // Corrupt residents (if IncludeCorrupt) + Homeless (if IncludeHomeless) + Commuters (if IncludeCommuters)
            m_entitiesToCleanup = GetDeletionCandidates(Allocator.Persistent);

            // If "Include Commuters" is ON, add all commuters
            if (m_settings?.IncludeCommuters == true)
            {
                BuildCommuterSet(Allocator.Persistent); // scans flags, builds snapshot
                UnionCommutersIntoCleanup(); // merge the base deletion list + commuters lists (+ de-dupe)
            }

            // reset throttles at start of each run
            m_lastProgressNotified = -1f;    // UI throttle
            m_lastLoggedBucket = -1;        // Log throttle
            m_cleanupIndex = 0;

            // immediately finish when nothing to do
            if (m_entitiesToCleanup.Length == 0)
            {
                s_log.Info("Cleanup requested, but there is nothing to clean.");
                if (m_entitiesToCleanup.IsCreated) m_entitiesToCleanup.Dispose();
                if (m_commuterCitizens.IsCreated) m_commuterCitizens.Dispose(); // avoid lingering snapshot
                m_isChunkedCleanupInProgress = false;

                OnCleanupProgress?.Invoke(1f);  // optional: send final progress of 100%
                OnCleanupNoWork?.Invoke();
                return;
            }

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

            // Delete a chunk this frame
            int remainingEntities = m_entitiesToCleanup.Length - m_cleanupIndex;
            int chunkSize = math.min(CLEANUP_CHUNK_SIZE, remainingEntities);
            
            var chunk = m_entitiesToCleanup.AsArray().GetSubArray(m_cleanupIndex, chunkSize);
            EntityManager.AddComponent<Deleted>(chunk);
            
            m_cleanupIndex += chunkSize;

            // --- Progress (float) for UI, throttled ~5% ---
            float progress = (float)m_cleanupIndex / m_entitiesToCleanup.Length;
            if (progress >= 0.999f || progress - m_lastProgressNotified >= 0.05f)
            {
                m_lastProgressNotified = progress;
                OnCleanupProgress?.Invoke(progress);
            }

            // --- Progress (int buckets) for logs ---
            //  LOG_BUCKET_PERCENT = 10 to reduce log spam; change to 5 for 5% (more spam)
            int percent = math.min(100, (int)math.floor(progress * 100f));
            int bucket  = percent / LOG_BUCKET_PERCENT;
            
            if (bucket > m_lastLoggedBucket)
            {
                 m_lastLoggedBucket = bucket;
                 if (percent >= 100)
                    s_log.Info("Cleanup 100% (finalizing)…");
                else
                    s_log.Info($"Cleanup {bucket * LOG_BUCKET_PERCENT}%…");
                }
            }

        /// <summary>
        /// Finishes the chunked cleanup process
        /// </summary>
        private void FinishChunkedCleanup()
        {
            if (m_entitiesToCleanup.IsCreated)
            {
                s_log.Info($"Cleanup completed. Marked {m_entitiesToCleanup.Length} entities for deletion.");
                m_entitiesToCleanup.Dispose();
                m_entitiesToCleanup = default;    // clear handle after dispose
            }

            if (m_commuterCitizens.IsCreated)
            {
                m_commuterCitizens.Dispose();
            }

            m_isChunkedCleanupInProgress = false;

            // Final UI snap to 100% only if we didn't already report it
            if (m_lastProgressNotified < 0.999f)
            {
            OnCleanupProgress?.Invoke(1f);
            }

            // Reset state for next run
            m_cleanupIndex = 0;
            m_lastProgressNotified = -1f;    // UI throttle reset
            m_lastLoggedBucket     = -1;    // log throttle reset

            // Notify settings that cleanup is complete
            OnCleanupCompleted?.Invoke();   
        }
        #endregion

        protected override void OnDestroy()
        {
            if (m_entitiesToCleanup.IsCreated)
            {
                m_entitiesToCleanup.Dispose();
            }
            
            if (m_commuterCitizens.IsCreated)
            {
                m_commuterCitizens.Dispose();
            }

            s_log.Info("CitizenCleanupSystem destroyed");
            base.OnDestroy();
        }
    }
} 
