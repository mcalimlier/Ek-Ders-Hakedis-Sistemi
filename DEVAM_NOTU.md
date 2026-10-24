# Son değişiklik — 24 Ekim 2026: Din Öğretimi bağımsız çekirdeği

- Din Öğretimi kullanıcının yeni Excel'inden ayrı `Hakedis.DinOgretimi.Core` projesiyle eklendi. Yalnız Contracts'a başvurur; kendi girdi/parametre/sonuç/takvim tipleri vardır. Temel Eğitim hesaplayıcısı çağrılmaz. Yeni enum `DinOgretimi` sona eklendi; eski enum/dosya/kural anahtarları değişmedi. İmam Hatip Lisesi eski bekleyen modül anahtarı yeniden kullanılmadı.
- Storage/PayrollCores tek uygulama yönlendirme noktasıdır. Din sonucunu mevcut ortak bordro/arşiv sözleşmesine açık alanlarla eşler. Arayüzdeki tüm modül seçimleri aynı katalogdan gelir. Aylık/hakediş/SGK/kişisel PDF doğru modülün çekirdeğini kullanır. Ortak sabitler ve ücretli öğretmen SGK işyeri profili korunur.
- Öğretmenler, aylık kayıtlar ve arşivler modülle ayrıdır; aynı TC iki modülde tutulamaz. Aynı ay iki ayrı modül kaydedilebilir; aynı modülde ikinci hakediş engellenir. Önceki ay personel sayısı modüle göre hesaplanır. Geçmiş düzenleme onayla aynı kaydı değiştirir; arşivde kullanılan sabitler güncel sabitlerden etkilenmez.
- Excel'deki `Bordro!Z8` silinmiş kesinti formülü, `F9` elle yazılmış saat, Personel Bildirimi `#REF!`, kullanılmayan AGİ `#NAME?` ve kişisel sayfadaki ana bordrodan farklı vergi formülleri tespit edildi. Bunlar hesap kuralı olarak taşınmadı. Sağlam I7:AA7 formülleri esas alındı; detaylar docs/DIN_OGRETIMI.md. Kaynak değiştirilmedi, gerçek öğretmenler otomatik içe aktarılmadı.
- Kaynak salt okunur Microsoft Excel'de CalculateFullRebuild ile 50 senaryo/14 sonuç için hesaplandı. Anonim karşılaştırma fixture'ı tests/Hakedis.Tests/Fixtures/din-excel-recalculated.json; özel isim/TC/IBAN içermez. Örnek 117 gündüz saati için net 22.193,93 TL.
- Son 178 birim testi geçti (14 yeni Din Öğretimi test vakası). Bağımsız derleme bağımlılığı, Excel karşılaştırması, canlı/aylık/modül ayrımı, kayıt/düzenleme, takvim hatasının parayı etkilememesi, bozuk tutar/kural sürümü, taslak modül kontrolü ve A/E/İ XML vakaları kapsandı.
- WPF akış testi önceki bütün kontrolleri ve Din modül seçimi → aylık giriş → canlı hakediş → başarılı bildirim/Çıktı → üç PDF → kişisel PDF/Klasör → İ mahiyetli XML akışını geçti; binding hatası yok. Günlük artifacts/din-ui-flow.log, görüntüler artifacts/ui-din-verified. Testler yalnız yapay kişiler kullanır; SGK'ya yükleme veya fiziksel baskı yapılmadı.
- pypdf/lxml bağımsız kontrolünde dört Din PDF'si tek sayfa, kişi/TC/net değerleri doğru; ana bordro yatay A4, kişisel bordro dikey A4. Özgün XSD XML'i doğruladı. Dört PDF'nin Poppler görüntüleri ve iki yeni ekran görsel olarak incelendi.
- Debug ve Release WPF derlemeleri sıfır uyarı/hata ile geçti. Günlükler artifacts/din-debug-build.log ve artifacts/din-ui-build.log. Son test günlüğü artifacts/din-tests-final.log.
- Gerçek veri yedeği artifacts/private-backups/before-din-ogretimi-* altında; manifest yolu artifacts/din-backup-path.txt. 19 gerçek veri dosyasının içerikleri ve dosya listesi ile kaynak Excel SHA-256 değişmedi. Yeni paket/ZIP hazırlanmadı. Program otomatik açılmadı; Visual Studio F5 güncel Debug sürümünü çalıştırır.
- İmza kullanıcının son düzeltmesine göre artık tam olarak `Created by Murat ÇALIMLIER`; WPF kontrolü de bu metni bekler.

