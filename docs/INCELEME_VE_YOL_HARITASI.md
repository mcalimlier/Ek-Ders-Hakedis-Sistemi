# Temel Eğitim Ücretli Öğretmen Hakediş Programı

İnceleme tarihi: 23 Ekim 2026.

## 1. Kapsam ve önerilen yön

İlk sürüm temel eğitim ücretli öğretmen ek ders ödemelerine ayrılacaktır. Kullanıcının açıklaması gereği usta öğretici Excel'i daha sonra ayrı bir hesaplama modülü olarak eklenecektir. Şimdi usta öğretici hesapları uygulanmayacak; ortak çekirdek bu genişlemeye uygun tutulacaktır.

Öneri: Windows üzerinde çevrimdışı çalışan C# / WPF masaüstü uygulaması; kurum ve dönemlere ayrılmış JSON dosyaları; Excel'den bağımsız, test edilebilir hesaplama motoru. Görsel dil mevcut OEHS dalından alınır, ücretli öğretmen iş akışı yeniden kurulur. Tek bilgisayarda yerel kullanım önerilen başlangıç varsayımıdır; çok kullanıcılı ağ erişimi henüz kararlaştırılmadı.

Bu belge bir inceleme ve önerilen yol haritasıdır. Ürün kodu oluşturulmadı; referans uygulama çalıştırılmadı. Arayüz değerlendirmesi XAML ve ilgili kaynak koduna dayanır. Kaynak çalışma kitapları değiştirilmedi. Belgelerdeki açıklamalar kaynak verisi ve dosya biçimi bilgisi olarak değerlendirildi; kullanıcı talimatı yerine geçirilmedi.

## 2. İncelenen kaynaklar

- Arşivdeki `01-Eylül 2026 - Temel Eğitim Ücretli Ek Ders Hakediş.xlsx`.
- Arşivdeki `Maas_Kesinti_Excel.xls`: bağımsız bir bordro motoru değil, Halkbank toplu ödeme/kesinti şablonu.
- GitHub: https://github.com/mcalimlier/OzelEgitimHakedisSistemi/tree/feature/official-payment-document-templates
- İncelenen dalın yerel commit'i: `284e66bf221d20b7d51becfdedba616a3673014c`.
- Arayüz kaynakları: `src/OzelEgitimHakedis.Wpf/App.xaml`, `MainWindow.xaml`; ayrıca proje dosyası, Excel dışa aktarma ve çekirdek servis yapısı.

## 3. Excel'in gerçek yapısı

| Sayfa | Formül | İşlev |
|---|---:|---|
| E-Bildirge Verileri | 172 | Kimlik, meslek kodu, giriş/çıkış, prim günü ve prim tutarlarının bildirime hazırlanması |
| VERİ GİRİŞİ | 58 | Personel, kurum, IBAN, gündüz/DYK saati, icra, dönem ve katsayı |
| Bordro (Çıktı-1) | 784 | Brüt, işveren maliyeti, vergi, SGK, kesinti ve net hesapları |
| Banka Listesi (Çıktı-2) | 179 | Personel bazında net ödeme ve IBAN listesi |
| Personel Bildirimi (Çıktı-3) | 16 | Personel hareketleri ve tahakkuk bildirimi |
| KişiselBordro (Talep Edilirse) | 18 | Seçilen kişinin bireysel bordrosu |
| **Toplam** | **1.227** | |

Dört tanımlı ad: KATSAYI, MEMMDR, SBMDR, TARİH. Ana kitapta dış çalışma kitabı bağlantısı tespit edilmedi. Sayfaların tamamı görünür. Veri girişinde ikinci görev yeri ve branş sütunları gizli; bunlar veri modelinde kaybedilmemeli. Bordrodaki sigorta numarası sütunu da gizli.

Hesap akışı:

Personel + dönem parametreleri + aylık saatler → bordro hesap sonucu → banka listesi / e-bildirge hazırlığı / personel bildirimi / kişisel bordro.

Halkbank dosyası mevcut durumda bu akışa otomatik bağlı değil. İki sayfası var: HalkbankDTO ve Açıklama. BIFF formülleri okunarak E8'in `SUM(C13:C65536)`, E9'un `COUNTA(D13:D65536)` olduğu doğrulandı. Personel satırları 13. satırdan başlıyor; kurum/ödeme üst bilgileri ilk bölümde tutuluyor.

## 4. Hesap kuralları: kaynak dosyanın davranışı

