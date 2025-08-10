using Colossal;
using Colossal.IO.AssetDatabase;
using Game.Input;
using Game.Modding;
using Game.Settings;
using Game.UI;
using Game.UI.Widgets;
using System.Collections.Generic;

namespace CitizenEntityCleaner
{
    [FileLocation("Citizen_Entity_Cleaner")]
    [SettingsUIGroupOrder(kFiltersGroup, kButtonGroup)]
    [SettingsUIShowGroupName(kFiltersGroup, kButtonGroup)]
    public class Setting : ModSetting
    {
        public const string kSection = "Main";

        public const string kButtonGroup = "Button";
        public const string kFiltersGroup = "Filters";

        public Setting(IMod mod) : base(mod)
        {

        }

        private bool _includeHomeless = false;
        private bool _includeCommuters = false;

        [SettingsUISection(kSection, kFiltersGroup)]
        public bool IncludeHomeless 
        { 
            get => _includeHomeless; 
            set 
            { 
                _includeHomeless = value; 
                RefreshEntityCounts();
            } 
        }

        [SettingsUISection(kSection, kFiltersGroup)]
        public bool IncludeCommuters 
        { 
            get => _includeCommuters; 
            set 
            { 
                _includeCommuters = value; 
                RefreshEntityCounts();
            } 
        }

        [SettingsUIButton]
        [SettingsUIConfirmation]
        [SettingsUISection(kSection, kButtonGroup)]
        public bool CleanupEntitiesButton 
        { 
            set 
            { 
                Mod.log.Info("Cleanup entities button clicked");
                if (Mod.CleanupSystem != null)
                {
                    Mod.CleanupSystem.TriggerCleanup();
                    RefreshEntityCounts();
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
                Mod.log.Info("Refresh counts button clicked");
                RefreshEntityCounts();
            } 
        }

        private string _totalCitizens = "Click Refresh to load";
        private string _corruptedCitizens = "Click Refresh to load";
        
        [SettingsUISection(kSection, kButtonGroup)]
        public string TotalCitizensDisplay { get => _totalCitizens; }
        
        [SettingsUISection(kSection, kButtonGroup)]
        public string CorruptedCitizensDisplay { get => _corruptedCitizens; }
        

        public override void SetDefaults()
        {
            _totalCitizens = "Click Refresh to load";
            _corruptedCitizens = "Click Refresh to load";
        }

        public void RefreshEntityCounts()
        {
            try
            {
                if (Mod.CleanupSystem != null)
                {
                    var stats = Mod.CleanupSystem.GetCitizenStatistics();
                    
                    _totalCitizens = $"{stats.totalCitizens:N0}";
                    _corruptedCitizens = $"{stats.corruptedCitizens:N0}";
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
        }
    }

    public class LocaleEN : IDictionarySource
    {
        private readonly Setting m_Setting;
        public LocaleEN(Setting setting)
        {
            m_Setting = setting;
        }
        public IEnumerable<KeyValuePair<string, string>> ReadEntries(IList<IDictionaryEntryError> errors, Dictionary<string, int> indexCounts)
        {
            return new Dictionary<string, string>
            {
                { m_Setting.GetSettingsLocaleID(), "CitizenEntityCleaner" },

                { m_Setting.GetOptionGroupLocaleID(Setting.kFiltersGroup), "Filters" },
                { m_Setting.GetOptionGroupLocaleID(Setting.kButtonGroup), "Main" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.IncludeHomeless)), "Include Homeless" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.IncludeHomeless)), "When enabled, also counts and cleans up citizens that the game officially flags as Homeless." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.IncludeCommuters)), "Include Commuters" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.IncludeCommuters)), "When enabled, also counts and cleans up commuter citizens. Commuters include Citizens that don't live in your city but travel to your city for work.\n\nSometimes, commuters previously lived in your city but moved out due to homelessness (feature added in game version 1.2.5)." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.CleanupEntitiesButton)), "Cleanup Citizens" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.CleanupEntitiesButton)), "Removes citizens from households that no longer have a PropertyRenter component. This also includes filtered citizens.\n\nBE CAREFUL: this is a hacky workaround and may corrupt other data. Create a backup of your save first!" },
                { m_Setting.GetOptionWarningLocaleID(nameof(Setting.CleanupEntitiesButton)), "This will permanently delete citizens from corrupted households and those you have filtered out.\n\nPlease backup your save first! Continue?" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.RefreshCountsButton)), "Refresh Counts" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.RefreshCountsButton)), "Updates all entity counts below to show current statistics from your city. Must have a save loaded.\nAfter cleaning, let the game run unpaused for one minute." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.TotalCitizensDisplay)), "Total Citizens" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.TotalCitizensDisplay)), "Total number of citizen entities currently in the simulation." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.CorruptedCitizensDisplay)), "Corrupted Citizens (including filters above)" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.CorruptedCitizensDisplay)), "Number of citizens in households without PropertyRenter components that will be cleaned up and deleted." },
                { m_Setting.GetOptionTabLocaleID(Setting.kSection), "Main" },

                //{ m_Setting.GetBindingMapLocaleID(), "Mod settings sample" },
            };
        }

        public void Unload()
        {

        }
    }
}
