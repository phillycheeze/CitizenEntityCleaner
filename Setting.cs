using Colossal.IO.AssetDatabase;
using Game.Modding;
using Game.Settings;
using UnityEngine;      // for About tab Application.OpenURL
using Game.UI.Widgets; // for SettingsUIMultilineText, SettingsUIDisplayName


namespace CitizenEntityCleaner
{
    internal static class ModKeys
    {
        public const string SettingsKey = "CitizenEntityCleaner";
    }

    [FileLocation(ModKeys.SettingsKey)]
    [SettingsUITabOrder(MainTab, AboutTab)]
    [SettingsUIGroupOrder(kFiltersGroup, kButtonGroup, InfoGroup, UsageGroup)]
    [SettingsUIShowGroupName(kFiltersGroup, kButtonGroup, UsageGroup)]    // Note: InfoGroup header omitted on purpose for About tab.
    public class Setting : ModSetting
    {
        public const string kSection = "Main";
        public const string MainTab = "Main";
        public const string AboutTab = "About";
        public const string InfoGroup = "Info";
        public const string UsageGroup = "Usage";    //About tab section for usage instructions

        public const string kButtonGroup = "Button";
        public const string kFiltersGroup = "Filters";

        private bool _includeCorrupt = true; // defaults ON
        private bool _includeHomeless = false;
        private bool _includeCommuters = false;
        private bool _includeMovingAwayNoPR = false;

        private string _totalCitizens = "Click Refresh to load";
        private string _corruptedCitizens = "Click Refresh to load";
        private string _cleanupStatus = "Idle";

        internal bool _isCleanupInProgress = false;

        public Setting(IMod mod) : base(mod) { }

        // -------------------------
        // Filter toggles (static labels)
        // -------------------------
        [SettingsUISection(kSection, kFiltersGroup)]
        public bool IncludeCorrupt
        {
            get => _includeCorrupt;
            set
            {
                if (_includeCorrupt == value) return;
                _includeCorrupt = value;
                ApplyAndSave();            // <-- persist the checkbox value.
                RefreshEntityCounts();    // optional live update
            }
        }

        [SettingsUISection(kSection, kFiltersGroup)]
        public bool IncludeHomeless
        {
            get => _includeHomeless;
            set
            {
                if (_includeHomeless == value) return;  // <-- remember if user checks box.
                _includeHomeless = value;
                ApplyAndSave();          // <-- persist the checkbox value.
                RefreshEntityCounts();   // optional live update
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
                ApplyAndSave();            // <-- persist the checkbox value.
                RefreshEntityCounts();    // optional live update
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
                ApplyAndSave();            // <-- persist the checkbox value.
                RefreshEntityCounts();    // optional
            }
        }


        // -------------------------
        // Buttons (static labels)
        // -------------------------
        [SettingsUIButton]
        [SettingsUIConfirmation]
        [SettingsUISection(kSection, kButtonGroup)]
        public bool CleanupEntitiesButton
        {
            set
            {
                if (_isCleanupInProgress)
                {
                    Mod.log.Info("Cleanup already in progress, ignoring button click");
                    return;
                }

                Mod.log.Info("Cleanup entities button clicked");
                if (Mod.CleanupSystem != null)
                {
                    StartCleanupProgress();
           
                    Mod.CleanupSystem.TriggerCleanup();
                }
                else
                {
                    Mod.log.Warn("CleanupSystem is not available");
                }
            }
        }

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

