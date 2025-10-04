// LocaleKO.cs
using System.Collections.Generic;  // Dictionary
using Colossal;                    // IDictionarySource

namespace CitizenEntityCleaner
{
    /// <summary>
    /// Korean locale entries (ko-KR)
    /// </summary>
    public class LocaleKO : IDictionarySource
    {
        private readonly Setting m_Setting;
        public LocaleKO(Setting setting) { m_Setting = setting; }

        public IEnumerable<KeyValuePair<string, string>> ReadEntries(
            IList<IDictionaryEntryError> errors, Dictionary<string, int> indexCounts)
        {
            return new Dictionary<string, string>
            {
                // Mod name in Options menu list
                { m_Setting.GetSettingsLocaleID(), Mod.Name },

                // Tabs
                { m_Setting.GetOptionTabLocaleID(Setting.kSection), "작업" },
                { m_Setting.GetOptionTabLocaleID(Setting.AboutTab), "정보" },

                // Groups
                { m_Setting.GetOptionGroupLocaleID(Setting.kFiltersGroup), "정리 대상" },
                { m_Setting.GetOptionGroupLocaleID(Setting.kButtonGroup), "작업" },
                { m_Setting.GetOptionGroupLocaleID(Setting.InfoGroup), "정보" },
                { m_Setting.GetOptionGroupLocaleID(Setting.DebugGroup), "디버그" },

                // Filter toggles
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.IncludeCorrupt)), "▪ 손상된 시민" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.IncludeCorrupt)),
                  "활성화(기본값)하면 **손상**된 시민을 집계하고 정리합니다.\n" +
                  "PropertyRenter 구성요소가 없고(노숙자·통근자·이주 중이 아님) 거주 중인 시민이 대상입니다.\n\n" +
                  "손상된 시민은 이 모드의 주요 대상입니다. 도시 내에 과도하게 존재하면 시간이 지나며 문제를 야기할 수 있습니다." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.IncludeMovingAwayNoPR)), "▪ 이주 중 (Rent = 0)" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.IncludeMovingAwayNoPR)),
                  "활성화 시, 현재 **이주 중**이며 Rent = 0(= PropertyRenter 없음)인 시민을 집계하고 정리합니다.\n\n" +
                  "PropertyRenter가 있거나 Rent > 0 인 이주 중 시민은 제거되지 않습니다." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.IncludeCommuters)), "▪ 통근자" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.IncludeCommuters)),
                  "활성화 시, **통근자** 시민을 집계하고 정리합니다. 통근자는 이 도시에 거주하지 않지만 일하러 드나드는 사람을 의미합니다.\n\n" +
                  "통근자가 과거에 이 도시에 살았지만 노숙으로 전출되었을 수도 있습니다(게임 버전 1.2.5 기능)." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.IncludeHomeless)), "▪ 홈리스" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.IncludeHomeless)),
                  "활성화 시, **홈리스** 시민을 집계하고 정리합니다.\n\n" +
                  "<주의>: 홈리스를 삭제하면 알 수 없는 부작용이 발생할 수 있습니다." },

                // Buttons
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.CleanupEntitiesButton)), "시민 정리" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.CleanupEntitiesButton)),
                  "먼저 저장된 도시를 불러오세요.\nPropertyRenter 구성요소가 더 이상 없는 가구에서 시민을 제거합니다.\n" +
                  "정리에는 [ ✓ ] 로 선택한 선택 항목도 포함됩니다.\n\n" +
                  "**주의**: 이는 우회 방법이므로 다른 데이터가 손상될 수 있습니다. 먼저 저장 파일을 백업하세요!" },

                // Warning (confirmation)
                { m_Setting.GetOptionWarningLocaleID(nameof(Setting.CleanupEntitiesButton)),
                  "옵션에서 선택한 항목을 영구적으로 삭제합니다.\n\n먼저 저장 파일을 백업하세요!\n 계속하시겠습니까?" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.RefreshCountsButton)), "개수 새로고침" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.RefreshCountsButton)),
                  "＜수치를 보려면 먼저 저장된 도시를 불러오세요.＞\n" +
                  "모든 엔티티 개수를 갱신하여 현재 도시 통계를 표시합니다.\n" +
                  "정리 후에는 잠시 동안 게임을 일시정지 해제 상태로 두세요." },

                // Debug preview - logs a sample list of corrupt citizens
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.LogCorruptPreviewButton)), "로그 - 손상 ID(처음 10개)" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.LogCorruptPreviewButton)),
                  "- 첫 **10개 손상 시민 ID** 를 로그 파일에 **(Index:Version)** 형식으로 추가합니다( Scene Explorer 교차 확인용 ).\n\n" +
                  "- **미리보기 전용** — 아무것도 삭제하지 않습니다.\n\n" +
                  "- 로그 파일 위치:\n" +
                  "%USERPROFILE%/AppData/LocalLow/Colossal Order/Cities Skylines II/logs/CitizenEntityCleaner.log" },

                // Sentence UNDER the button (multiline text row)
                // LabelLocale = inline body under the button
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.DebugCorruptNote)),
                  "디버그 용도: 샘플 목록을 로그로 출력 — 아무것도 삭제하지 않음.\n" +
                  "손상된 엔티티의 처음 10개 ID를 로그에 나열합니다." },

                // Displays
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.CleanupStatusDisplay)), "상태" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.CleanupStatusDisplay)),
                  "정리 상태를 표시합니다. 실행 중에는 실시간으로 갱신되며, 실행 중이 아니면 [개수 새로고침]으로 재계산하세요.\n\n" +
                  "\"**Idle**\" = 정리 실행 중 아님 또는 도시 미로드.\n" +
                  "\"**Nothing to clean**\" = 선택한 필터에 일치하는 시민 없음(또는 이미 제거됨).\n" +
                  "\"**Complete**\" = 마지막 정리 완료. 필터 변경 또는 새 정리 실행 전까지 유지됩니다." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.TotalCitizensDisplay)), "전체 시민 수" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.TotalCitizensDisplay)),
                  "현재 시뮬레이션에 존재하는 시민 엔티티의 총수.\n\n" +
                  "손상 엔티티를 포함할 수 있으므로 인구 수와 다를 수 있습니다." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.CorruptedCitizensDisplay)),
                  "정리할 시민: 위에서 [ ✓ ] 선택" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.CorruptedCitizensDisplay)),
                  "「**정리**」를 클릭할 때 제거될 시민 엔티티 수입니다.\n\n" +
                  "선택한 체크박스 [ ✓ ] 에 따라 달라집니다." },

                // Prompts (used by Setting.cs for placeholder text)
                { "CitizenEntityCleaner/Prompt/RefreshCounts", "[개수 새로고침] 클릭" },
                { "CitizenEntityCleaner/Prompt/NoCity", "도시가 로드되지 않음" },
                { "CitizenEntityCleaner/Prompt/Error",  "오류" },
                { "CitizenEntityCleaner/Status/Progress", "정리 진행 중… {0}" },
                { "CitizenEntityCleaner/Status/Cleaning", "정리 중… {0}" },

                // About tab fields
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.NameText)), "모드 이름" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.NameText)), "이 모드의 표시 이름." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.VersionText)), "버전" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.VersionText)), "현재 모드 버전." },

