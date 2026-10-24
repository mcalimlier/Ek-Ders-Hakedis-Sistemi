# EDHS adı, görünüm ve ekran ayarları

**Kapsam:** Kullanıcının yedi maddesi: EDHS adı; önceki büyük/altın kart simgeleri; İmam Hatip seçimini kaldırma; gerçek Din Öğretimi örneklerini ekleme; sabitlerin kilitli görünümü; kalıcı pencere boyutu ayarları; daraltılmış ve satırlara bölünen hibrit başlıklar.

**Kararlar:** Kart boyutları korunur. Önceki vektör simgeler ve altın süs çizgisi kullanılır; büyük simge sağda ayrı 48px sütunda, tutarlar solda yer alır. 1280×720, 1366×768 ve 1920×1080 pencere tercihleridir; mevcut monitörün kullanılabilir WPF alanına sığdırılır. Windows masaüstü çözünürlüğü değiştirilmez. Eski modül enum anahtarı yalnız dosya uyumluluğu için saklanır; katalogda gösterilmez. Veri/kurulum proje dizini yeniden adlandırılmaz. Yeni çıktılar EDHS klasörüne, EXE EDHS adıyla üretilir. Mevcut çıktı dosyaları yerinde kalır.

- [x] Ayar saklama/yeniden okuma, geçersiz boyut, küçük ekran sığdırma ve gizlenen modül testlerini önce çalıştır.
- [x] Ortak ad sabitleri, bağımsız arayüz tercihi JSON'u ve Ayarlar görünümünü bağla.
- [x] Kart simgelerini çakışmasız ayrı sütuna al, sabitlere kilitli gri stil ekle, hibrit sütunları sıkıştır.
- [ ] Kaynak ve gerçek veriyi yedekle; Din öğretmenlerini ve kaynak ay girişlerini mevcut veriyi ezmeden ekle. Arşiv otomatik oluşturulmaz; kurum/sabit/SGK profili değişmez. Gerçek kişiler Git'e girmez.
- [x] 1280/1366/1920 WPF görüntüleri, eski akışlar, büyük tutar geometrisi, testler ve derlemeleri doğrula.
- [x] Kullanım/devam notlarını güncelle ve doğrulanmış kaynakları yerel Git kaydına hazırla.

**Kapsam açıklaması:** İki Din öğretmeni/aylık girişi eklendi; üçüncü kişi aynı TC ile Temel Eğitim'de kayıtlı. Kullanıcı bu vaka için tarihli görev geçmişi ve tek kişi/ay için tek ödeme modülü tarif etti. Saatler iki bordroya bölünmeyecek. Geçmiş/tercih ekranı tasarımı ayrı belgede; kaynak kişi henüz taşınmadı. Modüller arası ikinci bordro güvenliği uygulandı ve 185 test geçti. Elektrik kesintisi sonrası disk/veri kontrolü tamamlandı; son geniş ekran görsel testi yeniden çalıştırıldı.

**Doğrulama:** Kesinti sonrası 185 test başarılı; WPF akışları ve üç boyutun aynı üretim XAML'inden tam boyutlu görselleri başarılı. 1920×1080 görüntüler siyah/kırpılmış alan olmadan incelendi. Sonuçlar artifacts/outage-unit-check.log, outage-ui-check.log ve ui-outage-check altında. Tarihli görev geçmişi/aylık tercih akışı yeni kapsam olarak bekliyor; mevcut kullanıcı kayıtları bu iş için değiştirilmedi.
