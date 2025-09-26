using Colossal.IO.AssetDatabase;    // [FileLocation]
using Game.Modding;     // IMod
using Game.SceneFlow;   // GameManager
using Game.Settings;    // ModSetting, [SettingsUI*]
using UnityEngine;      // Application.OpenURL
using System.Collections; // IEnumerator (for NextFrame)



namespace CitizenEntityCleaner
{
    internal static class ModKeys
    {
        public const string SettingsKey = "CitizenEntityCleaner";
    }

    /// <summary>
    /// Settings UI and State
    /// </summary>
    [FileLocation(ModKeys.SettingsKey)]
    [SettingsUITabOrder(MainTab, AboutTab)]
    [SettingsUIGroupOrder(kFiltersGroup, kButtonGroup, DebugGroup, InfoGroup, UsageGroup )]
    [SettingsUIShowGroupName(kFiltersGroup, kButtonGroup, DebugGroup)]  // InfoGroup + UsageGroup header omitted on purpose.

    public class Setting : ModSetting
    {
        // ---- UI structure ----
        public const string kSection = "Main";
        public const string MainTab = "Main";
        public const string AboutTab = "About";
        public const string InfoGroup = "Info";
        public const string UsageGroup = "Usage";    //About tab section for usage instructions
        public const string kButtonGroup = "Button";
        public const string kFiltersGroup = "Filters";
        public const string DebugGroup = "Debug";

        // ---- UI text defaults ----
        private const string DefaultCountPrompt = "Click [Refresh Counts]";

        // ---- Localization (i18n) keys ----
        private const string RefreshPromptKey = "CitizenEntityCleaner/Prompt/RefreshCounts";
        private const string NoCityKey = "CitizenEntityCleaner/Prompt/NoCity";
        private const string ErrorKey = "CitizenEntityCleaner/Prompt/Error";
        private const string StatusProgressKey = "CitizenEntityCleaner/Status/Progress"; // "{0}" = P0 percent
        private const string StatusCleaningKey = "CitizenEntityCleaner/Status/Cleaning"; // "{0}" = P0 percent

        private static string L(string key, string fallback)
        {
            var dict = GameManager.instance?.localizationManager?.activeDictionary;
            return (dict != null && dict.TryGetValue(key, out var s) && !string.IsNullOrWhiteSpace(s))
                ? s
                : fallback;
        }

        // ---- External links ----
        private const string UrlParadoxMods = "https://mods.paradoxplaza.com/mods/117161/Windows";
        private const string UrlGitHub = "https://github.com/phillycheeze/CitizenEntityCleaner";
        private const string UrlDiscord = "https://discord.com/channels/1024242828114673724/1402078697120469064";

        // ---- Backing fields for UI ----
        private bool _includeCorrupt = true; // defaults ON
        private bool _includeMovingAwayNoPR = false;
        private bool _includeHomeless = false;
        private bool _includeCommuters = false;

        private string _totalCitizens = DefaultCountPrompt;
        private string _corruptedCitizens = DefaultCountPrompt;
        private bool _showRefreshPrompt = true;  // whether "Citizens to Clean" should show the prompt


        private string _cleanupStatus = "Idle";

        private bool _isCleanupInProgress = false;


        /// <summary>
        /// Create settings object for this mod
        ///</summary>
        public Setting(IMod mod) : base(mod) { }

        // ---- Helpers ----
        private void ResetStatusIfNotRunning()
        {
            if (!_isCleanupInProgress)
                _cleanupStatus = "Idle";
        }

        // Mark 'Citizens to Clean' count as stale so the getter shows localized prompt until refreshed.
        private void ShowRefreshPrompt()
        {
            _showRefreshPrompt = true;   // show localized prompt until refreshed
            ApplyAndSave();
        }

        // Defer an action to next frame (lets confirmation modal finish opening/closing)
        private void NextFrame(System.Action action)
        {
            var gm = GameManager.instance;
            if (gm != null) gm.StartCoroutine(NextFrameCo(action));
            else action?.Invoke(); // fallback if GM missing (unlikely in Options UI)
        }

