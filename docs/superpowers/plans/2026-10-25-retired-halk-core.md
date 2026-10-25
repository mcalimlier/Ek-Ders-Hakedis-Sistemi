# Emekli Halk Eğitim Usta Öğretici uygulama planı

**Goal:** Emekli usta öğretici Excel'ini ayrı çekirdekle EDHS'ye bağlamak; normal personelin prim ve kayıtlarına dokunmamak.

**Architecture:** Enum sonuna yeni modül eklenir; bağımsız Hakedis.EmekliHalkEgitim.Core yalnız Contracts'a başvurur. Kendi mali ve SGK takvim sözleşmeleri vardır. Emekli dönem sabitleri ayrı JSON/kartta tutulur ve yalnız ilgili aktif çalışma alanına bağlanır. Arşiv sabitleri dondurulur. SGK profili emekli usta öğreticilere ayrılır; kullanıcı tarafından teyit edilen belge türü 02'dir. İşyerinin ortak unvan/adres/sicili normal usta profilinden okunabilir, belge türü ve isim tercihleri ayrıdır.

**Tech Stack:** .NET 10, C#, WPF, mevcut JSON depoları, xUnit/WPF testleri, salt okunur Excel doğrulaması.

## Kaynak ve kurallar

- Emekli iştirakçi oranı %7,5, işveren %24,75. Diğer mali adımlar kaynak Excel'in iki basamak yuvarlama sırasını izler; oranlar kendi kartından değiştirilebilir.
- Normal personelin mevcut dönem sabitleri aynı kalır; emeklinin bütün sabitleri ayrı kartta gösterilir. Güncelle/kaydet, gri/beyaz kilit ve eski arşiv koruması aynıdır.
- PDF prim başlıkları ve ünvan yeni modüle ve kullanılan oranlara uygun olmalıdır. Eski modüllerin kaynak başlıkları korunmalıdır.
- SGK belge türü 02; kullanıcı önceki unvan/adres/kanun/eksik gün kodlarının usta grubunda ortak olduğunu teyit etti. Gerçek işyeri bilgileri kod/depo içine alınmaz. XML özgün XSD'yi kullanır.
- Kaynak ve gerçek veriler korunur; uygulama açılışında otomatik kişi içe aktarılmaz. Geliştirme sırasındaki test kişisi özel yerel veride tutulur, dağıtıma/Git'e alınmaz. Kaldırılan personel alanları geri eklenmez. Tek kişi/ay bordro denetimi devam eder.

## İşler

- [x] Kaynağı salt okunur yeniden hesapla; 50 anonim senaryoyu fixture'a al ve modülün eksikliğini başarısız testle göster.
- [x] Bağımsız çekirdek, takvim, katalog ve yönlendirmeyi ekle; yeni emekli sabit deposunu bağla.
- [x] Ayrı sabit kartı ve aktif çalışma yönlendirmesi; arşiv dondurma, taslak ve kapanış korumasını doğrula.
- [x] PDF başlık/prim oranları ve ayrı SGK 02 profili; aylık/kişisel/çıktı akışlarını doğrula.
- [x] Tüm testler, WPF, PDF/XML, gerçek veri ve kaynak bütünlüğü, Debug/Release derlemeleri; belgeler ve yerel commit.

Sonuç: 221 birim testi ve tüm WPF akışları başarılı; 4 PDF ve XML özgün XSD doğrulandı. Kullanıcı başlıktaki Eylül dönemini ve SGK belge 02'yi teyit etti. Tek test kişisi/12 saat eklendi; hatalı kaynak IBAN kullanıcı isteğiyle boş bırakıldı. Önceki 16 kişi ve eski dosyalar/sabitler/profiller korundu. Debug/Release WPF sıfır hata/uyarı. Günlükler artifacts/retired-* altında.
