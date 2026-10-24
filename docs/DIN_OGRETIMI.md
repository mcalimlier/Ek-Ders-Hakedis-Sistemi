# Din Öğretimi

## Modül ve kayıtlar

Yeni modülün adı **Din Öğretimi**, kayıt anahtarı `DinOgretimi`dir. Eski `ImamHatipLisesi` anahtarı yeniden kullanılmadı; eski enum değerleri ve Temel Eğitim dosyaları korunur. Hazır olmayan modüller hazır görünmez.

`Hakedis.DinOgretimi.Core` ayrı .NET 10 projesidir. Yalnız Contracts'a başvurur; Temel Eğitim hesaplayıcısını çağırmaz. Kendi girdi, parametre, sonuç ve SGK takvim sınıfları vardır. `PayrollCores` uygulama adaptörü bu çekirdeğin sonucunu mevcut ortak bordro/arşiv biçimine eşler. Ortak biçim sınıflarının bir bölümü uyumluluk amacıyla eski TemelEgitim ad alanındadır; bu, Din Öğretimi'nin orada hesaplandığı anlamına gelmez.

Modül seçimleri öğretmen kaydı, aylık giriş, hakediş, öğretmen listesi ve kişisel bordroda kullanılabilir. Öğretmenler modülle sınırlandırılır; aynı TC iki çekirdeğe kaydedilemez. Aylık/arşiv dosyaları `AylikKayitlar/DinOgretimi` altında, PDF/XML çıktıları DinOgretimi klasöründe tutulur. Aynı modül/ay için ikinci hakediş engellenir; farklı modüllerde aynı ay kullanılabilir. Düzenle onayla aynı arşivi günceller. Personel bildiriminin önceki ay sayısı yalnız aynı modülün arşivinden gelir.

Dönem sabitleri, kurum/imza girişleri ve ücretli öğretmen SGK işyeri profili mevcut ortak panellerden alınır. Kayıtlı bordronun sabitleri ve kurum/imzaları dondurulmuştur; güncel sabitler eski bordroyu değiştirmez. Kaynak dosyadaki gerçek öğretmenler otomatik içe aktarılmadı; önceki gerçek kayıtlara dokunulmadı.

## Kaynak ve formüller

Kaynak: kullanıcının `01-Eylül 2026 - Dinöğretimi Ücretli Öğretmen Ek Ders Hakediş.xlsx` dosyası. SHA-256: `4C42B637EAA4A2D2765515F15F1CD5259E6D987946A8846FD2D7950B7CDD03B2`.

Sağlam örnek satır `Bordro!I7:AA7`:

| Alan | Hesap |
|---|---|
| Saat ücretleri | Katsayı × gösterge; DYK ayrıca çarpan ile |
| Gündüz brüt | Saat × birim ücret, iki ondalık |
| DYK brüt | Saat × birim ücret; toplam brütte iki ondalık |
| SGK'ya esas ücret | Gündüz + DYK brüt, iki ondalık |
| Prim günü | Toplam saat / 7,5, yukarı tam sayı |
| İştirakçi / işveren | Brüt × ilgili oran, ayrı iki ondalık |
| Tahakkuk | Brüt + işveren |
| GV matrahı | Tahakkuk − işveren − iştirakçi |
| Gelir / damga vergisi | İlgili matrahın istisnayı aşan kısmı × oran |
| Kesinti | Vergiler + iştirakçi + işveren + manuel icra, iki ondalık |
| Net | Tahakkuk − kesinti |

Yuvarlama Excel FIXED/VALUE davranışı için AwayFromZero'dur. İcra tutarı giriş hassasiyetinde korunur; toplam kesinti yuvarlanır. Oranlar/sabitler ortak Dönem Sabitleri panelinden gelir. Kaynak Eylül 2026 katsayısı 1,575512, gösterge 140 ve DYK çarpanı 2'dir.

## Kaynakta tespit edilen sapmalar

- `Bordro!Z8` boş: toplam kesinti kaybolduğu için `AA8` yanlışlıkla tahakkuka eşittir. Normal kesinti formülü programa uygulanır.
- `Bordro!F9` veri girişine bağlı formül yerine elle 116 yazılmıştır. Program saati öğretmenin seçilen ay kaydından alır.
- E-bildirge eksik gün satırlarında 17/26 gibi sabitler vardır. Kullanıcının önceki açıklaması geçerlidir: seçilen ayda normal işe giriş/çıkış ile kesişen dahilî takvim günleri − yukarı yuvarlanmış prim günü. Bu yan bilgi tutarlara etki etmez; geçersiz gün aralığı XML öncesi düzeltilmelidir.
- Personel Bildirimi'nde `#REF!`, eski yıl rakamları ve veri girişindeki kullanılmayan AGİ alanlarında `#NAME?` bulunur. Bunlar aktarılmaz. Mevcut doğrulanmış üç PDF ve kişisel bordro, kayıtlı hakediş sonuçlarından üretilir.
- KişiselBordro sayfasında istisna üzerindeki tutarın tamamını vergilendiren eski formüller ana bordrodan farklıdır. Programın kişisel PDF'si yeniden vergi hesaplamaz; ana bordroda kaydedilen kişi sonuçlarını gösterir.

## Doğrulama

Kaynak salt okunur açılarak Microsoft Excel CalculateFullRebuild ile sağlam 7. satırda 50 senaryo hesaplandı; anonim girdiler ve 14 sonuç `tests/Hakedis.Tests/Fixtures/din-excel-recalculated.json` içindedir. Gerçek kişi bilgisi içermez. Kaynak çalışma kitabı kaydedilmedi. Testler çekirdek bağımsızlığını, tarih kesişimlerini, modül izolasyonunu, aynı dönemde kayıt/düzenlemeyi, bozuk sonuç/kural sürümünü ve A/E/İ XML şema doğrulamasını kapsar. WPF akış testi üç PDF, kişisel PDF/Klasör ve ekran bağlamalarını ayrıca denetler.