Aşağıdaki oran ve tutarlar Excel'de bulunan değerlerdir; güncel mevzuata uygunluk onayı değildir. Canlı ödeme öncesinde dönem bazında doğrulanmalıdır.

`R2(x)` iki ondalığa yuvarlamayı ifade eder. C# tarafında `decimal` ve açık yuvarlama politikası kullanılmalı; varsayılan banka yuvarlamasına bırakılmamalıdır. Mevcut 10 örnekte HALF_UP ile uyum doğrulandı; sınır örnekleri ayrıca test edilmelidir.

| Sonuç | Kaynak hücre / kural |
|---|---|
| Aylık katsayı | VERİ GİRİŞİ!D43 = 1,575512 |
| Gösterge | VERİ GİRİŞİ!D44 = 140; ancak hesap formüllerinde ayrıca sabit 140 var |
| Gündüz saat ücreti | Bordro!M7 = KATSAYI × 140 = 220,57168 |
| DYK saat ücreti | Bordro!L7 = KATSAYI × 140 × 2 = 441,14336 |
| Gündüz brüt | J7 = R2(gündüz saati × gündüz saat ücreti) |
| DYK brüt | I7 = DYK saati × DYK saat ücreti; burada ayrı iki hane yuvarlama yok |
| Toplam saat | H7 = F7 + G7 |
| Prim günü | K7 = yukarı yuvarla(toplam saat / 7,5) |
| SGK'ya esas ücret / brüt | N7 = R2(I7 + J7) |
| İşveren katkısı | O7 ve X7 = R2(N7 × %21,75) |
| Toplam tahakkuk | P7 = R2(N7 + O7) |
| Çalışan SGK | W7 = R2(N7 × %14) |
| Gelir vergisi matrahı | Q7 = R2(P7 − O7 − W7) |
| GV istisna matrahı | M3 = 28.075,50; S7 buraya bağlı |
| Gelir vergisi | T7 = R2(max(Q7 − S7, 0) × %15) |
| DV istisna tabanı | M2 = 33.030,00; U7 buraya bağlı |
| Damga vergisi | V7 = R2(max(N7 − U7, 0) × 0,00759) |
| İcra | Y7 ← VERİ GİRİŞİ!S4 |
| Kesinti toplamı | Z7 = R2(T7 + V7 + W7 + X7 + Y7) |
| Net ödeme | AA7 = P7 − Z7 |

İşveren payı tahakkuka eklenip kesinti toplamında çıkarılıyor. Bu nedenle çalışan neti ayrıca `brüt − çalışan SGK − GV − DV − icra` ile kontrol edilebilir. Ana ekranda çalışan kesintileri ve işveren maliyeti ayrı gösterilmelidir; kaynak bordronun kesinti toplamı aynı adla çalışanın tamamından kesilmiş gibi sunulmamalıdır.

## 5. Yeniden hesaplama sonucu

Kaynakta dolu 10 kişi var. Her kişinin saat, prim günü, brüt, işveren katkısı, tahakkuk, matrah, vergi, SGK, icra, kesinti ve net alanları bağımsız Decimal hesabıyla yeniden hesaplandı. Excel'in kayıtlı sonuçları ile iki ondalık düzeyinde fark bulunmadı. Bordronun 7–35. satırları arasında satır formülü kopyalama sapması bulunmadı.

| Kontrol | Sonuç |
|---|---:|
| Personel | 10 |
| Gündüz saati | 972 |
| DYK saati | 0 |
| Prim günü toplamı | 134 |
| Brüt ücret | 214.395,70 TL |
| İşveren katkısı | 46.631,07 TL |
| Genel tahakkuk | 261.026,77 TL |
| Çalışan SGK | 30.015,36 TL |
| GV / DV / icra | 0,00 TL |
| Bordrodaki kesinti toplamı | 76.646,43 TL |
| Net ödeme | 184.380,34 TL |
| Halkbank dosyası toplamı | 221.939,30 TL |
| Banka dosyası − bordro neti | **37.558,96 TL** |

Bu doğrulama Excel'i açıp yeniden hesaplatma değildir; dosyanın formülleri bağımsız hesaplanıp kayıtlı hücre sonuçlarıyla karşılaştırılmıştır. DYK, vergi istisnasını aşma ve icra örneği mevcut dolu kayıtlarda yoktur; bu dallar için eşleşme iddiası yoktur. Banka dosyası farkının nedeni mevcut dosyalardan kesinleştirilemez; farklı sürüm veya manuel aktarım olasılığı araştırılmalıdır.

