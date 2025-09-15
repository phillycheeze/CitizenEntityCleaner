using Colossal.Logging;
<<<<<<< HEAD
using Game.Buildings;       // for PropertyRenter
using Game.Citizens;        // for Citizen, HouseholdMember, TravelPurpose
using Game.Common;          // for Deleted
using Unity.Collections;    // for NativeList
using Unity.Entities;       // Entity, SystemBase, ComponentType, EntityQuery, etc.
using Unity.Mathematics;    // for math
using Game.Agents;          // for MovingAway
using System.Text;
=======

using Game.Common;
>>>>>>> ebe24aa (Refactor: split CitizenCleanupSystem into Scan (read-only) and Apply (write-side); no behavior change)

using Unity.Collections;
using Unity.Entities;


namespace CitizenEntityCleaner
{
    // PART: System — system loop (OnCreate/OnUpdate/OnDestroy), public API, shared fields/events
    /// <summary>
    /// ECS System for cleanup of citizen entities triggered via UI
    /// </summary>
    public partial class CitizenCleanupSystem : SystemBase
    {
        private static readonly ILog s_log = Mod.log;

<<<<<<< HEAD
        // For selection bookkeeping (category + tallies)
        private enum CleanupType { None, Corrupt, Homeless, Commuters, MovingAway }

        private struct DeletionCounts
        {
            public int Corrupt, Homeless, Commuters, MovingAway;

            public void BumpCount(CleanupType type)
            {
                switch (type)
                {
                    case CleanupType.Corrupt: Corrupt++; break;
                    case CleanupType.Homeless: Homeless++; break;
                    case CleanupType.Commuters: Commuters++; break;
                    case CleanupType.MovingAway: MovingAway++; break;
                    case CleanupType.None:        /* no-op */    break;
                }
            }
        }

        private DeletionCounts m_lastCounts;

        #region Constants
        private const int CLEANUP_CHUNK_SIZE = 2000;   // entities to mark per frame
        #endregion

        #region Fields
        // ---- state, queries ----
        private float m_lastProgressNotified = -1f;   // UI progress throttle (~5% steps)
        private bool m_shouldRunCleanup = false;    // Flag to trigger cleanup operation
=======
        // ---- constants ----
        private const int CLEANUP_CHUNK_SIZE = 2000;    // how many entities to mark per frame
        private const Game.Creatures.HumanFlags HomelessFlag = Game.Creatures.HumanFlags.Homeless;  // explicit enum (was a magic number)
        private const int LOG_BUCKET_PERCENT = 10;    // log once per N% (change to 5 for more frequent 5% logs)

        // ---- fields ----
        private float m_lastProgressNotified = -1f;    // UI progress throttle (~5% steps)
        private int m_lastLoggedBucket = -1;   // -1 means "nothing logged yet" for log throttle state

        private bool m_shouldRunCleanup = false;    // Flag to trigger the cleanup operation
>>>>>>> ebe24aa (Refactor: split CitizenCleanupSystem into Scan (read-only) and Apply (write-side); no behavior change)

        // Chunked cleanup state
        private NativeList<Entity> m_entitiesToCleanup;
        private int m_cleanupIndex = 0;
        private bool m_isChunkedCleanupInProgress = false;

        // Cached query for reuse
        private EntityQuery m_householdMemberQuery;
<<<<<<< HEAD
=======


        private Setting? m_settings; // nullable. Store setting passed from Mod.Onload
>>>>>>> ebe24aa (Refactor: split CitizenCleanupSystem into Scan (read-only) and Apply (write-side); no behavior change)

        // settings
        private Setting? m_settings;
        #endregion

        #region Events
        // Callback for when cleanup is in progress and completed
        public event System.Action<float>? OnCleanupProgress;
        public event System.Action? OnCleanupCompleted;
        public event System.Action? OnCleanupNoWork;
<<<<<<< HEAD
        #endregion


        /// <summary>
        /// Setup queries and log creation
        /// </summary>
=======

>>>>>>> ebe24aa (Refactor: split CitizenCleanupSystem into Scan (read-only) and Apply (write-side); no behavior change)
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