        private IEnumerator NextFrameCo(System.Action action)
        {
            yield return null; // wait one frame
            action?.Invoke();
        }


        // ---- Filter & order of toggles ----
        [SettingsUISection(kSection, kFiltersGroup)]
        public bool IncludeCorrupt
        {
            get => _includeCorrupt;
            set
            {
                if (_includeCorrupt == value) return;
                _includeCorrupt = value;
                ResetStatusIfNotRunning();
                ShowRefreshPrompt();
            }
        }

        [SettingsUISection(kSection, kFiltersGroup)]
        public bool IncludeMovingAwayNoPR
        {
            get => _includeMovingAwayNoPR;
            set
            {
                if (_includeMovingAwayNoPR == value) return;
                _includeMovingAwayNoPR = value;
                ResetStatusIfNotRunning();
                ShowRefreshPrompt();
            }
        }

        [SettingsUISection(kSection, kFiltersGroup)]
        public bool IncludeCommuters
        {
            get => _includeCommuters;
            set
            {
                if (_includeCommuters == value) return;
                _includeCommuters = value;
                ResetStatusIfNotRunning();
                ShowRefreshPrompt();
            }
        }

        [SettingsUISection(kSection, kFiltersGroup)]
        public bool IncludeHomeless
        {
            get => _includeHomeless;
            set
            {
                if (_includeHomeless == value) return;
                _includeHomeless = value;
                ResetStatusIfNotRunning();
                ShowRefreshPrompt();
            }
        }

        // ---- Actions (buttons) ----

        [SettingsUIButton]
        [SettingsUISection(kSection, kButtonGroup)]
        public bool RefreshCountsButton
        {
            set
            {
                if (_isCleanupInProgress)
                {
                    Mod.log.Info("Cleanup in progress, ignoring refresh button click");
                    return;
                }

                Mod.log.Info("Refresh Counts button clicked");
                RefreshEntityCounts();
            }
        }

        /// <summary>
        /// Show a Yes/No confirmation. If Yes, wait one frame for the dialog to close cleanly 
        /// Avoids focus conflict errors in UI.log
        /// </summary>
        [SettingsUIButton]
        [SettingsUIConfirmation]    // Yes/No modal
        [SettingsUISection(kSection, kButtonGroup)]
        public bool CleanupEntitiesButton
        {

            set
            {
                // If user clicks "No", value = false — bail early
                if (!value)
                {
#if DEBUG
        Mod.log.Debug("[Cleanup] Confirmation declined (No).");
#endif
                    return;
                }

                // Defer one frame so the confirmation modal can fully close before we change states.
                NextFrame(() =>
                {
                    try
                    {
                        if (_isCleanupInProgress)
                        {
                            Mod.log.Info("Cleanup already in progress, ignoring button click");
                            return;
                        }
                      
                        if (Mod.CleanupSystem == null)  // If Init error, bail early
                        {
                            Mod.log.Error("CleanupSystem not initialized (mod load failure).");
                            return;
                        }

                        if (!Mod.CleanupSystem.HasAnyCitizenData())     // No data
                        {
                            Mod.log.Info("No city loaded (no citizen data). Load a city first");
                            return;
                        }

                        bool anySelected = _includeCorrupt || _includeMovingAwayNoPR || _includeCommuters || _includeHomeless;
                        if (!anySelected)
                        {
                            Mod.log.Info("No checkboxes selected; nothing to clean.");
                            _cleanupStatus = "Nothing to clean";
                            _corruptedCitizens = "0";
                            _showRefreshPrompt = false;
                            Apply();
                            return;
                        }

                        _isCleanupInProgress = true;
                        _showRefreshPrompt = false;

                        Mod.log.Info("Cleanup Citizens confirmed YES; triggering cleanup (deferred)");
                        Mod.CleanupSystem.TriggerCleanup();
                    }
                    catch (System.Exception ex)
                    {
                        Mod.log.Error($"[Cleanup] Deferred start failed: {ex.GetType().Name}: {ex.Message}");
#if DEBUG
            Mod.log.Debug(ex.ToString());
#endif
                    }
                });
            }

        }