# Önceki değişiklik — 24 Ekim 2026: kişisel bordro ve menü grupları

- Kullanıcının beş maddesi uygulandı: üst menüde Veri Girişi / Kurum ve İmza Bilgileri / Hakediş Hesapla / Öğretmen Listesi / Çıktı; ayrı alt grupta Kişisel Bordro ve onun altında Dönem Sabitleri. Sol altta program adı, altın çizgi ve tam olarak created by Murat ÇALIMLIER imzası var. Referans OEHS arayüzünün mevcut renk/kart dili kullanıldı.
- Eski Ücretli Öğretmen Listesi adı güncel arayüz başlığı, sol düğme ve kullanım belgesinde Öğretmen Listesi yapıldı. İç Teachers anahtarı ve JSON veri şemaları değiştirilmedi.
- PersonalPayrollViewModel yeni seçim modeli: modül/ay/yıl ile yalnız kayıtlı hakedişteki öğretmenler (ad/soyad, TC) listelenir; mevcut ana öğretmen kataloğu veya canlı taslaktan veri çekmez. Silinmiş öğretmenin geçmiş bordrosu kendi arşiv kaydından açılır. Kaynak SHA sürümü değişmişse PDF öncesi yenileme gerekir. Boş dönem/eklenmemiş modülde çıktı engellenir.
- Bordro oluştur tek öğretmen için A4 dikey PDF yazar. Güncel sabitlerle yeniden hesap yapmaz; kayıtlı girdi/sonuç/kurum/imzaları kullanır. Banka/kurum/imzalar kaynakta yoksa mevcut ortak bilgiler kullanılır. PDF kimlik/IBAN/görev/ders bilgilerini, ücret/kesinti/net ve SGK yan bilgisini içerir. Klasör düğmesi yalnız başarılı kayıt sonrası etkinleşir; son PDF'nin klasörünü açar. Dönem/öğretmen değişince eski dosyaya ilişkin seçim temizlenir; PDF silinmez.
- Çıktı: Masaüstü/ÜcretliUsta Öğretici Hakediş Sistemi/Çıktılar/Kişisel Bordrolar/modül/YYYY-MM/benzersiz-klasör/Kişisel Bordro.pdf. Zaman + tam GUID klasöründe tek PDF, ortak atomik/staging yazıcısı ile hazırlanır. İlk derin test yolu 260 karaktere ulaştığı için gereksiz öğretmen GUID/sürüm tekrarları çıkarıldı; kısa yol bağımsız PDF okuyucusunda tekrar doğrulandı.
- Dönem Sabitleri dokuz alanı üç eşit sütun/üç satır yapıldı; etiket yüksekliği sabit ve textbox genişlik/Y konumları eşit. Güncelle / Dönem sabitlerini değiştir ve salt okunur davranışı korundu.
- Altı lacivert/altın kart aynı ölçüdedir. Simge üst başlık satırında, tutarlar ayrı alt satırdadır; büyük değerler taşmadan ölçeklenir. Ödenek satırlarında etiket/tutar boşlukları korunur. WPF geometrik testinde 100.000 saatlik büyük tutarlar dahil 1366 ve 1040 genişlikte simge çakışması/taşma yok.
- 164 birim testi geçti; altı yeni kişisel seçim testi var. WPF bütün eski akışları ve yeni kişisel PDF/Klasör olaylarını geçti; bağlama hatası yok. Klasör açma OS sınırı testte yakalanarak doğru dizin doğrulandı, test amacıyla Explorer açılmadı. Tek kişilik PDF pypdf ile okundu: bir dikey A4, kimlik/IBAN ve 12 parasal alan kaynak arşivle aynı. Poppler render ve iki ekran ölçüsündeki görüntüler incelendi.
- Testler artifacts/ui-personal-compatible; günlükler artifacts/personal-*.log. Kullanım ve README güncellendi. Plan: docs/superpowers/plans/2026-10-24-personal-payroll-and-sidebar.md. Eski modüllere otomatik Temel Eğitim hesabı yapılmaz; yeni çekirdek gelince kişisel çıktı eşlemesi de eklenmelidir.
- Veri yedeği artifacts/private-backups/before-personal-payroll-20261024-201531/Veriler; manifestteki **19 gerçek dosya ve dosya listesi aynı**. Gerçek kişi/ödemeler üzerinde düzenleme/silme/test kaydı yapılmadı. Bu tur dağıtım ZIP'i üretilmedi. Debug ve Release güncel kaynakla sıfır uyarı/hatayla derlendi; gerçek uygulama otomatik açılmadı. Visual Studio F5 güncel arayüzü açar.

