# Excel ve hibrit hakediş denetimi — 24 Ekim 2026

## Doğrulama yöntemi

Kaynak: `01-Eylül 2026 - Temel Eğitim Ücretli Ek Ders Hakediş.xlsx`. Kaynak dosya değiştirilmedi. `scripts/Verify-Excel.ps1`, ayrı ve görünmeyen bir Microsoft Excel örneğinde dosyayı salt okunur açar, `CalculateFullRebuild` ile 50 senaryoyu hesaplatır ve kaydetmeden kapatır. Karşılaştırma verileri `tests/Hakedis.Tests/Fixtures/excel-recalculated.json` içindedir; personel kimliği/adı içermez. Programın son kullanıcı bilgisayarında Excel gereksinimi yoktur; Excel yalnız bu geliştirme denetiminde kullanıldı.

`ExcelRecalculationTests` her senaryoda 14 sonucu karşılaştırır: gündüz ve kurs brütleri, prim gün sayısı, iki saat ücreti, SGK'ya esas ücret, iki prim, tahakkuk, gelir vergisi matrahı, gelir vergisi, damga vergisi, toplam kesinti ve net. Parasal sonuçlar kuruş düzeyinde aynı; yuvarlanmayan ara sonuçların kayan nokta gösterim farkları için tolerans 0,00000001'dir.

Sıfır saat, 7,5/7,51 saat sınırı, yalnız kurs, karışık saatler, vergi istisnasını aşan tutarlar, küsuratlı icra ve sabit tohumla üretilmiş 34 ek senaryo dahildir. Kaynakta yer alan 10 öğretmenin toplamları da doğrulandı:

| Alan | Kaynak Excel / C# |
|---|---:|
| SGK'ya esas ücret | 214.395,70 |
| İşveren primi | 46.631,07 |
| Tahakkuk | 261.026,77 |
| İştirakçi primi | 30.015,36 |
| Net | 184.380,34 |
| Çalışma/prim günü toplamı | 134 |

## Bulunan ve düzeltilen fark

Excel'de Bordro Y sütunu manuel icra tutarını yuvarlamadan alır; Z sütununda toplam yuvarlanır. C# daha önce icrayı önce yuvarlıyordu. Bu ara yuvarlama kaldırıldı. Testte örneğin `1,005` tutarı Y karşılığında aynen korunur; nihai toplam Excel'in yuvarlama sırasını izler. İncelenen kaynak veride genel brüt/net formül uyuşmazlığı bulunmadı. Kaynak Eylül-2026 dışındaki bir ayın gerçek katsayı/istisna değerleri bu doğrulamadan çıkarılamaz; ilgili dönemin sabitleri ayrıca girilmelidir.

Bilinçli parametre farkı: Kaynak L/M saat ücreti formüllerinde gösterge `140` olarak sabittir; D44 değiştirilse bile sadece başlık değişir. Uygulamada ÜCRETLİ ÖĞRETMEN GÖSTERGE alanı hesabı gerçekten değiştirir. Kaynak `140` değeriyle sonuçlar eşleşir; farklı gösterge veya değiştirilmiş SGK/vergi oranlarının aynı kaynak Excel'in sabit formülleriyle eşleştiği iddia edilmez.

## SGK yan bilgisi — 24 Ekim kullanıcı açıklamasıyla uygulanan kural

Eksik gün sayısı yalnız SGK sitesine girişte kullanılacak yan bilgidir; hakedişin brüt, kesinti ve net hesaplarına girdi değildir.

- Çalışma Gün Sayısı = yukarı tam sayıya yuvarla((GÜNDÜZ DERS SAATİ + DESTEKLEME VE YETİŞTİRME KURSU) / 7,5).
- Ay içinde giriş/çıkış yoksa azami gün, seçili ayın gerçek takvim uzunluğudur: 28, 29, 30 veya 31.
- Ay içi giriş varsa ilk gün giriş günüdür. Ay içi çıkış varsa son gün çıkış günüdür. Her iki sınır dahildir.
- Azami gün = ayla çakışan çalışma aralığının bitişi − başlangıcı + 1.
- Eksik Gün Sayısı = azami gün − Çalışma Gün Sayısı.
- Aylık giriş/çıkış alanları doluysa o dönemin açık bilgisi olarak kullanılır; boşsa kayıtlı öğretmenin başlangıç/ayrılış tarihine başvurulur. Önceki yıldaki işe giriş tam ayın gün sayısını azaltmaz.

Örnek: Ekim-2026, 05.10.2026 giriş, çıkış yok, 117 saat: azami gün `31 − 5 + 1 = 27`; çalışma günü `yukarı(117 / 7,5) = 16`; eksik gün `27 − 16 = 11`.

Çalışma günü azami günü aşarsa negatif eksik gün gösterilmez: SGK sütunu `—` olur ve ayrı SGK bilgi kontrolünde neden belirtilir. Bu yan bilgi sorunu geçerli parasal hakedişi iptal etmez; oluşturulan görüntüde uyarı saklanır. Geçersiz tarih girişleri ise kaydetmeden önce düzeltilmelidir.

