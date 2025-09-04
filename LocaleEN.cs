// LocaleEN.cs
using Colossal;                    // IDictionarySource
using Colossal.IO.AssetDatabase.Internal;
using System.Collections.Generic;  // Dictionary

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
                { m_Setting.GetOptionTabLocaleID(Setting.kSection), "Actions" },
                { m_Setting.GetOptionTabLocaleID(Setting.AboutTab), "About" },

                // Groups
                { m_Setting.GetOptionGroupLocaleID(Setting.kFiltersGroup), "Cleanup Targets" },
                { m_Setting.GetOptionGroupLocaleID(Setting.kButtonGroup), "Actions" },
                { m_Setting.GetOptionGroupLocaleID(Setting.InfoGroup), "Info" },

                // Filter toggles
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.IncludeCorrupt)), "Corrupt Citizens" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.IncludeCorrupt)),
                  "When enabled (default), counts and cleans up **corrupt** citizens;\n" +
                  "residents that lack a PropertyRenter component (and are not homeless, commuters, tourists, or moving-away).\n\n" +
                  "Corrupt citizens are the main target of this mod. If the city contains too many, it could cause problems over time." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.IncludeMovingAwayNoPR)), "Moving-Away (Rent = 0)" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.IncludeMovingAwayNoPR)),
                  "When enabled, counts and cleans up citizens currently **Moving-Away** with Rent = 0 (i.e., no PropertyRenter component).\n\n" +
                  "Moving-Away citizens with PropertyRenter or Rent > 0 are not removed." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.IncludeCommuters)), "Commuters" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.IncludeCommuters)),
                  "When enabled, counts and cleans up **commuter** citizens. Commuters include citizens that don't live in your city but travel to your city for work.\n\n" +
                  "Sometimes, commuters previously lived in your city but moved out due to homelessness (feature added in game version 1.2.5)." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.IncludeHomeless)), "Homeless" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.IncludeHomeless)),
                  "When enabled, counts and cleans up **homeless** citizens.\n\n" +
                  "<BE CAREFUL>: deleting homeless can cause unknown side effects." },

                // Buttons (Main group)
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.CleanupEntitiesButton)), "Cleanup Citizens" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.CleanupEntitiesButton)),
                  "<Load a saved city first.>\nRemoves citizens from households that no longer have a PropertyRenter component.\n" +
                  "Cleanup also includes any optional items selected [ ✓ ].\n\n" +
                  "**BE CAREFUL**: this is a workaround and may corrupt other data. Create a backup of your save first!" },
                { m_Setting.GetOptionWarningLocaleID(nameof(Setting.CleanupEntitiesButton)),
                  "Permanently delete items selected in options.\n\n<Please backup your save first!>\n Continue?" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.RefreshCountsButton)), "Refresh Counts" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.RefreshCountsButton)),
                  "<Load a saved city first to get numbers.>\n" +
                  "Updates all entity counts to show current city statistics.\n" +
                  "After cleaning, let the game run unpaused for a minute." },

                // Displays
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.CleanupStatusDisplay)), "Status" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.CleanupStatusDisplay)),
                  "Shows the cleanup status. Updates live during an active cleanup; otherwise press [Refresh Counts] to recompute.\n\n" +
                  "\"**Idle**\" = no cleanup running or no city loaded yet.\n" +
                  "\"**Nothing to clean**\" = no citizens match the selected filters (or you already removed them).\n" +
                  "\"**Complete**\" = last cleanup finished; persists until you change filters or run a new cleanup." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.TotalCitizensDisplay)), "Total Citizens" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.TotalCitizensDisplay)),
                  "Total number of citizen entities **currently in the simulation.**\n\n" +
                  "This number can differ from your population because it may include corrupt entities." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.CorruptedCitizensDisplay)),
                  "Citizens to Clean: select [ ✓ ] above" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.CorruptedCitizensDisplay)),
                  "Number of citizen entities to remove when you click **[Cleanup]**,\n\n" +
                  "based on the selected boxes [ ✓ ]." },

                // Prompts (used by Setting.cs for placeholder text)
                { "CitizenEntityCleaner/Prompt/RefreshCounts", "Click [Refresh Counts]" },

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

                // Notes block
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.UsageNotes)),
                  "Notes:\n" +
                  "• This mod does **not** run automatically; use **[Cleanup Citizens]** each time for removals.\n" +
                  "• Revert to original saved city if needed for unexpected behavior." },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.UsageNotes)), "" },
            };
        }

        public void Unload() { }
    }
}
