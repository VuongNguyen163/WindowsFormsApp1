using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using VersOne.Epub; // Yêu cầu cài NuGet Package: VersOne.Epub

namespace WindowsFormsApp1.Data
{
    /// <summary>
    /// DTO đơn giản để chứa nội dung 1 chương
    /// </summary>
    public class BookChapter
    {
        public int ChapterNumber { get; set; }
        public string ChapterTitle { get; set; }
        public string Content { get; set; }
    }

    public class BookReaderService
    {
        private readonly DataManager _dataManager;

        public BookReaderService(DataManager dataManager)
        {
            _dataManager = dataManager;
        }

        #region Xử lý Vị Trí Đọc (Database)

        /// <summary>
        /// Lưu vị trí đọc vào bảng VT_DocSach VÀ cập nhật TrangHienTai trong bảng Sach
        /// </summary>
        public void SaveReadingPosition(int bookId, int userId, int chapterNumber, int positionInChapter)
        {
            try
            {
                using (var conn = DatabaseConnection.Instance.GetConnection())
                {
                    conn.Open();

                    // Query kết hợp:
                    // 1. Update hoặc Insert vào VT_DocSach (lưu chi tiết vị trí cuộn chuột)
                    // 2. Update TrangHienTai trong bảng Sach (để hiển thị % tiến độ ở list chính)
                    string query = @"
                        -- Update bảng chi tiết vị trí
                        IF EXISTS (SELECT 1 FROM VT_DocSach WHERE MaSach = @Bid AND MaNguoiDung = @Uid)
                        BEGIN
                            UPDATE VT_DocSach 
                            SET SoChap = @Ch, ViTriTrongChap = @Pos, NgayCapNhat = GETDATE()
                            WHERE MaSach = @Bid AND MaNguoiDung = @Uid
                        END
                        ELSE
                        BEGIN
                            INSERT INTO VT_DocSach (MaSach, MaNguoiDung, SoChap, ViTriTrongChap, NgayCapNhat)
                            VALUES (@Bid, @Uid, @Ch, @Pos, GETDATE())
                        END;

                        -- Update bảng Sach để tính % tiến độ
                        UPDATE Sach SET TrangHienTai = @Ch WHERE MaSach = @Bid";

                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Bid", bookId);
                        cmd.Parameters.AddWithValue("@Uid", userId);
                        cmd.Parameters.AddWithValue("@Ch", chapterNumber);
                        cmd.Parameters.AddWithValue("@Pos", positionInChapter);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                // Log lỗi nhẹ, không hiện MessageBox để tránh làm phiền khi đang scroll
                Console.WriteLine($"Lỗi lưu vị trí: {ex.Message}");
            }
        }

        /// <summary>
        /// Lấy vị trí đọc lần cuối
        /// </summary>
        /// <returns>Tuple (ChapterIndex, ScrollPosition)</returns>
        public (int chapter, int position) GetReadingPosition(int bookId, int userId)
        {
            try
            {
                string query = "SELECT SoChap, ViTriTrongChap FROM VT_DocSach WHERE MaSach = @Bid AND MaNguoiDung = @Uid";

                using (var conn = DatabaseConnection.Instance.GetConnection())
                {
                    conn.Open();
                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Bid", bookId);
                        cmd.Parameters.AddWithValue("@Uid", userId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                int ch = reader.GetInt32(0); // SoChap
                                int pos = reader.GetInt32(1); // ViTriTrongChap
                                return (ch, pos);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi lấy vị trí: {ex.Message}");
            }

            return (0, 0); // Mặc định chương 0, vị trí 0
        }

        #endregion

        #region Xử lý Nội dung Sách (File)

        /// <summary>
        /// Điều phối đọc nội dung dựa trên extension
        /// </summary>
        public List<BookChapter> ReadBookContent(Book book)
        {
            if (book == null || string.IsNullOrEmpty(book.FilePath))
                return new List<BookChapter>();

            string ext = Path.GetExtension(book.FilePath).ToLower();

            switch (ext)
            {
                case ".epub":
                    return ReadEpubContent(book.FilePath);
                case ".txt":
                    return ReadTxtContent(book.FilePath);
                case ".pdf":
                    return GetPlaceholderContent("PDF Reader", "Hiện tại ứng dụng chưa hỗ trợ render PDF trực tiếp. Vui lòng mở bằng ứng dụng hệ thống.");
                case ".mobi":
                    return GetPlaceholderContent("MOBI Reader", "Định dạng MOBI chưa được hỗ trợ. Vui lòng convert sang EPUB.");
                default:
                    return GetPlaceholderContent("Unknown Format", "Định dạng file không được hỗ trợ.");
            }
        }

        private List<BookChapter> ReadEpubContent(string filePath)
        {
            List<BookChapter> chapters = new List<BookChapter>();
            try
            {
                EpubBook epub = EpubReader.ReadBook(filePath);

                // Nếu EPUB có ReadingOrder (chuẩn), dùng nó
                if (epub.ReadingOrder != null && epub.ReadingOrder.Count > 0)
                {
                    int index = 0;
                    foreach (var item in epub.ReadingOrder)
                    {
                        string content = HtmlToPlainText(item.Content);
                        if (!string.IsNullOrWhiteSpace(content))
                        {
                            chapters.Add(new BookChapter
                            {
                                ChapterNumber = index++,
                                ChapterTitle = $"Section {index}", // Có thể cải thiện để lấy title từ TOC
                                Content = content
                            });
                        }
                    }
                }
                else
                {
                    // Fallback nếu cấu trúc lạ
                    chapters.Add(new BookChapter
                    {
                        ChapterNumber = 0,
                        ChapterTitle = "Full Content",
                        Content = "Không thể phân tách chương cho file EPUB này."
                    });
                }
            }
            catch (Exception ex)
            {
                chapters.Add(new BookChapter
                {
                    ChapterNumber = 0,
                    ChapterTitle = "Error",
                    Content = $"Lỗi đọc EPUB: {ex.Message}"
                });
            }
            return chapters;
        }

        private List<BookChapter> ReadTxtContent(string filePath)
        {
            List<BookChapter> chapters = new List<BookChapter>();
            try
            {
                string text = File.ReadAllText(filePath, Encoding.UTF8);

                // Cắt file TXT lớn thành các chương giả (ví dụ mỗi 5000 ký tự) để UI không bị lag
                int chunkSize = 5000;
                int length = text.Length;
                int count = 0;

                for (int i = 0; i < length; i += chunkSize)
                {
                    int len = Math.Min(chunkSize, length - i);
                    chapters.Add(new BookChapter
                    {
                        ChapterNumber = count,
                        ChapterTitle = $"Phần {count + 1}",
                        Content = text.Substring(i, len)
                    });
                    count++;
                }
            }
            catch (Exception ex)
            {
                chapters.Add(new BookChapter { ChapterNumber = 0, ChapterTitle = "Error", Content = ex.Message });
            }
            return chapters;
        }

        // Helper: Chuyển HTML sang Text thuần
        private string HtmlToPlainText(string html)
        {
            if (string.IsNullOrEmpty(html)) return "";

            // Decode ký tự HTML (&nbsp;, &lt;, ...)
            string text = System.Net.WebUtility.HtmlDecode(html);

            // Xóa thẻ script và style
            text = Regex.Replace(text, @"<script[^>]*>.*?</script>", "", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            text = Regex.Replace(text, @"<style[^>]*>.*?</style>", "", RegexOptions.IgnoreCase | RegexOptions.Singleline);

            // Thay thế thẻ <br>, <p>, <div> bằng xuống dòng
            text = Regex.Replace(text, @"<br\s*/?>", "\n", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"</p>", "\n\n", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"</div>", "\n", RegexOptions.IgnoreCase);

            // Xóa tất cả thẻ HTML còn lại
            text = Regex.Replace(text, @"<[^>]+>", "");

            // Xóa dòng trống thừa
            text = Regex.Replace(text, @"^\s*$\n|\r", "", RegexOptions.Multiline);

            return text.Trim();
        }

        private List<BookChapter> GetPlaceholderContent(string title, string message)
        {
            return new List<BookChapter>
            {
                new BookChapter { ChapterNumber = 0, ChapterTitle = title, Content = message }
            };
        }

        #endregion

        #region Tiện ích mở rộng

        /// <summary>
        /// Ước tính tổng số trang của sách dựa trên độ dài nội dung.
        /// Quy ước: 1 trang ~ 2000 ký tự.
        /// </summary>
        public int EstimateTotalPages(string filePath)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) return 0;

                string ext = Path.GetExtension(filePath).ToLower();
                long totalCharacters = 0;

                if (ext == ".epub")
                {
                    EpubBook epub = EpubReader.ReadBook(filePath);
                    if (epub.ReadingOrder != null)
                    {
                        foreach (var item in epub.ReadingOrder)
                        {
                            string text = HtmlToPlainText(item.Content);
                            totalCharacters += text.Length;
                        }
                    }
                }
                else if (ext == ".txt")
                {
                    string text = File.ReadAllText(filePath);
                    totalCharacters = text.Length;
                }

                // Ước tính: 2000 ký tự = 1 trang
                if (totalCharacters == 0) return 0;
                return (int)Math.Ceiling(totalCharacters / 2000.0);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi tính số trang: {ex.Message}");
                return 0;
            }
        }

        #endregion
    }
}