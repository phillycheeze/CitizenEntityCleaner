// LocaleFR.cs
using System.Collections.Generic;  // Dictionary
using Colossal;                    // IDictionarySource

namespace CitizenEntityCleaner
{
    /// <summary>
    /// French locale entries (fr-FR)
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
                // Mod name in Options menu list
                { m_Setting.GetSettingsLocaleID(), Mod.Name },

                // Tabs
                { m_Setting.GetOptionTabLocaleID(Setting.kSection), "Actions" },
                { m_Setting.GetOptionTabLocaleID(Setting.AboutTab), "À propos" },

                // Groups
                { m_Setting.GetOptionGroupLocaleID(Setting.kFiltersGroup), "Cibles à nettoyer" },
                { m_Setting.GetOptionGroupLocaleID(Setting.kButtonGroup), "Actions" },
                { m_Setting.GetOptionGroupLocaleID(Setting.InfoGroup), "Infos" },
                { m_Setting.GetOptionGroupLocaleID(Setting.DebugGroup), "Débogage" },

                // Filter toggles
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.IncludeCorrupt)), "Citoyens corrompus" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.IncludeCorrupt)),
                  "Lorsqu’activé (par défaut), compte et nettoie les **Citoyens Corrompus** ;\n" +
                  "résidents dépourvus du composant PropertyRenter (et qui ne sont ni sans-abri, ni navetteurs, ni touristes, ni en train de partir).\n\n" +
                  "Les citoyens corrompus sont la cible principale de ce mod. Trop nombreux, ils peuvent poser des problèmes à la longue." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.IncludeMovingAwayNoPR)), "En déménagement (Rent = 0)" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.IncludeMovingAwayNoPR)),
                  "Si activé, compte et supprime les citoyens ayant le statut **En déménagement** avec Rent = 0 (donc sans composant PropertyRenter).\n\n" +
                  "Les citoyens en déménagement avec PropertyRenter ou avec Rent > 0 ne sont pas supprimés." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.IncludeCommuters)), "Navetteurs" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.IncludeCommuters)),
                  "Lorsqu’activé, compte et nettoie les **Navetteurs**. Les navetteurs ne vivent pas dans votre ville mais y viennent pour travailler.\n\n" +
                  "Parfois, des navetteurs vivaient auparavant dans votre ville puis sont partis à cause du sans-abrisme (fonction ajoutée en version 1.2.5)." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.IncludeHomeless)), "Sans-abri" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.IncludeHomeless)),
                  "Lorsque cette option est activée, compte et nettoie les **Sans-Abri**.\n\n" +
                  "<ATTENTION>: supprimer des sans-abri peut entraîner des effets secondaires imprévus." },

                // Buttons
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.CleanupEntitiesButton)), "Nettoyer les citoyens" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.CleanupEntitiesButton)),
                  "<Chargez d’abord une partie enregistrée.>\nSupprime les citoyens des ménages qui n’ont plus le composant PropertyRenter.\n" +
                  "Le nettoyage inclut aussi les éléments optionnels cochés [ ✓ ].\n\n" +
                  "**ATTENTION** : solution de contournement pouvant corrompre d’autres données. Faites d’abord une sauvegarde de votre partie !" },
                
                // Warning (confirmation)
                { m_Setting.GetOptionWarningLocaleID(nameof(Setting.CleanupEntitiesButton)),
                  "Supprime définitivement les éléments cochés dans les options.\n\n<Merci de sauvegarder d’abord !>\nContinuer ?" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.RefreshCountsButton)), "Actualiser" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.RefreshCountsButton)),
                  "<Chargez d’abord une partie pour obtenir des chiffres.>\n" +
                  "Actualise tous les compteurs pour afficher les statistiques actuelles de la ville.\n" +
                  "Après le nettoyage, laissez le jeu tourner une minute sans pause." },

                // Debug preview
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.LogCorruptPreviewButton)), "LOG - ID corrompues (10 premières)" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.LogCorruptPreviewButton)),
                  "- Ajoute au journal les 10 premières ID de citoyens corrompus **(Index:Version)** pour vérification dans Scene Explorer.\n\n" +
                  "- **Aperçu uniquement** — rien n’est supprimé.\n\n" +
                  "- Fichier journal :\n" +
                  "%USERPROFILE%/AppData/LocalLow/Colossal Order/Cities Skylines II/logs/CitizenEntityCleaner.log" },

                // Sentence UNDER the button (multiline)
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.DebugCorruptNote)),
                  "Débogage : liste d’exemple — rien n’est supprimé.\n" +
                  "Écrit dans le journal les 10 premières ID d’entités corrompues." },

                // Displays
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.CleanupStatusDisplay)), "Statut" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.CleanupStatusDisplay)),
                  "Affiche le statut du nettoyage. Mise à jour en direct pendant un nettoyage actif ; sinon, appuyez sur [Actualiser] pour recalculer.\n\n" +
                  "\"**Idle**\" = aucun nettoyage en cours ou aucune ville chargée.\n" +
                  "\"**Nothing to clean**\" = aucun citoyen ne correspond aux filtres sélectionnés (ou vous les avez déjà supprimés).\n" +
                  "\"**Complete**\" = dernier nettoyage terminé ; persiste jusqu’à ce que vous changiez les filtres ou lanciez un nouveau nettoyage." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.TotalCitizensDisplay)), "Citoyens au total" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.TotalCitizensDisplay)),
                  "Nombre total d’entités citoyens **actuellement dans la simulation.**\n\n" +
                  "Ce nombre peut différer de votre population car il peut inclure des entités corrompues." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.CorruptedCitizensDisplay)),
                  "Citoyens à nettoyer : cochez [ ✓ ] ci-dessus" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.CorruptedCitizensDisplay)),
                  "Nombre d’entités citoyens supprimées lorsque vous cliquez sur **[Nettoyer les citoyens]**,\n\n" +
                  "selon les cases [ ✓ ] sélectionnées." },

                // Prompts (used by Setting.cs for placeholder text)
                { "CitizenEntityCleaner/Prompt/RefreshCounts", "Cliquez sur [Actualiser]" },
                { "CitizenEntityCleaner/Prompt/NoCity", "Aucune ville chargée" },
                { "CitizenEntityCleaner/Prompt/Error",  "Erreur" },
                { "CitizenEntityCleaner/Status/Progress", "Nettoyage en cours… {0}" },
                { "CitizenEntityCleaner/Status/Cleaning", "Nettoyage… {0}" },


                // About tab fields
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.NameText)), "Nom du mod" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.NameText)), "Nom d’affichage de ce mod." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.VersionText)), "Version" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.VersionText)), "Version actuelle du mod." },

