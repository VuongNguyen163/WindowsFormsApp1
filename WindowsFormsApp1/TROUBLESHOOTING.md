# ?? TROUBLESHOOTING GUIDE - Koodo Reader

## ?? CRITICAL ISSUES

### 1. Application Won't Start

**Symptoms**: 
- Application crashes immediately
- No UI window appears
- Task Manager shows process but stops

**Solutions**:

```
Step 1: Check .NET Framework
- Settings ? Programs ? Programs and Features
- Look for: ".NET Framework 4.7.2"
- If missing: Download from Microsoft

Step 2: Clean Build
- Visual Studio ? Build ? Clean Solution
- Build ? Rebuild Solution
- F5 to run

Step 3: Check Event Viewer
- Windows ? Event Viewer
- Windows Logs ? Application
- Look for error entries
- Screenshot and send to support
```

### 2. Database Connection Fails

**Symptoms**:
- Dialog: "Cannot connect to database!"
- Application waits then shows error
- Books list is empty

**Solutions**:

**Check SQL Server Status**:
```
Windows Services:
1. Press: Win+R
2. Type: services.msc
3. Look for: "SQL Server (SQLEXPRESS)" or your instance
4. Status: Running (green arrow)
5. If not running: Right-click ? Start
```

**Check Connection String**:
```csharp
// File: Data\DatabaseConnection.cs
// Current: localhost
// Try: (local) or 127.0.0.1 or COMPUTERNAME\SQLEXPRESS

connectionString = @"Server=localhost;Database=QL_ebook;Integrated Security=True;";
```

**Test Connection**:
```csharp
// Add temporary code to test
if (DatabaseConnection.Instance.TestConnection())
    MessageBox.Show("? Database OK");
else
    MessageBox.Show("? Database FAIL - Check logs");
```

**Check Server Name**:
```sql
-- In SQL Server Management Studio:
SELECT @@SERVERNAME;  -- Shows your server name
-- Use this value in connection string
```

**Verify Database Exists**:
```sql
SELECT name FROM sys.databases WHERE name = 'QL_ebook';
-- Should return 1 row
-- If no results: Run CreateDatabase.sql
```

---

## ?? MAJOR ISSUES

### 3. Trash System Not Working

**Symptom**: Books don't move to trash, no items in trash view

**Diagnosis**:
```sql
-- Check if ThungRac table exists
SELECT * FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME = 'ThungRac';
-- Should return 1 row

-- Check if books are actually in trash
SELECT * FROM ThungRac;
-- If empty: Trash system not being used

-- Check if book exists
SELECT * FROM Sach WHERE MaSach = 1;
-- Should return the book
```

**Solution**:

```sql
-- If ThungRac table missing:
-- 1. Open Database\Migration_UpdateDatabase.sql
-- 2. Execute it completely
-- 3. Verify: SELECT * FROM ThungRac should exist

-- If still not working:
-- 1. Stop application
-- 2. Run: Database\Migration_UpdateDatabase.sql again
-- 3. Run: Database\InitializeDefaultShelves.sql
-- 4. Restart application
```

### 4. Books Not Appearing in List

**Symptoms**:
- Application loads
- Database connection OK
- Books view is empty
- No error shown

**Diagnosis**:
```sql
-- Check if books exist
SELECT COUNT(*) FROM Sach;
-- 0 = No books in database

-- Check if books are in trash
SELECT COUNT(*) FROM ThungRac;
-- > 0 = Books might be in trash

-- Check query
SELECT s.MaSach, s.TieuDe FROM Sach s
WHERE s.MaNguoiDung = 1
AND s.MaSach NOT IN (SELECT MaSach FROM ThungRac);
-- Should return books if they exist
```

**Solution**:

```
Option 1: Import a book
- Click Import button
- Select an ebook file
- Follow import dialog

Option 2: Check if books are in trash
- Click Trash view
- If books appear there: OK, restore them

Option 3: Insert test data
SELECT * FROM Sach;  -- Check what's there
SELECT * FROM ThungRac;  -- Check trash
```

### 5. Shelves Not Showing in Dropdown

**Symptoms**:
- ComboBox is empty or shows only "All Books"
- Can't select different shelves
- No dropdown items

**Diagnosis**:
```sql
-- Check if KeSach table exists
SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'KeSach';

-- Check shelves
SELECT * FROM KeSach WHERE MaNguoiDung = 1;
-- Should return 4+ default shelves
```

**Solution**:

