# İki bilgisayardaki geliştirmelerin birleştirilmesi

Amaç: Bu bilgisayardaki şifre kasası çalışmasını diğer bilgisayardan GitHub'a gönderilmiş bordro/giriş geliştirmeleriyle birleştirmek.

- Ortak taban: `52d28b8`.
- Diğer bilgisayarın kaynak kaydı: `79ff845` (bordro detayları, modül filtreleme, okul geçişi saatleri ve daha güvenli giriş akışları).
- Yerel çalışma: iki kayıtlı şifreli kasa, taşıma/kurtarma/kilitleme, kompakt lacivert-altın arayüz, 10 karakter ve harf/rakam/sembol kuralı.
- Birleşik sürüm: `1.0.6`.

## İşlemler

- [x] Yerel 19 kaynak dosyasının kopyalarını doğrula ve Git stash ile ayrıca koru; kişisel veri dosyalarını dahil etme.
- [x] GitHub'daki kaydı fast-forward ile al, yerel kasa değişikliklerini üzerine uygula.
- [x] Tek çakışma olan UI test başlangıcında üretim verisini açmayan test başlatıcısını koru; Vault temasını ve test akışını ekle.
- [x] Bağımsız 13 yerel dosyanın içeriğinin satır sonları dışında aynı olduğunu doğrula; ortak dosyalarda iki tarafın değişikliklerini koru.
- [x] Tam Release birim testlerini çalıştır: 281/281 başarılı, `tests/Hakedis.Tests/TestResults/merged-full-suite.trx`.
- [x] Tüm WPF akışlarını ve kasa testlerini aynı uygulama kaynaklarıyla çalıştır: `artifacts/ui-merged-1.0.6-final`, binding hatası yok.
- [x] Birleşik EXE/ZIP oluştur; başlangıçtaki 9 kullanıcı dosyasının SHA256 özetlerinin değişmediğini doğrula.
- [x] Kaynakları tek yerel Git kaydında koru. Kullanıcının ek talimatıyla GitHub main eşitlemesi ve 1.0.6 kararlı sürüm yayını da kapsama alındı.

Yedek kaynaklar, stash ve kişisel dosya özetleri yerel `artifacts/merge-backup-*` altında korunur; Git'e dahil edilmez. Kişisel kasa bilgileri başka bilgisayarın işyeri verileriyle birleştirilmez; bu çalışma program kaynaklarının birleştirilmesidir.

Tam UI çalışması ilk denemede eski üç alt menü beklentisinde durdu. Kasa ile dördüncü düğme eklenmesini doğrulayacak şekilde `PersonalPayrollChecks` güncellendi; sonraki tam çalışma tüm modüller, yeni iki akış ve kasa testleriyle geçti. Bu düzeltme üretim menüsünün davranışını değiştirmedi.

Yayın doğrulaması: dist/v1.0.6; masaüstündeki EXE aynı SHA256 değerinde (08192A9D9ADE5E4A24061C3902F67BD43FFA9BD894694D669E6176D2C98259AB). ZIP yalnız EXE ve KULLANIM.txt içerir. Önceki EXE yerel kaynak yedeğinin yanında saklandı. Inno Setup derleyicisi bulunmadığından yeni kurulum sihirbazı üretilmedi; taşınabilir EXE/ZIP hazırlandı.

Ek kapsam: Yerel ve GitHub dalları denetlendi; yalnız main bulundu. Eski sürüm etiketleri dal değildir ve korunur. v1.0.6 kararlı sürüm, test edilmiş birleşik kaynak kaydını hedefleyecektir.
