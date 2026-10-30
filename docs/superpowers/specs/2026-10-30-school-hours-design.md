# Okul değişikliğinde kurum saatleri
Onaylanan kapsam: tarih yeni okulda başlama tarihidir. Yalnız geçiş ayında önceki ve yeni kurumun gündüz saatleri ayrı girilir; boş taraf sıfırdır ve hibrit tabloda uyarılır. Toplam seçilen ödeme modülünde hesaplanır; çıktılarda ayrım/uyarı yoktur. Sonraki ay tek alan, yeni geçiş ayında yeniden iki alan kullanılır.
Yaklaşım: mevcut MonthlyInput toplamına nullable kurum saati ayrıntıları eklenir. Eski JSON okunur, yeni tarih adıyla yazılır. Mevcut modül seçimi korunur. Aynı ay birden fazla geçişte ilk kurum/sonraki kurumlar toplamı gösterilir.
