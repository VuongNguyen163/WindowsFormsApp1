-- =====================================================
-- SCRIPT T?O B?NG CHO H? TH?NG Ð?C SÁCH (BOOK READER)
-- =====================================================
-- Ch?y script này trên database QL_ebook
-- Ð? kích ho?t tính nãng ð?c sách (Book Reader)

USE QL_ebook;
GO

PRINT '=================================================='
PRINT '   T?O CÁC B?NG CHO H? TH?NG Ð?C SÁCH'
PRINT '=================================================='
GO

-- =====================================================
-- B?NG 1: NoiDungSach (Lýu n?i dung các chýõng)
-- =====================================================
PRINT ''
PRINT '>>> Ðang t?o b?ng NoiDungSach...'

IF EXISTS (SELECT * FROM sys.tables WHERE name = 'NoiDungSach')
BEGIN
    PRINT 'B?ng NoiDungSach ð? t?n t?i, b? qua...'
END
ELSE
BEGIN
    CREATE TABLE NoiDungSach (
        MaNoiDung INT IDENTITY(1,1) PRIMARY KEY,
        MaSach INT NOT NULL,
        SoChap INT NOT NULL DEFAULT 0,
        TieuDe_Chap NVARCHAR(255),
        NoiDung NVARCHAR(MAX) NOT NULL,
        ThuTuSo INT NOT NULL DEFAULT 0,
        NgayThem DATETIME NOT NULL DEFAULT GETDATE(),
        CONSTRAINT FK_NoiDungSach_Sach FOREIGN KEY (MaSach) REFERENCES Sach(MaSach) ON DELETE CASCADE,
        CONSTRAINT UK_NoiDungSach_SoChap UNIQUE (MaSach, SoChap)
    );
    PRINT '? B?ng NoiDungSach ð? ðý?c t?o thành công'
END
GO

-- T?o Index cho b?ng NoiDungSach
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_NoiDungSach_MaSach')
BEGIN
    CREATE INDEX IX_NoiDungSach_MaSach ON NoiDungSach(MaSach);
    PRINT '? Index IX_NoiDungSach_MaSach ð? ðý?c t?o'
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_NoiDungSach_SoChap')
BEGIN
    CREATE INDEX IX_NoiDungSach_SoChap ON NoiDungSach(MaSach, SoChap);
    PRINT '? Index IX_NoiDungSach_SoChap ð? ðý?c t?o'
END

-- =====================================================
-- B?NG 2: VT_DocSach (V? trí ð?c c?a ngý?i dùng)
-- =====================================================
PRINT ''
PRINT '>>> Ðang t?o b?ng VT_DocSach...'

IF EXISTS (SELECT * FROM sys.tables WHERE name = 'VT_DocSach')
BEGIN
    PRINT 'B?ng VT_DocSach ð? t?n t?i, b? qua...'
END
ELSE
BEGIN
    CREATE TABLE VT_DocSach (
        MaVTDoc INT IDENTITY(1,1) PRIMARY KEY,
        MaSach INT NOT NULL,
        MaNguoiDung INT NOT NULL,
        SoChap INT NOT NULL DEFAULT 0,
        ViTriTrongChap INT NOT NULL DEFAULT 0,
        NgayCapNhat DATETIME NOT NULL DEFAULT GETDATE(),
        CONSTRAINT FK_VTDoc_Sach FOREIGN KEY (MaSach) REFERENCES Sach(MaSach) ON DELETE CASCADE,
        CONSTRAINT FK_VTDoc_NguoiDung FOREIGN KEY (MaNguoiDung) REFERENCES NguoiDung(MaNguoiDung) ON DELETE CASCADE,
        CONSTRAINT UK_VTDoc UNIQUE (MaSach, MaNguoiDung)
    );
    PRINT '? B?ng VT_DocSach ð? ðý?c t?o thành công'
END
GO

-- T?o Index cho b?ng VT_DocSach
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_VTDoc_MaSach')
BEGIN
    CREATE INDEX IX_VTDoc_MaSach ON VT_DocSach(MaSach);
    PRINT '? Index IX_VTDoc_MaSach ð? ðý?c t?o'
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_VTDoc_MaNguoiDung')
BEGIN
    CREATE INDEX IX_VTDoc_MaNguoiDung ON VT_DocSach(MaNguoiDung);
    PRINT '? Index IX_VTDoc_MaNguoiDung ð? ðý?c t?o'
END

-- =====================================================
-- KI?M TRA K?T QU?
-- =====================================================
PRINT ''
PRINT '=================================================='
PRINT '   KI?M TRA B?NG Ð? T?O'
PRINT '=================================================='

-- Li?t kê t?t c? b?ng ð? t?o
SELECT 
    TABLE_NAME as 'Tên B?ng',
    (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = t.TABLE_NAME) as 'S? C?t'
FROM INFORMATION_SCHEMA.TABLES t
WHERE TABLE_SCHEMA = 'dbo' 
  AND TABLE_NAME IN ('NoiDungSach', 'VT_DocSach')
ORDER BY TABLE_NAME;

-- Li?t kê các Index
PRINT ''
PRINT 'Các Index:'
SELECT 
    i.name as 'Tên Index',
    t.name as 'B?ng',
    COUNT(*) as 'S? C?t'
FROM sys.indexes i
JOIN sys.tables t ON i.object_id = t.object_id
WHERE t.name IN ('NoiDungSach', 'VT_DocSach')
GROUP BY i.name, t.name
ORDER BY t.name;

PRINT ''
PRINT '=================================================='
PRINT '   HOÀN THÀNH T?O CÁC B?NG!'
PRINT '=================================================='
PRINT ''
PRINT 'Các b?ng sau ð? ðý?c t?o:'
PRINT '  1. NoiDungSach - Lýu n?i dung các chýõng'
PRINT '  2. VT_DocSach - Lýu v? trí ð?c c?a ngý?i dùng'
PRINT ''
PRINT 'Bây gi? b?n có th? s? d?ng tính nãng Book Reader!'
PRINT ''
GO