Kaynak E-Bildirge'deki 17/20 sabitleri ve 2025'te kalmış tarihler kullanılmıyor. `TeacherCalendar.CalculateMonth` seçili yıl/aydan hesabı üretir; `TeacherPayroll` para hesabından bağımsızdır. Hakediş görüntüsünde her kişi için CalendarDays, PremiumDays, MissingDays ve varsa SgkError saklanır; yan bilgi kural sürümü `temel-egitim.sgk-yan-bilgi.v1` olarak işaretlenir. Eski hakediş görüntüleri geriye dönük değiştirilmez.

Kaynak dosyada ayrıca `E-Bildirge!J27/J28`, `Personel Bildirimi!F7` kırık referansları ve kişisel bordroda ana bordrodan farklı vergi formülleri var. Bunlar doğru kural diye taşınmadı. Eski bağımsız banka XLS dosyasının toplamının bordroyla farklı olması da bu hesap doğrulamasını değiştirmez; bu banka dosyasının dönem/veri eşleştirmesi ayrı konudur.

## Başlıklar ve meslek kataloğu

`ExcelHeadings.cs` kaynak hücrelerden çıkarıldı; `GV İstinası:` yazımı dahil kaynak metinleri ve satır sonları korunur. Yeni işlev adları (modül seçimi, kontrol gibi) ayrıca tanımlanır. Ücretli öğretmenin ikinci görev yeri/branşı ve ay içi giriş/çıkış tarihleri de veri modelinde bulunur.

PDF: `-MeslekAdıveKodralri.pdf`, 113 sayfa, **7.190 benzersiz kod**, 18 satır devamı. SHA256: `EBE61D9934F89BF706ED3BE4508F2DF248CA84AD5AB1F574F1B86FC467210798`. Gömülü `occupations.json` tümünü içerir; baştaki sıfırlar korunur. Arama bütün sözcükleri kod + meslek adında, Türkçe büyük/küçük harf duyarsız arar. Açılır kutunun en üstünde arama alanı vardır. Ekran kontrolünde `sınıf öğretmen` araması dört sonuç verir.

## Kayıt ve ekran akışı

- Öğretmen Bilgileri, Aylık Ders Bilgileri, öğretmen listesi ve Hakediş Hesapla kendi modül seçimlerine sahiptir. Birinin değiştirilmesi diğer panelin modülünü değiştirmez.
- Aylık giriş ve hakedişte DÖNEM ay adı, YIL ayrı açılır listedir; birleşik gösterim `Eylül-2026`, `Kasım-2027` biçimindedir. Modül/yıl/ay değişince ilgili kayıt yüklenir.
- Dönem sabitleri ayrı menüdedir; hakediş ekranıyla aynı modül/dönemi düzenler. Her dönemin parametreleri kendi dosyasında saklanır.
- Aylık girişte Kaydet kalıcıdır; mevcut kayıt seçildiğinde alanlar kilitli açılır, Düzenle ile açılır. Veri Girişi'nde taslak/hakediş düğmeleri kaldırıldı.
- Hibrit tabloda beyaz hücrelerde saat/kesinti/ay içi tarih düzenlenir; hesap sütunları salt okunurdur. Aynı modül/dönemdeki aylık giriş ve hibrit önizleme canlı eşleşir. Hatalı girdide önceki geçerli toplam korunmaz; kartlar `—` gösterir. Altında 3×2 toplam kartı vardır.
- Hakediş oluştur, geçerli girdileri ve hesap sonuçlarını öğretmen bilgileriyle birlikte ayrı JSON görüntüsüne kaydeder. IBAN boşsa oluşturmaz. Bu işlem henüz basılı resmî belge/PDF/Excel üretimi veya mali kesinleştirme değildir.
- `Veriler/AylikKayitlar/<Modül>/YYYY-MM.json` aylık girdileri ve sabitleri; `hakediş-...json` oluşturulan görüntüyü içerir. Atomik yazma, disk flush, işlem kilidi ve `.bak` yedeği vardır. Diğer kişinin kayıtları güncelleme altında korunur. İki uygulamada aynı kişinin aynı alanı düzenlenirse son kaydetme geçerlidir.
- Diğer modüllerde öğretmen/aylık bilgi saklanabilir; Temel Eğitim hesabı başka modüle uygulanmaz.

## Ekran doğrulaması

`tests/Hakedis.UiSmoke` gerçek WPF penceresini normal ve dar boyutta açar. Dört paneli, 3×2 kartları, meslek popup'ını render eder; arama/seçim geri bağlamasını, düzenlenen hücrede canlı hesaplamayı, ay/yıl ComboBox üzerinden Kasım-2027 seçimini ve bağlama hatalarını kontrol eder. Görseller `artifacts/ui-review-hybrid-verified` içindedir; yalnız sentetik personel içerir.

SGK eklemesi sonrası tam test koşusu: **81 başarılı, 0 başarısız**. Ek 13 senaryo; 28/29/30/31 gün, 5 Ekim giriş, çıkış ve birlikte giriş/çıkış, eski işe giriş, son gün giriş, ilk gün çıkış, sıfır saat, küsurat/DYK, yeniden açma, JSON yan bilgi alanları ve parasal sonuçların değişmemesini kapsar. WPF canlı SGK sütunu kontrolü artifacts/ui-review-sgk içinde doğrulandı.