---

# Son değişiklik — 24 Ekim 2026: mahiyet T/TİS açıklaması sorgulandı

- Kullanıcı son XML panelindeki T'nin kaynağını sordu; normal seçeneklerin Asıl/Ek/İptal olduğunu ve İptal için İ beklediğini söyledi.
- Kaynak tekrar incelendi: kullanıcının XSD'sinde BELGEMAHIYET yalnız length=1 ve default=A ile tanımlı, değer listesi yok. T, SGK'nın eski xls2xmly.xls örneğinin Module1.bas / Input_Check kontrolünden gelmiş; E10 için A/E/T kontrol ediyor. Aynı hata metni yalnız Asıl=A, Ek=E anlamlarını açıklıyor; T anlamını vermiyor. Makrolar çalıştırılmadı.
- Önceki T=Toplu İş Sözleşmesi/TİS açıklaması doğrulanmamış yorumdu. Ekrandaki ve hata metnindeki yanlış anlam kaldırıldı.
- Kullanıcı soruya açıkça A=Asıl, E=Ek, İ=İptal yanıtını verdi. Buna göre seçenekler A — Asıl / E — Ek / İ — İptal yapıldı; XML'e kullanıcı onaylı **Türkçe büyük İ (U+0130)** aynen yazılır. T ve noktasız I kabul edilmez. Eski geçersiz profil kodu otomatik eşlenmez; XML ekranında seçim boş gelir ve yeniden seçilmelidir. Açılışta profil dosyasına yazılmaz.
- Ayrıntı ve XSD parçası docs/SGK_MAHIYET_KAYNAK_KONTROLU.md; docs/SGK_XML.md güncellendi. XSD kontrolü iş anlamındaki kod doğrulamasının yerine geçmez. Gerçek dosyalar değiştirilmedi.
- Doğrulama: 158 Release testi geçti. WPF testi gerçek ComboBox'tan İ — İptal seçip XML'de BELGEMAHIYET="İ" değerini ve özgün XSD doğrulamasını kontrol etti; bütün eski akışlar geçti, binding hatası yok. Görüntü artifacts/ui-sgk-nature/SGK-cancellation.png. Release derlemesi sıfır uyarı/hata. Açık Debug uygulaması/Visual Studio dosya kilidi nedeniyle Debug kopyası güncellenemedi; kullanıcının açık işine müdahale edilmedi. Visual Studio'da durdurulup F5 ile yeniden derlenince düzeltme görünür. Bu tur dist ZIP yeniden üretilmedi.

---

# Son değişiklik — 24 Ekim 2026: yeni çekirdekler öncesi kapsamlı denetim

