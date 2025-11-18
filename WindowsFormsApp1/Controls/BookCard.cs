using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;

namespace WindowsFormsApp1.Controls
{
    public class BookCard : UserControl
    {
        private Book book;
        private PictureBox coverImage;
        private Label titleLabel;
        private Label progressLabel;
        private Button menuButton;
        private Panel hoverPanel;

        public Book Book
        {
            get { return book; }
            set
            {
                book = value;
                UpdateDisplay();
            }
        }

        public event EventHandler BookClicked;
        public event EventHandler MenuClicked;

        public BookCard()
        {
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            this.Size = new Size(150, 240);
            this.BackColor = Color.FromArgb(45, 45, 48);
            this.Cursor = Cursors.Hand;
            this.Padding = new Padding(5);

            coverImage = new PictureBox
            {
                Size = new Size(140, 180),
                Location = new Point(5, 5),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.FromArgb(60, 60, 63)
            };
            coverImage.Click += OnBookClicked;

            titleLabel = new Label
            {
                Location = new Point(5, 190),
                Size = new Size(140, 30),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9, FontStyle.Regular),
                TextAlign = ContentAlignment.TopLeft,
                AutoEllipsis = true
            };
            titleLabel.Click += OnBookClicked;

            progressLabel = new Label
            {
                Location = new Point(5, 220),
                Size = new Size(100, 15),
                ForeColor = Color.FromArgb(150, 150, 150),
                Font = new Font("Segoe UI", 8, FontStyle.Regular),
                Text = "0%"
            };

            menuButton = new Button
            {
                Location = new Point(105, 220),
                Size = new Size(40, 15),
                Text = "•••",
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            menuButton.FlatAppearance.BorderSize = 0;
            menuButton.Click += OnMenuClicked;

            hoverPanel = new Panel
            {
                Size = this.Size,
                Location = new Point(0, 0),
                BackColor = Color.FromArgb(30, 255, 255, 255),
                Visible = false
            };

            this.Controls.Add(hoverPanel);
            this.Controls.Add(coverImage);
            this.Controls.Add(titleLabel);
            this.Controls.Add(progressLabel);
            this.Controls.Add(menuButton);

            this.MouseEnter += (s, e) => hoverPanel.Visible = true;
            this.MouseLeave += (s, e) => hoverPanel.Visible = false;
        }

        private void UpdateDisplay()
        {
            if (book == null) return;

            titleLabel.Text = book.Title;
            progressLabel.Text = book.GetProgressText();

            if (!string.IsNullOrEmpty(book.CoverImagePath) && File.Exists(book.CoverImagePath))
            {
                try
                {
                    coverImage.Image = Image.FromFile(book.CoverImagePath);
                }
                catch
                {
                    SetDefaultCover();
                }
            }
            else
            {
                SetDefaultCover();
            }
        }

        private void SetDefaultCover()
        {
            Bitmap defaultCover = new Bitmap(140, 180);
            using (Graphics g = Graphics.FromImage(defaultCover))
            {
                g.Clear(Color.FromArgb(60, 60, 63));
                g.DrawString(book?.Title ?? "No Title",
                    new Font("Segoe UI", 12, FontStyle.Bold),
                    Brushes.White,
                    new RectangleF(10, 80, 120, 80),
                    new StringFormat { Alignment = StringAlignment.Center });
            }
            coverImage.Image = defaultCover;
        }

        private void OnBookClicked(object sender, EventArgs e)
        {
            BookClicked?.Invoke(this, EventArgs.Empty);
        }

        private void OnMenuClicked(object sender, EventArgs e)
        {
            MenuClicked?.Invoke(this, EventArgs.Empty);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                coverImage?.Image?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}