#if DEBUG
                // Only visible in DEBUG builds
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.InformationalVersionText)), "Version informationnelle" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.InformationalVersionText)), "Version du mod avec l’ID de commit" },
#endif

                // About tab links (the three external link buttons)
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OpenGithubButton)),  "GitHub" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OpenGithubButton)),   "Dépôt GitHub du mod ; s’ouvre dans le navigateur." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OpenDiscordButton)), "Discord" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OpenDiscordButton)),  "Salon Discord pour les retours sur le mod ; s’ouvre dans le navigateur." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OpenParadoxModsButton)), "Paradox Mods" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OpenParadoxModsButton)),  "Site Paradox Mods ; s’ouvre dans le navigateur." },

                // About tab --> Usage section header & blocks
                { m_Setting.GetOptionGroupLocaleID(Setting.UsageGroup), "UTILISATION" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.UsageSteps)),
                  "1. <Sauvegardez d’abord votre partie !>\n" +
                  "2. <Cliquez sur [Actualiser] pour voir les stats.>\n" +
                  "3. [ ✓ ] <Sélectionnez les éléments à inclure via les cases>\n" +
                  "4. <Cliquez sur [Nettoyer les citoyens] pour lancer le nettoyage.>" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.UsageSteps)), "" },

                // Notes block
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.UsageNotes)),
                  "Notes :\n" +
                  "• Ce mod ne s’exécute **pas** automatiquement ; utilisez **[Nettoyer les citoyens]** à chaque fois pour supprimer.\n" +
                  "• Revenez à votre sauvegarde d’origine en cas de comportement inattendu." },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.UsageNotes)), "" },
            };
        }

        public void Unload() { }
    }
}
