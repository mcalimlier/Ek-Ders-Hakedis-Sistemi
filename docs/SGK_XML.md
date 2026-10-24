# SGK XML çıktısı

Çıktı listesindeki **XML oluştur** düğmesi seçili kayıtlı hakedişi açar. Kurum bilgileri ve sigortalı ad/soyad ayrımları kontrol edilip onaylandıktan sonra dosya üretilir. Mevcut hakediş değişmez; açık ekranın kaynak kaydı değişmişse yeniden açılması istenir. SGK sitesine dosya gönderilmez.

Doğrulama kaynağı, kullanıcının verdiği XSD 2.1 dosyasının değiştirilmemiş gömülü kopyasıdır. Dosyanın SHA-256 özeti birim testinde sabittir. XML'de şemada bulunmayan alan, ek ad alanı veya schemaLocation üretilmez. İsteğe bağlı ve boş alanlar yazılmaz. Dosya UTF-8 olarak BOM olmadan yazılır; tutarlar noktalı ve iki ondalıklıdır.

Resmî başvuru kaynakları:

- [SGK XML şeması 2.1](https://ebildirge.sgk.gov.tr/WPEB/jsp/duyuru/meslekkod_files/AYLIKBILDIRGELER.xsd)
- [SGK'nın XML oluşturan Excel örneği](https://ebildirge.sgk.gov.tr/WPEB/jsp/duyuru/meslekkod_files/xls2xmly.xls): M/N sütunlarının GGAA başlıkları tarih biçimini doğrular; mahiyet açıklamasında A/E/T bulunur. İndirilen makrolar yalnız metin olarak incelendi, çalıştırılmadı.

| XML alanı | Kaynak |
| --- | --- |
| ISYERISICIL / KONTROLNO / ISYERIARACINO | Kullanıcının onayladığı 26 rakamın 21 + 2 + 3 ayrımı |
| ISYERIUNVAN / ISYERIADRES | XML ekranında düzenlenebilen yerel SGK işyeri bilgileri; boş veya 50 karakterden uzun olamaz |
| DONEMAY / DONEMYIL | Seçili kayıtlı hakedişin ayı ve yılı |
| BELGETURU / KANUN / BELGEMAHIYET | Kullanıcının belirlediği kodlar; şema sınırları ve A/E/T kontrolü uygulanır |
| AD / SOYAD | Kullanıcının onayladığı ayrım; her biri en fazla 18 karakter, kısaltma yapılmaz |
| TCKNO / MESLEKKOD | Hakedişte dondurulmuş öğretmen kaydı |
| PEK | Hakedişte dondurulmuş Result.Gross; mali hesap yeniden yapılmaz |
| GUN / EKSIKGUNSAYISI | Programın kullanıcı tarafından belirlenmiş SGK gün kuralı: toplam ders saati / 7,5 yukarı yuvarlama; ilgili giriş/çıkış aralığına göre eksik gün |
| GIRISGUN / CIKISGUN | Normal giriş/çıkış tarihi ilgili ay ve yıldaysa GGAA; diğer dönemlerin tarihleri yazılmaz |
| EKSIKGUNNEDENI | Önce satırdaki neden, boşsa ortak neden; eksik gün varsa zorunlu. İki hane korunur |
| ISTENCIKISNEDENI | İlgili ayda çıkış varsa kullanıcı tarafından girilen neden; uydurulmaz |
| RAPCALISTI / ILKSOYAD / PRIM_IKRAMIYE / UCRETLIIZINGUN / UIPEK | Kullanıcı gerektiğinde girer; boşsa şemaya eklenmez. İkramiye mevcut PEK içindeki tutardır, PEK'i artırmaz |

SGK bilgileri `Veriler/SgkXml-Ogretmenler.json` ve `Veriler/SgkXml-UstaOgreticiler.json` dosyalarında ayrıdır. Öğretmen modülleri ortak öğretmen işyeri profilini kullanır. Usta öğretici çekirdeği bu değişiklikte etkinleştirilmedi; onun işyeri sicili yalnız ilerisi için ayrı profilde tutulur. Gerçek siciller kaynak kodunda veya Git'te bulunmaz.

XML masaüstündeki proje klasörünün Çıktılar / modül / yıl-ay dizinine benzersiz adla yazılır. Önce bellek içinde şema doğrulanır, sonra geçici dosya tamamlanarak son adına taşınır. Doğrulama başarısızsa çıktı yazılmaz. Şema doğrulaması, SGK'nın kendi kayıtları ve iş kurallarıyla yapacağı son kontrolün yerine geçmez.

Aynı modül/yıl/ay için yeni hakediş kaydı, dönem dosyası kilidi altında denetlenir. Kilit kontrol ve kayıt bitene kadar tutulur; iki uygulama aynı anda denese de ikinci hakediş oluşmaz. Var olan kaydın düzeltilmesi Çıktı / Düzenle üzerinden sürer.
