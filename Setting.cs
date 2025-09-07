using Colossal.IO.AssetDatabase;    // [FileLocation]
using Game.Modding;     // IMod
using Game.Settings;    // ModSetting, [SettingsUI*]
using UnityEngine;      // Application.OpenURL
using Game.SceneFlow;   // GameManager


namespace CitizenEntityCleaner
{
    internal static class ModKeys
    {
        public const string SettingsKey = "CitizenEntityCleaner";
    }

    /// <summary>
    /// Mod settings UI and State
    /// </summary>
    [FileLocation(ModKeys.SettingsKey)]
    [SettingsUITabOrder(MainTab, AboutTab)]
    [SettingsUIGroupOrder(kFiltersGroup, kButtonGroup, InfoGroup, UsageGroup)]
    [SettingsUIShowGroupName(kFiltersGroup, kButtonGroup, UsageGroup)]    // Note: InfoGroup header omitted on purpose for About tab.

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

        // ---- UI text defaults ----
        private const string DefaultCountPrompt = "Click [Refresh Counts]";
        private const string SystemNA = "System not available";
        private const string ErrorText = "Error";

        // ---- Localization (static prompt) ----
        private const string RefreshPromptKey = "CitizenEntityCleaner/Prompt/RefreshCounts";

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
        private bool _needsRefresh = true;  // whether "Citizens to Clean" should show the prompt


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

        // Mark 'Citizens to Clean' count as stale so the getter shows the localized prompt until refreshed.
        private void SetCleanCountNeedsRefresh()
        {
            _needsRefresh = true;   // don't store the localized string anymore
            ApplyAndSave();
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
                SetCleanCountNeedsRefresh();
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
                SetCleanCountNeedsRefresh();
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
                SetCleanCountNeedsRefresh();
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
                SetCleanCountNeedsRefresh();
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
        /// Confirmation pop-up: on Yes, triggers CleanupSystem.
        /// </summary>
        [SettingsUIButton]
        [SettingsUIConfirmation]
        [SettingsUISection(kSection, kButtonGroup)]
        public bool CleanupEntitiesButton
        {
            set
            {
                // Prevent user double-clicks during a run
                if (_isCleanupInProgress)
                {
                    Mod.log.Info("Cleanup already in progress, ignoring button click");
                    return;
                }

                // Must have a system to run
                if (Mod.CleanupSystem == null)
                {
                    Mod.log.Error("CleanupSystem not initialized (mod load failure).");
                    return;
                }

                // Don't trigger if no city data present ('no city loaded' scenario)
                if (!Mod.CleanupSystem.HasAnyCitizenData())
                {
                    Mod.log.Info("No city loaded (no citizen data). Load a city first, then run Cleanup.");
                    return;
                }

                // Bail early if nothing is selected
                bool anySelected = _includeCorrupt || _includeMovingAwayNoPR || _includeCommuters || _includeHomeless;
                if (!anySelected)
                {
                    Mod.log.Info("No checkboxes selected; nothing to clean.");
                    _cleanupStatus = "Nothing to clean";
                    _needsRefresh = false;
                    Apply(); // update UI
                    return;
                }

                _isCleanupInProgress = true;
                _needsRefresh = false;

                Mod.log.Info("Cleanup Citizens confirmed YES; triggering cleanup");
                Mod.CleanupSystem.TriggerCleanup();

            }
        }


        // ---- Read-only displays ----
        [SettingsUISection(kSection, kButtonGroup)]
        public string CleanupStatusDisplay => string.IsNullOrEmpty(_cleanupStatus) ? "Idle" : _cleanupStatus;

        [SettingsUISection(kSection, kButtonGroup)]
        public string TotalCitizensDisplay => _totalCitizens;

        // If a language change occurs while UI is open, this getter re-reads the localized prompt.
        // Then, "Click [Refresh Counts]" reflects in the current language without storing a stale string.
        [SettingsUISection(kSection, kButtonGroup)]
        public string CorruptedCitizensDisplay =>
            _needsRefresh ? L(RefreshPromptKey, DefaultCountPrompt) : _corruptedCitizens;


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
            _includeHomeless  = false;
            _includeCommuters = false;

            // Display strings
            _totalCitizens = L(RefreshPromptKey, DefaultCountPrompt);
            _corruptedCitizens = L(RefreshPromptKey, DefaultCountPrompt);
            _cleanupStatus = "Idle";
            _needsRefresh = true;   // show prompt until refreshed
        }

        /// <summary>
        /// Update display values; handles null/errors
        /// </summary>
        public void RefreshEntityCounts()
        {
            try
            {
                if (Mod.CleanupSystem != null)
                {
                    var (totalCitizens, citizensToClean) = Mod.CleanupSystem.GetCitizenStatistics();

                    _totalCitizens = $"{totalCitizens:N0}";
                    _corruptedCitizens = $"{citizensToClean:N0}";

                    // Don’t overwrite "Complete" immediately after a cleanup; keep it until filters change or a new cleanup runs.
                    if (!_isCleanupInProgress && _cleanupStatus != "Complete")
                    {
                        _cleanupStatus = citizensToClean > 0 ? "Idle" : "Nothing to clean";
                    }
                    _needsRefresh = false; // we have fresh data now
                }
                else
                {
                    _totalCitizens = SystemNA;
                    _corruptedCitizens = SystemNA;

                    // No system (e.g., no city loaded). Prefer "Idle" over "Nothing to clean".
                    if (!_isCleanupInProgress && _cleanupStatus != "Complete")
                        _cleanupStatus = "Idle";
                    _needsRefresh = false;
                }
            }
            catch (System.Exception ex)
            {
                Mod.log.Warn($"Error refreshing entity counts: {ex.Message}");
                _totalCitizens = ErrorText;
                _corruptedCitizens = ErrorText;
                _needsRefresh = false;  // show error, not the prompt
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

            _needsRefresh = false;
            var pct = progress.ToString("P0");
            _cleanupStatus = $"Cleanup in progress… {pct}";
            _corruptedCitizens = $"Cleaning… {pct}";
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

