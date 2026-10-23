# Güncel tasarım kararları — 23 Ekim 2026

Bu belge kullanıcının son açıklamalarını ve önerilen teknik tasarımı ayırır. Önceki SQL önerisi kaldırılmıştır.

## Kesinleşen gereksinimler

Yeni ve bağımsız proje; Windows üzerinde kurulum ve Excel gerektirmeyen çalışma; SQL veri tabanı yok. Temel eğitim ilk modül, usta öğretici daha sonra eklenir. Windows dışındaki işletim sistemleri bu aşamanın kapsamı değildir. Her Windows sürümünde çalışma sözü verilmez; desteklenen Windows sürümleri ve işlemci mimarileri pilotta belirlenecektir.

## Önerilen uygulama ve dağıtım

C# / WPF uygunluğunu korur. Uygulama, .NET çalışma zamanı dahil self-contained taşınabilir klasör olarak yayımlanır. ZIP açılır ve EXE çalıştırılır. Yönetici yetkisi ve ayrı .NET/Office kurulumu hedeflenmez. Tek EXE seçeneği ayrıca denenebilir; ilk hedef eksiksiz taşınabilir klasördür. x64 ve gerekiyorsa ARM64 için ayrı paketler üretilir. Kaynak koddan derleme araçları son kullanıcıya gerekmez.

## SQL içermeyen saklama seçenekleri

| Seçenek | Artısı | Sınırı |
|---|---|---|
| Tek büyük JSON | Basit ve okunabilir | Her değişiklikte tüm veri yeniden yazılır; kurtarma alanı geniştir |
| **Kurum ve dönemlere ayrılmış JSON** | Küçük kayıtlar; dönem arşivi ve kurtarma kolay; ek sunucu gerekmez | Dosyalar arası tutarlılık uygulama tarafından sağlanır |
| Tek paket dosyası | Başka bilgisayara taşıması kolay | Her kayıt için paketi yeniden yazmak uygun olmayabilir |

Öneri: günlük çalışmada kurum/dönem bazlı JSON; taşımada ve yedekte tek `.hakedis` paketi. Paket, manifest ve JSON/belgeler içeren ZIP biçimidir; bu uzantı tek başına şifreleme sağlamaz. Excel aktarım/raporlama biçimidir; ana veri deposu değildir.

Örnek veri klasörü:

```text
Veriler/
  manifest.json
  kurumlar/<kurum-id>/kurum.json
  kisiler/<kisi-id>.json
  moduller/temel-egitim/parametreler/<surum>.json
  donemler/<kurum-id>/temel-egitim/2026-09/
    versions/<surum-id>/girdiler.json
    versions/<surum-id>/sonuclar.json
    versions/<surum-id>/belgeler.json
    current.json
  yedekler/
```

Usta öğretici ileride kendi modül klasörünü ve hesap kurallarını kullanır; ortak kişi/kurum kimlikleri korunur.

### Veri kaybına karşı çalışma düzeni

1. Yeni kayıt, hedefle aynı diskte geçici dosyaya yazılır, kapatılır ve tekrar okunarak doğrulanır.
2. Tek dosya değişimi atomik değiştirme ile yapılır; son sağlam kopya korunur.
3. Birden fazla dosyalı dönem işlemi yeni sürüm klasörüne eksiksiz yazılır. Sürüm doğrulanmadan `current.json` değiştirilmez. Böylece yarım kaydedilmiş girdiler ve sonuçlar birlikte geçerli olmaz.
4. Kesinleşmiş sürüm değişmez; düzeltme yeni sürüm üretir. Girdi, parametre ve kişi/banka bilgilerinin o tarihteki kopyası saklanır.
5. Kayıt sırasında kilit dosyasıyla ikinci yazıcı engellenir. Çökme sonrası eski kilit kontrollü kurtarılır. Ağ klasörü veya bulut eşitlemesi eşzamanlı kullanım çözümü kabul edilmez.
6. Açılışta manifest/şema sürümü ve bütünlük denetlenir; bozuk kayıt sessizce boş veri sayılmaz. Hash bozulmayı saptar, kötü niyetli değişikliği veya yetkisiz erişimi tek başına önlemez.
7. İşlem öncesi otomatik yedek ve kullanıcı tarafından 'Yedekle / Geri Yükle' işlemi bulunur. Saklama sayısı ve hedefi ayarlanabilir; aynı disk kopyası disk arızasına karşı yeterli değildir.
8. Şema yükseltmesinden önce yedek alınır; yükseltme kopya üzerinde denenir. Yeni sürüm okunamazsa mevcut veri üzerine yazılmaz.

