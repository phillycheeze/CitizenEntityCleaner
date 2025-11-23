// LocaleTR.cs
using System.Collections.Generic;  // Dictionary
using Colossal;                    // IDictionarySource

namespace CitizenEntityCleaner
{
    /// <summary>
    /// Turkish locale entries (tr-TR)
    /// </summary>
    public class LocaleTR : IDictionarySource
    {
        private readonly Setting m_Setting;
        public LocaleTR(Setting setting) { m_Setting = setting; }

        public IEnumerable<KeyValuePair<string, string>> ReadEntries(
            IList<IDictionaryEntryError> errors, Dictionary<string, int> indexCounts)
        {
            return new Dictionary<string, string>
            {
                // Mod name in Options menu list
                { m_Setting.GetSettingsLocaleID(), Mod.Name },

                // Tabs
                { m_Setting.GetOptionTabLocaleID(Setting.kSection), "İşlemler" },
                { m_Setting.GetOptionTabLocaleID(Setting.AboutTab), "Hakkında" },
                { m_Setting.GetOptionTabLocaleID(Setting.DebugTab), "Hata Ayıklama" },

                // Groups
                { m_Setting.GetOptionGroupLocaleID(Setting.kFiltersGroup), "Temizlik Hedefleri" },
                { m_Setting.GetOptionGroupLocaleID(Setting.kButtonGroup), "İşlemler" },
                { m_Setting.GetOptionGroupLocaleID(Setting.InfoGroup), "Bilgi" },
                { m_Setting.GetOptionGroupLocaleID(Setting.DebugGroup), "Hata Ayıklama" },

                // Filter toggles
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.IncludeCorrupt)), "▪ Bozuk Vatandaşlar" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.IncludeCorrupt)),
                  "Etkinleştirildiğinde (varsayılan), **bozuk vatandaşları** sayar ve temizler;\n" +
                  "PropertyRenter bileşeni olmayan sakinler (evsiz, işe gidip gelenler, turistler veya taşınanlar değil).\n\n" +
                  "Bozuk vatandaşlar bu modun ana hedefidir. Şehir çok fazla bozuk vatandaş içeriyorsa, zamanla sorunlara neden olabilir." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.IncludeMovingAwayNoPR)), "▪ Taşınanlar (Kira = 0)" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.IncludeMovingAwayNoPR)),
                  "Etkinleştirildiğinde, şu anda **Taşınıyor** durumunda olan ve Kira = 0 olan vatandaşları sayar ve temizler (yani, PropertyRenter bileşeni yok).\n\n" +
                  "PropertyRenter veya Kira > 0 olan taşınan vatandaşlar kaldırılmaz." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.IncludeCommuters)), "▪ İşe Gidip Gelenler" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.IncludeCommuters)),
                  "Etkinleştirildiğinde, **işe gidip gelen** vatandaşları sayar ve temizler. İşe gidip gelenler, şehrinizde yaşamayan ancak çalışmak için şehrinize seyahat eden vatandaşları içerir.\n\n" +
                  "Bazen, işe gidip gelenler daha önce şehrinizde yaşıyordu ancak evsizlik nedeniyle taşındı (oyun sürümü 1.2.5'te eklenen özellik)." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.IncludeHomeless)), "▪ Evsizler" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.IncludeHomeless)),
                  "Etkinleştirildiğinde, **evsiz** vatandaşları sayar ve temizler.\n\n" +
                  "<DİKKATLİ OLUN>: evsizleri silmek bilinmeyen yan etkilere neden olabilir." },

                // Buttons
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.CleanupEntitiesButton)), "Vatandaşları Temizle" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.CleanupEntitiesButton)),
                  "Önce kayıtlı bir şehir yükleyin.\nArtık PropertyRenter bileşeni olmayan hanelerden vatandaşları kaldırır.\n" +
                  "Temizlik ayrıca seçilen isteğe bağlı öğeleri de içerir [ ✓ ].\n\n" +
                  "**DİKKATLİ OLUN**: bu bir geçici çözümdür ve diğer verileri bozabilir. Önce kayıt dosyanızın yedeğini oluşturun!" },

                // Warning (confirmation)
                { m_Setting.GetOptionWarningLocaleID(nameof(Setting.CleanupEntitiesButton)),
                  "Seçeneklerde seçilen öğeleri kalıcı olarak sil.\n\nLütfen önce kayıt dosyanızı yedekleyin!\n Devam edilsin mi?" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.RefreshCountsButton)), "Sayıları Yenile" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.RefreshCountsButton)),
                  "<Sayıları görmek için önce kayıtlı bir şehir yükleyin.>\n" +
                  "Mevcut şehir istatistiklerini göstermek için tüm varlık sayılarını günceller.\n" +
                  "Temizlikten sonra, oyunun duraklatılmadan bir dakika çalışmasına izin verin." },

                // Cleanup Status and Counts
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.CleanupStatusDisplay)), "Durum" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.CleanupStatusDisplay)),
                  "Temizlik durumunu gösterir. Aktif bir temizlik sırasında canlı olarak güncellenir; aksi takdirde yeniden hesaplamak için [Sayıları Yenile]'ye basın.\n\n" +
                  "\"**Idle**\" = temizlik çalışmıyor veya henüz şehir yüklenmedi.\n" +
                  "\"**Nothing to clean**\" = seçilen filtrelere uyan vatandaş yok (veya zaten kaldırdınız).\n" +
                  "\"**Complete**\" = son temizlik tamamlandı; filtreleri değiştirene veya yeni bir temizlik yapana kadar devam eder." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.TotalCitizensDisplay)), "Toplam Vatandaş" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.TotalCitizensDisplay)),
                  "**Şu anda simülasyonda** bulunan toplam vatandaş varlığı sayısı.\n\n" +
                  "Bu sayı nüfusunuzdan farklı olabilir çünkü bozuk varlıklar içerebilir." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.CorruptedCitizensDisplay)),
                  "Temizlenecek Vatandaşlar: yukarıda [ ✓ ] seçin" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.CorruptedCitizensDisplay)),
                  "**[Temizle]**'yi tıkladığınızda kaldırılacak vatandaş varlıklarının sayısı,\n\n" +
                  "seçilen kutulara [ ✓ ] göre." },

                // Prompts (used by Setting.cs for placeholder text)
                { "CitizenEntityCleaner/Prompt/RefreshCounts", "[Sayıları Yenile]'ye tıklayın" },
                { "CitizenEntityCleaner/Prompt/NoCity", "Şehir yüklenmedi" },
                { "CitizenEntityCleaner/Prompt/Error",  "Hata" },
                { "CitizenEntityCleaner/Status/Progress", "Temizlik devam ediyor… {0}" },
                { "CitizenEntityCleaner/Status/Cleaning", "Temizleniyor… {0}" },


                // About tab fields
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.NameText)), "Mod Adı" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.NameText)), "Bu modun görünen adı." },
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.VersionText)), "Sürüm" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.VersionText)), "Mevcut mod sürümü." },

