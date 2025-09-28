using Game.Common; // for Deleted
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace CitizenEntityCleaner
{
    // PART: Apply (write-side) — starts chunked runs, mark chunks, throttle progress, log, signal completion
    public partial class CitizenCleanupSystem
    {

        private const int CLEANUP_CHUNK_SIZE = 2000;    // how many entities to mark per frame

        /// <summary>
        /// Starts chunked cleanup process
        /// </summary>
        private void StartChunkedCleanup()
        {
            // reset tallies for this run
            m_lastCounts = default;

            // Build base set according to toggles
            m_entitiesToCleanup = GetDeletionCandidates(Allocator.Persistent, tally: true);

            // reset throttles at start of each run
            m_lastProgressNotified = -1f;    // UI throttle
            m_cleanupIndex = 0;

            // immediately finish when nothing to do
            if (m_entitiesToCleanup.Length == 0)
            {
                s_Log.Info("Cleanup requested, but there is nothing to clean.");
                if (m_entitiesToCleanup.IsCreated) m_entitiesToCleanup.Dispose();
                m_isChunkedCleanupInProgress = false;

                OnCleanupProgress?.Invoke(1f);    // optional: send final progress of 100%
                OnCleanupNoWork?.Invoke();
                return;
            }

            m_isChunkedCleanupInProgress = true;
            OnCleanupProgress?.Invoke(0f);  // initial 0% so UI updates next frame
            s_Log.Info($"Starting chunked cleanup of {m_entitiesToCleanup.Length} citizens in chunks of {CLEANUP_CHUNK_SIZE}");
            s_Log.Info("Scan complete — starting mark phase…");
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

            // Reset state for next run
            m_cleanupIndex = 0;
            m_lastProgressNotified = -1f;    // UI throttle reset

            s_Log.Info($"Marked {totalMarked} entities for deletion " +
                       $"(Corrupt:{m_lastCounts.Corrupt}, Homeless:{m_lastCounts.Homeless}, " +
                       $"Commuters:{m_lastCounts.Commuters}, Moving-Away:{m_lastCounts.MovingAway}).");

            // Notify settings that cleanup is complete
            OnCleanupCompleted?.Invoke();
            s_Log.Info("Cleanup complete.");   // closing line
        }
    }
}
