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

## Önemli bekleyen — kullanıcı açıklayacak

Kullanıcı Eksik Gün Sayısı hesabını ayrıca detaylı açıklayacağını söyledi. Kaynak E-Bildirge ilk satırlarda 17, son satırlarda 20 sabiti kullanıyor; tarihleri 2025'te kalmış. Hiçbiri doğru kural sayılmadı. Ekran ve hakediş görüntüsü bu alanı Açıklama bekleniyor diye gösteriyor. TeacherCalendar yalnız testli tarih aralığı yardımcısı; üretimde SGK kuralı olarak etkin değil.

## Saklama

SQL yok. EXE yanındaki Veriler/ogretmenler.json modül sahipliğini koruyan katalogdur. Aynı TCKN farklı modülde tekrar oluşturulamaz.
Veriler/AylikKayitlar/Modül/YYYY-MM.json aylık girdileri ve dönem sabitlerini, hakediş-...json ise oluşturulan hesap görüntüsünü saklar. Atomik yazma, flush, işlem kilidi, sağlam önceki kaydın .bak yedeği var. Bozuk dosyanın üstüne yazılmaz.
Hakediş oluştur boş IBAN veya geçersiz girdiyle çalışmaz; henüz girilmemiş öğretmenler kayda dahil edilmez. Oluşturulan JSON, basılı resmî belge veya mali kesinleştirme anlamına gelmez.

## Doğrulama / paket

68 birim testi geçti; Excel'in 50 senaryo/14 alan doğrulaması dahil. WPF gerçek ekranında dört panel normal/dar boyutta render edildi; meslek popup'ı, canlı hücre hesabı ve ay/yıl ComboBox üzerinden Kasım-2027 seçimi, Hakediş oluştur düğmesinin kalıcı JSON yazması kontrol edildi. Binding hatası yok. Görseller artifacts/ui-review-hybrid-verified içinde sentetik personel kullanır.
Windows x64 paket: dist/Hakedis-win-x64-20261024-002042.zip. ZIP yalnız Hakedis.App.exe ve KULLANIM.txt içeriyor; gerçek kullanıcı verisi yok. Yayımlanan bağımsız EXE açıldı ve mesaj döngüsünün hazır olduğu doğrulandı; ardından kapatıldı.

## Sonraki ürün işleri

Kullanıcının eksik gün açıklaması; üç resmî basılı belge, isteğe bağlı Excel/PDF ve banka aktarımı. Diğer eğitim türlerinin bağımsız çekirdekleri kendi kaynakları geldikten sonra eklenecek; şu anda yalnız Temel Eğitim hesap yapar. Diğer modüllere kayıt yapılabilse de Temel Eğitim formülleri uygulanmaz.
