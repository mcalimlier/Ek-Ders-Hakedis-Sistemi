# İsteğe bağlı görev geçmişi ve aylık bordro tercihi

**Goal:** Tek kişi kartını koruyarak isteğe bağlı okul/modül geçişi kaydetmek ve geçiş ayında kişinin tamamını tek modülden ödemek.

**Architecture:** TeacherRecord'a geriye uyumlu, null ise JSON'a yazılmayan görev geçmişi eklenir. Geçmiş kendi değer eşitliğine sahip immutable koleksiyon içerir; böylece mevcut beklenen-sürüm ve hibrit kayıt kontrolleri bozulmaz. Katalogda tek kişi/TC tutulur; modül listeleri bu kartın göreve uygun görünümleridir. Aylık tercih ve seçilen modülün ay toplamı mevcut iki dosyalı PayrollEditStore günlüğüyle atomik yazılır. Diğer modülün eski aylık girdisi silinmez fakat tercih sonrası ödeme listesine alınmaz. Kayıtlı bordrolar değişmez; modüller arası tek kişi/ay koruması devam eder.

**Rules:** Geçiş tarihi SGK işe giriş/çıkışı değildir. Normal kişilerde kutu kapalı ve ek alanlar görünmez. Açıldığında okul, tarih ve hesap çekirdeğini belirleyen hedef modül girilir. Aynı ay birden fazla modül görevi varsa oluşturma öncesi tek ödeme modülü ve toplam gündüz/DYK/icra teyit edilir; saatler otomatik toplanmaz. Sonraki aylarda o ayın görevi tek modülse ayrıca seçim sorulmaz. Başka modülde bordrosu oluşmuş kişi için tercih değiştirilemez; önce mevcut bordro ele alınır.

- [x] Görev geçmişi, dönem yönlendirme, tek kişi kataloğu, atomik tercih ve eski kayıt uyumluluğu için başarısız testleri çalıştır.
- [x] Sözleşmeler, doğrulama, modül/dönem görünümleri ve tercih kaydını bağla.
- [x] Öğretmen formundaki onay kutusu ve geçmiş düzenleme alanlarını ekle.
- [x] Hesapla akışındaki tercih penceresini bağla; normal öğretmen akışını koru.
- [x] Birim/WPF/JSON/PDF/XML ve elektrik kesintisi kurtarma regresyonlarını çalıştır.
- [x] Gerçek veriyi yedekle; örnek 25.09.2026 geçişini tek mevcut kişi kartına ekle, ödeme tercihini kullanıcıya bırak.
- [x] Belgeler, devam notu ve yerel Git kaydı.

Doğrulama: 205 birim testi; gerçek WPF tercih iptali/onayı ve kayıtlı tercih değişikliği; onaysız/onaylı bordro silme; tek kişi/ay koruması; PDF/XML/XSD ve veri bütünlüğü kontrolleri başarılı. Debug ve Release WPF derlemelerinde sıfır uyarı/hata. Kaydedilmiş tercihi yeniden açmak için özel satır seçildiğinde görünen Ödeme tercihi düğmesi eklendi. Günlükler artifacts/history-choice-final-* altında; gerçek veri ve test çıktıları Git dışında.
