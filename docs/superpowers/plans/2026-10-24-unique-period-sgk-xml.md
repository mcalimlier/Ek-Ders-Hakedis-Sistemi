# Tek dönem kaydı, çıktı düğmeleri, altın detaylı kartlar ve SGK XML

Kullanıcının dört isteği uygulanır. Mali sonuçlar ve önceki kayıtlar değiştirilmez.

1. Aynı modül/yıl/ay için kayıt dosyası kilidi edin; mevcut hakedişi denetle; aylık dosya yazımından önce reddet. Kontrol ve kayıt aynı kilit boyunca yapılır. İkinci süreç ve farklı ay/modül sınırları test edilir.
2. Çıktı satırında PDF, Yazdır, Düzenle, XML oluştur eşit boyutta düğmeler. Dar ekranda yatay kaydırma korunur. Kartlar mevcut boyutta kalır; lacivert geçiş, altın ince kenar/ışık ve ayrı vektör simgeler kullanılır.
3. Kullanıcının verdiği XSD 2.1 dosyasını aynen kaynak olarak göm. SGK XML oluşturucusu yalnız tanımlı element/attribute üretir ve bu XSD ile doğrulamadan dosya yazmaz. UTF-8, noktalı ondalık, doğru dönem, kayıtlı Gross, SGK gün bilgisi, normal giriş/çıkış ve meslek kodu kullanılır. Şema uyumu SGK iş kurallarınca kabul garantisi değildir.
4. SGK işyeri profilleri ücretli öğretmenler ve ilerideki usta öğreticiler için ayrı yerel JSON dosyalarında saklanır. Sicil/kontrol/aracı ayrımı kullanıcıdan onaylandı. Eksik kurum ve belge alanları ilk XML ekranında sorulur. Kişi adı/soyadı ayrımı ve eksik gün/çıkış nedenleri açıkça gözden geçirilir; belirsiz değerler uydurulmaz. İptal edilirse XML yazılmaz.
5. XML doğrulama, alan/sıra/tutar ve hatalı girdi testleri; tekrar kayıt yarış testi; WPF akış, boyut ve düğme ölçüsü testleri. Gerçek verileri yedekle, gerçek SGK bilgilerini kaynak koduna veya Git'e koyma. SGK sitesine yükleme yapma.
