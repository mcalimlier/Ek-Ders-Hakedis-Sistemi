# Ayrı hesaplama çekirdekleri

Kullanıcının 23 Ekim 2026 talebi uygulanmıştır: her eğitim türü kendi ayrı hesaplama projesinde bulunacaktır.

| Modül | Proje | Durum |
|---|---|---|
| Temel Eğitim | Hakedis.TemelEgitim.Core | Kaynak Excel kuralı uygulandı |
| İmam Hatip Lisesi | Hakedis.ImamHatip.Core (gelecek) | Excel bekleniyor |
| Ortaöğretim | Hakedis.Ortaogretim.Core (gelecek) | Excel bekleniyor |
| Mesleki Eğitim | Hakedis.MeslekiEgitim.Core (gelecek) | Excel bekleniyor |
| Halk Eğitim Usta Öğreticileri | Hakedis.HalkEgitim.Core (gelecek) | Excel bekleniyor |

Contracts modül kimliği ve kayıt/çözümleme sözleşmesini barındırır. Öğretmene özgü girdiler, oranlar, yuvarlama ve sonuçlar TemelEgitim.Core içindedir; Contracts'a veya diğer çekirdeklere taşınmaz. Çekirdekler birbirine referans vermeyecektir. Benzer formüller olması aynı kuralları kullanmaları anlamına gelmez.

Öğretmenler modüller arasında paylaşılmaz. Ortak TeacherRecord tipi ModuleId ile tek modüle aittir; liste, düzenleme, silme ve hesap seçimi bu sahipliği denetler. Aynı ID/TCKN başka modüle tekrar kaydedilemez. Kalıcı dosya ve eski kayıt geçişi ayrıntıları: MODUL_OGRETMEN_KAYITLARI.md.

Arayüzün oluşturma noktasında uygulanan modüller ModuleRegistry'ye kaydedilir. Katalog seçilen modülün kayıtlı olup olmadığını gösterir. Resolve bilinmeyen/eklenmemiş modülde hata verir; Temel Eğitim'e geri dönüş yoktur. Yeni modül geldiğinde kendi girdi/parametre/sonuç tipleri, çekirdeği, görünüm modeli ve testleri eklenir. Kaydı yapılınca arayüzde etkinleştirilir.

Temel Eğitim artık toplu bordro, aylık giriş, ortak sabitler, geçmiş düzenleme/silme, PDF/yazdırma ve SGK XML akışlarını içerir. Aylık girişler ayrı JSON, kesinleşmiş hakedişler girdi/sabit/öğretmen/sonuç/kurum/personel hareketi görüntüsü olarak saklanır. Geçmiş sonuçlar güncel sabitlerle değiştirilmez. Dosya okunurken kendi kayıtlı sabitleri ve hesaplama kuralıyla tutarlılığı kontrol edilir; kaynak dosyaya yazılmaz. Hibrit iki dosyalı işlemde kalıcı günlük ve yeniden açılışta kurtarma vardır. Kayıtlar atomik değişim, yedek, dosya kilidi ve uygun işlemlerde beklenen sürüm kontrolüyle korunur.

Gösterge ortak parametreden okunur. İcra tutarı Excel gibi ham değeriyle taşınır; toplam kesinti iki haneye yuvarlanır. Parasal hesap için 50 senaryonun 14 hesap alanı Microsoft Excel ile karşılaştırılır. SGK yan bilgisi kullanıcı tarafından tarif edilen takvim/giriş/çıkış ve yukarı yuvarlanmış toplam saat/7,5 kuralıdır. Bu denetim yeni bir mevzuat veya kümülatif vergi kuralı tanımlamaz.

Yeni modül entegrasyonu yalnız katalogda etkinleştirme değildir. Mevcut PayrollRow, PaymentSnapshot, arşiv kuralı okuma/doğrulama ve PDF/XML eşlemeleri ilk çekirdeğin tiplerine bağlıdır. Yeni Excel geldiğinde ayrı Core projesiyle birlikte bu sınırlar için modüle uygun yönlendirme ve veri/çıktı uyarlayıcıları eklenecek; Temel Eğitim çekirdeği diğer modülün hesaplamasında kullanılmayacaktır. Geçmiş kayıt her zaman kendi ModuleId + RuleVersion bilgisiyle açılmalıdır. Ortak öğretmen/dönem/kimlik ve dosya güvenliği kuralları paylaşılabilir; hesaplama kuralları paylaşılamaz. Her yeni çekirdekte Excel karşılaştırması, aylık/modül ayrımı ve mevcut Temel Eğitim regresyon paketi birlikte geçmelidir.

Doğrulama: `dotnet test tests/Hakedis.Tests -c Release`.
Derleme: `dotnet build UcretliEkDers.slnx -c Release`.
EXE/ZIP: `powershell -File scripts/Publish-Windows.ps1`.
