# Yeni çekirdekler öncesi denetim — 24 Ekim 2026

## Değerlendirme

Mevcut Temel Eğitim çekirdeğinin yerel hesaplama, aylık kayıt, geçmiş düzenleme/silme ve belge üretme akışları aşağıdaki kontrollerden geçti. Yeni modüllerin Excel incelemesine ve ayrı çekirdeklerinin entegrasyonuna geçilebilir. Bu sonuç, bütün olası Windows/yazıcı ortamlarında kusursuzluk veya SGK sunucusunun dosyayı kabul ettiğine dair bir taahhüt değildir.

İnceleme başlangıcında 131 test vardı. Bulunan sorunları önce başarısız testlerle yeniden oluşturdum; düzeltmelerden sonra **151 birim testi, sıfır atlama ve sıfır başarısızlık** ile tamamlandı. WPF akış testi de geçti. Testlerde gerçek öğretmenler üzerinde düzenleme veya silme yapılmadı.

## Bulunan ve giderilen sorunlar

1. **Eski ekranın yeni kaydı ezmesi:** Aylık girişte Kaydet, Hakediş oluştur ve öğretmen listesinden Kaydet yollarında eksik olan beklenen içerik kontrolleri eklendi. Farklı güncel veri varsa işlem durur; dosyaya yazılmaz. Başka öğretmenin yeni aylık girişinin sessizce bordro dışında kalması da engellenir. Diske zaten birebir aynı girdiler yazılmışsa güvenli yeniden denemeye izin verilir.
2. **Dönem değiştirerek giriş kaybı:** Kaydedilmemiş hücre/aylık giriş varken modül, ay ve yıl seçimi kilitlenir; önce kaydetme veya onaylı çalışma temizleme gerekir. Pencere kapanırken aylık giriş, yeni öğretmen, öğretmen listesi, sabitler ve kurum bilgileri için varsayılanı Hayır olan uyarı vardır. Bu, elektrik kesilmeden önce kaydedilmemiş metinlerin otomatik kaydedildiği anlamına gelmez.
3. **Bozulmuş hesap sonuçlarının okunabilmesi:** Önceden yalnız net = tahakkuk − kesinti kontrolü vardı. Artık Temel Eğitim arşivinin bütün sonuç alanları kendi dondurulmuş girdileri/sabitleri ile karşılaştırılır. Bozuk kayıt sessizce onarılmaz veya güncel sabitlerle değiştirilmez; özgün dosya korunarak hata gösterilir. Yinelenen dolu TCKN de reddedilir.
4. **Aşırı büyük sayının kaydedilmesi:** Aylık formda ve ortak sabitlerde hesap sınırlarını aşan girişler açıklamalı doğrulama hatasıyla durdurulur; yeni dosya oluşmaz, önceki sabitler korunur. Geçerli sıfır saat kaydı korunmuştur.
5. **Eksik ön doğrulama ve yeniden deneme:** Bozuk kurum/imza dosyası veya geçersiz hakediş verisi aylık girdiler yazılmadan önce yakalanır. Yarım kalmış bir oluşturmanın aynı saatlerle güvenle yeniden denenmesi testlidir. Çok dosyalı hibrit işlemin açılışta kurtarılması sonrasında ekranın eski sürümüyle yazma denemesi reddedilir; yenileme sonrası kayıt yapılabilir.

## Doğrulanan senaryolar

| Alan | Kanıt / sonuç |
|---|---|
| Excel uyumu | Kaynak çalışma kitabı Microsoft Excel CalculateFullRebuild ile salt okunur yeniden hesaplandı. 50 senaryo × 14 hesap alanı çekirdekle eşleşti. Üç girdi dahil 850 sayısal referans alanı önceki test fikstürüyle de eşit. Kaynak kitabın SHA-256 değeri değişmedi. |
| Eksik gün | Saat/7,5 yukarı yuvarlama; 28/29/30/31 günlük aylar; ilk/son gün giriş/çıkış; aynı ay giriş ve çıkış; eski tarihler; DYK saatleri; kapasite aşımında parasal tutarın korunması. Kullanıcının belirlediği SGK yan bilgi kuralı esas alındı. |
| Ay / yıl ayrımı | Eylül'de 8 kişi, Ekim'de 4 ayrılış + 6 yeni giriş; Ekim'de 10 ödeme satırı ve farklı saatler. Eylül dosyası byte düzeyinde aynı. Yeni yılda eski saatler yüklenmiyor. Aralık/Ocak personel sayımı da testli. |
| Modül ayrımı | Modüle ait öğretmenler, değiştirilemeyen sahiplik, ID/TCKN tekilliği ve başka modülün aylık dosyalarının korunması. Eklenmemiş çekirdekler Temel Eğitim hesabına yönlendirilmez. |
| Arşiv | Aynı modül/ay/yılda ikinci kayıt engeli; iki eşzamanlı pencere; onaysız düzenleme/silmenin hiçbir şeyi değiştirmemesi; onaylı düzenlemede aynı dosyada tek kayıt; eski sürümün liste dışı yedeği; silmede diğer ayların ve aylık/öğretmen dosyalarının korunması. |
| Saklama | Atomik dosya değişimi, bozuk dosyanın üzerine yazmama, eski alanların okuma geçişi, beklenen sürüm kontrolü, hibrit işlem günlüğünün ileri/geri kurtarılması ve ikinci dosya yazma hatasında özgün verilerin korunması. Gerçek elektrik kesintisi uygulanmadı. |
| WPF | Altı panelin 1366×768 ve 1040 genişlikte toplam 12 görüntüsü; canlı hesap; aramalı meslek kodu; hücre düzenleme; beş eşit çıktı düğmesi; başarı bildirimi/yönlendirme; geçmiş onay/iptal; beş tür kaydedilmemiş form için kapanış Hayır/Evet. Veri bağlama hatası yok. |
| PDF / yazdırma modeli | Üç belge, Türkçe metin, bordro yatay A4, banka/personel dikey A4; yazıcı paginator sınırları. Uzun adlarla 10 kişi sığıyor. Okunabilirlik sınırını aşan 65 kişilik yoğun örnekte açık hata var; satır atılmıyor. Fiziksel yazıcıya iş gönderilmedi. |
| SGK XML | Kullanıcının XSD'si gömülü kaynakla byte düzeyinde aynı; zorunlu alanlar, isim uzunluğu, dönem/gün, eksik gün/çıkış nedeni, kullanıcı inceleme onayı, eski kaynak sürümünü reddetme, UTF-8/BOM kontrolü. Gerçek arşivden 10 satırın PEK ve günleri ayrıca Python/lxml ile özgün XSD'ye göre doğrulandı. SGK'ya yükleme yapılmadı. |
| Gerçek veri korunması | Çalışma başında alınan yedekteki 19 dosyanın SHA-256 değerleri ve dosya listesi değişmedi. Gerçek kayıttan özel test klasörüne üç tek A4 PDF ve XML üretildi; kaynaklara yazılmadı. |

