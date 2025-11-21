// LocaleVI.cs
using System.Collections.Generic;  // Dictionary
using Colossal;                    // IDictionarySource

namespace CitizenCleaner
{
    /// <summary>
    /// Vietnamese locale entries (vi-VN)
    /// </summary>
    public class LocaleVI : IDictionarySource
    {
        private readonly Setting m_Setting;
        public LocaleVI(Setting setting) { m_Setting = setting; }

        public IEnumerable<KeyValuePair<string, string>> ReadEntries(
            IList<IDictionaryEntryError> errors, Dictionary<string, int> indexCounts)
        {
            return new Dictionary<string, string>
            {
                // Mod name in Options menu list
                { m_Setting.GetSettingsLocaleID(), Mod.Name },

                // Tabs
                { m_Setting.GetOptionTabLocaleID(Setting.kSection), "Thao tác" },
                { m_Setting.GetOptionTabLocaleID(Setting.AboutTab), "About" },

                // Groups
                { m_Setting.GetOptionGroupLocaleID(Setting.kFiltersGroup), "Mục tiêu dọn dẹp" },
                { m_Setting.GetOptionGroupLocaleID(Setting.kButtonGroup), "Thao tác" },
                { m_Setting.GetOptionGroupLocaleID(Setting.InfoGroup), "Thông tin" },
                { m_Setting.GetOptionGroupLocaleID(Setting.DebugGroup), "Gỡ lỗi" },

                // Filter toggles
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.IncludeCorrupt)), "▪ Công dân lỗi" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.IncludeCorrupt)),
                  "Khi bật (mặc định), sẽ đếm và dọn **công dân lỗi**;\n" +
                  "cư dân thiếu component PropertyRenter (và không phải vô gia cư, commuter, khách du lịch, hay đang rời đi).\n\n" +
                  "Công dân lỗi là mục tiêu chính của mod. Nếu thành phố có quá nhiều, lâu dần có thể gây vấn đề." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.IncludeMovingAwayNoPR)), "▪ Bỏ đi (Tiền thuê = 0)" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.IncludeMovingAwayNoPR)),
                  "Khi bật, sẽ đếm và dọn các công dân đang **Bỏ đi** với Tiền thuê = 0 (tức là không có component PropertyRenter).\n\n" +
                  "Công dân Bỏ đi có PropertyRenter hoặc Tiền thuê > 0 sẽ không bị xóa." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.IncludeCommuters)), "▪ Người đi làm (commuter)" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.IncludeCommuters)),
                  "Khi bật, sẽ đếm và dọn **commuter**. Commuter là người không sống trong thành phố của bạn nhưng vào thành phố để làm việc.\n\n" +
                  "Đôi khi, commuter từng sống trong thành phố nhưng đã chuyển đi vì vô gia cư (tính năng thêm từ phiên bản game 1.2.5)." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.IncludeHomeless)), "▪ Vô gia cư" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.IncludeHomeless)),
                  "Khi bật, sẽ đếm và dọn **người vô gia cư**.\n\n" +
                  "<CẨN THẬN>: xóa người vô gia cư có thể gây tác dụng phụ khó lường." },

                // Buttons
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.CleanupEntitiesButton)), "Dọn công dân" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.CleanupEntitiesButton)),
                  "Hãy mở thành phố đã lưu trước.\nXóa công dân khỏi các hộ gia đình không còn component PropertyRenter.\n" +
                  "Dọn dẹp cũng bao gồm các mục tùy chọn đã chọn [ ✓ ].\n\n" +
                  "**CẨN THẬN**: đây là cách tạm thời và có thể làm hỏng dữ liệu khác. Hãy sao lưu save trước!" },

                // Warning (confirmation)
                { m_Setting.GetOptionWarningLocaleID(nameof(Setting.CleanupEntitiesButton)),
                  "Xóa vĩnh viễn các mục đã chọn trong tùy chọn.\n\nHãy sao lưu save trước!\n Tiếp tục?" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.RefreshCountsButton)), "Làm mới số liệu" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.RefreshCountsButton)),
                  "<Hãy mở thành phố đã lưu để có số liệu.>\n" +
                  "Cập nhật toàn bộ số đếm để hiển thị thống kê hiện tại của thành phố.\n" +
                  "Sau khi dọn, cho game chạy (không tạm dừng) khoảng một phút." },

                // Debug preview - logs a sample list of corrupt citizens
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.LogCorruptPreviewButton)), "LOG - ID công dân lỗi (10 đầu)" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.LogCorruptPreviewButton)),
                  "- Thêm danh sách **10 ID công dân lỗi đầu tiên** vào file log **(Index:Version)** để đối chiếu bằng Scene Explorer.\n\n" +
                  "- **Chỉ xem trước** — không xóa gì.\n\n" +
                  "- File log ở:\n" +
                  "%USERPROFILE%/AppData/LocalLow/Colossal Order/Cities Skylines II/logs/CitizenCleaner.log" },

                // Sentence UNDER the button (multiline text row)
                // LabelLocale = inline body under the button
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.DebugCorruptNote)),
                  "Dùng để debug: ghi danh sách mẫu — không xóa gì.\n" +
                  "Liệt kê 10 ID đầu của công dân lỗi vào log." },

                // Displays
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.CleanupStatusDisplay)), "Trạng thái" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.CleanupStatusDisplay)),
                  "Hiển thị trạng thái dọn dẹp. Cập nhật trực tiếp khi đang dọn; nếu không, bấm [Làm mới số liệu] để tính lại.\n\n" +
                  "\"**Nghỉ**\" = chưa chạy dọn hoặc chưa mở city.\n" +
                  "\"**Không có gì để dọn**\" = không có công dân khớp bộ lọc đã chọn (hoặc bạn đã xóa hết).\n" +
                  "\"**Xong**\" = lần dọn trước đã hoàn tất; giữ nguyên cho đến khi đổi bộ lọc hoặc chạy dọn mới." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.TotalCitizensDisplay)), "Tổng công dân" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.TotalCitizensDisplay)),
                  "Tổng số thực thể công dân **đang có trong mô phỏng.**\n\n" +
                  "Con số này có thể khác dân số vì có thể gồm cả thực thể lỗi." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.CorruptedCitizensDisplay)),
                  "Công dân sẽ dọn: chọn [ ✓ ] ở trên" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.CorruptedCitizensDisplay)),
                  "Số công dân sẽ bị xóa khi bấm **[Dọn công dân]**,\n\n" +
                  "dựa trên các ô đã chọn [ ✓ ]." },

                // Prompts (used by Setting.cs for placeholder text)
                { "CitizenCleaner/Prompt/RefreshCounts", "Bấm [Làm mới số liệu]" },
                { "CitizenCleaner/Prompt/NoCity", "Chưa mở thành phố" },
                { "CitizenCleaner/Prompt/Error",  "Lỗi" },
                { "CitizenCleaner/Status/Progress", "Đang dọn dẹp… {0}" },
                { "CitizenCleaner/Status/Cleaning", "Đang dọn… {0}" },

                // About tab fields
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.NameText)), "Tên mod" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.NameText)), "Tên hiển thị của mod." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.VersionText)), "Phiên bản" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.VersionText)), "Phiên bản mod hiện tại." },

