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
    [SettingsUIGroupOrder(kButtonGroup)]
    [SettingsUIShowGroupName(kButtonGroup)]
    public class Setting : ModSetting
    {
        public const string kSection = "Main";

        public const string kButtonGroup = "Button";

        public Setting(IMod mod) : base(mod)
        {

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
                    // Refresh the counts after cleanup
                    RefreshEntityCounts();
                }
                else
                {
                    Mod.log.Warn("CleanupSystem is not available");
                }
            } 
        }

        // Manual refresh button
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

        // Statistics display fields
        private string _totalCitizens = "Click Refresh to load";
        private string _totalHouseholds = "Click Refresh to load";
        private string _corruptedCitizens = "Click Refresh to load";
        private string _corruptedHouseholds = "Click Refresh to load";
        
        [SettingsUISection(kSection, kButtonGroup)]
        public string TotalCitizensDisplay { get => _totalCitizens; }
        
        [SettingsUISection(kSection, kButtonGroup)]
        public string TotalHouseholdsDisplay { get => _totalHouseholds; }
        
        [SettingsUISection(kSection, kButtonGroup)]
        public string CorruptedCitizensDisplay { get => _corruptedCitizens; }
        
        [SettingsUISection(kSection, kButtonGroup)]
        public string CorruptedHouseholdsDisplay { get => _corruptedHouseholds; }
        

        public override void SetDefaults()
        {
            // Initialize with default messages
            _totalCitizens = "Click Refresh to load";
            _totalHouseholds = "Click Refresh to load";
            _corruptedCitizens = "Click Refresh to load";
            _corruptedHouseholds = "Click Refresh to load";
        }

        public void RefreshEntityCounts()
        {
            try
            {
                if (Mod.CleanupSystem != null)
                {
                    var stats = Mod.CleanupSystem.GetDetailedEntityStatistics();
                    
                    _totalCitizens = $"{stats.totalCitizens:N0}";
                    _totalHouseholds = $"{stats.totalHouseholds:N0}";
                    _corruptedCitizens = $"{stats.corruptedCitizens:N0}";
                    _corruptedHouseholds = $"{stats.corruptedHouseholds:N0}";
                }
                else
                {
                    _totalCitizens = "System not available";
                    _totalHouseholds = "System not available";
                    _corruptedCitizens = "System not available";
                    _corruptedHouseholds = "System not available";
                }
            }
            catch (System.Exception ex)
            {
                Mod.log.Warn($"Error refreshing entity counts: {ex.Message}");
                _totalCitizens = "Error";
                _totalHouseholds = "Error";
                _corruptedCitizens = "Error";
                _corruptedHouseholds = "Error";
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

                { m_Setting.GetOptionGroupLocaleID(Setting.kButtonGroup), "Main" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.CleanupEntitiesButton)), "Cleanup Corrupted Citizens" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.CleanupEntitiesButton)), "Removes citizens from households that no longer have a PropertyRenter component. This also includes homeless citizens.\n\nBE CAREFUL: this is a hacky workaround and may corrupt other data. Create a backup of your save first!" },
                { m_Setting.GetOptionWarningLocaleID(nameof(Setting.CleanupEntitiesButton)), "This will permanently delete citizens from corrupted and homeless households. Please backup your save first!\nContinue?" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.RefreshCountsButton)), "Refresh Counts" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.RefreshCountsButton)), "Updates all entity counts below to show current statistics from your city. Must have a save loaded.\nAfter cleaning, let the game run unpaused for one minute." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.TotalCitizensDisplay)), "Total Citizens" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.TotalCitizensDisplay)), "Total number of citizen entities currently in the simulation." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.TotalHouseholdsDisplay)), "Total Households" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.TotalHouseholdsDisplay)), "Total number of household entities currently in the simulation." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.CorruptedCitizensDisplay)), "Corrupted/Homeless Citizens" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.CorruptedCitizensDisplay)), "Number of citizens in households without PropertyRenter components that will be cleaned up and deleted." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.CorruptedHouseholdsDisplay)), "Corrupted/Homeless Households" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.CorruptedHouseholdsDisplay)), "Number of households without PropertyRenter components indicating corruption, homelessness, or another issue." },
                { m_Setting.GetOptionTabLocaleID(Setting.kSection), "Main" },

                //{ m_Setting.GetBindingMapLocaleID(), "Mod settings sample" },
            };
        }

        public void Unload()
        {

        }
    }
}