## 6. Dönüşümden önce çözülecek sorunlar

1. **Banka mutabakatı:** Aynı ay açıklamasına rağmen toplamlar farklı. Yeni banka çıktısı kesinleşmiş bordro sonucundan üretilecek; personel sayısı, kişi bazında tutar ve toplam net birebir eşleşmeden dışa aktarım tamamlanmayacak.
2. **Kırık formüller:** E-Bildirge Verileri!J27, J28 ve Personel Bildirimi!F7 doğrudan `#REF!`. Veri girişindeki T18:T32 doğrulama kuralı da kırık başvuru içeriyor. Bu alanların neyi göstermesi gerektiği belirlenmeli.
3. **Kapasite uyuşmazlığı:** Notta 50 kişi yazıyor; veri girişi 4–32 ve bordro 7–35 aralığıyla 29 satır. E-bildirge aktarımı B3:B22 ile 20 kişi, J23 toplamı J3:J22. Yeni uygulamada kişi sayısı sabit satır aralığına bağlı olmayacak.
4. **Dönem tutarsızlıkları:** Aktif dönem Eylül 2026; e-bildirge M2/M3 tarihleri Eylül 2025, F40 açıklaması Kasım 2024, personel bildirimi bütçe yılı 2025. Dönem tek merkezden beslenecek.
5. **Eksik gün sabitleri:** E-bildirge H sütununda 17 veya 20 eksi prim günü kullanılıyor. Kişinin işe giriş/çıkışı ve dönem takvimiyle ilişkisi açıkça tanımlanmalı. Negatif eksik gün, ay üstü prim günü ve kısmi ay senaryoları test edilmeli; kaynakta olmayan mevzuat sınırı varsayılmamalı.
6. **Vergi modeli sınırlı:** Ana bordro %15 sabit oran kullanıyor; süre gelen matrah sütunu hesaplamaya bağlı değil. Dönemler arası matrah, diğer kurum ödemeleri ve istisna hakkının hangi kurumda kullanıldığı ayrıca tanımlanmalı. Kaynaktaki eski AGİ düzeltme notu güncel bir hesap kuralı kabul edilmemeli.
7. **Kişisel bordro farkı:** L10 istisna eşiği aşılınca matrahın tamamına %15; O10 ise brütün tamamına 0,00759 uyguluyor. Ana bordro eşiği aşan kısma uyguluyor. Kişisel bordro ana sonuçtan üretilecek, yeniden vergi hesaplamayacak. Q9 başlığında %20,5 yazarken ana hesap %21,75 kullanıyor.
8. **Gösterge bağlantısı eksik:** D44'ü değiştirmek gösterge etiketini değiştiriyor; L/M formüllerindeki sabit 140 nedeniyle saat ücretini değiştirmiyor. Tek yetkili dönem parametresi kullanılmalı.
9. **Kimlik verisi hassasiyeti:** Sigorta numaraları sayısal ve bilimsel gösterimde tutulmuş; 15 basamağı aşan sayılarda hassasiyet kaybı riski var. Orijinal basamaklar dosyadan geri getirilebilir varsayılmamalı. TCKN, SGK sicili, meslek kodu ve IBAN metin olarak tutulmalı.
10. **IBAN kontrolü yetersiz:** O sütunu sadece 26 karaktere bakıyor. Başlıktaki 'IBAN girilmezse hesap yapılmaz' ifadesi bordro formüllerinde uygulanmıyor. Hesaplanmış taslak ile bankaya aktarılabilir kayıt ayrı durumlar olmalı; banka çıktısı için gerçek biçim/sağlama kontrolü yapılmalı.
11. **İsimle eşleştirme:** Kişisel bordro VLOOKUP ile ad-soyaddan kişi seçiyor. Aynı adlar için iç PersonId kullanılmalı.
12. **Yazdırma alanı:** Veri girişinin yazdırma alanı B21:O26 gibi dar bir bölüm. Rapor sayfalaması sabit Excel alanlarından bağımsız tasarlanmalı. TODAY() kullanan imza tarihleri kesinleşen belgede dondurulmalı.

## 7. Arayüz referansından alınabilecekler

OEHS, C# / .NET 8 / WPF projesi. Görsel dil doğrudan App.xaml'de tanımlı:

| Öğe | Kaynak değer |
|---|---|
| Ana lacivert | #031C38 |
| Koyu lacivert | #00152D |
| Birincil mavi | #2468D8 |
| Altın vurgu | #FFB41F |
| Açık yüzey | #F7F9FC |
| Kart geçişi | #082744 → #0A3154 → #0B365C |
| Yazı tipi | Segoe UI |
| Kart köşesi | 10–12 px |
| Düğme köşesi | 7 px |
| Sol menü | Kaynakta 215 px; içerik esnek sütun |

Card, CardCompact, PremiumPanel, ModernButton ve NavButton stilleri bağımsız ResourceDictionary'ye çıkarılarak değerlendirilebilir. Üst başlık, sol menü, özet kartları ve belge önizleme yaklaşımı korunabilir. İhale, hat ve yüklenici ekranları ücretli öğretmen programına taşınmayacak.

Kaynakta veri/yedek, belge önizleme ve çıktı servisleri var; bunlar yaklaşım referansıdır. Mevcut ExcelDocumentExporter genel XLSX tablo üretir; Halkbank'ın eski XLS şablonunu koruyan dışa aktarıcı yerine kullanılamaz. Şablon biçimini koruma, yazdırma ve PDF kodunun uygunluğu ayrıca doğrulanmalıdır.

Yeni uygulamada önerilen ekranlar:

- Ana ekran: seçili ay/yıl, personel sayısı, toplam saat, brüt, çalışan kesintileri, işveren maliyeti, net ve kontrol uyarıları.
- Personel: kimlik, banka, kurum/branş, görev tarihleri ve birden fazla görevlendirme.
- Aylık puantaj: gündüz/DYK saatleri, icra, giriş/çıkış ve veri kontrolü. İlk sürüm kaynak Excel gibi aylık toplam saatle çalışır; günlük ders takvimi ayrı genişlemedir.
- Bordro: hesapla, kişi bazında açıklama, kontrol, kesinleştir ve düzeltme sürümü.
- Belgeler: toplu bordro, banka listesi, personel bildirimi ve kişisel bordro.
- Banka aktarımı: kurum hesabı, ödeme tarihi/türü, şablon üretimi ve mutabakat.
- E-bildirge hazırlığı: prim günü, eksik gün, meslek kodu ve prim tutarı. Resmî sisteme otomatik gönderim ilk kapsamda önerilmiyor.
- Geçmiş ve ayarlar: dönem parametreleri, imzacılar, yedek/geri yükleme.

## 8. Ortak çekirdek ve daha sonra eklenecek usta öğretici modülü

Önerilen katmanlar:

1. **Ortak çekirdek:** kişi kimliği, kurum, dönem kimliği, belge üst bilgileri, para/yuvarlama araçları, kayıt sürümü ve denetim izi.
2. **Temel eğitim modülü:** öğretmen görevlendirmesi, gündüz/DYK girdileri, bordro hesap kuralları ve belge alan eşlemeleri.
3. **Uygulama servisleri:** içe aktarma önizlemesi, doğrulama, hesaplama, kesinleştirme, düzeltme ve çıktı iş akışları.
4. **Altyapı:** sürümlü JSON dosyaları, Excel/XLS/PDF adaptörleri ve yedekleme.
5. **WPF arayüz:** MVVM ekranları ve OEHS'den alınacak görsel kaynaklar.

Örnek modül sözleşmesi: modül kimliği ve sürümü; kendine ait girdi doğrulama; dönem parametresi şeması; hesaplama; sonuç dökümü; belge sağlayıcısı. Modüle özgü saat/gün/vergi kuralları ortak çekirdeğe sabitlenmemeli. Usta öğretici Excel'i geldiğinde kendi analizinden sonra ayrı uygulama olarak bu sözleşmeye bağlanır; şu an kuralları tahmin edilmez.

Önerilen veri modeli:

- Person: uygulama içi kimlik, TCKN, ad-soyad ve banka bilgileri.
- Assignment: kişi, kurum, branş, modül ve geçerlilik tarihleri.
- PayrollPeriod: yıl/ay, kurum, modül, taslak/kesin/düzeltme durumu.
- ParameterSet: geçerli tarih aralığı, katsayı/gösterge/oran/istisnalar, kaynak notu ve kural sürümü.
- TeacherMonthlyInput: gündüz/DYK saatleri, icra ve dönemsel hareket bilgileri.
- PayrollResult: kişi bazında tüm ara hesaplar, parametre kopyası ve sürüm.
- DocumentRecord / ExportRecord: üretilen belge, dönem/sürüm bağlantısı ve mutabakat özeti.
- AuditLog: değişiklik zamanı, önceki/yeni sürüm ilişkisi.

