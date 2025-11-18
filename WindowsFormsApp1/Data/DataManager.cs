using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Windows.Forms;

namespace WindowsFormsApp1.Data
{
    public class DataManager
    {
        private static DataManager instance;
        private int currentUserId = 1; // Mặc định user admin (MaNguoiDung = 1)

        public static DataManager Instance
        {
            get
            {
                if (instance == null)
                    instance = new DataManager();
                return instance;
            }
        }

        private DataManager()
        {
            // Test connection
            if (!DatabaseConnection.Instance.TestConnection())
            {
                MessageBox.Show(
                    "Không thể kết nối đến database!\nVui lòng kiểm tra SQL Server và connection string.",
                    "Lỗi kết nối",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        // Lấy tất cả sách của user hiện tại (không bao gồm sách đã xóa)
        public List<Book> GetAllBooks()
        {
            List<Book> books = new List<Book>();
            string query = @"
                SELECT s.MaSach, s.TieuDe, s.MoTa, s.DuongDanAnhBia, 
                       s.DuongDanFile, s.DinhDang, s.TongSoTrang, s.TrangHienTai,
                       s.XepHang, s.YeuThich, s.NgayThem,
                       STRING_AGG(tg.TenTacGia, ', ') AS TacGia
                FROM Sach s
                LEFT JOIN Sach_TacGia stg ON s.MaSach = stg.MaSach
                LEFT JOIN TacGia tg ON stg.MaTacGia = tg.MaTacGia
                WHERE s.MaNguoiDung = @MaNguoiDung
                AND s.MaSach NOT IN (SELECT MaSach FROM ThungRac)
                GROUP BY s.MaSach, s.TieuDe, s.MoTa, s.DuongDanAnhBia, 
                         s.DuongDanFile, s.DinhDang, s.TongSoTrang, s.TrangHienTai,
                         s.XepHang, s.YeuThich, s.NgayThem
                ORDER BY s.NgayThem DESC";

            try
            {
                using (SqlConnection conn = DatabaseConnection.Instance.GetConnection())
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@MaNguoiDung", currentUserId);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                double progress = 0;
                                if (!reader.IsDBNull(reader.GetOrdinal("TongSoTrang")) &&
                                    !reader.IsDBNull(reader.GetOrdinal("TrangHienTai")))
                                {
                                    int totalPages = reader.GetInt32(reader.GetOrdinal("TongSoTrang"));
                                    int currentPage = reader.GetInt32(reader.GetOrdinal("TrangHienTai"));
                                    if (totalPages > 0)
                                        progress = (double)currentPage / totalPages * 100;
                                }

                                books.Add(new Book
                                {
                                    Id = reader.GetInt32(reader.GetOrdinal("MaSach")),
                                    Title = reader.GetString(reader.GetOrdinal("TieuDe")),
                                    Author = reader.IsDBNull(reader.GetOrdinal("TacGia")) ? "Unknown" : reader.GetString(reader.GetOrdinal("TacGia")),
                                    Description = reader.IsDBNull(reader.GetOrdinal("MoTa")) ? "" : reader.GetString(reader.GetOrdinal("MoTa")),
                                    CoverImagePath = reader.IsDBNull(reader.GetOrdinal("DuongDanAnhBia")) ? null : reader.GetString(reader.GetOrdinal("DuongDanAnhBia")),
                                    FilePath = reader.GetString(reader.GetOrdinal("DuongDanFile")),
                                    FileType = reader.IsDBNull(reader.GetOrdinal("DinhDang")) ? "" : reader.GetString(reader.GetOrdinal("DinhDang")),
                                    Progress = progress,
                                    TotalPages = reader.IsDBNull(reader.GetOrdinal("TongSoTrang")) ? 0 : reader.GetInt32(reader.GetOrdinal("TongSoTrang")),
                                    CurrentPage = reader.IsDBNull(reader.GetOrdinal("TrangHienTai")) ? 0 : reader.GetInt32(reader.GetOrdinal("TrangHienTai")),
                                    IsFavorite = reader.GetBoolean(reader.GetOrdinal("YeuThich")),
                                    DateAdded = reader.GetDateTime(reader.GetOrdinal("NgayThem")),
                                    IsDeleted = false
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi lấy danh sách sách: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return books;
        }

        // Lấy sách yêu thích
        public List<Book> GetFavoriteBooks()
        {
            var allBooks = GetAllBooks();
            return allBooks.Where(b => b.IsFavorite).ToList();
        }

        // Lấy sách đã xóa (từ ThungRac)
        public List<Book> GetDeletedBooks()
        {
            List<Book> books = new List<Book>();
            string query = @"
                SELECT s.MaSach, s.TieuDe, s.MoTa, s.DuongDanAnhBia, 
                       s.DuongDanFile, s.DinhDang, s.TongSoTrang, s.TrangHienTai,
                       s.XepHang, s.YeuThich, s.NgayThem,
                       STRING_AGG(tg.TenTacGia, ', ') AS TacGia
                FROM Sach s
                LEFT JOIN Sach_TacGia stg ON s.MaSach = stg.MaSach
                LEFT JOIN TacGia tg ON stg.MaTacGia = tg.MaTacGia
                INNER JOIN ThungRac tr ON s.MaSach = tr.MaSach
                WHERE s.MaNguoiDung = @MaNguoiDung
                GROUP BY s.MaSach, s.TieuDe, s.MoTa, s.DuongDanAnhBia, 
                         s.DuongDanFile, s.DinhDang, s.TongSoTrang, s.TrangHienTai,
                         s.XepHang, s.YeuThich, s.NgayThem";

            try
            {
                using (SqlConnection conn = DatabaseConnection.Instance.GetConnection())
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@MaNguoiDung", currentUserId);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                double progress = 0;
                                if (!reader.IsDBNull(reader.GetOrdinal("TongSoTrang")) &&
                                    !reader.IsDBNull(reader.GetOrdinal("TrangHienTai")))
                                {
                                    int totalPages = reader.GetInt32(reader.GetOrdinal("TongSoTrang"));
                                    int currentPage = reader.GetInt32(reader.GetOrdinal("TrangHienTai"));
                                    if (totalPages > 0)
                                        progress = (double)currentPage / totalPages * 100;
                                }

                                books.Add(new Book
                                {
                                    Id = reader.GetInt32(reader.GetOrdinal("MaSach")),
                                    Title = reader.GetString(reader.GetOrdinal("TieuDe")),
                                    Author = reader.IsDBNull(reader.GetOrdinal("TacGia")) ? "Unknown" : reader.GetString(reader.GetOrdinal("TacGia")),
                                    Description = reader.IsDBNull(reader.GetOrdinal("MoTa")) ? "" : reader.GetString(reader.GetOrdinal("MoTa")),
                                    CoverImagePath = reader.IsDBNull(reader.GetOrdinal("DuongDanAnhBia")) ? null : reader.GetString(reader.GetOrdinal("DuongDanAnhBia")),
                                    FilePath = reader.GetString(reader.GetOrdinal("DuongDanFile")),
                                    FileType = reader.IsDBNull(reader.GetOrdinal("DinhDang")) ? "" : reader.GetString(reader.GetOrdinal("DinhDang")),
                                    Progress = progress,
                                    TotalPages = reader.IsDBNull(reader.GetOrdinal("TongSoTrang")) ? 0 : reader.GetInt32(reader.GetOrdinal("TongSoTrang")),
                                    CurrentPage = reader.IsDBNull(reader.GetOrdinal("TrangHienTai")) ? 0 : reader.GetInt32(reader.GetOrdinal("TrangHienTai")),
                                    IsFavorite = reader.GetBoolean(reader.GetOrdinal("YeuThich")),
                                    DateAdded = reader.GetDateTime(reader.GetOrdinal("NgayThem")),
                                    IsDeleted = true
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi lấy sách đã xóa: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return books;
        }

        // Tìm kiếm sách (không bao gồm sách đã xóa)
        public List<Book> SearchBooks(string query)
        {
            query = query.ToLower();
            return GetAllBooks().Where(b =>
                b.Title.ToLower().Contains(query) ||
                b.Author.ToLower().Contains(query))
                .ToList();
        }

        // Thêm sách mới
        public void AddBook(Book book)
        {
            string query = @"
                INSERT INTO Sach (MaNguoiDung, TieuDe, MoTa, DuongDanFile, DinhDang, 
                                  TongSoTrang, TrangHienTai, YeuThich, NgayThem)
                VALUES (@MaNguoiDung, @TieuDe, @MoTa, @DuongDanFile, @DinhDang, 
                        @TongSoTrang, @TrangHienTai, @YeuThich, @NgayThem);
                
                SELECT CAST(SCOPE_IDENTITY() as int);";

            try
            {
                using (SqlConnection conn = DatabaseConnection.Instance.GetConnection())
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@MaNguoiDung", currentUserId);
                        cmd.Parameters.AddWithValue("@TieuDe", book.Title ?? "Untitled");
                        cmd.Parameters.AddWithValue("@MoTa", (object)book.Description ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@DuongDanFile", book.FilePath ?? "");
                        cmd.Parameters.AddWithValue("@DinhDang", (object)book.FileType ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@TongSoTrang", book.TotalPages);
                        cmd.Parameters.AddWithValue("@TrangHienTai", book.CurrentPage);
                        cmd.Parameters.AddWithValue("@YeuThich", book.IsFavorite);
                        cmd.Parameters.AddWithValue("@NgayThem", DateTime.Now);

                        int newId = (int)cmd.ExecuteScalar();
                        book.Id = newId;

                        // Thêm tác giả nếu có
                        if (!string.IsNullOrEmpty(book.Author) && book.Author != "Unknown Author")
                        {
                            AddOrGetAuthor(book.Author, newId, conn);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi thêm sách: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Thêm hoặc lấy tác giả
        private void AddOrGetAuthor(string authorName, int bookId, SqlConnection conn)
        {
            try
            {
                // Kiểm tra tác giả đã tồn tại chưa
                string checkQuery = "SELECT MaTacGia FROM TacGia WHERE TenTacGia = @TenTacGia";
                int authorId = 0;

                using (SqlCommand cmd = new SqlCommand(checkQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@TenTacGia", authorName);
                    object result = cmd.ExecuteScalar();

                    if (result != null)
                    {
                        authorId = (int)result;
                    }
                    else
                    {
                        // Thêm tác giả mới
                        string insertQuery = "INSERT INTO TacGia (TenTacGia) VALUES (@TenTacGia); SELECT CAST(SCOPE_IDENTITY() as int);";
                        using (SqlCommand insertCmd = new SqlCommand(insertQuery, conn))
                        {
                            insertCmd.Parameters.AddWithValue("@TenTacGia", authorName);
                            authorId = (int)insertCmd.ExecuteScalar();
                        }
                    }
                }

                // Liên kết sách với tác giả
                string linkQuery = "IF NOT EXISTS (SELECT 1 FROM Sach_TacGia WHERE MaSach = @MaSach AND MaTacGia = @MaTacGia) " +
                                 "INSERT INTO Sach_TacGia (MaSach, MaTacGia) VALUES (@MaSach, @MaTacGia)";
                using (SqlCommand cmd = new SqlCommand(linkQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@MaSach", bookId);
                    cmd.Parameters.AddWithValue("@MaTacGia", authorId);
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi thêm tác giả: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Cập nhật sách
        public void UpdateBook(Book book)
        {
            string query = @"
                UPDATE Sach 
                SET TrangHienTai = @TrangHienTai,
                    YeuThich = @YeuThich,
                    TieuDe = @TieuDe
                WHERE MaSach = @MaSach AND MaNguoiDung = @MaNguoiDung";

            try
            {
                using (SqlConnection conn = DatabaseConnection.Instance.GetConnection())
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@MaSach", book.Id);
                        cmd.Parameters.AddWithValue("@MaNguoiDung", currentUserId);
                        cmd.Parameters.AddWithValue("@TrangHienTai", book.CurrentPage);
                        cmd.Parameters.AddWithValue("@YeuThich", book.IsFavorite);
                        cmd.Parameters.AddWithValue("@TieuDe", book.Title ?? "Untitled");
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi cập nhật sách: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Di chuyển sách vào thùng rác (soft delete)
        public void DeleteBook(int bookId)
        {
            string query = "INSERT INTO ThungRac (MaSach) VALUES (@MaSach)";

            try
            {
                using (SqlConnection conn = DatabaseConnection.Instance.GetConnection())
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@MaSach", bookId);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi xóa sách: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Restore sách từ thùng rác
        public void RestoreBook(int bookId)
        {
            string query = "DELETE FROM ThungRac WHERE MaSach = @MaSach";

            try
            {
                using (SqlConnection conn = DatabaseConnection.Instance.GetConnection())
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@MaSach", bookId);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi khôi phục sách: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Xóa vĩnh viễn sách từ database
        public void PermanentlyDeleteBook(int bookId)
        {
            string query = @"
                DELETE FROM ThungRac WHERE MaSach = @MaSach;
                DELETE FROM Sach_TacGia WHERE MaSach = @MaSach;
                DELETE FROM Sach_TheLoai WHERE MaSach = @MaSach;
                DELETE FROM KeSach_Sach WHERE MaSach = @MaSach;
                DELETE FROM GhiChu WHERE MaSach = @MaSach;
                DELETE FROM DanhDauTrang WHERE MaSach = @MaSach;
                DELETE FROM Sach WHERE MaSach = @MaSach;";

            try
            {
                using (SqlConnection conn = DatabaseConnection.Instance.GetConnection())
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@MaSach", bookId);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi xóa vĩnh viễn: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Toggle yêu thích
        public void ToggleFavorite(int bookId)
        {
            string query = "UPDATE Sach SET YeuThich = ~YeuThich WHERE MaSach = @MaSach AND MaNguoiDung = @MaNguoiDung";

            try
            {
                using (SqlConnection conn = DatabaseConnection.Instance.GetConnection())
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@MaSach", bookId);
                        cmd.Parameters.AddWithValue("@MaNguoiDung", currentUserId);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi cập nhật yêu thích: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Lấy danh sách shelf
        public List<string> GetShelves()
        {
            List<string> shelves = new List<string>();
            string query = "SELECT TenKeSach FROM KeSach WHERE MaNguoiDung = @MaNguoiDung ORDER BY NgayTao";

            try
            {
                using (SqlConnection conn = DatabaseConnection.Instance.GetConnection())
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@MaNguoiDung", currentUserId);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                shelves.Add(reader.GetString(0));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi lấy danh sách shelf: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            // Nếu chưa có shelf nào, thêm default shelf
            if (shelves.Count == 0)
            {
                shelves.Add("All Books");
            }

            return shelves;
        }

        public void AddShelf(string shelfName)
        {
            string query = @"
                IF NOT EXISTS (SELECT 1 FROM KeSach WHERE MaNguoiDung = @MaNguoiDung AND TenKeSach = @TenKeSach)
                INSERT INTO KeSach (MaNguoiDung, TenKeSach, NgayTao)
                VALUES (@MaNguoiDung, @TenKeSach, @NgayTao)";

            try
            {
                using (SqlConnection conn = DatabaseConnection.Instance.GetConnection())
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@MaNguoiDung", currentUserId);
                        cmd.Parameters.AddWithValue("@TenKeSach", shelfName);
                        cmd.Parameters.AddWithValue("@NgayTao", DateTime.Now);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi thêm shelf: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void RemoveShelf(string shelfName)
        {
            string query = "DELETE FROM KeSach WHERE MaNguoiDung = @MaNguoiDung AND TenKeSach = @TenKeSach";

            try
            {
                using (SqlConnection conn = DatabaseConnection.Instance.GetConnection())
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@MaNguoiDung", currentUserId);
                        cmd.Parameters.AddWithValue("@TenKeSach", shelfName);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi xóa shelf: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Đặt user hiện tại
        public void SetCurrentUser(int userId)
        {
            currentUserId = userId;
        }

        // Lấy user hiện tại
        public int GetCurrentUser()
        {
            return currentUserId;
        }
    }
}