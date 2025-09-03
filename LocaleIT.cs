// LocaleIT.cs
using Colossal;                    // IDictionarySource
using System.Collections.Generic;  // Dictionary

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
                  "Se abilitato (predefinito), conta e ripulisce i **cittadini corrotti**;\n" +
                  "residenti senza il componente PropertyRenter (e che non siano senzatetto, pendolari, turisti o in partenza).\n\n" +
                  "I cittadini corrotti sono l’obiettivo principale di questa mod. Se la città ne contiene troppi, nel tempo possono causare problemi." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.IncludeHomeless)), "Includi senzatetto" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.IncludeHomeless)),
                  "Se abilitato, conta e ripulisce i **senzatetto**." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.IncludeCommuters)), "Includi pendolari" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.IncludeCommuters)),
                  "Se abilitato, conta e ripulisce i **pendolari**. I pendolari non vivono nella tua città ma viaggiano per lavorare.\n\n" +
                  "A volte i pendolari vivevano qui in passato e sono andati via per mancanza di alloggio (funzione introdotta con la versione di gioco 1.2.5)." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.IncludeMovingAwayNoPR)), "Includi in partenza (senza affitto)" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.IncludeMovingAwayNoPR)),
                  "Se abilitato, conta e ripulisce i cittadini **in partenza** che **non** hanno il componente PropertyRenter.\n\n" +
                  "I cittadini in partenza con PropertyRenter vengono conservati e non sono inclusi." },

                // Buttons (Main group)
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.CleanupEntitiesButton)), "Pulisci cittadini" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.CleanupEntitiesButton)),
                  "<Carica prima una città salvata.>\nRimuove i cittadini dai nuclei familiari che non hanno più il componente PropertyRenter.\n" +
                  "La pulizia include anche eventuali elementi opzionali selezionati [ ✓ ].\n\n" +
                  "**ATTENZIONE**: questa è una soluzione di ripiego e può danneggiare altri dati. Crea prima un backup del tuo salvataggio!" },
                { m_Setting.GetOptionWarningLocaleID(nameof(Setting.CleanupEntitiesButton)),
                  "Elimina in modo permanente gli elementi selezionati nelle opzioni.\n\n<Per favore, esegui prima un backup!>\nContinuare?" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.RefreshCountsButton)), "Aggiorna conteggi" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.RefreshCountsButton)),
                  "<Carica prima una città salvata per ottenere i numeri.>\n" +
                  "Aggiorna tutti i conteggi per mostrare le statistiche correnti della città.\n" +
                  "Dopo la pulizia, lascia il gioco in esecuzione per un minuto senza pausa." },

                // Displays
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.CleanupStatusDisplay)), "Stato" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.CleanupStatusDisplay)),
                  "Mostra lo stato della pulizia. Si aggiorna in tempo reale durante una pulizia attiva; altrimenti premi [Aggiorna conteggi] per ricalcolare.\n\n" +
                  "\"**Idle**\" = nessuna pulizia in corso o nessuna città ancora caricata.\n" +
                  "\"**Nothing to clean**\" = nessun cittadino corrisponde ai filtri selezionati (oppure li hai già rimossi).\n" +
                  "\"**Complete**\" = l’ultima pulizia è terminata; persiste finché non cambi i filtri o avvii una nuova pulizia." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.TotalCitizensDisplay)), "Cittadini totali" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.TotalCitizensDisplay)),
                  "Numero totale di entità cittadino **attualmente nella simulazione.**\n\n" +
                  "Questo numero può differire dalla popolazione perché può includere entità corrotte." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.CorruptedCitizensDisplay)),
                  "Cittadini da pulire: seleziona [ ✓ ] sopra" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.CorruptedCitizensDisplay)),
                  "Numero di entità cittadino che verranno rimosse quando fai clic su **[Pulisci cittadini]**,\n\n" +
                  "in base alle caselle selezionate [ ✓ ]." },

                // Prompts (used by Setting.cs for placeholder text)
                { "CitizenEntityCleaner/Prompt/RefreshCounts", "Fai clic su [Aggiorna conteggi]" },

                // About tab fields
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.NameText)), "Nome mod" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.NameText)), "Nome visualizzato di questa mod." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.VersionText)), "Versione" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.VersionText)), "Versione corrente della mod." },

#if DEBUG
                // Only visible in DEBUG builds
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.InformationalVersionText)), "Versione informativa" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.InformationalVersionText)), "Versione della mod con ID commit" },
#endif

                // About tab links (external link buttons)
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OpenGithubButton)),  "GitHub" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OpenGithubButton)),   "Repository GitHub della mod; si apre nel browser." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OpenDiscordButton)), "Discord" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OpenDiscordButton)),  "Canale Discord per feedback sulla mod; si apre nel browser." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OpenParadoxModsButton)), "Paradox Mods" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OpenParadoxModsButton)),  "Sito Paradox Mods; si apre nel browser." },

                // About tab --> Usage section header & blocks
                { m_Setting.GetOptionGroupLocaleID(Setting.UsageGroup), "UTILIZZO" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.UsageSteps)),
                  "1. <Esegui prima il backup del salvataggio!>\n" +
                  "2. <Fai clic su [Aggiorna conteggi] per vedere le statistiche correnti.>\n" +
                  "3. <[ ✓ ] Seleziona gli elementi da includere usando le caselle>\n" +
                  "4. <Fai clic su [Pulisci cittadini] per ripulire le entità.>" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.UsageSteps)), "" },

                // Notes block
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.UsageNotes)),
                  "Note:\n" +
                  "• Questa mod **non** funziona automaticamente; usa **[Pulisci cittadini]** ogni volta che vuoi rimuovere elementi.\n" +
                  "• In caso di comportamenti imprevisti, torna al salvataggio originale." },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.UsageNotes)), "" },
            };
        }

        public void Unload() { }
    }
}
