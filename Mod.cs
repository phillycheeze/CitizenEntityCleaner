using System.Reflection;    // for Assembly attributes
using Colossal.IO.AssetDatabase;
using Colossal.Logging;
using Game;
using Game.Modding;
using Game.SceneFlow;

namespace CitizenEntityCleaner
{
    public class Mod : IMod
    {
        // Mod information
        private static readonly Assembly Asm = Assembly.GetExecutingAssembly();

        public static readonly string Name =
                Asm.GetCustomAttribute<AssemblyTitleAttribute>()?.Title
            ?? "Citizen Entity Cleaner";    // fallback title

        public static readonly string Version =
            Asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? "1.0.0";    // fallback to 1.0.0

        public static readonly string Author =
            Asm.GetCustomAttribute<AssemblyCompanyAttribute>()?.Company
            ?? "phillycheese";    // fallback author

        public static ILog log = LogManager
            .GetLogger($"{nameof(CitizenEntityCleaner)}.{nameof(Mod)}")
            .SetShowsErrorsInUI(false);
            
        private Setting m_Setting;
        private LocaleEN m_Locale;    // keep a reference to locale source to unregister later
        
        // Reference to the cleanup system
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

            // One time banner static guard to avoid duplicates on hot reload
            if (!s_bannerLogged)    
            {
                log.Info($"Mod: {Name} v{Version} by {Author}");    // add info banner at the top of log
                s_bannerLogged = true;
            }

            if (GameManager.instance.modManager.TryGetExecutableAsset(this, out var asset))
                log.Info($"Current mod asset at {asset.path}");

            // Settings + Locale
            m_Setting = new Setting(this);
            m_Locale = new LocaleEN(m_Setting); // Register & keep reference
            GameManager.instance.localizationManager.AddSource("en-US", m_Locale);

            // Load saved settings (or defaults on first run)
            AssetDatabase.global.LoadSettings(ModKeys.SettingsKey, m_Setting, new Setting(this));

            m_Setting.RegisterInOptionsUI();

            // System registration
            // Register the cleanup system to run before the game's deletion system (Modification2)
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
