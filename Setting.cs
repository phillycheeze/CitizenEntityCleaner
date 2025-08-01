using Colossal;
using Colossal.IO.AssetDatabase;
using Game.Input;
using Game.Modding;
using Game.Settings;
using Game.UI;
using Game.UI.Widgets;
using System.Collections.Generic;
using Unity.Entities;

namespace CitizenEntityCleaner
{
    [FileLocation("ModsSettings/" + nameof(CitizenEntityCleaner))]
    [SettingsUIGroupOrder(kButtonGroup, kToggleGroup, kSliderGroup, kDropdownGroup)]
    [SettingsUIShowGroupName(kButtonGroup, kToggleGroup, kSliderGroup, kDropdownGroup,)]
    [SettingsUIKeyboardAction(Mod.kVectorActionName, ActionType.Vector2, usages: new string[] { Usages.kMenuUsage, "TestUsage" }, interactions: new string[] { "UIButton" }, processors: new string[] { "ScaleVector2(x=100,y=100)" })]
    [SettingsUIKeyboardAction(Mod.kAxisActionName, ActionType.Axis, usages: new string[] { Usages.kMenuUsage, "TestUsage" }, interactions: new string[] { "UIButton" })]
    [SettingsUIKeyboardAction(Mod.kButtonActionName, ActionType.Button, usages: new string[] { Usages.kMenuUsage, "TestUsage" }, interactions: new string[] { "UIButton" })]
    [SettingsUIGamepadAction(Mod.kButtonActionName, ActionType.Button, usages: new string[] { Usages.kMenuUsage, "TestUsage" }, interactions: new string[] { "UIButton" })]
    [SettingsUIMouseAction(Mod.kButtonActionName, ActionType.Button, usages: new string[] { Usages.kMenuUsage, "TestUsage" }, interactions: new string[] { "UIButton" })]
    public class Setting : ModSetting
    {
        public const string kSection = "Main";

        public const string kButtonGroup = "Button";
        public const string kToggleGroup = "Toggle";
        public const string kSliderGroup = "Slider";
        public const string kDropdownGroup = "Dropdown";

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
                    // Refresh the count after cleanup
                    RefreshEntityCount();
                }
                else
                {
                    Mod.log.Warn("CleanupSystem is not available");
                }
            } 
        }

        [SettingsUISection(kSection, kButtonGroup)]
        public string CitizenCountDisplay 
        { 
            get 
            {
                return GetCurrentCitizenCount();
            }
        }

        private string _entityStats = "Loading...";
        
        [SettingsUISection(kSection, kButtonGroup)]
        public string EntityStatsDisplay 
        { 
            get => _entityStats;
        }

        public override void SetDefaults()
        {
            // Initialize with default message, will update when system is available
            _entityStats = "Click Cleanup button to refresh statistics";
        }

        private string GetCurrentCitizenCount()
        {
            try
            {
                if (Mod.CleanupSystem != null)
                {
                    var stats = Mod.CleanupSystem.GetEntityStatistics();
                    return $"Total Citizens: {stats.totalCitizens:N0}";
                }
                return "Citizens: System not available";
            }
            catch (System.Exception ex)
            {
                Mod.log.Warn($"Error getting citizen count: {ex.Message}");
                return "Citizens: Error";
            }
        }

        public void RefreshEntityCount()
        {
            try
            {
                if (Mod.CleanupSystem != null)
                {
                    var stats = Mod.CleanupSystem.GetEntityStatistics();
                    int candidatesForCleanup = Mod.CleanupSystem.GetCleanupCandidateCount();
                    
                    _entityStats = $"Citizens: {stats.totalCitizens:N0} | Households: {stats.totalHouseholds:N0} | Broken: {stats.brokenHouseholds:N0} | Will Clean: {candidatesForCleanup:N0}";
                }
                else
                {
                    _entityStats = "Cleanup System not initialized";
                }
            }
            catch (System.Exception ex)
            {
                Mod.log.Warn($"Error refreshing entity count: {ex.Message}");
                _entityStats = "Error loading statistics";
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
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.CleanupEntitiesButton)), "Cleanup Broken Citizens" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.CleanupEntitiesButton)), "Removes citizens from households that no longer have a PropertyRenter component, which typically indicates broken or orphaned household data. This helps clean up simulation inconsistencies." },
                { m_Setting.GetOptionWarningLocaleID(nameof(Setting.CleanupEntitiesButton)), "This will permanently delete citizens from broken households. Continue?" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.EntityStatsDisplay)), "Entity Statistics" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.EntityStatsDisplay)), "Current count of citizens, households, broken households, and citizens that will be cleaned up. Updates when you click the Cleanup button. Broken households are those without PropertyRenter components." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.CitizenCountDisplay)), "Citizen Count" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.CitizenCountDisplay)), "Total number of citizen entities currently in the simulation." },
                { m_Setting.GetOptionTabLocaleID(Setting.kSection), "Main" },

                //{ m_Setting.GetBindingMapLocaleID(), "Mod settings sample" },
            };
        }

        public void Unload()
        {

        }
    }
}
