using Colossal.Logging;
using Game.Common;
using Unity.Collections;
using Unity.Entities;


namespace CitizenEntityCleaner
{
    // PART: System — system loop (OnCreate/OnUpdate/OnDestroy), public API, shared fields/events
    /// <summary>
    /// ECS System that handles cleanup of citizen entities when triggered via settings UI
    /// </summary>
    public partial class CitizenCleanupSystem : SystemBase
    {
        private static readonly ILog s_Log = Mod.log;

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
                    case CleanupType.None: /* no-op */         break;
                }
            }
        }



        // ---- fields ----
        private DeletionCounts m_lastCounts;
        private float m_lastProgressNotified = -1f;    // UI progress throttle (~5% steps)
        private bool m_shouldRunCleanup = false;    // Flag to trigger the cleanup operation

        // Chunked cleanup state
        private NativeList<Entity> m_entitiesToCleanup;
        private int m_cleanupIndex = 0;
        private bool m_isChunkedCleanupInProgress = false;

        // Cached query for reuse
        private EntityQuery m_householdMemberQuery;


        private Setting? m_settings; // nullable. Store setting passed from Mod.Onload

        // Callback for when cleanup is in progress and completed
        // event instead of public delegate prevents external code accidental overwrite delegate list
        public event System.Action<float>? OnCleanupProgress;
        public event System.Action? OnCleanupCompleted;
        public event System.Action? OnCleanupNoWork;



        protected override void OnCreate()
        {
            m_householdMemberQuery = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[] { ComponentType.ReadOnly<Game.Citizens.HouseholdMember>() },
                None = new ComponentType[] { ComponentType.ReadOnly<Deleted>() }
            });

            base.OnCreate();

            s_Log.Info("CitizenCleanupSystem created");
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
            s_Log.Info("Starting citizen entity cleanup...");
            StartChunkedCleanup();
        }

        /// <summary>
        /// Sets the settings reference for filtering
        /// </summary>
        public void SetSettings(Setting settings) => m_settings = settings;

        /// <summary>
        /// Triggers the cleanup operation to run on the next update
        /// </summary>
        public void TriggerCleanup()
        {
            s_Log.Info("Cleanup operation triggered from settings");
            m_shouldRunCleanup = true;
        }

        // Returns true if there is at least one HouseholdMember in the world
        public bool HasAnyCitizenData()
        {
            try
            {
                // Quick HH>0 check; query created in OnCreate
                return !m_householdMemberQuery.IsEmptyIgnoreFilter;
            }
            catch
            {
                return false;
            }
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
                s_Log.Warn($"Error getting citizen statistics: {ex.Message}");
                return (0, 0);
            }
        }


        protected override void OnDestroy()
        {
            if (m_entitiesToCleanup.IsCreated)
            {
                m_entitiesToCleanup.Dispose();
            }

            s_Log.Info("CitizenCleanupSystem destroyed");
            base.OnDestroy();
        }
    }
}
