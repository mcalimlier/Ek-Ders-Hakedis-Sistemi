# Ayrı hesaplama çekirdekleri

Kullanıcının 23 Ekim 2026 talebi uygulanmıştır: her eğitim türü kendi ayrı hesaplama projesinde bulunacaktır.

| Modül | Proje | Durum |
|---|---|---|
| Temel Eğitim | Hakedis.TemelEgitim.Core | Kaynak Excel kuralı uygulandı |
| Din Öğretimi | Hakedis.DinOgretimi.Core | Ayrı çekirdek ve Excel karşılaştırması uygulandı |
| Ortaöğretim | Hakedis.Ortaogretim.Core (gelecek) | Excel bekleniyor |
| Mesleki Eğitim | Hakedis.MeslekiEgitim.Core (gelecek) | Excel bekleniyor |
| Halk Eğitim Usta Öğreticileri | Hakedis.HalkEgitim.Core (gelecek) | Excel bekleniyor |

Contracts modül kimliği ve kayıt/çözümleme sözleşmesini barındırır. Öğretmene özgü girdiler, oranlar, yuvarlama ve sonuçlar her modülün kendi Core projesindedir; diğer çekirdeklere taşınmaz. Çekirdekler birbirine referans vermez. Benzer formüller olması aynı kuralları kullanmaları anlamına gelmez.

İmam Hatip kapsamı Din Öğretimi'ne dahildir; seçim listesinde ikinci bir İmam Hatip Lisesi seçeneği yoktur. Eski enum değeri dosya uyumluluğu için ayrılmıştır ve yeni kayıt seçimlerinde gösterilmez.

Öğretmenin kişi kartı tektir; tarihli görev geçmişi modül görünümlerini belirler. Aynı ID/TC ile ikinci kişi oluşturulmaz. Geçiş ayında kullanıcı tek ödeme modülünü seçer; o modülün çekirdeği kişinin ay toplamını hesaplar. Normal kişilerde tek modül akışı korunur. Kalıcı dosya ve eski kayıt geçişi ayrıntıları: MODUL_OGRETMEN_KAYITLARI.md.

Arayüzün oluşturma noktasında uygulanan modüller ModuleRegistry'ye kaydedilir. Katalog seçilen modülün kayıtlı olup olmadığını gösterir. Resolve bilinmeyen/eklenmemiş modülde hata verir; Temel Eğitim'e geri dönüş yoktur. Yeni modül geldiğinde kendi girdi/parametre/sonuç tipleri, çekirdeği, görünüm modeli ve testleri eklenir. Kaydı yapılınca arayüzde etkinleştirilir.

Temel Eğitim artık toplu bordro, aylık giriş, ortak sabitler, geçmiş düzenleme/silme, PDF/yazdırma ve SGK XML akışlarını içerir. Aylık girişler ayrı JSON, kesinleşmiş hakedişler girdi/sabit/öğretmen/sonuç/kurum/personel hareketi görüntüsü olarak saklanır. Geçmiş sonuçlar güncel sabitlerle değiştirilmez. Dosya okunurken kendi kayıtlı sabitleri ve hesaplama kuralıyla tutarlılığı kontrol edilir; kaynak dosyaya yazılmaz. Hibrit iki dosyalı işlemde kalıcı günlük ve yeniden açılışta kurtarma vardır. Kayıtlar atomik değişim, yedek, dosya kilidi ve uygun işlemlerde beklenen sürüm kontrolüyle korunur.

Gösterge ortak parametreden okunur. İcra tutarı Excel gibi ham değeriyle taşınır; toplam kesinti iki haneye yuvarlanır. Parasal hesap için 50 senaryonun 14 hesap alanı Microsoft Excel ile karşılaştırılır. SGK yan bilgisi kullanıcı tarafından tarif edilen takvim/giriş/çıkış ve yukarı yuvarlanmış toplam saat/7,5 kuralıdır. Bu denetim yeni bir mevzuat veya kümülatif vergi kuralı tanımlamaz.

Yeni modül entegrasyonu yalnız katalogda etkinleştirme değildir. Mevcut PayrollRow, PaymentSnapshot, arşiv kuralı okuma/doğrulama ve PDF/XML eşlemeleri ilk çekirdeğin tiplerine bağlıdır. Yeni Excel geldiğinde ayrı Core projesiyle birlikte bu sınırlar için modüle uygun yönlendirme ve veri/çıktı uyarlayıcıları eklenecek; Temel Eğitim çekirdeği diğer modülün hesaplamasında kullanılmayacaktır. Geçmiş kayıt her zaman kendi ModuleId + RuleVersion bilgisiyle açılmalıdır. Ortak öğretmen/dönem/kimlik ve dosya güvenliği kuralları paylaşılabilir; hesaplama kuralları paylaşılamaz. Her yeni çekirdekte Excel karşılaştırması, aylık/modül ayrımı ve mevcut Temel Eğitim regresyon paketi birlikte geçmelidir.

24 Ekim 2026: Din Öğretimi için bu uyarlama tamamlandı. Storage/PayrollCores katalog, kural sürümü, parasal hesap ve takvim yönlendirmesinin tek bileşim noktasıdır. DinOgretimi.Core yalnız Contracts'a başvurur; kendi DinOgretimiInput/Parameters/Result/CalendarResult tiplerini üretir. Adaptör eski ortak arşiv/çıktı biçimine açık alan eşlemesi yapar. PDF ve XML biçimleyicileri paylaşılır; XML günleri seçilen çekirdeğin takvim adaptöründen gelir. Ücretli öğretmen işyeri profili kullanıcı talebine göre ortaktır. Ayrıntılar: [Din Öğretimi](DIN_OGRETIMI.md).

25 Ekim 2026: Halk Eğitim Merkezi Usta Öğretici için Hakedis.HalkEgitim.Core eklendi. Kendi mali/gün sözleşmeleri ve sürümleriyle çalışır; diğer çekirdeklere başvurmaz. PayrollCores alanları mevcut arşiv/PDF/XML biçimine eşler. Bordro başlığı ve ünvan modüle göre seçilir; usta öğretici işyeri profili öğretmenlerden ayrı kalır. Kaynak Excel ile 50 anonim senaryo karşılaştırılır. Ayrıntılar: [Halk Eğitim](HALK_EGITIMI.md).

Doğrulama: `dotnet test tests/Hakedis.Tests -c Release`.
Derleme: `dotnet build UcretliEkDers.slnx -c Release`.
EXE/ZIP: `powershell -File scripts/Publish-Windows.ps1`.
