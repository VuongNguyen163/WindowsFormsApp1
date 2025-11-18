# Koodo Reader - H??ng D?n C?p Nh?t

## ?? Tóm T?t Các Thay ??i

### 1. **Book.cs** - Thêm Properties M?i
- ? `PublisherId` - ID nhà xu?t b?n
- ? `Description` - Mô t? sách
- ? `Rating` - X?p h?ng sách
- ? Method `UpdateProgress()` - C?p nh?t ti?n ?? ??c

### 2. **DataManager.cs** - Implement Trash System & Shelf Management
- ? `GetAllBooks()` - L?y sách (không bao g?m sách ?ã xóa)
- ? `GetDeletedBooks()` - L?y sách t? thùng rác
- ? `DeleteBook()` - Di chuy?n sách vào thùng rác (soft delete)
- ? `RestoreBook()` - Khôi ph?c sách t? thùng rác
- ? `PermanentlyDeleteBook()` - Xóa v?nh vi?n sách
- ? `GetShelves()` - L?y danh sách shelf t? database
- ? `AddShelf()` - Thêm shelf m?i
- ? `RemoveShelf()` - Xóa shelf
- ? `GetCurrentUser()` - L?y user hi?n t?i

### 3. **MainForm.cs** - C?p Nh?t Logic Menu
- ? `ShowBookMenu()` - C?p nh?t logic hi?n th? menu d?a vào `IsDeleted` flag

## ??? Database Schema Mapping

### B?ng Chính: `Sach`
```
MaSach (ID)
TieuDe (Title) 
TacGia (Author) - qua Sach_TacGia
MoTa (Description)
DuongDanFile (FilePath)
DinhDang (FileType)
TongSoTrang (TotalPages)
TrangHienTai (CurrentPage)
YeuThich (IsFavorite)
NgayThem (DateAdded)
```

### B?ng Trash: `ThungRac`
```
MaSach - Khóa ngo?i t?i Sach
- Sách n?m ? ?ây ???c coi là ?ã xóa
- Xóa record t? ThungRac = khôi ph?c sách
- Xóa record t? Sach (và related tables) = xóa v?nh vi?n
```

### B?ng Shelf: `KeSach`
```
MaKeSach (ID)
MaNguoiDung (UserID)
TenKeSach (Name)
MoTa (Description)
NgayTao (DateCreated)
```

## ?? H??ng D?n S? D?ng

### 1. Kh?i T?o Database
```sql
-- Ch?y CreateDatabase.sql ?? t?o database và schema
-- Ch?y InitializeDefaultShelves.sql ?? t?o các shelf m?c ??nh
```

### 2. Connection String
C?p nh?t file `DatabaseConnection.cs`:
```csharp
// Windows Authentication
connectionString = @"Server=localhost;Database=QL_ebook;Integrated Security=True;";

// SQL Server Authentication
connectionString = @"Server=localhost;Database=QL_ebook;User Id=sa;Password=yourpassword;";
```

### 3. Tính N?ng Trash (Thùng Rác)
- **Di chuy?n vào trash**: Chu?t ph?i ? "Move to Trash"
- **Khôi ph?c**: Xem "Trash" ? Chu?t ph?i ? "Restore"
- **Xóa v?nh vi?n**: Trash ? Chu?t ph?i ? "Delete Permanently"

### 4. Shelf Management
- Shelves ???c l?u tr? trong database `KeSach`
- Default shelves: "All Books", "Reading", "Want to Read", "Completed"
- ComboBox t? ??ng load t? database

### 5. Tác Gi? (Authors)
- Tác gi? ???c qu?n lý trong b?ng `TacGia`
- Liên k?t nhi?u-nhi?u qua `Sach_TacGia`
- T? ??ng t?o tác gi? m?i n?u ch?a t?n t?i

## ?? Flow Xóa Sách

### Normal Delete (Move to Trash)
```
User Click "Move to Trash"
?
DeleteBook(bookId) ???c g?i
?
INSERT INTO ThungRac (MaSach) VALUES (@MaSach)
?
Sách không còn hi?n th? ? "Books" view
?
Sách hi?n th? ? "Trash" view
```

### Restore from Trash
```
User Click "Restore" ? Trash view
?
RestoreBook(bookId) ???c g?i
?
DELETE FROM ThungRac WHERE MaSach = @MaSach
?
Sách quay l?i "Books" view
```

### Permanent Delete
```
User Click "Delete Permanently" ? Trash view
?
PermanentlyDeleteBook(bookId) ???c g?i
?
DELETE FROM: ThungRac, Sach_TacGia, Sach_TheLoai, KeSach_Sach, GhiChu, DanhDauTrang, Sach
?
Sách b? xóa hoàn toàn kh?i database
```

## ?? Query Examples

### L?y t?t c? sách (không bao g?m trash)
```sql
SELECT s.* FROM Sach s
WHERE s.MaNguoiDung = @MaNguoiDung
AND s.MaSach NOT IN (SELECT MaSach FROM ThungRac)
```

### L?y sách ? trash
```sql
SELECT s.* FROM Sach s
INNER JOIN ThungRac tr ON s.MaSach = tr.MaSach
WHERE s.MaNguoiDung = @MaNguoiDung
```

### L?y sách yêu thích (không bao g?m trash)
```sql
SELECT s.* FROM Sach s
WHERE s.MaNguoiDung = @MaNguoiDung
AND s.YeuThich = 1
AND s.MaSach NOT IN (SELECT MaSach FROM ThungRac)
```

## ?? Quan Tr?ng

1. **Database Connection**: ??m b?o SQL Server ?ang ch?y và connection string chính xác
2. **User ID**: Code m?c ??nh dùng `currentUserId = 1`, thay ??i n?u c?n
3. **Backup**: Tr??c khi ch?y "Delete Permanently", hãy backup database
4. **NULL handling**: Code x? lý NULL values cho Author, Description, vv.

## ?? Troubleshooting

### "Không th? k?t n?i ??n database"
- Ki?m tra SQL Server Service
- Ki?m tra connection string ? DatabaseConnection.cs
- Ki?m tra firewall

### Sách không xu?t hi?n ? Trash
- Ki?m tra ThungRac table có record không
- Ki?m tra IsDeleted flag ? code

### L?i khi xóa sách
- Ki?m tra constraints (Foreign Keys)
- Ensure user có quy?n DELETE trên các tables
- Check SQL Server error logs

## ?? Notes

- T?t c? queries s? d?ng parameterized commands ?? tránh SQL Injection
- Code s? d?ng using statements ?? ??m b?o resource cleanup
- Error handling v?i try-catch và MessageBox
- Soft delete pattern s? d?ng ThungRac table (không c?n IsDeleted column ? Sach)
