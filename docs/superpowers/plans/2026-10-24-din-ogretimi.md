# Din Öğretimi modülü uygulama planı

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Kullanıcının Din Öğretimi Excel'ini ayrı bir çekirdekle mevcut Windows uygulamasına bağlamak.

**Architecture:** Hakedis.DinOgretimi.Core yalnız Contracts'a bağımlı olacak; kendi girdi, parametre, sonuç ve takvim kurallarını içerecek. Storage içindeki açık modül adaptörü arayüzün mevcut bordro sözleşmesine dönüştürecek. Din Öğretimi hesapları Temel Eğitim hesaplayıcısına yönlendirilmeyecek. Ortak PDF ve XML biçimleyicileri hesap yapmadan seçilen çekirdeğin sonuçlarını işleyecek.

**Tech Stack:** C#, .NET 10, WPF, atomik JSON, xUnit, mevcut PDF/XSD altyapısı.

**Spec:** Kullanıcı talebi ve önceki modül ayrımı gereksinimleri. Din Öğretimi yeni ve bağımsız modüldür; mevcut İmam Hatip Lisesi enum değeri/dosyaları yeniden adlandırılmaz. Yeni enum değeri sona eklenir. Ortak dönem sabitleri ve ücretli öğretmen SGK işyeri bilgileri kullanılır. Kullanıcı istemediği için Excel'deki gerçek kişiler otomatik içe aktarılmaz.

## Global Constraints
- Gerçek Veriler dosyaları ve eski hakediş kural sürümleri korunur.
- Öğretmen, aylık giriş, arşiv, kişisel PDF ve önceki ay personel sayısı modülle sınırlıdır.
- Eksik gün kullanıcının belirlediği tarih aralığı/7,5 saat kuralıdır; tutarlara etki etmez.
- Kaynak Excel değiştirilmez; silinmiş formüller/elle yazılmış satır sapmaları kural sayılmaz.

## Review Focus
- Aynı ay iki farklı modül kaydedilebilir; aynı modülde ikinci hakediş engellenir.
- Din Öğretimi kayıtları kendi kural sürümüyle yeniden doğrulanır; bozuk tutarlar reddedilir.
- Geçmiş sabitler ve kişisel bordro arşivden okunur.
- XML yalnız eşleşen modül takvimini kullanır; A/E/İ korunur.
- Hazır olmayan diğer modüllere sessiz Temel Eğitim geri dönüşü yoktur.

## Tasks
- [x] Excel formül farklarını ve anonim karşılaştırma örneklerini çıkar; gerçek veri yedeği/hash manifesti al.
- [x] Önce başarısız çekirdek/modül izolasyonu testlerini çalıştır.
- [x] Ayrı Din Öğretimi çekirdeği, kataloğu ve hesap/takvim adaptörünü bağla.
- [x] Hakediş doğrulama, canlı tablo, arşiv, PDF, kişisel PDF ve XML yollarını modüle bağla.
- [x] Excel yeniden hesaplama karşılaştırmaları, tüm testler, WPF PDF/XML akış kontrolü ve derlemeyi tamamla.
- [x] Gerçek veri hashlerini ve kaynak Excel hashini doğrula; kullanım/devam notlarını güncelle, yerel Git kaydına hazırla.

## Sonuç ve kararlar

178 test geçti. Sağlam kaynak satırda Microsoft Excel ile 50 senaryo/14 sonuç karşılaştırıldı; yeni çekirdek assembly'sinin Temel Eğitim'e bağımlı olmadığı ayrıca test edildi. WPF eski akışları ve yeni modülün uçtan uca PDF/XML işlemlerini geçti. pypdf/lxml ile dört PDF ve özgün XSD bağımsız kontrol edildi; Poppler çıktıları incelendi. Debug/Release derlemeleri sıfır uyarı/hata. Gerçek 19 dosya ve kaynak Excel hashleri değişmedi.

Ruling: Kaynaktaki tek satırda silinmiş kesinti ve elle yazılmış saat, yeni bir hesap kuralı kabul edilmedi — sağlam satırın bordro formülleri ve kullanıcının önceki takvim açıklaması esas alındı — kaynakta görünen hatalı tekil net tutar kasıtlı olarak tekrarlanmaz.

Ruling: Ortak çıktı biçimleri mevcut ad alanında bırakıldı — geçmiş JSON sözleşmesi ve onaylanmış PDF düzeni korunur — yeni çekirdek sonuçları açık adaptörle bu biçime çevrilir; yeni çekirdekler birbirini çağırmaz.
