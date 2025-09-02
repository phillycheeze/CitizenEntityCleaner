// LocaleDE.cs
using Colossal;                    // IDictionarySource
using System.Collections.Generic;  // Dictionary

namespace CitizenEntityCleaner
{
    /// <summary>
    /// German locale entries (de-DE)
    /// </summary>
    public class LocaleDE : IDictionarySource
    {
        private readonly Setting m_Setting;
        public LocaleDE(Setting setting) { m_Setting = setting; }

        public IEnumerable<KeyValuePair<string, string>> ReadEntries(
            IList<IDictionaryEntryError> errors, Dictionary<string, int> indexCounts)
        {
            return new Dictionary<string, string>
            {
                // Mod name in Options menu list
                { m_Setting.GetSettingsLocaleID(), Mod.Name },

                // Tabs
                { m_Setting.GetOptionTabLocaleID(Setting.kSection), "Haupt" },
                { m_Setting.GetOptionTabLocaleID(Setting.AboutTab), "Über" },

                // Groups
                { m_Setting.GetOptionGroupLocaleID(Setting.kFiltersGroup), "Filter" },
                { m_Setting.GetOptionGroupLocaleID(Setting.kButtonGroup), "Aktionen" },
                { m_Setting.GetOptionGroupLocaleID(Setting.InfoGroup), "Info" },

                // Filter toggles
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.IncludeCorrupt)), "Korrupte Bürger einschließen" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.IncludeCorrupt)),
                  "Wenn aktiviert (Standard), zählt und bereinigt **korrupte** Bürger:\n" +
                  "Bewohner ohne PropertyRenter (und nicht obdachlos, Pendler, Touristen oder wegziehend).\n\n" +
                  "Korrupte Bürger sind das Hauptziel dieses Mods. Zu viele können langfristig Probleme verursachen." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.IncludeHomeless)), "Obdachlose einschließen" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.IncludeHomeless)),
                  "Wenn aktiviert, zählt und bereinigt **obdachlose** Bürger." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.IncludeCommuters)), "Pendler einschließen" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.IncludeCommuters)),
                  "Wenn aktiviert, zählt und bereinigt **Pendler**. Pendler leben nicht in deiner Stadt, kommen aber zur Arbeit hierher.\n\n" +
                  "Manche lebten früher hier und sind wegen Obdachlosigkeit weggezogen (seit Spielversion 1.2.5)." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.IncludeMovingAwayNoPR)), "Wegziehende (ohne Miete) einschließen" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.IncludeMovingAwayNoPR)),
                  "Wenn aktiviert, zählt und bereinigt Bürger, die **wegziehen** und **keinen** PropertyRenter haben.\n\n" +
                  "Wegziehende mit PropertyRenter bleiben erhalten." },

                // Buttons (Main group)
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.CleanupEntitiesButton)), "Bürger bereinigen" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.CleanupEntitiesButton)),
                  "<Zuerst einen Spielstand laden.>\n" +
                  "Entfernt Bürger aus Haushalten ohne PropertyRenter.\n" +
                  "Beinhaltet auch alle optional markierten Elemente [ ✓ ].\n\n" +
                  "VORSICHT: Workaround – vorher Backup erstellen!" },
                { m_Setting.GetOptionWarningLocaleID(nameof(Setting.CleanupEntitiesButton)),
                  "Ausgewählte Elemente werden dauerhaft gelöscht.\n\n<Bitte zuerst Backup erstellen!>\nFortfahren?" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.RefreshCountsButton)), "Aktualisieren" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.RefreshCountsButton)),
                  "<Lade zuerst einen Spielstand, um Zahlen zu sehen.>\n" +
                  "Aktualisiert alle Zähler mit den aktuellen Stadtstatistiken.\n" +
                  "Nach dem Bereinigen das Spiel eine Minute unpausiert laufen lassen." },

                // Displays
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.CleanupStatusDisplay)), "Status" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.CleanupStatusDisplay)),
                  "Zeigt den aktuellen Bereinigungsstatus. Aktualisiert sich live, solange die Einstellungen geöffnet sind." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.TotalCitizensDisplay)), "Bürger gesamt" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.TotalCitizensDisplay)),
                  "Gesamtzahl der Bürger-Entitäten **derzeit in der Simulation.**\n\n" +
                  "Kann von der Bevölkerung abweichen, wenn korrupte Entitäten enthalten sind." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.CorruptedCitizensDisplay)),
                  "Zu bereinigende Bürger: [ ✓ ] oben auswählen" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.CorruptedCitizensDisplay)),
                  "Anzahl der Bürger-Entitäten, die beim Klick auf **[Bürger bereinigen]** entfernt werden,\n\n" +
                  "abhängig von den gewählten Kästchen [ ✓ ]." },

                // About tab fields
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.NameText)), "Mod-Name" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.NameText)), "Anzeigename dieses Mods." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.VersionText)), "Version" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.VersionText)), "Aktuelle Mod-Version." },

#if DEBUG
                // Only visible in DEBUG builds
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.InformationalVersionText)), "Info-Version" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.InformationalVersionText)), "Mod-Version mit Commit-ID" },
#endif

                // About tab links (external)
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OpenGithubButton)),  "GitHub" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OpenGithubButton)),   "GitHub-Repository des Mods (öffnet Browser)." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OpenDiscordButton)), "Discord" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OpenDiscordButton)),  "Discord-Kanal für Feedback (öffnet Browser)." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OpenParadoxModsButton)), "Paradox Mods" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OpenParadoxModsButton)),  "Paradox-Mods-Seite (öffnet Browser)." },

                // Usage section
                { m_Setting.GetOptionGroupLocaleID(Setting.UsageGroup), "NUTZUNG" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.UsageSteps)),
                  "1. <Sichere zuerst deinen Spielstand!>\n" +
                  "2. <Klicke [Aktualisieren], um die aktuellen Statistiken zu sehen.>\n" +
                  "3. <[ ✓ ] Wähle die gewünschten Elemente über die Kästchen>\n" +
                  "4. <Klicke [Bürger bereinigen], um die Bereinigung zu starten.>" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.UsageSteps)), "" },

                // Notes block
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.UsageNotes)),
                  "Hinweise:\n" +
                  "• Dieser Mod läuft **nicht** automatisch; verwende **[Bürger bereinigen]** bei Bedarf.\n" +
                  "• Bei unerwartetem Verhalten zur ursprünglichen Speicherung zurückkehren." },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.UsageNotes)), "" },
            };
        }

        public void Unload() { }
    }
}
