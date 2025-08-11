using Colossal.IO.AssetDatabase;
using Colossal.Logging;
using Game;
using Game.Modding;
using Game.SceneFlow;

namespace CitizenEntityCleaner
{
    public class Mod : IMod
    {
        public static ILog log = LogManager.GetLogger($"{nameof(CitizenEntityCleaner)}.{nameof(Mod)}").SetShowsErrorsInUI(false);
        private Setting m_Setting;
        
        // Reference to our cleanup system
        public static CitizenCleanupSystem CleanupSystem { get; private set; }
        public static TreeCullingPreprocessor TreeSystem {  get; private set; }

        public void OnLoad(UpdateSystem updateSystem)
        {
            log.Info(nameof(OnLoad));

            if (GameManager.instance.modManager.TryGetExecutableAsset(this, out var asset))
                log.Info($"Current mod asset at {asset.path}");

            m_Setting = new Setting(this);
            m_Setting.RegisterInOptionsUI();
            GameManager.instance.localizationManager.AddSource("en-US", new LocaleEN(m_Setting));
            AssetDatabase.global.LoadSettings(nameof(CitizenEntityCleaner), m_Setting, new Setting(this));

            OptimizedTreeCullingPatch.ApplyPatches();
            updateSystem.UpdateAt<TreeCullingPreprocessor>(SystemUpdatePhase.Rendering);
            TreeSystem = updateSystem.World.GetOrCreateSystemManaged<TreeCullingPreprocessor>();

            // Register our cleanup system to run before the game's deletion system (Modification2)
            updateSystem.UpdateAt<CitizenCleanupSystem>(SystemUpdatePhase.Modification1);
            CleanupSystem = updateSystem.World.GetOrCreateSystemManaged<CitizenCleanupSystem>();
            CleanupSystem.SetSettings(m_Setting);
            
            // Set up callbacks for cleanup progress and completion
            CleanupSystem.OnCleanupProgress += (progress) => m_Setting.UpdateCleanupProgress(progress);
            CleanupSystem.OnCleanupCompleted += () => m_Setting.FinishCleanupProgress();
            
            log.Info("CitizenCleanupSystem registered");
        }

        public void OnDispose()
        {
            log.Info(nameof(OnDispose));

            OptimizedTreeCullingPatch.RemovePatches();

            if (m_Setting != null)
            {
                m_Setting.UnregisterInOptionsUI();
                m_Setting = null;
            }
            
            // Clean up system reference and callbacks
            if (CleanupSystem != null)
            {
                CleanupSystem.OnCleanupProgress -= (progress) => m_Setting.UpdateCleanupProgress(progress);
                CleanupSystem.OnCleanupCompleted -= () => m_Setting.FinishCleanupProgress();
            }
            CleanupSystem = null;
        }


    }
}
