# Son değişiklik — 24 Ekim 2026: hibrit öğretmen düzenleme

Kullanıcının beş maddesi uygulandı:
- SİGORTA NO, görevlendirildiği branş, görev yeri 2 ve ikinci görev yeri branşı; TeacherRecord, TeacherEditor, formlar, tablolar ve yeni JSON kayıtlarından çıkarıldı. Eski alan adları yalnız şema 1/2 uyumluluk kodunda ve geçiş testlerinde kaldı. Şema 3 yazılıyor; eski dosyanın .bak yedeği korunuyor. Eski hakediş görüntüleri değiştirilmedi.
- Bordro hibrit tablosu yalnız VERİ GİRİŞİ alanlarını kaynak sırasıyla gösterir (kaldırılan dört sütun hariç). 15 sütun: sıra, ad soyad, TC, IBAN, başlangıç/ayrılış ve ay içi tarihler, SGK meslek kodu, görev yeri, İBAN KONTROL, gündüz/DYK saatleri, SAAT TOPLAMI, icra. Sıra/kontrol/toplam otomatik; diğer hücreler düzenlenebilir. Hesaplanan bordro/SGK sütunları tablodan çıkarıldı; altı toplam kartı ve aylık SGK özeti korundu. Satırlar 30; hücreler yatay/dikey ortalı.
- Başlık yanındaki Sil/Düzenle yalnız seçili satır varsa etkin. Sil ana öğretmen kaydını onayla siler; geçmiş dosyalar korunur.
- Düzenle öğretmen formunda kopya açar; modül/aylık kart kilitlidir. Öğretmen Değişiklik kaydet / Vazgeç düğmeleri görünür. Kaydet bütün ekranları günceller, Hakediş Hesapla'ya döner ve Tamam ile kapatılan Kayıtlar güncellendi bildirimi verir. Vazgeç hiçbir kaydı değiştirmez; önceki tablo taslakları korunur. Döngü sonunda düğmeler Öğretmeni Kaydet / Formu Temizle olur.
- Değişiklikleri kaydet düğmesi tablonun altında, yalnız değişiklik varken etkin. Öğretmen ve aylık bilgiler önce tüm satırlar için doğrulanır, sonra iki dosya kalıcı işlem günlüğüyle yazılır. Hatalı satır, yinelenen TC veya eski ekran çakışması kayıt yazmaz. Yarım işlem açılışta ve sonraki yazmadan önce tamamlanır/geri alınır. OEHS HybridSourceUpdate/HybridRow örnekleri kullanıldı.

102 birim testi geçti: Excel hesap doğrulamaları, model temizliği, eski JSON geçişi/yedek, iptal, tüm görünümlerde güncelleme, geri alınan tablo değerinin temiz sayılması, geçersiz toplu kayıt, TC çakışması, eski ekran/aylık kayıt çakışması, değişmeyen arşiv, silme/seçim, yarım işlem ve ikinci dosyanın yazılamaması, kurtarma sonrası yeni kayıt, bozuk dosya şekilleri.

Gerçek Debug verileri önce artifacts/private-backups/before-hybrid-editing-20261024-141039/Veriler altına kopyalandı; dosya hashleri eşleşti. Aktif Debug kataloğu yeni şema 3'e geçirildi; tüm kalan öğretmen alanları eşit ve aylık dosya byte düzeyinde aynı. 10 öğretmen, 10 aylık kayıt, 972 saat; brüt 214.395,70 TL / net 184.380,34 TL doğrulandı. Özel yedek/yardımcılar Git tarafından yok sayılır.

Aktif veri yolu hâlâ src/Hakedis.App/bin/Debug/net10.0-windows/Veriler. Önceki dist/20261024-004600 programı ve verileri ayrıdır; otomatik eşitlenmez. Bu değişiklik için yeni dağıtım ZIP'i henüz oluşturulmadı. Kaynak Excel değiştirilmedi. WPF görüntüleri artifacts/ui-review-teacher-editing-final içindedir (sentetik öğretmenler).

---

# Güncel çalışma konumu — 24 Ekim 2026

Proje artık C:\Users\MuratPC\Documents\GitHub\TemelEgitimUcretliOgretmenHakedis konumunda. Bundan sonraki tüm komutlarda bu klasörü çalışma dizini olarak kullanın; bu sohbetin eski Desktop çalışma kökü boş kaldı. Aşağıdaki eski yol kayıtları tarihçedir.