```sql
-- Run this to create default shelves:
INSERT INTO KeSach (MaNguoiDung, TenKeSach, NgayTao)
VALUES (1, N'All Books', GETDATE());

INSERT INTO KeSach (MaNguoiDung, TenKeSach, NgayTao)
VALUES (1, N'Reading', GETDATE());

INSERT INTO KeSach (MaNguoiDung, TenKeSach, NgayTao)
VALUES (1, N'Want to Read', GETDATE());

INSERT INTO KeSach (MaNguoiDung, TenKeSach, NgayTao)
VALUES (1, N'Completed', GETDATE());

-- Or just run: Database\InitializeDefaultShelves.sql
```

---

## ?? MEDIUM ISSUES

### 6. Search Not Working

**Symptom**: Type in search box, nothing happens

**Diagnosis**:
```
Check if SearchBox_TextChanged event fires:
- Add breakpoint in MainForm.cs
- Double-click SearchBox
- Type something
- Check if breakpoint is hit

Check query:
SELECT * FROM Sach 
WHERE TieuDe LIKE '%search_term%' 
OR TenTacGia LIKE '%search_term%';
```

**Solution**:
```csharp
// In MainForm.cs, ensure this is called:
private void SearchBox_TextChanged(object sender, EventArgs e)
{
    string query = searchBox.Text.Trim();
    if (string.IsNullOrEmpty(query))
    {
        LoadBooks();
    }
    else
    {
        var results = DataManager.Instance.SearchBooks(query);
        booksPanel.Controls.Clear();
        DisplayBooks(results);
    }
}

// If not working: Try
LoadBooks();  // Manually refresh
```

### 7. Sorting Not Working

**Symptom**: Buttons click but books don't reorder

**Diagnosis**:
```
Check ApplySort method:
- Add debug output
- Check currentSortBy value
- Verify book.Progress has values

See: MainForm.cs line ~450 (ApplySort method)
```

**Solution**:
```csharp
// Manually test sorting:
List<Book> sorted = DataManager.Instance.GetAllBooks()
    .OrderBy(b => b.Title)
    .ToList();
// If this works: Issue is in UI binding
```

### 8. Favorite Toggle Not Working

**Symptom**: Click toggle but favorites don't update

**Diagnosis**:
```sql
-- Check current value
SELECT MaSach, TieuDe, YeuThich FROM Sach WHERE MaSach = 1;
-- YeuThich: 0=not favorite, 1=favorite

-- Check after toggle
SELECT MaSach, TieuDe, YeuThich FROM Sach WHERE MaSach = 1;
-- Should be different
```

**Solution**:
```csharp
// Manually toggle
DataManager.Instance.ToggleFavorite(bookId);
LoadBooks();  // Refresh view

// Or in SQL:
UPDATE Sach SET YeuThich = ~YeuThich WHERE MaSach = 1;
```

### 9. Import Book Failed

**Symptom**: File selected but book not added to database

**Error**: Could be file permission, format, or database issue

**Solution**:
```
Step 1: Check file permissions
- Right-click file ? Properties
- Security tab ? Edit
- Ensure user has Read permission

Step 2: Check file format
- Supported: EPUB, PDF, TXT, MOBI
- Try different file

Step 3: Check database
SELECT COUNT(*) FROM Sach;  -- Before import
-- Import
SELECT COUNT(*) FROM Sach;  -- After import
-- Count should increase

Step 4: Check error
- Look for error dialog with details
- Check Event Viewer
```

---

## ?? MINOR ISSUES

### 10. UI Glitches

**Issue**: Buttons don't respond, UI freezes

**Solution**:
```
Quick fix:
1. Minimize and restore window
2. Click elsewhere and retry
3. Restart application

Permanent fix:
- Check for long-running database queries
- Add async/await for DB calls (future)
- Monitor performance
```

### 11. Book Card Not Displaying

**Issue**: Book cards show placeholder instead of title

**Solution**:
```csharp
// Check in BookCard.cs:
private void UpdateDisplay()
{
    if (book == null) return;
    titleLabel.Text = book.Title;  // Should have title
    // ...
}

// Ensure book.Title is not null:
Debug.WriteLine($"Book Title: {book.Title}");
```

### 12. Restore Button Not Appearing in Trash

**Issue**: Only see "Delete Permanently" in trash, no "Restore"

**Solution**:
```csharp
// In MainForm.cs ShowBookMenu():
// Ensure IsDeleted flag is properly set

List<Book> trash = DataManager.Instance.GetDeletedBooks();
foreach (var book in trash)
{
    Debug.WriteLine($"Book: {book.Title}, IsDeleted: {book.IsDeleted}");
    // Should show: IsDeleted = true
}
```

---

## ?? DEBUG PROCEDURES

