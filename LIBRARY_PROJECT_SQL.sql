/*========================================================================================
   PROJE ADI: ÜNİVERSİTE KÜTÜPHANE OTOMASYONU (FİNAL SÜRÜMÜ)
   GELİŞTİRİCİ: (Kendi Adınızı Yazın)
   AÇIKLAMA: Bu SQL betiği, yepyeni bir veritabanı (KutuphaneDB_Final) oluşturarak 
             tüm tabloları, kısıtlamaları ve otomasyon kurallarını sıfırdan kurar.
========================================================================================*/

CREATE DATABASE KutuphaneDB_Final;
GO

USE KutuphaneDB_Final;
GO

-- Performans ve tutarlılık için otomatik artan kimlik (Identity) numaralarının atlamasını devre dışı bırakır
ALTER DATABASE SCOPED CONFIGURATION SET IDENTITY_CACHE = OFF;
GO

/*========================================================================================
   1. VERİTABANI TABLOLARI VE İLİŞKİLERİ (DDL)
========================================================================================*/

-- Sistemdeki hiyerarşik rolleri ve bu rollere ait iade esnekliklerini tutan tablo
CREATE TABLE Roller(
    RolID INT PRIMARY KEY,
    RolAdi VARCHAR(50) NOT NULL UNIQUE,
    IadeSuresiGun INT 
);

-- Sisteme kayıtlı olan tüm öğrencileri, akademisyenleri ve yöneticileri tutan tablo
CREATE TABLE Kullanicilar(
    KullaniciID INT IDENTITY(1,1) PRIMARY KEY,
    AdSoyad VARCHAR(100) NOT NULL,
    KullaniciNo VARCHAR(50) NOT NULL UNIQUE, -- Öğrenci Numarası veya Akademisyen Sicil Numarası
    Email VARCHAR(100) NOT NULL UNIQUE,
    Sifre VARCHAR(100) NOT NULL,
    RolID INT NOT NULL,
    KayitTarihi DATE DEFAULT GETDATE(),
    Durum BIT DEFAULT 1, -- Veri bütünlüğü için Soft Delete (1: Aktif, 0: Pasif/Silinmiş Hesap)
    FOREIGN KEY (RolID) REFERENCES Roller(RolID)
);

-- Kitapların sınıflandırıldığı ana kategoriler
CREATE TABLE Kategoriler(
    KategoriID INT IDENTITY(1,1) PRIMARY KEY,
    KategoriAdi VARCHAR(100) NOT NULL UNIQUE
);

-- Kütüphane envanterindeki fiziksel materyalleri ve stok durumlarını tutan tablo
CREATE TABLE Kitaplar(
    KitapID INT IDENTITY(1,1) PRIMARY KEY,
    KitapAdi VARCHAR(150) NOT NULL,
    Yazar VARCHAR(100) NOT NULL,
    KategoriID INT NOT NULL,
    StokAdedi INT NOT NULL CHECK(StokAdedi >= 0),
    BasimYili INT CHECK(BasimYili >= 1900),
    ISBN VARCHAR(20) UNIQUE,
    Durum BIT DEFAULT 1, -- Veri bütünlüğü için Soft Delete (1: Aktif, 0: Pasif/Gizli)
    FOREIGN KEY (KategoriID) REFERENCES Kategoriler(KategoriID)
);

-- Kullanıcıların kitap ödünç alma ve planlanan teslim tarihlerini takip eden tablo
CREATE TABLE OduncIslemleri(
    IslemID INT IDENTITY(1,1) PRIMARY KEY,
    KullaniciID INT NOT NULL,
    KitapID INT NOT NULL,
    AlisTarihi DATETIME DEFAULT GETDATE(),      -- Hassas takip ve test için DATETIME kullanılmıştır
    PlanlananIadeTarihi DATETIME NOT NULL,      
    GercekIadeTarihi DATETIME NULL,             
    FOREIGN KEY (KullaniciID) REFERENCES Kullanicilar(KullaniciID),
    FOREIGN KEY (KitapID) REFERENCES Kitaplar(KitapID)
);

