// LocaleIT.cs
using Colossal;                    // IDictionarySource, IDictionaryEntryError
using System.Collections.Generic;  // Dictionary, IEnumerable, IList

namespace CitizenEntityCleaner
{
    /// <summary>
    /// Italian locale entries (it-IT)
    /// </summary>
    public class LocaleIT : IDictionarySource
    {
        private readonly Setting m_Setting;
        public LocaleIT(Setting setting) { m_Setting = setting; }

        public IEnumerable<KeyValuePair<string, string>> ReadEntries(
            IList<IDictionaryEntryError> errors, Dictionary<string, int> indexCounts)
        {
            return new Dictionary<string, string>
            {
                // Mod name in Options menu list
                { m_Setting.GetSettingsLocaleID(), Mod.Name },

                // Tabs
                { m_Setting.GetOptionTabLocaleID(Setting.kSection), "Principale" },
                { m_Setting.GetOptionTabLocaleID(Setting.AboutTab), "Informazioni" },

                // Groups
                { m_Setting.GetOptionGroupLocaleID(Setting.kFiltersGroup), "Filtri" },
                { m_Setting.GetOptionGroupLocaleID(Setting.kButtonGroup), "Azioni" },
                { m_Setting.GetOptionGroupLocaleID(Setting.InfoGroup), "Info" },

                // Filter toggles
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.IncludeCorrupt)), "Includi cittadini corrotti" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.IncludeCorrupt)),
                  "Quando abilitato (predefinito), conta e pulisce i **cittadini corrotti**:\n" +
                  "residenti senza componente PropertyRenter (e non senzatetto, pendolari, turisti o in partenza).\n\n" +
                  "I cittadini corrotti sono il principale obiettivo di questa mod. Se sono troppi, col tempo possono causare problemi." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.IncludeHomeless)), "Includi senzatetto" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.IncludeHomeless)),
                  "Quando abilitato, conta e pulisce i **senzatetto**." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.IncludeCommuters)), "Includi pendolari" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.IncludeCommuters)),
                  "Quando abilitato, conta e pulisce i **pendolari**. I pendolari non vivono nella tua città ma viaggiano qui per lavoro.\n\n" +
                  "A volte erano residenti ma sono andati via per mancanza di alloggio (funzione introdotta nella versione 1.2.5 del gioco)." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.IncludeMovingAwayNoPR)), "Includi in partenza (senza affitto)" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.IncludeMovingAwayNoPR)),
                  "Quando abilitato, conta e pulisce i cittadini che **stanno andando via** e **non** hanno la componente PropertyRenter.\n\n" +
                  "Chi ha PropertyRenter viene mantenuto." },

                // Buttons (Main group)
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.CleanupEntitiesButton)), "Pulisci cittadini" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.CleanupEntitiesButton)),
                  "<Carica prima una città salvata.>\n" +
                  "Rimuove i cittadini dalle famiglie che non hanno più la componente PropertyRenter.\n" +
                  "La pulizia include anche gli elementi opzionali selezionati [ ✓ ].\n\n" +
                  "ATTENZIONE: è un workaround e potrebbe corrompere altri dati. Fai prima un backup!" },
                { m_Setting.GetOptionWarningLocaleID(nameof(Setting.CleanupEntitiesButton)),
                  "Elimina in modo permanente gli elementi selezionati nelle opzioni.\n\n<Per favore crea prima un backup!>\nContinuare?" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.RefreshCountsButton)), "Aggiorna" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.RefreshCountsButton)),
                  "<Carica prima una città salvata per vedere i numeri.>\n" +
                  "Aggiorna tutti i contatori per mostrare le statistiche attuali della città.\n" +
                  "Dopo la pulizia, lascia il gioco in esecuzione (non in pausa) per un minuto." },

                // Displays
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.CleanupStatusDisplay)), "Stato" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.CleanupStatusDisplay)),
                  "Mostra lo stato corrente della pulizia. Si aggiorna in tempo reale mentre la schermata delle impostazioni è aperta." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.TotalCitizensDisplay)), "Cittadini totali" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.TotalCitizensDisplay)),
                  "Numero totale di entità cittadino **attualmente nella simulazione.**\n\n" +
                  "Questo numero può non corrispondere alla popolazione perché può includere entità corrotte." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.CorruptedCitizensDisplay)),
                  "Cittadini da pulire: seleziona [ ✓ ] sopra" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.CorruptedCitizensDisplay)),
                  "Numero di entità cittadino rimosse quando clicchi **[Pulisci cittadini]**,\n\n" +
                  "in base alle caselle [ ✓ ] selezionate." },

                // About tab fields
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.NameText)), "Nome mod" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.NameText)), "Nome visualizzato di questa mod." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.VersionText)), "Versione" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.VersionText)), "Versione attuale della mod." },

#if DEBUG
                // Only visible in DEBUG builds
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.InformationalVersionText)), "Versione informativa" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.InformationalVersionText)), "Versione della mod con ID commit" },
#endif

                // About tab links (external)
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OpenGithubButton)),  "GitHub" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OpenGithubButton)),   "Repository GitHub della mod; si apre nel browser." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OpenDiscordButton)), "Discord" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OpenDiscordButton)),  "Canale Discord per feedback sulla mod; si apre nel browser." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OpenParadoxModsButton)), "Paradox Mods" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OpenParadoxModsButton)),  "Pagina Paradox Mods; si apre nel browser." },

                // Usage section
                { m_Setting.GetOptionGroupLocaleID(Setting.UsageGroup), "UTILIZZO" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.UsageSteps)),
                  "1. <Fai prima un backup del salvataggio!>\n" +
                  "2. <Clicca [Aggiorna] per vedere le statistiche correnti.>\n" +
                  "3. <Seleziona [ ✓ ] gli elementi da includere>\n" +
                  "4. <Clicca [Pulisci cittadini] per avviare la pulizia.>" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.UsageSteps)), "" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.UsageNotes)),
                  "Note:\n" +
                  "• Questa mod **non** si esegue automaticamente; usa **[Pulisci cittadini]** quando serve.\n" +
                  "• In caso di comportamenti imprevisti, ripristina il salvataggio originale." },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.UsageNotes)), "" },
            };
        }

        public void Unload() { }
    }
}
