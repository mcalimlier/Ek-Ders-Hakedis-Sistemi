# Hibrit hakediş ve Excel eşleştirmesi Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans. Steps use checkbox syntax for tracking.

**Goal:** Kullanıcının dokuz maddesini C#/WPF uygulamasında tamamlamak.
**Architecture:** Temel eğitim çekirdeği para hesabını ve açık tarih aralığına dayalı gün hesabını yapar. Modül/yıl/ay kapsamındaki aylık girdiler ve dönem sabitleri atomik JSON belgelerinde saklanır. Bağımsız seçimler aynı kayıt kataloğunu kapsam filtresiyle okur; hibrit satırlar canlı önizleme verir, oluşturma kalıcı hesap görüntüsü kaydeder.
**Tech Stack:** .NET 10, WPF, decimal, JSON, xUnit.
**Spec:** Kullanıcının 23 Ekim dokuz maddelik mesajı; docs/EXCEL_DENETIMI.md kaynak bulgularını kaydedecek.

## Global Constraints
- SQL yok; Excel/.NET kurulumu gerektirmeyen Windows x64 dağıtımı.
- Kaynak Excel başlıkları aynen, satır sonları dahil kullanılacak.
- Öğretmen yalnızca kendi modülünde; yeni kayıt yalnızca Veri Girişi'nde.
- Diğer modüllere Temel Eğitim hesapları uygulanmayacak.
- Meslek listesi verilen PDF'nin tamamı; kodlar metin olarak korunur.

## Review Focus
- Bozuk JSON'un üzerine yazılmaması ve önceki kaydın yedeği.
- Modül/dönem değişiminde eski satırların başka döneme kaydedilmemesi.
- Geçersiz hücrede eski toplamın geçerliymiş gibi gösterilmemesi.
- Prim gününün tarih aralığını aşmasında negatif eksik gün üretilmemesi.
- PDF'nin çok satırlı meslek adları ve Türkçe araması.

## Tasks
- [x] Hesap denetimi: kaynak formüller/cached sonuçlar, yuvarlama ve sınır testleri; gün hesabı testleri. TeacherPayroll.cs ve tests/PayrollTests.cs.
- [x] MonthlyPayrollStore.cs: modül/yıl/ay zarfı, tekrar açma ve bozulma testleri. PeriodWorkspace.cs: aylık kayıt, canlı satır, parametre ve oluşturma.
- [x] OccupationCatalog.cs + gömülü JSON; SearchableOccupation.xaml/cs; PDF tüm sayfalarının kod sayısı ve satır devamı kontrolü.
- [x] ShellViewModel.cs: bağımsız modül seçimleri; MainWindow.xaml: veri girişi, dönem sabitleri, hibrit tablo ve 3×2 kartlar. TeacherForm.xaml: birebir başlıklar.
- [x] Birim testleri, WPF binding/render incelemesi, kaynak Excel'in 10 satırının hücre bazında kıyası; paket, kullanım/devam notu, yerel commit.

## Decisions / progress
- Kaynak E-Bildirge H3:H16 17, H17:H22 20 sabiti kullanıyor. Bu tutarsızlık kodda kişiye/satır sırasına göre kopyalanmayacak. Açık başlangıç/bitiş tarihleri gösterilecek; kullanıcıya eksik gün dayanağı soruldu. Kaynak örnek aralığı Eylül 14–30 (17 gün); yıl eski hücreden alınmayacak.
- Mevcut liste/JSON sahiplik korumaları korunacak. Eski tek kişi taslak API'si uyumluluk için kalabilir, ekrandaki taslak düğmeleri kaldırılacak.

- Karar: Kullanıcı eksik gün kuralını sonra açıklayacak. Tarih aralığı yardımcısı etkin hesap değildir; alan ve hakediş görüntüsü açıkça eksik durumunu taşır.
- Birim testleri 68 başarılı. Gerçek Excel 50 senaryo × 14 alan eşleşti. WPF kontrolleri ve görsel inceleme geçti.
- Ayrı inceleyici aracı bulunmadığından modül/dönem kapsamı, bozuk dosya/yedek, hücre doğrulaması, parametre olay bağları ve paket içeriği yerel olarak ayrıca gözden geçirildi.

- Yayın: dist/Hakedis-win-x64-20261024-002042.zip. ZIP içeriği ve tek EXE açılışı doğrulandı; kullanıcı verisi pakete dahil değil.