- Kullanıcı diğer Excel'leri göndermeden önce mevcut Temel Eğitim çekirdeğinin ve uygulamanın doğrulanmasını istedi. Denetim tamamlandı; rapor docs/CEKIRDEK_ONCESI_DENETIM_2026-10-24.md. Gelecek çekirdekler henüz eklenmedi.
- 131 testten 151 teste çıkıldı; Debug ve Release 151/151 geçti. Kaynak Excel, salt okunur Microsoft Excel CalculateFullRebuild ile yeniden hesaplandı: 50 senaryo / 14 hesap alanı eşleşiyor; 850 sayısal referans alanı da eski fixture ile aynı. Kaynak XLSX SHA-256 değişmedi.
- Aylık Kaydet, Hakediş oluştur ve öğretmen listesi Kaydet işlemlerinin eski ekranla daha yeni kaydı ezmesi kapatıldı. Farklı aylık içerik veya öğretmen sürümü reddedilir; aynı içeriğin güvenli yeniden denenmesine izin verilir. Başka pencerede yeni girilen öğretmenin saatleri sessizce dışarıda bırakılamaz.
- Kaydedilmemiş hücre/aylık bilgi varken dönem/modül seçimi kilitlidir; önce Kaydet/Değişiklikleri kaydet ya da onaylı Çalışmayı temizle gerekir. Kapanışta aylık, yeni öğretmen, öğretmen listesi, sabit ve kurum bilgi taslakları için varsayılan Hayır uyarısı var. Bu, kaydedilmemiş metnin elektrik kesintisinde otomatik kurtarıldığı anlamına gelmez.
- Arşiv sonuçları artık kendi kayıtlı girdileri ve sabitleriyle bütün alanlarda karşılaştırılır; bozuk sonuç sessizce kabul edilmez ve özgün dosya değiştirilmez. Dolu TCKN tekrarları reddedilir. Aşırı büyük aylık sayılar ve ortak sabitler dosyaya ulaşmadan doğrulama hatası verir. Sıfır saatli kayıt geçerlidir.
- Kurum/imza ve hakediş doğrulaması aylık dosya yazılmadan önce yapılır. Kurtarma günlüğüyle geri alınmış aylık girdiyi eski ekrandan ezmeye çalışma artık reddedilir; yenilemeden sonra kayıt testlidir. İlgili eski testler, yeni güvenli geçiş için açıkça kaydetme/temizleme/yenileme yapacak şekilde güncellendi; hesap beklentileri değiştirilmedi.
- 8 öğretmen Eylül, 4 ayrılış + 6 yeni girişle 10 ödeme satırlı Ekim, değişen saatler ve yeni yıl senaryosu geçti. Eski aylık hakediş byte düzeyinde aynı kalır. Eksik gün, onaylı silme/değiştirme, tek kayıt ve kesintili hibrit işlem kurtarma testleri de geçti.
- WPF: artifacts/ui-core-audit-final-151 altında 12 panel görüntüsü, 1366/1040 genişlik, bütün eski akışlar ve beş tür taslak için kapanış Hayır/Evet geçti; binding hatası yok. Gerçek arşivden 3 tek A4 PDF ve 10 kişilik XML özel test klasörüne üretildi. Python/lxml özgün XSD, her kişinin PEK/gün ve PDF sayfa/yön kontrollerini ayrıca geçti. SGK yükleme ve fiziksel yazdırma yapılmadı.
- Başlangıç yedeği artifacts/private-backups/before-core-audit-20261024-180112/Veriler. Manifestteki **19 gerçek veri dosyası ve dosya listesi değişmedi**. Güncel gerçek 975 saatlik verileri eski 972 saatlik örnekle değiştirmeyin. Kaynakta/Git'te gerçek kişi/SGK profil verisi yok.
- Debug uygulaması yeniden derlendi: sıfır uyarı/hata. Gerçek uygulama açılıp veri değiştirilmedi. Bağımsız temiz EXE/ZIP dist/Hakedis-win-x64-20261024-182518.zip üretildi ve son sabit sınır kontrolüyle güncellendi (151 Release testi geçti). ZIP yalnız EXE + KULLANIM.txt içeriyor; Veriler yok. Kurulu .NET arama yolları kapatılmış ayrı süreçte EXE penceresi açıldı, yanıt verdi ve normal kapandı. Bu paket sentetik/boş test içindir; kullanıcının gerçek Veriler klasörünü değiştirmez. Mevcut yayın betiği portable EXE/ZIP'tir; installer henüz yoktur.
- MODUL_MIMARISI.md ve README güncellendi: mevcut tablo/arşiv/çıktı tipleri ilk çekirdeğe bağlıdır. Yeni Excel'lerde ayrı Core + module yönlendirme + girdi/sonuç/arşiv kuralı/çıktı uyarlayıcıları gerekecek. Yalnız katalog kaydı yapmak yeterli değildir; diğer modüle Temel Eğitim hesabı çalıştırılmayacak.

---

# Son değişiklik — 24 Ekim 2026: hakediş silme ve düzenlemede tek kayıt

- Çıktı listesinde düğme sırası PDF oluştur / Yazdır / Düzenle / Sil / XML oluştur. Beş düğme 100×32; Sil hafif kırmızı tonludur. 1366 ekranda beşi birlikte görülecek şekilde bilgi sütunları ayarlandı.
- Sil varsayılan Hayır olan onay penceresinde modül, dönem ve kişi sayısını gösterir. Onay yoksa hiçbir kayıt değişmez. Onay varsa yalnız ilgili hakediş aktif listeden kaldırılır; öğretmenler, aylık girişler ve diğer dönemler korunur. Kaynak JSON, Veriler/SilinenHakedisler/modül altına atomik taşınarak kurtarma amacıyla tutulur. Bu klasör Çıktı listesinde taranmaz. Silinen dönem yeniden oluşturulabilir.
- Silme dönem kilidi ve kayıt yazma kilidi altında SHA sürüm kontrolü yapar. Başka pencerede değişmiş kayıt silinmez. Kurtarma klasörüne taşımak başarısızsa özgün kayıt yerinde kalır.
- Düzenle / Değiştir-Kaydet onayı açıklaştırıldı: eski hakediş yenisiyle değiştirilir, listede yalnızca bir kayıt kalır. Mevcut atomik aynı dosyaya yazma mantığı korundu; eski sürümün .bak kurtarma yedeği listede görünmez. Onaysız kaydetme veya Vazgeç eski kaydı değiştirmez.
- 131 birim testi geçti. WPF testi beş eşit düğmeyi, silme Hayır/Evet, düzenleme Hayır/Evet ve düzenleme sonrası tek kayıt kaldığını gerçek olay işleyicileriyle doğruladı. Diğer PDF/XML ve panel testleri de geçti; binding hatası yok. Derleme sıfır hata/uyarıyla tamamlandı.
- Gerçek verilerde silme/düzenleme testi yapılmadı; tüm denemeler sentetik verilerle yapıldı. Başlangıç yedeğindeki bütün gerçek veri dosyaları SHA-256 karşılaştırmasında değişmedi. Yedek artifacts/private-backups/before-archive-delete-*; ekranlar artifacts/ui-archive-delete-review altında.

