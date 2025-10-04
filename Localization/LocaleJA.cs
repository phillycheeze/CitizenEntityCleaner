// LocaleJA.cs
using System.Collections.Generic;  // Dictionary
using Colossal;                    // IDictionarySource

namespace CitizenEntityCleaner
{
    /// <summary>
    /// Japanese locale entries (ja-JP)
    /// </summary>
    public class LocaleJA : IDictionarySource
    {
        private readonly Setting m_Setting;
        public LocaleJA(Setting setting) { m_Setting = setting; }

        public IEnumerable<KeyValuePair<string, string>> ReadEntries(
            IList<IDictionaryEntryError> errors, Dictionary<string, int> indexCounts)
        {
            return new Dictionary<string, string>
            {
                // Mod name in Options menu list
                { m_Setting.GetSettingsLocaleID(), Mod.Name },

                // Tabs
                { m_Setting.GetOptionTabLocaleID(Setting.kSection), "アクション" },
                { m_Setting.GetOptionTabLocaleID(Setting.AboutTab), "概要" },

                // Groups
                { m_Setting.GetOptionGroupLocaleID(Setting.kFiltersGroup), "クリーンアップ対象" },
                { m_Setting.GetOptionGroupLocaleID(Setting.kButtonGroup), "アクション" },
                { m_Setting.GetOptionGroupLocaleID(Setting.InfoGroup), "情報" },
                { m_Setting.GetOptionGroupLocaleID(Setting.DebugGroup), "デバッグ" },

                // Filter toggles
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.IncludeCorrupt)), "▪ 破損した市民" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.IncludeCorrupt)),
                  "有効（既定）にすると、**破損**した市民をカウントしてクリーンアップします。\n" +
                  "PropertyRenter コンポーネントがなく（かつホームレス、通勤者、移転中ではない）住民が対象です。\n\n" +
                  "破損した市民は本 Mod の主要な対象です。都市内に多すぎると、時間の経過とともに不具合の原因になり得ます。" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.IncludeMovingAwayNoPR)), "▪ 移転中 (Rent = 0)" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.IncludeMovingAwayNoPR)),
                  "有効にすると、現在 **移転中** で Rent = 0（= PropertyRenter コンポーネントなし）の市民をカウントしてクリーンアップします。\n\n" +
                  "PropertyRenter を持つ、または Rent > 0 の移転中市民は削除されません。" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.IncludeCommuters)), "▪ 通勤者" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.IncludeCommuters)),
                  "有効にすると、**通勤者** の市民をカウントしてクリーンアップします。通勤者とは、この都市に居住していないが仕事のために通う市民を指します。\n\n" +
                  "通勤者は、以前は本市に居住していたがホームレス化により転出した場合もあります（ゲーム v1.2.5 で追加された仕様）。" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.IncludeHomeless)), "▪ ホームレス" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.IncludeHomeless)),
                  "有効にすると、**ホームレス** の市民をカウントしてクリーンアップします。\n\n" +
                  "<注意>：ホームレスを削除すると未知の副作用を引き起こす可能性があります。" },

                // Buttons
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.CleanupEntitiesButton)), "市民をクリーンアップ" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.CleanupEntitiesButton)),
                  "先にセーブ済みの都市を読み込んでください。\nPropertyRenter コンポーネントが存在しない世帯から市民を削除します。\n" +
                  "クリーンアップには、[ ✓ ] で選択した任意項目も含まれます。\n\n" +
                  "**注意**：これは回避策であり、他のデータが破損する可能性があります。まずセーブのバックアップを作成してください！" },

                // Warning (confirmation)
                { m_Setting.GetOptionWarningLocaleID(nameof(Setting.CleanupEntitiesButton)),
                  "オプションで選択した項目を永久に削除します。\n\nまずセーブをバックアップしてください！\n 続行しますか？" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.RefreshCountsButton)), "カウントを更新" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.RefreshCountsButton)),
                  "＜数値を取得するには、先にセーブ済みの都市を読み込んでください。＞\n" +
                  "すべてのエンティティ数を更新し、現状の統計を表示します。\n" +
                  "クリーンアップ後は、ゲームを一時停止せずにしばらく動かしてください。" },

                // Debug preview - logs a sample list of corrupt citizens
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.LogCorruptPreviewButton)), "ログ - 破損ID（最初の10件）" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.LogCorruptPreviewButton)),
                  "- 最初の **10 件の破損した市民 ID** をログファイルに **(Index:Version)** 形式で追加します（Scene Explorer で照合）。\n\n" +
                  "- **プレビューのみ** — 何も削除しません。\n\n" +
                  "- ログファイルの場所:\n" +
                  "%USERPROFILE%/AppData/LocalLow/Colossal Order/Cities Skylines II/logs/CitizenEntityCleaner.log" },

                // Sentence UNDER the button (multiline text row)
                // LabelLocale = inline body under the button
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.DebugCorruptNote)),
                  "デバッグ用途：サンプル一覧をログ出力 — 何も削除しません。\n" +
                  "破損エンティティの最初の10件のIDをログに列挙します。" },

                // Displays
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.CleanupStatusDisplay)), "ステータス" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.CleanupStatusDisplay)),
                  "クリーンアップ状況を表示します。実行中はライブ更新されます。実行中でない場合は [カウントを更新] を押して再計算してください。\n\n" +
                  "\"**Idle**\" = クリーンアップ未実行、または都市が未読み込み。\n" +
                  "\"**Nothing to clean**\" = 選択されたフィルターに一致する市民がいない（または既に削除済み）。\n" +
                  "\"**Complete**\" = 直近のクリーンアップが完了。フィルターを変更するか新たに実行するまで維持されます。" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.TotalCitizensDisplay)), "市民総数" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.TotalCitizensDisplay)),
                  "現在のシミュレーション内に存在する市民エンティティの総数。\n\n" +
                  "破損エンティティを含む可能性があるため、人口とは異なる場合があります。" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.CorruptedCitizensDisplay)),
                  "クリーン対象の市民：上の [ ✓ ] を選択" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.CorruptedCitizensDisplay)),
                  "「**クリーンアップ**」をクリックした際に削除される市民エンティティ数。\n\n" +
                  "選択したチェックボックス [ ✓ ] に基づきます。" },

                // Prompts (used by Setting.cs for placeholder text)
                { "CitizenEntityCleaner/Prompt/RefreshCounts", "[カウントを更新] をクリック" },
                { "CitizenEntityCleaner/Prompt/NoCity", "都市が読み込まれていません" },
                { "CitizenEntityCleaner/Prompt/Error",  "エラー" },
                { "CitizenEntityCleaner/Status/Progress", "クリーンアップ進行中… {0}" },
                { "CitizenEntityCleaner/Status/Cleaning", "クリーン中… {0}" },

                // About tab fields
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.NameText)), "Mod 名" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.NameText)), "この Mod の表示名。" },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.VersionText)), "バージョン" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.VersionText)), "現在の Mod バージョン。" },

