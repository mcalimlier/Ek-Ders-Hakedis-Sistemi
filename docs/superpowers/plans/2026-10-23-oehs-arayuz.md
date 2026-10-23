# OEHS arayüzü ve kayıt akışı

Kullanıcının seçime bıraktığı iki referanstan OEHS'nin mevcut dalı esas alınır. C# / WPF korunur. App.xaml kaynakları ayrı ResourceDictionary'ye taşınır: ModernButton, SecondaryButton, DangerButton, NavButton, Card, CardTitle, FieldLabel, FieldTextBox, FieldComboBox ve ModernDataGrid. Sol menü, üst durum şeridi, iki sütunlu kartlar ve Türkçe işlem metinleri bu görsel dilde yeniden kurulur.

1. Yeni kayıt yalnızca Veri Girişi'ndeki bağımsız kayıt formu üzerinden yapılır.
2. Öğretmen listesi seçili mevcut kaydı düzenler/siler; seçim yokken kayıt ekleme ve kaydetme engellenir. Depoda Update mevcut olmayan ID'yi oluşturmaz.
3. Kayıt formu başarılı kayıttan sonra temizlenir, liste yenilenir, kişi puantaj alanında seçilir.
4. Testler: listeden ekleme engeli, yeni kaydın listeye ve hesaba aktarımı, mevcut kayıt düzenleme, silinen kaydı yeniden yaratma engeli, kaynak hesapların korunması.
5. Release derleme, WPF kaynak yükleme/görsel kontrol ve yeni self-contained Windows paketi. Gerçek kullanıcı verileri pakete eklenmez.
