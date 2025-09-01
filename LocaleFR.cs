// LocaleFR.cs
using Colossal;                    // IDictionarySource, IDictionaryEntryError
using System.Collections.Generic;  // IEnumerable, KeyValuePair, Dictionary

namespace CitizenEntityCleaner
{
    /// <summary>
    /// French (fr-FR) locale entries for the Citizen Entity Cleaner settings UI.
    /// </summary>
    public class LocaleFR : IDictionarySource
    {
        private readonly Setting m_Setting;
        public LocaleFR(Setting setting) { m_Setting = setting; }

        public IEnumerable<KeyValuePair<string, string>> ReadEntries(
            IList<IDictionaryEntryError> errors, Dictionary<string, int> indexCounts)
        {
            return new Dictionary<string, string>
            {
                // Mod name in Options list
                { m_Setting.GetSettingsLocaleID(), Mod.Name },

                // Tabs
                { m_Setting.GetOptionTabLocaleID(Setting.kSection), "Principal" },
                { m_Setting.GetOptionTabLocaleID(Setting.AboutTab), "À propos" },

                // Groups
                { m_Setting.GetOptionGroupLocaleID(Setting.kFiltersGroup), "Filtres" },
                { m_Setting.GetOptionGroupLocaleID(Setting.kButtonGroup), "Général" },
                { m_Setting.GetOptionGroupLocaleID(Setting.InfoGroup), "Infos" },

                // Filter toggles
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.IncludeCorrupt)), "Inclure citoyens corrompus" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.IncludeCorrupt)),
                  "Quand activé (par défaut), compte et supprime les citoyens « corrompus » :\n" +
                  "résidents sans PropertyRenter (et non sans-abri, navetteurs, touristes, ni en déménagement).\n\n" +
                  "Les corrompus sont la cible principale du mod." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.IncludeHomeless)), "Inclure sans-abri" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.IncludeHomeless)), "Quand activé, compte et supprime les citoyens sans-abri." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.IncludeCommuters)), "Inclure navetteurs" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.IncludeCommuters)),
                  "Quand activé, compte et supprime les navetteurs (vivent hors-ville mais viennent travailler ici).\n\n" +
                  "Certains ont pu quitter la ville après être devenus sans-abri (jeu 1.2.5)." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.IncludeMovingAwayNoPR)), "Inclure déménagement (sans loyer)" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.IncludeMovingAwayNoPR)),
                  "Quand activé, compte et supprime les citoyens « en déménagement » qui n’ont pas de PropertyRenter.\n\n" +
                  "Ceux avec PropertyRenter sont conservés." },

                // Buttons
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.CleanupEntitiesButton)), "Nettoyer citoyens" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.CleanupEntitiesButton)),
                  "<Chargez d’abord une ville.>\n" +
                  "Supprime les citoyens de ménages sans PropertyRenter.\n" +
                  "Inclut aussi les éléments cochés [ ✓ ].\n\n" +
                  "ATTENTION : contournement risqué. Sauvegardez votre partie !" },
                { m_Setting.GetOptionWarningLocaleID(nameof(Setting.CleanupEntitiesButton)),
                  "Suppression définitive des éléments cochés.\n\n<Sauvegardez d’abord !>\nContinuer ?" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.RefreshCountsButton)), "Actualiser" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.RefreshCountsButton)),
                  "<Chargez une ville pour voir les nombres.>\n" +
                  "Met à jour les compteurs.\n" +
                  "Après nettoyage, laissez tourner une minute." },

                // Displays
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.CleanupStatusDisplay)), "Statut" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.CleanupStatusDisplay)),
                  "Affiche l’avancement en direct pendant l’ouverture des options." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.TotalCitizensDisplay)), "Citoyens totaux" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.TotalCitizensDisplay)),
                  "Nombre total d’entités citoyennes **actuelles** dans la simulation.\n\n" +
                  "Peut différer de la population s’il y a des entités corrompues." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.CorruptedCitizensDisplay)),
                  "Citoyens à supprimer : cochez [ ✓ ] ci-dessus" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.CorruptedCitizensDisplay)),
                  "Nombre d’entités supprimées quand vous cliquez **[Nettoyer]**,\n\n" +
                  "selon les cases [ ✓ ] sélectionnées." },

                // About tab fields
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.NameText)), "Nom du mod" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.NameText)), "Nom affiché du mod." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.VersionText)), "Version" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.VersionText)), "Version actuelle du mod." },

#if DEBUG
                // Only visible in DEBUG builds
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.InformationalVersionText)), "Version info" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.InformationalVersionText)), "Version du mod avec ID de commit" },
#endif

                // About tab links (external)
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OpenGithubButton)),  "GitHub" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OpenGithubButton)),   "Dépôt GitHub du mod (ouvre le navigateur)." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OpenDiscordButton)), "Discord" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OpenDiscordButton)),  "Salon Discord pour retours (ouvre le navigateur)." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OpenParadoxModsButton)), "Paradox Mods" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OpenParadoxModsButton)),  "Page Paradox Mods (ouvre le navigateur)." },

                // Usage section
                { m_Setting.GetOptionGroupLocaleID(Setting.UsageGroup), "UTILISATION" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.UsageSteps)),
                  "1. <Sauvegardez votre partie !>\n" +
                  "2. <Cliquez [Actualiser] pour voir les statistiques.>\n" +
                  "3. <Cochez [ ✓ ] les éléments à inclure>\n" +
                  "4. <Cliquez [Nettoyer citoyens] pour lancer le nettoyage.>" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.UsageSteps)), "" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.UsageNotes)),
                  "Notes :\n" +
                  "• Le mod **ne** s’exécute pas automatiquement ; utilisez **[Nettoyer]** à chaque fois.\n" +
                  "• Revenez à une sauvegarde si comportement inattendu." },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.UsageNotes)), "" },
            };
        }

        public void Unload() { }
    }
}