Test günlükleri, görüntüler, özel PDF/XML ve yedekler `artifacts/` altında Git dışında tutulur. Kişi ve kurumun özel bilgileri bu rapora veya kaynak koduna aktarılmadı.

Debug derlemesi sıfır uyarı/hata ile tamamlandı. Release testleri de geçtikten sonra `dist/Hakedis-win-x64-20261024-182518.zip` paketi son düzeltmeyle güncellendi. Pakette kişisel veri veya Veriler klasörü yoktur. Üretilen EXE, farklı çalışma dizininden, kurulu .NET arama yolları devre dışı bırakılarak başlatıldı; pencere oluştu, yanıt verdi ve normal kapanış tamamlandı. Bu, bu bilgisayardaki bağımsız EXE açılış kontrolüdür; temiz başka Windows bilgisayarı testi değildir.

## Tekrarlanabilir komutlar

```powershell
dotnet test tests/Hakedis.Tests --no-restore -m:1 --verbosity quiet
dotnet build tests/Hakedis.UiSmoke -c Release --no-restore -m:1 --verbosity quiet
./tests/Hakedis.UiSmoke/bin/Release/net10.0-windows/Hakedis.UiSmoke.exe artifacts/ui-denetim-yeni
./scripts/Publish-Windows.ps1
```

UI testinde yeni çıktı klasörü kullanılmalıdır; test kendi sentetik verilerini bu klasörde oluşturur. Gerçek Veriler klasörünü test veri dizini olarak vermeyin. Gerçek arşivi inceleyen ayrı salt okunur seçenekler mevcut olsa da özel çıktı üretirler; bu dosyalar paylaşılmamalıdır.

## Yeni çekirdek eklerken korunacak sınırlar

Her Excel için ayrı Core projesi, hesaplama kural sürümü ve Excel karşılaştırma testleri eklenecek. Öğretmenler ve aylık girdiler modüle göre ayrılmaya devam edecek. Ortak sabitler eski hakedişleri değiştirmeyecek. İlgili modül seçildiğinde yalnız onun çekirdeği çalışacak.

Mevcut tablo/arşiv/çıktı tipleri ilk çekirdeğe göre hazırlanmıştır. Yeni modül yalnız katalogda etkinleştirilmez: modül yönlendirmesi, girdi/sonuç uyarlayıcıları, arşivin kendi kural sürümüyle okunması ve PDF/XML eşlemeleri de tamamlanmalıdır. Özellikle Halk Eğitim'in farklı hesabı Temel Eğitim formülüne uydurulmayacaktır. Ayrıntı: MODUL_MIMARISI.md.

## Yerel denetimin kapsamı dışında kalanlar

- Gerçek SGK yükleme/kabul sonucu ve gerçek yazıcı çıktısı.
- Farklı, temiz bir Windows bilgisayarında uçtan uca kabul testi.
- Formüllerin veya dönem sabitlerinin yeni bir mevzuat incelemesi. Bu çalışma Excel ve kullanıcının verdiği hesap kurallarıyla uyumu sınar.
- Nihai dağıtım öncesi kişisel verilerin temizlenmesi ve kurulum sihirbazı. Mevcut yayın betiği kendi .NET ortamını içeren taşınabilir EXE/ZIP üretir; installer üretmez. Veriler EXE yanındadır; klasör kullanıcı tarafından yazılabilir olmalıdır.
- Diskin fiziksel arızası veya hiç Kaydet'e basılmadan yaşanan elektrik kesintisinden sıfır kayıpla kurtulma garantisi. Mevcut test, atomik yazmayı ve kalıcı işlem günlüğünün toparlanmasını doğrular.
