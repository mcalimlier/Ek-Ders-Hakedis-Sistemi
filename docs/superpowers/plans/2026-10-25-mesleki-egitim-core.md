# Mesleki Eğitim Merkezi Ücretli Öğretmen planı

**Goal:** Eski Excel'in mali formüllerini bağımsız çekirdekle gelecekteki mesleki eğitim ödemelerine hazırlamak.

**Architecture:** Mevcut MeslekiEgitim kimliği korunur. Hakedis.MeslekiEgitim.Core yalnız Contracts'a başvurur; kendi mali ve SGK gün sözleşmeleri olur. PayrollCores açık alan eşlemesiyle mevcut akışa bağlar. Normal ücretli öğretmenlerin ortak dönem sabitleri ve SGK profili kullanılır; kaynakta eski katsayı yalnız anonim referans testindedir.

**Scope:** Kaynak dosya adı Ocak 2025 olsa da iç dönem Ocak 2026'dır; kaynak sürümü bu dönemle adlandırılır. Bordro mali satırları aynı formüldedir. Eski bildirge yardımcı takvimi yerine kullanıcının mevcut kapsayıcı giriş/çıkış takvim kuralı kullanılır. Kişi kaydı içe aktarılmaz, gerçek hakediş oluşturulmaz, mevcut veriler/sabitler/profiller değiştirilmez. Yeni modülün tam adı Mesleki Eğitim Merkezi Ücretli Öğretmen'dir.

- [x] Salt okunur Excel: 50 anonim senaryo, kaynağın kendi 1,387871 katsayısıyla karşılaştırma ve SHA koruması.
- [x] Başarısız test; bağımsız mali/takvim projesi, kayıt ve yönlendirme.
- [x] Güncel ortak sabitlerle aylık/hakediş/kişisel/PDF/XML akışları; eski katsayının taşınmadığını doğrulama.
- [x] Regresyon/WPF ve gerçek veri hash denetimi, Debug/Release, belgeler ve yerel commit.

Sonuç: 227 test ve tüm WPF akışları başarılı. 4 PDF, XML/özgün XSD ve 30 gerçek veri dosyası/kaynak hash denetimi geçti. Debug/Release WPF sıfır hata/uyarı. Kaynak kişi/aylık giriş içe aktarılmadı. Günlükler artifacts/mesleki-* altında; özel veriler Git dışında.
