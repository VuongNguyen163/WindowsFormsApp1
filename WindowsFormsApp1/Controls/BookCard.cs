using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using WindowsFormsApp1.Data;

namespace WindowsFormsApp1.Controls
{
    public partial class BookCard : UserControl
    {
        private Book _book;

        public event EventHandler BookClicked;
        public event EventHandler MenuClicked;

        private PictureBox coverBox;
        private Label titleLabel;
        private Label progressLabel;
        private Button menuButton;
        private Panel infoPanel;

        public BookCard()
        {
            InitializeCustomComponents();

            this.Click += (s, e) => BookClicked?.Invoke(this, e);
            this.Cursor = Cursors.Hand;
            this.BackColor = Color.Transparent;
            this.Size = new Size(150, 240);
            this.Margin = new Padding(15);
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

            // 3. Xử lý hiển thị Ảnh bìa (Logic mới: Load an toàn + Fallback)
            string imagePath = _book.CoverImagePath;
            bool imageLoaded = false;

            if (!string.IsNullOrEmpty(imagePath))
            {
                // Trường hợp A: Đường dẫn tuyệt đối trong Database đúng
                if (File.Exists(imagePath))
                {
                    LoadImageSafe(imagePath);
                    imageLoaded = true;
                }
                // Trường hợp B: Đường dẫn sai (do copy app), tự dò trong folder CoverImages cạnh file exe
                else
                {
                    try
                    {
                        string fileName = Path.GetFileName(imagePath);
                        string localPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CoverImages", fileName);

                        if (File.Exists(localPath))
                        {
                            LoadImageSafe(localPath);
                            imageLoaded = true;
                        }
                    }
                    catch { }
                }
            }

            // 4. Nếu không tìm thấy ảnh nào, dùng Placeholder
            if (!imageLoaded)
            {
                coverBox.Image = null; // Hoặc gán Properties.Resources.DefaultBook nếu có
                coverBox.BackColor = Color.FromArgb(60, 60, 60);
            }
        }

        // Hàm load ảnh an toàn, tránh lỗi File Lock
        private void LoadImageSafe(string path)
        {
            try
            {
                using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read))
                {
                    if (coverBox.Image != null) coverBox.Image.Dispose();
                    coverBox.Image = Image.FromStream(fs);
                }
            }
            catch
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
                BackColor = Color.FromArgb(60, 60, 60)
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

            // 4. Bottom Row
            Panel bottomRow = new Panel
            {
                Dock = DockStyle.Top,
                Height = 20
            };

            progressLabel = new Label
            {
                Dock = DockStyle.Left,
                AutoSize = true,
                Font = new Font("Segoe UI", 9, FontStyle.Regular),
                ForeColor = Color.Gray,
                Text = "0.00%",
                TextAlign = ContentAlignment.MiddleLeft
            };

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