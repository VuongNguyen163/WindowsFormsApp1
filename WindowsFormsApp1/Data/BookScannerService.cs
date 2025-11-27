using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Windows.Forms;
using VersOne.Epub;
using UglyToad.PdfPig; // NuGet: PdfPig
using UglyToad.PdfPig.Content; // Để xử lý nội dung PDF

namespace WindowsFormsApp1.Data
{
    public class BookScannerService
    {
        private readonly DataManager _dataManager;
        private readonly string _coverImageFolder;

        public BookScannerService(DataManager dataManager)
        {
            _dataManager = dataManager;
            _coverImageFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CoverImages");
            if (!Directory.Exists(_coverImageFolder)) Directory.CreateDirectory(_coverImageFolder);
        }

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
            catch { return null; }
        }

        public int GetFileSizeKB(string filePath)
        {
            try
            {
                if (!File.Exists(filePath)) return 0;
                return (int)(new FileInfo(filePath).Length / 1024);
            }
            catch { return 0; }
        }

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
            catch { return null; }
        }

        // Helper: Convert PdfPig Image to Byte Array
        private byte[] ConvertPdfImageToBytes(IPdfImage pdfImage)
        {
            try
            {
                if (pdfImage.TryGetPng(out byte[] pngBytes)) return pngBytes;
                // Nếu không lấy được PNG, thử lấy Raw Bytes (thường là JPG hoặc JP2)
                return pdfImage.RawBytes.ToArray();
            }
            catch { return null; }
        }

        public Book CreateBookFromFile(string filePath, bool readMetadata = true)
        {
            try
            {
                if (!File.Exists(filePath)) return null;

                var fileInfo = new FileInfo(filePath);
                string ext = fileInfo.Extension.ToLower();
                string rawExt = ext.Replace(".", "").ToUpper();

                var book = new Book
                {
                    FilePath = filePath,
                    FileType = rawExt,
                    Title = Path.GetFileNameWithoutExtension(filePath),
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

                if (!readMetadata) return book;

                // --- Xử lý EPUB ---
                if (ext == ".epub")
                {
                    try
                    {
                        EpubBook epub = EpubReader.ReadBook(filePath);
                        if (!string.IsNullOrWhiteSpace(epub.Title)) book.Title = epub.Title;
                        if (epub.AuthorList != null && epub.AuthorList.Count > 0) book.Author = string.Join(", ", epub.AuthorList);
                        else if (!string.IsNullOrWhiteSpace(epub.Author)) book.Author = epub.Author;
                        book.Description = epub.Description;

                        if (epub.CoverImage != null && epub.CoverImage.Length > 0)
                            book.CoverImagePath = SaveCoverImage(epub.CoverImage, Path.GetFileNameWithoutExtension(filePath));
                    }
                    catch (Exception ex) { Console.WriteLine($"Lỗi Metadata EPUB: {ex.Message}"); }
                }
                // --- [MỚI] Xử lý PDF (Dùng PdfPig) ---
                else if (ext == ".pdf")
                {
                    try
                    {
                        using (var pdf = PdfDocument.Open(filePath))
                        {
                            // 1. Đọc Metadata cơ bản
                            if (pdf.Information.Title != null) book.Title = pdf.Information.Title;
                            if (pdf.Information.Author != null) book.Author = pdf.Information.Author;

                            // 2. Lấy tổng số trang chính xác của PDF
                            book.TotalPages = pdf.NumberOfPages;

                            // 3. Trích xuất ảnh bìa (Thử lấy ảnh đầu tiên ở trang 1)
                            // Lưu ý: PdfPig trích xuất ảnh nhúng, không phải render trang thành ảnh.
                            // Nếu trang 1 là text thuần, sẽ không có ảnh bìa.
                            try
                            {
                                var page1 = pdf.GetPage(1);
                                var images = page1.GetImages();
                                foreach (var img in images)
                                {
                                    // Lấy ảnh đầu tiên tìm thấy
                                    byte[] imgBytes = ConvertPdfImageToBytes(img);
                                    if (imgBytes != null && imgBytes.Length > 0)
                                    {
                                        book.CoverImagePath = SaveCoverImage(imgBytes, Path.GetFileNameWithoutExtension(filePath));
                                        break; // Chỉ lấy 1 ảnh làm bìa
                                    }
                                }
                            }
                            catch { /* Bỏ qua lỗi ảnh bìa PDF */ }
                        }
                    }
                    catch (Exception ex) { Console.WriteLine($"Lỗi Metadata PDF: {ex.Message}"); }
                }

                // Tính số trang ước tính cho các loại file khác (EPUB, TXT)
                if (ext != ".pdf")
                {
                    try
                    {
                        var readerService = new BookReaderService(_dataManager);
                        book.TotalPages = readerService.EstimateTotalPages(filePath);
                    }
                    catch { book.TotalPages = 0; }
                }

                return book;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi file {Path.GetFileName(filePath)}: {ex.Message}");
                return null;
            }
        }

        public void ScanFolderAndImport(string folderPath, int userId, Action<string> onProgress)
        {
            if (!Directory.Exists(folderPath)) return;

            string[] extensions = { "*.epub", "*.pdf", "*.txt", "*.mobi" };
            List<string> files = new List<string>();

            foreach (var ext in extensions)
            {
                try { files.AddRange(Directory.GetFiles(folderPath, ext, SearchOption.AllDirectories)); }
                catch { }
            }

            int total = files.Count;
            int current = 0;
            int imported = 0;
            int skipped = 0;

            foreach (string file in files)
            {
                current++;
                string fileName = Path.GetFileName(file);
                onProgress?.Invoke($"[{current}/{total}] {fileName}");

                try
                {
                    if (_dataManager.IsBookExists(file))
                    {
                        skipped++;
                        continue;
                    }

                    var book = CreateBookFromFile(file, readMetadata: true);
                    if (book != null)
                    {
                        _dataManager.AddBook(book);
                        imported++;
                    }
                }
                catch (Exception ex)
                {
                    onProgress?.Invoke($"Lỗi: {ex.Message}");
                }
            }
            onProgress?.Invoke($"HOÀN TẤT! Thêm: {imported}, Bỏ qua: {skipped}.");
        }
    }
}