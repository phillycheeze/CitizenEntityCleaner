// Mod.cs
using System;
using System.Reflection;    // only for Assembly metadata
using Colossal;             // IDictionarySource
using Colossal.IO.AssetDatabase;
using Colossal.Localization;    // LocalizationManager
using Colossal.Logging;
using Colossal.PSI.Environment;  // EnvPath.
using Game;                     // UpdateSystem
using Game.Modding;
using Game.SceneFlow;           // GameManager

namespace CitizenEntityCleaner
{
    public class Mod : IMod
    {
        // ---- Mod metadata ----
        private static readonly Assembly s_asm = Assembly.GetExecutingAssembly();
        public static readonly string Name =
                s_asm.GetCustomAttribute<AssemblyTitleAttribute>()?.Title ?? "Citizen Cleaner";    // fallback title

        // Versions (short + full)
        private static readonly string s_versionInformationalRaw =
            s_asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "1.0.0";

        public static readonly string VersionShort = s_versionInformationalRaw.Split(' ', '+')[0];
        public static readonly string VersionInformational = s_versionInformationalRaw;

        // ---- Logging ----
        private const string kLogId = "CitizenEntityCleaner"; // single source for logger + filename
        public static readonly ILog log = LogManager
            .GetLogger(kLogId)              // log file in \CitySkylines II\logs\CitizenEntityCleaner.log
            .SetShowsErrorsInUI(false);

        // Public helper for the absolute log path (used by Settings)
        public static string LogFilePath => $"{EnvPath.kUserDataPath}/Logs/{kLogId}.log";


        // ---- State ----
        private static bool s_bannerLogged;    // static guard to avoid duplicates

        private Setting? m_Setting;     // nullable; assigned in OnLoad
        public static CitizenCleanupSystem? CleanupSystem { get; private set; } // nullable; assigned in OnLoad

        // Keep same delegate instances and use for both += and -= so -= works (ensures unsubscribe works).
        private Action<float>? _onProgress;
        private Action? _onCompleted;
        private Action? _onNoWork;   // for event if nothing to clean, nullable

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
                GameManager.instance.modManager.TryGetExecutableAsset(this, out ExecutableAsset? asset))
            {
#if DEBUG
                log.Info($"Current mod asset at {asset.path}");
#endif
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
            var zhCN = new LocaleZH_CN(m_Setting); // Simplified Chinese, not fully translated
            var ja = new LocaleJA(m_Setting);
            var ko = new LocaleKO(m_Setting);
            var vi = new LocaleVI(m_Setting);
            var ptBR = new LocalePT_BR(m_Setting);


            RegisterLocale("en-US", en);
            RegisterLocale("fr-FR", fr);
            RegisterLocale("es-ES", es);
            RegisterLocale("de-DE", de);
            RegisterLocale("it-IT", it);
            RegisterLocale("ja-JP", ja);
            RegisterLocale("ko-KR", ko);
            RegisterLocale("vi-VN", vi);
            RegisterLocale("pt-BR", ptBR);
            RegisterLocale("zh-HANS", zhCN);    // log shows this is used

            // Register ZH under several common ids so LocalizationManager can find matching one
            RegisterLocale("zh-CN", zhCN);      // fallback
            RegisterLocale("zh", zhCN);         // fallback

            RegisterLocale("pt", ptBR); // fallback if the game reports just "pt"

            // Log language selected (guarded for null)
            LocalizationManager? lm = GameManager.instance?.localizationManager;
            if (lm != null)
            {
                Mod.log.Info($"[Locale] ACTIVE at LOAD: {lm.activeLocaleId}");  // One-time info at load
#if DEBUG
            // Debug-only: track locale changes for testing
            lm.onActiveDictionaryChanged += () => Mod.log.Info($"[Locale] Active changed -> {lm.activeLocaleId}");
#endif
            }

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
                CitizenCleanupSystem? cs = CleanupSystem;
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
            LocalizationManager? lm = GameManager.instance?.localizationManager;
            if (lm == null)
            {
#if DEBUG
                log.Debug("[Locale] No localization manager; skip " + localeId);
#endif
                return;
            }
            if (source == null)
            {
#if DEBUG
                log.Debug("[Locale] Null source; skip " + localeId);
#endif
                return;
            }

            try
            {
                lm.AddSource(localeId, source);
#if DEBUG
                log.Info($"[Locale] Registered {localeId}");
#endif
            }
            catch (Exception ex)
            {
                log.Warn($"[Locale] AddSource failed for {localeId}: {ex.GetType().Name}: {ex.Message}");
            }
        }

    }
}
