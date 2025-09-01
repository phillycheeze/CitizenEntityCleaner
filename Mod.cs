using Colossal.IO.AssetDatabase;
using Colossal.Logging;
using Game;
using Game.Modding;
using Game.SceneFlow;
using System;
using System.Reflection;    // for Assembly attributes

namespace CitizenEntityCleaner
{
    public class Mod : IMod
    {
        // Mod information
        private static readonly Assembly Asm = Assembly.GetExecutingAssembly();

        public static readonly string Name =
                Asm.GetCustomAttribute<AssemblyTitleAttribute>()?.Title
            ?? "Citizen Entity Cleaner";    // fallback title

        // Versions (short + full)
        private static readonly string VersionInformationalRaw =
            Asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? "1.0.0";

        public static readonly string VersionShort = VersionInformationalRaw.Split(' ', '+')[0];
        public static readonly string VersionInformational = VersionInformationalRaw;


        public static readonly ILog log = LogManager
            .GetLogger("CitizenEntityCleaner") // log file located in ..\CitySkylines II\logs\CitizenEntityCleaner.log
            .SetShowsErrorsInUI(false);

        private Setting? m_Setting;     // nullable; assigned in OnLoad
        private LocaleEN? m_Locale;    // keep a reference to locale source to unregister later

        // Reference to the cleanup system
        public static CitizenCleanupSystem? CleanupSystem { get; private set; } // nullable; assigned in OnLoad

        // --- fields ---
        private static bool s_bannerLogged;    // static guard to avoid duplicates
        private bool m_LocaleRegistered;

        // Keep same delegate instances and use for both += and -= so -= works (ensures unsubscribe works).
        private System.Action<float>? _onProgress;
        private System.Action? _onCompleted;
        private System.Action? _onNoWork;   // <-- for new event if nothing to clean, nullable


        public void OnLoad(UpdateSystem updateSystem)
        {
            log.Info(nameof(OnLoad));

            // One time banner static guard (avoid duplicates on hot reload)
            if (!s_bannerLogged)
            {
                log.Info($"Mod: {Name} | Version: {VersionShort} | Info: {VersionInformational}");  // add info banner at the top of log
                s_bannerLogged = true;
            }

            if (GameManager.instance.modManager.TryGetExecutableAsset(this, out var asset))
                log.Info($"Current mod asset at {asset.path}");

            Mod.log.Info("[DebugPing] OnLoad reached");

            // Settings + Locale
            m_Setting = new Setting(this);
            m_Locale = new LocaleEN(m_Setting); // keep reference

            // Run the locale self-test in DEBUG builds (logs to CitizenEntityCleaner.log)
#if DEBUG
            LocaleSelfTest.ValidateRequiredEntries(m_Locale!, m_Setting!, Mod.log);
#endif
            // Register locale source
            GameManager.instance.localizationManager.AddSource("en-US", m_Locale);
            m_LocaleRegistered = true;

            // Load saved settings (or defaults on first run)
            AssetDatabase.global.LoadSettings(ModKeys.SettingsKey, m_Setting, new Setting(this));

            // Expose Options UI
            m_Setting.RegisterInOptionsUI();

            // System registration, callbacks (run before deletion system)
            updateSystem.UpdateAt<CitizenCleanupSystem>(SystemUpdatePhase.Modification1);
            CleanupSystem = updateSystem.World.GetOrCreateSystemManaged<CitizenCleanupSystem>();
            CleanupSystem.SetSettings(m_Setting);

            // Wire up progress / completion callbacks
            _onProgress = m_Setting.UpdateCleanupProgress;
            _onCompleted = m_Setting.FinishCleanupProgress;
            _onNoWork = m_Setting.FinishCleanupNoWork;

            // Set up callbacks for Cleanup progress, completion, or no work.
            CleanupSystem.OnCleanupProgress += _onProgress;
            CleanupSystem.OnCleanupCompleted += _onCompleted;
            CleanupSystem.OnCleanupNoWork += _onNoWork;

            log.Info("CitizenCleanupSystem registered");
        }

        // Hardened OnDispose with try/catch guards for common NREs
        // Ensures all resources are cleaned up and events unsubscribed
        public void OnDispose()
        {
            try
            {
                log.Info(nameof(OnDispose));

                // 1) Unsubscribe events BEFORE nulling references
                if (CleanupSystem != null)
                {
                    try { if (_onProgress != null) CleanupSystem.OnCleanupProgress -= _onProgress; }
                    catch (System.Exception ex) { log.Warn($"[Events] Unsub OnCleanupProgress failed: {ex.GetType().Name}: {ex.Message}"); }

                    try { if (_onCompleted != null) CleanupSystem.OnCleanupCompleted -= _onCompleted; }
                    catch (System.Exception ex) { log.Warn($"[Events] Unsub OnCleanupCompleted failed: {ex.GetType().Name}: {ex.Message}"); }

                    try { if (_onNoWork != null) CleanupSystem.OnCleanupNoWork -= _onNoWork; }
                    catch (System.Exception ex) { log.Warn($"[Events] Unsub OnCleanupNoWork failed: {ex.GetType().Name}: {ex.Message}"); }
                }

                // 2) Unregister Options UI (safe even if UI never opened)
                if (m_Setting != null)
                {
                    try { m_Setting.UnregisterInOptionsUI(); }
                    catch (System.Exception ex) { log.Warn($"[UI] UnregisterInOptionsUI failed: {ex.GetType().Name}: {ex.Message}"); }
                }

                // 3) Remove locale source safely (only if actually added)
                if (m_LocaleRegistered && m_Locale != null && GameManager.instance != null)
                {
                    var lm = GameManager.instance.localizationManager;
                    if (lm != null)
                    {
                        try
                        {
                            lm.RemoveSource("en-US", m_Locale);
                        }
                        catch (System.NullReferenceException ex)
                        {
                            // Common when another mod throws during dictionary-changed notification
                            log.Info("[Locale] RemoveSource ignored (NRE likely from another mod during dictionary change).");
#if DEBUG
                            log.Debug(ex.ToString());
#endif
                        }
                        catch (System.Exception ex)
                        {
                            log.Warn($"[Locale] RemoveSource failed: {ex.GetType().Name}: {ex.Message}");
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                // Last-resort guard: log it for diagnostics,
                // but do not rethrow. Nothing escapes OnDispose to avoid crashing the game.
                log.Error($"OnDispose fatal: {ex.GetType().Name}: {ex.Message}");
#if DEBUG
                log.Debug(ex.ToString());   // stack trace in debug builds
#endif
            }
            finally
            {
                // 4) Clear flags and references so loader can reload cleanly
                m_LocaleRegistered = false;
                m_Locale = null;

                _onProgress = null;
                _onCompleted = null;
                _onNoWork = null;

                CleanupSystem = null;
                m_Setting = null;
            }
        }

    }
}
