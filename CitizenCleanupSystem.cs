using Colossal.Logging;
using Game.Buildings;       // for PropertyRenter
using Game.Citizens;        // for Citizen, HouseholdMember, TravelPurpose
using Game.Common;          // for Deleted
using Unity.Collections;    // for NativeList
using Unity.Entities;       // Entity, SystemBase, ComponentType, EntityQuery, etc.
using Unity.Mathematics;    // for math
using Game.Agents;          // for MovingAway

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
        private EntityQuery m_householdMemberQuery;

        // settings
        private Setting? m_settings; // nullable. Store setting passed from Mod.Onload
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
            var candidates = new NativeList<Entity>(math.max(1, householdMembers.Length), allocator);

            try
            {
                using var processedHouseholds =
                    new NativeHashSet<Entity>(math.max(1, householdMembers.Length), Allocator.TempJob);

                foreach (var householdMember in householdMembers)
                {
                    Entity householdEntity = householdMember.m_Household;

                    // Skip if missing or already processed
                    if (!EntityManager.Exists(householdEntity)) continue;
                    if (!processedHouseholds.Add(householdEntity)) continue;

                    // Must have members buffer, skip households without members list
                    if (!EntityManager.HasBuffer<HouseholdCitizen>(householdEntity)) continue;

                    // Household category flags
                    bool hasPropertyRenter = EntityManager.HasComponent<PropertyRenter>(householdEntity);
                    bool isHomelessHH = EntityManager.HasComponent<HomelessHousehold>(householdEntity);
                    bool isCommuterHH = EntityManager.HasComponent<CommuterHousehold>(householdEntity);
                    bool isTouristHH = EntityManager.HasComponent<TouristHousehold>(householdEntity);

                    // Which categories are enabled?
                    bool wantCorrupt = (m_settings?.IncludeCorrupt ?? true);
                    bool wantHomeless = (m_settings?.IncludeHomeless == true);
                    bool wantCommuters = (m_settings?.IncludeCommuters == true);
                    bool wantMovingAwayNoPR = (m_settings?.IncludeMovingAwayNoPR == true);

                    // Corrupt resident = no PR and not homeless/commuter/tourist
                    bool isResidentCorrupt = !hasPropertyRenter && !isHomelessHH && !isCommuterHH && !isTouristHH;

                    // Do we need to inspect citizens in this household at all?
                    bool householdMatchesAny =
                        (wantHomeless && isHomelessHH) ||
                        (wantCommuters && isCommuterHH) ||
                        (wantCorrupt && isResidentCorrupt);

                    // If nothing about this HH is interesting AND we’re not doing the Moving-Away (no PR) pass,
                    // skip to the next household.
                    if (!householdMatchesAny && !wantMovingAwayNoPR) continue;

                    // Iterate members and apply per-citizen rules
                    var householdCitizens = EntityManager.GetBuffer<HouseholdCitizen>(householdEntity);

                    for (int j = 0; j < householdCitizens.Length; j++)
                    {
                        var citizenEntity = householdCitizens[j].m_Citizen;
                        if (!EntityManager.Exists(citizenEntity)) continue;
                        if (EntityManager.HasComponent<Deleted>(citizenEntity)) continue;

                        bool movingAway = IsMovingAway(citizenEntity);

                        bool add = false;

                        // Homeless ON → delete all members of homeless HH
                        if (wantHomeless && isHomelessHH)
                            add = true;

                        // Commuters ON → delete all members of commuter HH
                        else if (wantCommuters && isCommuterHH)
                            add = true;

                        // Corrupt ON → delete corrupt members but EXCLUDE Moving-Away
                        else if (wantCorrupt && isResidentCorrupt)
                            add = !movingAway;

                        // Independent toggle: Moving-Away (that have no PropertyRenter)
                        if (!add && wantMovingAwayNoPR && movingAway && !hasPropertyRenter)
                            add = true;

                        if (add)
                            candidates.Add(citizenEntity);
                    }
                }
            }
            catch (System.Exception ex)
            {
                s_log.Warn($"Error building deletion candidates list: {ex.Message}");
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

        // Returns true if the citizen is Moving-Away state.
        // rely on Game.Agents.MovingAway to avoid field-name guesswork.
        private bool IsMovingAway(Entity citizenEntity)
        {
            // Primary: tag component exists?
            if (EntityManager.HasComponent<MovingAway>(citizenEntity))
                return true;

            // Secondary cross-check: enum purpose value, not a flag
            if (EntityManager.HasComponent<TravelPurpose>(citizenEntity))
            {
                var tp = EntityManager.GetComponentData<TravelPurpose>(citizenEntity);
                if (tp.m_Purpose == Purpose.MovingAway)
                    return true;
            }

            return false;
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

            // reset throttles at start of each run
            m_lastProgressNotified = -1f;    // UI throttle
            m_lastLoggedBucket = -1;        // Log throttle
            m_cleanupIndex = 0;

            // immediately finish when nothing to do
            if (m_entitiesToCleanup.Length == 0)
            {
                s_log.Info("Cleanup requested, but there is nothing to clean.");
                if (m_entitiesToCleanup.IsCreated) m_entitiesToCleanup.Dispose();
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

            s_log.Info("CitizenCleanupSystem destroyed");
            base.OnDestroy();
        }
    }
} 