#if DEBUG
                // Only visible in DEBUG builds
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.InformationalVersionText)), "정보 버전" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.InformationalVersionText)), "커밋 ID가 포함된 모드 버전" },
#endif

                // About tab links (the three external link buttons)
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OpenGithubButton)),  "GitHub" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OpenGithubButton)),   "이 모드의 GitHub 저장소. 브라우저에서 열립니다." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OpenDiscordButton)), "Discord" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OpenDiscordButton)),  "모드 피드백용 Discord. 브라우저에서 열립니다." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OpenParadoxModsButton)), "Paradox Mods" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OpenParadoxModsButton)),  "Paradox Mods 웹사이트. 브라우저에서 열립니다." },

                // About tab --> Usage section header & blocks
                { m_Setting.GetOptionGroupLocaleID(Setting.UsageGroup), "사용법" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.UsageSteps)),
                  "1. ＜먼저 저장 파일을 백업!＞\n" +
                  "2. ＜[개수 새로고침]을 클릭하여 현재 통계를 확인＞\n" +
                  "3. [ ✓ ] ＜체크박스로 포함할 항목 선택＞\n" +
                  "4. ＜[시민 정리]를 클릭하여 엔티티 정리＞" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.UsageSteps)), "" },

                // Notes block
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.UsageNotes)),
                  "참고:\n" +
                  "• 이 모드는 **자동으로** 실행되지 않습니다. 제거가 필요할 때마다 **[시민 정리]** 를 사용하세요.\n" +
                  "• 예기치 않은 동작이 발생하면 원본 저장으로 되돌리세요." },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.UsageNotes)), "" },
            };
        }

        public void Unload() { }
    }
}