            // Start new cleanup run: requested via TriggerCleanup. Clear request flag, log it, initialize chunked workflow.
            m_shouldRunCleanup = false;
            StartChunkedCleanup();
        }

        #region Public API
        /// <summary>
        /// Sets the settings reference for filtering
        /// </summary>
        public void SetSettings(Setting settings)
        {   
            m_settings = settings;
        }

        /// <summary>
        /// Triggers the cleanup operation to run on the next update
        /// </summary>
        public void TriggerCleanup()
        {
#if DEBUG
            s_log.Debug("[Cleanup] trigger requested (from Settings UI)");
#endif
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
<<<<<<< HEAD
                int citizensToClean = GetCitizensToCleanCount();

                return (totalCitizens, citizensToClean);
=======
                int corruptedCitizens = GetCorruptedCitizenCount();

                return (totalCitizens, corruptedCitizens);
>>>>>>> ebe24aa (Refactor: split CitizenCleanupSystem into Scan (read-only) and Apply (write-side); no behavior change)
            }
            catch (System.Exception ex)
            {
                s_log.Warn($"Error getting citizen statistics: {ex.Message}");
                return (0, 0);
            }
        }

<<<<<<< HEAD
        public bool HasAnyCitizenData()
        {
            try
            {
                // Cheap test for City Loaded: check for any HouseholdMember entity
                return m_householdMemberQuery.CalculateEntityCount() > 0;
            }
            catch
            {
                return false;
            }
        }
