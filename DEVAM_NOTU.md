# Devam notu — 23 Ekim 2026

## Kullanıcının son istediği düzen

- Sol menüde alt alta Veri Girişi, Hakediş Hesapla, Ücretli Öğretmen Listesi.
- Modül seçimi Veri Girişi panelinde açılır listeden yapılır.
- Öğretmen listesinde yeni kayıt, seçili kişiyi düzenleme, kaydetme ve onaylı silme.
- Kayıtlı kişinin adını veri girişine aktarma.

## Uygulananlar

Üç panel ve açılır modül seçimi kodlandı. Öğretmen kaydı ortak Contracts içinde; hesaplama çekirdekleri ayrılığı korunuyor. Öğretmenler EXE yanındaki Veriler/ogretmenler.json dosyasında saklanıyor. Kaydetmeden form değişikliği listeye yansımaz. Silme geçmiş hesap taslaklarını etkilemez. Yazma kilidi, geçici dosya, atomik değiştirme ve önceki listeyi .bak olarak koruma var. TCKN yinelenmesi ve temel alan biçimleri kontrol edilir. Bozuk dosya açılışta hata olarak gösterilir, boş liste ile üzerine yazılmaz.

Elektrik kesintisi bildirimi sonrası proje kaynaklarının diskte durduğu doğrulandı. İncelenen proje ağacında kullanıcıya ait öğretmen kayıt dosyası bulunmadı; başka yerde kaydedilmiş taslaklar bu iddiaya dahil değil. Kaydedilmemiş ekran girdilerinin kurtarılacağı söylenmedi.

## Sonraki ürün işleri

Toplu bordro hesaplama, dönem kesinleştirme, üç basılı belge, bilgi amaçlı Excel ve Halkbank aktarımı henüz yok. Diğer eğitim türlerinin ayrı çekirdekleri kendi Excel kaynakları geldikten sonra yazılacak. Şu an yalnızca Temel Eğitim çekirdeği etkin.

Görsel kontrol önceki oturumda ekran yakalama aracı siyah görüntü verdiği için tamamlanamadı; kullanıcı yeni paketle ekranı değerlendirmeli.

## Doğrulama ve paket

Son test koşusu: **43 başarılı, 0 başarısız**. Öğretmen kayıt/düzenleme/silme, panel geçişleri, kayıtların tekrar açılması, bozuk dosyanın açılışta uygulamayı kapatmaması ve mevcut hesaplama testleri geçti.

Windows x64 EXE yayını başarıyla tamamlandı. Yeni paket: `dist/Hakedis-win-x64-20261023-210011.zip`. ZIP çıkarılıp `Hakedis.App.exe` çalıştırılır. Mevcut kullanıcı kayıtları yeni pakete taşınırken `Veriler` klasörü korunmalıdır. Pakette gerçek öğretmen kaydı yoktur.

İki saat sonrası için bu sohbete tek hatırlatma oluşturuldu: Hakediş projesine dönüş.