### Taşınabilir kullanım

Varsayılan veri konumu uygulama yanındaki `Veriler` klasörüdür. Kullanıcı farklı bir yazılabilir klasör seçebilir; erişim yoksa sessizce başka yere kayıt yapılmaz. EXE güncellemesi kullanıcı verisinin üstüne yazmaz. USB ile taşımadan önce uygulama kapatılır; daha güvenli aktarım 'Veri Paketini Dışa Aktar / Aç' ile yapılır. Yazma yarıda kesilmesi ve salt okunur klasör senaryoları test edilir.

İsteğe bağlı parola korumalı yedek daha sonra eklenebilir; düz JSON gizlilik sağlamaz. Farklı bilgisayara taşınabilirlik için yalnızca tek Windows hesabına bağlı şifreleme anahtarı kullanılmamalıdır. Parola ve anahtar kurtarma davranışı ayrıca tasarlanır; henüz şifreleme uygulandığı iddia edilmez.

## Yazdırma ve Excel çıktıları

| Kaynak sayfa / çıktı | Üründeki davranış |
|---|---|
| Bordro (Çıktı-1) | Önizle, yazdır, PDF kaydet |
| Banka Listesi (Çıktı-2) | Önizle, yazdır, PDF kaydet |
| Personel Bildirimi (Çıktı-3) | Önizle, yazdır, PDF kaydet |
| KişiselBordro (Talep Edilirse) | Kişi seçilerek isteğe bağlı önizle/yazdır/PDF |
| E-Bildirge Verileri | Bilgi amaçlı isteğe bağlı Excel; zorunlu yazdırma yok |
| VERİ GİRİŞİ | Personel/saat/parametre dökümü için isteğe bağlı Excel |
| HalkbankDTO | Banka şablonuna uygun isteğe bağlı yükleme dosyası; resmî basılı belge grubundan ayrı |
| Diğer bilgi raporları | İsteğe bağlı Excel: kişi listesi, aylık saatler, hesap ayrıntıları, dönem karşılaştırması |

Excel çıktısı üretmek Excel kurulumunu gerektirmemelidir. Üretilen dosyayı açmak için kullanıcının uyumlu bir görüntüleyici kullanması ayrı konudur. PDF önizleme/yazdırma Windows baskı altyapısı üzerinden çalışmalıdır. Resmî belgeler ile bilgi çıktıları aynı kesinleşmiş sonuca bağlanır; ayrıca vergi hesaplamaz. A4 yönü/sayfa kırılması mevcut şablonlarla görsel olarak doğrulanacaktır.

## Güncellenmiş geliştirme sırası

1. Açık Excel hataları ve dönem hesap kuralları için şartname.
2. Bağımsız hesap motoru ve kuruş düzeyinde karşılaştırma testleri.
3. JSON sürümleme, atomik kayıt, yedek/geri yükleme ve kurtarma testleri.
4. Personel/dönem aktarımı ve OEHS görsel diliyle ekranlar.
5. Üç basılı çıktı, isteğe bağlı kişisel bordro/Excel ve banka şablonu.
6. Temiz Windows ortamında Excel/.NET kurulmadan taşınabilir paket pilotu.

Kabul örnekleri: kayıt sırasında süreç kapanınca son sağlam sürüm açılmalı; aynı klasörü ikinci uygulama yazamamalı; paket başka bilgisayarda açılmalı; EXE güncellemesi verileri korumalı; ana bordro ile banka neti ve kişisel bordro aynı olmalı. Usta öğretici hesabı bu aşamada geliştirilmez.
