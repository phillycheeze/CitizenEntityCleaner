// LocaleES.cs
using System.Collections.Generic;  // Dictionary
using Colossal;                    // IDictionarySource

namespace CitizenCleaner
{
    /// <summary>
    /// Spanish locale entries (es-ES)
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
                { m_Setting.GetOptionTabLocaleID(Setting.kSection), "Acciones" },
                { m_Setting.GetOptionTabLocaleID(Setting.AboutTab), "Info" },

                // Groups
                { m_Setting.GetOptionGroupLocaleID(Setting.kFiltersGroup), "Grupos a eliminar" },
                { m_Setting.GetOptionGroupLocaleID(Setting.kButtonGroup), "Acciones" },
                { m_Setting.GetOptionGroupLocaleID(Setting.InfoGroup), "Info" },
                { m_Setting.GetOptionGroupLocaleID(Setting.DebugGroup), "Depuración" },

                // Filter toggles
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.IncludeCorrupt)), "Ciudadanos corruptos" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.IncludeCorrupt)),
                  "Cuando está activado (predeterminado), cuenta y limpia ciudadanos **Corruptos**;\n" +
                  "residentes que no tienen el componente PropertyRenter (y que no sean sin hogar, pendulares, turistas o en mudanza).\n\n" +
                  "Los ciudadanos corruptos son el objetivo principal de este mod. Si hay demasiados en la ciudad, pueden causar problemas con el tiempo." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.IncludeMovingAwayNoPR)), "En mudanza (salida, Rent = 0)" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.IncludeMovingAwayNoPR)),
                  "Si está activado, cuenta y elimina a los ciudadanos con estado **En mudanza** y Rent = 0 (es decir, sin componente PropertyRenter).\n\n" +
                  "Los ciudadanos en mudanza con PropertyRenter o con Rent > 0 no se eliminan." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.IncludeCommuters)), "Pendulares" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.IncludeCommuters)),
                  "Cuando está activado, cuenta y limpia a los **Pendulares**. Los pendulares no viven en tu ciudad, pero se desplazan para trabajar.\n\n" +
                  "A veces vivían aquí y se marcharon por quedarse sin hogar (función añadida en la versión 1.2.5 del juego)." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.IncludeHomeless)), "Ciudadanos sin hogar" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.IncludeHomeless)),
                  "Cuando está activado, cuenta y limpia a los **Ciudadanos Sin Hogar**.\n\n" +
                  "<CUIDADO>: eliminar ciudadanos sin hogar puede causar efectos inesperados." },

                // Buttons
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.CleanupEntitiesButton)), "Limpiar ciudadanos" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.CleanupEntitiesButton)),
                  "<Carga primero una ciudad guardada.>\nElimina ciudadanos de hogares que ya no tienen el componente PropertyRenter.\n" +
                  "La limpieza también incluye cualquier elemento opcional marcado [ ✓ ].\n\n" +
                  "**CUIDADO**: esto es un apaño y puede corromper otros datos. ¡Haz una copia de seguridad de tu partida primero!" },

                // Warning (confirmation)
                { m_Setting.GetOptionWarningLocaleID(nameof(Setting.CleanupEntitiesButton)),
                  "Los elementos seleccionados en las opciones se eliminarán de forma permanente.\n\n<Por favor, haz antes una copia de seguridad.>\n¿Continuar?" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.RefreshCountsButton)), "Actualizar recuentos" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.RefreshCountsButton)),
                  "<Carga primero una ciudad para obtener cifras.>\n" +
                  "Actualiza todos los contadores para mostrar las estadísticas actuales de la ciudad.\n" +
                  "Después de limpiar, deja el juego sin pausa durante un minuto." },

                // Debug preview
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.LogCorruptPreviewButton)), "LOG - IDs corruptas (primeras 10)" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.LogCorruptPreviewButton)),
                  "- Añade al registro las primeras 25 IDs de ciudadanos corruptos **(Index:Version)** para verificar en Scene Explorer.\n\n" +
                  "- **Solo vista previa** — no elimina nada.\n\n" +
                  "- Archivo de registro:\n" +
                  "%USERPROFILE%/AppData/LocalLow/Colossal Order/Cities Skylines II/logs/CitizenCleaner.log" },

                // Sentence UNDER the button (multiline)
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.DebugCorruptNote)),
                  "Uso de depuración: lista de muestra — no se elimina nada.\n" +
                  "Escribe en el registro las 25 primeras ID de entidades corruptas." },

                // Displays
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.CleanupStatusDisplay)), "Estado" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.CleanupStatusDisplay)),
                  "Muestra el estado de la limpieza. Se actualiza en vivo durante una limpieza activa; en otro caso pulsa [Actualizar recuentos] para recalcular.\n\n" +
                  "\"**Idle**\" = no hay limpieza en curso o aún no hay ciudad cargada.\n" +
                  "\"**Nothing to clean**\" = ningún ciudadano coincide con los filtros seleccionados (o ya los eliminaste).\n" +
                  "\"**Complete**\" = la última limpieza terminó; permanece hasta que cambies filtros o ejecutes una nueva limpieza." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.TotalCitizensDisplay)), "Total de ciudadanos" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.TotalCitizensDisplay)),
                  "Número total de entidades de ciudadanos **actualmente en la simulación.**\n\n" +
                  "Puede diferir de tu población porque puede incluir entidades corruptas." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.CorruptedCitizensDisplay)),
                  "Ciudadanos a limpiar: marca [ ✓ ] arriba" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.CorruptedCitizensDisplay)),
                  "Número de entidades de ciudadanos que se eliminarán al pulsar **[Limpiar ciudadanos]**, \n\n" +
                  "según las casillas [ ✓ ] seleccionadas." },

                // Prompts (used by Setting.cs for placeholder text)
                { "CitizenCleaner/Prompt/RefreshCounts", "Haz clic en [Actualizar recuentos]" },
                { "CitizenCleaner/Prompt/NoCity", "No hay ciudad cargada" },
                { "CitizenCleaner/Prompt/Error",  "Error" },
                { "CitizenCleaner/Status/Progress", "Limpieza en curso… {0}" },
                { "CitizenCleaner/Status/Cleaning", "Limpiando… {0}" },


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

                // About tab links (the three external link buttons)
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OpenGithubButton)),  "GitHub" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OpenGithubButton)),   "Repositorio del mod en GitHub; se abre en el navegador." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OpenDiscordButton)), "Discord" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OpenDiscordButton)),  "Canal de Discord para comentarios; se abre en el navegador." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OpenParadoxModsButton)), "Paradox Mods" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OpenParadoxModsButton)),  "Página de Paradox Mods; se abre en el navegador." },

                // About tab --> Usage section header & blocks
                { m_Setting.GetOptionGroupLocaleID(Setting.UsageGroup), "USO" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.UsageSteps)),
                  "1. <¡Haz primero una copia de seguridad de tu partida!>\n" +
                  "2. <Haz clic en [Actualizar recuentos] para ver las estadísticas actuales.>\n" +
                  "3. [ ✓ ] <Selecciona los elementos a incluir con las casillas>\n" +
                  "4. <Haz clic en [Limpiar Ciudadanos] para iniciar la limpieza.>" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.UsageSteps)), "" },

                // Notes block
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.UsageNotes)),
                  "Notas:\n" +
                  "• Este mod **no** se ejecuta automáticamente; usa **[Limpiar ciudadanos]** cada vez que quieras eliminar.\n" +
                  "• Vuelve a tu ciudad guardada original si es necesario por comportamiento inesperado." },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.UsageNotes)), "" },
            };
        }

        public void Unload() { }
    }
}
