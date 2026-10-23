# OEHS arayüzü ve tek kayıt girişi

Kullanıcının 23 Ekim 2026 arayüz düzeltmesi: C# / WPF korunur; referans olarak Özel Eğitim Hakediş Sistemi'nin feature/official-payment-document-templates dalı seçilmiştir.

Kaynak: https://github.com/mcalimlier/OzelEgitimHakedisSistemi/tree/feature/official-payment-document-templates
İncelenen kaynak commit: 284e66bf221d20b7d51becfdedba616a3673014c.

`Themes/Oehs.xaml` referans uygulamanın App.xaml kaynaklarını içerir. ModernButton, SecondaryButton, DangerButton, NavButton, Card, CardTitle, FieldLabel, FieldTextBox, FieldComboBox, FieldDatePicker, MetricCard ve ModernDataGrid aynı WPF şablonlarıyla kullanılır. Workspace.xaml yalnızca bu uygulamanın form boşlukları ve ölçülerini ekler. Lacivert/mavi/altın renk sistemi, Segoe UI, yuvarlatılmış düğmeler, aktif menü altın çizgisi, kart geçişleri ve üst durum şeridi esas alınmıştır.

Veri Girişi: üstte modül ve ödeme dönemi; solda yeni öğretmen kayıt kartı; sağda aylık ders bilgileri ve dönem parametreleri. Yeni kayıt sadece buradan kaydedilir, form temizlenir ve kayıtlı kişi hesaplama alanında seçilir.

Ücretli Öğretmen Listesi: kayıtlı liste, seçili kişiyi veri girişine alma, silme ve mevcut bilgileri düzenleme. Yeni kayıt düğmesi yoktur; seçim yapılmadan düzenleme kaydedilemez. Depo Update işlemi bulunmayan ID'yi yeniden yaratmaz.

Hakediş Hesapla: öğretmen özeti, brüt/tahakkuk/net tutar kartları ve hesap dökümü. Temel Eğitim hesap çekirdeği değiştirilmemiştir.

WPF görsel kontrol aracı: `dotnet run --project tests/Hakedis.UiSmoke -c Release -- artifacts/ui-review`. Yalnızca sentetik öğretmen bilgisi kullanır; veri dosyaları ve ekran görselleri git dışında artifacts/ altında kalır. 1440 ve 1040 piksel genişlikte üç paneli üretir; binding hatası varsa hata koduyla çıkar.