Masaüstündeki inceleme kaynakları, Excel örnekleri ve referans proje de yeni projenin Git tarafından yok sayılan inceleme/ klasörüne alındı. Başlangıçtaki ayrı INCELEME_VE_YOL_HARITASI.md bu klasörde korundu. Yerel gerçek test kayıtları yeni konumda dist/Hakedis-win-x64-20261024-004600/Veriler altında aynı içerikle duruyor.

Taşıma kontrolü: 1.606 dosya / 1.584.298.330 bayt eşleşti; veri/Git/Excel için 11 SHA256 kontrolü eşleşti. Git geçmişi ve temiz çalışma durumu korundu. GitHub Desktop, github open komutuyla yeni klasöre yönlendirildi. Yeni yolda 81 test geçti; yayımlanmış bağımsız EXE açılışı doğrulandı ve kontrol sonrası kapatıldı. 10 öğretmen ve 10 Eylül-2026 aylık kaydı mevcut. Taşıma makbuzu artifacts/tasima-kontrol.json içinde; bu dosya Git'e alınmaz.

---

# Devam notu — 24 Ekim 2026

## Son istek ve uygulanan akış

Kullanıcının dokuz maddesi ve ek ay/yıl isteği işlendi. C# / WPF OEHS kabuğu korunuyor.
- Öğretmen kaydı yalnız Veri Girişi / Öğretmen Bilgileri kartında; kendi modül seçimi var.
- Aylık Ders Bilgileri kartı kendi modülü, DÖNEM (ay adı), YIL ve kayıtlı öğretmen seçimiyle açılıyor. Kaydet ve Düzenle düğmeleri altında; taslak aç/kaydet/hakediş düğmeleri kaldırıldı.
- Öğretmen listesi yalnız mevcut kaydı düzenleme/onaylı silme için; ayrıca modül seçimi var.
- Hakediş Hesapla modül ve ay/yıl seçimine göre hibrit tabloyu açıyor. Gösterim Eylül-2026 / Kasım-2027. Saat/kesinti/tarih hücreleri düzenlenirken sonuçlar ve 3×2 toplam kartı canlı güncelleniyor. Aylık giriş de aynı modül/dönemde canlı eşleşiyor.
- Dönem sabitleri ayrı sol menü/panelde, hakediş paneliyle aynı modül/döneme bağlı.
- ExcelHeadings.cs kaynak Excel hücrelerinden birebir; imla, satır sonları ve GV İstinası: yazımı korunuyor. İkinci görev yeri/branşı ve ay içi giriş/çıkış saklanıyor.
- PDF'nin 113 sayfasından 7.190 benzersiz meslek kodu, 18 satır devamı birleştirilerek gömülü kaynak yapıldı. Açılan kutunun üstünde canlı Türkçe sözcük/kod araması var. Sınıf öğretmen araması dört sonuç.

## Hesap denetimi

Microsoft Excel ayrı gizli örnekte salt okunur kaynak üzerinde CalculateFullRebuild ile 50 senaryo çalıştırdı; C# ile 14 hesap alanı karşılaştırıldı. Kaynak dosya kaydedilmedi/değiştirilmedi. Sentetik sayısal sonuçlar test fixture'ında; kişi kimliği/adı yok.
Brüt/prim/tahakkuk/vergi/kesinti/net karşılaştırmaları eşleşti. On öğretmenin kaynak toplam neti 184.380,34 TL. İcra kesintisinin yanlış erken yuvarlanması kaldırıldı: Excel gibi toplam kesintide yuvarlanıyor.
Gösterge programda gerçek parametre; Excel'in L/M formüllerinde 140 sabit. Farklı gösterge/oranların kaynak sabit formülleriyle aynı olduğu iddia edilmiyor.
Ayrıntı: docs/EXCEL_DENETIMI.md. Tekrar Excel doğrulama: scripts/Verify-Excel.ps1.

## SGK eksik gün açıklaması uygulandı

Kullanıcının sonraki açıklaması: bu alan hakedişi etkilemez, SGK sitesi için yan bilgidir. Çalışma günü toplam saat / 7,5 yukarı tam sayıya; eksik gün ayın gerçek takvim gününden çalışma gününü çıkararak bulunur. Ay içinde giriş/çıkışta, her iki sınır dahil çalışma aralığı esas alınır. Ekim-2026 5'inde giriş: 27 azami gün; 117 saat: 16 çalışma, 11 eksik gün.
Aylık açık tarihler öğretmen kaydındaki tarihlere göre öncelikli; boşsa kayıtlı tarihler kullanılır. Şubat ve artık yıl, son gün giriş, ilk gün çıkış, aynı ayda ikisi, sıfır saat ve DYK dahil sınırlar testli.
SGK hata/uyarısı para sonucunu silmez. Azami günden fazla çalışma günü varsa negatif eksik gün yerine — ve ayrı uyarı üretilir. Tabloda azami gün/çalışma/eksik gün ve kontrol sütunları; aylık girişte canlı özet var. Hakediş JSON'unda CalendarResult ve kural sürümü saklanır. Eski hakediş görüntüleri değiştirilmez.

