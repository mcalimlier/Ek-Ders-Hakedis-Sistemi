# Modüle ait öğretmen kayıtları

Onaylanan OEHS arayüzü korunur. Kullanıcı talebi: her hesaplama çekirdeğinin öğretmenleri ayrı; Temel Eğitim öğretmeni başka modülün listesinde veya hesabında yer alamaz.

- TeacherRecord modül kimliği taşır. Tipler ortaktır, kişi sahipliği ortak değildir.
- JSON katalog şeması 2: her kayıtta açık ModuleId. Tek atomik katalog, modüller arası aynı TCKN veya ID ile tekrar kaydı da tek kilit altında engeller. Read/Save/Update/Delete seçilen modülle sınırlıdır.
- Eski şema 1 kayıtları yalnızca Temel Eğitim kabul edilir. Okuma kaynak dosyayı değiştirmez. İlk değişiklikte şema 2 yazılır ve eski dosya .bak korunur. Bozuk veya belirsiz sahiplik sessizce atanmaz.
- Modül seçimi listeyi ve yeni kayıt formunu değiştirir. Eski seçim, kişi adı, ders saatleri, icra ve hesap sonucu temizlenir. Diğer çekirdekler henüz hesap yapmaz; modül kayıt sınırı şimdiden geçerlidir.
- Testler: filtreleme, yabancı kayıt kaydetme/düzenleme/silme, modüller arası TCKN/ID tekrarı, eski kayıtların taşınması ve yedek, modül değişiminde veri temizliği, mevcut hesapların korunması.
- Sonra WPF render kontrolü ve Windows EXE/ZIP yayını.
