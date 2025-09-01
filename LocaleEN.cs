// LocaleEN.cs
using Colossal;                    // IDictionarySource, IDictionaryEntryError
using Colossal.Logging;            // ILog
using System.Collections.Generic;  // IEnumerable, KeyValuePair, Dictionary, List, HashSet

namespace CitizenEntityCleaner
{
    /// <summary>
    /// English locale entries
    /// </summary>
    public class LocaleEN : IDictionarySource
    {
        private readonly Setting m_Setting;
        public LocaleEN(Setting setting) { m_Setting = setting; }

        public IEnumerable<KeyValuePair<string, string>> ReadEntries(
            IList<IDictionaryEntryError> errors, Dictionary<string, int> indexCounts)
        {
            return new Dictionary<string, string>
            {
                // Mod name in Options menu list
                { m_Setting.GetSettingsLocaleID(), Mod.Name },

                // Tabs
                { m_Setting.GetOptionTabLocaleID(Setting.kSection), "Main" },
                { m_Setting.GetOptionTabLocaleID(Setting.AboutTab), "About" },

                // Groups
                { m_Setting.GetOptionGroupLocaleID(Setting.kFiltersGroup), "Filters" },
                { m_Setting.GetOptionGroupLocaleID(Setting.kButtonGroup), "Main" },
                { m_Setting.GetOptionGroupLocaleID(Setting.InfoGroup), "Info" },

                // Filter toggles
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.IncludeCorrupt)), "Include Corrupt Citizens" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.IncludeCorrupt)),
                  "When enabled (default), counts and cleans up **corrupt** citizens;\n" +
                  "residents that lack a PropertyRenter component (and are not homeless, commuters, tourists, or moving-away).\n\n" +
                  "Corrupt citizens are the main target of this mod. If the city contains too many, it could cause issues over time." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.IncludeHomeless)), "Include Homeless" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.IncludeHomeless)),
                  "When enabled, counts and cleans up **homeless** citizens." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.IncludeCommuters)), "Include Commuters" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.IncludeCommuters)),
                  "When enabled, counts and cleans up **commuter** citizens. Commuters include citizens that don't live in your city but travel to your city for work.\n\n" +
                  "Sometimes, commuters previously lived in your city but moved out due to homelessness (feature added in game version 1.2.5)." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.IncludeMovingAwayNoPR)), "Include Moving-Away (with no rent)" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.IncludeMovingAwayNoPR)),
                  "When enabled, counts and cleans up citizens currently **moving away** who **do not** have a PropertyRenter component.\n\n" +
                  "Moving-away citizens with PropertyRenter are preserved and not included." },

                // Buttons (Main group)
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.CleanupEntitiesButton)), "Cleanup Citizens" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.CleanupEntitiesButton)),
                  "<Load a saved city first.>\nRemoves citizens from households that no longer have a PropertyRenter component.\n Cleanup also includes any optional items selected [ ✓ ].\n\nBE CAREFUL: this is a workaround and may corrupt other data. Create a backup of your save first!" },
                { m_Setting.GetOptionWarningLocaleID(nameof(Setting.CleanupEntitiesButton)),
                  "Permanently delete items selected in options.\n\n<Please backup your save first!>\n Continue?" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.RefreshCountsButton)), "Refresh Counts" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.RefreshCountsButton)),
                  "<Load a saved city first to get numbers.>\nUpdates all entity counts to show current city statistics.\nAfter cleaning, let the game run unpaused for a minute." },

                // Displays
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.CleanupStatusDisplay)), "Status" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.CleanupStatusDisplay)),
                  "Shows the current cleanup status. Updates live while the settings screen is open." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.TotalCitizensDisplay)), "Total Citizens" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.TotalCitizensDisplay)),
                  "Total number of citizen entities **currently in the simulation.**\n\n" +
                  "This number may not match your population because it may include possible corrupt entities." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.CorruptedCitizensDisplay)),
                  "Citizens to Clean: select [ ✓ ] above" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.CorruptedCitizensDisplay)),
                  "Number of citizen entities to remove when you click **[Cleanup]**,\n\n" +
                  "based on the selected boxes [ ✓ ]." },

                // About tab fields
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.NameText)), "Mod Name" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.NameText)), "Display name of this mod." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.VersionText)), "Version" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.VersionText)), "Current mod version." },

#if DEBUG
                // Only visible in DEBUG builds
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.InformationalVersionText)), "Informational Version" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.InformationalVersionText)), "Mod Version with Commit ID" },
#endif

                // About tab links (the three external link buttons)
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OpenGithubButton)),  "GitHub" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OpenGithubButton)),   "GitHub repository for the mod; opens in browser." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OpenDiscordButton)), "Discord" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OpenDiscordButton)),  "Discord chat for feedback on the mod; opens in browser." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OpenParadoxModsButton)), "Paradox Mods" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OpenParadoxModsButton)),  "Paradox Mods website; opens in browser." },

                // About tab --> Usage section header & blocks
                { m_Setting.GetOptionGroupLocaleID(Setting.UsageGroup), "USAGE" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.UsageSteps)),
                  "1. <Backup your save file first!>\n" +
                  "2. <Click [Refresh Counts] to see current statistics.>\n" +
                  "3. <[ ✓ ] Select the items to include using the checkboxes>\n" +
                  "4. <Click [Cleanup Citizens] to clean up entities.>" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.UsageSteps)), "" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.UsageNotes)),
                  "Notes:\n" +
                  "• This mod does **not** run automatically; use **[Cleanup Citizens]** each time for removals.\n" +
                  "• Revert to original saved city if needed for unexpected behavior." },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.UsageNotes)), "" },
            };
        }

        public void Unload() { }
    }

#if DEBUG
    /// <summary>DEBUG-only: validates critical locale keys exist; logs warnings if missing.</summary>
    public static class LocaleSelfTest
    {
        public static void ValidateRequiredEntries(LocaleEN source, Setting setting, ILog log)
        {
            var entries = source.ReadEntries(new List<IDictionaryEntryError>(),
                                             new Dictionary<string, int>());
            var keys = new HashSet<string>();
            foreach (var kv in entries) keys.Add(kv.Key);

            var required = new[]
            {
                setting.GetOptionLabelLocaleID(nameof(Setting.CleanupEntitiesButton)),
                setting.GetOptionLabelLocaleID(nameof(Setting.RefreshCountsButton)),
                setting.GetOptionLabelLocaleID(nameof(Setting.OpenGithubButton)),
                setting.GetOptionDescLocaleID(nameof(Setting.OpenGithubButton)),
                setting.GetOptionLabelLocaleID(nameof(Setting.OpenDiscordButton)),
                setting.GetOptionDescLocaleID(nameof(Setting.OpenDiscordButton)),
                setting.GetOptionLabelLocaleID(nameof(Setting.OpenParadoxModsButton)),
                setting.GetOptionDescLocaleID(nameof(Setting.OpenParadoxModsButton)),
            };

            int missing = 0;
            foreach (var k in required)
            {
                if (!keys.Contains(k))
                {
                    missing++;
                    log.Warn($"[LocaleSelfTest] Missing locale entry: {k}");
                }
            }

            if (missing == 0)
                log.Info("[LocaleSelfTest] All required locale keys present.");
        }
    }
#endif
}
