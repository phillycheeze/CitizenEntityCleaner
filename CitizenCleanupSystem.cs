using Colossal.Logging;
using Game.Citizens;        // for Citizen, HouseholdMember, TravelPurpose
using Game.Common;          // for Deleted
using Unity.Collections;    // for NativeList
using Unity.Entities;       // Entity, SystemBase, ComponentType, EntityQuery, etc.


namespace CitizenEntityCleaner
{
    /// <summary>
    /// ECS System for cleanup of citizen entities triggered via UI
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

        // Chunked cleanup state
        private NativeList<Entity> m_entitiesToCleanup;
        private int m_cleanupIndex = 0;
        private bool m_isChunkedCleanupInProgress = false;

        // Cached query for reuse
        private EntityQuery m_householdMemberQuery;

        // settings
        private Setting? m_settings;
        #endregion

        #region Events
        // Callback for when cleanup is in progress and completed
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
            s_Log.Debug("[Cleanup] trigger request (from Settings UI)");
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
                int citizensToClean = GetCitizensToCleanCount();

                return (totalCitizens, citizensToClean);
            }
            catch (System.Exception ex)
            {
                s_Log.Warn($"Error getting citizen statistics: {ex.Message}");
                return (0, 0);
            }
        }

        public bool HasAnyCitizenData()
        {
            try
            {
                // Cheap test for City Loaded: true when there is at least one household member
                return !m_householdMemberQuery.IsEmptyIgnoreFilter;
            }
            catch
            {
                return false;
            }
        }
        #endregion

        protected override void OnDestroy()
        {
            if (m_entitiesToCleanup.IsCreated)
            {
                m_entitiesToCleanup.Dispose();
            }
            base.OnDestroy();

            s_Log.Info("CitizenCleanupSystem destroyed");

        }
    }
}
