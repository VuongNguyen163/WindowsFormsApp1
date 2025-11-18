using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Linq;
using WindowsFormsApp1.Controls;
using WindowsFormsApp1.Data;

namespace WindowsFormsApp1
{
    public partial class MainForm : Form
    {
        private Panel sidebarPanel;
        private Panel contentPanel;
        private FlowLayoutPanel booksPanel;
        private TextBox searchBox;
        private Button menuButton;
        private Button booksButton;
        private Button favoritesButton;
        private Button notesButton;
        private Button highlightsButton;
        private Button trashButton;
        private Button importButton;
        private Label totalBooksLabel;
        private ComboBox shelfComboBox;
        private string currentView = "Books";
        private string currentSortBy = "Reading progress";
        private bool sortAscending = true;

        public MainForm()
        {
            InitializeMainForm();
            LoadBooks();
        }

        private void InitializeMainForm()
        {
            // Form settings
            this.Text = "Koodo Reader";
            this.Size = new Size(1200, 800);
            this.BackColor = Color.FromArgb(30, 30, 30);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Icon = SystemIcons.Application;

            // Sidebar Panel
            sidebarPanel = new Panel
            {
                Dock = DockStyle.Left,
                Width = 240,
                BackColor = Color.FromArgb(37, 37, 38)
            };

            // Logo
            Label logoLabel = new Label
            {
                Text = "koodo",
                Font = new Font("Arial", 24, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(20, 20),
                Size = new Size(150, 40),
                Cursor = Cursors.Hand
            };

            // Menu Button
            menuButton = CreateIconButton("☰", 20, 80, 30, 30);

            // Sidebar Buttons
            int yPos = 140;
            booksButton = CreateSidebarButton("📚 Books", yPos);
            booksButton.Click += (s, e) => SwitchView("Books");

            yPos += 50;
            favoritesButton = CreateSidebarButton("❤️ Favorites", yPos);
            favoritesButton.Click += (s, e) => SwitchView("Favorites");

            yPos += 50;
            notesButton = CreateSidebarButton("💡 Notes", yPos);
            notesButton.Click += (s, e) => SwitchView("Notes");

            yPos += 50;
            highlightsButton = CreateSidebarButton("⭐ Highlights", yPos);
            highlightsButton.Click += (s, e) => SwitchView("Highlights");

            yPos += 50;
            trashButton = CreateSidebarButton("🗑️ Trash", yPos);
            trashButton.Click += (s, e) => SwitchView("Trash");

            // Shelf Section
            yPos += 80;
            Label shelfLabel = new Label
            {
                Text = "Shelf",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(180, 180, 180),
                Location = new Point(20, yPos),
                Size = new Size(200, 25)
            };

            yPos += 30;
            shelfComboBox = new ComboBox
            {
                Location = new Point(20, yPos),
                Size = new Size(200, 25),
                BackColor = Color.FromArgb(45, 45, 48),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            shelfComboBox.Items.AddRange(DataManager.Instance.GetShelves().ToArray());
            shelfComboBox.SelectedIndex = 0;
            shelfComboBox.SelectedIndexChanged += ShelfComboBox_SelectedIndexChanged;

            sidebarPanel.Controls.AddRange(new Control[] {
                logoLabel, menuButton, booksButton, favoritesButton,
                notesButton, highlightsButton, trashButton,
                shelfLabel, shelfComboBox
            });

            // Content Panel
            contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(30, 30, 30)
            };

            // Top Bar
            Panel topBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = Color.FromArgb(37, 37, 38)
            };

            // Search Box
            searchBox = new TextBox
            {
                Location = new Point(20, 15),
                Size = new Size(300, 30),
                BackColor = Color.FromArgb(45, 45, 48),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11),
                BorderStyle = BorderStyle.FixedSingle
            };
            searchBox.TextChanged += SearchBox_TextChanged;

