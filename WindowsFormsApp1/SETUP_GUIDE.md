# Koodo Reader - H??ng D?n Setup Hoàn Ch?nh

## ?? M?c Tiêu
C?p nh?t Koodo Reader application ?? hoàn toàn phù h?p v?i database schema và implement ??y ?? tính n?ng Trash.

## ? Các Thay ??i ?ã Hoàn Thành

### Code Changes
1. ? **Book.cs** - Thêm 3 properties m?i: `PublisherId`, `Description`, `Rating`
2. ? **DataManager.cs** - Implement 10+ methods liên quan ??n Trash và Shelf
3. ? **MainForm.cs** - C?p nh?t logic ShowBookMenu ?? handle Trash ?úng cách

### Files T?o M?i
- `Database\InitializeDefaultShelves.sql` - Script kh?i t?o shelves
- `Database\Migration_UpdateDatabase.sql` - Script update database
- `README_UPDATES.md` - Tài li?u chi ti?t

## ?? Các B??c Setup

### B??c 1: Chu?n B? Database

#### N?u ch?a có database
```sql
-- Ch?y script này trong SQL Server Management Studio:
-- 1. M? file: CreateDatabase.sql (t? database folder)
-- 2. F5 ho?c Ctrl+Shift+E ?? execute
-- 3. Ch? hoàn thành
```

#### N?u ?ã có database c?
```sql
-- 1. M? file: Migration_UpdateDatabase.sql
-- 2. Execute nó ?? update schema
-- 3. Verify output
```

### B??c 2: Kh?i T?o Default Shelves

```sql
-- M? file: InitializeDefaultShelves.sql
-- Execute nó ?? t?o các shelf m?c ??nh
```

### B??c 3: C?p Nh?t Connection String

**File**: `WindowsFormsApp1\Data\DatabaseConnection.cs`

```csharp
// Tùy ch?n 1: Windows Authentication (n?u dùng Windows domain)
connectionString = @"Server=localhost;Database=QL_ebook;Integrated Security=True;";

// Tùy ch?n 2: SQL Server Authentication (n?u có username/password)
connectionString = @"Server=localhost;Database=QL_ebook;User Id=sa;Password=yourpassword;";

// Tùy ch?n 3: Khác server
connectionString = @"Server=192.168.1.100;Database=QL_ebook;Integrated Security=True;";
```

### B??c 4: Build & Run Application

```
Visual Studio:
1. Build Solution (Ctrl+Shift+B)
2. Run (F5)
3. ?ng d?ng s? test connection khi kh?i ??ng
4. N?u có l?i connection, s? show dialog
```

## ?? Database Mapping

### Book ? Sach Table

| C# Property | Database Column | Type | Note |
|---|---|---|---|
| Id | MaSach | INT | Primary Key |
| Title | TieuDe | NVARCHAR(255) | Required |
| Author | TenTacGia (Sach_TacGia) | NVARCHAR(100) | Many-to-Many |
| Description | MoTa | NVARCHAR(MAX) | Optional |
| FilePath | DuongDanFile | NVARCHAR(MAX) | Required |
| FileType | DinhDang | VARCHAR(10) | e.g., "EPUB", "PDF" |
| TotalPages | TongSoTrang | INT | Default 0 |
| CurrentPage | TrangHienTai | INT | Default 0 |
| Progress | Calculated | FLOAT | (CurrentPage / TotalPages) * 100 |
| IsFavorite | YeuThich | BIT | Default 0 |
| DateAdded | NgayThem | DATETIME | Auto set |
| CoverImagePath | DuongDanAnhBia | NVARCHAR(MAX) | Optional |
| IsDeleted | Via ThungRac | - | Soft delete |
| Rating | XepHang | TINYINT | Default 0 |

## ??? Folder Structure

```
WindowsFormsApp1/
??? Data/
?   ??? DataManager.cs (? Updated)
?   ??? DatabaseConnection.cs (unchanged)
??? Controls/
?   ??? BookCard.cs (unchanged)
??? Database/
?   ??? CreateDatabase.sql (original)
?   ??? InitializeDefaultShelves.sql (new)
?   ??? Migration_UpdateDatabase.sql (new)
??? Book.cs (? Updated)
??? MainForm.cs (? Updated)
??? MainForm.Designer.cs (unchanged)
??? Program.cs (unchanged)
??? README_UPDATES.md (new)
```

## ?? Flow Diagram: Trash System

