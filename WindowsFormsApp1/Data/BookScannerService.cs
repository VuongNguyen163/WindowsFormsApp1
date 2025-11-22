using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Windows.Forms;
using VersOne.Epub; 

namespace WindowsFormsApp1.Data
{
    public class BookScannerService
    {
        private readonly DataManager _dataManager;
        private readonly string _coverImageFolder;

        public BookScannerService(DataManager dataManager)
        {
            _dataManager = dataManager;

            // Tạo thư mục lưu ảnh bìa nếu chưa có
            _coverImageFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CoverImages");
            if (!Directory.Exists(_coverImageFolder))
            {
                Directory.CreateDirectory(_coverImageFolder);
            }
        }

        /// <summary>
        /// Tính mã MD5 của file (để kiểm tra trùng lặp nội dung chính xác)
        /// </summary>
        public string CalculateMD5(string filePath)
        {
            try
            {
                if (!File.Exists(filePath)) return null;
                using (var md5 = MD5.Create())
                using (var stream = File.OpenRead(filePath))
                {
                    var hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Lấy kích thước file (KB)
        /// </summary>
        public int GetFileSizeKB(string filePath)
        {
            try
            {
                if (!File.Exists(filePath)) return 0;
                return (int)(new FileInfo(filePath).Length / 1024);
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Lưu byte ảnh bìa ra file và trả về đường dẫn
        /// </summary>
        private string SaveCoverImage(byte[] coverBytes, string fileNameWithoutExt)
        {
            if (coverBytes == null || coverBytes.Length == 0) return null;

            try
            {
                string fileName = $"{fileNameWithoutExt}_{Guid.NewGuid().ToString().Substring(0, 8)}.jpg";
                string destPath = Path.Combine(_coverImageFolder, fileName);
                File.WriteAllBytes(destPath, coverBytes);
                return destPath;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Tạo đối tượng Book từ file vật lý
        /// </summary>
        public Book CreateBookFromFile(string filePath, bool readMetadata = true)
        {
            try
            {
                if (!File.Exists(filePath)) return null;

                var fileInfo = new FileInfo(filePath);
                string ext = fileInfo.Extension.ToLower(); // .epub, .pdf
                string rawExt = ext.Replace(".", "").ToUpper(); // EPUB, PDF

                // Khởi tạo thông tin cơ bản
                var book = new Book
                {
                    FilePath = filePath,
                    FileType = rawExt,
                    Title = Path.GetFileNameWithoutExtension(filePath), // Mặc định là tên file
                    Author = "Unknown Author",
                    FileSizeKB = GetFileSizeKB(filePath),
                    MD5 = CalculateMD5(filePath),
                    DateAdded = DateTime.Now,
                    IsFavorite = false,
                    IsDeleted = false,
                    Progress = 0,
                    TotalPages = 0,
                    CurrentPage = 0
                };

                // Nếu không cần đọc metadata chi tiết thì trả về luôn
                if (!readMetadata) return book;

                // Xử lý riêng cho EPUB (Dùng thư viện VersOne.Epub)
                if (ext == ".epub")
                {
                    try
                    {
                        EpubBook epub = EpubReader.ReadBook(filePath);

                        if (!string.IsNullOrWhiteSpace(epub.Title))
                            book.Title = epub.Title;

                        if (epub.AuthorList != null && epub.AuthorList.Count > 0)
                            book.Author = string.Join(", ", epub.AuthorList);
                        else if (!string.IsNullOrWhiteSpace(epub.Author))
                            book.Author = epub.Author;

                        book.Description = epub.Description;

                        // Xử lý ảnh bìa
                        if (epub.CoverImage != null && epub.CoverImage.Length > 0)
                        {
                            book.CoverImagePath = SaveCoverImage(epub.CoverImage, Path.GetFileNameWithoutExtension(filePath));
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Lỗi đọc metadata EPUB: {ex.Message}");
                        // Vẫn giữ thông tin cơ bản nếu lỗi metadata
                    }
                }
                // Có thể mở rộng thêm logic đọc PDF metadata ở đây nếu cần

                // Xử lý tính toán số trang ước tính
                try
                {
                    var readerService = new BookReaderService(_dataManager);
                    book.TotalPages = readerService.EstimateTotalPages(filePath);
                }
                catch { book.TotalPages = 0; }

                return book;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi xử lý file {Path.GetFileName(filePath)}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Quét folder và import sách (dùng cho tính năng Scan Folder)
        /// </summary>
        public void ScanFolderAndImport(string folderPath, int userId, Action<string> onProgress)
        {
            if (!Directory.Exists(folderPath)) return;

            // Các định dạng hỗ trợ
            string[] extensions = { "*.epub", "*.pdf", "*.txt", "*.mobi" };
            List<string> files = new List<string>();

            foreach (var ext in extensions)
            {
                try
                {
                    files.AddRange(Directory.GetFiles(folderPath, ext, SearchOption.AllDirectories));
                }
                catch { /* Bỏ qua lỗi truy cập folder hệ thống */ }
            }

            int total = files.Count;
            int current = 0;
            int imported = 0;
            int skipped = 0;

            foreach (string file in files)
            {
                current++;
                string fileName = Path.GetFileName(file);

                // Báo cáo tiến độ
                onProgress?.Invoke($"[{current}/{total}] Đang kiểm tra: {fileName}");

                try
                {
                    // 1. Kiểm tra tồn tại trong DB (tránh trùng lặp)
                    if (_dataManager.IsBookExists(file))
                    {
                        skipped++;
                        continue;
                    }

                    // 2. Tạo đối tượng Book
                    var book = CreateBookFromFile(file, readMetadata: true);
                    if (book != null)
                    {
                        // 3. Thêm vào DB
                        _dataManager.AddBook(book);
                        imported++;
                    }
                }
                catch (Exception ex)
                {
                    onProgress?.Invoke($"Lỗi file {fileName}: {ex.Message}");
                }
            }

            onProgress?.Invoke($"HOÀN TẤT! Đã thêm: {imported}, Bỏ qua: {skipped}.");
        }
    }
}