# Ek Ders Hakediş Sistemi (EDHS)

C# / WPF Windows masaüstü projesi; OEHS düğme, kart ve panel şablonlarını kullanır. Öğretmen kaydı yalnız Veri Girişi'nde yapılır. Aylık ders girişi, dönem sabitleri ve canlı hibrit hakediş tablosu JSON'a kaydedilir. Aylık kayıtlar ve hakedişler modül/yıl/ay kapsamında ayrılır; sabitler ortaktır. SQL, son kullanıcıda Excel veya ayrıca .NET kurulumu gerektirmez. Kaynak Excel başlıkları aynen korunur. 7.190 meslek kodu aramalı açılır listeden seçilir. Eksik Gün Sayısı SGK yan bilgisidir; parasal hakedişi etkilemez. Kayıtlı hakedişten üç PDF, doğrudan yazdırma ve özgün XSD ile doğrulanan SGK XML çıktısı alınır.

Her öğretmenin tek kişi kartı vardır; aynı TC ile ikinci kişi açılmaz. İsteğe bağlı okul/modül değişikliği alanında okul, tarih ve hedef modül kaydedilir. Geçiş ayında hakediş oluşturulurken tek ödeme modülü ve ay toplamı teyit edilir; kişi iki modül bordrosuna bölünmez. Görev geçmişi normal SGK işe giriş/çıkış tarihlerini veya kayıtlı bordroları değiştirmez. Eski modülsüz veriler ilk sürümün Temel Eğitim Ücretli Öğretmen kapsamına göre, orijinali yedeklenerek yeni biçime geçirilir.

## Kullanıcının belirlediği kapsam

- Windows için kendi .NET çalışma ortamını içeren EXE/ZIP yayınlanır. Mevcut yayın betiği taşınabilir paket üretir; kurulum sihirbazı henüz üretilmez.
- SQL/SQLite ve veri tabanı sunucusu kullanılmaz.
- Temel Eğitim Ücretli Öğretmen, Din Öğretimi Ücretli Öğretmen, Mesleki Eğitim Merkezi Ücretli Öğretmen, Halk Eğitim Merkezi Usta Öğretici ve Halk Eğitim Merkezi Emekli Usta Öğretici ayrı hesaplama çekirdekleriyle kullanılabilir. Diğer eğitim modülleri daha sonra kendi kurallarıyla eklenir.
- OEHS projesinin `feature/official-payment-document-templates` dalındaki renk ve kart düzeni referans alınır.
- Bordro, Banka Listesi ve Personel Bildirimi çıktı akışındadır. Bordro tek yatay A4'e okunaklı sığmıyorsa açık hata verilir; satırlar düşürülmez.
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

[Yeni çekirdekler öncesi uygulama denetimi](docs/CEKIRDEK_ONCESI_DENETIM_2026-10-24.md).

[Din Öğretimi Ücretli Öğretmen çekirdeği, Excel formülleri ve kaynakta tespit edilen sapmalar](docs/DIN_OGRETIMI.md).

[Halk Eğitim Merkezi Usta Öğretici çekirdeği ve Excel doğrulaması](docs/HALK_EGITIMI.md).

[Emekli Usta Öğretici çekirdeği, ayrı dönem sabitleri ve SGK belge 02](docs/EMEKLI_HALK_EGITIMI.md).

[Mesleki Eğitim Merkezi Ücretli Öğretmen çekirdeği ve eski/güncel sabit ayrımı](docs/MESLEKI_EGITIMI.md).

Kişisel Bordro ekranında modül ve ay/yıl seçilerek kayıtlı hakedişten tek öğretmen için A4 PDF alınır; Klasör düğmesi kaydedilen konumu açar. Sol menüde temel işlemler ve Kişisel Bordro/Dönem Sabitleri grupları ayrıdır. Öğretmen Listesi adı, sabitlerin hizaları ve simgelerden ayrı tutar alanları günceldir; sol altta kullanıcı imzası bulunur.

Program adı EDHS'dir; güncel derleme `EDHS.exe` üretir. Diğer İşlemler grubunun sonundaki Ayarlar paneli 1280×720, 1366×768 ve 1920×1080 pencere tercihlerini JSON'da saklar. Pencere kullanılabilir ekran alanına sığdırılır; tercih sonraki açılışta uygulanır. Kilitli dönem sabitleri gri, Güncelle ile açılan alanlar beyazdır. Hibrit başlıklar değiştirilmeden satırlara bölünür; toplam sütun genişliği azaltılmıştır. Yeni PDF/XML çıktılarının kökü Masaüstü/EDHS/Çıktılar'dır; daha önceki çıktı dosyaları taşınmaz.
