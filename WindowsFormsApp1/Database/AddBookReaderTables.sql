USE QL_ebook;
GO

PRINT '>>> Ðang t?o b?ng NoiDungSach...'
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

-- T?o Index ð? t?m ki?m nhanh
CREATE INDEX IX_NoiDungSach_MaSach ON NoiDungSach(MaSach);
CREATE INDEX IX_NoiDungSach_SoChap ON NoiDungSach(MaSach, SoChap);

PRINT '>>> Ðang t?o b?ng VT_DocSach (V? trí ð?c)...'
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

CREATE INDEX IX_VTDoc_MaSach ON VT_DocSach(MaSach);
CREATE INDEX IX_VTDoc_MaNguoiDung ON VT_DocSach(MaNguoiDung);

PRINT '>>> HOÀN THÀNH T?O CÁC B?NG BOOK READER!'
GO
