// CitzenCleanupSystem.Apply.cs
using Game.Common;          // Deleted
using Unity.Collections;    // Allocator
using Unity.Entities;
using Unity.Mathematics;    // math

namespace CitizenEntityCleaner
{
    // PART: Apply (write-side) — starts chunked runs, mark chunks, throttle progress, signal completion
    public partial class CitizenCleanupSystem : SystemBase
    {
        // ---- constants ----
        private const int CLEANUP_CHUNK_SIZE = 2000;   // entities to mark per frame

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

            // nothing to do, finish
            if (m_entitiesToCleanup.Length == 0)
            {
                s_Log.Info("Cleanup requested, but nothing matched the selected filters.");
                if (m_entitiesToCleanup.IsCreated) m_entitiesToCleanup.Dispose();
                m_isChunkedCleanupInProgress = false;

                OnCleanupNoWork?.Invoke();
                return;
            }

            m_isChunkedCleanupInProgress = true;
            OnCleanupProgress?.Invoke(0f);     // initial “0%” so UI updates next frame
            s_Log.Info($"Scan complete: {m_entitiesToCleanup.Length} citizens. Marking in chunks of {CLEANUP_CHUNK_SIZE}");
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
            var remainingEntities = m_entitiesToCleanup.Length - m_cleanupIndex;
            var chunkSize = math.min(CLEANUP_CHUNK_SIZE, remainingEntities);

            NativeArray<Entity> chunk = m_entitiesToCleanup.AsArray().GetSubArray(m_cleanupIndex, chunkSize);
            EntityManager.AddComponent<Deleted>(chunk);

            m_cleanupIndex += chunkSize;

            // --- UI progress throttled ~5% ---
            var progress = (float)m_cleanupIndex / m_entitiesToCleanup.Length;
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
            var totalMarked = 0;
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

            s_Log.Info($"Marked {totalMarked} entities for deletion " +
                $"(Corrupt:{m_lastCounts.Corrupt}, Homeless:{m_lastCounts.Homeless}, " +
                $"Commuters:{m_lastCounts.Commuters}, Moving-Away:{m_lastCounts.MovingAway}).");

            // Notify settings that cleanup is complete
            OnCleanupCompleted?.Invoke();

            // Reset state for next run
            m_cleanupIndex = 0;
            m_lastProgressNotified = -1f;    // UI throttle reset
            m_lastCounts = default;

            s_Log.Info("Cleanup complete.");   // closing line
        }
        #endregion
    }
}
