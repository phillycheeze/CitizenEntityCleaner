using Game.Common; // for Deleted

using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace CitizenEntityCleaner
{
    // PART: Apply (write-side) — starts chunked runs, mark chunks, throttle progress, log, signal completion
    public partial class CitizenCleanupSystem
    {
        /// <summary>
        /// Starts chunked cleanup process
        /// </summary>
        private void StartChunkedCleanup()
        {
            m_entitiesToCleanup = GetCorruptedCitizenEntities(Allocator.Persistent);

            // reset throttles at start of each run
            m_lastProgressNotified = -1f;    // UI throttle
            m_lastLoggedBucket = -1;         // Log throttle
            m_cleanupIndex = 0;

            // immediately finish when nothing to do
            if (m_entitiesToCleanup.Length == 0)
            {
                s_log.Info("Cleanup requested, but there is nothing to clean.");
                if (m_entitiesToCleanup.IsCreated) m_entitiesToCleanup.Dispose();
                m_isChunkedCleanupInProgress = false;

                OnCleanupProgress?.Invoke(1f);    // optional: send final progress of 100%
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
            int bucket = percent / LOG_BUCKET_PERCENT;

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
                s_log.Info($"Entity cleanup completed. Marked {m_entitiesToCleanup.Length} citizens for deletion.");
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
            m_lastLoggedBucket = -1;         // log throttle reset

            // Notify settings that cleanup is complete
            OnCleanupCompleted?.Invoke();
        }
    }
}
