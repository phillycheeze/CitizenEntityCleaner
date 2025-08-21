using Colossal;
using Colossal.IO.AssetDatabase;
using Game.Modding;
using Game.Settings;
using System.Collections.Generic;
using UnityEngine;      // for About tab Application.OpenURL


namespace CitizenEntityCleaner
{
    public static class ModKeys
    {
        public const string SettingsKey = "CitizenEntityCleaner"; // Reuse same settings key everywhere
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
        public const string UsageGroup = "Usage";    //section in About tab

        public const string kButtonGroup = "Button";
        public const string kFiltersGroup = "Filters";

        private bool _includeHomeless = false;
        private bool _includeCommuters = false;

        private string _totalCitizens = "Click Refresh to load";
        private string _corruptedCitizens = "Click Refresh to load";
        private string _cleanupStatus = "Idle";

        internal bool _isCleanupInProgress = false;

        public Setting(IMod mod) : base(mod) { }

        // -------------------------
        // Filter toggles (static labels)
        // -------------------------
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
        public string VersionText =>
        #if DEBUG
            $"{Mod.Version} - DEV";
        #else
            Mod.Version;
        #endif

        [SettingsUISection(AboutTab, InfoGroup)]
        public string AuthorText => Mod.Author;

        // -------------------------
        // About Tab links
        // -------------------------
        [SettingsUIButtonGroup("SocialLinks")]    // Group to get Github & Discord links on same line
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

        // Paradox Mods link on its own row (no group)
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
        public string UsageSteps => string.Empty;   // Note: UsageSteps is a static label.
        
        [SettingsUIMultilineText]
        [SettingsUISection(AboutTab, UsageGroup)]
        public string UsageNotes => string.Empty;

           
        public override void SetDefaults()
        {
            // Explicit defaults for checkboxes for clarity
            _includeHomeless  = false;
            _includeCommuters = false;

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
                    var (totalCitizens, corruptedCitizens) = Mod.CleanupSystem.GetCitizenStatistics();

                    _totalCitizens = $"{totalCitizens:N0}";
                    _corruptedCitizens = $"{corruptedCitizens:N0}";
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

            // Nudge the UI to re-read values (labels remain static)
            ApplyAndSave();
        }

        /// <summary>Starts progress tracking for cleanup operation</summary>
        public void StartCleanupProgress()
        {
            _isCleanupInProgress = true;
            _cleanupStatus = "Cleanup in progress… 0%";
            _corruptedCitizens = "Cleaning... 0%";
            ApplyAndSave();
        }

        /// <summary>Updates cleanup progress display</summary>
        public void UpdateCleanupProgress(float progress)
        {
            if (_isCleanupInProgress)
            {
                _cleanupStatus = $"Cleanup in progress… {progress:P0}";
                _corruptedCitizens = $"Cleaning... {progress:P0}";
                ApplyAndSave();
            }
        }

        /// <summary>Finishes progress tracking and refreshes final counts</summary>
        public void FinishCleanupProgress()
        {
            _isCleanupInProgress = false;
            _cleanupStatus = "Complete";
            RefreshEntityCounts();    // updates values & calls ApplyAndSave()
        }
        
        /// <summary>
        /// Finishes progress when there was nothing to clean.
        /// </summary>
        public void FinishCleanupNoWork()
        {
            _isCleanupInProgress = false;
            _cleanupStatus = "Nothing to clean";
            RefreshEntityCounts();    // updates values and calls ApplyAndSave()
        }
    }

    
    // -------------------------
    // Locale (ALL labels are static now)
    // -------------------------
    public class LocaleEN : IDictionarySource
    {
        private readonly Setting m_Setting;
        public LocaleEN(Setting setting) { m_Setting = setting; }

