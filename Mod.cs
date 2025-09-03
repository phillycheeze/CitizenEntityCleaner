using Colossal;         // IDictionarySource
using Colossal.IO.AssetDatabase;
using Colossal.Logging;
using Game;                 // UpdateSystem
using Game.Modding;
using Game.SceneFlow;       // GameManager
using System;
using System.Reflection;    // Assembly attributes

namespace CitizenEntityCleaner
{
    public class Mod : IMod
    {
        // ---- Mod metadata ----
        private static readonly Assembly Asm = Assembly.GetExecutingAssembly();
        public static readonly string Name =
                Asm.GetCustomAttribute<AssemblyTitleAttribute>()?.Title ?? "Citizen Entity Cleaner";    // fallback title

        // Versions (short + full)
        private static readonly string VersionInformationalRaw =
            Asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "1.0.0";

        public static readonly string VersionShort = VersionInformationalRaw.Split(' ', '+')[0];
        public static readonly string VersionInformational = VersionInformationalRaw;

        // ---- Logging ----
        public static readonly ILog log = LogManager
            .GetLogger("CitizenEntityCleaner") // log file located in ..\CitySkylines II\logs\CitizenEntityCleaner.log
            .SetShowsErrorsInUI(false);

        // ---- State ----
        private static bool s_bannerLogged;    // static guard to avoid duplicates

        private Setting? m_Setting;     // nullable; assigned in OnLoad
        public static CitizenCleanupSystem? CleanupSystem { get; private set; } // nullable; assigned in OnLoad

        // Keep same delegate instances and use for both += and -= so -= works (ensures unsubscribe works).
        private Action<float>? _onProgress;
        private Action? _onCompleted;
        private Action? _onNoWork;   // <-- for event if nothing to clean, nullable


        public void OnLoad(UpdateSystem updateSystem)
        {
            log.Info(nameof(OnLoad));

            // One time banner static guard (avoid duplicates on hot reload)
            if (!s_bannerLogged)
            {
                log.Info($"Mod: {Name} | Version: {VersionShort} | Info: {VersionInformational}");  // add info banner at the top of log
                s_bannerLogged = true;
            }
            // Asset path for diagnostics
            if (GameManager.instance?.modManager != null &&
                GameManager.instance.modManager.TryGetExecutableAsset(this, out var asset))
            {
                log.Info($"Current mod asset at {asset.path}");
            }
#if DEBUG
            log.Info("[DebugPing] OnLoad reached");
#endif

            // ---- Settings + Locale ----
            m_Setting = new Setting(this);

            // ADD LOCALES HERE
            var en = new LocaleEN(m_Setting);
            var fr = new LocaleFR(m_Setting);
            var es = new LocaleES(m_Setting);
            var de = new LocaleDE(m_Setting);
            var it = new LocaleIT(m_Setting);

            RegisterLocale("en-US", en);
            RegisterLocale("fr-FR", fr);
            RegisterLocale("es-ES", es);
            RegisterLocale("de-DE", de);
            RegisterLocale("it-IT", it);

            // Load saved settings (or defaults on first run)
            AssetDatabase.global.LoadSettings(ModKeys.SettingsKey, m_Setting, new Setting(this));

            // Expose Options UI
            m_Setting.RegisterInOptionsUI();

            // System registration (run before deletion system)
            updateSystem.UpdateAt<CitizenCleanupSystem>(SystemUpdatePhase.Modification1);
            CleanupSystem = updateSystem.World.GetOrCreateSystemManaged<CitizenCleanupSystem>();
            CleanupSystem.SetSettings(m_Setting);

            // Progress / completion callbacks
            _onProgress = m_Setting.UpdateCleanupProgress;
            _onCompleted = m_Setting.FinishCleanupProgress;
            _onNoWork = m_Setting.FinishCleanupNoWork;

            // Set up callbacks for Cleanup progress, completion, or no work.
            CleanupSystem.OnCleanupProgress += _onProgress;
            CleanupSystem.OnCleanupCompleted += _onCompleted;
            CleanupSystem.OnCleanupNoWork += _onNoWork;

            log.Info("CitizenCleanupSystem registered");
        }

        public void OnDispose()
        {
            try
            {
                log.Info(nameof(OnDispose));

                // Unregister Options UI
                if (m_Setting != null)
                {
                    try { m_Setting.UnregisterInOptionsUI(); }
                    catch (Exception ex) { log.Warn($"[UI] UnregisterInOptionsUI failed: {ex.GetType().Name}: {ex.Message}"); }
                }

                // Unsubscribe events
                var cs = CleanupSystem;
                if (cs != null)
                {
                    try { if (_onProgress != null) cs.OnCleanupProgress -= _onProgress; }
                    catch (Exception ex) { log.Warn($"[Events] OnCleanupProgress -= failed: {ex.GetType().Name}: {ex.Message}"); }

                    try { if (_onCompleted != null) cs.OnCleanupCompleted -= _onCompleted; }
                    catch (Exception ex) { log.Warn($"[Events] OnCleanupCompleted -= failed: {ex.GetType().Name}: {ex.Message}"); }

                    try { if (_onNoWork != null) cs.OnCleanupNoWork -= _onNoWork; }
                    catch (Exception ex) { log.Warn($"[Events] OnCleanupNoWork -= failed: {ex.GetType().Name}: {ex.Message}"); }
                }

            }
            catch (Exception ex)
            {
                // Last-resort guard
                log.Error($"OnDispose fatal: {ex.GetType().Name}: {ex.Message}");
#if DEBUG
                log.Debug(ex.ToString());
#endif
            }
            finally
            {
                // Clear references
                _onProgress = null;
                _onCompleted = null;
                _onNoWork = null;

                CleanupSystem = null;
                m_Setting = null;
            }
        }

        // ---- Helpers ----
        private void RegisterLocale(string localeId, IDictionarySource source)
        {
            var lm = GameManager.instance?.localizationManager;
            if (lm == null)
            {
                log.Debug("[Locale] No localization manager; skip " + localeId);
                return;
            }
            if (source == null)
            {
                log.Debug("[Locale] Null source; skip " + localeId);
                return;
            }

            try {
                lm.AddSource(localeId, source);
                log.Info($"[Locale] Registered {localeId}");
            } catch (Exception ex) {
                log.Warn($"[Locale] AddSource failed for {localeId}: {ex.GetType().Name}: {ex.Message}");
            }
        }

    }
}
