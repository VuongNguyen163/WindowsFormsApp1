using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Linq;
using System.IO;
using System.ComponentModel;
using WindowsFormsApp1.Controls;
using WindowsFormsApp1.Data;
using WindowsFormsApp1.Forms;

namespace WindowsFormsApp1
{
    public partial class MainForm : Form
    {
        // --- UI COMPONENTS ---
        private Panel sidebarPanel;
        private Panel contentPanel;
        private FlowLayoutPanel booksPanel;
        private TextBox searchBox;

        // --- FILTER BAR CONTROLS (MỚI) ---
        private Panel pnlFilterBar;
        private Label lblFilterBook;
        private ComboBox cmbFilterBook;
        // ---------------------------------

        // Sidebar Buttons
        private Button menuButton;
        private Button booksButton;
        private Button favoritesButton;
        private Button notesButton;
        private Button highlightsButton;
        private Button trashButton;

        // Shelf UI
        private FlowLayoutPanel pnlShelfContainer;
        private Button btnShelfToggle;
        private bool isShelfExpanded = true;
        private int activeShelfId = -1; // -1 = All Books

        // Top Bar
        private Panel topBar;
        private Button importButton;
        private Button scanFolderButton;
        private Button sortButton;
        private Label totalBooksLabel;

        // Auth UI
        private Button userButton;
        private Label lblUsername;
        private ContextMenuStrip authMenu;

        // --- STATE VARIABLES ---
        private string currentView = "Books";
        private string currentSortBy = "Reading progress";
        private bool sortAscending = false;

        private User _currentUser = null;

        public MainForm()
        {
            InitializeMainForm();

            // Mặc định chưa đăng nhập (Guest)
            DataManager.Instance.SetCurrentUser(0);
            UpdateUIAuth();
        }

        #region UI INITIALIZATION (KHỞI TẠO GIAO DIỆN)

        private void InitializeMainForm()
        {
            // Form Settings
            this.Text = "Koodo Reader";
            this.Size = new Size(1280, 800);
            this.BackColor = Color.FromArgb(30, 30, 30);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Icon = SystemIcons.Application;

            // --- 1. SIDEBAR ---
            sidebarPanel = new Panel
            {
                Dock = DockStyle.Left,
                Width = 240,
                BackColor = Color.FromArgb(37, 37, 38)
            };

            Label logoLabel = new Label
            {
                Text = "koodo",
                Font = new Font("Arial", 24, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(20, 20),
                Size = new Size(150, 40),
                Cursor = Cursors.Hand
            };

            menuButton = CreateIconButton("☰", 20, 80, 30, 30);

            int yPos = 140;
            booksButton = CreateSidebarButton("📚 Sách", yPos);
            booksButton.Click += (s, e) => SwitchView("Books");

            yPos += 50;
            favoritesButton = CreateSidebarButton("❤️ Yêu thích", yPos);
            favoritesButton.Click += (s, e) => SwitchView("Favorites");

            yPos += 50;
            notesButton = CreateSidebarButton("💡 Ghi chú", yPos);
            notesButton.Click += (s, e) => SwitchView("Notes");

            yPos += 50;
            highlightsButton = CreateSidebarButton("⭐ Đánh dấu", yPos);
            highlightsButton.Click += (s, e) => SwitchView("Highlights");

            yPos += 50;
            trashButton = CreateSidebarButton("🗑️ Thùng rác", yPos);
            trashButton.Click += (s, e) => SwitchView("Trash");

            // Shelf Section
            yPos += 60;
            btnShelfToggle = new Button
            {
                Text = "˅  Kệ sách",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Color.FromArgb(180, 180, 180),
                Location = new Point(10, yPos),
                Size = new Size(220, 40),
                TextAlign = ContentAlignment.MiddleLeft,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand,
                Padding = new Padding(10, 0, 0, 0)
            };
            btnShelfToggle.FlatAppearance.BorderSize = 0;
            btnShelfToggle.Click += (s, e) => ToggleShelf();

            // Container for shelf items
            pnlShelfContainer = new FlowLayoutPanel
            {
                Location = new Point(20, yPos + 45),
                Size = new Size(220, 300),
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                Visible = true
            };

            RefreshSidebarShelves();

            sidebarPanel.Controls.AddRange(new Control[] {
                logoLabel, menuButton, booksButton, favoritesButton,
                notesButton, highlightsButton, trashButton,
                btnShelfToggle, pnlShelfContainer
            });

            // --- 2. CONTENT PANEL ---
            contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(30, 30, 30)
            };

            // --- 3. TOP BAR ---
            topBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = Color.FromArgb(37, 37, 38)
            };

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

