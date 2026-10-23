# Öğretmen paneli ve veri kaydı

Sol menü: Veri Girişi → Hakediş Hesapla → Ücretli Öğretmen Listesi. Modül seçimi Veri Girişi panelinde açılır listedir. Veri girişi ve hesap sonuçları panel değişiminde korunur.

Öğretmen listesinde Yeni Öğretmen ile form temizlenir; bir satır seçilince düzenleme formu açılır. Kaydetmeden form değişiklikleri kayıtlı listeyi değiştirmez. Silme, kişinin adı gösterilerek onaylanır; yalnızca seçilen ID silinir. Geçmiş hesap taslakları değiştirilmez.

Ortak kişi verisi `Veriler/ogretmenler.json` içinde saklanır; herhangi bir hesaplama çekirdeğine bağlanmaz. Kayıt sırasında dosya kilidi alınır ve liste yeniden okunur; yeni dosya geçici ada yazılıp doğrulanır, atomik değiştirme ile eski liste `.bak` olarak korunur. SQL kullanılmaz. TCKN/SGK/IBAN metindir. Aynı isimli kişiler farklı ID'lerle tutulabilir; dolu TCKN yinelenemez. Buradaki TCKN ve IBAN kontrolleri biçim kontrolüdür, resmî kimlik veya banka doğrulaması değildir.

Kaydet düğmesine basılmadan yalnızca ekranda kalan form otomatik kaydedilmez. Elektrik kesintisi sonrası böyle bir formun kurtarılacağı varsayılmamalıdır. Kaydedilmiş dosya bozuksa üstüne boş liste yazılmaz; kullanıcıya hata gösterilir. Güncellemede `Veriler` klasörü korunmalıdır.

Otomatik testler yeniden açma, düzenlemede ID koruma, aynı adlı kişiler, seçili silme, farklı depo örneklerinin güncellemesi, yinelenen TCKN, geçersiz bilgi, bozuk dosya koruması, panel geçişi ve kişi bilgisini hesaplama alanına taşımayı kapsar.