            // Import Button
            importButton = new Button
            {
                Text = "Import",
                Location = new Point(topBar.Width - 150, 15),
                Size = new Size(120, 30),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            importButton.FlatAppearance.BorderSize = 0;
            importButton.Click += ImportButton_Click;

            topBar.Controls.Add(searchBox);
            topBar.Controls.Add(importButton);

            // Books Panel
            booksPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.FromArgb(30, 30, 30),
                Padding = new Padding(20)
            };

            // Bottom Bar
            Panel bottomBar = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 40,
                BackColor = Color.FromArgb(37, 37, 38)
            };

            totalBooksLabel = new Label
            {
                Text = "Total 0 books",
                Location = new Point(20, 10),
                Size = new Size(200, 20),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9)
            };
            bottomBar.Controls.Add(totalBooksLabel);

            contentPanel.Controls.Add(booksPanel);
            contentPanel.Controls.Add(topBar);
            contentPanel.Controls.Add(bottomBar);

            this.Controls.Add(contentPanel);
            this.Controls.Add(sidebarPanel);

            SetActiveButton(booksButton);
            // Sort Button
            Button sortButton = new Button
            {
                Text = "Sort by",
                Location = new Point(340, 15),
                Size = new Size(100, 30),
                BackColor = Color.FromArgb(45, 45, 48),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10),
                Cursor = Cursors.Hand
            };
            sortButton.FlatAppearance.BorderSize = 1;
            sortButton.FlatAppearance.BorderColor = Color.FromArgb(60, 60, 63);
            sortButton.Click += SortButton_Click;

            topBar.Controls.Add(sortButton);
        }

        private Button CreateIconButton(string text, int x, int y, int width, int height)
        {
            var btn = new Button
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(width, height),
                BackColor = Color.Transparent,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 14),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        private Button CreateSidebarButton(string text, int yPos)
        {
            var btn = new Button
            {
                Text = text,
                Location = new Point(10, yPos),
                Size = new Size(220, 40),
                BackColor = Color.Transparent,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 11),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        private void SetActiveButton(Button activeBtn)
        {
            foreach (Control ctrl in sidebarPanel.Controls)
            {
                if (ctrl is Button btn && btn != menuButton)
                {
                    btn.BackColor = Color.Transparent;
                }
            }
            activeBtn.BackColor = Color.FromArgb(45, 45, 48);
        }

        private void SwitchView(string view)
        {
            currentView = view;

            switch (view)
            {
                case "Books":
                    SetActiveButton(booksButton);
                    LoadBooks();
                    break;
                case "Favorites":
                    SetActiveButton(favoritesButton);
                    LoadFavoriteBooks();
                    break;
                case "Notes":
                    SetActiveButton(notesButton);
                    ShowComingSoon("Notes");
                    break;
                case "Highlights":
                    SetActiveButton(highlightsButton);
                    ShowComingSoon("Highlights");
                    break;
                case "Trash":
                    SetActiveButton(trashButton);
                    LoadTrashBooks();
                    break;
            }
        }

        private void LoadBooks()
        {
            booksPanel.Controls.Clear();
            ApplySort(); // Thay vì gọi DisplayBooks trực tiếp
        }

        private void LoadFavoriteBooks()
        {
            booksPanel.Controls.Clear();
            ApplySort(); // Thay vì gọi DisplayBooks trực tiếp
        }

        private void LoadTrashBooks()
        {
            booksPanel.Controls.Clear();
            ApplySort(); // Thay vì gọi DisplayBooks trực tiếp
        }

        private void DisplayBooks(List<Book> books)
        {
            foreach (var book in books)
            {
                var bookCard = new BookCard
                {
                    Book = book,
                    Margin = new Padding(10)
                };

                bookCard.BookClicked += (s, e) => OpenBook(book);
                bookCard.MenuClicked += (s, e) => ShowBookMenu(book, bookCard);

                booksPanel.Controls.Add(bookCard);
            }

            totalBooksLabel.Text = $"Total {books.Count} books";
        }

        private void OpenBook(Book book)
        {
            MessageBox.Show($"Opening: {book.Title}\n\nThis feature will open the book reader.",
                "Open Book", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ShowBookMenu(Book book, BookCard card)
        {
            ContextMenuStrip menu = new ContextMenuStrip
            {
                BackColor = Color.FromArgb(45, 45, 48),
                ForeColor = Color.White
            };

            menu.Items.Add("Toggle Favorite").Click += (s, e) =>
            {
                DataManager.Instance.ToggleFavorite(book.Id);
                if (currentView == "Books")
                    LoadBooks();
                else if (currentView == "Favorites")
                    LoadFavoriteBooks();
            };

            menu.Items.Add("Edit Info").Click += (s, e) =>
            {
                MessageBox.Show("Edit book info - Coming soon!", "Info");
            };

            // Nếu sách chưa xóa, hiển thị "Move to Trash"
            if (!book.IsDeleted)
            {
                menu.Items.Add("Move to Trash").Click += (s, e) =>
                {
                    DataManager.Instance.DeleteBook(book.Id);
                    if (currentView == "Books")
                        LoadBooks();
                    else if (currentView == "Favorites")
                        LoadFavoriteBooks();
                };
            }
            else
            {
                // Nếu sách đã xóa, hiển thị "Restore" và "Delete Permanently"
                menu.Items.Add("Restore").Click += (s, e) =>
                {
                    DataManager.Instance.RestoreBook(book.Id);
                    LoadTrashBooks();
                };

                menu.Items.Add("Delete Permanently").Click += (s, e) =>
                {
                    if (MessageBox.Show("Delete permanently? This action cannot be undone.",
                        "Confirm Delete",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning) == DialogResult.Yes)
                    {
                        DataManager.Instance.PermanentlyDeleteBook(book.Id);
                        LoadTrashBooks();
                    }
                };
            }

            menu.Show(card, new Point(0, card.Height));
        }

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

        private void ShelfComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            LoadBooks();
        }

        private void ImportButton_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Title = "Open";
                ofd.Filter = "Books (*.epub;*.pdf;*.txt;*.mobi)|*.epub;*.pdf;*.txt;*.mobi|" +
                             "EPUB Files (*.epub)|*.epub|" +
                             "PDF Files (*.pdf)|*.pdf|" +
                             "Text Files (*.txt)|*.txt|" +
                             "MOBI Files (*.mobi)|*.mobi|" +
                             "All Files (*.*)|*.*";
                ofd.FilterIndex = 1;
                ofd.Multiselect = true; // Cho phép chọn nhiều file
                ofd.CheckFileExists = true;
                ofd.CheckPathExists = true;

                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    foreach (string filePath in ofd.FileNames)
                    {
                        ImportSingleBook(filePath);
                    }

                    // Refresh danh sách sách sau khi import
                    LoadBooks();

                    MessageBox.Show(
                        $"Đã import thành công {ofd.FileNames.Length} sách!",
                        "Import thành công",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
            }
        }

        private void ImportBookDialog(string filePath)
        {
            Form dialog = new Form
            {
                Text = "Import Book",
                Size = new Size(400, 300),
                BackColor = Color.FromArgb(37, 37, 38),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false
            };

            Label titleLabel = new Label
            {
                Text = "Title:",
                Location = new Point(20, 20),
                Size = new Size(100, 20),
                ForeColor = Color.White
            };

            TextBox titleBox = new TextBox
            {
                Location = new Point(120, 20),
                Size = new Size(240, 25),
                Text = System.IO.Path.GetFileNameWithoutExtension(filePath),
                BackColor = Color.FromArgb(45, 45, 48),
                ForeColor = Color.White
            };

            Label authorLabel = new Label
            {
                Text = "Author:",
                Location = new Point(20, 60),
                Size = new Size(100, 20),
                ForeColor = Color.White
            };

            TextBox authorBox = new TextBox
            {
                Location = new Point(120, 60),
                Size = new Size(240, 25),
                Text = "Unknown Author",
                BackColor = Color.FromArgb(45, 45, 48),
                ForeColor = Color.White
            };

            Button importBtn = new Button
            {
                Text = "Import",
                Location = new Point(260, 220),
                Size = new Size(100, 30),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            importBtn.FlatAppearance.BorderSize = 0;

            importBtn.Click += (s, e) =>
            {
                var book = new Book
                {
                    Title = titleBox.Text,
                    Author = authorBox.Text,
                    FilePath = filePath,
                    FileType = System.IO.Path.GetExtension(filePath)
                };

                DataManager.Instance.AddBook(book);
                dialog.Close();
                LoadBooks();

                MessageBox.Show("Book imported successfully!", "Success",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            };

            dialog.Controls.AddRange(new Control[] {
                titleLabel, titleBox, authorLabel, authorBox, importBtn
            });

            dialog.ShowDialog();
        }

        private void ShowComingSoon(string feature)
        {
            booksPanel.Controls.Clear();
            Label label = new Label
            {
                Text = $"{feature} feature coming soon!",
                Font = new Font("Segoe UI", 16),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(50, 50)
            };
            booksPanel.Controls.Add(label);
            totalBooksLabel.Text = "Total 0 items";
     
        }

        private void SortButton_Click(object sender, EventArgs e)
        {
            Button sortBtn = (Button)sender;

            // Tạo context menu
            ContextMenuStrip sortMenu = new ContextMenuStrip
            {
                BackColor = Color.FromArgb(45, 45, 48),
                ForeColor = Color.White,
                ShowCheckMargin = true,
                ShowImageMargin = false
            };

            // Các tùy chọn sắp xếp
            var sortOptions = new[]
            {
        "Recently read",
        "Book name",
        "Date",
        "Reading duration",
        "Author name",
        "Reading progress"
    };

            foreach (var option in sortOptions)
            {
                ToolStripMenuItem item = new ToolStripMenuItem(option)
                {
                    Checked = (currentSortBy == option),
                    CheckOnClick = false,
                    ForeColor = Color.White
                };

                item.Click += (s, ev) =>
                {
                    currentSortBy = option;
                    sortMenu.Close(); // Đóng menu ngay lập tức
                    ApplySort();
                };

                sortMenu.Items.Add(item);
            }

            // Separator
            sortMenu.Items.Add(new ToolStripSeparator());

            // Ascend/Descend - chỉ hiển thị tick cho option đang chọn
            ToolStripMenuItem ascendItem = new ToolStripMenuItem("Ascend")
            {
                Checked = sortAscending,
                CheckOnClick = false,
                ForeColor = Color.White
            };
            ascendItem.Click += (s, ev) =>
            {
                sortAscending = true;
                sortMenu.Close(); // Đóng menu ngay lập tức
                ApplySort();
            };

            ToolStripMenuItem descendItem = new ToolStripMenuItem("Descend")
            {
                Checked = !sortAscending,
                CheckOnClick = false,
                ForeColor = Color.White
            };
            descendItem.Click += (s, ev) =>
            {
                sortAscending = false;
                sortMenu.Close(); // Đóng menu ngay lập tức
                ApplySort();
            };

            sortMenu.Items.Add(ascendItem);
            sortMenu.Items.Add(descendItem);

            // Tùy chỉnh style
            sortMenu.Renderer = new CustomMenuRenderer();

            // Hiển thị menu
            sortMenu.Show(sortBtn, new Point(0, sortBtn.Height));
        }

        private void ApplySort()
        {
            List<Book> books;

            // Lấy danh sách sách theo view hiện tại
            switch (currentView)
            {
                case "Favorites":
                    books = DataManager.Instance.GetFavoriteBooks();
                    break;
                case "Trash":
                    books = DataManager.Instance.GetDeletedBooks();
                    break;
                default:
                    books = DataManager.Instance.GetAllBooks();
                    break;
            }

            // Sắp xếp theo tiêu chí
            switch (currentSortBy)
            {
                case "Recently read":
                    books = sortAscending
                        ? books.OrderBy(b => b.DateAdded).ToList()
                        : books.OrderByDescending(b => b.DateAdded).ToList();
                    break;

                case "Book name":
                    books = sortAscending
                        ? books.OrderBy(b => b.Title).ToList()
                        : books.OrderByDescending(b => b.Title).ToList();
                    break;

                case "Date":
                    books = sortAscending
                        ? books.OrderBy(b => b.DateAdded).ToList()
                        : books.OrderByDescending(b => b.DateAdded).ToList();
                    break;

                case "Reading duration":
                    // Tính thời gian đọc (giả sử dựa vào ngày thêm - có thể tùy chỉnh)
                    books = sortAscending
                        ? books.OrderBy(b => (DateTime.Now - b.DateAdded).TotalDays).ToList()
                        : books.OrderByDescending(b => (DateTime.Now - b.DateAdded).TotalDays).ToList();
                    break;

                case "Author name":
                    books = sortAscending
                        ? books.OrderBy(b => b.Author).ToList()
                        : books.OrderByDescending(b => b.Author).ToList();
                    break;

                case "Reading progress":
                    books = sortAscending
                        ? books.OrderBy(b => b.Progress).ToList()
                        : books.OrderByDescending(b => b.Progress).ToList();
                    break;
            }

            // Hiển thị lại
            booksPanel.Controls.Clear();
            DisplayBooks(books);
        }

        // Custom renderer cho menu
        public class CustomMenuRenderer : ToolStripProfessionalRenderer
        {
            public CustomMenuRenderer() : base(new CustomColorTable()) { }

            protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
            {
                if (e.Item.Selected)
                {
                    Rectangle rc = new Rectangle(Point.Empty, e.Item.Size);
                    e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(60, 60, 63)), rc);
                }
                else
                {
                    base.OnRenderMenuItemBackground(e);
                }
            }

            protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
            {
                // Vẽ dấu tick màu trắng
                if (e.Item is ToolStripMenuItem menuItem && menuItem.Checked)
                {
                    e.Graphics.DrawString("✓", new Font("Segoe UI", 10, FontStyle.Bold),
                        Brushes.White, new PointF(5, 3));
                }
            }
        }

        // Custom color table
        public class CustomColorTable : ProfessionalColorTable
        {
            public override Color MenuItemSelected
            {
                get { return Color.FromArgb(60, 60, 63); }
            }

            public override Color MenuItemSelectedGradientBegin
            {
                get { return Color.FromArgb(60, 60, 63); }
            }

            public override Color MenuItemSelectedGradientEnd
            {
                get { return Color.FromArgb(60, 60, 63); }
            }

            public override Color MenuBorder
            {
                get { return Color.FromArgb(60, 60, 63); }
            }

            public override Color MenuItemBorder
            {
                get { return Color.FromArgb(60, 60, 63); }
            }
        }
        private void ImportSingleBook(string filePath)
        {
            try
            {
                // Lấy thông tin file
                System.IO.FileInfo fileInfo = new System.IO.FileInfo(filePath);
                string fileName = System.IO.Path.GetFileNameWithoutExtension(filePath);
                string fileExtension = System.IO.Path.GetExtension(filePath).ToUpper().Replace(".", "");

                // Tạo book object
                var book = new Book
                {
                    Title = fileName,
                    Author = "Unknown Author",
                    FilePath = filePath,
                    FileType = fileExtension,
                    TotalPages = 0,
                    CurrentPage = 0,
                    Progress = 0,
                    IsFavorite = false,
                    DateAdded = DateTime.Now
                };

                // Lưu vào database
                DataManager.Instance.AddBook(book);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Lỗi khi import file {System.IO.Path.GetFileName(filePath)}:\n{ex.Message}",
                    "Lỗi Import",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
        private void ImportWithDialog(string filePath)
        {
            Form dialog = new Form
            {
                Text = "Import Book - " + System.IO.Path.GetFileName(filePath),
                Size = new Size(500, 350),
                BackColor = Color.FromArgb(37, 37, 38),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false
            };

            // File path display
            Label filePathLabel = new Label
            {
                Text = "File:",
                Location = new Point(20, 20),
                Size = new Size(100, 20),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9)
            };

            TextBox filePathBox = new TextBox
            {
                Location = new Point(120, 20),
                Size = new Size(340, 25),
                Text = filePath,
                BackColor = Color.FromArgb(45, 45, 48),
                ForeColor = Color.Gray,
                ReadOnly = true,
                Font = new Font("Segoe UI", 9)
            };

            // Title
            Label titleLabel = new Label
            {
                Text = "Title:",
                Location = new Point(20, 60),
                Size = new Size(100, 20),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9)
            };

            TextBox titleBox = new TextBox
            {
                Location = new Point(120, 60),
                Size = new Size(340, 25),
                Text = System.IO.Path.GetFileNameWithoutExtension(filePath),
                BackColor = Color.FromArgb(45, 45, 48),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10)
            };

            // Author
            Label authorLabel = new Label
            {
                Text = "Author:",
                Location = new Point(20, 100),
                Size = new Size(100, 20),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9)
            };

            TextBox authorBox = new TextBox
            {
                Location = new Point(120, 100),
                Size = new Size(340, 25),
                Text = "Unknown Author",
                BackColor = Color.FromArgb(45, 45, 48),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10)
            };

            // Total Pages
            Label pagesLabel = new Label
            {
                Text = "Total Pages:",
                Location = new Point(20, 140),
                Size = new Size(100, 20),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9)
            };

            NumericUpDown pagesBox = new NumericUpDown
            {
                Location = new Point(120, 140),
                Size = new Size(150, 25),
                BackColor = Color.FromArgb(45, 45, 48),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10),
                Maximum = 10000,
                Value = 0
            };

            // File Type Display
            Label fileTypeLabel = new Label
            {
                Text = "Type:",
                Location = new Point(20, 180),
                Size = new Size(100, 20),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9)
            };

            Label fileTypeValue = new Label
            {
                Text = System.IO.Path.GetExtension(filePath).ToUpper().Replace(".", ""),
                Location = new Point(120, 180),
                Size = new Size(100, 20),
                ForeColor = Color.FromArgb(0, 120, 215),
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };

            // Buttons
            Button importBtn = new Button
            {
                Text = "Import",
                Location = new Point(260, 260),
                Size = new Size(100, 35),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10),
                Cursor = Cursors.Hand
            };
            importBtn.FlatAppearance.BorderSize = 0;

            Button cancelBtn = new Button
            {
                Text = "Cancel",
                Location = new Point(370, 260),
                Size = new Size(90, 35),
                BackColor = Color.FromArgb(60, 60, 63),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10),
                Cursor = Cursors.Hand
            };
            cancelBtn.FlatAppearance.BorderSize = 0;
            cancelBtn.Click += (s, e) => dialog.Close();

            importBtn.Click += (s, e) =>
            {
                var book = new Book
                {
                    Title = titleBox.Text.Trim(),
                    Author = authorBox.Text.Trim(),
                    FilePath = filePath,
                    FileType = System.IO.Path.GetExtension(filePath).ToUpper().Replace(".", ""),
                    TotalPages = (int)pagesBox.Value,
                    CurrentPage = 0,
                    Progress = 0,
                    IsFavorite = false,
                    DateAdded = DateTime.Now
                };

                if (string.IsNullOrEmpty(book.Title))
                {
                    MessageBox.Show("Vui lòng nhập tên sách!", "Thông báo",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                DataManager.Instance.AddBook(book);
                dialog.Close();
            };

            dialog.Controls.AddRange(new Control[] {
        filePathLabel, filePathBox,
        titleLabel, titleBox,
        authorLabel, authorBox,
        pagesLabel, pagesBox,
        fileTypeLabel, fileTypeValue,
        importBtn, cancelBtn
    });

            dialog.ShowDialog();
        }
    }

}