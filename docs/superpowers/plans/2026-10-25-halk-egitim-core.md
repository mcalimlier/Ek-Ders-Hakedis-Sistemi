# Halk Eğitim Merkezi Usta Öğretici modülü uygulama planı

**Goal:** Gönderilen usta öğretici Excel'inin mali formüllerini bağımsız çekirdekle mevcut EDHS akışına bağlamak.

**Architecture:** Hakedis.HalkEgitim.Core kendi girdi/parametre/sonuç sözleşmesini ve SGK gün hesabını taşır; diğer çekirdekleri çağırmaz. PayrollCores yalnız sözleşmeleri eşler. Var olan modüle göre ayrılan kişi/aylık/arşiv ve SGK profil deposu korunur. PDF ve ekran bordro başlığı seçili modüle göre değişir. Mevcut ortak dönem sabitleri kullanılır; emekli hesapları bu dosyada tanımlanmadığından eklenmez.

**Tech Stack:** C# / .NET 10, WPF, mevcut JSON depoları, xUnit ve WPF akış testleri.

## Kaynak inceleme ve sınırlar

- Dosya adı Ekim 2026; VERİ GİRİŞİ!E38:F38 Eylül 2026. Program ay/yıl seçimini kullanır, dosya adından dönem çıkarmayıp kayıtları otomatik içe aktarmaz.
- Bordro!I7:AA36 mali formülleri mevcut iki çekirdekle aynı matematikte: gündüz iki basamak yuvarlanır, DYK ham çarpılır; brüt ve prim/vergi/kesinti toplamları Excel gibi yuvarlanır; gün yukarı yuvarla(toplam saat / 7,5).
- Bordro D8, Personel Bildirimi G7 gibi #REF! referansları ve sabit 2025 yılı taşınmaz. Daha önce kaldırılan sigorta/branş/ikinci görev yeri/ay içi giriş-çıkış alanları geri eklenmez. Programdaki normal giriş/çıkış, takvim ve seçili dönem kuralları korunur.
- Ortak sabitler ve eski arşivler değişmez. Modüller arası tek kişi/ay ödeme koruması devam eder. Usta öğretici SGK profili öğretmen profilinden ayrıdır; kullanıcı teyidi olmayan belge/neden kodu tahmin edilmez.
- Kaynak dosya salt okunur incelenir, SHA-256 ile korunur. Gerçek kişiler ve kaynak dökümleri Git'e alınmaz. Çekirdek fixture'ı yalnız sayısal senaryolar içerir.

## İşler

- [x] Özgün Excel'i salt okunur yeniden hesapla; sıfır, kesir, DYK, kesinti ve vergi eşiği senaryolarını anonim fixture'a al; farklı parametreyi çekirdek testinde doğrula.
- [x] Yeni modülün eksikliğini başarısız testle göster; bağımsız mali ve SGK gün çekirdeğini ekle, projeleri ve yönlendiriciyi bağla.
- [x] Modüle özgü bordro başlığını/ünvanını bağla; ortak üç PDF/kişisel PDF ve SGK profil ayrımını koru.
- [x] Kayıt, aylık giriş, tek bordro, düzenleme/silme, sabitlerin dondurulması, kişi/ay koruması, PDF/XML ve pencere akışlarını doğrula.
- [x] Gerçek veri/kaynak bütünlüğünü kontrol et; Debug/Release derle, belgeleri güncelle ve yerel Git kaydı al.

Sonuç: 213 birim testi ve tüm WPF akışları başarılı. 4 PDF ve XML özgün XSD/metin/toplam kontrollerini geçti. Debug/Release WPF derlemelerinde hata/uyarı yok. Kullanıcı emekli olmayan usta öğretici için 13 / 00000 / 07 kodlarını, işyeri unvanını ve adresini teyit etti; özel profilde yalnız bu boş alanlar tamamlandı. Eski kişi/aylık/arşiv/sabit/öğretmen SGK dosyaları ve kaynak Excel hashleri aynı. Günlükler artifacts/halk-*; gerçek kişiler ve özel ayarlar Git dışında.
