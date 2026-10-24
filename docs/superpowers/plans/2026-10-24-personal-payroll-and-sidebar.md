# Kişisel bordro ve sol menü düzeni

**Amaç:** Kullanıcının beş maddesini mevcut C#/WPF tasarımını ve altı kartın ölçülerini koruyarak uygulamak.

**Kararlar:** Kişisel bordro, seçilen modül/yıl/ayın kesinleşmiş hakedişindeki tek kişinin dondurulmuş girdi ve sonuçlarını kullanır. Güncel öğretmen kataloğu veya güncel sabitlerle tekrar hesap yapılmaz. Kaynak hakediş değişmiş/silinmişse yenileme gerekir. A4 dikey kişisel PDF; kimlik/ders/ücret/kesinti/net ve imza bilgilerini içerir. Çıktı kişiye ait benzersiz klasöre yazılır; Klasör düğmesi son oluşan PDF'nin klasörünü açar. Menü üstte Veri Girişi/Kurum ve İmza Bilgileri/Hakediş Hesapla/Öğretmen Listesi/Çıktı, altta Kişisel Bordro/Dönem Sabitleri olarak ayrılır. Sabitlerde üç eşit sütun ve sabit etiket yüksekliği kullanılır. Kartlarda simge alanı üstte, tutar alanı altta ayrılır; kart boyutu korunur.

- [x] Kişisel bordro seçim modeli ve kaynak sürüm kontrolü için davranış testleri yaz; başarısızlığı doğrula.
- [x] PersonalPayrollViewModel ve Shell navigasyonunu ekle; Öğretmen Listesi başlıklarını güncelle.
- [x] Mevcut PDF çizicisini ortaklaştır; kişisel PDF yerleşimini ve atomik çıktısını ekle.
- [x] Menü grupları, kişisel panel, imza, sabit alanlarının hizaları ve kart simge/tutar ayrımını uygula.
- [x] Birim testleri, WPF olay/binding/yerleşim testi ve PDF render kontrolünü tamamla. Sentetik test verisi kullan; kullanıcının verilerini değiştirme.
- [x] Kullanım ve devam notunu güncelle; yerel Git kaydı için değişiklikleri hazırla.

Sonuç: 164 birim testi geçti. WPF testi 1366/1040 genişlikte iki menü grubunu, dokuz sabitin hizasını, kişisel listede yalnız iki sütunu, seçim sonrası PDF/Klasör düğmelerini, yanlış öğretmenin reddini, çok kişilik kayıttan yalnız seçilen kişinin alınmasını ve büyük tutarlarda simgeyle çakışmamayı doğruladı. Kartların önceki ölçüsü korundu. PDF Python/pypdf ile ayrıca kontrol edildi: tek dikey A4, kimlik/IBAN ve 12 tutar kaynakla eşit; Poppler render incelendi. Başlangıçtaki 19 gerçek veri dosyası ve dosya listesi değişmedi. Debug ve Release derlemelerinde uyarı/hata yok.

İncelemede derin test dizinindeki 260 karakterlik yol bazı araçlarla açılamadı. Kişisel çıktı klasöründeki gereksiz öğretmen GUID/sürüm tekrarları kaldırıldı; zaman + tam GUID ile benzersizlik korunarak yol kısaltıldı ve bağımsız PDF okuması tekrar geçti. Gerçek SGK'ya yükleme veya yazıcıya iş gönderilmedi.

**Kontrol noktaları:** Yanlış ay/modül ve mevcut olmayan öğretmen seçimi; silinen öğretmenin geçmiş bordrosu; arşiv değişikliği/silinmesi; boş dönem; uzun isimler; 1366/1040 genişlik; büyük tutarlarda simgeyle çakışmama; klasör açma düğmesinin yalnız PDF sonrası etkinleşmesi. Yeni modüllerin kişisel çıktı eşlemesi kendi entegrasyonlarında tamamlanır.
