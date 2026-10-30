# İşyeri bilgileri kasası

Sol menüde **Şifre Kasası** seçilir. İlk kullanımda en az 10 karakterlik, en az bir harf, bir rakam ve bir sembol içeren, başka yerde kullanılmayan bir ana parola ve tekrarı girilir. Boşluk sembol yerine geçmez. Daha uzun parola kullanılabilir. Program parolanızı saklamaz.

İlk oluşturma ekranındaki 64 karakterlik kurtarma anahtarını ayrı, güvenli bir yerde saklayın. Bu anahtar kasayı açabilir; kasa dosyasıyla birlikte paylaşmayın. Program anahtarı yeniden göstermez. Ana parola ve kurtarma anahtarı birlikte unutulursa erişim kurtarılamaz. Kurtarma anahtarıyla açtıktan sonra yeni ana parola belirleyebilirsiniz.

Kasada iki sabit grup bulunur: Ücretli Öğretmenler ve Usta Öğreticiler. Her grupta kullanıcı kodu, sistem şifresi, işyeri şifresi, işyeri kodu ve işyeri sicil no bulunur. **Bilgileri kaydet** iki grubun bilgilerini şifreleyerek kaydeder. Kaydedilmemiş girişler kilitlemede silinir.

Kasa her açılışta kilitlidir. İki dakika hareketsizlikte, pencereden başka pencereye geçildiğinde, Windows oturumu kilitlendiğinde ve pencere kapatıldığında kilitlenir. Göster düğmesi bir alanı 15 saniye gösterir. Kopyalanan içerik 20 saniye sonra (başka içerikle değişmemişse) temizlenir. Windows pano geçmişi ve bulut eşitlemesine alınmaması istenir; üçüncü taraf pano yöneticilerinin davranışı garanti edilemez.

Kasa masaüstündeki Ek Ders Hakediş Sistemi/Kasa/isyeri.edhskasa dosyasında tutulur; program kurulduğunda bu dosya oluşturulmaz. Bordro verilerine ve normal otomatik yedeklere katılmaz. **Şifreli kasayı dışa aktar** ile düzenli yedek alın. Dosyayı USB ile başka bilgisayara taşıyın; programı kurup Şifre Kasası ekranındaki ana parola alanına taşınan kasanın parolasını yazın ve **Şifreli kasayı içe aktar** seçin. Geçersiz parola/dosya mevcut kasayı değiştirmez. İçe aktarma mevcut kasanın yerini alır; birleştirme ve otomatik eşitleme yoktur. Önce mevcut kasayı dışa aktarın.

Ana parola değiştirilince mevcut dosya yeni parolayı ister. Kurtarma anahtarı değişmez. Önceden dışa aktarılmış kopyalar eski parolalarıyla açılır; parola değişikliği onları uzaktan güncellemez.

## Güvenlik sınırları

Argon2id (64 MiB, 3 geçiş, paralellik 1) paroladan anahtar türetir. Rastgele 256 bit veri anahtarı AES-256-GCM ile tüm alanları şifreler; veri anahtarı parola ve 256 bit rastgele kurtarma anahtarıyla ayrı ayrı sarılır. Her şifreleme işleminde yeni 96 bit nonce kullanılır. Dosyalar sınırlı boyutta okunur; bilinmeyen sürümler reddedilir. Yazımlar atomik ve eşzamanlı oturum çakışmaları denetimlidir.

Kaynak kod GitHub'da yayımlanabilir; kişisel kasa, ana parola ve kurtarma anahtarı kaynak/kurulum paketine konmaz. .gitignore kişisel kasa uzantılarını dışlar; kullanıcı kurtarma anahtarını kaynak klasörüne kaydetmemelidir. Paket yalnız derleme çıktıları ve kullanım belgesinden üretilir.

Kilitleme anahtar baytlarını temizler, kontrolleri boşaltır ve oturumu kapatır. .NET/WPF içindeki metinlerin tüm bellek kopyalarını silme garantisi yoktur. Zararlı yazılım, tuş kaydedici, yönetici erişimi veya bellek dökümü karşısında mutlak koruma sağlanmaz. Windows hesabını ve diski de koruyun. Bağımsız güvenlik denetimi yapılmış bir parola yöneticisi olduğu iddia edilmez.

Teknik kaynaklar: https://cheatsheetseries.owasp.org/cheatsheets/Cryptographic_Storage_Cheat_Sheet.html ve https://learn.microsoft.com/dotnet/api/system.security.cryptography.aesgcm.encrypt