-- İade süresi aşılmış işlemler için uygulanan gecikme cezalarını barındıran tablo
CREATE TABLE CezaIslemleri(
    CezaID INT IDENTITY(1,1) PRIMARY KEY,
    IslemID INT NOT NULL,
    GecikmeSaniye INT NOT NULL, -- Akademik sunum ve anlık test senaryosu için saniye bazlı gecikme
    CezaTutari DECIMAL(10,2) NOT NULL,
    FOREIGN KEY (IslemID) REFERENCES OduncIslemleri(IslemID)
);

-- Envanterde mevcut olan ancak o an tüm stokları tükenmiş kitaplar için bekleme sırası
CREATE TABLE Rezervasyonlar(
    RezervasyonID INT IDENTITY(1,1) PRIMARY KEY,
    KullaniciID INT NOT NULL,
    KitapID INT NOT NULL,
    Durum VARCHAR(50) DEFAULT 'Sırada Bekliyor',
    Tarih DATE DEFAULT GETDATE(),
    FOREIGN KEY (KullaniciID) REFERENCES Kullanicilar(KullaniciID),
    FOREIGN KEY (KitapID) REFERENCES Kitaplar(KitapID)
);

-- Kütüphane envanterinde hiç bulunmayan materyaller için yapılan yeni satın alma talepleri
CREATE TABLE KitapTalepleri(
    TalepID INT IDENTITY(1,1) PRIMARY KEY,
    KullaniciID INT NOT NULL,
    IstenenKitapAdi VARCHAR(150) NOT NULL,
    Yazar VARCHAR(100) NOT NULL,
    Durum VARCHAR(50) DEFAULT 'Beklemede', 
    TalepTarihi DATE DEFAULT GETDATE(),
    FOREIGN KEY (KullaniciID) REFERENCES Kullanicilar(KullaniciID)
);

-- Sistem üzerinde gerçekleştirilen kritik güvenlik ve veri hareketlerinin denetim günlüğü (Audit Log)
CREATE TABLE LogKayitlari(
    LogID INT IDENTITY(1,1) PRIMARY KEY,
    Aciklama VARCHAR(250) NOT NULL,
    LogTarihi DATETIME DEFAULT GETDATE()
);
GO

/*========================================================================================
   2. VARSAYILAN SİSTEM VERİLERİNİN EKLENMESİ (DML)
========================================================================================*/

-- Roller ve iade süreleri tanımlanır (Yöneticiler süresiz, kullanıcılar hiyerarşik sürelere tabidir)
INSERT INTO Roller (RolID, RolAdi, IadeSuresiGun) VALUES 
(1, 'Kurucu Admin', 0), (2, 'Admin', 0),
(3, 'Lisans Ogrencisi', 14), (4, 'Yuksek Lisans', 21), (5, 'Akademisyen', 28);

-- Envanter başlangıç kategorileri
INSERT INTO Kategoriler(KategoriAdi) VALUES ('Bilim Kurgu'), ('Tarih'), ('Yazilim'), ('Akademik Yayinlar');

-- Sistem kurulumunu yönetecek en üst düzey yetkili hesabı (Kurucu) oluşturulur
INSERT INTO Kullanicilar(AdSoyad, KullaniciNo, Email, Sifre, RolID)
VALUES('Kurucu Mudur', 'ADMIN001', 'kurucu@kutuphane.com', '1234', 1);
GO

/*========================================================================================
   3. SAKLI YORDAMLAR (STORED PROCEDURES)
========================================================================================*/

-- Sisteme yeni bir kullanıcı veya akademisyen kaydeder
CREATE PROCEDURE sp_UyeOl
    @AdSoyad VARCHAR(100), @KullaniciNo VARCHAR(50), @Email VARCHAR(100), @Sifre VARCHAR(100), @RolID INT
AS
BEGIN
    INSERT INTO Kullanicilar(AdSoyad, KullaniciNo, Email, Sifre, RolID) VALUES(@AdSoyad, @KullaniciNo, @Email, @Sifre, @RolID);
    INSERT INTO LogKayitlari(Aciklama) VALUES('Yeni kullanici eklendi: ' + @KullaniciNo);
END
GO

