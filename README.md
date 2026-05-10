# Library-Automation
It is developed with SQL(SSMS) and C# [windows forms app (.NET Framework)].
Üniversite Kütüphane Otomasyonu 
Bu proje, bir üniversite kütüphanesinin envanter yönetimini, üye takibini ve ödünç alma süreçlerini dijitalleştirmek amacıyla geliştirilmiş, C# ve SQL Server tabanlı bir masaüstü uygulamasıdır.

 Öne Çıkan Özellikler
Hiyerarşik Yetkilendirme: Kurucu, Admin ve Standart Üye (Öğrenci/Akademisyen) rolleri.

Güvenli Silme (Soft Delete): Veritabanı bütünlüğünü korumak adına kullanıcılar ve kitaplar fiziksel olarak silinmez, "Pasif" durumuna çekilerek geçmiş veriler korunur.

Dinamik Ceza Sistemi: SQL Trigger mekanizması ile teslim tarihi saniye bazlı kontrol edilir ve gecikmelere anlık ceza tahakkuk ettirilir.

Gerçekçi Envanter Görünümü: Öğrenciler stok adedini sayısal olarak değil, kütüphane mantığına uygun şekilde "Rafta" veya "Ödünç Verildi" olarak görür.

Akıllı Bildirimler: Üyeler giriş yaptıklarında onaylanan kitap talepleri veya rezerve ettikleri kitapların stok durumları hakkında otomatik bilgi alırlar.

 Kullanılan Teknolojiler
Dil: C# (.NET Framework)

Veritabanı: Microsoft SQL Server (RDBMS)

Veri Erişimi: ADO.NET (Parametrik sorgu yönetimi)

Tasarım: Windows Forms

 Veritabanı Yapısı
Sistem KutuphaneDB_Final isimli ilişkisel veritabanı üzerinde çalışmaktadır.

Stored Procedures: Veri güvenliği ve performans için tüm INSERT/UPDATE işlemleri prosedürler üzerinden yürütülür.

Triggers: İade işlemleri sırasında ceza hesaplaması arka planda otomatik yapılır.

 Kurulum
SQL Server üzerinde projedeki Kutuphane_SQL_Final.sql dosyasını çalıştırarak veritabanını oluşturun.

Visual Studio üzerinden projeyi açın.

Form1.cs dosyasındaki connectionString alanını kendi yerel SQL sunucunuzun adresine göre güncelleyin.

Projeyi derleyin ve çalıştırın.
