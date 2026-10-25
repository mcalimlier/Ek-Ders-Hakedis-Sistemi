# Mesleki Eğitim Merkezi Ücretli Öğretmen

Hakedis.MeslekiEgitim.Core bağımsız mali ve SGK gün çekirdeğidir. Yalnız Contracts'a başvurur; başka hesaplama çekirdeğini çağırmaz. Mevcut MeslekiEgitim kimliği korunarak katalogda **Mesleki Eğitim Merkezi Ücretli Öğretmen** adıyla açılmıştır.

## Eski kaynak ve güncel kullanım

Dosya adında Ocak 2025 yazmasına rağmen VERİ GİRİŞİ!E37:F37 iç dönemi Ocak 2026'dır. Kaynaktaki katsayı 1,387871, gösterge 140'tır. Kaynak sürümü `mesleki-egitim.excel-2026-01.v1` olarak tutulur; bu ad hesaplanacak ayı sınırlamaz.

Eski katsayı programın varsayılanına veya ortak sabit deposuna yazılmaz. Modül, normal ücretli öğretmenlerin **Ortak Dönem Sabitleri** değerlerini kullanır. Hesaplanacak ay/yıl kullanıcıdan alınır. Eski katsayı yalnız kaynağın kendi sonuçlarıyla yapılan anonim karşılaştırma testinde kullanılır. Kullanıcının bu bölümde güncel personel olmadığını belirtmesi nedeniyle eski Excel'den kişi/aylık giriş aktarılmamıştır.

Bordro!I7:AA35 mali satırları aynı matematiği izler: gündüz brütü önce iki ondalığa yuvarlanır; DYK çarpımı ayrıca yuvarlanmadan toplam brüte katılır. Brüt, primler, tahakkuk, vergi matrahı, vergiler ve kesinti toplamı kaynak sırasıyla iki ondalığa yuvarlanır. İcra girdisi aynen korunur; net = tahakkuk − kesinti toplamı. Kaynak prim oranları %14/%21,75, gelir %15, damga %0,759'dur; uygulamada kullanılan değerler ortak sabitlerden gelir. Yeni bir mevzuat veya vergi dilimi kuralı tanımlanmaz.

## Gün bilgileri ve çıktılar

Kaynağın Bildirge Veri Girişi yardımcı takviminde son gün − ilk gün biçimindeki eski hesap bulunur. Kullanıcının mevcut talimatına uygun olarak uygulamada giriş ve çıkış günü dahil edilir: çalışma günü yukarı yuvarla(toplam saat/7,5), eksik gün = seçili ayda çalışılabilecek takvim günü − çalışma günü. Bağımsız sürüm `mesleki-egitim.sgk-yan-bilgi.v1`'dir. Gün hatası parasal tutarı silmez; SGK XML'i engeller.

Öğretmen kaydı, aylık giriş, canlı tablo, arşiv, onaylı düzenleme/silme, üç PDF ve kişisel bordro mevcut modül ayrımını kullanır. Bordro başlığı ve ünvan Ücretli Öğretmen olarak kalır. Ortak ücretli öğretmen SGK işyeri profili, belge 13 / kanun 00000 / eksik gün nedeni 07 kullanılır; emekli profiline yönlendirme yapılmaz. Kişi/ay tek bordro denetimi sürer.

## Doğrulama

`scripts/Verify-HalkExcel.ps1` için `-BaselineHours 51 -PeriodMonthCell E37 -PeriodYearCell F37` seçenekleri kullanılır. Kaynak salt okunur açılıp CalculateFullRebuild ile 50 senaryo hesaplanır ve kaydedilmeden kapatılır. Fixture kaynak katsayısını ve istisnaları ayrı Parameters alanında taşır; gerçek ad/TC/IBAN içermez. C# sonuçları bütün mali alanlarda karşılaştırılır. Güncel ortak katsayıyla 51 saat örneği de ayrıca test edilir.

Kaynak ve gerçek veri dosyaları SHA-256 ile korunur. WPF akışı, tek yatay A4 bordro ve diğer PDF'ler, kişisel bordro, XML/özgün XSD, eski arşiv sabitleri ve mevcut modüllerin testleri birlikte kontrol edilir. Gerçek SGK yüklemesi veya fiziksel baskı yapılmaz.
