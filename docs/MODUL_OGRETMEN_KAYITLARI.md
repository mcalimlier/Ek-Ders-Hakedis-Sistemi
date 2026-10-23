# Öğretmenlerin modül sahipliği

Kullanıcının 23 Ekim 2026 kararı: arayüz korunacak; her hesaplama çekirdeğinin öğretmenleri ayrı olacaktır. Temel Eğitim öğretmeni Ortaöğretim veya başka modülde seçilemez.

Her TeacherRecord kalıcı ModuleId taşır. Veri tipinin Contracts içinde olması, öğretmenin modüller arasında ortak olduğu anlamına gelmez. ModuleId formdan değiştirilemez. TeacherDirectory işlemleri seçili modül kapsamındadır; Read sadece o modülün kişilerini verir, Save/Update yabancı kaydı reddeder, Delete yalnızca aynı modülün ID'sini silebilir. Aynı kayıt ID'si veya dolu TCKN başka modüle tekrar kaydedilemez. Ad soyad tek başına kişi kimliği kabul edilmez; aynı isimli farklı kişiler olabilir. TCKN boşsa gerçek kişi eşitliğini isimden tahmin etmeyiz; kayıt ID'sinin sahipliği yine korunur.

Veriler/ogretmenler.json şema 2 kullanır. Her kayıtta ModuleId açıkça yazılıdır. Modüller arasında TCKN tekilliğini ve güncelleme bütünlüğünü tek işlemde denetlemek için katalog ortak bir dosyada ve ortak kilit altında tutulur; kullanıcıya verilen listeler ve değişiklik yetkisi modüle göre ayrıdır. Başka modülün kayıtları düzenleme/silme sırasında korunur. Şema 2'de ModuleId eksikse veri bozuk kabul edilir; varsayılan modüle sessizce atanmaz.

Eski şema 1 dosyası modül bilgisi taşımıyordu; ilk sürümün Temel Eğitim kapsamına göre eski kayıtlar geriye uyum için Temel Eğitim sayılır. Okuma dosyayı değiştirmez. İlk başarılı değişiklikte şema 2 yazılır ve orijinal dosya .bak olarak korunur. Diğer modüller bu eski kişileri göremez. Bu kural uygulama içindeki geçiş davranışıdır; geliştirme sırasında kullanıcı dosyaları topluca dönüştürülmez.

Modül seçimi değiştiğinde öğretmen listesi yenilenir; seçili öğretmen, kişi adı, gündüz/DYK saatleri, icra, yeni kayıt formu ve hesap sonucu temizlenir. Aynı modülü yeniden seçmek girdileri silmez. Yeni kayıt sadece Veri Girişi'nden seçili modüle yapılır. Liste ekranı mevcut kayıt düzenleme/silme içindir. Henüz eklenmemiş çekirdekler hesap yapmaz; öğretmen verilerinin sahipliği şimdiden bu modüllere göre tutulabilir.

Otomatik testler modül filtrelemesi, yabancı kayıtla işlem engelleri, TCKN/ID tekrar engeli, eski dosya geçişi/yedeği ve modül değişimindeki temizliği doğrular. WPF kontrol aracı, açık ekran üzerinde modül değiştirerek başka modülün kişisinin görünmediğini ve veri bağlarının hatasız kaldığını kontrol eder.


## 24 Ekim güncellemesi

Yeni ekranlarda öğretmen kaydı, aylık giriş, öğretmen listesi ve hakediş modül seçimleri bağımsızdır. Aylık kayıtlar ayrıca modül/yıl/ay kapsamında ayrılır. Eski tek modül seçiminin bütün ekranları değiştirdiği akış yerine bu seçimler kullanılır. Ayrıntılar EXCEL_DENETIMI.md içindedir.
