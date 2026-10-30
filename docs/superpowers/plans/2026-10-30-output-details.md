# Çıktı / giriş uygulama planı
Spec: ../specs/2026-10-30-output-details-design.md
- [x] Yeni davranışları testle tanımla ve eksiklikleri doğrula.
- [x] Modül filtreli arşiv listesi ve salt okunur detay penceresi.
- [x] Boş aylık giriş görünümü, IBAN mesajı/listesi, sayısal sıfır davranışı.
- [x] Birim + gerçek WPF akışları + arşivin değişmediğini doğrula.
- [x] Yedekle, yeni sürümü yayımla, kapalı uygulamayı güncelle, canlı dosya özetlerini karşılaştır.

Sonuç: 265 Release testi geçti. Tam WPF UI smoke tüm modüller, PDF/XML, yeni boş seçim, IBAN popup/kırmızı liste, sıfır temizleme ve Detay/Kapat akışlarıyla geçti; binding hatası yok. 14 kişi, 14 aylık giriş ve 3 eski arşiv kopyası başarıyla okundu. 70 canlı dosyanın SHA256 özeti güncelleme sonrası aynı. 1.0.5 yerinde kuruldu; önce veri ve önceki EXE yedeklendi.
