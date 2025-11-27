using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Drawing; // Để dùng class Image
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using VersOne.Epub;     // NuGet: VersOne.Epub
using UglyToad.PdfPig;  // NuGet: PdfPig
using UglyToad.PdfPig.Content; // Để dùng IPdfImage
using System.Text;

namespace WindowsFormsApp1.Data
{
    public class BookChapter
    {
        public int ChapterNumber { get; set; }
        public string ChapterTitle { get; set; }
        public string Content { get; set; }
        // Lưu trữ ảnh của chương này (Key: mã placeholder, Value: dữ liệu ảnh)
        public Dictionary<string, Image> Images { get; set; } = new Dictionary<string, Image>();
    }

    public class BookReaderService
    {
        private readonly DataManager _dataManager;

        public BookReaderService(DataManager dataManager)
        {
            _dataManager = dataManager;
        }

        #region Database Methods (Lưu vị trí đọc)

        public void SaveReadingPosition(int bookId, int userId, int chapterNumber, int positionInChapter)
        {
            try
            {
                using (var conn = DatabaseConnection.Instance.GetConnection())
                {
                    conn.Open();
                    // Upsert: Cập nhật nếu có, chèn mới nếu chưa
                    string query = @"
                        IF EXISTS (SELECT 1 FROM VT_DocSach WHERE MaSach = @Bid AND MaNguoiDung = @Uid)
                        BEGIN
                            UPDATE VT_DocSach SET SoChap = @Ch, ViTriTrongChap = @Pos, NgayCapNhat = GETDATE()
                            WHERE MaSach = @Bid AND MaNguoiDung = @Uid
                        END
                        ELSE
                        BEGIN
                            INSERT INTO VT_DocSach (MaSach, MaNguoiDung, SoChap, ViTriTrongChap, NgayCapNhat)
                            VALUES (@Bid, @Uid, @Ch, @Pos, GETDATE())
                        END;
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
            catch (Exception ex) { Console.WriteLine($"Lỗi lưu vị trí: {ex.Message}"); }
        }

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
                            if (reader.Read()) return (reader.GetInt32(0), reader.GetInt32(1));
                        }
                    }
                }
            }
            catch { }
            return (0, 0);
        }

        #endregion

        #region Content Reading (Main)

        public List<BookChapter> ReadBookContent(Book book)
        {
            if (book == null || string.IsNullOrEmpty(book.FilePath)) return new List<BookChapter>();
            string ext = Path.GetExtension(book.FilePath).ToLower();

            if (ext == ".epub") return ReadEpubContent(book.FilePath);
            if (ext == ".txt") return ReadTxtContent(book.FilePath);
            if (ext == ".pdf") return ReadPdfContent(book.FilePath);

            return GetPlaceholderContent("Định dạng không hỗ trợ", "Chỉ hỗ trợ EPUB, PDF và TXT.");
        }

        #endregion

        #region Format Specific Readers (Epub, PDF, Txt)

        private List<BookChapter> ReadEpubContent(string filePath)
        {
            List<BookChapter> rawChapters = new List<BookChapter>();
            try
            {
                EpubBook epub = EpubReader.ReadBook(filePath);

                // Map Title từ Navigation
                Dictionary<string, string> titleMap = new Dictionary<string, string>();
                if (epub.Navigation != null)
                {
                    foreach (var nav in epub.Navigation)
                    {
                        string key = nav.Link?.ContentFilePath;
                        if (!string.IsNullOrEmpty(key) && !titleMap.ContainsKey(key))
                            titleMap[key] = nav.Title;
                    }
                }

                if (epub.ReadingOrder != null)
                {
                    int index = 1;
                    foreach (var item in epub.ReadingOrder)
                    {
                        var parsedData = ParseHtmlContent(item.Content, epub);

                        if (!string.IsNullOrWhiteSpace(parsedData.text) || parsedData.images.Count > 0)
                        {
                            string chapterTitle = "Chương " + index;
                            if (titleMap.ContainsKey(item.Key)) chapterTitle = titleMap[item.Key];

                            rawChapters.Add(new BookChapter
                            {
                                ChapterTitle = chapterTitle,
                                Content = parsedData.text,
                                Images = parsedData.images
                            });
                            index++;
                        }
                    }
                }
                return MergeShortChapters(rawChapters);
            }
            catch (Exception ex)
            {
                return GetPlaceholderContent("Lỗi", $"Không đọc được file: {ex.Message}");
            }
        }

        private List<BookChapter> ReadPdfContent(string filePath)
        {
            List<BookChapter> chapters = new List<BookChapter>();
            try
            {
                using (var pdf = PdfDocument.Open(filePath))
                {
                    for (int i = 1; i <= pdf.NumberOfPages; i++)
                    {
                        var page = pdf.GetPage(i);
                        string text = page.Text;
                        var images = new Dictionary<string, Image>();

                        // Lấy ảnh từ PDF
                        foreach (var img in page.GetImages())
                        {
                            try
                            {
                                byte[] imgBytes = null;
                                if (img.TryGetPng(out byte[] png)) imgBytes = png;
                                else imgBytes = img.RawBytes.ToArray();

                                if (imgBytes != null)
                                {
                                    using (var ms = new MemoryStream(imgBytes))
                                    {
                                        // Clone Bitmap để tránh lỗi GDI+ khi stream đóng
                                        using (var temp = Image.FromStream(ms))
                                        {
                                            Image image = new Bitmap(temp);
                                            string key = $"{{{{IMG:{Guid.NewGuid()}}}}}";
                                            images.Add(key, image);
                                            text += $"\n\n{key}\n\n";
                                        }
                                    }
                                }
                            }
                            catch { }
                        }

                        chapters.Add(new BookChapter
                        {
                            ChapterNumber = i - 1,
                            ChapterTitle = $"Trang {i}",
                            Content = text,
                            Images = images
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                return GetPlaceholderContent("Lỗi đọc PDF", ex.Message);
            }
            return chapters;
        }

        private List<BookChapter> ReadTxtContent(string filePath)
        {
            List<BookChapter> chapters = new List<BookChapter>();
            try
            {
                string text = File.ReadAllText(filePath, Encoding.UTF8);
                int idealLength = 8000;
                int start = 0;
                int count = 1;

                while (start < text.Length)
                {
                    int length = Math.Min(idealLength, text.Length - start);
                    int end = start + length;

                    if (end < text.Length)
                    {
                        int lastNewLine = text.LastIndexOf('\n', end, 2000);
                        if (lastNewLine > start) end = lastNewLine + 1;
                    }

                    chapters.Add(new BookChapter
                    {
                        ChapterNumber = count - 1,
                        ChapterTitle = $"Phần {count}",
                        Content = text.Substring(start, end - start).Trim()
                    });
                    start = end; count++;
                }
            }
            catch (Exception ex) { return GetPlaceholderContent("Error", ex.Message); }
            return chapters;
        }

        #endregion

        #region Helpers (Parse, Merge, Estimate)

        private (string text, Dictionary<string, Image> images) ParseHtmlContent(string html, EpubBook epubBook)
        {
            var images = new Dictionary<string, Image>();
            if (string.IsNullOrEmpty(html)) return ("", images);

            string processedText = html;
            var imgRegex = new Regex(@"<img[^>]+src\s*=\s*['""]([^'""]+)['""][^>]*>", RegexOptions.IgnoreCase);

            processedText = imgRegex.Replace(processedText, match =>
            {
                string src = match.Groups[1].Value;
                src = System.Net.WebUtility.UrlDecode(src);
                string fileName = Path.GetFileName(src);

                if (epubBook.Content?.Images?.Local != null)
                {
                    foreach (var imgFile in epubBook.Content.Images.Local)
                    {
                        if (imgFile.FilePath.EndsWith(fileName, StringComparison.OrdinalIgnoreCase))
                        {
                            try
                            {
                                using (var ms = new MemoryStream(imgFile.Content))
                                {
                                    using (var temp = Image.FromStream(ms))
                                    {
                                        Image img = new Bitmap(temp);
                                        string uniqueKey = $"{{{{IMG:{Guid.NewGuid()}}}}}";
                                        images.Add(uniqueKey, img);
                                        return $"\n\n{uniqueKey}\n\n";
                                    }
                                }
                            }
                            catch { }
                        }
                    }
                }
                return "[Ảnh]";
            });

            // Clean HTML
            processedText = System.Net.WebUtility.HtmlDecode(processedText);
            processedText = Regex.Replace(processedText, @"<head>.*?</head>", "", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            processedText = Regex.Replace(processedText, @"<script[^>]*>.*?</script>", "", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            processedText = Regex.Replace(processedText, @"<style[^>]*>.*?</style>", "", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            processedText = Regex.Replace(processedText, @"</?(p|div|h[1-6]|br|li)[^>]*>", "\n", RegexOptions.IgnoreCase);
            processedText = Regex.Replace(processedText, @"<[^>]+>", "");
            processedText = Regex.Replace(processedText, @"(\n\s*){3,}", "\n\n");

            return (processedText.Trim(), images);
        }

        private List<BookChapter> MergeShortChapters(List<BookChapter> input)
        {
            var result = new List<BookChapter>();
            if (input.Count == 0) return result;

            BookChapter currentBuffer = null;
            int chapterCounter = 1;

            foreach (var chapter in input)
            {
                if (currentBuffer == null)
                {
                    currentBuffer = chapter;
                    currentBuffer.ChapterNumber = chapterCounter;
                }
                else
                {
                    bool isShort = currentBuffer.Content.Length < 1000 && currentBuffer.Images.Count < 2;
                    if (isShort)
                    {
                        currentBuffer.Content += "\n\n" + "--- " + (chapter.ChapterTitle ?? "*") + " ---\n\n" + chapter.Content;
                        foreach (var img in chapter.Images)
                        {
                            if (!currentBuffer.Images.ContainsKey(img.Key))
                                currentBuffer.Images.Add(img.Key, img.Value);
                        }
                    }
                    else
                    {
                        result.Add(currentBuffer);
                        chapterCounter++;
                        currentBuffer = chapter;
                        currentBuffer.ChapterNumber = chapterCounter;
                    }
                }
            }
            if (currentBuffer != null) result.Add(currentBuffer);

            for (int i = 0; i < result.Count; i++)
            {
                if (string.IsNullOrEmpty(result[i].ChapterTitle) || result[i].ChapterTitle.Length > 50)
                    result[i].ChapterTitle = $"Chương {i + 1}";
            }
            return result;
        }

        private List<BookChapter> GetPlaceholderContent(string title, string msg)
        {
            return new List<BookChapter> { new BookChapter { ChapterNumber = 0, ChapterTitle = title, Content = msg } };
        }

        // Utility: Tính số trang (Used by BookScannerService)
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
                        foreach (var item in epub.ReadingOrder) totalCharacters += item.Content.Length;
                }
                else if (ext == ".txt")
                {
                    totalCharacters = new FileInfo(filePath).Length;
                }
                else if (ext == ".pdf")
                {
                    using (var pdf = PdfDocument.Open(filePath)) return pdf.NumberOfPages;
                }

                if (totalCharacters == 0) return 0;
                return (int)Math.Ceiling(totalCharacters / 3000.0);
            }
            catch { return 0; }
        }

        #endregion
    }
}