---

# Son değişiklik — 24 Ekim 2026: tek hakediş, altın kartlar, SGK XML

- Aynı modül/yıl/ay için ikinci hakediş oluşturulamaz. Dönem dosyası kilidi kontrolün başından kayıt bitene kadar tutulur. Eski kayıt ve aylık dosya ikinci denemede değişmez; kullanıcı Çıktı / Düzenle'ye yönlendiren hata alır. İki eşzamanlı pencere yarış testi geçti. Mevcut kayıtlar otomatik silinmez.
- Kayıtlı hakediş satırlarında PDF oluştur, Yazdır, Düzenle, XML oluştur aynı 100×32 ölçüdedir. 1366 ekranda dördü de görünür; dar ekranda yatay kaydırma korunur. Kayıt zamanı yerel saatle gösterilir.
- Altı kartın boyutları korunarak lacivert geçiş, altın tonlu kenarlar, ince ışık çizgisi ve vektör para/transfer/kalkan/kurum/saat/kesinti simgeleri eklendi. İki ölçüde WPF görüntüleri kontrol edildi.
- XML oluştur seçili kaydın SGK ekranını açar. İşyeri, ad/soyad ayrımı, eksik gün ve çıkış nedenleri kontrol edilir. Kontrol onayı olmadan çıktı yazılmaz. Ad ve soyad en fazla 18 karakter; hiçbir alan otomatik kısaltılmaz. Kayıt açık ekran sırasında değişirse eski sürümle XML alınması engellenir.
- Kullanıcının verdiği XSD 2.1 byte düzeyinde aynı gömülü kaynak olarak kullanılır (SHA-256 pin testi var). Şema doğrulaması olmadan dosya yazılmaz. UTF-8, BOM yok; PEK kaynak kaydın Gross toplamıdır. Tarihler SGK resmî Excel'indeki GGAA biçiminde yalnız ilgili ay/yıldaysa yazılır. Eksik gün 07 kodunun iki hanesi korunur. İsteğe bağlı boş alanlar eklenmez.
- SGK profilleri Veriler/SgkXml-Ogretmenler.json ve SgkXml-UstaOgreticiler.json dosyalarındadır. Kullanıcı öğretmenler ve gelecekteki usta öğreticiler için ayrı 26 haneli numaralar verdi, 21+2+3 ayrımını onayladı. Öğretmen profiline kullanıcı onaylı işyeri unvan/adres, belge türü 13, kanun 00000, eksik gün 07 kaydedildi. Usta öğretici profilinde yalnız verilen sicil şimdilik var; hesaplama çekirdeği açılmadı. Gerçek işyeri numaraları kaynak/Git içinde yok. Dağıtımda özel Veriler klasörü temizlenmeli.
- 128 birim testi, WPF akış testi ve gerçek kayıt üzerinden XML üretim testi geçti. Gerçek 10 sigortalının XML'i hem .NET hem bağımsız lxml ile kullanıcının özgün XSD'sine göre doğrulandı; PEK toplamı kaynakla eşit. Başlangıç yedeğindeki bütün veri dosyaları byte düzeyinde aynı. SGK'ya yükleme yapılmadı; şema doğrulaması kurumun son iş kuralı kontrolünün yerine geçmez.

Yedek: artifacts/private-backups/before-sgk-xml-*; testler artifacts/ui-sgk-final ve artifacts/private-sgk-final altında, Git dışında. Teknik eşlemeler docs/SGK_XML.md içinde. Güncel Debug sürümü Visual Studio içinde F5 ile açıldı ve açık bırakıldı. Canlı pencere yanıt veriyor. Yeni dağıtım paketi üretilmedi.

