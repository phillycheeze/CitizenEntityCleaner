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
        private static readonly ILog s_log = Mod.log;

        // ---- constants ----
        private const int CLEANUP_CHUNK_SIZE = 2000;    // how many entities to mark per frame
        private const Game.Creatures.HumanFlags HomelessFlag = Game.Creatures.HumanFlags.Homeless;  // explicit enum (was a magic number)
        private const int LOG_BUCKET_PERCENT = 10;    // log once per N% (change to 5 for more frequent 5% logs)

        // ---- fields ----
        private float m_lastProgressNotified = -1f;    // UI progress throttle (~5% steps)
        private int m_lastLoggedBucket = -1;   // -1 means "nothing logged yet" for log throttle state

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

        // Returns true if there is at least one HouseholdMember in the world
        public bool HasAnyCitizenData()
        {
            try
            {
                // Quick HH>0 check; query created in OnCreate
                return !m_householdMemberQuery.IsEmptyIgnoreFilter;
            }
            catch (System.Exception ex)
            {
                s_log.Warn($"HasAnyCitizenData() failed: {ex}");
                return false;
            }
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
                s_log.Warn($"Error getting citizen statistics: {ex}");
                return (0, 0);
            }
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
