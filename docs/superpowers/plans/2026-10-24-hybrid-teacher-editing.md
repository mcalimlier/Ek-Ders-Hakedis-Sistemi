# Hibrit öğretmen düzenleme uygulama planı

> **For agentic workers:** Use superpowers:executing-plans; kullanıcı uygulama değişikliklerini doğrudan istedi.

**Goal:** Kaldırılan dört alan olmadan Excel veri giriş tablosu ve güvenli iki alternatif düzenleme akışı.

**Architecture:** TeacherRecord sadeleştirilir; eski kataloglar yalnız okuma uyumluluğu üzerinden yeni şemaya geçer. PayrollRow kayıtlı değerleri ve geçici değişiklikleri ayırır. Öğretmen ve aylık girdiler önceden doğrulanarak kurtarılabilir dosya işlemiyle kaydedilir. Form düzenlemesi ayrı kopyada tutulur.

**Tech Stack:** C#/.NET 10, WPF, JSON; SQL yok.

**Spec:** Kullanıcının 24.10.2026 tarihli beş maddelik isteği.

## Global Constraints
- Excel başlıkları birebir; dört kaldırılmış alan hariç yalnız VERİ GİRİŞİ sütunları.
- Öğretmen kimliği ve modülü değişmez; eski hakedişler yeniden yazılmaz.
- Gerçek veriler Git ve dağıtım paketine girmez.
- Seçim yokken Sil/Düzenle pasif; kayıt oluşturma yalnız Veri Girişi.

## Review Focus
- Bozuk veya eski katalog: veri kaybetmeden okuma, eski alanları yeni kayıttan çıkarma.
- Tek hatalı satır: bütün toplu işlemi yazmadan reddetme.
- Eşzamanlı değişmiş kayıt: eski ekranın yeni kaydı ezmesini reddetme.
- Vazgeç: önceden var olan tablo taslaklarını koruma.
- Form kaydı: diğer ekranlar güncel, aylık taslaklar ve arşivler korunmuş.

### Task 1: Model ve uyumluluk
- [x] Model alanları ve eski JSON geçişi için başarısız test yaz/çalıştır.
- [x] TeacherRecord, TeacherEditor, TeacherDirectory ve formlardan dört alanı kaldır; şema 3 yaz.
- [x] Katalog testlerini çalıştır.

### Task 2: Geçici değişiklikler ve kayıt
- [x] Hibrit değişiklik takibi, iptal, toplu doğrulama, çakışma testleri yaz/çalıştır.
- [x] PayrollRow.HasChanges/AcceptChanges, PeriodWorkspace.SaveHybridChanges, ShellViewModel.BeginHybridTeacherEdit/SaveTeacherEdit/CancelTeacherEdit ekle.
- [x] Öğretmen ve aylık kayıt için geri alınabilir günlük dosyasıyla işlem ekle; başlangıçta yarım işlemi kurtar.
- [x] Tüm birim testlerini çalıştır.

### Task 3: WPF ve uçtan uca doğrulama
- [x] HybridGrid sütunlarını kaynak sırasına getir, satır 30, hücreleri ortala.
- [x] Seçim düğmeleri ve form moduna göre Kaydet/Vazgeç metinleri; kayıt sonrası Tamam popup.
- [x] Hücre düzenleme ve Değişiklikleri kaydet düğmesini bağla.
- [x] UI smoke: seçim, iptal, kaydet, kolonlar, ortalama, canlı toplamlar ve kalıcılık.
- [x] Gerçek verileri yedekle; build, test, görsel kontrol; devam notunu güncelle.

## Uygulama kaydı
- Referans: OEHS feature/official-payment-document-templates / MainWindow.HybridSourceUpdate.cs ve HybridRow.cs.
- 102 birim testi geçti; 50 Excel senaryosu / 14 hesap alanı karşılaştırması korunuyor.
- WPF 8 panel görünümü, meslek araması, hücre düzenleme, seçim düğmeleri, form iptal/kayıt ve toplu kayıt doğrulandı; binding hatası yok.
- Native yürütme; ayrı inceleyici/subagent aracı yok. Manuel diff incelemesi ve hata senaryoları tamamlandı.
- Karar: Sil, öğretmen ana kaydını onayla siler; geçmiş aylık kayıtlar ve hakediş görüntüleri korunur. Diğer satırlardaki geçici girişler kalır.
- Karar: Formda öğretmen bilgileri ayrı kopyadadır; aylık kart bu sırada pasiftir. Mevcut tablo taslağı iptalden etkilenmez.
- Eski alan isimleri yalnız şema 1/2 okuma geçişinde ve geçiş testlerinde kalır; aktif model/UI/yeni JSON'da yoktur.