## Saklama

SQL yok. EXE yanındaki Veriler/ogretmenler.json modül sahipliğini koruyan katalogdur. Aynı TCKN farklı modülde tekrar oluşturulamaz.
Veriler/AylikKayitlar/Modül/YYYY-MM.json aylık girdileri ve dönem sabitlerini, hakediş-...json ise oluşturulan hesap görüntüsünü saklar. Atomik yazma, flush, işlem kilidi, sağlam önceki kaydın .bak yedeği var. Bozuk dosyanın üstüne yazılmaz.
Hakediş oluştur boş IBAN veya geçersiz girdiyle çalışmaz; henüz girilmemiş öğretmenler kayda dahil edilmez. Oluşturulan JSON, basılı resmî belge veya mali kesinleştirme anlamına gelmez.

## Doğrulama / paket

81 birim testi geçti; Excel'in 50 senaryo/14 alan doğrulaması dahil. WPF gerçek ekranında dört panel normal/dar boyutta render edildi; meslek popup'ı, canlı hücre hesabı ve ay/yıl ComboBox üzerinden Kasım-2027 seçimi, Hakediş oluştur düğmesinin kalıcı JSON yazması kontrol edildi. Binding hatası yok. Görseller artifacts/ui-review-hybrid-verified içinde sentetik personel kullanır.
Windows x64 paket: dist/Hakedis-win-x64-20261024-004600.zip. ZIP yalnız Hakedis.App.exe ve KULLANIM.txt içeriyor; gerçek kullanıcı verisi yok. Yayımlanan bağımsız EXE açıldı ve mesaj döngüsünün hazır olduğu doğrulandı; ardından kapatıldı.

## Sonraki ürün işleri

Üç resmî basılı belge, isteğe bağlı Excel/PDF ve banka aktarımı. Diğer eğitim türlerinin bağımsız çekirdekleri kendi kaynakları geldikten sonra eklenecek; şu anda yalnız Temel Eğitim hesap yapar. Diğer modüllere kayıt yapılabilse de Temel Eğitim formülleri uygulanmaz.

## Bu eklemenin doğrulaması

Yeni SGK kuralı için önce eski uygulamada başarısız olan 9 uçtan uca test yazıldı; kural uygulandıktan sonra takvim sınırları da eklendi. Son tam koşuda 81 test geçti; eski 68 teste 13 SGK senaryosu eklendi. WPF ekranında Eksik Gün Sayısı 117 saat/Eylül için 14, hücre 108 saate değişince 15 olarak doğrulandı; binding hatası yok. Görseller artifacts/ui-review-sgk içinde. Yeni paket: dist/Hakedis-win-x64-20261024-004600.zip. ZIP içeriği yalnız EXE ve kullanım notu; bağımsız EXE açılışı doğrulandı.

## Laptop ve aylık süreklilik sorusu — 24 Ekim

Kullanıcı laptop için 1366×768 istedi. Varsayılan pencere bu boyuta alındı. OnSourceInitialized açılış ölçülerini Windows çalışma alanına (DPI ve görev çubuğu alanı) sığdırıyor; alt sınırlar da ekranı aşmıyor. Dört panel 1366×768 ve 1040×728 boyutlarında gerçek WPF render/binding denetiminden geçti; görüntüler artifacts/ui-review-laptop içinde.

Kullanıcı ayrıca her ay değişen öğretmen kadrosunu ve saatlerini güvenli nasıl yöneteceğini sordu. Bu kısmın kapsamı yol gösterme; dönem kilidi/atama geçmişi henüz kodlanmadı. Somut tasarım önerisi docs/AYLIK_CALISMA_AKISI_ONERISI.md içinde. Mevcut açığı gizleme: aylık saatler ve oluşturulan hakediş görüntüleri ayrı olsa da eski çalışma tablosu güncel öğretmen kataloğuyla birleşiyor; silinen kişi orada görünmez, değişen kart bilgisi eski ayın yeniden hesabına yansır. Öneri: görevlendirme geçmişi, dönem kadrosu, boş saatlerle yeni ay hazırlama, kesinleştirme/arşivden okuma, sürümlü düzeltme, eksik ay takibi ve düzenli yedek.
Laptop paketi: dist/Hakedis-win-x64-20261024-004600.zip. 81 test ve WPF kontrolleri geçti; paket yalnız EXE/kullanım notu içeriyor, bağımsız EXE açılışı doğrulandı.

