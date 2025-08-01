using Unity.Entities;
using Unity.Collections;
using Colossal.Logging;

namespace CitizenEntityCleaner
{
    /// <summary>
    /// ECS System that handles cleanup of citizen entities when triggered via settings UI
    /// </summary>
    public partial class CitizenCleanupSystem : SystemBase
    {
        private static ILog s_log = LogManager.GetLogger($"{nameof(CitizenEntityCleaner)}.{nameof(CitizenCleanupSystem)}");
        
        // Flag to trigger the cleanup operation
        private bool m_shouldRunCleanup = false;
        
        protected override void OnCreate()
        {
            base.OnCreate();
            s_log.Info("CitizenCleanupSystem created");
        }

        protected override void OnUpdate()
        {
            // Only run the cleanup when explicitly triggered
            if (!m_shouldRunCleanup)
                return;

            m_shouldRunCleanup = false;
            
            s_log.Info("Starting citizen entity cleanup...");
            
            // Your entity deletion query will go here
            RunEntityCleanup();
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
        /// This is where you'll put your existing entity deletion query code
        /// </summary>
        private void RunEntityCleanup()
        {
            // TODO: Replace this with your actual entity deletion query
            // Example structure for entity deletion:
            
            /*
            // Example: Query for entities you want to delete
            var query = GetEntityQuery(ComponentType.ReadOnly<YourComponentType>());
            var entities = query.ToEntityArray(Allocator.TempJob);
            
            s_log.Info($"Found {entities.Length} entities to delete");
            
            // Delete the entities
            EntityManager.DestroyEntity(entities);
            
            entities.Dispose();
            */
            
            s_log.Info("Entity cleanup completed (placeholder - add your query here)");
        }

        protected override void OnDestroy()
        {
            s_log.Info("CitizenCleanupSystem destroyed");
            base.OnDestroy();
        }
    }
} 