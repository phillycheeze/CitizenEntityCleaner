// LocalePT_BR.cs
using System.Collections.Generic;  // Dictionary
using Colossal;                    // IDictionarySource

namespace CitizenCleaner
{
    /// <summary>
    /// Portuguese (Brazil) locale entries (pt-BR)
    /// </summary>
    public class LocalePT_BR : IDictionarySource
    {
        private readonly Setting m_Setting;
        public LocalePT_BR(Setting setting) { m_Setting = setting; }

        public IEnumerable<KeyValuePair<string, string>> ReadEntries(
            IList<IDictionaryEntryError> errors, Dictionary<string, int> indexCounts)
        {
            return new Dictionary<string, string>
            {
                // Mod name in Options menu list
                { m_Setting.GetSettingsLocaleID(), Mod.Name },

                // Tabs
                { m_Setting.GetOptionTabLocaleID(Setting.kSection), "Ações" },
                { m_Setting.GetOptionTabLocaleID(Setting.AboutTab), "About" },   // keep label in English

                // Groups
                { m_Setting.GetOptionGroupLocaleID(Setting.kFiltersGroup), "Alvos da limpeza" },
                { m_Setting.GetOptionGroupLocaleID(Setting.kButtonGroup), "Ações" },
                { m_Setting.GetOptionGroupLocaleID(Setting.InfoGroup), "Informações" },
                { m_Setting.GetOptionGroupLocaleID(Setting.DebugGroup), "Depuração" },

                // Filter toggles
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.IncludeCorrupt)), "▪ Cidadãos corrompidos" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.IncludeCorrupt)),
                  "Quando ativado (padrão), conta e limpa **cidadãos corrompidos**;\n" +
                  "residentes que não possuem o componente PropertyRenter (e que não são sem-teto, commuters, turistas ou estão mudando-se).\n\n" +
                  "Cidadãos corrompidos são o principal alvo deste mod. Se a cidade tiver muitos, isso pode causar problemas com o tempo." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.IncludeMovingAwayNoPR)), "▪ Mudando-se (Aluguel = 0)" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.IncludeMovingAwayNoPR)),
                  "Quando ativado, conta e limpa cidadãos que estão **Mudando-se** com Aluguel = 0 (ou seja, sem o componente PropertyRenter).\n\n" +
                  "Cidadãos Mudando-se com PropertyRenter ou Aluguel > 0 não são removidos." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.IncludeCommuters)), "▪ Commuters" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.IncludeCommuters)),
                  "Quando ativado, conta e limpa **commuters**. Commuters incluem cidadãos que não moram na sua cidade, mas viajam para trabalhar nela.\n\n" +
                  "Às vezes, commuters já moraram na cidade e se mudaram por virarem sem-teto (recurso adicionado na versão 1.2.5 do jogo)." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.IncludeHomeless)), "▪ Sem-teto" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.IncludeHomeless)),
                  "Quando ativado, conta e limpa **pessoas sem-teto**.\n\n" +
                  "<CUIDADO>: remover sem-teto pode causar efeitos colaterais desconhecidos." },

                // Buttons
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.CleanupEntitiesButton)), "Limpar cidadãos" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.CleanupEntitiesButton)),
                  "Carregue uma cidade salva antes.\nRemove cidadãos de domicílios que não têm mais o componente PropertyRenter.\n" +
                  "A limpeza também inclui quaisquer itens opcionais marcados [ ✓ ].\n\n" +
                  "**CUIDADO**: isto é um contorno e pode corromper outros dados. Faça backup do seu save primeiro!" },

                // Warning (confirmation)
                { m_Setting.GetOptionWarningLocaleID(nameof(Setting.CleanupEntitiesButton)),
                  "Excluir permanentemente os itens selecionados nas opções.\n\nFaça backup do seu save antes!\n Continuar?" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.RefreshCountsButton)), "Atualizar contagens" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.RefreshCountsButton)),
                  "<Carregue uma cidade salva para ver os números.>\n" +
                  "Atualiza todas as contagens para mostrar as estatísticas atuais da cidade.\n" +
                  "Após a limpeza, deixe o jogo rodar sem pausa por um minuto." },

                // Debug preview - logs a sample list of corrupt citizens
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.LogCorruptPreviewButton)), "LOG - IDs corrompidos (10 primeiros)" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.LogCorruptPreviewButton)),
                  "- Adiciona a lista dos **10 primeiros IDs de cidadãos corrompidos** ao arquivo de log **(Index:Version)** para conferência no Scene Explorer.\n\n" +
                  "- **Somente prévia** — nada é excluído.\n\n" +
                  "- Arquivo de log em:\n" +
                  "%USERPROFILE%/AppData/LocalLow/Colossal Order/Cities Skylines II/logs/CitizenCleaner.log" },

                // Sentence UNDER the button (multiline text row)
                // LabelLocale = inline body under the button
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.DebugCorruptNote)),
                  "Uso de debug: registrar lista de amostra — nada é excluído.\n" +
                  "Listar os 10 primeiros IDs de entidades corrompidas no log." },

                // Displays
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.CleanupStatusDisplay)), "Status" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.CleanupStatusDisplay)),
                  "Mostra o status da limpeza. Atualiza em tempo real durante uma limpeza ativa; caso contrário, pressione [Atualizar contagens] para recalcular.\n\n" +
                  "\"**Parado**\" = nenhuma limpeza em execução ou nenhuma cidade carregada.\n" +
                  "\"**Nada para limpar**\" = nenhum cidadão corresponde aos filtros selecionados (ou você já os removeu).\n" +
                  "\"**Concluído**\" = a última limpeza terminou; permanece até você mudar os filtros ou iniciar uma nova limpeza." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.TotalCitizensDisplay)), "Total de cidadãos" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.TotalCitizensDisplay)),
                  "Número total de entidades de cidadãos **atualmente na simulação.**\n\n" +
                  "Esse número pode diferir da sua população porque pode incluir entidades corrompidas." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.CorruptedCitizensDisplay)),
                  "Cidadãos a limpar: selecione [ ✓ ] acima" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.CorruptedCitizensDisplay)),
                  "Quantidade de entidades de cidadãos que serão removidas ao clicar em **[Limpar cidadãos]**,\n\n" +
                  "com base nas caixas selecionadas [ ✓ ]." },

                // Prompts (used by Setting.cs for placeholder text)
                { "CitizenCleaner/Prompt/RefreshCounts", "Clique em [Atualizar contagens]" },
                { "CitizenCleaner/Prompt/NoCity", "Nenhuma cidade carregada" },
                { "CitizenCleaner/Prompt/Error",  "Erro" },
                { "CitizenCleaner/Status/Progress", "Limpeza em andamento… {0}" },
                { "CitizenCleaner/Status/Cleaning", "Limpando… {0}" },

                // About tab fields
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.NameText)), "Nome do mod" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.NameText)), "Nome exibido deste mod." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.VersionText)), "Versão" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.VersionText)), "Versão atual do mod." },