        public IEnumerable<KeyValuePair<string, string>> ReadEntries(
            IList<IDictionaryEntryError> errors, Dictionary<string, int> indexCounts)
        {
            return new Dictionary<string, string>
            {
                { m_Setting.GetSettingsLocaleID(), Mod.Name }, //Mod name display in game Options Menu

                // Tabs
                { m_Setting.GetOptionTabLocaleID(Setting.kSection), "Main" },
                { m_Setting.GetOptionTabLocaleID(Setting.AboutTab), "About" },

                // Groups
                { m_Setting.GetOptionGroupLocaleID(Setting.kFiltersGroup), "Filters" },
                { m_Setting.GetOptionGroupLocaleID(Setting.kButtonGroup), "Main" },
                { m_Setting.GetOptionGroupLocaleID(Setting.InfoGroup), "Info" },

                // Filter toggles
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.IncludeHomeless)), "Include Homeless" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.IncludeHomeless)), "When enabled, also counts and cleans up citizens that the game officially flags as Homeless." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.IncludeCommuters)), "Include Commuters" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.IncludeCommuters)), "When enabled, also counts and cleans up commuter citizens. Commuters include citizens that don't live in your city but travel to your city for work.\n\nSometimes, commuters previously lived in your city but moved out due to homelessness (feature added in game version 1.2.5)." },

                // Buttons (STATIC labels)
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.CleanupEntitiesButton)), "Cleanup Citizens" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.CleanupEntitiesButton)), "Removes citizens from households that no longer have a PropertyRenter component. This also includes filtered citizens.\n\nBE CAREFUL: this is a hacky workaround and may corrupt other data. Create a backup of your save first!" },
                { m_Setting.GetOptionWarningLocaleID(nameof(Setting.CleanupEntitiesButton)), "This will permanently delete citizens from corrupted households and those you have filtered out.\n\nPlease backup your save first! Continue?" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.RefreshCountsButton)), "Refresh Counts" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.RefreshCountsButton)), "<Load a saved city first to get numbers.>\nUpdates all entity counts to show current city statistics.\nAfter cleaning, let the game run unpaused for a minute." },

                // Displays
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.CleanupStatusDisplay)), "Status" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.CleanupStatusDisplay)), "Shows the current cleanup status. Updates live while the settings screen is open." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.TotalCitizensDisplay)), "Total Citizens" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.TotalCitizensDisplay)), "Total number of citizen entities currently in the simulation." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.CorruptedCitizensDisplay)), "Corrupted Citizens (including filters above)" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.CorruptedCitizensDisplay)), "Number of citizens in households without PropertyRenter components that will be cleaned up and deleted." },

                // About tab fields
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.NameText)), "Mod Name" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.NameText)), "Display name of this mod." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.VersionText)), "Version" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.VersionText)), "Current mod version." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.AuthorText)), "Author" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.AuthorText)), "Mod author" }, 

                // About tab links
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OpenGithubButton)),  "GitHub" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OpenGithubButton)),   "GitHub repository for the mod; opens in browser." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OpenDiscordButton)), "Discord" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OpenDiscordButton)),  "Discord chat for feedback on the mod; opens in browser." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OpenParadoxModsButton)), "Paradox Mods" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OpenParadoxModsButton)),  "Paradox Mods website; opens in browser." },


                // About tab --> Usage section header
                { m_Setting.GetOptionGroupLocaleID(Setting.UsageGroup), "USAGE" },

                // Steps block (normal spacing)
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.UsageSteps)),
                  "1. <Backup your save file first!>\n" +
                  "2. <Click [Refresh Counts] to see current statistics.>\n" +
                  "3. <[ ✓ ]  Use optional checkboxes to include homeless or commuters.>\n" +
                  "4. <Click [Cleanup Citizens] to clean up entities.>"
                },
                {m_Setting.GetOptionDescLocaleID(nameof(Setting.UsageSteps)), "" }, // no tooltip needed
                
                // Notes (separate block gives larger gap above)
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.UsageNotes)),
                "Notes:\n" +
                "• This mod does not run automatically; [Cleanup Citizens] must be used each time for removals.\n" +
                "• Revert to original saved city if needed for unexpected behavior."
                },
                
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.UsageNotes)), "" }, // No tooltip needed


            };
        }

        public void Unload() { }
    }
}
