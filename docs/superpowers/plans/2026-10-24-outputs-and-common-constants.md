# Ortak sabitler ve hakediş çıktıları uygulama planı

> Use superpowers:executing-plans. Kullanıcı dört ayrıntılı değişikliği doğrudan istedi; C#/WPF ve JSON düzeni korunur.

**Goal:** Tek görev tarihi, yan yana öğretmen kartları, ortak sabitler ve güvenli geçmiş/üç belge çıktısı.

**Architecture:** Ortak sabitler ayrı sürümlü JSON'da tutulur; hakediş görüntüleri kendi sabit/öğretmen/sonuç kopyalarıyla değişmez kalır. Geçmiş düzenleme yalnız ayrı görüntü kopyasında çalışır; onay ve dosya sürümü doğrulamasından sonra atomik değiştirir. PDF ve yazdırma aynı sayfa modelinden üretilir; Excel kurulumu gerekmez.

**Tech stack:** .NET 10/WPF, JSON, PDFsharp-WPF 6.2.4 (MIT, sabit sürüm).

**Spec:** Kullanıcının bu turdaki dört maddesi.

## Constraints / kararlar
- Ay içi tarihler aktif modelden çıkarılır. Eski dosyalardaki anahtarlar yalnız okuma geçişinde tanınır; eski görüntülerin sonuçları yeniden hesaplanmaz.
- Yeni hesaplarda ortak sabitler; geçmiş düzenlemede kaydın kendi sabitleri.
- Sabit düzenleme: Güncelle -> düzenlenebilir -> Dönem sabitlerini değiştir -> kaydet ve kilitle. Eski aylık/ödenek dosyaları bu işlemde yazılmaz.
- Aktif çalışma varken geçmiş düzenlenmez. Bitirilen hakediş veya açıkça temizlenmiş çalışma engel oluşturmaz.
- Geçmiş değişikliği ana öğretmen kataloğunu ve aylık taslağı değiştirmez; yalnız seçili arşiv kaydını günceller. Hayır/Vazgeç dosyaya dokunmaz. Eski sürüm .bak korunur.
- Belgeler: Bordro (Çıktı-1), Banka Listesi (Çıktı-2), Personel Bildirimi (Çıktı-3). Kaynak başlıklar korunur; kırık #REF/eski dönem ve kişiye özel AGİ açıklaması taşınmaz.
- Çıktı hedefi: Masaüstü/ÜcretliUsta Öğretici Hakediş Sistemi/Çıktılar/modül/dönem/kayıt-sürümü. Her üretim ayrı klasör; önceki çıktı ezilmez.
- PDF ve doğrudan yazdırma ayrı düğmeler. Yazdır düğmesi yazıcı seçimini açar ve aynı üç belgeyi tek iş olarak gönderir; testte fiziksel yazıcıya iş gönderilmez.
- Kurum, banka ve imza bilgileri Çıktı panelinden düzenlenebilir; gerçek varsayılanlar yalnız yerel Veriler içine alınır. Personel hareketleri önceki ayın kayıtlı kadrosuyla karşılaştırılır; veri kaynağı belgede belirtilir.

## Review focus
- Eski JSON, silinmiş öğretmen ve eski SGK yan bilgisi korunarak çıktı açılır.
- Ortak sabit değişikliği önceki kaydın tutarını ve byte içeriğini değiştirmez.
- Geçersiz/çakışan geçmiş düzenleme, onaysız değiştirme ve Vazgeç dosya yazmaz.
- Çok satır/uzun ad/uzun kurum başlığı ve Türkçe fontlar sayfaya sığar; başlıklar tekrarlanır.
- Aktif ders/form/sabit değişikliği geçmiş düzenlemeyi engeller; temizle kaydı silmez.

## Tasks
- [x] Tarih alanlarını kaldırma ve eski JSON geçişi: önce başarısız test, uygulama, takvim ve tüm birim testleri.
- [x] Ortak sabit deposu/düzenleme modu: aktif hesap yayını ve geçmişin değişmemesi testleri.
- [x] Arşiv listeleme/sürüm denetimi/ayrı düzenleme: engelleme, iptal, onay ve yedek testleri.
- [x] Yan yana öğretmen kartları ve yeni Çıktı paneli; arşiv düzenlemede yalnız ilgili düğmeler.
- [x] Üç rapor modeli, PDF ve yazdırma: sentetik 1/10/60+ kişi; PDF metin/tutar denetimi ve tüm sayfaları görsel inceleme.
- [x] Gerçek verileri yedekle, WPF smoke, Debug çalıştırma, devam notu ve yerel commit.

## Sonuç / inceleme kaydı
- 112 test, WPF 10 panel görüntüsü ve PDF/yazıcı akışı geçti; Debug derleme 0 uyarı/0 hata.
- 1 ve 65 kişilik altı gerçek PDF Poppler ile render edildi; Türkçe metin/başlık/parametre/tutar/sayfa sınırları kontrol edildi. Fiziksel yazıcı testi yapılmadı.
- PDFsharp 6.2.4 sabit sürümü NuGet ve resmi PDFsharp belgeleriyle doğrulandı; Excel/COM bağımlılığı eklenmedi.
- Kullanıcı Excel kurum/banka/imza bilgilerini başlangıç için onayladı; yalnız yerel Veriler'e aktarıldı.
- Yerel gerçek saat toplamı artık 975; 10 öğretmen ve tüm parasal girdiler korundu. Eski 972 saatlik örneğin yeniden yüklenmesi yasak.
- Arşiv sayısı 0; gerçek bir finansal görüntü test için otomatik oluşturulmadı.
- Araçta bağımsız inceleyici yok; manuel diff incelemesi, eski JSON, sürüm çakışması, iptal ve saklama testleri tamamlandı.
