# Masaüstü ve kayıt sürekliliği uygulama planı

**Amaç:** Taşınabilir uygulama tek masaüstü klasöründen güvenilir kayıtlar, anlaşılır kayıt bildirimleri ve kompakt arayüz ile çalışır.

**Kararlar:** Ana klasör Masaüstü/Ek Ders Hakediş Sistemi. Veriler, Çıktılar, Yedekler, Günlükler ve Kaynaklar burada ayrılır. EXE'nin başka yerden açılması veri yolunu değiştirmez. Mevcut ve RAR içindeki kişisel kayıtlar Git'e/pakete girmez. Aktarım çakışmaları otomatik ezilmez.

- [x] Mevcut/RAR verisini bağımsız okuyucu ile say ve doğrula; kaynak dosyaları koru.
- [x] Yeniden açılış testini önce başarısız gör; son kayıtlı hakedişi açılışta seç.
- [x] Tek sabit veri yolu, eski EXE konumundan güvenli ve çakışma bildiren aktarım; günlük yedek ve hata günlüğü.
- [x] Tek örnek çalıştırma ve başlangıç hatalarında görünür bildirim.
- [x] Harf içeren hesap alanı için TCMB biçimini kabul et; kayıt testleri.
- [x] Kayıt düğmelerinde gerçek kalıcılıktan sonra başarı, başarısızlıkta açıklayıcı popup.
- [x] Kompakt sabit kartları ve özgün simge; masaüstü kısayolu.
- [x] Tam test, WPF smoke, gerçek arşiv + PDF ve kapat/aç kontrolleri.
- [x] Yeni EXE'yi aynı klasörde güncelle; kaynak/paket/yedekleri alt klasörlerde düzenle.