#if DEBUG
                // Only visible in DEBUG builds
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.InformationalVersionText)), "情報バージョン" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.InformationalVersionText)), "コミット ID 付きの Mod バージョン" },
#endif

                // About tab links (the three external link buttons)
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OpenGithubButton)),  "GitHub" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OpenGithubButton)),   "この Mod の GitHub リポジトリ。ブラウザで開きます。" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OpenDiscordButton)), "Discord" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OpenDiscordButton)),  "Mod へのフィードバック用 Discord。ブラウザで開きます。" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OpenParadoxModsButton)), "Paradox Mods" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OpenParadoxModsButton)),  "Paradox Mods のウェブサイト。ブラウザで開きます。" },

                // About tab --> Usage section header & blocks
                { m_Setting.GetOptionGroupLocaleID(Setting.UsageGroup), "使用方法" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.UsageSteps)),
                  "1. ＜まずセーブファイルをバックアップ！＞\n" +
                  "2. ＜[カウントを更新] をクリックして現在の統計を表示＞\n" +
                  "3. [ ✓ ] ＜含める項目をチェックボックスで選択＞\n" +
                  "4. ＜[市民をクリーンアップ] をクリックしてエンティティを整理＞" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.UsageSteps)), "" },

                // Notes block
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.UsageNotes)),
                  "注意：\n" +
                  "• 本 Mod は自動では動作しません。削除するたびに **[市民をクリーンアップ]** を使用してください。\n" +
                  "• 予期しない動作があれば、元のセーブに戻してください。" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.UsageNotes)), "" },
            };
        }

        public void Unload() { }
    }
}
