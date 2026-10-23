# Temel Eğitim Ücretli Öğretmen Hakediş Sistemi

Yeni ve bağımsız Windows masaüstü projesi. Veri Girişi panelinde açılır modül seçimi; ayrı Hakediş Hesapla panelinde Temel Eğitim kişi bazında hesaplama ve ara hesap dökümü; Ücretli Öğretmen Listesi panelinde kalıcı kayıt ekleme, düzenleme ve onaylı silme. Öğretmen listesi ve hesap taslakları SQL olmadan JSON'da saklanır. Toplu bordro, kesinleştirme ve resmî çıktılar henüz uygulanmadı.

## Kullanıcının belirlediği kapsam

- Windows'ta kurulum, Excel ve ayrıca .NET kurulumu gerektirmeyen taşınabilir dağıtım hedefi.
- SQL/SQLite ve veri tabanı sunucusu kullanılmaz.
- İlk modül temel eğitim ücretli öğretmen ödemeleri; usta öğretici daha sonra ayrı hesaplama modülü olarak eklenir.
- OEHS projesinin `feature/official-payment-document-templates` dalındaki renk ve kart düzeni referans alınır.
- Bordro (Çıktı-1), Banka Listesi (Çıktı-2), Personel Bildirimi (Çıktı-3) yazdırılabilir resmî belge akışındadır.
- Kişisel bordro talep üzerine; diğer bilgi amaçlı Excel çıktıları isteğe bağlıdır.
- Halkbank yükleme dosyası ayrı bir aktarım çıktısıdır, yazdırılacak belge değildir.

## Belgeler

- [Güncel saklama, taşınabilirlik ve çıktı tasarımı](docs/TASARIM_KARARLARI.md)
- [Excel incelemesi ve güncellenmiş yol haritası](docs/INCELEME_VE_YOL_HARITASI.md)
- [Ayrı hesaplama çekirdekleri ve geliştirme komutları](docs/MODUL_MIMARISI.md)
- [İlk sürüm kullanım notu](docs/KULLANIM.txt)

Gerçek personel Excel'leri, kullanıcı verileri, yedekler ve ödeme dosyaları bu depoya dahil edilmez.
