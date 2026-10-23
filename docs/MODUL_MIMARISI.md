# Ayrı hesaplama çekirdekleri

Kullanıcının 23 Ekim 2026 talebi uygulanmıştır: her eğitim türü kendi ayrı hesaplama projesinde bulunacaktır.

| Modül | Proje | Durum |
|---|---|---|
| Temel Eğitim | Hakedis.TemelEgitim.Core | Kaynak Excel kuralı uygulandı |
| İmam Hatip Lisesi | Hakedis.ImamHatip.Core (gelecek) | Excel bekleniyor |
| Ortaöğretim | Hakedis.Ortaogretim.Core (gelecek) | Excel bekleniyor |
| Mesleki Eğitim | Hakedis.MeslekiEgitim.Core (gelecek) | Excel bekleniyor |
| Halk Eğitim Usta Öğreticileri | Hakedis.HalkEgitim.Core (gelecek) | Excel bekleniyor |

Contracts modül kimliği ve kayıt/çözümleme sözleşmesini barındırır. Öğretmene özgü girdiler, oranlar, yuvarlama ve sonuçlar TemelEgitim.Core içindedir; Contracts'a veya diğer çekirdeklere taşınmaz. Çekirdekler birbirine referans vermeyecektir. Benzer formüller olması aynı kuralları kullanmaları anlamına gelmez.

Öğretmenler modüller arasında paylaşılmaz. Ortak TeacherRecord tipi ModuleId ile tek modüle aittir; liste, düzenleme, silme ve hesap seçimi bu sahipliği denetler. Aynı ID/TCKN başka modüle tekrar kaydedilemez. Kalıcı dosya ve eski kayıt geçişi ayrıntıları: MODUL_OGRETMEN_KAYITLARI.md.

Arayüzün oluşturma noktasında uygulanan modüller ModuleRegistry'ye kaydedilir. Katalog seçilen modülün kayıtlı olup olmadığını gösterir. Resolve bilinmeyen/eklenmemiş modülde hata verir; Temel Eğitim'e geri dönüş yoktur. Yeni modül geldiğinde kendi girdi/parametre/sonuç tipleri, çekirdeği, görünüm modeli ve testleri eklenir. Kaydı yapılınca arayüzde etkinleştirilir.

İlk sürüm kullanıcı tarafından seçilen tek kişinin hesap çalışma alanıdır; tam toplu bordro ürünü değildir. JSON taslağı şema, modül kimliği, kural sürümü, dönem, etiket, girdiler ve parametreleri içerir. Sonuçlar dosyadan alınmaz, çekirdekte yeniden hesaplanır. Kesinleşmiş çok dosyalı dönem arşivi ve paket yedekleme sonraki adımdır; mevcut Storage yalnızca atomik taslak dosyası + sağlam önceki kopya sağlar.

Temel Eğitim kaynağından iki açık karar: gösterge hücredeki parametreden okunur (Excel'in sabit 140 bağlantı hatası taşınmaz); icra para tutarı iki haneye yuvarlanır. Prim günü kaynak gibi toplam saat/7,5 yukarı yuvarlamadır; yasal üst sınır/eksik gün ve kümülatif vergi üretim kuralı henüz eklenmemiştir.

Doğrulama: `dotnet test tests/Hakedis.Tests -c Release`.
Derleme: `dotnet build UcretliEkDers.slnx -c Release`.
EXE/ZIP: `powershell -File scripts/Publish-Windows.ps1`.