```
???????????????????
?  Regular Books  ?
?   (Books view)  ?
???????????????????
         ? Right-click ? "Move to Trash"
         ?
   DeleteBook(id)
         ?
    INSERT INTO ThungRac
         ?
???????????????????????
?  Trash Books        ?
?  (Trash view)       ?
?  in ThungRac table  ?
???????????????????????
         ?
    ???????????
    ?          ?
    ?          ?
Restore   Delete Permanently
    ?          ?
    ?          ?
DELETE    DELETE FROM
FROM      (ThungRac +
ThungRac  Related tables)
    ?          ?
    ?          ?
Back to      Removed
Books        from DB
```

## ?? Queries Chính

### 1. Get All Books (không trash)
```sql
SELECT s.* FROM Sach s
WHERE MaNguoiDung = @userId
  AND MaSach NOT IN (SELECT MaSach FROM ThungRac)
ORDER BY NgayThem DESC
```

### 2. Get Deleted Books (only trash)
```sql
SELECT s.* FROM Sach s
INNER JOIN ThungRac tr ON s.MaSach = tr.MaSach
WHERE MaNguoiDung = @userId
```

### 3. Move to Trash (soft delete)
```sql
INSERT INTO ThungRac (MaSach) VALUES (@bookId)
```

### 4. Restore from Trash
```sql
DELETE FROM ThungRac WHERE MaSach = @bookId
```

### 5. Permanent Delete
```sql
DELETE FROM ThungRac WHERE MaSach = @bookId;
DELETE FROM Sach_TacGia WHERE MaSach = @bookId;
DELETE FROM Sach_TheLoai WHERE MaSach = @bookId;
DELETE FROM KeSach_Sach WHERE MaSach = @bookId;
DELETE FROM GhiChu WHERE MaSach = @bookId;
DELETE FROM DanhDauTrang WHERE MaSach = @bookId;
DELETE FROM Sach WHERE MaSach = @bookId;
```

## ?? Testing Checklist

- [ ] Connect to database successfully
- [ ] Load all books in Books view
- [ ] Load favorites
- [ ] Search books
- [ ] Move book to trash (check ThungRac table)
- [ ] Load trash view (should show deleted books)
- [ ] Restore book from trash
- [ ] Permanently delete book
- [ ] Toggle favorite
- [ ] Import book
- [ ] Sort books (by various options)
- [ ] Check shelves in combobox

## ?? Environment Requirements

- **Visual Studio 2017+** (or compatible IDE)
- **SQL Server 2012+** or **SQL Server Express**
- **.NET Framework 4.7.2**
- **System Clipboard** (for copy/paste operations)

## ?? Troubleshooting

### Issue: "Cannot connect to database"
**Solution:**
1. Verify SQL Server service is running
2. Check connection string in DatabaseConnection.cs
3. Verify server name/credentials
4. Check firewall settings

### Issue: "Book not appearing in Trash"
**Solution:**
1. Verify record in ThungRac table: `SELECT * FROM ThungRac`
2. Check MaSach value in ThungRac matches Sach
3. Verify FK constraint isn't broken

### Issue: "Error when deleting book"
**Solution:**
1. Check if book has related records (notes, highlights)
2. Verify foreign key constraints
3. Check SQL Server error logs
4. Try deleting via SSMS directly

### Issue: "Shelves not showing in combobox"
**Solution:**
1. Verify KeSach table has records
2. Run InitializeDefaultShelves.sql
3. Verify MaNguoiDung = 1 has shelf records
4. Check GetShelves() query

## ?? Security Notes

- All queries use parameterized commands (SQL Injection safe)
- Connection pooling handled by SqlConnection
- No hardcoded credentials in code
- User context enforced (MaNguoiDung parameter)

## ?? Performance Optimization

Added indexes:
- `IDX_Sach_MaNguoiDung` - For filtering by user
- `IDX_Sach_YeuThich` - For favorite books
- `IDX_KeSach_MaNguoiDung` - For shelf queries

## ?? Next Steps (Future)

- [ ] Implement Notes feature (GhiChu table)
- [ ] Implement Highlights feature (DanhDauTrang table)
- [ ] Add book cover upload
- [ ] Add book categories (TheLoai)
- [ ] User authentication UI
- [ ] Cloud sync capability

---

**Version**: 1.0  
**Last Updated**: 2024  
**Status**: ? Ready for Testing