#if DEBUG
                // Only visible in DEBUG builds
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.InformationalVersionText)), "Versão informativa" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.InformationalVersionText)), "Versão do mod com o ID do commit" },
#endif

                // About tab links (the three external link buttons)
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OpenGithubButton)),  "GitHub" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OpenGithubButton)),   "Repositório do mod no GitHub; abre no navegador." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OpenDiscordButton)), "Discord" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OpenDiscordButton)),  "Discord para feedback sobre o mod; abre no navegador." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OpenParadoxModsButton)), "Paradox Mods" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OpenParadoxModsButton)),  "Site Paradox Mods; abre no navegador." },

                // About tab --> Usage section header & blocks
                { m_Setting.GetOptionGroupLocaleID(Setting.UsageGroup), "USO" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.UsageSteps)),
                  "1. <Faça backup do save primeiro!>\n" +
                  "2. <Clique em [Atualizar contagens] para ver as estatísticas atuais.>\n" +
                  "3. [ ✓ ] <Marque os itens a incluir usando as caixas>\n" +
                  "4. <Clique em [Limpar cidadãos] para limpar as entidades.>" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.UsageSteps)), "" },

                // Notes block
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.UsageNotes)),
                  "Notas:\n" +
                  "• Este mod **não** roda automaticamente; use **[Limpar cidadãos]** sempre que quiser remover.\n" +
                  "• Volte ao save original se precisar por comportamento inesperado." },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.UsageNotes)), "" },
            };
        }

        public void Unload() { }
    }
}
