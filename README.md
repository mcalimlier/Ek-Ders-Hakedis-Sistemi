# Temel Eğitim Ücretli Öğretmen Hakediş Sistemi

C# / WPF Windows masaüstü projesi; OEHS düğme, kart ve panel şablonlarını kullanır. Öğretmen kaydı yalnız Veri Girişi'nde, düzenleme/silme öğretmen listesindedir. Aylık ders girişi, dönem sabitleri ve canlı hibrit hakediş tablosu modül/yıl/ay kapsamında JSON'a kaydedilir. SQL, son kullanıcıda Excel veya .NET kurulumu gerektirmez. Kaynak Excel başlıkları aynen korunur. 7.190 meslek kodu aramalı açılır listeden seçilir. Eksik Gün Sayısı, ayın takvim günleri ve giriş/çıkış tarihleri üzerinden SGK yan bilgisi olarak hesaplanır; hakediş tutarını etkilemez. Hakediş kaydı henüz resmî basılı belge üretimi anlamına gelmez.

Her öğretmen yalnızca kendi modülüne aittir. Listeler ve değişiklik işlemleri modüle göre ayrılır; aynı kayıt ID'si veya TCKN başka modüle tekrar kaydedilemez. Eski modülsüz veriler ilk sürümün Temel Eğitim kapsamına göre, orijinali yedeklenerek yeni biçime geçirilir.

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
- [Arayüz referansı ve yeni kayıt akışı](docs/ARAYUZ_REFERANSI.md)
- [Modüle ait öğretmen kayıtları ve veri geçişi](docs/MODUL_OGRETMEN_KAYITLARI.md)

Gerçek personel Excel'leri, kullanıcı verileri, yedekler ve ödeme dosyaları bu depoya dahil edilmez.


[Excel denetimi, 50 senaryo karşılaştırması ve yeni ekran akışı](docs/EXCEL_DENETIMI.md).
