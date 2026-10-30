# Kurum saatleri uygulama planı
Goal: Onaylanan geçiş ayı davranışını mevcut WPF uygulamasına ekle.
Spec: ../specs/2026-10-30-school-hours-design.md
- [x] Tarih JSON uyumluluğu ve nullable kurum saatleri için başarısız testleri yaz/çalıştır.
- [x] PayrollRow, aylık kart, hibrit tablo ve ödeme tercihi üzerinden ayrıntıları koru ve toplamı hesapla.
- [x] Geçiş ayı, sonraki ay, yeni geçiş, tek taraf boş, negatif, kayıt/arşiv tekrar açılış testlerini çalıştır.
- [x] Tam test ve WPF derlemesini doğrula.

Doğrulama: 261/261 birim testi geçti; WPF uygulaması 0 hata/uyarıyla derlendi. Tam UI smoke çalışması uygulama açılış diyaloğunda beklediğinden tamamlanmadı. Bağımsız incelemedeki iki bulgu düzeltildi ve regresyon testleri eklendi.

Tam arayüz doğrulaması tamamlandı: üretim açılışını tetikleyen test başlatıcısı yalnız WPF kaynaklarını yükleyecek şekilde ayrıldı; bütün modül, PDF, XML, kayıt/iptal, görünüm ve geçiş saati UI kontrolleri geçti. Yeni alanlar için gerçek TextBox girişleri ve hibrit uyarı görünürlüğü doğrulandı; binding hatası yok. Gerçek verinin kopyasında 14 kişi, 14 aylık giriş, 3 arşiv okundu. Asıl 70 veri/çıktı dosyasının SHA256 özeti değişmedi; güncelleme öncesi ZIP doğrulandı.
