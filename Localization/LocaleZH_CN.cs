// LocaleZH_CN.cs
using System.Collections.Generic;  // Dictionary
using Colossal;                    // IDictionarySource

namespace CitizenCleaner
{
    /// <summary>
    /// Simplified Chinese (zh-CN) locale entries
    /// </summary>
    public class LocaleZH_CN : IDictionarySource
    {
        private readonly Setting m_Setting;
        public LocaleZH_CN(Setting setting) { m_Setting = setting; }

        public IEnumerable<KeyValuePair<string, string>> ReadEntries(
            IList<IDictionaryEntryError> errors, Dictionary<string, int> indexCounts)
        {
            return new Dictionary<string, string>
            {
                // Mod name in Options menu list (keep Mod.Name so display stays consistent)
                { m_Setting.GetSettingsLocaleID(), Mod.Name },

                // Tabs
                { m_Setting.GetOptionTabLocaleID(Setting.kSection), "操作" },
                { m_Setting.GetOptionTabLocaleID(Setting.AboutTab), "关于" },

                // Groups
                { m_Setting.GetOptionGroupLocaleID(Setting.kFiltersGroup), "清理目标" },
                { m_Setting.GetOptionGroupLocaleID(Setting.kButtonGroup), "操作" },
                { m_Setting.GetOptionGroupLocaleID(Setting.InfoGroup), "信息" },
                { m_Setting.GetOptionGroupLocaleID(Setting.DebugGroup), "调试" },

                // Filter toggles
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.IncludeCorrupt)), "▪ 损坏的市民" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.IncludeCorrupt)),
                  "启用（默认）后，将统计并清理**损坏的**市民；\n" +
                  "即缺少 PropertyRenter 组件且不是无家可归者、通勤者、游客或搬离中的常住居民。\n\n" +
                  "损坏的市民是本模组的主要清理对象；数量过多会随时间造成问题。" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.IncludeMovingAwayNoPR)), "▪ 搬离中（租金 = 0）" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.IncludeMovingAwayNoPR)),
                  "启用后，将统计并清理当前**搬离中**且租金为 0 的市民（即无 PropertyRenter 组件）。\n\n" +
                  "拥有 PropertyRenter 或租金 > 0 的搬离中市民不会被移除。" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.IncludeCommuters)), "▪ 通勤者" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.IncludeCommuters)),
                  "启用后，将统计并清理**通勤者**。通勤者是不居住在你的城市、仅为工作而来往的市民。\n\n" +
                  "有时通勤者曾居住在你的城市，但因无家可归而搬离（游戏 1.2.5 版本新增的行为）。" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.IncludeHomeless)), "▪ 无家可归者" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.IncludeHomeless)),
                  "启用后，将统计并清理**无家可归者**。\n\n" +
                  "<注意>：删除无家可归者可能产生未知的副作用。" },

                // Buttons
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.CleanupEntitiesButton)), "清理市民" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.CleanupEntitiesButton)),
                  "<请先加载存档。>\n从不再拥有 PropertyRenter 组件的家庭中移除市民。\n" +
                  "清理同时包含你勾选的可选项 [ ✓ ]。\n\n" +
                  "**请注意**：这是权宜之计，可能损坏其他数据。请先备份你的存档！" },

                // Warning (confirmation)
                { m_Setting.GetOptionWarningLocaleID(nameof(Setting.CleanupEntitiesButton)),
                  "将永久删除在选项中勾选的项目。\n\n<请先备份你的存档！>\n是否继续？" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.RefreshCountsButton)), "刷新计数" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.RefreshCountsButton)),
                  "<要获取数据，请先加载存档。>\n" +
                  "更新所有实体计数以显示当前城市统计。\n" +
                  "清理后请让游戏继续运行一段时间。" },

                // Debug preview (add missing)
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.LogCorruptPreviewButton)), "日志 - 损坏 ID（前 10 个）" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.LogCorruptPreviewButton)),
                  "— 将前 25 个损坏市民的 ID **（Index:Version）** 写入日志，便于在 Scene Explorer 中核对。\n\n" +
                  "- **仅预览** — 不会删除任何内容。\n\n" +
                  "- 日志文件：\n" +
                  "%USERPROFILE%/AppData/LocalLow/Colossal Order/Cities Skylines II/logs/CitizenCleaner.log" },


                // Sentence UNDER the button (multiline)
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.DebugCorruptNote)),
                  "调试用：日志示例列表 — 不删除任何内容。\n" +
                  "把前 25 个损坏实体的 ID 写入日志。" },


                // Displays
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.CleanupStatusDisplay)), "状态" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.CleanupStatusDisplay)),
                  "显示清理状态。在清理进行中会实时更新；否则请点击 [刷新计数] 重新计算。\n\n" +
                  "“**Idle**” = 未在清理或尚未加载城市。\n" +
                  "“**Nothing to clean**” = 没有市民符合所选过滤条件（或你已清理完毕）。\n" +
                  "“**Complete**” = 上次清理已结束；直到你修改过滤条件或再次运行清理前保持该状态。" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.TotalCitizensDisplay)), "市民总数" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.TotalCitizensDisplay)),
                  "当前**参与模拟**的市民实体总数。\n\n" +
                  "该数字可能与人口数不同，因为其中可能包含损坏的实体。" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.CorruptedCitizensDisplay)),
                  "待清理的市民：请在上方勾选 [ ✓ ]" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.CorruptedCitizensDisplay)),
                  "当你点击 **[清理]** 时将要移除的市民数量，\n\n" +
                  "基于你在上方勾选的选项 [ ✓ ]。" },

                // Prompts (used by Setting.cs for placeholder text)
                { "CitizenCleaner/Prompt/RefreshCounts", "点击 [刷新计数]" },
                { "CitizenCleaner/Prompt/NoCity", "未加载城市" },
                { "CitizenCleaner/Prompt/Error",  "错误" },
                { "CitizenCleaner/Status/Progress", "正在清理… {0}" },
                { "CitizenCleaner/Status/Cleaning", "清理中… {0}" },

                // About tab fields
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.NameText)), "模组名称" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.NameText)), "本模组的显示名称。" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.VersionText)), "版本" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.VersionText)), "当前模组版本。" },

#if DEBUG
                // Only visible in DEBUG builds
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.InformationalVersionText)), "信息版本" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.InformationalVersionText)), "包含提交 ID 的版本号" },
#endif

                // About tab links (the three external link buttons)
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OpenGithubButton)),  "GitHub" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OpenGithubButton)),   "打开浏览器访问本模组的 GitHub 仓库。" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OpenDiscordButton)), "Discord" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OpenDiscordButton)),  "打开浏览器加入模组反馈的 Discord 频道。" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OpenParadoxModsButton)), "Paradox Mods" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OpenParadoxModsButton)),  "打开浏览器访问 Paradox Mods 页面。" },

                // About tab --> Usage section header & blocks
                { m_Setting.GetOptionGroupLocaleID(Setting.UsageGroup), "用法" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.UsageSteps)),
                  "1. <请先备份你的存档！>\n" +
                  "2. <点击 [刷新计数] 查看当前统计。>\n" +
                  "3. <使用复选框勾选> [ ✓ ] <要包含的项目>\n" +
                  "4. <点击 [清理市民] 执行清理。>" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.UsageSteps)), "" },

                // Notes block
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.UsageNotes)),
                  "注意：\n" +
                  "• 本模组**不会**自动运行；每次需要移除时请手动点击 **[清理市民]**。\n" +
                  "• 如出现异常行为，请还原到原始存档。" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.UsageNotes)), "" },
            };
        }

        public void Unload() { }
    }
}

