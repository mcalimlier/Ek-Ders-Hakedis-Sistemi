# Halk Eğitim Merkezi Emekli Usta Öğretici

Bu modül normal Halk Eğitim usta öğretici modülünden ayrı bir hesaplama çekirdeğidir. Hakedis.EmekliHalkEgitim.Core yalnız Contracts'a başvurur; diğer çekirdekleri çağırmaz. Mevcut enum değerleri değiştirilmeden yeni modül sona eklenmiştir.

## Dönem ve hesap

Kullanıcı başlıktaki **Eylül 2026** döneminin geçerli olduğunu teyit etti. Kaynağın VERİ GİRİŞİ!E38 hücresinde kalan Ağustos bilgisi dönem belirlemek için kullanılmaz. Anonim fixture bu hücreyi izlenebilirlik için SourcePeriodCell=2026-08, geçerli dönemi Period=2026-09 olarak ayrı tutar. Parasal kural sürümü `emekli-halk-egitim.excel-2026-09.v1`'dir. Kullanıcı programdan başka ay/yıl için hesap yapabilir.

Kaynak Bordro!I7:AA36 mali formülleri izlenir. İştirakçi oranı %7,5; işveren %24,75'tir. X sütunu O sütunundaki işveren primini aynen taşır. Gündüz brütü önce iki ondalığa yuvarlanır; DYK çarpımı ayrıca yuvarlanmadan toplam brüte katılır. Brüt, primler, tahakkuk, vergi matrahı, vergiler ve kesinti toplamı Excel ile aynı sırada iki ondalığa yuvarlanır. İcra girdisi korunur; net = tahakkuk − toplam kesinti. Kaynak örneğinde 12 saat için brüt 2.646,86 TL, iştirakçi 198,51 TL, işveren 655,10 TL, net 2.448,35 TL'dir.

Bu entegrasyon yeni mevzuat veya kümülatif vergi kuralı tanımlamaz. SGK yan bilgi hesabı kullanıcının daha önce tarif ettiği toplam saat/7,5 yukarı yuvarlama ve normal giriş/çıkış tarihleri dahil takvim aralığı kuralıdır. Bağımsız gün sürümü `emekli-halk-egitim.sgk-yan-bilgi.v1`'dir. Gün hesabı parasal sonucu değiştirmez; hatalı gün aralığı XML'i engeller.

## Ayrı dönem sabitleri

Dönem Sabitleri panelinin altına **Emekli Usta Öğretici dönem sabitleri** kartı eklenmiştir. Dokuz parametrenin tamamı bu karta aittir; `EmekliUstaSabitleri.json` içinde ayrı saklanır. İlk değerler emekli kaynak Excel'inden gelir. Normal personelin `OrtakSabitler.json` dosyası değişmez.

Kart önce gri ve salt okunurdur. Güncelle alanları açar; Dönem sabitlerini değiştir kaydeder ve tekrar kilitler. İlgili modüldeki aktif ve sonraki hesaplar bu kartı kullanır; normal personelin sabit güncellemesi emekli oranlarına karışmaz. Kayıtlı hakedişler kendi dondurulmuş sabitlerini korur. Arşiv düzenlemesi de kayıtlı oranlarla açılır. Emekli sabitlerinde tamamlanmamış düzenleme varsa kapanış ve arşiv düzenleme korumaları uygulanır.

## PDF ve SGK

Ekran ve PDF başlığı kaynakta olduğu gibi **DERS ÜCRETİ KARŞILIĞINDA GÖREVLENDİRİLENLER BORDROSU ( EMEKLİ USTA ÖĞRETİCİ)** olur. Ünvan Emekli Usta Öğretici'dir. PDF prim başlıkları kullanılan sabitlere göre %7,5 ve %24,75 gösterir; eski %14/%21,75 başlıkları kullanılmaz. Üç ana PDF, kişisel PDF ve yazdırma mevcut akıştan alınır.

Kullanıcının teyit ettiği SGK belge türü **02**'dir. `SgkXml-EmekliUstaOgreticiler.json` ayrı profildir. Bu dosya yoksa normal usta öğretici profilinin ortak işyeri bilgileri başlangıç olarak alınır; belge türü 02 olur ve kişi ad/soyad tercihleri kopyalanmaz. Profil kaydedilince normal usta veya öğretmen profilleri değişmez. Gerçek sicil/unvan/adres bilgileri kaynak koduna ya da Git'e yazılmaz. Son XML kontrol/onayı ve özgün XSD doğrulaması devam eder.

## Denetim

Yerel test verilerine kaynaktaki tek kişi Eylül 2026 için 12 saatle eklenmiştir. Kaynak IBAN doğrulamadan geçmedi; kullanıcı şimdilik boş bırakılmasını istedi. Ödeme oluşturmadan önce doğru IBAN girilmelidir. Gerçek kişi ve özel içe aktarma dosyaları Git'e veya dağıtıma alınmaz.

`scripts/Verify-HalkExcel.ps1 -BaselineHours 12 -ReferencePeriod 2026-09` ile kaynak ayrı gizli Excel örneğinde salt okunur yeniden hesaplanır; hiçbir zaman kaydedilmez. 50 senaryo mali sonuçların tamamıyla karşılaştırılır. Kaynağın SHA-256 değeri korunur. Anonim fixture gerçek kişi bilgisi içermez. Birim ve WPF testleri modül/sabit/profil ayrımı, eski arşiv, PDF/XML ve normal modüllerin regresyonlarını kapsar.