        // ---- Debug button ----
        [SettingsUIButton]
        [SettingsUISection(kSection, DebugGroup)]
        public bool LogCorruptPreviewButton
        {
            set
            {
                // Guard before work
                if (Mod.CleanupSystem == null)
                {
                    Mod.log.Error("CleanupSystem not initialized (mod load failure).");
                    return;
                }

                if (!Mod.CleanupSystem.HasAnyCitizenData())
                {
                    Mod.log.Info("[Preview] No city loaded (no citizen data). Load a city first.");
                    return;
                }

                // Preview: logs up to 10 Corrupt citizen IDs (Index:Version)
                // Method logs exactly one line:
                //  - "[Preview] Corrupt …" when there are matches
                //  - "[Preview] No Corrupt citizens found with current city data." when none
                Mod.CleanupSystem.LogCorruptPreviewToLog(10);


            }
        }

        [SettingsUIMultilineText]
        [SettingsUISection(kSection, DebugGroup)]
        public string DebugCorruptNote => string.Empty;


        // ---- Read-only displays ----
        [SettingsUISection(kSection, kButtonGroup)]
        public string CleanupStatusDisplay => string.IsNullOrEmpty(_cleanupStatus) ? "Idle" : _cleanupStatus;

        [SettingsUISection(kSection, kButtonGroup)]
        public string TotalCitizensDisplay => _totalCitizens;

        // If a language change occurs while UI is open, this getter re-reads the localized prompt.
        // Then, "Click [Refresh Counts]" reflects in current language without storing a stale string.
        [SettingsUISection(kSection, kButtonGroup)]
        public string CorruptedCitizensDisplay =>
            _showRefreshPrompt ? L(RefreshPromptKey, DefaultCountPrompt) : _corruptedCitizens;


        // ---- About tab: info ----
        [SettingsUISection(AboutTab, InfoGroup)]
        public string NameText => Mod.Name;

        [SettingsUISection(AboutTab, InfoGroup)]
        public string VersionText => Mod.VersionShort;

#if DEBUG
        [SettingsUISection(AboutTab, InfoGroup)]
        public string InformationalVersionText => Mod.VersionInformational;
#endif

        // ---- About tab links: order below determines button order ----
        [SettingsUIButtonGroup("SocialLinks")]
        [SettingsUIButton]
        [SettingsUISection(AboutTab, InfoGroup)]
        public bool OpenParadoxModsButton
        {
            set
            {
                try { Application.OpenURL(UrlParadoxMods); }
                catch (System.Exception ex) { Mod.log.Warn($"Failed to open Paradox Mods: {ex.Message}"); }
            }
        }

        [SettingsUIButtonGroup("SocialLinks")]
        [SettingsUIButton]
        [SettingsUISection(AboutTab, InfoGroup)]
        public bool OpenGithubButton
        {
            set
            {
                try { Application.OpenURL(UrlGitHub); }
                catch (System.Exception ex) { Mod.log.Warn($"Failed to open GitHub: {ex.Message}"); }
            }
        }

        [SettingsUIButtonGroup("SocialLinks")]
        [SettingsUIButton]
        [SettingsUISection(AboutTab, InfoGroup)]
        public bool OpenDiscordButton
        {
            set
            {
                try { Application.OpenURL(UrlDiscord); }
                catch (System.Exception ex) { Mod.log.Warn($"Failed to open Discord: {ex.Message}"); }
            }
        }

        // ---- About tab: USAGE ----
        [SettingsUIMultilineText]
        [SettingsUISection(AboutTab, UsageGroup)]
        public string UsageSteps => string.Empty;

        [SettingsUIMultilineText]
        [SettingsUISection(AboutTab, UsageGroup)]
        public string UsageNotes => string.Empty;