Kesinleşmiş kayıtlar o tarihteki parametre ve girdilerin kopyasını saklamalı; sonraki katsayı değişiklikleri eski bordroları değiştirmemeli. Kişinin kurum/banka değişiklikleri de eski belgelerin kimlik ve hesap bilgilerini değiştirmemeli. Düzeltme eski sonucu silmez; yeni sürüm üretir. Aynı kişi/kurum/modül/dönem için yinelenen kesin kayıt engellenir.

## 9. Teknik seçenekler

| Seçenek | Kazanç | Sınır |
|---|---|---|
| **Bağımsız C# / WPF + JSON** | Mevcut arayüz becerisi ve kaynaklarıyla uyum; Excel kurulumu olmadan çalışma; modül eklemeye elverişli | Belge ve banka adaptörleri yazılmalı |
| Mevcut OEHS içine yeni modül | Ortak uygulama kabuğu ve bazı servislerin paylaşımı | İhale/taşıma modeli ile personel bordrosunu aynı üründe yönetme karmaşıklığı |
| Excel'i arka planda çalıştıran masaüstü kabuk | İlk hesap benzerliği daha hızlı kurulabilir | Excel sürümü/kurulumu, kırık başvurular ve dosya bazlı veri tutarlılığına bağımlılık |

Öneri birinci seçenek; OEHS'nin görsel kaynakları paylaşılır, yeni hesap motoru bağımsız kurulur. Bu bir teknik öneridir, kullanıcının seçtiği teknoloji olarak kayda geçirilmemiştir.

Yeni proje için .NET 10 LTS değerlendirilmelidir. Referansın .NET 8 hedefi aynen alınmamalı; Microsoft'un 23 Ekim 2026 tarihinde kontrol edilen politikasında .NET 8 desteği Kasım 2026'da, .NET 10 desteği Kasım 2028'de bitiyor. Kaynak: https://dotnet.microsoft.com/en-us/platform/support/policy . Kütüphane ve hedef Windows uyumu uygulama başlangıcında doğrulanmalı.

## 10. Aşamalı yol haritası ve kabul koşulları

### Aşama 1 — Hesap şartnamesi ve veri sözlüğü

Her girdi, ara hesap, çıktı ve hata için hücre eşlemesi hazırlanır. Bu incelemedeki sorunlar karar listesine çevrilir. Mevcut Excel davranışı ile düzeltilmiş üretim kuralı ayrı kaydedilir. Oranlar/istisnalar ve prim günü/eksik gün kuralı yetkili dönem kaynaklarıyla doğrulanır.

Kabul: Bordro matematiği ve açık kararlar yazılı; banka farkının kaynağı açıklanmış veya hangi dosyanın esas alınacağı belirlenmiş; kırık alanların anlamı netleşmiş.

### Aşama 2 — Hesaplama motoru ve anlamlı testler

Önce kaynak davranışıyla karşılaştırma; sonra onaylanmış kural düzeltmeleri. Decimal hassasiyeti, ara yuvarlamalar ve dönem parametreleri açıkça tanımlanır.

Kabul: Mevcut 10 kişi tüm ara alanlarda kuruş düzeyinde eşleşir. DYK, karma saat, 0 saat, 7,5 saat sınırı, vergi istisnası altı/eşit/üstü, icra, kısmi ay ve yuvarlama sınırları ayrı örneklerle sınanır. Değişen kuralın Excel'den neden farklı sonuç verdiği gösterilir.

### Aşama 3 — Dosya tabanlı saklama ve Excel'den aktarım

Personel ve dönem aktarımı için önizleme; hatalı kimlik, yinelenen kişi, hassasiyeti kaybolmuş sicil ve geçersiz IBAN uyarıları. Veri kaydı işlem bütünlüğü içinde yapılır. Kimlik bilgileri test verisinde kullanılmaz.

Kabul: İçe aktarım raporu satır bazında okunur; hata sessizce sıfıra çevrilmez; eski sonuç parametre değişikliğinden etkilenmez; yedekten geri yükleme doğrulanır.

### Aşama 4 — OEHS görsel diliyle kullanıcı akışı

Bağımsız tema kaynakları, ana ekran, personel, aylık puantaj ve bordro kontrol ekranları hazırlanır. İş akışı: dönem aç → personel/saatleri getir → eksikleri düzelt → hesapla → sonuçları kontrol et → kesinleştir → belge/banka dosyası üret.

