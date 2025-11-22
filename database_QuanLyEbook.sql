USE master;
GO

-- Xóa database cũ để tạo lại
IF EXISTS (SELECT * FROM sys.databases WHERE name = 'QL_ebook')
BEGIN
    PRINT 'Đang xóa database cũ...'
    ALTER DATABASE QL_ebook SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE QL_ebook;
END
GO

PRINT 'Đang tạo database QL_ebook...'
CREATE DATABASE QL_ebook;
GO

USE QL_ebook;
GO

-- =============================================
-- PHẦN 1: TẠO CÁC BẢNG (TABLES)
-- =============================================

PRINT '>>> Đang tạo bảng NguoiDung...'
CREATE TABLE NguoiDung (
    MaNguoiDung INT IDENTITY(1,1) PRIMARY KEY,
    TenDangNhap NVARCHAR(50) NOT NULL UNIQUE,
    MatKhauHash NVARCHAR(255) NOT NULL,
    Email NVARCHAR(100) UNIQUE,
    TenHienThi NVARCHAR(100),
    NgayTao DATETIME NOT NULL DEFAULT GETDATE()
);

PRINT '>>> Đang tạo bảng NhaXuatBan...'
CREATE TABLE NhaXuatBan (
    MaNhaXuatBan INT IDENTITY(1,1) PRIMARY KEY,
    TenNXB NVARCHAR(100) NOT NULL
);

PRINT '>>> Đang tạo bảng TheLoai...'
CREATE TABLE TheLoai (
    MaTheLoai INT IDENTITY(1,1) PRIMARY KEY,
    TenTheLoai NVARCHAR(100) NOT NULL UNIQUE
);

PRINT '>>> Đang tạo bảng TacGia...'
CREATE TABLE TacGia (
    MaTacGia INT IDENTITY(1,1) PRIMARY KEY,
    TenTacGia NVARCHAR(100) NOT NULL
);

PRINT '>>> Đang tạo bảng Plugin...'
CREATE TABLE Plugin (
    MaPlugin INT IDENTITY(1,1) PRIMARY KEY,
    TenHienThi NVARCHAR(100) NOT NULL,
    LoaiPlugin NVARCHAR(50),
    BieuTuong NVARCHAR(MAX),
    PhienBan NVARCHAR(20),
    CauHinh NVARCHAR(MAX),
    KichHoat BIT NOT NULL DEFAULT 0
);

PRINT '>>> Đang tạo bảng KeSach...'
CREATE TABLE KeSach (
    MaKeSach INT IDENTITY(1,1) PRIMARY KEY,
    MaNguoiDung INT NOT NULL,
    TenKeSach NVARCHAR(100) NOT NULL,
    MoTa NVARCHAR(255),
    NgayTao DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_KeSach_NguoiDung FOREIGN KEY (MaNguoiDung) REFERENCES NguoiDung(MaNguoiDung) ON DELETE CASCADE
);

PRINT '>>> Đang tạo bảng Sach...'
CREATE TABLE Sach (
    MaSach INT IDENTITY(1,1) PRIMARY KEY,
    MaNguoiDung INT NOT NULL,
    MaNhaXuatBan INT NULL,
    TieuDe NVARCHAR(255) NOT NULL,
    MoTa NVARCHAR(MAX),
    MaMD5 VARCHAR(32),
    DuongDanAnhBia NVARCHAR(MAX),
    DinhDang VARCHAR(10),
    KichThuocKB INT,
    TongSoTrang INT,
    DuongDanFile NVARCHAR(MAX) NOT NULL,
    NgayThem DATETIME NOT NULL DEFAULT GETDATE(),
    TrangHienTai INT NOT NULL DEFAULT 0,
    XepHang TINYINT,
    YeuThich BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_Sach_NguoiDung FOREIGN KEY (MaNguoiDung) REFERENCES NguoiDung(MaNguoiDung),
    CONSTRAINT FK_Sach_NhaXuatBan FOREIGN KEY (MaNhaXuatBan) REFERENCES NhaXuatBan(MaNhaXuatBan)
);

-- BẢNG MỚI: THÙNG RÁC (ĐÃ RÚT GỌN)
PRINT '>>> Đang tạo bảng ThungRac (MỚI)...'
CREATE TABLE ThungRac (
    MaRac INT IDENTITY(1,1) PRIMARY KEY,
    MaSach INT NOT NULL UNIQUE, -- Chỉ cần lưu ID sách để biết sách nào đang ở thùng rác
    
    CONSTRAINT FK_ThungRac_Sach FOREIGN KEY (MaSach) REFERENCES Sach(MaSach) ON DELETE CASCADE
);

-- CÁC BẢNG LIÊN KẾT

PRINT '>>> Đang tạo bảng Sach_TacGia...'
CREATE TABLE Sach_TacGia (
    MaSach INT NOT NULL,
    MaTacGia INT NOT NULL,
    PRIMARY KEY (MaSach, MaTacGia),
    CONSTRAINT FK_SachTacGia_Sach FOREIGN KEY (MaSach) REFERENCES Sach(MaSach) ON DELETE CASCADE,
    CONSTRAINT FK_SachTacGia_TacGia FOREIGN KEY (MaTacGia) REFERENCES TacGia(MaTacGia) ON DELETE CASCADE
);

PRINT '>>> Đang tạo bảng Sach_TheLoai...'
CREATE TABLE Sach_TheLoai (
    MaSach INT NOT NULL,
    MaTheLoai INT NOT NULL,
    PRIMARY KEY (MaSach, MaTheLoai),
    CONSTRAINT FK_SachTheLoai_Sach FOREIGN KEY (MaSach) REFERENCES Sach(MaSach) ON DELETE CASCADE,
    CONSTRAINT FK_SachTheLoai_TheLoai FOREIGN KEY (MaTheLoai) REFERENCES TheLoai(MaTheLoai) ON DELETE CASCADE
);