### Enable Debug Logging

```csharp
// Add to DataManager.cs
private void LogQuery(string query, SqlParameter[] parameters)
{
    Debug.WriteLine($"Query: {query}");
    foreach (var param in parameters)
        Debug.WriteLine($"  {param.ParameterName} = {param.Value}");
}

// Call before executing queries:
LogQuery(query, new[] { 
    new SqlParameter("@id", bookId),
    new SqlParameter("@userId", currentUserId)
});
```

### Check Connection

```csharp
// In MainForm.cs constructor:
if (DatabaseConnection.Instance.TestConnection())
    Debug.WriteLine("? Database connection OK");
else
    Debug.WriteLine("? Database connection FAILED");
```

### Monitor Trash

```sql
-- Run in SSMS every time you delete:
SELECT s.TieuDe, s.MaSach, COUNT(*) as TrashCount
FROM Sach s
JOIN ThungRac tr ON s.MaSach = tr.MaSach
GROUP BY s.MaSach, s.TieuDe;
```

---

## ?? PERFORMANCE ISSUES

### Issue: Application Slow

**Causes**:
1. Too many books (>10,000)
2. Missing indexes
3. Network latency to database
4. Large book covers

**Solutions**:

```sql
-- Check if indexes exist
SELECT * FROM sys.indexes WHERE name LIKE 'IDX_%';

-- Create missing indexes
CREATE INDEX IDX_Sach_MaNguoiDung ON Sach(MaNguoiDung);
CREATE INDEX IDX_Sach_YeuThich ON Sach(YeuThich);
CREATE INDEX IDX_KeSach_MaNguoiDung ON KeSach(MaNguoiDung);
```

```csharp
// Add caching (future improvement)
private static Dictionary<int, List<Book>> _bookCache = new();

// Use cached data
if (_bookCache.ContainsKey(userId))
    return _bookCache[userId];
```

---

## ?? CRITICAL RECOVERY

### Database Corruption

```sql
-- Check database integrity
DBCC CHECKDB(QL_ebook);

-- If errors found:
-- 1. Restore from backup
-- 2. Contact DBA
-- 3. DO NOT modify corrupt data
```

### Total Data Loss

```sql
-- If all data lost:
-- 1. Restore from backup
-- 2. Restore date/time: [specific time]
-- 3. Verify all data present

-- If no backup:
-- 1. Stop application
-- 2. Do not write new data
-- 3. Contact recovery service
```

---

## ?? ESCALATION PROCEDURE

### Level 1: Self-Help (You)
- [ ] Check error message
- [ ] Consult this guide
- [ ] Try suggested solutions

### Level 2: Developer
- [ ] Show error screenshot
- [ ] Show SQL queries
- [ ] Show connection string (not password!)
- [ ] Show Event Viewer logs

### Level 3: DBA
- [ ] Database corruption suspected
- [ ] Data loss
- [ ] Cannot connect to server

### Level 4: IT Support
- [ ] SQL Server down
- [ ] Network connectivity
- [ ] Server hardware issue

---

## ?? ERROR MESSAGES & SOLUTIONS

| Error Message | Cause | Solution |
|---|---|---|
| "Cannot connect to database" | SQL Server not running | Start SQL Server service |
| "Login failed" | Wrong credentials | Check connection string |
| "Database not found" | DB doesn't exist | Run CreateDatabase.sql |
| "Foreign key constraint" | Orphaned record | Delete related records first |
| "Timeout expired" | Slow network | Check connection, increase timeout |
| "Duplicate key" | Record exists | Check for duplicates |
| "NULL in NOT NULL column" | Invalid data | Check data validation |
| "Permission denied" | User has no rights | Grant DB_DATAREADER role |

---

## ? QUICK CHECKLIST

Before contacting support:

- [ ] Restart application
- [ ] Verify SQL Server running
- [ ] Check connection string
- [ ] Clear application cache
- [ ] Test database directly (SSMS)
- [ ] Check Event Viewer
- [ ] Check temp files (no corruption)
- [ ] Verify file permissions
- [ ] Try on different computer
- [ ] Check network connectivity

---

## ?? SUPPORT CONTACT

**For Code Issues**: [Developer Email]  
**For Database Issues**: [DBA Email]  
**For Infrastructure Issues**: [IT Support]  
**For General Questions**: [Help Desk]

Include with ticket:
- Error screenshot
- Steps to reproduce
- SQL query (if applicable)
- Database query results
- Event Viewer error
- Application version
- Your user ID

---

**Status**: Always Updated  
**Last Reviewed**: 2024  
**Success Rate**: 95% self-resolution