Kabul: Örnek dönem bu akışla tamamlanır. Uzun ad/kurum, klavye ile giriş, 1366×768 ve %125/%150 ölçeklerde taşma kontrolü yapılır. İşlem uyarıları hangi satırın nasıl düzeltileceğini söyler.

### Aşama 5 — Belgeler ve Halkbank adaptörü

Toplu bordro, banka listesi, personel bildirimi ve kişisel bordro aynı sonuç kayıtlarından üretilir. E-bildirge veri hazırlığı tamamlanır. Banka dosyası için XLS biçimini/şablonunu koruyan adaptör seçilir; basit dosya uzantısı değişikliği yapılmaz. Kurum hesabı, tarih ve tür üst bilgileri program ayarlarından gelir.

Kabul: Ana bordro ve kişisel bordro vergi/neti eşleşir; banka kişi tutarları ve toplam neti aynı sürüme bağlıdır; 29'dan fazla kişide tüm raporlar eksiksizdir. PDF/yazdırma sayfa kırılması ve Türkçe karakterler gözle kontrol edilir. Banka portalı kabulü yetkili kullanıcı tarafından doğrulanmadan 'bankaya uyumlu' tamamlandı sayılmaz.

### Aşama 6 — Pilot dönem ve Windows dağıtımı

Excel ile paralel bir dönem yürütülür; taslak, kesinleştirme ve düzeltme senaryoları denenir. Excel gerektirmeyen kurulum/taşınabilir paket seçilir; sürüm ve veri dosyası şeması yükseltmesi planlanır. Paylaşılacak pakette gerçek personel kaydı bulunmaz.

Kabul: Uygulama yeniden açıldığında veriler ve kesin sonuçlar korunur; yedek başka boş ortamda geri yüklenir; kullanıcı bordro ve banka mutabakatını tamamlar; kurulum temiz Windows ortamında doğrulanır.

### Daha sonra — Usta öğretici modülü

Yeni Excel ayrıca incelenir; ortak kişi/dönem/çıktı altyapısı kullanılırken kendi hesaplama ve doğrulama kuralları eklenir. Temel eğitim modülünün karşılaştırma testleri tekrar çalıştırılarak mevcut sonuçların değişmediği doğrulanır.

## 11. İlk sürüm sınırları ve açık kararlar

- İlk sürümde temel eğitim ücretli öğretmen, aylık toplam saat, gündüz/DYK, kesintiler, belgeler, banka dosyası, dönem geçmişi ve yedekleme öneriliyor.
- Usta öğretici hesapları sonraki aşamada; modül sınırı şimdi hazırlanacak.
- Günlük ders çizelgesi, otomatik banka/SGK gönderimi, bulut ve eşzamanlı çok kullanıcılık ilk sürüm önerisine dahil değil.
- Geliştirme öncesi kesinleştirilecekler: tek kurum/çok kurum, tek bilgisayar/eşzamanlı kullanıcı, geçerli dönem parametrelerinin kaynağı, kümülatif matrah/istisna kullanımı ve kesin banka şablon gereksinimi.
- Teknik mimari ve ekran tasarımı bu rapordan sonra kararlaştırılabilir. Süre tahmini, bu kararlar ve resmî çıktı beklentileri netleşince aşama bazında verilmelidir.

## 12. İnceleme kanıtları

Yerel `inceleme/formul-envanteri.json` formülleri, hata alanlarını, gizli sütunları ve doğrulamaları; `inceleme/hesap-dogrulama.json` yeniden hesaplama sonuçlarını içerir. Rapor kişisel kayıtları tekrar yayımlamaz. Kaynaklar yerel inceleme klasöründedir; GitHub'a veya başka bir dış hizmete yüklenmedi.


## 13. Son kullanıcı kararları

Windows üzerinde kurulum ve Excel gerektirmeyen taşınabilir paket hedeflenir. SQL kullanılmaz. Günlük kayıtlar sürümlü JSON; yedek ve taşıma tek paket dosyası olarak önerilir. Ayrıntı: yeni bağımsız projenin docs/TASARIM_KARARLARI.md belgesi.

Yazdırma grubu yalnızca Bordro (Çıktı-1), Banka Listesi (Çıktı-2), Personel Bildirimi (Çıktı-3). Kişisel bordro talep üzerine; e-bildirge/veri girişi ve diğer bilgi Excel dökümleri isteğe bağlıdır. Halkbank dosyası ayrı banka yükleme çıktısıdır.

