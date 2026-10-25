# Taşınabilir EXE ve GitHub yayını

EDHS'nin Windows x64 paketi kendi .NET çalışma ortamını içerir. Kullanıcının ayrıca Excel veya .NET kurması gerekmez. Bu paket taşınabilirdir: ZIP tamamen bir klasöre çıkarılır ve EDHS.exe açılır. Kurulum sihirbazı içermez.

## Paketi yeniden hazırlama

Proje kökünde PowerShell'den çalıştırın:

```powershell
.\scripts\Publish-Windows.ps1 -Runtime win-x64
```

Betik önce testleri çalıştırır. Başarılıysa kendi çalışma ortamını içeren tek EXE üretir; kullanım dosyasını ekleyip `dist/EDHS-win-x64-tarih-saat.zip` oluşturur. Aynı adlı klasör içinde EDHS.exe bulunur. ARM64 Windows hedefi gerektiğinde betiğe `-Runtime win-arm64` verilebilir; farklı hedefler ayrı hazırlanıp doğrulanmalıdır.

Dağıtımda ZIP'i paylaşın. Alıcı ZIP'i tamamen çıkarsın ve EDHS.exe'yi açsın. Paket gerçek/test öğretmen kayıtlarını, aylık girişleri, SGK işyeri profilini veya eski hakedişleri içermez. İlk kullanımda kurum ve personel bilgileri girilir.

Güncelleme yapılırken program kapatılır, mevcut `Veriler` klasörü yedeklenip korunur ve yeni EDHS.exe aynı program klasörüne konur. Başka bilgisayara kişisel kayıt taşımak gerektiğinde Veriler klasörü ayrıca, yalnız yetkili kullanıcıya aktarılır; herkese açık yayın paketine eklenmez.

## GitHub üzerinden dağıtım

GitHub'a yüklemek zorunlu değildir; dosyayı doğrudan paylaşabilirsiniz. Merkezi bir indirme bağlantısı ve sürüm geçmişi için Releases kullanılabilir:

1. Kaynak değişikliklerinin tamamını yerel olarak kaydedin. GitHub Desktop'ta Push origin ile yayımlanacak kaynak sürümünü GitHub'a gönderin.
2. Deponun web sayfasında Releases bölümünü, ardından Draft a new release seçeneğini açın.
3. Sürüm etiketi seçin veya oluşturun; örneğin ilk deneme sürümü için `v1.0.0-preview.1`. Target olarak ilgili güncel kaynak dalını seçin.
4. Başlık ve sürüm notlarını yazın. Hazırlanan EDHS ZIP'ini dosya ekleme alanına yükleyin.
5. Deneme yayını için This is a pre-release seçeneğini işaretleyin. Save draft ile taslak tutabilir veya paylaşmaya hazır olduğunuzda Publish release ile yayımlayabilirsiniz.
6. Arkadaşlarınıza Releases sayfasındaki Assets bölümünde yer alan EDHS ZIP'inin indirme bağlantısını gönderin. Deponun erişim izinleri, yayın dosyalarına erişimi de etkiler.

Dosyaları ana kaynak kod klasörüne eklemek yerine derlenmiş paket için Releases kullanılır. Öğretmen/IBAN/TC bilgileri, Veriler klasörü ve özel yedekler yayın dosyaları arasına konmaz.

Resmî kaynaklar: [GitHub Releases hakkında](https://docs.github.com/en/repositories/releasing-projects-on-github/about-releases), [web arayüzünden yayın oluşturma](https://docs.github.com/en/repositories/releasing-projects-on-github/managing-releases-in-a-repository).