---

# Son değişiklik — 24 Ekim 2026: referans PDF'ler ve Çıktı akışı

- Hakediş oluştur başarılı olunca Tamam ile kapanan başarı bildirimi görünür; kullanıcı Çıktı paneline yönlendirilir ve son kayıt seçilir. Hatalı kayıt başarı bildirimi üretmez.
- Kayıtlı Hakedişler tablosunda bütün metin ve başlıklar ortalıdır. Çıktı-1/2/3 etiketleri belge adlarından, metinlerden ve arayüzden kaldırıldı.
- Kurum ve İmza Bilgileri menüsü Veri Girişi ile Hakediş Hesapla arasına eklendi. Kurum/banka/imzacılar burada düzenlenir; hastalık izni beyanı da değiştirilebilir. Bu bilgiler yeni hakedişte dondurulur.
- Banka PDF başlığı kullanıcının ekran görüntüsündeki etiket/değer yapısındadır; ay Türkçe büyük harftir. Toplam satırındaki etiket bölünmez.
- Bordro PDF bütün alanları iki kompakt yatay bölümde, dar kenar boşluklarıyla TEK yatay A4 sayfasında gösterir. Gövde yazısı en az 6 puntodur. Sığmayan aşırı yoğun bordroda satır kesilmez veya ikinci sayfa üretilmez; açık hata ile çıktı oluşturulmaz. On öğretmenlik gerçek kayıt ve uzun isimli sentetik kayıt tek sayfada doğrulandı.
- Personel Bildirimi verilen PDF'nin A4 koordinatlarıyla tekrar çizildi: dört ayrı yıl rakamı, ay, I/II tablolar, altı boş hastalık satırı, imzalar ve dipnotlar. Tahakkuk/net kayıtlı sonuçlardan gelir.
- Personel sayımı employment-dates.v2: önceki sayı aynı modülün önceki takvim ayının son hakedişinden alınır; giriş/çıkışlar normal StartDate/EndDate ay ve yılıyla sayılır. Ödeme yapılacak sayı mevcut bordro satır sayısıdır. Katalogda bulunan ama ödeme satırında olmayan ayrılanlar da sayılır. Yeni hareket bilgisi kayıt anında dondurulur. Eski hareket hesabı çıktıda bellekte düzeltilir; kaynak JSON değiştirilmez. Ocak/Aralık sınırı, diğer modül, eski başlangıç tarihi ve sonradan katalog değişikliği testlidir.
- Altı kartın önceki boyutu WPF ölçüm testiyle karşılaştırılarak korundu. Tahakkuk/net aynı kartta; SGK iştirakçi/işveren aynı kartta. Ödenek kartı 01.04_Kişi ücreti = Gross toplamı, 02.04_SGK Ücreti = EmployerPremium toplamı. Diğer kartlar SGK'ya esas ücret, toplam ders saati ve kesinti toplamıdır.
- Standart WPF yazıcı seçimi penceresi belge önizlemesi sunmuyor. Üç belgenin ortak yazıcı paginator'ü testlidir; fiziksel yazıcıya iş gönderilmedi.

VERİLER: Çalışma başında artifacts/private-backups/before-reference-outputs-* altına tam Veriler yedeği alındı. Güncel gerçek kayıt 10 öğretmen / 975 saat / 1 kayıtlı hakediş içeriyordu. Üç gerçek PDF test klasörüne üretildi; toplamlar kayıtla eşit. Yedekteki bütün veri dosyaları byte düzeyinde değişmeden duruyor. Gerçek kişi bilgileri, PDF'ler ve test görüntüleri Git dışında.

Güncel Debug sürümü Visual Studio içinde F5 ile açıldı ve açık bırakıldı. Canlı pencerede yeni Kurum ve İmza menüsü ve 10 öğretmen kaydı doğrulandı; süreç yanıt veriyor.

Doğrulama: 115 birim testi geçti; WPF testinde 12 panel görüntüsü, kart ölçüleri, başarı bildirimi/yönlendirme, merkezleme, eski sayımın bellekte düzeltilmesi, yoğun bordro sınırı, PDF ve yazıcı sayfaları kontrol edildi. Testler artifacts altında sentetik verilerle çalışır. --verify-local-archive seçeneği gerçek arşivi salt okunur inceleyip özel test çıktısı üretir.

---

# Son değişiklik — 24 Ekim 2026: ortak sabitler, geçmiş ve üç çıktı

