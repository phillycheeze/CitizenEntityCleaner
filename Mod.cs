using Colossal.IO.AssetDatabase;
using Colossal.Logging;
using Game;
using Game.Modding;
using Game.SceneFlow;
using HarmonyLib;

namespace CitizenEntityCleaner
{
    public class Mod : IMod
    {
        public static ILog log = LogManager.GetLogger($"{nameof(CitizenEntityCleaner)}.{nameof(Mod)}").SetShowsErrorsInUI(false);
        private Setting m_Setting;
        private Harmony m_Harmony;
        
        // Reference to our cleanup system
        public static CitizenCleanupSystem CleanupSystem { get; private set; }
        public void OnLoad(UpdateSystem updateSystem)
        {
            log.Info(nameof(OnLoad));

            if (GameManager.instance.modManager.TryGetExecutableAsset(this, out var asset))
                log.Info($"Current mod asset at {asset.path}");

            m_Setting = new Setting(this);
            m_Setting.RegisterInOptionsUI();
            GameManager.instance.localizationManager.AddSource("en-US", new LocaleEN(m_Setting));
            AssetDatabase.global.LoadSettings(nameof(CitizenEntityCleaner), m_Setting, new Setting(this));

            m_Harmony = new Harmony($"{nameof(CitizenEntityCleaner)}.{nameof(Mod)}");
            m_Harmony.PatchAll();

            // Register our cleanup system to run before the game's deletion system (Modification2)
            updateSystem.UpdateAt<CitizenCleanupSystem>(SystemUpdatePhase.Modification1);
            CleanupSystem = updateSystem.World.GetOrCreateSystemManaged<CitizenCleanupSystem>();
            CleanupSystem.SetSettings(m_Setting);
            
            updateSystem.UpdateAt<OcclusionCullingSystem>(SystemUpdatePhase.PreCulling);
            var occlusionSystem = updateSystem.World.GetOrCreateSystemManaged<OcclusionCullingSystem>();
            
            // Set up callbacks for cleanup progress and completion
            CleanupSystem.OnCleanupProgress += (progress) => m_Setting.UpdateCleanupProgress(progress);
            CleanupSystem.OnCleanupCompleted += () => m_Setting.FinishCleanupProgress();
            
            log.Info("CitizenCleanupSystem registered");
        }

        public void OnDispose()
        {
            log.Info(nameof(OnDispose));

            m_Harmony.UnpatchAll();
            m_Harmony = null;

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
