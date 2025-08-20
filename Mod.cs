using System.Reflection;    // for Assembly, AssemblyTitleAttribute
using Colossal.IO.AssetDatabase;
using Colossal.Logging;
using Game;
using Game.Modding;
using Game.SceneFlow;
using Colossal.Localization;

namespace CitizenEntityCleaner
{
    public class Mod : IMod
    {
        public static string Author = "phillycheese";
        public static string Name = Assembly
            .GetExecutingAssembly()
            .GetCustomAttribute<AssemblyTitleAttribute>()?.Title
            ?? "Citizen Entity Cleaner";    // spaced fallback title
        public static string Version = Assembly
            .GetExecutingAssembly()
            .GetName()
            .Version.ToString(3);
            
        public static ILog log = LogManager
            .GetLogger($"{nameof(CitizenEntityCleaner)}.{nameof(Mod)}")
            .SetShowsErrorsInUI(false);
            
        private Setting m_Setting;
        private LocaleEN m_Locale;    // keep a reference to locale source to unregister later
        
        // Reference to our cleanup system
        public static CitizenCleanupSystem CleanupSystem { get; private set; }


        // --- fields ---
        private static bool s_bannerLogged;    // static guard to avoid duplicates
        private System.Action _onNoWork;   // <-- for new event if nothing to clean.

        // Keep the same delegate instance and use for both += and -= so -= works (ensures unsubscribe works).
        private System.Action<float> _onProgress;
        private System.Action _onCompleted;
        
        
        public void OnLoad(UpdateSystem updateSystem)
        {
            log.Info(nameof(OnLoad));
           #if DEBUG
            const string BuildTag = " - DEV";
            #else
            const string BuildTag = "";
            #endif

            if (!s_bannerLogged)    // static guard to avoid duplicates on hot reload
            {
            log.Info($"Mod: {Name} v{Version}{BuildTag} by {Author}");    // add info banner at the top of log
            s_bannerLogged = true;
            }

            if (GameManager.instance.modManager.TryGetExecutableAsset(this, out var asset))
                log.Info($"Current mod asset at {asset.path}");

            // Settings UI
            m_Setting = new Setting(this);
            m_Setting.RegisterInOptionsUI();

            // Localization source (register & keep reference)
            m_Locale = new LocaleEN(m_Setting);
            GameManager.instance.localizationManager.AddSource("en-US", m_Locale);
            
             // Load saved settings (or defaults on first run)
            AssetDatabase.global.LoadSettings(nameof(CitizenEntityCleaner), m_Setting, new Setting(this));

            // System registration
            // Register our cleanup system to run before the game's deletion system (Modification2)
            updateSystem.UpdateAt<CitizenCleanupSystem>(SystemUpdatePhase.Modification1);
            CleanupSystem = updateSystem.World.GetOrCreateSystemManaged<CitizenCleanupSystem>();
            CleanupSystem.SetSettings(m_Setting);

            // Subscribe in On Load: event handlers stored in _onProgress / _onCompleted so -= works.
            _onProgress  = m_Setting.UpdateCleanupProgress;    // method group
            _onCompleted = m_Setting.FinishCleanupProgress;
            _onNoWork = m_Setting.FinishCleanupNoWork;   // if nothing to clean

            // Set up callbacks for Cleanup progress, completion, or no work.
            CleanupSystem.OnCleanupProgress  += _onProgress;
            CleanupSystem.OnCleanupCompleted += _onCompleted;
            CleanupSystem.OnCleanupNoWork    += _onNoWork;

            log.Info("CitizenCleanupSystem registered");
        }

        public void OnDispose()
        {
            log.Info(nameof(OnDispose));
            
            // Unhook events BEFORE nulling m_Setting
            if (CleanupSystem != null)
            {
                if (_onProgress  != null) CleanupSystem.OnCleanupProgress  -= _onProgress;
                if (_onCompleted != null) CleanupSystem.OnCleanupCompleted -= _onCompleted;
                if (_onNoWork    != null) CleanupSystem.OnCleanupNoWork    -= _onNoWork;  // for nothing to clean
            }
    
            // Unregister localization source
            if (m_Locale != null)
                GameManager.instance.localizationManager.RemoveSource("en-US", m_Locale);

            // Unregister settings UI (settings already persisted via ApplyAndSave in setters)
            if (m_Setting != null)
            {
                m_Setting.UnregisterInOptionsUI();
                m_Setting = null;
            }
            
            // Clean up system reference and callbacks
    
            CleanupSystem = null;
            _onProgress = null;
            _onCompleted = null;
            _onNoWork = null;
            m_Locale = null;
        }

    }
}
