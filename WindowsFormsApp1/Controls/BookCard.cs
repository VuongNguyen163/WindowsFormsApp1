using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using WindowsFormsApp1.Data; // <--- QUAN TRỌNG: Phải import namespace này để dùng được class Book

namespace WindowsFormsApp1.Controls
{
    public partial class BookCard : UserControl
    {
        // Field private đặt tên có dấu gạch dưới để chuẩn coding convention
        private Book _book;

        // Định nghĩa các Event để MainForm có thể bắt sự kiện
        public event EventHandler BookClicked;
        public event EventHandler MenuClicked;

        // Các Control giao diện
        private PictureBox coverBox;
        private Label titleLabel;
        private Label progressLabel;
        private Button menuButton;
        private Panel infoPanel;

        public BookCard()
        {
            InitializeCustomComponents();

            // Sự kiện click vào toàn bộ thẻ
            this.Click += (s, e) => BookClicked?.Invoke(this, e);
            this.Cursor = Cursors.Hand;
            this.BackColor = Color.Transparent;
            this.Size = new Size(150, 240);
            this.Margin = new Padding(15); // Tăng margin để thoáng hơn
        }

        public Book Book
        {
            get { return _book; }
            set
            {
                _book = value;
                UpdateDisplay();
            }
        }

        private void UpdateDisplay()
        {
            if (_book == null) return;

            // 1. Hiển thị Title
            titleLabel.Text = _book.Title;

            // 2. Hiển thị Tiến độ
            if (_book.TotalPages > 0)
            {
                progressLabel.Text = $"{_book.Progress:0.00}%";
            }
            else
            {
                progressLabel.Text = "0.00%";
            }

            // 3. Hiển thị Ảnh bìa
            if (!string.IsNullOrEmpty(_book.CoverImagePath) && File.Exists(_book.CoverImagePath))
            {
                try
                {
                    using (var fs = new FileStream(_book.CoverImagePath, FileMode.Open, FileAccess.Read))
                    {
                        coverBox.Image = Image.FromStream(fs);
                    }
                }
                catch
                {
                    coverBox.Image = null;
                }
            }
            else
            {
                coverBox.Image = null;
            }
        }

        private void InitializeCustomComponents()
        {
            // 1. Cover Image
            coverBox = new PictureBox
            {
                Dock = DockStyle.Top,
                Height = 190,
                SizeMode = PictureBoxSizeMode.StretchImage,
                BackColor = Color.FromArgb(60, 60, 60) // Placeholder color
            };
            coverBox.Click += (s, e) => BookClicked?.Invoke(this, e);

            // 2. Info Panel
            infoPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 5, 0, 0)
            };

            // 3. Title
            titleLabel = new Label
            {
                Dock = DockStyle.Top,
                Height = 20,
                Font = new Font("Segoe UI", 10, FontStyle.Regular),
                ForeColor = Color.White,
                AutoEllipsis = true,
                Text = "Book Title"
            };
            titleLabel.Click += (s, e) => BookClicked?.Invoke(this, e);

            // 4. Bottom Row (Progress + Menu)
            Panel bottomRow = new Panel
            {
                Dock = DockStyle.Top,
                Height = 20
            };

            // Progress
            progressLabel = new Label
            {
                Dock = DockStyle.Left,
                AutoSize = true,
                Font = new Font("Segoe UI", 9, FontStyle.Regular),
                ForeColor = Color.Gray,
                Text = "0.00%",
                TextAlign = ContentAlignment.MiddleLeft
            };

            // Menu Button
            menuButton = new Button
            {
                Text = "...",
                Dock = DockStyle.Right,
                Width = 30,
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.Gray,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleRight,
                Cursor = Cursors.Hand
            };
            menuButton.FlatAppearance.BorderSize = 0;
            menuButton.FlatAppearance.MouseOverBackColor = Color.Transparent;
            menuButton.FlatAppearance.MouseDownBackColor = Color.Transparent;
            menuButton.Click += (s, e) => MenuClicked?.Invoke(this, e);

            bottomRow.Controls.Add(menuButton);
            bottomRow.Controls.Add(progressLabel);

            infoPanel.Controls.Add(bottomRow);
            infoPanel.Controls.Add(titleLabel);

            this.Controls.Add(infoPanel);
            this.Controls.Add(coverBox);
        }
    }
}