            sortButton = new Button
            {
                Text = "Sắp xếp",
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

            // Nút Scan Folder
            scanFolderButton = new Button
            {
                Text = "Quét thư mục",
                Size = new Size(120, 30),
                BackColor = Color.FromArgb(100, 150, 100),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Visible = false
            };
            scanFolderButton.FlatAppearance.BorderSize = 0;
            scanFolderButton.Click += ScanFolderButton_Click;

            // Nút Import
            importButton = new Button
            {
                Text = "Nhập sách",
                Size = new Size(120, 30),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Visible = false
            };
            importButton.FlatAppearance.BorderSize = 0;
            importButton.Click += ImportButton_Click;

            // Nút User (Avatar)
            userButton = new Button
            {
                Text = "👤",
                Size = new Size(40, 40),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Gray,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 14),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            userButton.FlatAppearance.BorderSize = 0;

            // Bo tròn nút
            System.Drawing.Drawing2D.GraphicsPath gp = new System.Drawing.Drawing2D.GraphicsPath();
            gp.AddEllipse(0, 0, 40, 40);
            userButton.Region = new Region(gp);
            userButton.Click += UserButton_Click;

            lblUsername = new Label
            {
                Text = "",
                AutoSize = true,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Visible = false
            };

            authMenu = new ContextMenuStrip();
            authMenu.RenderMode = ToolStripRenderMode.System;

            topBar.Controls.AddRange(new Control[] {
                searchBox, sortButton,
                scanFolderButton, importButton,
                userButton, lblUsername
            });

            // --- 3.5. FILTER BAR (Thanh phụ chứa bộ lọc) ---
            pnlFilterBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 50,
                BackColor = Color.FromArgb(30, 30, 30), // Cùng màu nền content để nhìn liền mạch
                Visible = false // Mặc định ẩn
            };

            lblFilterBook = new Label
            {
                Text = "Filter by book", // Để tiếng Anh cho giống ảnh mẫu
                ForeColor = Color.Silver,
                AutoSize = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            // Căn vị trí: Cách lề phải khoảng 250px
            lblFilterBook.Location = new Point(pnlFilterBar.Width - 300, 15);

            cmbFilterBook = new ComboBox
            {
                // Căn vị trí: Bên phải label
                Location = new Point(pnlFilterBar.Width - 180, 12),
                Size = new Size(160, 25),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(45, 45, 48),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Font = new Font("Segoe UI", 9)
            };

            cmbFilterBook.SelectedIndexChanged += (s, e) =>
            {
                if (currentView == "Highlights") LoadHighlightsView();
                else if (currentView == "Notes") LoadNotesView();
            };

            pnlFilterBar.Controls.Add(lblFilterBook);
            pnlFilterBar.Controls.Add(cmbFilterBook);

            // --- 4. BOOKS PANEL ---
            booksPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.FromArgb(30, 30, 30),
                Padding = new Padding(20)
            };

            // --- 5. BOTTOM BAR ---
            Panel bottomBar = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 40,
                BackColor = Color.FromArgb(37, 37, 38)
            };

