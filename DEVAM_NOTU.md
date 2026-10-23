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
Windows x64 paket: dist/Hakedis-win-x64-20261024-003745.zip. ZIP yalnız Hakedis.App.exe ve KULLANIM.txt içeriyor; gerçek kullanıcı verisi yok. Yayımlanan bağımsız EXE açıldı ve mesaj döngüsünün hazır olduğu doğrulandı; ardından kapatıldı.

## Sonraki ürün işleri

Üç resmî basılı belge, isteğe bağlı Excel/PDF ve banka aktarımı. Diğer eğitim türlerinin bağımsız çekirdekleri kendi kaynakları geldikten sonra eklenecek; şu anda yalnız Temel Eğitim hesap yapar. Diğer modüllere kayıt yapılabilse de Temel Eğitim formülleri uygulanmaz.

## Bu eklemenin doğrulaması

Yeni SGK kuralı için önce eski uygulamada başarısız olan 9 uçtan uca test yazıldı; kural uygulandıktan sonra takvim sınırları da eklendi. Son tam koşuda 81 test geçti; eski 68 teste 13 SGK senaryosu eklendi. WPF ekranında Eksik Gün Sayısı 117 saat/Eylül için 14, hücre 108 saate değişince 15 olarak doğrulandı; binding hatası yok. Görseller artifacts/ui-review-sgk içinde. Yeni paket: dist/Hakedis-win-x64-20261024-003745.zip. ZIP içeriği yalnız EXE ve kullanım notu; bağımsız EXE açılışı doğrulandı.
