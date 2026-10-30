# İşyeri bilgileri kasası

Kullanıcının onayladığı tasarım: tek ana parola, iki sabit işyeri (Ücretli Öğretmenler / Usta Öğreticiler), her birinde kullanıcı kodu, sistem şifresi, işyeri şifresi, işyeri kodu ve işyeri sicil no. İlk kurulumda gerçek kasa oluşturulmaz.

Seçenekler: harici şifre yöneticisi veya uygulama içinde taşınabilir şifreli kasa. Kullanıcı uygulama içindeki kasayı seçti. WPF menüsünden ayrı pencere açılır; bordro verilerine bağlanmaz.

Kasa dosyası çalışma kökünde Kasa/isyeri.edhskasa olarak saklanır. Argon2id (64 MiB, 3 geçiş, 1 paralellik), AES-256-GCM ve rastgele veri anahtarı kullanılır. Ana parola ve rastgele kurtarma anahtarı veri anahtarını ayrı ayrı sarar. Kurtarma anahtarı yalnız ilk oluşturulduğunda gösterilir. Kalıcı düz metin, parola kaydı, rapor entegrasyonu ve otomatik eşitleme yoktur.

İki dakika hareketsizlik, pencerenin odağı kaybetmesi, Windows kilidi ve kapanış kasayı kilitler. Alanlar maskelidir; gösterim kısa sürelidir. Kopyalanan içerik, başka bir pano içeriğiyle değişmemişse kısa sürede temizlenir; geçmiş/eşitleme için Windows pano kısıtları kullanılır. Kaydedilmemiş girişler otomatik kilitte silinir; ekranda açık uyarı bulunur.

İçe aktarma parolayla doğrulanmadan mevcut kasayı değiştirmez; değiştirmeden önce kullanıcıya açık onay sunar. Dosya yazımı atomik, eşzamanlı düzenleme çakışmaları reddedilir. Dosya boyutu ve biçim doğrulaması yapılır. Parola değişimi ve kurtarma desteklenir. Eski dışa aktarılmış kopyalar eski parola/kurtarma anahtarıyla açılmaya devam eder.

Git ve dağıtım paketi kişisel kasa/kurtarma dosyalarını içermez. Güçlü ana parola gereklidir; zararlı yazılım, bellek dökümü, pano yöneticileri ve açık kasada yönetici erişimine karşı mutlak koruma vaat edilmez.

Kullanıcının son parola kuralı: yeni kasa oluşturma ve ana parola değiştirme için en az 10 karakter, en az bir harf, bir rakam ve bir sembol gerekir. Boşluk sembol sayılmaz; daha uzun parolalara izin verilir. Kural eski kasayı açma ve içe aktarma sırasında uygulanmaz; mevcut parolalar değiştirilmez.
