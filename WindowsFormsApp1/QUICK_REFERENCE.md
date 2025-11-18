# Koodo Reader - Quick Reference Guide

## ?? Kh?i ??ng Nhanh

### Connection String Setup
```csharp
// File: WindowsFormsApp1\Data\DatabaseConnection.cs
connectionString = @"Server=localhost;Database=QL_ebook;Integrated Security=True;";
```

### Run Application
```
F5 in Visual Studio
```

## ?? Thay ??i Chính

### 1. Trash System (Thùng Rác)
**Tr??c**: Sách b? xóa ngay l?p t?c  
**Sau**: Sách ???c di chuy?n vào ThungRac table (soft delete)

**Code Usage**:
```csharp
// Move to trash
DataManager.Instance.DeleteBook(bookId);

// Restore from trash
DataManager.Instance.RestoreBook(bookId);

// Permanent delete
DataManager.Instance.PermanentlyDeleteBook(bookId);

// Get deleted books
List<Book> deletedBooks = DataManager.Instance.GetDeletedBooks();
```

### 2. Database Mapping
**Book.cs** ? **Sach Table**
- `Title` ? `TieuDe`
- `Author` ? `TenTacGia` (via Sach_TacGia)
- `FilePath` ? `DuongDanFile`
- `FileType` ? `DinhDang`
- `Progress` ? Calculated from `TrangHienTai/TongSoTrang`
- `IsDeleted` ? Checked via ThungRac table presence

### 3. Shelf System
**B?ng**: KeSach  
**Methods**:
```csharp
// Get all shelves for current user
List<string> shelves = DataManager.Instance.GetShelves();

// Add new shelf
DataManager.Instance.AddShelf("My Shelf");

// Remove shelf
DataManager.Instance.RemoveShelf("My Shelf");
```

## ?? Key Methods

### DataManager Methods
```csharp
// Get books
GetAllBooks()           // All books (excluding trash)
GetFavoriteBooks()      // Only favorites
GetDeletedBooks()       // Only in trash
SearchBooks(query)      // Search by title/author

// Manage books
AddBook(book)           // Create new book
UpdateBook(book)        // Update existing book
DeleteBook(id)          // Move to trash
RestoreBook(id)         // Restore from trash
PermanentlyDeleteBook() // Delete permanently

// Favorites
ToggleFavorite(id)      // Toggle favorite status

// Shelves
GetShelves()            // Get all shelves
AddShelf(name)          // Create new shelf
RemoveShelf(name)       // Delete shelf

// User context
SetCurrentUser(userId)  // Set current user
GetCurrentUser()        // Get current user ID
```

### MainForm Methods (Relevant)
```csharp
LoadBooks()             // Reload books view
LoadFavoriteBooks()     // Reload favorites view
LoadTrashBooks()        // Reload trash view
ShowBookMenu()          // Show context menu
SearchBox_TextChanged() // Handle search
ApplySort()             // Apply sorting
```

## ?? Database Schema Summary

```
Tables:
??? Sach (Books) - Main table
??? ThungRac (Trash) - Soft delete marker
??? KeSach (Shelves) - User shelves
??? Sach_TacGia (Book-Author) - Many-to-many
??? Sach_TheLoai (Book-Category) - Many-to-many
??? GhiChu (Notes) - Future feature
??? DanhDauTrang (Bookmarks) - Future feature
??? TacGia (Authors), TheLoai (Categories), etc.

Key Relationships:
- Sach ? ThungRac: 1:0..1 (soft delete marker)
- Sach ? Sach_TacGia ? TacGia (authors)
- Sach ? Sach_TheLoai ? TheLoai (categories)
- Sach ? GhiChu (notes)
- Sach ? DanhDauTrang (bookmarks)
- KeSach ? KeSach_Sach ? Sach (shelves)
```

## ?? SQL Quick Reference

### Check Trash
```sql
SELECT * FROM ThungRac;
SELECT COUNT(*) FROM ThungRac;
```

