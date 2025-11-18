-- Script kh?i t?o các Shelf m?c ??nh cho user
-- Ch?y script này sau khi t?o database t? CreateDatabase.sql

USE QL_ebook;
GO

-- Thêm các shelf m?c ??nh cho admin user (MaNguoiDung = 1)
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

-- Thêm các shelf m?c ??nh cho user th? hai (MaNguoiDung = 2)
IF NOT EXISTS (SELECT 1 FROM KeSach WHERE MaNguoiDung = 2 AND TenKeSach = N'All Books')
BEGIN
    INSERT INTO KeSach (MaNguoiDung, TenKeSach, MoTa, NgayTao)
    VALUES (2, N'All Books', N'T?t c? sách c?a b?n', GETDATE());
    PRINT 'Thêm shelf "All Books" cho user 2 thành công!';
END

PRINT 'Hoàn thành kh?i t?o shelf!';
GO