## Gerçek Excel verileriyle yerel deneme — kullanıcı isteğiyle

Kullanıcı dağıtım öncesi temizlemek üzere gerçek Excel değerlerinin eklenmesini ve programın açılmasını istedi. Kaynak Excel değiştirilmeden 10 öğretmenin kimlik/iletişim/SGK/görev/tarih alanları, 972 ders saati ve Eylül-2026 sabitleri aktarıldı. Dönem, bordronun kullandığı E37/F37 hücrelerinden alındı; kullanılmayan U4/V4 içindeki eski Ocak-2023 değerleri alınmadı.

Gerçek veriler yalnız dist/Hakedis-win-x64-20261024-004600/Veriler altında yereldir. Kaynak koda veya mevcut dağıtım ZIP'ine gömülmedi. ZIP hâlâ yalnız EXE ve KULLANIM.txt içeriyor. artifacts/real-data-import içinde özel aktarım girdisi, geçici doğrulama verileri ve yardımcı program var; hepsi Git tarafından yok sayılıyor. Bu klasörlerdeki içerikleri yayınlamayın.

Aktarım önce geçici alanda uygulamanın TeacherDirectory ve MonthlyPayrollStore sınıflarıyla doğrulandı. Ardından hedefte mevcut Veriler bulunmadığı kontrol edilip doğrulanmış dosyalar tek dizin yeniden adlandırmasıyla yerleştirildi. Hedefte önceden kayıt bulunsaydı üzerine yazılmayacaktı. Kaynakla her öğretmenin brüt/net sonucu ve toplamlar karşılaştırıldı: brüt 214.395,70 TL, net 184.380,34 TL. Meslek kodlarının 10'u da gömülü PDF kataloğunda doğrulandı.

Gerçek verileri okuyan Hakedis.App.exe görünür olarak açıldı; Ücretli Ek Ders Hakediş Sistemi başlıklı pencere ve çalışan süreç doğrulandı. Program bilerek açık bırakıldı. Kullanıcı Hakediş Hesapla / Temel Eğitim / Eylül-2026 üzerinden deneyebilir. Gelecek yayına gerçek Veriler veya artifacts içeriklerini dahil etmeyin; kullanıcı denemesi devam ederken bu verileri silmeyin.

## Visual Studio ile çalışma — 24 Ekim

Kullanıcının isteğiyle Visual Studio Community 2026 içinde UcretliEkDers.slnx açıldı. Başlangıç projesi Hakedis.App; IDE başlığı UcretliEkDers (Çalışıyor) olarak doğrulandı ve Debug modundaki Hakedis.App.exe IDE tarafından başlatıldı. Uygulama penceresi öne getirildi; erişilebilirlik ağacında 10 öğretmen / 10 aylık giriş ve Eylül-2026 bilgisi doğrulandı. Visual Studio ve uygulama bilerek açık bırakıldı.

Debug derlemesi 0 uyarı / 0 hata ile tamamlandı. Mevcut yayın klasöründeki gerçek Veriler, daha önce bulunmayan src/Hakedis.App/bin/Debug/net10.0-windows/Veriler klasörüne kopyalandı ve tüm dosyaların hash eşleşmesi kontrol edildi. Yayındaki asıl kayıtlar değiştirilmedi. Bundan sonra Visual Studio'dan çalıştırılan uygulamanın kaydetmeleri bu DEBUG Veriler kopyasına gider; yayımlanmış EXE'nin Veriler klasörüyle otomatik eşitlenmez. Kullanıcının yeni denemeleri için Debug Veriler klasörünü kontrol edin; derleme çıktısı temizliği yaparken bu gerçek kayıtları silmeyin. Dağıtım paketine gerçek Veriler eklenmez.

Kullanıcı yeniden açmak isterse yeni proje kökündeki UcretliEkDers.slnx çözüm dosyasını seçmeli; Hakedis.App başlangıç projesiyle F5 çalıştırır, Shift+F5 hata ayıklamayı durdurur.