        /// <summary>
        /// Initialize checkbox defaults and display text
        /// </summary>
        public override void SetDefaults()
        {
            // Checkbox defaults
            _includeCorrupt = true;
            _includeMovingAwayNoPR = false;
            _includeHomeless = false;
            _includeCommuters = false;

            // Display strings
            _totalCitizens = L(RefreshPromptKey, DefaultCountPrompt);
            _corruptedCitizens = L(RefreshPromptKey, DefaultCountPrompt);
            _cleanupStatus = "Idle";
            _showRefreshPrompt = true;   // show prompt until refreshed
        }

        /// <summary>
        /// Update display values; handles errors
        /// </summary>
        public void RefreshEntityCounts()
        {
            try
            {
                // Treat missing system OR empty citizen data as “No city loaded”.
                if (Mod.CleanupSystem == null || !Mod.CleanupSystem.HasAnyCitizenData())
                {
                    _totalCitizens = L(NoCityKey, "No city loaded");
                    _corruptedCitizens = L(NoCityKey, "No city loaded");

                    // No city data. Sticky “Complete”, otherwise stay idle. 
                    if (!_isCleanupInProgress && _cleanupStatus != "Complete")
                        _cleanupStatus = "Idle";

                    _showRefreshPrompt = false; // show error message, not the Refresh prompt
                    Apply();    // early return, nothing to do
                    return;
                }

                // Data exists
                var (totalCitizens, citizensToClean) = Mod.CleanupSystem.GetCitizenStatistics();

                _totalCitizens = $"{totalCitizens:N0}";
                _corruptedCitizens = $"{citizensToClean:N0}";

                // Don’t overwrite "Complete" immediately after a cleanup; keep it until filters change or a new cleanup runs.
                if (!_isCleanupInProgress && _cleanupStatus != "Complete")
                {
                    _cleanupStatus = citizensToClean > 0 ? "Idle" : "Nothing to clean";
                }
                _showRefreshPrompt = false; // display counts, not Refresh prompt

            }
            catch (System.Exception ex)
            {
                Mod.log.Warn($"Error refreshing entity counts: {ex.Message}");
                _totalCitizens = L(ErrorKey, "Error");
                _corruptedCitizens = L(ErrorKey, "Error");
                _showRefreshPrompt = false; // show error, not Refresh prompt
            }

            Apply();
        }

        /// <summary>
        /// Updates cleanup progress display
        /// </summary>
        public void UpdateCleanupProgress(float progress)
        {
            if (!_isCleanupInProgress)
            {
#if DEBUG
                Mod.log.Debug("Ignoring progress update because no cleanup is active.");
#endif
                return;
            }

            _showRefreshPrompt = false;
            var pct = progress.ToString("P0");
            _cleanupStatus = string.Format(L(StatusProgressKey, "Cleanup in progress… {0}"), pct);
            _corruptedCitizens = string.Format(L(StatusCleaningKey, "Cleaning… {0}"), pct);
            Apply();

        }

        /// <summary>
        /// Finishes progress tracking and refreshes final counts
        /// </summary>
        public void FinishCleanupProgress()
        {
            if (!_isCleanupInProgress)
            {
#if DEBUG
                Mod.log.Debug("FinishCleanupProgress called while not in progress; ignoring.");
#endif
                return;
            }

            _isCleanupInProgress = false;
            _cleanupStatus = "Complete";    // set first so Refresh won't change it
            RefreshEntityCounts();
        }

        /// <summary>
        /// Finishes progress when there was nothing to clean.
        /// </summary>
        public void FinishCleanupNoWork()
        {
            if (!_isCleanupInProgress)
            {
#if DEBUG
                Mod.log.Debug("FinishCleanupNoWork called while not in progress; ignoring.");
#endif
                return;
            }

            _isCleanupInProgress = false;
            _cleanupStatus = "Nothing to clean";
            RefreshEntityCounts();
        }
    }
}

