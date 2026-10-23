# Devam notu — 23 Ekim 2026

## Kullanıcının son istediği düzen

- Sol menüde alt alta Veri Girişi, Hakediş Hesapla, Ücretli Öğretmen Listesi.
- Modül seçimi Veri Girişi panelinde açılır listeden yapılır.
- Yeni öğretmen kaydı yalnızca Veri Girişi'ndeki kayıt kartında. Öğretmen listesinde sadece seçili mevcut kaydı düzenleme, kaydetme ve onaylı silme.
- Kayıtlı kişinin adını veri girişine aktarma.

## Uygulananlar

Üç panel ve açılır modül seçimi kodlandı. Arayüz C# / WPF; OEHS feature/official-payment-document-templates dalının kaynak stil sözlüğü Themes/Oehs.xaml'e taşındı. Düğme şablonları, altın çerçeveli lacivert kartlar, üst şerit, aktif menü çizgisi ve Segoe UI referansla aynı kaynaklardan gelir. Ayrıntı docs/ARAYUZ_REFERANSI.md.

Yeni kayıt TeacherRegistrationViewModel ile sadece Veri Girişi'nde oluşturulur. TeacherListViewModel.Save seçim ve mevcut ID gerektirir; depoda Update eksik ID'yi oluşturamaz. Öğretmen kaydı ortak Contracts içinde; hesaplama çekirdekleri ayrılığı korunuyor. Öğretmenler EXE yanındaki Veriler/ogretmenler.json dosyasında saklanıyor. Kaydetmeden form değişikliği listeye yansımaz. Silme geçmiş hesap taslaklarını etkilemez. Yazma kilidi, geçici dosya, atomik değiştirme ve önceki listeyi .bak olarak koruma var. Bozuk dosya açılışta hata olarak gösterilir, boş liste ile üzerine yazılmaz.

Elektrik kesintisi bildirimi sonrası proje kaynaklarının diskte durduğu doğrulandı. İncelenen proje ağacında kullanıcıya ait öğretmen kayıt dosyası bulunmadı; başka yerde kaydedilmiş taslaklar bu iddiaya dahil değil. Kaydedilmemiş ekran girdilerinin kurtarılacağı söylenmedi.

## Sonraki ürün işleri

Toplu bordro hesaplama, dönem kesinleştirme, üç basılı belge, bilgi amaçlı Excel ve Halkbank aktarımı henüz yok. Diğer eğitim türlerinin ayrı çekirdekleri kendi Excel kaynakları geldikten sonra yazılacak. Şu an yalnızca Temel Eğitim çekirdeği etkin.

Önceki ekran yakalama sorunu, bu projeyi yerel WPF olarak render eden UiSmoke aracıyla aşıldı. Üç panelin normal ve dar pencere görüntüleri artifacts/ui-review-oehs altında üretildi ve incelendi. XAML kaynakları yüklendi, binding hatası bulunmadı. Örnek görüntüler yalnızca sentetik öğretmen içerir.

## Doğrulama ve paket

Son test koşusu: **46 başarılı, 0 başarısız**. Öğretmen kayıt/düzenleme/silme, panel geçişleri, kayıtların tekrar açılması, bozuk dosyanın açılışta uygulamayı kapatmaması ve mevcut hesaplama testleri geçti. Listeden yeni kayıt oluşturma engeli, Veri Girişi'nden yeni kaydın liste/hesap alanına taşınması ve silinen kaydın düzenleme sırasında yeniden yaratılmaması ayrıca doğrulandı.

Windows x64 EXE yayını başarıyla tamamlandı. OEHS arayüzlü yeni paket: `dist/Hakedis-win-x64-20261023-231302.zip`. ZIP çıkarılıp `Hakedis.App.exe` çalıştırılır. Mevcut kullanıcı kayıtları yeni pakete taşınırken `Veriler` klasörü korunmalıdır. Pakette gerçek öğretmen kaydı yoktur.

İki saat sonrası için bu sohbete tek hatırlatma oluşturuldu: Hakediş projesine dönüş.