#if DEBUG
                // Only visible in DEBUG builds
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.InformationalVersionText)), "Bilgilendirme Sürümü" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.InformationalVersionText)), "Commit ID ile Mod Sürümü" },
#endif

                // About tab links (the three external link buttons)
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OpenGithubButton)),  "GitHub" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OpenGithubButton)),   "Mod için GitHub deposu; tarayıcıda açılır." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OpenDiscordButton)), "Discord" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OpenDiscordButton)),  "Mod hakkında geri bildirim için Discord sohbeti; tarayıcıda açılır." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OpenParadoxModsButton)), "Paradox Mods" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OpenParadoxModsButton)),  "Paradox Mods web sitesi; tarayıcıda açılır." },

                // About tab --> Usage section header & blocks
                { m_Setting.GetOptionGroupLocaleID(Setting.UsageGroup), "KULLANIM" },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.UsageSteps)),
                  "1. <Önce kayıt dosyanızı yedekleyin!>\n" +
                  "2. <Mevcut istatistikleri görmek için [Sayıları Yenile]'ye tıklayın.>\n" +
                  "3. [ ✓ ] <Onay kutularını kullanarak dahil edilecek öğeleri seçin>\n" +
                  "4. <Varlıkları temizlemek için [Vatandaşları Temizle]'ye tıklayın.>" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.UsageSteps)), "" },

                // Notes block
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.UsageNotes)),
                  "Notlar:\n" +
                  "• Bu mod **otomatik olarak çalışmaz**; kaldırma işlemleri için her seferinde **[Vatandaşları Temizle]** kullanın.\n" +
                  "• Beklenmeyen davranış için gerekirse orijinal kayıtlı şehre geri dönün." },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.UsageNotes)), "" },


                 // Debug Tab preview - logs a sample list of corrupt citizens
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.LogCorruptPreviewButton)), "GÜNLÜK - Bozuk ID'ler (ilk 10)" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.LogCorruptPreviewButton)),
                  "- Günlük dosyasına Scene Explorer çapraz kontrol için ilk **10 Bozuk vatandaş ID'lerinin** bir listesini ekler **(Index:Version)**.\n\n" +
                  "- **Yalnızca önizleme** — hiçbir şey silinmez.\n\n" +
                  "- Günlük dosyası konumu:\n" +
                  "%USERPROFILE%/AppData/LocalLow/Colossal Order/Cities Skylines II/logs/CitizenEntityCleaner.log" },

                // Sentence UNDER the button (multiline text row)
                // LabelLocale = inline body under the button
                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.DebugCorruptNote)),
                  "Hata ayıklama kullanımı: örnek liste günlüğe kaydedilir — hiçbir şey silinmez.\n" +
                  "Günlüğe ilk 10 bozuk varlık ID'sini listele." },

                { m_Setting.GetOptionLabelLocaleID(nameof(Setting.OpenLogButton)), "Günlüğü Aç" },
                { m_Setting.GetOptionDescLocaleID(nameof(Setting.OpenLogButton)), "Günlük dosyasını varsayılan metin düzenleyicide açın." },

            };
        }
        public void Unload() { }
    }
}
