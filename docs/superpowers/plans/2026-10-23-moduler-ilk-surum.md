# Modüler ilk sürüm uygulama planı

**Amaç:** Ayrı Temel Eğitim çekirdeğini kullanıcı tarafından seçilen modüle göre çağıran çalışan Windows başlangıç sürümü.
**Mimari:** Contracts ortak kimlik/sonuçları; TemelEgitim.Core yalnızca bu modülün kurallarını; Storage JSON dosyalarını; App WPF ekranını barındırır. Gelecek dört modül kendi ayrı Core projelerine eklenecek. Kuralı bilinmeyen modüller hesap yapamaz.
**Teknoloji:** .NET 10, C#, WPF, JSON, xUnit; SQL yok.
**Şartname:** ../../TASARIM_KARARLARI.md ve kullanıcı modül ayrımı talebi.

- [x] Contracts içinde ModuleId, CalculationRequest/Result, ICalculationModule ve güvenli kayıt/çağırma sözleşmesini tanımla.
- [x] Kaynak Excel'in anonim 10 kişi girdisini, vergi/DYK/yuvarlama sınırlarını ve yanlış modüle yönlendirmeyi test et; başarısızlığı gör.
- [x] Temel Eğitim hesabını ayrı Core projesinde uygula; aynı girdilerle sonucu doğrula. Kaynak oranları açıkça Eylül 2026 örneği olarak işaretle.
- [x] JSON taslak kaydet/aç, atomik dosya değiştirme ve sağlam yedek için test yaz; sonra Storage uygula. Açılan girdiler yeniden hesaplanır; kayıtlı tutara güvenilmez.
- [x] WPF modül seçici ve girdi/sonuç ekranını bağla. Diğer modüller henüz hazır değil mesajı verir. Eksik modülde eski sonuç kalmaz.
- [x] Test, Release derlemesi ve self-contained EXE yayını yap. Gerçek ödeme ve resmî belge akışı bu başlangıç sürümünde henüz yoktur.

İnceleme odağı: yanlış modül, negatif/kültüre bağlı sayılar, bozuk/yeni sürüm JSON, veri kaydı yarıda kesilmesi ve Excel'in ara yuvarlamaları.