#if DEBUG
                // Only visible in DEBUG builds
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.InformationalVersionText)), "Phiên bản thông tin" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.InformationalVersionText)), "Phiên bản mod kèm Commit ID" },
#endif

                // About tab links (the three external link buttons)
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OpenGithubButton)),  "GitHub" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OpenGithubButton)),   "Kho GitHub của mod; mở trong trình duyệt." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OpenDiscordButton)), "Discord" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OpenDiscordButton)),  "Discord để góp ý về mod; mở trong trình duyệt." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OpenParadoxModsButton)), "Paradox Mods" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OpenParadoxModsButton)),  "Trang Paradox Mods; mở trong trình duyệt." },

                // About tab --> Usage section header & blocks
                { m_Setting.GetOptionGroupLocaleID(Setting.UsageGroup), "CÁCH DÙNG" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.UsageSteps)),
                  "1. <Hãy sao lưu save trước!>\n" +
                  "2. <Bấm [Làm mới số liệu] để xem thống kê hiện tại.>\n" +
                  "3. [ ✓ ] <Đánh dấu những mục muốn xử lý>\n" +
                  "4. <Bấm [Dọn công dân] để dọn thực thể.>" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.UsageSteps)), "" },

                // Notes block
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.UsageNotes)),
                  "Ghi chú:\n" +
                  "• Mod **không** chạy tự động; hãy dùng **[Dọn công dân]** mỗi lần muốn xóa.\n" +
                  "• Có thể quay lại save gốc nếu gặp hành vi bất thường." },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.UsageNotes)), "" },
            };
        }

        public void Unload() { }
    }
}
