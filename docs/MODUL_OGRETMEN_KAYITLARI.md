# Tek kişi kartı, görev geçmişi ve aylık ödeme modülü

24 Ekim 2026'da kullanıcı önceki mutlak modül ayrımı kuralını açıkladı: öğretmen farklı tarihlerde başka okul/modüle geçebilir; aynı kişi aynı ayda yalnız tek modülden ücretini almalıdır. Bu kural uygulanmıştır.

- `ogretmenler.json` tek kişi kataloğudur. ID ve dolu TC benzersizdir. Aynı kişiyi ikinci kayıtla açmak yerine mevcut kişinin isteğe bağlı okul/modül değişikliği alanı kullanılır.
- `TeacherRecord.Assignments` yoksa eski davranış korunur. Normal öğretmenlerde ek alanlar kapalıdır ve ödeme tercihi sorulmaz. Null geçmiş JSON'a yazılmaz.
- Geçmiş; ilk kayıt modülü/okulu, tarihli okul/modül geçişleri ve kişi/yıl/ay için ödeme tercihlerini taşır. Kişi ID'si değişmez. Okul geçişi normal SGK işe giriş/çıkışı değildir.
- `TeacherDirectory.Read` modülün görev geçmişi görünümlerini döndürür; iki listede görünen kişi katalogda tek kayıttır. `ReadForPeriod` ilgili ayın görevleri ve ödeme tercihiyle filtreler. Geçmiş ayın okulu sonraki okul değişikliğiyle sessizce değiştirilmez.
- Paylaşılan ad/TC/banka gibi alanlar tek kişi kartında güncellenir. Kayıtlı bordrolar dondurulmuş kişi/girdi/sabit/sonuç görüntülerini korur. Bir kişi kartını silmek bütün modül listelerinden kaldırır; arşiv bordrolarını silmez. Geçişli kişi silinirken bu durum mevcut onay mesajında açıklanır.

## Geçiş ayının bordrosu

Örnek 25.09.2026 geçişinde Eylül için Temel Eğitim Ücretli Öğretmen ve Din Öğretimi Ücretli Öğretmen adaydır. Kullanıcı ödeme modülünü seçer ve ayın toplam saatlerini teyit eder. 117 saat iki modülde kayıtlıysa otomatik 234 yapılmaz. `BillingChoiceStore`, tercih ve hedef aylık girişi aynı iki dosyalı kurtarılabilir işlemde kaydeder. Eski modüldeki aylık girdi silinmez; tercih sonrası o modülün hesap listesine alınmaz. Yalnız bir görev modülü olan sonraki aylarda ek tercih gerekmez.

Bir başka modülde aynı kişi/yıl/ay bordrosu varsa tercih değişikliği engellenir. Yeni bordro ve geçmiş düzenleme, tüm modüller arasında ID/TC + yıl/ay tekilliğini dosya kilidi altında denetler. Mevcut bordro gerektiğinde onaylı düzenleme/silme akışıyla ele alınır. XML seçilen tek bordronun kişilerini kullanır.

## Veri biçimi ve güvenlik

Yeni yazımlar şema 4'tür; eski şema 1/2/3 okunur. Şema 1'in modülsüz kayıtları Temel Eğitim Ücretli Öğretmen kabul edilir. Şema 2 ve sonrasında ModuleId zorunludur. Okuma dosyayı değiştirmez; ilk yazmada eski dosya .bak ile korunur. Şema 1/2'nin önceden kaldırılmış alanları yalnız okuma geçişinde temizlenir.

Geçmiş koleksiyonu yapısal eşitlik ve savunmalı kopyalar kullanır; tekrar JSON okuması, beklenen-sürüm ve hibrit düzenleme denetimini bozmaz. Yeni kişi/TC oluşturulmaz, görevler kronolojik ve geçerli tarihlerle doğrulanır. Tercih işlemi katalog + hedef aylık dosyayı kalıcı günlükle yazar; yarım kalan işlem açılışta tamamlanır. Başka ekranda değişen kişi veya aylık veriler eski görüntüyle ezilemez.