### Find Book
```sql
SELECT * FROM Sach WHERE TieuDe LIKE '%title%';
SELECT * FROM Sach WHERE MaSach = 1;
```

### Check Authors
```sql
SELECT * FROM TacGia WHERE TenTacGia = 'Author Name';
SELECT * FROM Sach_TacGia WHERE MaSach = 1;
```

### Move to Trash
```sql
INSERT INTO ThungRac (MaSach) VALUES (1);
```

### Restore from Trash
```sql
DELETE FROM ThungRac WHERE MaSach = 1;
```

### Permanent Delete
```sql
DELETE FROM ThungRac WHERE MaSach = 1;
-- Then delete from related tables (see code)
```

### Get Books by User
```sql
SELECT * FROM Sach WHERE MaNguoiDung = 1;
```

### Get Shelves
```sql
SELECT * FROM KeSach WHERE MaNguoiDung = 1;
```

## ?? Breaking Changes

### Before (Old Code)
```csharp
DeleteBook(id)  // Permanently deleted from DB
RestoreBook(id) // Did nothing
GetDeletedBooks() // Returned empty list
GetShelves()    // Returned hardcoded list
```

### After (New Code)
```csharp
DeleteBook(id)  // Moves to ThungRac (soft delete)
RestoreBook(id) // Removes from ThungRac
GetDeletedBooks() // Returns books in ThungRac
GetShelves()    // Returns from KeSach table
```

## ? Verification Steps

1. **Build Solution**: `Ctrl+Shift+B`
   - Should complete without errors

2. **Run Application**: `F5`
   - Should connect to database
   - Should load books
   - Should not show connection error

3. **Test Trash**:
   - Right-click book ? "Move to Trash"
   - Check Trash view
   - Right-click ? "Restore"
   - Should reappear in Books view

4. **Test Database**:
   - Check `SELECT * FROM ThungRac` for deleted books
   - Check `SELECT * FROM Sach` for all books
   - Check `SELECT * FROM KeSach` for shelves

## ??? Debugging Tips

### Enable SQL Logging
```csharp
// Add to DataManager for debugging
string query = @"SELECT ...";
Debug.WriteLine($"SQL: {query}");
Debug.WriteLine($"Param: @userId={currentUserId}");
```

### Check Connection
```csharp
// Verify connection works
if (DatabaseConnection.Instance.TestConnection())
    MessageBox.Show("? Connected!");
else
    MessageBox.Show("? Connection failed!");
```

### Monitor Trash
```sql
-- Run in SSMS to monitor trash changes
SELECT COUNT(*) AS TrashCount FROM ThungRac;
SELECT s.TieuDe, tr.MaSach FROM Sach s 
JOIN ThungRac tr ON s.MaSach = tr.MaSach;
```

## ?? Files Modified

- ? `Book.cs` - Added 3 properties
- ? `DataManager.cs` - Major update (150+ lines changed)
- ? `MainForm.cs` - Updated ShowBookMenu method

## ?? Files Created

- ? `Database\InitializeDefaultShelves.sql`
- ? `Database\Migration_UpdateDatabase.sql`
- ? `README_UPDATES.md`
- ? `SETUP_GUIDE.md`
- ? `QUICK_REFERENCE.md` (this file)

## ?? Learning Resources

- **SOLID Principles**: DataManager uses Singleton pattern
- **SQL Best Practices**: Parameterized queries, proper indexing
- **Design Patterns**: Repository pattern (DataManager)
- **Exception Handling**: Try-catch with user feedback

## ?? Related Files

| File | Purpose |
|------|---------|
| DatabaseConnection.cs | Connection management |
| DataManager.cs | Data access layer |
| Book.cs | Domain model |
| MainForm.cs | UI logic |
| BookCard.cs | Book card UI component |

---

**Quick Start**: 
1. Update connection string
2. Run Migration SQL
3. Press F5
4. Done! ?
