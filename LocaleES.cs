// LocaleES.cs
using Colossal;                    // IDictionarySource
using System.Collections.Generic;  // Dictionary

namespace CitizenEntityCleaner
{
    /// <summary>
    /// Spanish locale entries
    /// </summary>
    public class LocaleES : IDictionarySource
    {
        private readonly Setting m_Setting;
        public LocaleES(Setting setting) { m_Setting = setting; }

        public IEnumerable<KeyValuePair<string, string>> ReadEntries(
            IList<IDictionaryEntryError> errors, Dictionary<string, int> indexCounts)
        {
            return new Dictionary<string, string>
            {
                // Mod name in Options menu list
                { m_Setting.GetSettingsLocaleID(), Mod.Name },

                // Tabs
                { m_Setting.GetOptionTabLocaleID(Setting.kSection), "Principal" },
                { m_Setting.GetOptionTabLocaleID(Setting.AboutTab), "Acerca de" },

                // Groups
                { m_Setting.GetOptionGroupLocaleID(Setting.kFiltersGroup), "Filtros" },
                { m_Setting.GetOptionGroupLocaleID(Setting.kButtonGroup), "General" },
                { m_Setting.GetOptionGroupLocaleID(Setting.InfoGroup), "Información" },

                // Filter toggles
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.IncludeCorrupt)), "Incluir ciudadanos corruptos" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.IncludeCorrupt)),
                  "Cuando está activado (por defecto), cuenta y limpia ciudadanos **corruptos**;\n" +
                  "residentes que no tienen el componente PropertyRenter (y que no son sin hogar, commuters, turistas ni en mudanza).\n\n" +
                  "Los corruptos son el objetivo principal del mod. Si hay demasiados, puede causar problemas con el tiempo." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.IncludeHomeless)), "Incluir sin hogar" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.IncludeHomeless)),
                  "Cuando está activado, cuenta y limpia ciudadanos **sin hogar**." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.IncludeCommuters)), "Incluir commuters" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.IncludeCommuters)),
                  "Cuando está activado, cuenta y limpia **commuters**. Son ciudadanos que no viven en tu ciudad pero viajan para trabajar.\n\n" +
                  "A veces fueron residentes que se marcharon tras quedarse sin hogar (añadido en la versión 1.2.5 del juego)." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.IncludeMovingAwayNoPR)), "Incluir en mudanza (sin alquiler)" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.IncludeMovingAwayNoPR)),
                  "Cuando está activado, cuenta y limpia ciudadanos **en mudanza** que **no** tienen el componente PropertyRenter.\n\n" +
                  "Los que sí tienen PropertyRenter se conservan y no se incluyen." },

                // Buttons (Main/General group)
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.CleanupEntitiesButton)), "Limpiar ciudadanos" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.CleanupEntitiesButton)),
                  "<Carga primero una ciudad guardada.>\nElimina ciudadanos de hogares que ya no tienen el componente PropertyRenter.\n" +
                  " La limpieza también incluye los elementos opcionales marcados [ ✓ ].\n\n" +
                  "¡CUIDADO! Es un apaño y puede corromper otros datos. ¡Haz una copia de seguridad antes!" },
                { m_Setting.GetOptionWarningLocaleID(nameof(Setting.CleanupEntitiesButton)),
                  "Eliminará permanentemente los elementos seleccionados en las opciones.\n\n<¡Haz copia de seguridad primero!>\n¿Continuar?" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.RefreshCountsButton)), "Actualizar recuentos" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.RefreshCountsButton)),
                  "<Carga una ciudad guardada para ver números.>\nActualiza todos los recuentos para mostrar las estadísticas actuales.\n" +
                  "Tras limpiar, deja el juego un minuto sin pausar." },

                // Displays (read-only)
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.CleanupStatusDisplay)), "Estado" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.CleanupStatusDisplay)),
                  "Muestra el estado actual de la limpieza. Se actualiza en directo mientras las opciones están abiertas." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.TotalCitizensDisplay)), "Ciudadanos totales" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.TotalCitizensDisplay)),
                  "Número total de entidades de ciudadanos **actualmente en la simulación.**\n\n" +
                  "Puede no coincidir con la población porque puede incluir entidades corruptas." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.CorruptedCitizensDisplay)),
                  "Ciudadanos a limpiar: marca [ ✓ ] arriba" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.CorruptedCitizensDisplay)),
                  "Número de entidades que se eliminarán al hacer clic en **[Limpiar]**,\n\n" +
                  "según las casillas [ ✓ ] seleccionadas." },

                // About tab fields
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.NameText)), "Nombre del mod" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.NameText)), "Nombre visible de este mod." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.VersionText)), "Versión" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.VersionText)), "Versión actual del mod." },

#if DEBUG
                // Only visible in DEBUG builds
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.InformationalVersionText)), "Versión informativa" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.InformationalVersionText)), "Versión del mod con ID de commit" },
#endif

                // External link buttons (About tab)
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OpenGithubButton)),  "GitHub" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OpenGithubButton)),   "Repositorio GitHub del mod; se abre en el navegador." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OpenDiscordButton)), "Discord" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OpenDiscordButton)),  "Chat de Discord para comentarios; se abre en el navegador." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OpenParadoxModsButton)), "Paradox Mods" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OpenParadoxModsButton)),  "Página de Paradox Mods; se abre en el navegador." },

                // Usage section (header + blocks)
                { m_Setting.GetOptionGroupLocaleID(Setting.UsageGroup), "USO" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.UsageSteps)),
                  "1. <¡Haz una copia de seguridad de tu partida!>\n" +
                  "2. <Haz clic en [Actualizar recuentos] para ver las estadísticas.>\n" +
                  "3. <[ ✓ ] Selecciona qué incluir usando las casillas>\n" +
                  "4. <Haz clic en [Limpiar ciudadanos] para limpiar entidades.>" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.UsageSteps)), "" },

                // Notes block
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.UsageNotes)),
                  "Notas:\n" +
                  "• Este mod **no** se ejecuta automáticamente; usa **[Limpiar ciudadanos]** cada vez.\n" +
                  "• Vuelve a una copia de seguridad si aparece un comportamiento inesperado." },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.UsageNotes)), "" },
            };
        }

        public void Unload() { }
    }
}
