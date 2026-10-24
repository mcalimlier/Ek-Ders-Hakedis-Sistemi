# Kayıtlı hakedişi silme ve tek kayıtla düzenleme

1. Çıktı satırına Düzenle ile XML oluştur arasına aynı ölçüde Sil ekle. Varsayılan Hayır olan onayda dönem/modül ve kişi sayısını göster.
2. Silme yalnız seçili hakedişi kaldırır; öğretmen/aylık giriş dosyaları değişmez. Dönem ve kayıt yazma kilidi altında sürüm denetlenir. Kurtarma kopyası listelenmeyen SilinenHakedisler klasörüne taşınır; kaynak hakediş yolu kalkar.
3. Düzenleme zaten aynı dosyayı atomik değiştirir. Onay metnini eski kaydın yenisiyle değişeceği ve listede tek satır kalacağı şekilde açıklaştır. Kurtarma yedeği listelenmez.
4. Onaysız işlem, eski sürüm çakışması, diğer kayıtların korunması ve aynı dönemin tekrar oluşturulabilmesini test et. WPF'te beş eşit düğme, onay/iptal ve düzenlemeden sonra tek satır akışını doğrula. Gerçek verilerde silme testi yapma.