-- Yetkili tarafından sistemden bir kullanıcı hesabını pasife alır (Soft Delete).
-- Geçmiş ödünç ve ceza kayıtlarının referans bütünlüğü (Foreign Key) bozulmadan korunur.
CREATE PROCEDURE sp_KullaniciSil @SilinecekKullaniciID INT
AS
BEGIN
    UPDATE Kullanicilar SET Durum = 0 WHERE KullaniciID = @SilinecekKullaniciID;
    INSERT INTO LogKayitlari(Aciklama) VALUES('Kullanici pasife alindi (Soft Delete). ID: ' + CAST(@SilinecekKullaniciID AS VARCHAR));
END
GO

-- Envantere yeni bir kitap veya akademik yayın kaydeder
CREATE PROCEDURE sp_KitapEkle
    @KitapAdi VARCHAR(150), @Yazar VARCHAR(100), @KategoriID INT, @StokAdedi INT, @BasimYili INT, @ISBN VARCHAR(20)
AS
BEGIN
    INSERT INTO Kitaplar(KitapAdi,Yazar,KategoriID,StokAdedi,BasimYili,ISBN) VALUES(@KitapAdi,@Yazar,@KategoriID,@StokAdedi,@BasimYili,@ISBN);
    INSERT INTO LogKayitlari(Aciklama) VALUES('Admin yeni kitap ekledi: ' + @KitapAdi);
END
GO

-- Kitabı pasife çeker (Soft Delete). Geçmiş ödünç ve ceza kayıtlarının referans bütünlüğünü korur.
CREATE PROCEDURE sp_KitapSil @KitapID INT
AS
BEGIN
    UPDATE Kitaplar SET Durum = 0 WHERE KitapID = @KitapID;
    INSERT INTO LogKayitlari(Aciklama) VALUES('Kitap sistemden kaldirildi. ID: ' + CAST(@KitapID AS VARCHAR));
END
GO

-- Materyal ödünç alma işlemini gerçekleştirir. (Akademik sunum testi için iade süresi 5 saniye olarak ayarlanmıştır)
CREATE PROCEDURE sp_KitapOduncAl
    @KullaniciID INT, @KitapID INT
AS
BEGIN
    IF EXISTS(SELECT * FROM Kitaplar WHERE KitapID=@KitapID AND StokAdedi > 0 AND Durum = 1)
    BEGIN
        DECLARE @PlanlananTarih DATETIME = DATEADD(SECOND, 5, GETDATE());
        INSERT INTO OduncIslemleri(KullaniciID, KitapID, PlanlananIadeTarihi) VALUES(@KullaniciID, @KitapID, @PlanlananTarih);
        UPDATE Kitaplar SET StokAdedi = StokAdedi - 1 WHERE KitapID=@KitapID;
    END
END
GO

/*========================================================================================
   4. OTOMASYON TETİKLEYİCİLERİ (TRIGGERS)
========================================================================================*/

-- Bir materyal iade edildiği anda arka planda otomatik tetiklenen kural zinciri.
-- Stokları günceller ve gecikme süresi varsa saniye bazlı ceza tahakkuk ettirir.
CREATE TRIGGER trg_KitapIade
ON OduncIslemleri
AFTER UPDATE
AS
BEGIN
    IF UPDATE(GercekIadeTarihi)
    BEGIN
        DECLARE @KitapID INT, @IslemID INT, @Planlanan DATETIME, @Gercek DATETIME, @Gecikme INT, @Ceza DECIMAL(10,2);

        SELECT @KitapID = KitapID, @IslemID = IslemID, @Planlanan = PlanlananIadeTarihi, @Gercek = GercekIadeTarihi
        FROM inserted WHERE GercekIadeTarihi IS NOT NULL;

        -- Fiziksel stok kütüphaneye geri eklenir
        UPDATE Kitaplar SET StokAdedi = StokAdedi + 1 WHERE KitapID=@KitapID;

        -- Planlanan iade tarihi ile gerçek iade tarihi arasındaki saniye farkı hesaplanır
        SET @Gecikme = DATEDIFF(SECOND, @Planlanan, @Gercek);
        
        -- Eğer belirlenen süre aşılmışsa, gecikilen her saniye için 2 TL ceza bedeli yansıtılır
        IF @Gecikme > 0
        BEGIN
            SET @Ceza = @Gecikme * 2; 
            INSERT INTO CezaIslemleri(IslemID, GecikmeSaniye, CezaTutari) VALUES(@IslemID, @Gecikme, @Ceza);
        END
    END
END
GO