# Mahiyet alanı: kaynak kontrolü ve kullanıcı onayı

24 Ekim 2026: Kullanıcı XML ekranındaki T/TİS açıklamasını sorguladı; beklenen işlemlerin Asıl, Ek ve İptal olduğunu belirtti.

## Kullanıcının XSD'si

SgkAylikBildirgeler.xsd dosyasında 76–81. satırlar:

```xml
<xs:attribute name="BELGEMAHIYET" use="optional" default="A">
  <xs:simpleType>
    <xs:restriction base="xs:string">
      <xs:length value="1"/>
    </xs:restriction>
  </xs:simpleType>
</xs:attribute>
```

Burada kod listesi veya T'nin anlamı yoktur. Alan yalnız tek karakterle sınırlandırılmıştır. Bu şemadan İptal için I, İ veya T kullanılacağı sonucuna varılamaz.

## T nereden alındı?

[SGK'nın eski XML hazırlama Excel örneği](https://ebildirge.sgk.gov.tr/WPEB/jsp/duyuru/meslekkod_files/xls2xmly.xls), yerel inceleme kopyasında artifacts/sgk-reference/sgk-resmi-xml-ornek.xls dosyasıdır. Makro çalıştırılmadan Module1.bas metni incelendi.

Input_Check, Bildirge sayfasının E10 hücresinde A, E veya T bulunmasını kontrol eder. Aynı makronun hata metni A'yı Asıl, E'yi Ek olarak açıklar; T'yi açıklamaz. XML_Hazirla ise E10 değerini dönüştürmeden BORDRO öğesinin BELGEMAHIYET niteliğine yazar.

Dolayısıyla T kodunun kaynağı bu eski Excel'dir; kullanıcının yüklediği XSD değildir. T'ye “Toplu İş Sözleşmesi” anlamı vermek için doğrulanmış kanıt bulunmadı. Yanlış TİS etiketi ve açıklaması kaldırıldı.

## İptal

[SGK e-Bildirge kılavuzu](https://ebildirge.sgk.gov.tr/WPEB/HTML/e-bildirge-klavuzV1.2.pdf) Asıl/Ek/İptal işlemlerini listeler. Erişilen kaynaklar İptal'in bu XML sürümündeki tek karakterlik kodunu açıklamadığından kullanıcıya soruldu; personel veya işyeri bilgisi istenmedi.

Kullanıcı açıkça A=Asıl, E=Ek, İ=İptal olarak yanıtladı. Bu doğrudan kullanıcı onayına göre arayüz ve XML doğrulaması düzeltildi. İptal kodu **Türkçe büyük İ (U+0130)** harfidir. Örnek BORDRO satırı:

```xml
<BORDRO DONEMAY="10" DONEMYIL="2026" BELGEMAHIYET="İ" />
```

Ekranda A — Asıl / E — Ek / İ — İptal gösterilir; XML'e yalnız kod yazılır. T ve noktasız I geçersizdir. Eski kayıttaki T otomatik olarak İptal'e çevrilmez; seçim boş açılır ve yeniden seçilmelidir. Mevcut profil dosyaları yalnız pencere açılmasıyla değiştirilmez. XSD dosyasına dokunulmadı.

Testler A/E/İ kodlarının XML'de birebir korunmasını ve özgün XSD'den geçmesini, T/I'nin reddini ve eski geçersiz profilin yeni seçim gerektirmesini kontrol eder. WPF akış testi İ — İptal seçip oluşan XML'in BELGEMAHIYET değerini ayrıca denetler. Gerçek SGK yüklemesi yapılmadı.