Kullanıcının bu turdaki dört maddesi uygulandı. Önceki bölümler tarihçedir; güncel davranış:
- Ay içinde işe giriş/ay içinde işten ayrılış alanları MonthlyInput, PayrollRow, PeriodWorkspace, arayüz ve yeni JSON'dan kaldırıldı. SGK hesabı yalnız TeacherRecord.StartDate/EndDate kullanır. Eski anahtar adları yalnız JsonDraftStore okuma geçişinde tanınır. Eski hakediş sonuçları yeniden hesaplanmaz.
- Ücretli Öğretmen Listesi iki yan yana karttır. Solda yalnız ad-soyad/TC; seçim sağdaki tüm kalan alanları açar. Sağ kart kendi içinde kayar.
- Dönem sabitleri panelinde modül/ay/yıl seçimi yok. OrtakSabitler.json bütün canlı ve sonraki hesapları besler. Alanlar salt okunur; Güncelle açar, Dönem sabitlerini değiştir kaydeder ve tekrar kilitler. Aktif önizleme canlı değişir; kayıtlı aylık dosyalar ve hakediş görüntüleri yalnız sabit değişikliğiyle yazılmaz. Geçmiş düzenlemesi kaydın sabitleriyle çalışır. Emekli grupları henüz eklenmedi.
- Çıktı paneli hakediş-*.json görüntülerini listeler. Satır başına PDF oluştur / Yazdır / Düzenle vardır. PDFsharp-WPF 6.2.4 (MIT) ile Excel gerektirmeden Bordro (Çıktı-1), Banka Listesi (Çıktı-2), Personel Bildirimi (Çıktı-3) üretilir. PDF ve doğrudan yazdırma aynı sayfa modelini kullanır. Fiziksel yazıcıya test işi gönderilmedi; tek işte bütün sayfalar, sığdırma ve otomatik yön testi yapıldı.
- PDF hedefi: Masaüstü/ÜcretliUsta Öğretici Hakediş Sistemi/Çıktılar/modül/YYYY-MM/kayıt-sürümü. Üç belge ayrı PDF; uzun tablo birden çok sayfa olabilir. Bordro A4 yatay, başlıklar kısaltılmadan iki tablo; banka/personel A4 dikey. Tamamlanmadan son klasör görünmez, eski PDF'ler ezilmez.
- Kaynak Excel'deki kırık #REF ve eski bütçe yılı düzeltilerek kaydın dönemi/sonuçları kullanılır. AGİ için kişiye özel eski açıklama taşınmaz. Personel hareketleri önceki ayın son kayıtlı kadrosuyla kıyaslanır; önceki ay yoksa 0 başlangıç ve kontrol notu vardır. Girenlerin isim listesi eklenir. Hastalık izni kaynağı olmadığı için ilgili tablo personel birimine bırakılır; yoktur diye uydurulmaz.
- Kullanıcı kurum/banka/imza bilgilerinin Excel'den başlangıçta alınmasını ayrıca onayladı. Bu bilgiler yalnız yerel Veriler/BelgeBilgileri.json içine aktarıldı; Çıktı panelinde düzenlenebilir. Yeni görüntü içine kopyalanır; eski görüntüde bilgi yoksa paneldeki bilgiler kullanılır. Kaynak Excel değiştirilmedi; kişi/kurum/imza verileri Git'e alınmaz.
- Geçmiş Düzenle aktif iş/form/sabit değişikliği varken engellenir. Önce Hakediş oluştur veya onaylı Çalışmayı temizle gerekir. Temizle hiçbir kayıtlı dosyayı silmez. Geçmiş ayrı kopyada açılır; normal kayıt/öğretmen silme düğmeleri yerine yalnız Değiştir/Kaydet ve Vazgeç kullanılır. Onaylı kayıt yalnız seçili hakediş dosyasını değiştirir, .bak korur; aylık ve ana öğretmen dosyası değişmez. Vazgeç/onaysız kayıt dokunmaz. Başka süreçte değişen sürüm SHA256 kontrolüyle reddedilir. Silinmiş öğretmen de arşiv kopyasından açılır.

Doğrulama: 112 birim testi geçti; 10 WPF panel görüntüsü, iki boyutta yerleşim, sabitlerin salt okunur/düzenleme döngüsü, geçmiş iptali ve PDF/yazıcı sayfalaması doğrulandı; binding hatası yok. Sentetik 1 ve 65 öğretmenle altı PDF üretildi. Gerçek PDF'ler Poppler ile render edildi; tüm sayfalar artifacts/pdf-render-outputs-final/contact.png üzerinde incelendi. Türkçe karakter, katsayının altı ondalığı, toplamların kaynak kayda eşitliği ve sayfa sınırları denetlendi. Görseller/PDF'ler test amaçlıdır; Git dışında.

