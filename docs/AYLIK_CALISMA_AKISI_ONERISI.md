# Kesintisiz aylık hakediş akışı — tasarım önerisi

Durum: Kullanıcının 24 Ekim sorusuna yönelik öneridir. Dönem kadrosu, kesinleştirme/kilit ve arşiv ekranı henüz uygulanmadı. 1366×768 pencere düzeni ayrı olarak uygulanmıştır.

## Mevcut durum ve kapatılacak boşluk

Aylık saatler modül/yıl/ay dosyalarında bağımsızdır. Oluşturulan hakediş görüntüsü öğretmen bilgilerini, girdileri, sabitleri ve sonuçları kopyalar; sonraki kayıtlar ayrı dosya oluşturur. Ancak mevcut dönem tablosu güncel TeacherDirectory ile birleştirilir. Öğretmen silinirse geçmiş aylık girdi dosyası dursa bile o öğretmen bu çalışma tablosunda görünmez. Öğretmen bilgisi değişirse eski ayı yeniden hesaplamak güncel bilgiyi kullanır. Henüz dönem kesinleştirme/kilit, geçmiş görüntüden açan arşiv ekranı ve eksik ay takibi yoktur.

## Önerilen kayıt ayrımı

1. **Öğretmen kartı:** kimlik ve ait olduğu modül. Aynı kişi yeniden göreve gelirse kartı yeniden yaratmak yerine aynı kayıt kullanılır. Modül sahipliği kuralı korunur.
2. **Görevlendirme geçmişi:** başlangıç/ayrılış aralıkları. Ayrılış silme değildir; eski dönemlerle ilişkisi korunur. Yeniden geliş yeni görevlendirme aralığıdır.
3. **Dönem kadrosu ve ders bilgileri:** seçili modül/ay/yıl için öğretmen bilgisi kopyası, görevlendirme aralığı, o aya özel saatler, kesintiler ve açık giriş durumu. Ay açıldığında hazırlanır; sonraki öğretmen kartı değişiklikleri dönem kadrosunu sessizce değiştirmez. Taslakta açık kadro güncelleme işlemiyle yeni kişi eklenebilir.
4. **Kesinleştirilmiş hakediş:** dönem kadrosu + aylık girdiler + dönem sabitleri + hesap kural sürümü + sonuçlar. Geçmiş ekran ve çıktılar güncel katalogdan yeniden üretmek yerine bu arşivi okur.

SQL gerekmez. Mevcut JSON/atomik yazma düzeni korunabilir; veri biçimi değişirse mevcut dosyalar yedeklenerek sürümlü geçiş yapılır. Mevcut geçmiş görüntüler arşiv için kullanılabilir; geçmişi bugünkü katalogdan tahmin ederek doldurmak doğru değildir.

## Kullanıcının örneği

| Dönem | Kadro | Ders bilgisi | Geçmişe etkisi |
|---|---|---|---|
| Eylül-2026 | 8 öğretmen | Eylül'e özel girilir ve kesinleştirilir | 8 kişinin bilgisi ve sonucu arşivde kalır |
| Ekim-2026 | 4 devam eden + 6 yeni = 10 | Tümünün Ekim saati yeniden girilir | Eylül saatleri ve sonuçları değişmez |

Bu sayı, ayrılan dört kişinin Eylül sonunda ayrıldığı varsayımıyladır. Ekim içinde ayrılmışlarsa Ekim kadrosunda da yer alırlar; çalıştıkları saatleri girilir. Yeni gelenler de başlangıç tarihlerine göre dahil edilir. SGK eksik gün bilgisi ayrıca hesaplanır ve para hesabını değiştirmez.

## Ekran akışı

**Yeni dönemi hazırla → Kadroyu kontrol et → Aylık dersleri gir → Hesapla/kontrol et → Dönemi kesinleştir ve arşivle.**

- Yeni aya saatler ve icra kesintileri otomatik taşınmaz. Varsayılan durum **girilmedi** olur. Sıfır saat açıkça girilmiş ayrı bir durumdur; eksik giriş sıfır ödemeymiş gibi geçmez.
- Devam eden öğretmenler görevlendirme aralıklarına göre önerilir. Yeni öğretmen o ayın kadrosuna eklenir. Ayrılmış kişi sonraki ayın önerisinde yer almaz.
- Yeni dönemin katsayı/istisna/oranları o dönem için kontrol edilir. Önceki dönem değerlerini getirme açık işlem olabilir; geçmiş aya ait değerler sessizce doğru kabul edilmez.
- Kesinleşmiş dönem salt okunur açılır. Düzeltme gerekiyorsa gerekçeyle yeni sürüm oluşturulur; eski sürüm ve önceki çıktılar korunur. Düzeltmenin hangi aya ait olduğu açık kalır.
- Geçmiş kayda bağlı öğretmen kartı silinemez; ayrılış/pasif kayıt işlemi kullanılır. Yanlışlıkla açılmış ve hiçbir dönemde kullanılmamış kayıtlar silinebilir.
- Her modül kendi başlangıç dönemi ve son tamamlanan ayını takip eder. Önceki ay kapanmadan sonraki aya geçildiğinde eksik dönem gösterilir. Öğretmen/çalışma olmayan ay da açıkça **bu ay hakediş yok** kaydıyla kapatılır.
- Her kesinleştirmede otomatik yedek, ayrıca Veriler klasörünü başka diske/USB'ye yedekleme/geri yükleme seçeneği bulunur. Tek önceki `.bak`, uzun süreli dönem arşivinin veya farklı diskteki yedeğin yerini tutmaz.

## Geliştirme sırası ve kabul koşulları

1. Görevlendirme geçmişi ve geçmişte kullanılmış öğretmeni silmeye karşı koruma. Aynı öğretmenin ayrılıp dönmesi aynı kimlikle izlenmeli.
2. Dönem kadrosu ve yeni ay hazırlama. Eylül'deki 8 kişiden Ekim'deki 10 kişiye geçişte Eylül dosyaları değişmemeli; Ekim saatleri girilmedi durumunda açılmalı.
3. Kesinleştirme, arşivden açma ve sürümlü düzeltme. Sonradan öğretmenin okul/IBAN bilgisini değiştirmek Eylül çıktısını değiştirmemeli.
4. Eksik ay takibi ve yedekten geri yükleme. Aralık→Ocak geçişi ve çalışmasız aylar dahil, modüller birbirini etkilemeden izlenmeli.

Bu tasarım uygulanana kadar ayrılan öğretmeni eski aylarda kullandıysanız listeden silmeyin. Yeni ayı seçip ders saatlerini o aya özel kaydedin; mevcut eski hakediş görüntülerini ve Veriler klasörünü koruyun.
