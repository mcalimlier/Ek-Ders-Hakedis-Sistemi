# Şifre Kasası Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans. Steps use checkbox syntax for tracking.

**Goal:** Onaylanan iki kayıtlı taşınabilir kasayı WPF programına eklemek.
**Architecture:** Storage katmanında doğrulanan şifreli zarf ve kilitlenebilir oturum; WPF'de ayrı kasa penceresi. Kişisel dosyalar bordro verilerinden ve dağıtımdan ayrılır.
**Tech Stack:** .NET 10, WPF, Konscious Argon2id, System.Security.Cryptography.AesGcm, xUnit.
**Spec:** docs/superpowers/specs/2026-10-29-sifre-kasasi-design.md

## Global Constraints
- İki sabit grup, beşer alan; şifreler ve kurtarma anahtarı loglanmaz.
- Anahtar türetme 64 MiB / 3 / 1, 256 bit AES-GCM, her işlemde rastgele nonce.
- Kasa yalnız kullanıcı isteğiyle oluşturulur, Git ve paket dışında kalır.

## Review Focus
- Yanlış parola, bozuk/değiştirilmiş ve aşırı büyük dosya mevcut kasayı bozamaz.
- İki oturumun aynı dosyayı değiştirmesi sessiz veri kaybı yaratmaz.
- Kilitlenen oturumdan okuma/yazma yapılamaz.
- Şifreli kopya başka klasörde aynı parola ve kurtarma anahtarıyla açılır.
- Parola değişikliği sonrası yeni dosya eski parolayı reddeder, eski yedeklerin davranışı belgelenir.

## Tasks
- [x] Önce VaultTests.cs testlerini yaz, başarısız derlemeyi doğrula.
- [x] VaultStore.cs: oluşturma, açma, kurtarma, atomik kayıt, dışa/içe aktarma ve çakışma denetimi; testleri geçir.
- [x] VaultWindow.cs: oluştur/aç/kurtar, iki grup, maskele/göster/kopyala, kaydet/kilitle, taşıma ve parola değiştir; MainWindow menüsüne bağla.
- [x] Git dışlama ve kullanıcı belgesi; birim testleri, WPF derleme ve sentetik UI doğrulaması.
- [x] Güncel dağıtım paketini hazırla, kişisel kasa içermediğini doğrula.


## Verification — 29 Ekim 2026
- Önce testler eklendi; VaultStore henüz yokken derlemenin başarısız olduğu görüldü.
- Release birim testleri: 261/261 başarılı; tests/Hakedis.Tests/TestResults/vault-full-suite.trx.
- Unicode sınır testi ilk çalışmada başarısız oldu; şifreli yük sınırı geçerli en büyük kaydı destekleyecek şekilde düzeltildi.
- Son WPF testi: artifacts/ui-vault-verified; kurulum, iki grup/on alan, kayıt, süreli gösterim, pano kısıtı, kilit, yanlış parola, kurtarma, parola değiştirme, hareketsizlik/odak ve SessionLock olayları başarılı. Yalnız sentetik kayıtlar kullanıldı.
- Release WPF derlemesi: 0 hata/uyarı. Storage bağımlılık taramasında bildirilen güvenlik açığı yok.
- dist/v1.0.4 self-contained win-x64 tek dosya yayın; ZIP girdileri yalnız Ek Ders Hakediş Sistemi.exe + KULLANIM.txt. Masaüstü çalışma klasöründeki EXE ile yayın EXE'sinin SHA-256 değerleri aynı.
- SHA-256: 388529CB62A5C15F813C0EC65516A7F7D190829FA477DCAC214AA0DC698C67CD.
- İnceleme bu oturumda yapıldı; bağımsız güvenlik denetimi değildir. GitHub push/publish yapılmadı. Inno Setup derleyicisi bulunmadığından yeni kurulum sihirbazı üretilmedi; taşınabilir EXE/ZIP hazırlandı ve kurulum kaynağı 1.0.4'e güncellendi.

## Görsel bütünlük güncellemesi — 29 Ekim 2026
- Kullanıcı kasa panellerinin küçültülmesini ve ana programın renk düzeninin uygulanmasını istedi.
- Pencere 860×620 oldu; kartlar CardCompact/PremiumPanel renkleri, altın başlık ve birincil düğmeler, lacivert giriş alanları ve daha sıkı satır aralıkları kullanıyor. Kasa stilleri Themes/Vault.xaml içinde isimlendirilmiş kaynaklarla sınırlı; diğer ekranların stilleri değişmedi.
- Ana parola değiştirme kartı açılır/kapanır oldu; mevcut WPF testi bu bölümü açarak aynı parola değiştirme akışını doğruluyor.
- artifacts/ui-vault-compact-final ekran testlerinin tümü başarılı; son görüntü gözle kontrol edildi. Test/publish işlemleri ilk denemede ortak PDB dosyalarında çakıştı; son yayın ve test sırayla başarıyla tamamlandı.
- Çalışma klasöründeki EXE güncellendi; SHA-256: 7F77572447D4299A9442D73BAD9D2AF33E9F5F17D1B67977C31643F4E5E815BA.
- Paket: dist/Ek-Ders-Hakedis-Sistemi-1.0.4-kompakt-win-x64.zip; yalnız EXE ve KULLANIM.txt. Şifreleme/depolama kodu değiştirilmedi.

## Ana parola kuralı güncellemesi — 29 Ekim 2026
- Kullanıcının açık isteği: en az 10 karakter; en az bir harf, rakam ve sembol. Boşluk sembol sayılmaz. Büyük/küçük harfin ikisi birden zorunlu değildir.
- VaultStore.ValidatePassword oluşturma ve parola değiştirmede ortak kuralı uygular. Unicode karakterler Rune olarak sayılır. Açma/içe aktarma eski parolaları yeni politikayla reddetmez; şifreleme biçimi değişmedi.
- Önce eklenen sınır ve bileşim testleri eski sürümde 5 başarısızlık gösterdi; uygulama sonrası 16/16 kasa testi başarılı: tests/Hakedis.Tests/TestResults/vault-password-policy.trx.
- WPF ekran testi tam 10 karakterli Türkçe parola ile oluşturma, açma, kurtarma ve parola değiştirmeyi başarıyla doğruladı: artifacts/ui-vault-ten-character.
- Yardım metinleri ve kullanım kılavuzu yeni kuralla uyumlu. Sürüm 1.0.5; self-contained win-x64 yayını başarılı. Çalışma klasöründeki EXE ve kullanım belgesi güncellendi; paket yalnız EXE ve KULLANIM.txt içerir.
