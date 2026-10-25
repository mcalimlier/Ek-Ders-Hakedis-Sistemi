# Halk Eğitim Merkezi Usta Öğretici

Bu modül emekli olmayan usta öğreticiler içindir. Hakedis.HalkEgitim.Core bağımsız bir projedir; yalnız Contracts'a başvurur. Diğer hesaplama çekirdeklerini çağırmaz. Emekli usta öğreticilerin farklı prim ve SGK belge türü bu modüle uygulanmaz; ayrı kurallar geldikten sonra eklenecektir.

## Kaynak ve hesap

Gönderilen dosyanın adı Ekim 2026 olmasına rağmen VERİ GİRİŞİ!E38:F38 Eylül 2026'dır. Çekirdeğin kaynak sürümü `halk-egitim.excel-2026-09.v1` olarak adlandırılır. Hakedişin gerçek ayı/yılı programdan seçilir. Kaynak dosya değiştirilmez.

Bordro!I7:AA36 satırlarının mali formülleri aynı düzeni izler:

- Gündüz birim ücret = katsayı × gösterge. DYK birim ücret = gündüz birim ücret × DYK çarpanı. Kaynak örneğinde gösterge 140, çarpan 2'dir.
- Gündüz toplamı iki ondalığa yuvarlanır; DYK çarpımı brüt toplamdan önce ayrıca yuvarlanmaz. SGK'ya esas brüt bu ikisinin iki ondalığa yuvarlanmış toplamıdır.
- İştirakçi ve işveren primleri ayrı ayrı iki ondalığa yuvarlanır. Tahakkuk = brüt + işveren primi. Vergi matrahı = tahakkuk − işveren primi − iştirakçi primi.
- Gelir ve damga istisnası düşüldükten sonra pozitif kalan matraha ilgili oran uygulanır. Vergiler ve kesinti toplamı iki ondalığa yuvarlanır. İcra girdisi kaynakta olduğu gibi korunur. Net = tahakkuk − kesinti toplamı.
- Ortak Dönem Sabitleri kullanılır; kaynak örneğinin oranları %14, %21,75, %15, %0,759'dur. Bu modül yeni bir mevzuat/vergi dilimi kuralı tanımlamaz.
- Çalışma günü = yukarı yuvarla((gündüz + DYK) / 7,5). SGK eksik günü kullanıcının daha önce tarif ettiği seçili ay ve normal işe giriş/çıkış tarihleri aralığından bulunur. SGK yan bilgi sürümü `halk-egitim.sgk-yan-bilgi.v1`'dir; bu gün bilgileri parasal tutarı değiştirmez.

Kaynakta Bordro!D8 ve Personel Bildirimi!G7 gibi bozuk #REF! bağlantıları, sabit 2025 yılı ve eski Nisan tarihli yardımcı takvim alanları vardır. Bunlar yeni çekirdeğin girdisi yapılmaz. Önceden kaldırılan sigorta no, branş, ikinci görev yeri ve ay içi ayrı giriş/çıkış alanları yeniden eklenmez.

## Uygulama ve veri

Veri Girişi'ndeki kişi ve aylık ders modül seçimlerinden Halk Eğitim Merkezi Usta Öğretici seçilir. Hakediş Hesapla'da aynı modül ile ay/yıl seçilir. Kişi kaydı, aylık giriş, arşiv, kişisel bordro ve çıktı klasörleri mevcut modül ayrımını kullanır. Tek kişi kartı ve kişi/ay için tek bordro koruması devam eder. Excel'deki gerçek personel kayıtları bu entegrasyon sırasında otomatik içe aktarılmaz.

Ekran ve bordro PDF başlığı kaynakla aynı **DERS ÜCRETİ KARŞILIĞINDA GÖREVLENDİRİLENLER BORDROSU (USTA ÖĞRETİCİ)** olur; ünvan Usta Öğretici'dir. Üç ana PDF, tek yatay A4 bordro, kişisel bordro, yazdırma, onaylı silme ve geçmiş düzenleme aynı akıştan kullanılabilir. Çıktılar arşivdeki dondurulmuş sonuçları kullanır.

Usta öğretici SGK işyeri profili `SgkXml-UstaOgreticiler.json` içinde öğretmenlerin profilinden ayrı saklanır. Kullanıcı bu modül için belge türü 13, kanun 00000 ve eksik gün nedeni 07'yi teyit etti. İşyeri unvanı ve adresi ortak olarak kullanıcıdan alındı; sicil ve diğer kişisel/kurumsal ayarlar Git'e eklenmez. XML hazırlama ekranındaki son kontrol ve onay zorunluluğu sürer.

## Doğrulama

`scripts/Verify-HalkExcel.ps1` kaynağı ayrı ve gizli bir Excel örneğinde salt okunur açar, hiçbir zaman kaydetmez. 50 senaryoyu CalculateFullRebuild ile hesaplar; sıfır, 60/27/18 saat, DYK, kesirli saat/icra, vergi eşiği ve yüksek tutarlar kapsanır. Fixture yalnız sayısal veriler içerir; gerçek ad/TC/IBAN taşımaz. Testler bütün sonuç alanlarını karşılaştırır ve çekirdeğin diğer çekirdeklere bağımlı olmadığını denetler.

Kaynak ve gerçek veriler yedek/hash ile korunur. XML özgün XSD ile, PDF'ler sayfa/metin/toplam kontrolleriyle ayrıca doğrulanır. Bu kontroller gerçek SGK sistemine dosya yükleme veya fiziksel yazıcı denemesi değildir.
