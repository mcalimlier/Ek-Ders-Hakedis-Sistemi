# Tarihli öğretmen görev geçmişi — öneri

Kullanıcı 24 Ekim'de önceki mutlak modül ayrımı kuralını açıkladı: kişi farklı tarihlerde Temel Eğitim ve Din Öğretimi okullarında çalışabilir. Aynı kişinin okul/modül değiştirmesi yeni bir kişi olarak kaydedilmemelidir. Bu belge tasarımdır; mevcut sürümde geçiş işlemi henüz uygulanmış değildir.

1. **Kişi kartı:** TC, ad soyad ve banka bilgileri tek kişiye aittir. TC benzersizliği kişi düzeyinde korunur.
2. **Görevlendirmeler:** kişi, modül, okul, başlangıç ve bitiş tarihlerini taşır. Her görevin kendi kimliği vardır. Eski görevin modülü değiştirilmez; eski görev kapatılıp yeni tarihli görev açılır.
3. **Görev/Modül Değiştir:** Öğretmen Listesi'nden kişi seçilince eski görev bitişi, yeni görev başlangıcı, yeni modül ve okul girilir. Bir özet gösterilerek onaylanır; kaydetme tek kurtarılabilir işlem olmalıdır. Aynı kişinin farklı modüllerde çakışan görev tarihleri açıklığa kavuşmadan kabul edilmemelidir.
4. **Aylık ödeme tercihi:** kullanıcı 25.09.2026 örneğiyle kuralı netleştirdi: geçiş ayında kişi iki modül bordrosuna bölünmez. Kullanıcı Temel Eğitim veya Din Öğretimi'ni seçer; kişinin ay toplamı yalnız bu modülün çekirdeğinde hesaplanır. Tercih kişi + yıl + ay için saklanır. Önceden girilmiş saatler körlemesine toplanmaz; ay toplamı kullanıcıya teyit ettirilir.
5. **Geçmiş:** kaydedilmiş hakediş kişi/okul/tarih/hesap görüntüsünü korur. Görev geçişi eski hakedişi değiştirmez. Önceden hesaplanmış ayda düzeltme gerekiyorsa mevcut onaylı geçmiş düzenleme yolu kullanılır.
6. **Tek bordro denetimi:** kişi + yıl + ay için modüller arasında ikinci hakediş engellenir. Geçmiş düzenleme de başka bordroyla çakışamaz. Tercih edilen modülde bordro oluşturulduktan sonra tercih değişikliği sessizce yapılamaz; önce eski bordro onaylı düzenleme/silme akışıyla ele alınmalıdır. XML tek kişiyi tek bordrodan alır.

Okul/modül geçiş tarihi SGK işe giriş veya işten çıkış tarihi değildir; normal sigortalılık tarihleri kullanıcı ayrıca değiştirmedikçe korunur. Kişinin ay toplamı ve eksik gün bilgisi okul transferi nedeniyle ikiye bölünmez. Bu yazılım kuralı kullanıcının açık tarifidir; dışarıdan bir SGK mevzuat kuralı varsayılmamıştır.

Önerilen geçişte mevcut öğretmen ID'leri ve eski JSON arşivleri korunur; yeni kişi/görev depolarına atomik, yedekli geçiş yapılır. Yeni depo olmadan TeacherDirectory'nin TC kontrolünü kaldırmak, görevi tarihsiz başka modüle taşımak veya eski kaydı silip yenisini açmak uygulanmaz.

Kullanıcı örnek geçiş tarihi olarak 25.09.2026'yı belirtti. Tarihli görev geçmişi ve aylık ödeme modülü tercih ekranları henüz uygulanmadı; mevcut Temel Eğitim kişi kaydı korunuyor. Diğer iki Din Öğretimi kaydı ve Eylül 2026 aylık girişleri eklendi. Tek kişi/yıl/ay için ikinci bordroyu engelleyen kayıt ve geçmiş düzenleme kontrolü mevcut sürüme eklendi. Gerçek kişi isimleri/TC'ler bu tasarım belgesine veya Git'e eklenmez.