                Mod.log.Info("Refresh counts button clicked");
                RefreshEntityCounts();
            }
        }

        // -------------------------
        // Read-only displays (dynamic VALUES; labels are static via locale)
        // -------------------------
        [SettingsUISection(kSection, kButtonGroup)]
        public string CleanupStatusDisplay => _cleanupStatus;

        [SettingsUISection(kSection, kButtonGroup)]
        public string TotalCitizensDisplay => _totalCitizens;

        [SettingsUISection(kSection, kButtonGroup)]
        public string CorruptedCitizensDisplay => _corruptedCitizens;


        // -------------------------
        // About Tab info
        // -------------------------
        [SettingsUISection(AboutTab, InfoGroup)]
        public string NameText => Mod.Name;

        [SettingsUISection(AboutTab, InfoGroup)]
        public string VersionText => Mod.VersionShort;

        #if DEBUG
        [SettingsUISection(AboutTab, InfoGroup)]
        public string InformationalVersionText => Mod.VersionInformational;
        #endif

        // -------------------------
        // About Tab links
        // -------------------------
        [SettingsUIButtonGroup("SocialLinks")]
        [SettingsUIButton]
        [SettingsUISection(AboutTab, InfoGroup)]
        public bool OpenGithubButton
        {
            set
            {
                try { Application.OpenURL("https://github.com/phillycheeze/CitizenEntityCleaner"); }
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
                try { Application.OpenURL("https://discord.com/channels/1024242828114673724/1402078697120469064"); }
                catch (System.Exception ex) { Mod.log.Warn($"Failed to open Discord: {ex.Message}"); }
            }
        }

        [SettingsUIButtonGroup("SocialLinks")]
        [SettingsUIButton]
        [SettingsUISection(AboutTab, InfoGroup)]
        public bool OpenParadoxModsButton
        {
            set
            {
                try { Application.OpenURL("https://mods.paradoxplaza.com/mods/117161/Windows"); }
                catch (System.Exception ex) { Mod.log.Warn($"Failed to open Paradox Mods: {ex.Message}"); }
            }
        }
       
        // --- About tab: USAGE ---
        [SettingsUIMultilineText]
        [SettingsUISection(AboutTab, UsageGroup)]
        public string UsageSteps => string.Empty;
        
        [SettingsUIMultilineText]
        [SettingsUISection(AboutTab, UsageGroup)]
        public string UsageNotes => string.Empty;

           
        public override void SetDefaults()
        {
            // Explicit defaults for checkboxes
            _includeCorrupt = true;
            _includeHomeless  = false;
            _includeCommuters = false;
            _includeMovingAwayNoPR = false;

            // Display strings
            _totalCitizens = "Click Refresh to load";
            _corruptedCitizens = "Click Refresh to load";
            _cleanupStatus = "Idle";
        }

        // -------------------------
        // Logic
        // -------------------------
        public void RefreshEntityCounts()
        {
            try
            {
                if (Mod.CleanupSystem != null)
                {
                    var (totalCitizens, citizensToClean) = Mod.CleanupSystem.GetCitizenStatistics();

                    _totalCitizens = $"{totalCitizens:N0}";
                    _corruptedCitizens = $"{citizensToClean:N0}";
                }
                else
                {
                    _totalCitizens = "System not available";
                    _corruptedCitizens = "System not available";
                }
            }
            catch (System.Exception ex)
            {
                Mod.log.Warn($"Error refreshing entity counts: {ex.Message}");
                _totalCitizens = "Error";
                _corruptedCitizens = "Error";
            }

            // Nudge UI to re-read values
            ApplyAndSave();
        }

        /// <summary>
        /// Starts progress tracking for cleanup operation
        /// </summary>
        public void StartCleanupProgress()
        {
            _isCleanupInProgress = true;
            _cleanupStatus = "Cleanup in progress… 0%";
            _corruptedCitizens = "Cleaning... 0%";
            ApplyAndSave();
        }

        /// <summary>
        /// Updates cleanup progress display
        /// </summary>
        public void UpdateCleanupProgress(float progress)
        {
            if (_isCleanupInProgress)
            {
                _cleanupStatus = $"Cleanup in progress… {progress:P0}";
                _corruptedCitizens = $"Cleaning... {progress:P0}";
                ApplyAndSave();
            }
        }

        /// <summary>
        /// Finishes progress tracking and refreshes final counts
        /// </summary>
        public void FinishCleanupProgress()
        {
            _isCleanupInProgress = false;
            _cleanupStatus = "Complete";
            RefreshEntityCounts();
        }
        
        /// <summary>
        /// Finishes progress when there was nothing to clean.
        /// </summary>
        public void FinishCleanupNoWork()
        {
            _isCleanupInProgress = false;
            _cleanupStatus = "Nothing to clean";
            RefreshEntityCounts();
        }
    }
}

