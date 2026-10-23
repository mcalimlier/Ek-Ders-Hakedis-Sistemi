# İlk sürüm doğrulama kaydı

23 Ekim 2026 — Yerel Windows ortamı, .NET SDK 10.0.402.

- Temel Eğitim hesapları önce boş hesap metodu ile test edildi: 13 beklenen başarısızlık; ardından uygulandı.
- JSON saklama testleri boş metodlarla çalıştırıldı: 8 beklenen başarısızlık; ardından uygulandı.
- Son test koşusu: **32 başarılı, 0 başarısız**. Excel toplamları, DYK/yuvarlama, vergi eşikleri, geçersiz girdiler, eksik modüller, JSON eski kopya/kilit/bozuk dosya, arayüz modelinde sonuç temizleme ve taslağı yeni oturumda açma.
- Release çözüm derlemesi: **0 hata, 0 uyarı**.
- Self-contained win-x64, PublishSingleFile ile EXE üretildi; ZIP açılarak iki girdisi doğrulandı: Hakedis.App.exe ve KULLANIM.txt.
- Uygulama açıldı; beş modül, girdi ve sonuç alanları Windows erişilebilirlik ağacında gözlendi. Ekran yakalama siyah görüntü döndürdüğü için görsel düzen ve tıklama akışı tam doğrulanamadı. Temiz ve .NET kurulu olmayan başka Windows bilgisayarında çalıştırma ayrıca yapılmalıdır.
- Kodun yerel incelemesi: Contracts öğretmen girdisi içermiyor; Core WPF veya Storage'a bağlı değil; diğer dört modülde geri dönüş hesabı yok. Taslak açılırken modül/kural ve veri doğrulanıyor, sonuç yeniden hesaplanıyor. Gerçek personel kayıtları ve EXE/ZIP git dışında.

Kalan ürün işleri: çok personelli kayıt, kurum/görevlendirme, Excel içe aktarımı, dönem parametrelerinin yetkili kaynakla onayı, kesinleşmiş dönem sürümleri, paket yedek/geri yükleme, üç basılı belge, isteğe bağlı Excel ve Halkbank çıktısı. İlk sürüm bunları tamamlamış sayılmaz.