#endregion

        #region Selection Logic (build deletion set)
        /// <summary>
        /// Builds deletion set based on toggles (or overrides for debug preview):
        /// - Corrupt households (no PropertyRenter & not homeless/commuter/tourist) - excludes members who are Moving-Away
        /// - HomelessHousehold members when IncludeHomeless == true
        /// - CommuterHousehold members when IncludeCommuters == true
        /// - Moving-Away (no PropertyRenter) when IncludeMovingAwayNoPR == true
        ///
        /// Notes:
        /// • Optional overrides let callers force a specific combo (e.g., Debug preview runs "Corrupt-only" regardless of checkboxes).
        /// • If "tally" = true, method increments per-category counters in "m_lastCounts" as it builds the candidate list.
        ///     Set tally = false only when need the list/count (e.g., UI refresh or preview).
        /// </summary>
        private NativeList<Entity> GetDeletionCandidates(
            Allocator allocator,
            bool tally = true,
            bool? overrideWantCorrupt = null,
            bool? overrideWantHomeless = null,
            bool? overrideWantCommuters = null,
            bool? overrideWantMovingAwayNoPR = null)
        {
            using var householdMembers = m_householdMemberQuery.ToComponentDataArray<HouseholdMember>(Allocator.TempJob);
            var candidates = new NativeList<Entity>(math.max(1, householdMembers.Length), allocator);

        
            // Mirror UI defaults: Corrupt defaults ON; others default OFF
            // Order: override → UI settings → fallback
            bool wantCorrupt = ResolveToggle(overrideWantCorrupt, m_settings?.IncludeCorrupt, fallback: true);
            bool wantHomeless = ResolveToggle(overrideWantHomeless, m_settings?.IncludeHomeless, fallback: false);
            bool wantCommuters = ResolveToggle(overrideWantCommuters, m_settings?.IncludeCommuters, fallback: false);
            bool wantMovingAwayNoPR = ResolveToggle(overrideWantMovingAwayNoPR, m_settings?.IncludeMovingAwayNoPR, fallback: false);


            // Early-out: nothing selected
            if (!wantCorrupt && !wantHomeless && !wantCommuters && !wantMovingAwayNoPR)
                return candidates;

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

                    // Quick HH-level skip optimization: if nothing in this HH could match
                    // and the independent Moving-Away rule is OFF, skip members.
                    bool isResidentCorrupt = !hasPropertyRenter && !isHomelessHH && !isCommuterHH && !isTouristHH;
                    bool householdMatchesAny =
                        (wantHomeless && isHomelessHH) ||
                        (wantCommuters && isCommuterHH) ||
                        (wantCorrupt && isResidentCorrupt);

                    if (!householdMatchesAny && !wantMovingAwayNoPR)
                        continue;

                    // Iterate members and apply per-citizen rules (via shared classifier)
                    var householdCitizens = EntityManager.GetBuffer<HouseholdCitizen>(householdEntity);
                    for (int j = 0; j < householdCitizens.Length; j++)
                    {
                        var citizenEntity = householdCitizens[j].m_Citizen;
                        if (!EntityManager.Exists(citizenEntity)) continue;
                        if (EntityManager.HasComponent<Deleted>(citizenEntity)) continue;

                        var reason = ClassifyCitizenForDeletion(
                            wantCorrupt, wantHomeless, wantCommuters, wantMovingAwayNoPR,
                            isHomelessHH, isCommuterHH, isTouristHH, hasPropertyRenter,
                            citizenEntity);

                        if (reason != CleanupType.None)
                        {
                            // mark for deletion; only update tallies when a real cleanup is run
                            candidates.Add(citizenEntity); 
                            if (tally) m_lastCounts.BumpCount(reason);
                        }
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
        /// Count citizens to clean based on toggles:
        /// IncludeCorrupt, IncludeHomeless, IncludeCommuters.
        /// </summary>
        private int GetCitizensToCleanCount()
        {
            using var candidates = GetDeletionCandidates(Allocator.TempJob, tally: false);
            return candidates.Length;
        }
        #endregion

        #region Helpers

        // Resolves boolean: precedence: override → UI setting → fallback default.
        // Example: ResolveToggle(forced:true,  setting:false, fallback:false) => true
        //          ResolveToggle(forced:null,  setting:true,  fallback:false) => true
        //          ResolveToggle(forced:null,  setting:null,  fallback:true)  => true
        private static bool ResolveToggle(bool? overrideValue, bool? settingValue, bool fallback)
        {
            return overrideValue ?? (settingValue ?? fallback);
        }

        // Formats an Entity as "Index:Version" for Scene Explorer cross-checks and logs.
        private static string FormatIndexVersion(Entity e) => $"{e.Index}:{e.Version}";


        // Returns true if the citizen is in a Moving-Away state.
        // Checks the tag component first; falls back to TravelPurpose if present.
        private bool IsMovingAway(Entity citizenEntity)
        {
            // Primary
            if (EntityManager.HasComponent<MovingAway>(citizenEntity))
                return true;

            // Fallback 
            if (EntityManager.HasComponent<TravelPurpose>(citizenEntity))
            {
                var tp = EntityManager.GetComponentData<TravelPurpose>(citizenEntity);
                if (tp.m_Purpose == Purpose.MovingAway)
                    return true;
            }

            return false;
        }

        private CleanupType ClassifyCitizenForDeletion(
            bool wantCorrupt,
            bool wantHomeless,
            bool wantCommuters,
            bool wantMovingAwayNoPR,
            bool isHomelessHH,
            bool isCommuterHH,
            bool isTouristHH,
            bool hasPropertyRenter,
            Entity citizenEntity)
        {
            bool movingAway = IsMovingAway(citizenEntity);

            // Precedence: Homeless → Commuters → Corrupt, then independent Moving-Away (no PR)
            if (wantHomeless && isHomelessHH) return CleanupType.Homeless;
            if (wantCommuters && isCommuterHH) return CleanupType.Commuters;

            if (wantCorrupt && !hasPropertyRenter && !isHomelessHH && !isCommuterHH && !isTouristHH)
            {
                // Skip corrupt if the person is Moving-Away
                if (!movingAway) return CleanupType.Corrupt;
            }

            if (wantMovingAwayNoPR && movingAway && !hasPropertyRenter) return CleanupType.MovingAway;

            return CleanupType.None;
        }

        #endregion

        #region Chunked Cleanup Workflow
        /// <summary>
        /// Starts the chunked cleanup process
        /// </summary>
        private void StartChunkedCleanup()
        {
            // reset tallies/samples for this run
            m_lastCounts = default;

            // Build the base set according to toggles:
            // Corrupt residents (if IncludeCorrupt) + Homeless (if IncludeHomeless) + Commuters (if IncludeCommuters)
            m_entitiesToCleanup = GetDeletionCandidates(Allocator.Persistent);


            // reset throttles at start of each run
            m_lastProgressNotified = -1f;    // UI throttle
            m_cleanupIndex = 0;

            // immediately finish when nothing to do
            if (m_entitiesToCleanup.Length == 0)
            {
                s_log.Info("Cleanup requested, but nothing matched the selected filters.");
                if (m_entitiesToCleanup.IsCreated) m_entitiesToCleanup.Dispose();
                m_isChunkedCleanupInProgress = false;

                OnCleanupNoWork?.Invoke();
                return;
            }

            m_isChunkedCleanupInProgress = true;
            OnCleanupProgress?.Invoke(0f);     // initial “0%” so UI updates next frame
            s_log.Info($"Starting chunked cleanup of {m_entitiesToCleanup.Length} citizens in chunks of {CLEANUP_CHUNK_SIZE}");
            s_log.Info("Scan complete — starting mark phase…");
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

            // Mark a chunk this frame
            int remainingEntities = m_entitiesToCleanup.Length - m_cleanupIndex;
            int chunkSize = math.min(CLEANUP_CHUNK_SIZE, remainingEntities);
            
            var chunk = m_entitiesToCleanup.AsArray().GetSubArray(m_cleanupIndex, chunkSize);
            EntityManager.AddComponent<Deleted>(chunk);
            
            m_cleanupIndex += chunkSize;

            // --- UI progress throttled ~5% ---
            float progress = (float)m_cleanupIndex / m_entitiesToCleanup.Length;
            if (progress >= 0.999f || progress - m_lastProgressNotified >= 0.05f)
            {
                m_lastProgressNotified = progress;
                OnCleanupProgress?.Invoke(progress);
            }

        }

        /// <summary>
        /// Finishes the chunked cleanup process
        /// </summary>
        private void FinishChunkedCleanup()
        {
            // Capture count before disposing
            int totalMarked = 0;
            if (m_entitiesToCleanup.IsCreated)
            {
                totalMarked = m_entitiesToCleanup.Length;
                m_entitiesToCleanup.Dispose();
                m_entitiesToCleanup = default;    // clear handle after dispose
            }

            m_isChunkedCleanupInProgress = false;

            // Final UI snap to 100%
            if (m_lastProgressNotified < 0.999f)
            {
                OnCleanupProgress?.Invoke(1f);
            }

         
            s_log.Info($"Marked {totalMarked} entities for deletion " +
                $"(Corrupt:{m_lastCounts.Corrupt}, Homeless:{m_lastCounts.Homeless}, " +
                $"Commuters:{m_lastCounts.Commuters}, Moving-Away:{m_lastCounts.MovingAway}).");

            // Notify settings that cleanup is complete
            OnCleanupCompleted?.Invoke();

            // Reset state for next run
            m_cleanupIndex = 0;
            m_lastProgressNotified = -1f;    // UI throttle reset
            m_lastCounts = default;

            s_log.Info("Cleanup complete.");   // closing line
        }
        #endregion

        #region Debug helpers
        // --- Debug helpers (preview only, no delete) ---
        public void LogCorruptPreviewToLog(int max)
        {
            if (max <= 0) return;

            // Reuse the same traversal, force Corrupt=true and others=false; no state changes.
            using var candidates = GetDeletionCandidates(
                Allocator.TempJob,
                tally: false,
                overrideWantCorrupt: true,
                overrideWantHomeless: false,
                overrideWantCommuters: false,
                overrideWantMovingAwayNoPR: false);

            int count = math.min(max, candidates.Length);
            if (count <= 0)
            {
                s_log.Info("[Preview] No Corrupt citizens found with the current city data.");
                return;
            }

            var sb = new StringBuilder();
            for (int i = 0; i < count; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append("Corrupt ").Append(FormatIndexVersion(candidates[i]));
            }

            s_log.Info($"[Preview] {sb}");
        }

        #endregion

=======
>>>>>>> ebe24aa (Refactor: split CitizenCleanupSystem into Scan (read-only) and Apply (write-side); no behavior change)
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
