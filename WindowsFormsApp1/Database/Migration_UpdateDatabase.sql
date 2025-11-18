-- Migration Script: Update Database Schema n?u c?n
-- Script này an toàn ?? ch?y l?i nhi?u l?n (s? d?ng IF EXISTS / IF NOT EXISTS)

USE QL_ebook;
GO

PRINT '=== B?T ??U MIGRATION SCRIPT ===';
PRINT 'Ki?m tra và c?p nh?t database schema...';
GO

-- 1. Ki?m tra xem ThungRac table ?ã t?n t?i ch?a
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'ThungRac')
BEGIN
    PRINT '>>> T?o b?ng ThungRac...';
    CREATE TABLE ThungRac (
        MaRac INT IDENTITY(1,1) PRIMARY KEY,
        MaSach INT NOT NULL UNIQUE,
        CONSTRAINT FK_ThungRac_Sach FOREIGN KEY (MaSach) REFERENCES Sach(MaSach) ON DELETE CASCADE
    );
    PRINT 'T?o b?ng ThungRac thành công!';
END
ELSE
BEGIN
    PRINT 'B?ng ThungRac ?ã t?n t?i.';
END
GO

-- 2. Ki?m tra xem Sach table có c?t XepHang ch?a
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
               WHERE TABLE_NAME = 'Sach' AND COLUMN_NAME = 'XepHang')
BEGIN
    PRINT '>>> Thêm c?t XepHang vào b?ng Sach...';
    ALTER TABLE Sach ADD XepHang TINYINT DEFAULT 0;
    PRINT 'Thêm c?t XepHang thành công!';
END
ELSE
BEGIN
    PRINT 'C?t XepHang ?ã t?n t?i.';
END
GO

-- 3. Ki?m tra xem KeSach table có c?t TenKeSach ch?a
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
               WHERE TABLE_NAME = 'KeSach' AND COLUMN_NAME = 'TenKeSach')
BEGIN
    PRINT '>>> Thêm c?t TenKeSach vào b?ng KeSach...';
    ALTER TABLE KeSach ADD TenKeSach NVARCHAR(100) NOT NULL DEFAULT N'Default Shelf';
    PRINT 'Thêm c?t TenKeSach thành công!';
END
ELSE
BEGIN
    PRINT 'C?t TenKeSach ?ã t?n t?i.';
END
GO

-- 4. Ki?m tra xem KeSach table có c?t MoTa ch?a
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
               WHERE TABLE_NAME = 'KeSach' AND COLUMN_NAME = 'MoTa')
BEGIN
    PRINT '>>> Thêm c?t MoTa vào b?ng KeSach...';
    ALTER TABLE KeSach ADD MoTa NVARCHAR(255);
    PRINT 'Thêm c?t MoTa thành công!';
END
ELSE
BEGIN
    PRINT 'C?t MoTa ?ã t?n t?i.';
END
GO

-- 5. T?o indexes ?? t?i ?u hóa queries
PRINT '>>> T?o indexes ?? t?i ?u hóa...';

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IDX_Sach_MaNguoiDung')
BEGIN
    CREATE INDEX IDX_Sach_MaNguoiDung ON Sach(MaNguoiDung);
    PRINT 'T?o index IDX_Sach_MaNguoiDung thành công!';
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IDX_Sach_YeuThich')
BEGIN
    CREATE INDEX IDX_Sach_YeuThich ON Sach(YeuThich);
    PRINT 'T?o index IDX_Sach_YeuThich thành công!';
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IDX_KeSach_MaNguoiDung')
BEGIN
    CREATE INDEX IDX_KeSach_MaNguoiDung ON KeSach(MaNguoiDung);
    PRINT 'T?o index IDX_KeSach_MaNguoiDung thành công!';
END
GO

-- 6. Thêm các shelf m?c ??nh n?u ch?a có
PRINT '>>> Thêm shelves m?c ??nh...';

IF NOT EXISTS (SELECT 1 FROM KeSach WHERE MaNguoiDung = 1 AND TenKeSach = N'All Books')
BEGIN
    INSERT INTO KeSach (MaNguoiDung, TenKeSach, MoTa, NgayTao)
    VALUES (1, N'All Books', N'T?t c? sách c?a b?n', GETDATE());
    PRINT 'Thêm shelf "All Books" thành công!';
END

IF NOT EXISTS (SELECT 1 FROM KeSach WHERE MaNguoiDung = 1 AND TenKeSach = N'Reading')
BEGIN
    INSERT INTO KeSach (MaNguoiDung, TenKeSach, MoTa, NgayTao)
    VALUES (1, N'Reading', N'Sách ?ang ??c', GETDATE());
    PRINT 'Thêm shelf "Reading" thành công!';
END

IF NOT EXISTS (SELECT 1 FROM KeSach WHERE MaNguoiDung = 1 AND TenKeSach = N'Want to Read')
BEGIN
    INSERT INTO KeSach (MaNguoiDung, TenKeSach, MoTa, NgayTao)
    VALUES (1, N'Want to Read', N'Sách mu?n ??c', GETDATE());
    PRINT 'Thêm shelf "Want to Read" thành công!';
END

IF NOT EXISTS (SELECT 1 FROM KeSach WHERE MaNguoiDung = 1 AND TenKeSach = N'Completed')
BEGIN
    INSERT INTO KeSach (MaNguoiDung, TenKeSach, MoTa, NgayTao)
    VALUES (1, N'Completed', N'Sách ?ã ??c xong', GETDATE());
    PRINT 'Thêm shelf "Completed" thành công!';
END
GO

PRINT '=== HOÀN THÀNH MIGRATION ===';
PRINT 'Database ?ã ???c c?p nh?t thành công!';
GO

-- Verify migration
PRINT '';
PRINT '=== KI?M CH?NG D? LI?U ===';
SELECT 'Tables' AS Type, COUNT(*) AS Count FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo';
SELECT 'Shelves' AS Type, COUNT(*) AS Count FROM KeSach;
SELECT 'Books' AS Type, COUNT(*) AS Count FROM Sach;
SELECT 'Deleted Books' AS Type, COUNT(*) AS Count FROM ThungRac;
GO