GERÇEK VERİLER: Kullanıcı son denemesinde saatleri değiştirmiş: güncel Debug verilerinde 10 öğretmen / 10 aylık kayıt / **975 saat** var. Kaynak örnekteki 972 saati tekrar yüklemeyin! Değişiklik öncesi 8 dosya artifacts/private-backups/before-outputs-20261024-150510/Veriler içine kopyalandı; tüm hashler doğrulandı. Geçişte yalnız bir aylık dosyanın kaldırılmış tarih anahtarları çıkarıldı; diğer tüm aylık alanlar ve sabitler eşit kaldı. Ana öğretmen kataloğu byte düzeyinde aynı. Mevcut hakediş arşiv sayısı **0**; dolayısıyla Çıktı listesinin dolması için kullanıcının Hakediş oluştur düğmesini kullanması gerekir. Otomatik gerçek hakediş veya ödeme kaydı yaratılmadı.

Güncel Debug uygulaması Visual Studio içinde F5 ile başlatıldı; Ücretli Ek Ders Hakediş Sistemi penceresi açık bırakıldı.

Aktif yol: src/Hakedis.App/bin/Debug/net10.0-windows/Veriler. Önceki dist programı ve verileri ayrıdır; otomatik eşitlenmez. Yeni dağıtım ZIP'i bu turda hazırlanmadı. Sonraki ürün işi: isteğe bağlı Excel/banka aktarım dosyası ve diğer modül çekirdekleri.

---

# Son değişiklik — 24 Ekim 2026: hibrit öğretmen düzenleme

Kullanıcının beş maddesi uygulandı:
- SİGORTA NO, görevlendirildiği branş, görev yeri 2 ve ikinci görev yeri branşı; TeacherRecord, TeacherEditor, formlar, tablolar ve yeni JSON kayıtlarından çıkarıldı. Eski alan adları yalnız şema 1/2 uyumluluk kodunda ve geçiş testlerinde kaldı. Şema 3 yazılıyor; eski dosyanın .bak yedeği korunuyor. Eski hakediş görüntüleri değiştirilmedi.
- Bordro hibrit tablosu yalnız VERİ GİRİŞİ alanlarını kaynak sırasıyla gösterir (kaldırılan dört sütun hariç). 15 sütun: sıra, ad soyad, TC, IBAN, başlangıç/ayrılış ve ay içi tarihler, SGK meslek kodu, görev yeri, İBAN KONTROL, gündüz/DYK saatleri, SAAT TOPLAMI, icra. Sıra/kontrol/toplam otomatik; diğer hücreler düzenlenebilir. Hesaplanan bordro/SGK sütunları tablodan çıkarıldı; altı toplam kartı ve aylık SGK özeti korundu. Satırlar 30; hücreler yatay/dikey ortalı.
- Başlık yanındaki Sil/Düzenle yalnız seçili satır varsa etkin. Sil ana öğretmen kaydını onayla siler; geçmiş dosyalar korunur.
- Düzenle öğretmen formunda kopya açar; modül/aylık kart kilitlidir. Öğretmen Değişiklik kaydet / Vazgeç düğmeleri görünür. Kaydet bütün ekranları günceller, Hakediş Hesapla'ya döner ve Tamam ile kapatılan Kayıtlar güncellendi bildirimi verir. Vazgeç hiçbir kaydı değiştirmez; önceki tablo taslakları korunur. Döngü sonunda düğmeler Öğretmeni Kaydet / Formu Temizle olur.
- Değişiklikleri kaydet düğmesi tablonun altında, yalnız değişiklik varken etkin. Öğretmen ve aylık bilgiler önce tüm satırlar için doğrulanır, sonra iki dosya kalıcı işlem günlüğüyle yazılır. Hatalı satır, yinelenen TC veya eski ekran çakışması kayıt yazmaz. Yarım işlem açılışta ve sonraki yazmadan önce tamamlanır/geri alınır. OEHS HybridSourceUpdate/HybridRow örnekleri kullanıldı.

Visual Studio içinde güncel Debug sürümü F5 ile başlatıldı ve Veri Girişi ekranı açık bırakıldı. Gerçek uygulama erişilebilirlik ağacında 10 öğretmen / 10 aylık giriş ve 0 binding hatası doğrulandı.

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
