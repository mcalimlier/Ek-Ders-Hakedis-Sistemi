# Referans çıktılar ve kayıt sonrası akış

Kullanıcının on maddelik isteği uygulanır. Mevcut mali çekirdek ve kayıtlı mali sonuçlar korunur.

1. Personel hareketini normal giriş/çıkış tarihlerinden hesapla; önceki ay sayısını aynı modülün önceki ay son kaydından al. Yeni sayım kuralını sürümle; eski kayıtlarda PDF hazırlanırken hareket bilgisini bellekte tamamla, dosyayı değiştirme. Tarih, yıl sınırı ve modül ayrımı testleri ekle.
2. Hakediş oluşturma başarı bildirimi ve Çıktı yönlendirmesini pencere olayına ekle. Çıktı hücrelerini ortala. Kurum ve İmza Bilgileri panelini iki menü arasına yerleştir. Altı kartın dış düzenini koru; iç bilgileri birleştir ve ödenekleri ekle.
3. Çıktı sıra etiketlerini kaldır. Banka başlığını üç etiket/değer satırı olarak çiz. Bordroyu tek yatay A4 üzerinde bütün verileri içeren iki kompakt bölümde yerleştir; okunaklı alt sınırın altında çıktı üretmek yerine açık hata ver.
4. Personel bildirimini referansın koordinatlarına göre yeniden çiz: yıl rakamları ayrı hücreler, I/II tablolar, altı boş hastalık satırı, imzalar ve dipnotlar. Referanstaki hastalık beyanını kullanıcı tarafından değiştirilebilir tut.
5. Birim testleri, WPF akış testi, PDF ve yazıcı paginator kontrolü; PDF'leri Poppler ile görüntüleyip kontrol et. Gerçek Veriler klasörünü önce yedekle; sentetik testleri ayrı klasörlerde yap.
