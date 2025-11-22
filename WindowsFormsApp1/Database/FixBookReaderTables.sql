-- =====================================================
-- SCRIPT C?P NH?T B?NG CHO BOOK READER (UTF-8)
-- =====================================================
USE QL_ebook;
GO

-- Xóa các b?ng c? n?u t?n t?i
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'VT_DocSach')
    DROP TABLE VT_DocSach;

IF EXISTS (SELECT * FROM sys.tables WHERE name = 'NoiDungSach')
    DROP TABLE NoiDungSach;

GO

-- =====================================================
-- B?NG 1: NoiDungSach - Lýu n?i dung các chýõng (UTF-8)
-- =====================================================
CREATE TABLE NoiDungSach (
    MaNoiDung INT IDENTITY(1,1) PRIMARY KEY,
    MaSach INT NOT NULL,
    SoChap INT NOT NULL DEFAULT 0,
    TieuDeChap NVARCHAR(500),
    NoiDung NVARCHAR(MAX) NOT NULL,
    SoTu INT DEFAULT 0,
    NgayThem DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_NoiDungSach_Sach FOREIGN KEY (MaSach) REFERENCES Sach(MaSach) ON DELETE CASCADE,
    CONSTRAINT UK_NoiDungSach UNIQUE (MaSach, SoChap)
);

-- Index
CREATE INDEX IX_NoiDungSach_MaSach ON NoiDungSach(MaSach);
CREATE INDEX IX_NoiDungSach_SoChap ON NoiDungSach(MaSach, SoChap);

PRINT 'B?ng NoiDungSach ð? ðý?c t?o thành công (UTF-8)';
GO

-- =====================================================
-- B?NG 2: VT_DocSach - V? trí ð?c c?a ngý?i dùng
-- =====================================================
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

-- Index
CREATE INDEX IX_VTDoc_MaSach ON VT_DocSach(MaSach);
CREATE INDEX IX_VTDoc_MaNguoiDung ON VT_DocSach(MaNguoiDung);

PRINT 'B?ng VT_DocSach ð? ðý?c t?o thành công';
GO

-- =====================================================
-- KI?M TRA K?T QU?
-- =====================================================
SELECT 
    TABLE_NAME,
    (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS 
     WHERE TABLE_NAME = t.TABLE_NAME) AS SoCot
FROM INFORMATION_SCHEMA.TABLES t
WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME IN ('NoiDungSach', 'VT_DocSach')
ORDER BY TABLE_NAME;

PRINT '=== Hoàn thành c?p nh?t! ===';
