using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsFormsApp1.Data
{
    public class Highlight
    {
        public int Id { get; set; }
        public int BookId { get; set; }
        public int UserId { get; set; }
        public string SelectedText { get; set; } // Nội dung trích dẫn
        public string Note { get; set; }         // Ghi chú của người dùng
        public string ColorHex { get; set; }     // Màu sắc (#FFFF00)
        public int ChapterIndex { get; set; }    // Chương số mấy
        public int StartIndex { get; set; }      // Vị trí bắt đầu bôi đen
        public int Length { get; set; }          // Độ dài đoạn bôi đen
        public DateTime DateCreated { get; set; }

        // Thuộc tính phụ để hiển thị tên sách
        public string BookTitle { get; set; }
    }
}