PRINT '>>> Đang tạo bảng KeSach_Sach...'
CREATE TABLE KeSach_Sach (
    MaKeSach INT NOT NULL,
    MaSach INT NOT NULL,
    NgayThemVaoKe DATETIME DEFAULT GETDATE(),
    PRIMARY KEY (MaKeSach, MaSach),
    CONSTRAINT FK_KeSachSach_KeSach FOREIGN KEY (MaKeSach) REFERENCES KeSach(MaKeSach) ON DELETE CASCADE,
    CONSTRAINT FK_KeSachSach_Sach FOREIGN KEY (MaSach) REFERENCES Sach(MaSach) ON DELETE CASCADE
);

-- CÁC BẢNG PHỤ THUỘC

PRINT '>>> Đang tạo bảng GhiChu...'
CREATE TABLE GhiChu (
    MaGhiChu INT IDENTITY(1,1) PRIMARY KEY,
    MaSach INT NOT NULL,
    NgayTao DATETIME NOT NULL DEFAULT GETDATE(),
    TenChuong NVARCHAR(255),
    NoiDungTrichDan NVARCHAR(MAX),
    GhiChuCuaNguoiDung NVARCHAR(MAX),
    ViTriCFI NVARCHAR(255),
    PhanTramDoc FLOAT,
    MauSac VARCHAR(7),
    TheTag NVARCHAR(100),
    CONSTRAINT FK_GhiChu_Sach FOREIGN KEY (MaSach) REFERENCES Sach(MaSach) ON DELETE CASCADE
);

PRINT '>>> Đang tạo bảng DanhDauTrang...'
CREATE TABLE DanhDauTrang (
    MaDanhDau INT IDENTITY(1,1) PRIMARY KEY,
    MaSach INT NOT NULL,
    NhanDan NVARCHAR(255),
    ViTriCFI NVARCHAR(255),
    PhanTramDoc FLOAT,
    TenChuong NVARCHAR(255),
    NgayTao DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_DanhDauTrang_Sach FOREIGN KEY (MaSach) REFERENCES Sach(MaSach) ON DELETE CASCADE
);

PRINT 'HOÀN THÀNH TẠO TẤT CẢ CÁC BẢNG!'
GO
-- Tạo bảng VT_DocSach
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

-- Tạo Index
CREATE INDEX IX_VTDoc_MaSach ON VT_DocSach(MaSach);
CREATE INDEX IX_VTDoc_MaNguoiDung ON VT_DocSach(MaNguoiDung);

PRINT 'Bảng VT_DocSach đã được tạo thành công!';
GO
-- =============================================
-- PHẦN 2: CHÈN DỮ LIỆU MẪU
-- =============================================

PRINT '--- BẮT ĐẦU CHÈN DỮ LIỆU MẪU ---';

-- 1. Dữ liệu cơ bản
INSERT INTO NguoiDung (TenDangNhap, MatKhauHash, Email, TenHienThi) VALUES
('admin', 'HASH123', 'admin@sachhub.com', N'Quản Trị Viên'),
('thanh', 'HASH456', 'thanh@gmail.com', N'Nguyễn Văn Thành');

INSERT INTO TacGia (TenTacGia) VALUES (N'Frank Herbert'), (N'Dan Brown'), (N'J.K. Rowling');
INSERT INTO TheLoai (TenTheLoai) VALUES (N'Viễn tưởng'), (N'Trinh thám'), (N'Kinh điển');
INSERT INTO NhaXuatBan (TenNXB) VALUES (N'NXB Kim Đồng'), (N'NXB Trẻ');

INSERT INTO Plugin (TenHienThi, LoaiPlugin, KichHoat) VALUES 
(N'Trình Dịch Thuật', N'Dịch thuật', 1);

INSERT INTO KeSach (MaNguoiDung, TenKeSach, MoTa) VALUES
(1, N'Sách Hay Nhất', N'Những cuốn sách tâm đắc'),
(1, N'Đọc Cuối Tuần', N'Sách thư giãn');

-- 2. Sách
INSERT INTO Sach (MaNguoiDung, MaNhaXuatBan, TieuDe, MoTa, DinhDang, TongSoTrang, DuongDanFile, TrangHienTai, YeuThich)
VALUES
(1, 2, N'Dune', N'Hành tinh cát...', 'EPUB', 800, N'C:\Books\dune.epub', 100, 1),
(1, 1, N'Mật mã Da Vinci', N'Bí ẩn tôn giáo...', 'PDF', 600, N'C:\Books\davinci.pdf', 50, 0),
(1, 1, N'Harry Potter 1', N'Cậu bé phù thủy...', 'MOBI', 300, N'C:\Books\hp1.mobi', 10, 0);

-- 3. Liên kết
INSERT INTO Sach_TacGia (MaSach, MaTacGia) VALUES (1, 1), (2, 2), (3, 3);
INSERT INTO Sach_TheLoai (MaSach, MaTheLoai) VALUES (1, 1), (2, 2), (3, 1);
INSERT INTO KeSach_Sach (MaKeSach, MaSach) VALUES (1, 1), (1, 2);
INSERT INTO GhiChu (MaSach, NoiDungTrichDan, GhiChuCuaNguoiDung, MauSac) VALUES
(1, N'Nước là sự sống', N'Câu này hay', '#FFFF00');

-- 4. Đưa vào Thùng Rác (Đã sửa gọn lại)
PRINT 'Đang đưa sách ID 3 vào thùng rác...'
INSERT INTO ThungRac (MaSach) VALUES (3);

PRINT '--- HOÀN THÀNH TOÀN BỘ ---';
GO