            totalBooksLabel = new Label
            {
                Text = "Vui lòng đăng nhập",
                Location = new Point(20, 10),
                Size = new Size(200, 20),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9)
            };
            bottomBar.Controls.Add(totalBooksLabel);

            // --- THỨ TỰ ADD CONTROLS QUAN TRỌNG CHO DOCKING ---
            // 1. Bottom (Dưới cùng)
            contentPanel.Controls.Add(bottomBar);
            // 2. Books Panel (Giữa - Fill)
            contentPanel.Controls.Add(booksPanel);
            // 3. Filter Bar (Trên - Dock Top)
            contentPanel.Controls.Add(pnlFilterBar);
            // 4. Top Bar (Trên cùng nhất - Dock Top)
            contentPanel.Controls.Add(topBar);

            this.Controls.Add(contentPanel);
            this.Controls.Add(sidebarPanel);

            SetActiveButton(booksButton);
        }

        private Button CreateIconButton(string text, int x, int y, int width, int height)
        {
            return new Button
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(width, height),
                BackColor = Color.Transparent,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 14),
                Cursor = Cursors.Hand,
                FlatAppearance = { BorderSize = 0 }
            };
        }

        private Button CreateSidebarButton(string text, int yPos)
        {
            return new Button
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
                Cursor = Cursors.Hand,
                FlatAppearance = { BorderSize = 0 }
            };
        }

        private Button CreateSidebarSubButton(string text)
        {
            return new Button
            {
                Text = text,
                Size = new Size(190, 30),
                BackColor = Color.Transparent,
                ForeColor = Color.FromArgb(200, 200, 200),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10),
                TextAlign = ContentAlignment.MiddleLeft,
                Cursor = Cursors.Hand,
                FlatAppearance = { BorderSize = 0 },
                Margin = new Padding(0, 2, 0, 2)
            };
        }

        #endregion

        #region AUTH LOGIC

        private void UpdateUIAuth()
        {
            int rightMargin = 20;
            int gap = 15;

            userButton.Location = new Point(topBar.Width - userButton.Width - rightMargin, 10);
            userButton.Visible = true;

            if (_currentUser == null)
            {
                scanFolderButton.Visible = false;
                importButton.Visible = false;
                lblUsername.Visible = false;

                userButton.BackColor = Color.Gray;
                userButton.Text = "👤";
            }
            else
            {
                userButton.BackColor = Color.IndianRed;
                userButton.Text = "⏻";

                lblUsername.Text = _currentUser.DisplayName;
                lblUsername.Visible = true;
                lblUsername.Location = new Point(userButton.Left - lblUsername.Width - gap, 20);

                importButton.Visible = true;
                importButton.Location = new Point(lblUsername.Left - importButton.Width - gap, 15);

                scanFolderButton.Visible = true;
                scanFolderButton.Location = new Point(importButton.Left - scanFolderButton.Width - gap, 15);
            }

            RefreshSidebarShelves();
        }

        private void UserButton_Click(object sender, EventArgs e)
        {
            authMenu.Items.Clear();

            if (_currentUser == null)
            {
                authMenu.Items.Add("Đăng Nhập", null, (s, ev) => ShowLoginForm());
                authMenu.Items.Add("Đăng Ký", null, (s, ev) => ShowRegisterForm());
            }
            else
            {
                var logoutItem = authMenu.Items.Add("Đăng Xuất");
                logoutItem.ForeColor = Color.Red;
                logoutItem.Click += (s, ev) => PerformLogout();
            }

            authMenu.Show(userButton, new Point(0, userButton.Height));
        }

        private void ShowLoginForm()
        {
            LoginForm login = new LoginForm();
            var result = login.ShowDialog();

            if (result == DialogResult.OK)
            {
                _currentUser = login.LoggedInUser;
                UpdateUIAuth();
                LoadBooks();
                MessageBox.Show($"Chào mừng trở lại, {_currentUser.DisplayName}!", "Thành công");
            }
            else if (result == DialogResult.Retry)
            {
                ShowRegisterForm();
            }
        }

        private void ShowRegisterForm()
        {
            RegisterForm reg = new RegisterForm();
            var result = reg.ShowDialog();

            if (result == DialogResult.OK)
            {
                _currentUser = reg.RegisteredUser;
                UpdateUIAuth();
                LoadBooks();
                MessageBox.Show($"Đăng ký thành công! Chào {_currentUser.DisplayName}", "Thành công");
            }
            else if (result == DialogResult.Retry)
            {
                ShowLoginForm();
            }
        }

        private void PerformLogout()
        {
            if (MessageBox.Show("Bạn có chắc muốn đăng xuất?", "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                _currentUser = null;
                DataManager.Instance.SetCurrentUser(0);

                booksPanel.Controls.Clear();
                totalBooksLabel.Text = "Vui lòng đăng nhập";

                UpdateUIAuth();
            }
        }

        #endregion

        #region LOGIC & DATA HANDLING

        private void ToggleShelf()
        {
            isShelfExpanded = !isShelfExpanded;
            pnlShelfContainer.Visible = isShelfExpanded;
            btnShelfToggle.Text = isShelfExpanded ? "˅  Kệ sách" : ">  Kệ sách";
        }

        private void RefreshSidebarShelves()
        {
            pnlShelfContainer.Controls.Clear();

            if (_currentUser == null) return;

            // 1. New Shelf
            Button btnNew = CreateSidebarSubButton("+  Kệ mới");
            btnNew.Click += BtnAddShelf_Click;
            pnlShelfContainer.Controls.Add(btnNew);

            // 2. Manage Shelf
            Button btnManage = CreateSidebarSubButton("✎  Quản lý kệ");
            btnManage.Click += BtnManageShelf_Click;
            pnlShelfContainer.Controls.Add(btnManage);

            // 3. List Shelves
            var shelves = DataManager.Instance.GetShelvesList();
            foreach (var shelf in shelves)
            {
                Button btnShelf = CreateSidebarSubButton("   " + shelf.Name);
                btnShelf.Click += (s, e) => {
                    activeShelfId = shelf.Id;
                    foreach (Control c in pnlShelfContainer.Controls)
                        if (c is Button b) b.ForeColor = Color.FromArgb(200, 200, 200);
                    btnShelf.ForeColor = Color.White;

                    currentView = "Shelf";
                    SwitchView(currentView);
                };
                pnlShelfContainer.Controls.Add(btnShelf);
            }
        }

        private void SetActiveButton(Button activeBtn)
        {
            foreach (Control ctrl in sidebarPanel.Controls)
            {
                if (ctrl is Button btn && btn != menuButton && btn != btnShelfToggle)
                {
                    btn.BackColor = Color.Transparent;
                }
            }
            activeBtn.BackColor = Color.FromArgb(45, 45, 48);
        }

        private void LoadFilterCombobox()
        {
            if (_currentUser == null) return;

            List<Book> books = new List<Book>();

            // --- LOGIC MỚI: CHỌN NGUỒN DỮ LIỆU THEO MÀN HÌNH ---
            if (currentView == "Highlights")
            {
                // Nếu đang ở màn hình Đánh dấu -> Chỉ lấy sách có Highlight
                books = DataManager.Instance.GetBooksWithHighlights();
            }
            else if (currentView == "Notes")
            {
                // Nếu đang ở màn hình Ghi chú -> Chỉ lấy sách có Note
                books = DataManager.Instance.GetBooksWithNotes();
            }
            else
            {
                // Các trường hợp khác -> Lấy tất cả
                books = DataManager.Instance.GetAllBooks();
            }
            // ---------------------------------------------------

            // Tạo mục mặc định
            var defaultOption = new Book { Id = -1, Title = "Tất cả sách" }; // Hoặc "Please select" tùy bạn
            books.Insert(0, defaultOption);

            // Gán dữ liệu vào ComboBox
            // (Gỡ sự kiện tạm thời để tránh lỗi reload không mong muốn)
            cmbFilterBook.SelectedIndexChanged -= null;

            cmbFilterBook.DataSource = books;
            cmbFilterBook.DisplayMember = "Title";
            cmbFilterBook.ValueMember = "Id";

            // Reset về mục đầu tiên
            if (books.Count > 0)
                cmbFilterBook.SelectedIndex = 0;

            // Đăng ký lại sự kiện
            cmbFilterBook.SelectedIndexChanged += (s, e) =>
            {
                if (currentView == "Highlights") LoadHighlightsView();
                else if (currentView == "Notes") LoadNotesView();
            };
        }
        private void SwitchView(string view)
        {
            // 1. Kiểm tra đăng nhập
            if (_currentUser == null && view != "Books") return;

            // 2. [QUAN TRỌNG] Cập nhật biến currentView NGAY LẬP TỨC
            // Phải cập nhật dòng này trước thì LoadFilterCombobox mới biết đang ở đâu
            currentView = view;

            // 3. Xử lý Ẩn/Hiện Filter Bar
            if (view == "Highlights" || view == "Notes")
            {
                LoadFilterCombobox(); // Lúc này currentView đã đúng là "Highlights"/"Notes"
                pnlFilterBar.Visible = true;
                sortButton.Visible = false;
            }
            else
            {
                pnlFilterBar.Visible = false;
                sortButton.Visible = true;
            }

            // 4. Chuyển đổi giao diện chính
            switch (view)
            {
                case "Books":
                    SetActiveButton(booksButton);
                    LoadBooks();
                    break;
                case "Favorites":
                    SetActiveButton(favoritesButton);
                    LoadBooks();
                    break;
                case "Highlights":
                    SetActiveButton(highlightsButton);
                    LoadHighlightsView();
                    break;
                case "Notes":
                    SetActiveButton(notesButton);
                    LoadNotesView();
                    break;
                case "Trash":
                    SetActiveButton(trashButton);
                    LoadBooks();
                    break;
                case "Shelf":
                    LoadBooks();
                    break;
            }
        }

        private void LoadBooks()
        {
            booksPanel.Controls.Clear();

            if (_currentUser == null)
            {
                totalBooksLabel.Text = "Vui lòng đăng nhập";
                return;
            }

            List<Book> books;

            if (currentView == "Trash")
                books = DataManager.Instance.GetDeletedBooks();
            else if (currentView == "Favorites")
                books = DataManager.Instance.GetFavoriteBooks();
            else if (currentView == "Shelf")
                books = DataManager.Instance.GetBooksByShelf(activeShelfId);
            else
                books = DataManager.Instance.GetAllBooks();

            // Search Filter
            string query = searchBox.Text.Trim().ToLower();
            if (!string.IsNullOrEmpty(query))
            {
                books = books.Where(b => b.Title.ToLower().Contains(query) || b.Author.ToLower().Contains(query)).ToList();
            }

            ApplySort(ref books);
            DisplayBooks(books);
        }

        private void LoadHighlightsView()
        {
            booksPanel.Controls.Clear();
            totalBooksLabel.Text = "Danh sách Đánh dấu";
            if (_currentUser == null) return;

            var highlights = DataManager.Instance.GetOnlyHighlights(_currentUser.Id);

            // 1. Lọc theo ComboBox
            if (cmbFilterBook.Visible && cmbFilterBook.SelectedValue != null)
            {
                if (int.TryParse(cmbFilterBook.SelectedValue.ToString(), out int selectedBookId))
                {
                    if (selectedBookId != -1)
                    {
                        highlights = highlights.Where(h => h.BookId == selectedBookId).ToList();
                    }
                }
            }

            // 2. Lọc theo Search Box
            string query = searchBox.Text.Trim().ToLower();
            if (!string.IsNullOrEmpty(query))
            {
                highlights = highlights.Where(h =>
                    h.BookTitle.ToLower().Contains(query) ||
                    h.SelectedText.ToLower().Contains(query)
                ).ToList();
            }

            foreach (var hl in highlights)
            {
                Panel card = CreateInfoCard(hl, false);
                booksPanel.Controls.Add(card);
            }
            totalBooksLabel.Text = $"Tìm thấy {highlights.Count} đánh dấu";
        }

        private void LoadNotesView()
        {
            booksPanel.Controls.Clear();
            totalBooksLabel.Text = "Danh sách Ghi chú";
            if (_currentUser == null) return;

            var notes = DataManager.Instance.GetOnlyNotes(_currentUser.Id);

            // 1. Lọc theo ComboBox
            if (cmbFilterBook.Visible && cmbFilterBook.SelectedValue != null)
            {
                if (int.TryParse(cmbFilterBook.SelectedValue.ToString(), out int selectedBookId))
                {
                    if (selectedBookId != -1)
                    {
                        notes = notes.Where(n => n.BookId == selectedBookId).ToList();
                    }
                }
            }

            // 2. Lọc theo Search Box
            string query = searchBox.Text.Trim().ToLower();
            if (!string.IsNullOrEmpty(query))
            {
                notes = notes.Where(n =>
                    n.BookTitle.ToLower().Contains(query) ||
                    n.Note.ToLower().Contains(query) ||
                    n.SelectedText.ToLower().Contains(query)
                ).ToList();
            }

            foreach (var note in notes)
            {
                Panel card = CreateInfoCard(note, true);
                booksPanel.Controls.Add(card);
            }
            totalBooksLabel.Text = $"Tìm thấy {notes.Count} ghi chú";
        }

        private void ShowBookMenu(Book book, BookCard card)
        {
            ContextMenuStrip menu = new ContextMenuStrip
            {
                BackColor = Color.FromArgb(45, 45, 48),
                ForeColor = Color.White
            };

            if (!book.IsDeleted)
            {
                menu.Items.Add("Thêm vào kệ").Click += (s, e) =>
                {
                    using (var dlg = new WindowsFormsApp1.Forms.AddToShelfDialog())
                    {
                        if (dlg.ShowDialog() == DialogResult.OK)
                        {
                            try
                            {
                                int targetShelfId = -1;

                                if (!string.IsNullOrEmpty(dlg.NewShelfName))
                                {
                                    targetShelfId = DataManager.Instance.AddShelf(dlg.NewShelfName);
                                    RefreshSidebarShelves();
                                }
                                else
                                {
                                    targetShelfId = dlg.SelectedShelfId;
                                }

                                if (targetShelfId != -1)
                                {
                                    DataManager.Instance.AddBookToShelf(book.Id, targetShelfId);
                                    MessageBox.Show("Đã thêm sách vào kệ thành công!", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                }
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show("Lỗi: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                    }
                };

                menu.Items.Add("Mở thư mục chứa file").Click += (s, e) =>
                {
                    try
                    {
                        if (File.Exists(book.FilePath))
                        {
                            System.Diagnostics.Process.Start("explorer.exe", $"/select, \"{book.FilePath}\"");
                        }
                        else
                        {
                            MessageBox.Show("File không còn tồn tại trong máy tính!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Không thể mở thư mục: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                };

                string favText = book.IsFavorite ? "Bỏ thích" : "Yêu thích";
                menu.Items.Add(favText).Click += (s, e) =>
                {
                    DataManager.Instance.ToggleFavorite(book.Id);
                    if (currentView == "Books" || currentView == "Shelf") LoadBooks();
                    else if (currentView == "Favorites") LoadBooks();
                };

                menu.Items.Add("Sửa thông tin").Click += (s, e) =>
                {
                    MessageBox.Show("Chức năng đang phát triển!", "Thông báo");
                };

                var delItem = menu.Items.Add("Chuyển vào thùng rác");
                delItem.ForeColor = Color.Red;
                delItem.Click += (s, e) =>
                {
                    DataManager.Instance.DeleteBook(book.Id);
                    LoadBooks();
                };
            }
            else
            {
                menu.Items.Add("Khôi phục").Click += (s, e) =>
                {
                    DataManager.Instance.RestoreBook(book.Id);
                    LoadBooks();
                };

                var del = menu.Items.Add("Xóa vĩnh viễn");
                del.ForeColor = Color.Red;
                del.Click += (s, e) =>
                {
                    if (MessageBox.Show("Xóa vĩnh viễn sách này? Không thể hoàn tác.", "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                    {
                        DataManager.Instance.PermanentlyDeleteBook(book.Id);
                        LoadBooks();
                    }
                };
            }
            menu.Show(card, new Point(0, card.Height));
        }

        private void OpenBook(Book book)
        {
            try
            {
                if (!File.Exists(book.FilePath))
                {
                    MessageBox.Show($"File sách không tồn tại:\n{book.FilePath}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                BookReaderForm readerForm = new BookReaderForm(book);
                readerForm.ShowDialog();
                LoadBooks();
            }
            catch (Exception ex) { MessageBox.Show($"Lỗi mở sách: {ex.Message}"); }
        }

        private Panel CreateInfoCard(Highlight item, bool isNote)
        {
            Panel card = new Panel
            {
                Size = new Size(booksPanel.Width - 60, isNote ? 140 : 100),
                BackColor = Color.FromArgb(45, 45, 48),
                Margin = new Padding(10),
                Cursor = Cursors.Hand
            };

            Panel colorBar = new Panel
            {
                Dock = DockStyle.Left,
                Width = 6,
                BackColor = ColorTranslator.FromHtml(item.ColorHex)
            };

            Label lblBook = new Label
            {
                Text = "📖 " + item.BookTitle,
                ForeColor = Color.Gray,
                Font = new Font("Segoe UI", 9, FontStyle.Italic),
                Location = new Point(15, 10),
                AutoSize = true
            };

            Label lblQuote = new Label
            {
                Text = $"\"{item.SelectedText}\"",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10, isNote ? FontStyle.Regular : FontStyle.Bold),
                Location = new Point(15, 35),
                Size = new Size(card.Width - 100, 40),
                AutoEllipsis = true
            };

            Button btnJump = new Button
            {
                Text = "Đi tới ➔",
                Size = new Size(80, 30),
                Location = new Point(card.Width - 90, 10),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnJump.FlatAppearance.BorderSize = 0;

            btnJump.Click += (s, e) =>
            {
                var book = DataManager.Instance.GetAllBooks().FirstOrDefault(b => b.Id == item.BookId);
                if (book != null)
                {
                    BookReaderForm reader = new BookReaderForm(book);
                    reader.ShowDialog();
                }
            };

            card.Controls.Add(btnJump);
            card.Controls.Add(lblQuote);
            card.Controls.Add(lblBook);
            card.Controls.Add(colorBar);

            if (isNote)
            {
                Label lblUserNote = new Label
                {
                    Text = "📝 " + item.Note,
                    ForeColor = Color.Yellow,
                    Font = new Font("Segoe UI", 10, FontStyle.Bold),
                    Location = new Point(15, 80),
                    Size = new Size(card.Width - 30, 50),
                    AutoEllipsis = true
                };
                card.Controls.Add(lblUserNote);
            }
            else
            {
                card.Click += (s, e) => btnJump.PerformClick();
                lblQuote.Click += (s, e) => btnJump.PerformClick();
            }

            return card;
        }

        private void ApplySort(ref List<Book> books)
        {
            switch (currentSortBy)
            {
                case "Vừa đọc":
                case "Recently read":
                case "Ngày thêm":
                case "Date":
                    books = sortAscending ? books.OrderBy(b => b.DateAdded).ToList() : books.OrderByDescending(b => b.DateAdded).ToList(); break;
                case "Tên sách":
                case "Book name":
                    books = sortAscending ? books.OrderBy(b => b.Title).ToList() : books.OrderByDescending(b => b.Title).ToList(); break;
                case "Tác giả":
                case "Author name":
                    books = sortAscending ? books.OrderBy(b => b.Author).ToList() : books.OrderByDescending(b => b.Author).ToList(); break;
                case "Tiến độ đọc":
                case "Reading progress":
                    books = sortAscending ? books.OrderBy(b => b.Progress).ToList() : books.OrderByDescending(b => b.Progress).ToList(); break;
                default:
                    books = books.OrderByDescending(b => b.DateAdded).ToList(); break;
            }
        }

        private void DisplayBooks(List<Book> books)
        {
            booksPanel.SuspendLayout();
            foreach (var book in books)
            {
                var bookCard = new BookCard { Book = book, Margin = new Padding(10) };
                bookCard.BookClicked += (s, e) => OpenBook(book);
                bookCard.MenuClicked += (s, e) => ShowBookMenu(book, bookCard);
                booksPanel.Controls.Add(bookCard);
            }
            booksPanel.ResumeLayout();
            totalBooksLabel.Text = $"Tổng {books.Count} cuốn";
        }

        #endregion

        #region EVENT HANDLERS (Sort, Import, Scan)

        private void SearchBox_TextChanged(object sender, EventArgs e)
        {
            if (currentView == "Highlights") LoadHighlightsView();
            else if (currentView == "Notes") LoadNotesView();
            else LoadBooks();
        }

        private void BtnAddShelf_Click(object sender, EventArgs e)
        {
            if (_currentUser == null)
            {
                MessageBox.Show("Vui lòng đăng nhập để tạo kệ sách!", "Yêu cầu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (var dlg = new AddShelfDialog())
            {
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        DataManager.Instance.AddShelf(dlg.ShelfName, dlg.ShelfDescription);
                        RefreshSidebarShelves();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void BtnManageShelf_Click(object sender, EventArgs e)
        {
            if (_currentUser == null) return;
            using (var dlg = new ManageShelfDialog())
            {
                dlg.ShowDialog();
                RefreshSidebarShelves();
            }
        }

        private void SortButton_Click(object sender, EventArgs e)
        {
            Button btn = (Button)sender;
            ContextMenuStrip menu = new ContextMenuStrip { BackColor = Color.FromArgb(45, 45, 48), ForeColor = Color.White };
            string[] opts = { "Vừa đọc", "Tên sách", "Ngày thêm", "Tác giả", "Tiến độ đọc" };
            foreach (var o in opts)
            {
                var item = new ToolStripMenuItem(o) { Checked = currentSortBy == o };
                item.Click += (s, ev) => { currentSortBy = o; LoadBooks(); };
                menu.Items.Add(item);
            }
            menu.Items.Add("-");

            var ascItem = new ToolStripMenuItem("Tăng dần") { Checked = sortAscending };
            ascItem.Click += (s, ev) => { sortAscending = true; LoadBooks(); };
            menu.Items.Add(ascItem);

            var descItem = new ToolStripMenuItem("Giảm dần") { Checked = !sortAscending };
            descItem.Click += (s, ev) => { sortAscending = false; LoadBooks(); };
            menu.Items.Add(descItem);

            menu.Show(btn, new Point(0, btn.Height));
        }

        private void ImportButton_Click(object sender, EventArgs e)
        {
            if (_currentUser == null)
            {
                MessageBox.Show("Vui lòng đăng nhập!", "Yêu cầu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (OpenFileDialog ofd = new OpenFileDialog { Multiselect = true, Filter = "Ebooks|*.epub;*.pdf;*.txt;*.mobi" })
            {
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    var scanner = new BookScannerService(DataManager.Instance);
                    int count = 0;
                    List<string> errorFiles = new List<string>();

                    foreach (var f in ofd.FileNames)
                    {
                        try
                        {
                            if (DataManager.Instance.IsBookExists(f)) continue;

                            var book = scanner.CreateBookFromFile(f);
                            if (book != null)
                            {
                                DataManager.Instance.AddBook(book);
                                count++;
                            }
                            else
                            {
                                errorFiles.Add(Path.GetFileName(f));
                            }
                        }
                        catch { errorFiles.Add(Path.GetFileName(f)); }
                    }

                    if (count > 0)
                        MessageBox.Show($"Đã thêm thành công {count} sách!");

                    if (errorFiles.Count > 0)
                        MessageBox.Show($"Có {errorFiles.Count} file lỗi không thể thêm:\n" + string.Join("\n", errorFiles.Take(5)) + "...", "Lỗi Import", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                    LoadBooks();
                }
            }
        }

        private void ScanFolderButton_Click(object sender, EventArgs e)
        {
            if (_currentUser == null) return;

            using (FolderBrowserDialog fbd = new FolderBrowserDialog())
            {
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    Form progress = new Form { Text = "Đang quét...", Size = new Size(300, 100), StartPosition = FormStartPosition.CenterParent };
                    Label lbl = new Label { Text = "Đang xử lý...", Location = new Point(20, 20), AutoSize = true };
                    progress.Controls.Add(lbl);
                    progress.Show();

                    BackgroundWorker worker = new BackgroundWorker();
                    worker.DoWork += (s, ev) => {
                        new BookScannerService(DataManager.Instance).ScanFolderAndImport(fbd.SelectedPath, _currentUser.Id, (msg) => {
                            if (lbl.InvokeRequired) lbl.Invoke(new Action(() => lbl.Text = msg));
                        });
                    };
                    worker.RunWorkerCompleted += (s, ev) => { progress.Close(); LoadBooks(); MessageBox.Show("Hoàn tất!"); };
                    worker.RunWorkerAsync();
                }
            }
        }

        #endregion
    }
}