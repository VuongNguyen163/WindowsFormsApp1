using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
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
        // --- COLORS & THEME ---
        // Định nghĩa bảng màu để dễ quản lý và đồng bộ
        private readonly Color clrBackground = Color.FromArgb(32, 33, 36);      // Nền chính
        private readonly Color clrSidebar = Color.FromArgb(25, 25, 27);         // Nền sidebar (tối hơn)
        private readonly Color clrTopBar = Color.FromArgb(40, 41, 45);          // Nền topbar
        private readonly Color clrAccent = Color.FromArgb(138, 180, 248);       // Màu nhấn (Xanh nhạt)
        private readonly Color clrTextActive = Color.White;
        private readonly Color clrTextInactive = Color.FromArgb(154, 160, 166);
        private readonly Color clrHover = Color.FromArgb(60, 64, 67);

        // --- UI COMPONENTS ---
        private Panel sidebarPanel;
        private Panel contentPanel;
        private FlowLayoutPanel booksPanel;
        private TextBox searchBox;
        private Panel pnlFilterBar;
        private Label lblFilterBook;
        private ComboBox cmbFilterBook;

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
        private int activeShelfId = -1;

        // Top Bar Controls
        private Panel topBar;
        private ModernButton importButton;     // Dùng nút Custom
        private ModernButton scanFolderButton; // Dùng nút Custom
        private ModernButton sortButton;       // Dùng nút Custom
        private Label totalBooksLabel;
        private Label logoLabel;
        private Label searchIcon;
        private Panel searchPanel;

        // Auth UI
        private Button userButton; // Giữ nguyên button tròn
        private Label lblUsername;
        private ContextMenuStrip authMenu;

        // --- STATE VARIABLES ---
        private string currentView = "Books";
        private string currentSortBy = "Reading progress";
        private bool sortAscending = false;
        private bool isSidebarExpanded = true;

        private User _currentUser = null;

        public MainForm()
        {
            InitializeMainForm();

            string coverFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CoverImages");
            if (!Directory.Exists(coverFolder)) Directory.CreateDirectory(coverFolder);

            DataManager.Instance.SetCurrentUser(0);
            UpdateUIAuth();
        }

        private void InitializeMainForm()
        {
            this.Text = "Koodo Reader";
            this.Size = new Size(1280, 800);
            this.BackColor = clrBackground;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Icon = SystemIcons.Application;
            this.DoubleBuffered = true; // Giảm giật lag khi vẽ lại giao diện

            // ================================================================
            // 1. TOP BAR
            // ================================================================
            topBar = new Panel { Dock = DockStyle.Top, Height = 70, BackColor = clrTopBar, Padding = new Padding(10) };

            // Nút Menu
            menuButton = CreateIconButton("☰", 15, 20, 40, 30);
            menuButton.FlatAppearance.MouseOverBackColor = clrHover;
            menuButton.Click += (s, e) => ToggleSidebar();

            // Logo
            logoLabel = new Label
            {
                Text = "koodo",
                Font = new Font("Segoe UI", 18, FontStyle.Bold), // Đổi sang Segoe UI cho hiện đại
                ForeColor = Color.White,
                Location = new Point(60, 18),
                AutoSize = true,
                Cursor = Cursors.Hand
            };

            // Search Panel (Bo tròn)
            searchPanel = new Panel
            {
                Location = new Point(200, 15),
                Size = new Size(400, 40),
                BackColor = Color.FromArgb(50, 50, 55), // Màu nền thanh tìm kiếm
                Cursor = Cursors.IBeam
            };
            searchPanel.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                // Vẽ bo tròn mượt hơn
                using (GraphicsPath path = GetRoundedRectangle(new Rectangle(0, 0, searchPanel.Width - 1, searchPanel.Height - 1), 20))
                using (Pen pen = new Pen(Color.FromArgb(70, 70, 70), 1))
                {
                    searchPanel.Region = new Region(path);
                    e.Graphics.DrawPath(pen, path);
                }
            };
            searchPanel.Click += (s, e) => searchBox.Focus();

            searchIcon = new Label
            {
                Text = "🔍",
                Font = new Font("Segoe UI Emoji", 12),
                ForeColor = clrTextInactive,
                Location = new Point(10, 8),
                Size = new Size(30, 30),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
            searchIcon.Click += (s, e) => searchBox.Focus();

            searchBox = new TextBox
            {
                Location = new Point(45, 10),
                Size = new Size(340, 25),
                BackColor = Color.FromArgb(50, 50, 55),
                ForeColor = clrTextInactive,
                Font = new Font("Segoe UI", 11),
                BorderStyle = BorderStyle.None,
                Text = "Tìm kiếm sách, tác giả..."
            };

            // Sự kiện SearchBox giữ nguyên logic
            searchBox.GotFocus += (s, e) => { if (searchBox.Text == "Tìm kiếm sách, tác giả...") { searchBox.Text = ""; searchBox.ForeColor = Color.White; } };
            searchBox.LostFocus += (s, e) => { if (string.IsNullOrWhiteSpace(searchBox.Text)) { searchBox.Text = "Tìm kiếm sách, tác giả..."; searchBox.ForeColor = clrTextInactive; } };
            searchBox.TextChanged += SearchBox_TextChanged;
            searchPanel.Controls.Add(searchIcon);
            searchPanel.Controls.Add(searchBox);

            // Nút Sort (ModernButton)
            sortButton = new ModernButton
            {
                Text = "⇅  Sắp xếp",
                Location = new Point(630, 15),
                Size = new Size(110, 40),
                BackColor = Color.Transparent,
                ForeColor = clrTextActive,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                BorderRadius = 20, // Bo tròn
                BorderColor = Color.FromArgb(80, 80, 80),
                BorderSize = 1,
                HoverColor = clrHover
            };
            sortButton.Click += SortButton_Click;

            // Nút Scan (ModernButton)
            scanFolderButton = new ModernButton
            {
                Text = "📂 Quét",
                Size = new Size(100, 40),
                BackColor = Color.FromArgb(30, 100, 50), // Xanh lá đậm dịu hơn
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Visible = false,
                BorderRadius = 20,
                HoverColor = Color.FromArgb(40, 130, 70)
            };
            scanFolderButton.Click += ScanFolderButton_Click;

            // Nút Import (ModernButton)
            importButton = new ModernButton
            {
                Text = "📥 Nhập",
                Size = new Size(100, 40),
                BackColor = Color.FromArgb(0, 90, 160), // Xanh dương đậm dịu hơn
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Visible = false,
                BorderRadius = 20,
                HoverColor = Color.FromArgb(0, 110, 190)
            };
            importButton.Click += ImportButton_Click;

            // User Button (Avatar tròn)
            userButton = new Button
            {
                Text = "👤",
                Size = new Size(46, 46),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(60, 64, 67),
                ForeColor = Color.White,
                Font = new Font("Segoe UI Emoji", 18),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            userButton.FlatAppearance.BorderSize = 0;
            GraphicsPath gp = new GraphicsPath();
            gp.AddEllipse(0, 0, 46, 46);
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

            // Custom Renderer cho ContextMenu tối màu
            authMenu = new ContextMenuStrip
            {
                BackColor = clrTopBar,
                ForeColor = Color.White,
                Renderer = new DarkMenuRenderer() // Sử dụng renderer tối màu
            };

            topBar.Controls.AddRange(new Control[] { menuButton, logoLabel, searchPanel, sortButton, scanFolderButton, importButton, userButton, lblUsername });

            // ================================================================
            // 2. SIDEBAR
            // ================================================================
            sidebarPanel = new Panel
            {
                Dock = DockStyle.Left,
                Width = 250,
                BackColor = clrSidebar,
                Padding = new Padding(0, 20, 0, 0) // Padding top để cách xa topbar xíu
            };

            int yPos = 20;
            booksButton = CreateSidebarButton("📚 Sách", yPos); booksButton.Click += (s, e) => SwitchView("Books");
            yPos += 55; favoritesButton = CreateSidebarButton("❤️ Yêu thích", yPos); favoritesButton.Click += (s, e) => SwitchView("Favorites");
            yPos += 55; notesButton = CreateSidebarButton("💡 Ghi chú", yPos); notesButton.Click += (s, e) => SwitchView("Notes");
            yPos += 55; highlightsButton = CreateSidebarButton("⭐ Đánh dấu", yPos); highlightsButton.Click += (s, e) => SwitchView("Highlights");
            yPos += 55; trashButton = CreateSidebarButton("🗑️ Thùng rác", yPos); trashButton.Click += (s, e) => SwitchView("Trash");

            yPos += 70;
            // Nút Kệ sách
            btnShelfToggle = new Button
            {
                Text = "📚  Kệ sách",
                Tag = "Kệ sách",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = clrTextInactive,
                Location = new Point(10, yPos), // Thụt vào 1 chút cho đẹp
                Size = new Size(230, 45),
                TextAlign = ContentAlignment.MiddleLeft,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand,
                Padding = new Padding(10, 0, 0, 0)
            };
            btnShelfToggle.FlatAppearance.BorderSize = 0;
            btnShelfToggle.FlatAppearance.MouseOverBackColor = clrHover;
            btnShelfToggle.Click += (s, e) => ToggleShelf();

            pnlShelfContainer = new FlowLayoutPanel
            {
                Location = new Point(0, yPos + 50),
                Size = new Size(250, 300),
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                Visible = true,
                BackColor = Color.Transparent,
                Padding = new Padding(20, 0, 0, 0) // Thụt lề nội dung kệ sách
            };
            RefreshSidebarShelves();

            sidebarPanel.Controls.AddRange(new Control[] { booksButton, favoritesButton, notesButton, highlightsButton, trashButton, btnShelfToggle, pnlShelfContainer });

            // ================================================================
            // 3. CONTENT PANEL
            // ================================================================
            contentPanel = new Panel { Dock = DockStyle.Fill, BackColor = clrBackground };

            // Filter Bar
            pnlFilterBar = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = clrBackground, Visible = false };
            lblFilterBook = new Label { Text = "Lọc theo sách:", ForeColor = clrTextInactive, AutoSize = true, Anchor = AnchorStyles.Top | AnchorStyles.Right, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            lblFilterBook.Location = new Point(pnlFilterBar.Width - 300, 15);

            cmbFilterBook = new ComboBox
            {
                Location = new Point(pnlFilterBar.Width - 190, 12),
                Size = new Size(170, 25),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(50, 50, 55),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Font = new Font("Segoe UI", 9)
            };
            cmbFilterBook.SelectedIndexChanged += (s, e) => { if (currentView == "Highlights") LoadHighlightsView(); else if (currentView == "Notes") LoadNotesView(); };
            pnlFilterBar.Controls.Add(lblFilterBook); pnlFilterBar.Controls.Add(cmbFilterBook);

            // Books Panel
            booksPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = clrBackground, Padding = new Padding(30) };

            // Bottom Bar
            Panel bottomBar = new Panel { Dock = DockStyle.Bottom, Height = 30, BackColor = clrSidebar };
            totalBooksLabel = new Label { Text = "Vui lòng đăng nhập", Location = new Point(15, 5), Size = new Size(300, 20), ForeColor = clrTextInactive, Font = new Font("Segoe UI", 9) };
            bottomBar.Controls.Add(totalBooksLabel);

            contentPanel.Controls.Add(bottomBar);
            contentPanel.Controls.Add(booksPanel);
            contentPanel.Controls.Add(pnlFilterBar);

            // ================================================================
            // 4. ASSEMBLY
            // ================================================================
            this.Controls.Add(contentPanel);
            this.Controls.Add(sidebarPanel);
            this.Controls.Add(topBar);

            SetActiveButton(booksButton);
        }

        // --- GRAPHICS HELPERS ---
        private GraphicsPath GetRoundedRectangle(Rectangle bounds, int radius)
        {
            int diameter = radius * 2;
            Size size = new Size(diameter, diameter);
            Rectangle arc = new Rectangle(bounds.Location, size);
            GraphicsPath path = new GraphicsPath();

            if (radius == 0) { path.AddRectangle(bounds); return path; }

            path.AddArc(arc, 180, 90);
            arc.X = bounds.Right - diameter;
            path.AddArc(arc, 270, 90);
            arc.Y = bounds.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            arc.X = bounds.Left;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();
            return path;
        }

        // --- SIDEBAR LOGIC ---
        private void ToggleSidebar()
        {
            isSidebarExpanded = !isSidebarExpanded;
            if (isSidebarExpanded)
            {
                sidebarPanel.Width = 250;
                UpdateButtonText(booksButton, true); UpdateButtonText(favoritesButton, true);
                UpdateButtonText(notesButton, true); UpdateButtonText(highlightsButton, true); UpdateButtonText(trashButton, true);
                btnShelfToggle.Text = " " + btnShelfToggle.Tag.ToString(); // Hack khoảng trắng
                pnlShelfContainer.Visible = isShelfExpanded;
                logoLabel.Visible = true;
            }
            else
            {
                sidebarPanel.Width = 70;
                UpdateButtonText(booksButton, false); UpdateButtonText(favoritesButton, false);
                UpdateButtonText(notesButton, false); UpdateButtonText(highlightsButton, false); UpdateButtonText(trashButton, false);
                btnShelfToggle.Text = "📚";
                pnlShelfContainer.Visible = false;
                logoLabel.Visible = false;
            }
        }

        private void UpdateButtonText(Button btn, bool showText)
        {
            if (btn == null) return;
            string fullText = btn.Tag.ToString();
            if (showText)
            {
                btn.Text = fullText;
                btn.TextAlign = ContentAlignment.MiddleLeft;
                btn.Padding = new Padding(20, 0, 0, 0); // Padding lớn hơn khi mở rộng
            }
            else
            {
                // Chỉ lấy icon (ký tự đầu tiên)
                if (fullText.Contains(" ")) btn.Text = fullText.Split(' ')[0];
                else btn.Text = fullText.Substring(0, 1);
                btn.TextAlign = ContentAlignment.MiddleCenter;
                btn.Padding = new Padding(0);
            }
        }

        private Button CreateIconButton(string text, int x, int y, int width, int height) => new Button
        {
            Text = text,
            Location = new Point(x, y),
            Size = new Size(width, height),
            BackColor = Color.Transparent,
            ForeColor = clrTextInactive,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 14),
            Cursor = Cursors.Hand,
            FlatAppearance = { BorderSize = 0 }
        };

        private Button CreateSidebarButton(string text, int yPos)
        {
            return new Button
            {
                Text = text,
                Tag = text,
                Location = new Point(10, yPos), // Thụt vào một chút để tạo hiệu ứng floating
                Size = new Size(230, 45),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = Color.Transparent,
                ForeColor = clrTextInactive,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 11),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(20, 0, 0, 0),
                Cursor = Cursors.Hand,
                FlatAppearance = { BorderSize = 0, MouseOverBackColor = clrHover }
            };
        }

        private Button CreateSidebarSubButton(string text)
        {
            var btn = new Button
            {
                Text = text,
                Size = new Size(210, 35),
                BackColor = Color.Transparent,
                ForeColor = clrTextInactive,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10),
                TextAlign = ContentAlignment.MiddleLeft,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 2, 0, 2),
                Padding = new Padding(10, 0, 0, 0)
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = clrHover;
            return btn;
        }

        // --- AUTH & LAYOUT HELPERS ---
        private void UpdateUIAuth()
        {
            int rightMargin = 20; int gap = 15;
            userButton.Location = new Point(topBar.Width - userButton.Width - rightMargin, 12);
            userButton.Visible = true;

            if (_currentUser == null)
            {
                scanFolderButton.Visible = false;
                importButton.Visible = false;
                lblUsername.Visible = false;
                userButton.BackColor = Color.FromArgb(80, 80, 80);
                userButton.Text = "👤";
            }
            else
            {
                userButton.BackColor = Color.FromArgb(234, 67, 53); // Google Red
                userButton.Text = _currentUser.DisplayName.Length > 0 ? _currentUser.DisplayName.Substring(0, 1).ToUpper() : "U"; // Avatar chữ cái

                lblUsername.Text = _currentUser.DisplayName;
                lblUsername.Visible = true;
                lblUsername.Location = new Point(userButton.Left - lblUsername.Width - gap, 25);

                importButton.Visible = true;
                importButton.Location = new Point(lblUsername.Left - importButton.Width - gap, 15);

                scanFolderButton.Visible = true;
                scanFolderButton.Location = new Point(importButton.Left - scanFolderButton.Width - gap, 15);
            }
            RefreshSidebarShelves();
        }

        // --- LOGIC GIỮ NGUYÊN (EVENTS) ---
        private void UserButton_Click(object sender, EventArgs e)
        {
            authMenu.Items.Clear();
            if (_currentUser == null)
            {
                var loginItem = new ToolStripMenuItem("🔑  Đăng Nhập") { Font = new Font("Segoe UI", 10, FontStyle.Bold) };
                loginItem.Click += (s, ev) => ShowLoginForm();
                authMenu.Items.Add(loginItem);

                var registerItem = new ToolStripMenuItem("📝  Đăng Ký") { Font = new Font("Segoe UI", 10) };
                registerItem.Click += (s, ev) => ShowRegisterForm();
                authMenu.Items.Add(registerItem);
            }
            else
            {
                var userInfoItem = new ToolStripMenuItem($"👤  {_currentUser.DisplayName} (Sửa)") { Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = clrAccent };
                userInfoItem.Click += (s, ev) => {
                    using (var pwdForm = new WindowsFormsApp1.Forms.PasswordPromptForm())
                    {
                        if (pwdForm.ShowDialog() == DialogResult.OK && pwdForm.IsVerified)
                        {
                            using (var editForm = new WindowsFormsApp1.Forms.EditProfileForm(_currentUser))
                            {
                                if (editForm.ShowDialog() == DialogResult.OK && editForm.UpdatedUser != null)
                                {
                                    _currentUser = editForm.UpdatedUser; UpdateUIAuth();
                                    MessageBox.Show("Cập nhật thông tin thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                }
                            }
                        }
                    }
                };
                authMenu.Items.Add(userInfoItem);
                authMenu.Items.Add(new ToolStripSeparator());
                var logoutItem = new ToolStripMenuItem("🚪  Đăng Xuất") { Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = Color.FromArgb(255, 100, 100) };
                logoutItem.Click += (s, ev) => PerformLogout();
                authMenu.Items.Add(logoutItem);
            }
            authMenu.Show(userButton, new Point(0, userButton.Height));
        }

        private void ShowLoginForm() { LoginForm login = new LoginForm(); if (login.ShowDialog() == DialogResult.OK) { _currentUser = login.LoggedInUser; UpdateUIAuth(); LoadBooks(); MessageBox.Show($"Chào mừng trở lại, {_currentUser.DisplayName}!", "Thành công"); } else if (login.DialogResult == DialogResult.Retry) ShowRegisterForm(); }
        private void ShowRegisterForm() { RegisterForm reg = new RegisterForm(); if (reg.ShowDialog() == DialogResult.OK) { _currentUser = reg.RegisteredUser; UpdateUIAuth(); LoadBooks(); MessageBox.Show($"Đăng ký thành công! Chào {_currentUser.DisplayName}", "Thành công"); } else if (reg.DialogResult == DialogResult.Retry) ShowLoginForm(); }
        private void PerformLogout() { if (MessageBox.Show("Bạn có chắc muốn đăng xuất?", "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes) { _currentUser = null; DataManager.Instance.SetCurrentUser(0); booksPanel.Controls.Clear(); totalBooksLabel.Text = "Vui lòng đăng nhập"; UpdateUIAuth(); } }
        private void ToggleShelf() { if (!isSidebarExpanded) return; isShelfExpanded = !isShelfExpanded; pnlShelfContainer.Visible = isShelfExpanded; btnShelfToggle.Text = isShelfExpanded ? "  Kệ sách" : "  Kệ sách"; } // Icon handled by font

        private void RefreshSidebarShelves()
        {
            pnlShelfContainer.Controls.Clear();
            if (_currentUser == null) return;
            Button btnNew = CreateSidebarSubButton("➕  Kệ mới"); btnNew.ForeColor = Color.FromArgb(76, 175, 80); btnNew.Font = new Font("Segoe UI", 10, FontStyle.Bold); btnNew.Click += BtnAddShelf_Click; pnlShelfContainer.Controls.Add(btnNew);
            Button btnManage = CreateSidebarSubButton("⚙️  Quản lý kệ"); btnManage.ForeColor = clrAccent; btnManage.Font = new Font("Segoe UI", 10, FontStyle.Bold); btnManage.Click += BtnManageShelf_Click; pnlShelfContainer.Controls.Add(btnManage);
            var separator = new Panel { Height = 1, Width = 200, BackColor = Color.FromArgb(60, 60, 63), Margin = new Padding(5, 8, 5, 8) }; pnlShelfContainer.Controls.Add(separator);
            var shelves = DataManager.Instance.GetShelvesList();
            foreach (var shelf in shelves)
            {
                Button btnShelf = CreateSidebarSubButton("📖  " + shelf.Name); btnShelf.Tag = shelf.Id;
                btnShelf.Click += (s, e) => { activeShelfId = shelf.Id; foreach (Control c in pnlShelfContainer.Controls) if (c is Button b && b.Tag is int) { b.ForeColor = clrTextInactive; b.BackColor = Color.Transparent; } btnShelf.ForeColor = Color.White; btnShelf.BackColor = clrHover; SwitchView("Shelf"); };
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
                    btn.ForeColor = clrTextInactive; // Màu chữ khi không active
                }
            }
            if (activeBtn != null)
            {
                // Tạo hiệu ứng active: nền sáng hơn chút, có vạch màu bên trái
                activeBtn.BackColor = Color.FromArgb(50, 50, 55);
                activeBtn.ForeColor = clrAccent; // Màu chữ sáng lên
            }
        }

        private void LoadFilterCombobox() { if (_currentUser == null) return; List<Book> books = (currentView == "Highlights") ? DataManager.Instance.GetBooksWithHighlights() : (currentView == "Notes" ? DataManager.Instance.GetBooksWithNotes() : DataManager.Instance.GetAllBooks()); var defaultOption = new Book { Id = -1, Title = "Tất cả sách" }; books.Insert(0, defaultOption); cmbFilterBook.SelectedIndexChanged -= null; cmbFilterBook.DataSource = books; cmbFilterBook.DisplayMember = "Title"; cmbFilterBook.ValueMember = "Id"; if (books.Count > 0) cmbFilterBook.SelectedIndex = 0; cmbFilterBook.SelectedIndexChanged += (s, e) => { if (currentView == "Highlights") LoadHighlightsView(); else if (currentView == "Notes") LoadNotesView(); }; }
        private void SwitchView(string view) { if (_currentUser == null && view != "Books") return; currentView = view; if (view == "Highlights" || view == "Notes") { LoadFilterCombobox(); pnlFilterBar.Visible = true; sortButton.Visible = false; } else { pnlFilterBar.Visible = false; sortButton.Visible = true; } switch (view) { case "Books": SetActiveButton(booksButton); LoadBooks(); break; case "Favorites": SetActiveButton(favoritesButton); LoadBooks(); break; case "Highlights": SetActiveButton(highlightsButton); LoadHighlightsView(); break; case "Notes": SetActiveButton(notesButton); LoadNotesView(); break; case "Trash": SetActiveButton(trashButton); LoadBooks(); break; case "Shelf": LoadBooks(); break; } }
        private void LoadBooks() { booksPanel.Controls.Clear(); if (_currentUser == null) { totalBooksLabel.Text = "Vui lòng đăng nhập"; return; } List<Book> books; if (currentView == "Trash") books = DataManager.Instance.GetDeletedBooks(); else if (currentView == "Favorites") books = DataManager.Instance.GetFavoriteBooks(); else if (currentView == "Shelf") books = DataManager.Instance.GetBooksByShelf(activeShelfId); else books = DataManager.Instance.GetAllBooks(); string query = searchBox.Text.Trim().ToLower(); if (!string.IsNullOrEmpty(query) && query != "tìm kiếm sách, tác giả...") books = books.Where(b => b.Title.ToLower().Contains(query) || b.Author.ToLower().Contains(query)).ToList(); ApplySort(ref books); DisplayBooks(books); }
        private void LoadHighlightsView() { booksPanel.Controls.Clear(); totalBooksLabel.Text = "Danh sách Đánh dấu"; if (_currentUser == null) return; var highlights = DataManager.Instance.GetOnlyHighlights(_currentUser.Id); if (cmbFilterBook.Visible && cmbFilterBook.SelectedValue != null && int.TryParse(cmbFilterBook.SelectedValue.ToString(), out int selectedBookId) && selectedBookId != -1) highlights = highlights.Where(h => h.BookId == selectedBookId).ToList(); string query = searchBox.Text.Trim().ToLower(); if (!string.IsNullOrEmpty(query) && query != "tìm kiếm sách, tác giả...") highlights = highlights.Where(h => h.BookTitle.ToLower().Contains(query) || h.SelectedText.ToLower().Contains(query)).ToList(); foreach (var hl in highlights) { Panel card = CreateInfoCard(hl, false); booksPanel.Controls.Add(card); } totalBooksLabel.Text = $"Tìm thấy {highlights.Count} đánh dấu"; }
        private void LoadNotesView() { booksPanel.Controls.Clear(); totalBooksLabel.Text = "Danh sách Ghi chú"; if (_currentUser == null) return; var notes = DataManager.Instance.GetOnlyNotes(_currentUser.Id); if (cmbFilterBook.Visible && cmbFilterBook.SelectedValue != null && int.TryParse(cmbFilterBook.SelectedValue.ToString(), out int selectedBookId) && selectedBookId != -1) notes = notes.Where(n => n.BookId == selectedBookId).ToList(); string query = searchBox.Text.Trim().ToLower(); if (!string.IsNullOrEmpty(query) && query != "tìm kiếm sách, tác giả...") notes = notes.Where(n => n.BookTitle.ToLower().Contains(query) || n.Note.ToLower().Contains(query) || n.SelectedText.ToLower().Contains(query)).ToList(); foreach (var note in notes) { Panel card = CreateInfoCard(note, true); booksPanel.Controls.Add(card); } totalBooksLabel.Text = $"Tìm thấy {notes.Count} ghi chú"; }

        // --- DISPLAY LOGIC (Cards, Menus) ---
        private Panel CreateInfoCard(Highlight item, bool isNote)
        {
            Panel card = new Panel { Size = new Size(booksPanel.Width - 60, isNote ? 140 : 100), BackColor = Color.FromArgb(45, 45, 48), Margin = new Padding(10), Cursor = Cursors.Hand };
            // Bo tròn cho card (Optional: có thể thêm Paint event nếu muốn card bo tròn)
            Panel colorBar = new Panel { Dock = DockStyle.Left, Width = 6, BackColor = ColorTranslator.FromHtml(item.ColorHex) };
            Label lblBook = new Label { Text = "📖 " + item.BookTitle, ForeColor = Color.Gray, Font = new Font("Segoe UI", 9, FontStyle.Italic), Location = new Point(15, 10), AutoSize = true };
            Label lblQuote = new Label { Text = $"\"{item.SelectedText}\"", ForeColor = Color.White, Font = new Font("Segoe UI", 10, isNote ? FontStyle.Regular : FontStyle.Bold), Location = new Point(15, 35), Size = new Size(card.Width - 140, 40), AutoEllipsis = true };
            Button btnJump = new Button { Text = "Đi tới ➔", Size = new Size(70, 30), Location = new Point(card.Width - 120, 10), BackColor = Color.FromArgb(0, 120, 215), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand }; btnJump.FlatAppearance.BorderSize = 0;
            Button btnDelete = new Button { Text = "🗑", Size = new Size(40, 30), Location = new Point(card.Width - 45, 10), BackColor = Color.Transparent, ForeColor = Color.IndianRed, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Font = new Font("Segoe UI", 11) }; btnDelete.FlatAppearance.BorderSize = 0; btnDelete.FlatAppearance.MouseOverBackColor = Color.FromArgb(60, 60, 60);
            btnDelete.Click += (s, e) => { if (MessageBox.Show("Bạn có chắc chắn muốn xóa mục này không?", "Xác nhận xóa", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes) { try { DataManager.Instance.DeleteHighlight(item.Id); if (isNote) LoadNotesView(); else LoadHighlightsView(); } catch (Exception ex) { MessageBox.Show("Lỗi xóa: " + ex.Message); } } };
            btnJump.Click += (s, e) => { var book = DataManager.Instance.GetBookById(item.BookId); if (book != null) { BookReaderForm reader = new BookReaderForm(book, item.ChapterIndex, item.StartIndex); reader.ShowDialog(); } else { MessageBox.Show("Không tìm thấy cuốn sách này (có thể đã bị xóa)."); } };
            card.Controls.AddRange(new Control[] { btnDelete, btnJump, lblQuote, lblBook, colorBar });
            if (isNote) { Label lblUserNote = new Label { Text = "📝 " + item.Note, ForeColor = Color.Yellow, Font = new Font("Segoe UI", 10, FontStyle.Bold), Location = new Point(15, 80), Size = new Size(card.Width - 30, 50), AutoEllipsis = true }; card.Controls.Add(lblUserNote); } else { card.Click += (s, e) => btnJump.PerformClick(); lblQuote.Click += (s, e) => btnJump.PerformClick(); }
            return card;
        }

        private void ShowBookMenu(Book book, BookCard card)
        {
            ContextMenuStrip menu = new ContextMenuStrip { BackColor = clrTopBar, ForeColor = Color.White, Renderer = new DarkMenuRenderer() };
            if (!book.IsDeleted)
            {
                var addToShelfItem = new ToolStripMenuItem("📚  Thêm vào kệ"); addToShelfItem.Click += (s, e) => { using (var dlg = new WindowsFormsApp1.Forms.AddToShelfDialog()) { if (dlg.ShowDialog() == DialogResult.OK) { try { int targetShelfId = -1; if (!string.IsNullOrEmpty(dlg.NewShelfName)) { targetShelfId = DataManager.Instance.AddShelf(dlg.NewShelfName); RefreshSidebarShelves(); } else { targetShelfId = dlg.SelectedShelfId; } if (targetShelfId != -1) { DataManager.Instance.AddBookToShelf(book.Id, targetShelfId); MessageBox.Show("Đã thêm sách vào kệ thành công!", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information); } } catch (Exception ex) { MessageBox.Show("Lỗi: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error); } } } }; menu.Items.Add(addToShelfItem);
                var openFolderItem = new ToolStripMenuItem("📁  Mở thư mục chứa file"); openFolderItem.Click += (s, e) => { try { if (File.Exists(book.FilePath)) System.Diagnostics.Process.Start("explorer.exe", $"/select, \"{book.FilePath}\""); else MessageBox.Show("File không còn tồn tại trong máy tính!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning); } catch (Exception ex) { MessageBox.Show("Không thể mở thư mục: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error); } }; menu.Items.Add(openFolderItem);
                var favoriteItem = new ToolStripMenuItem(book.IsFavorite ? "💔  Bỏ thích" : "❤️  Yêu thích"); favoriteItem.Click += (s, e) => { DataManager.Instance.ToggleFavorite(book.Id); LoadBooks(); }; menu.Items.Add(favoriteItem);
                menu.Items.Add(new ToolStripSeparator { BackColor = clrHover });
                var deleteItem = new ToolStripMenuItem("🗑️  Chuyển vào thùng rác") { ForeColor = Color.FromArgb(255, 100, 100) }; deleteItem.Click += (s, e) => { DataManager.Instance.DeleteBook(book.Id); LoadBooks(); }; menu.Items.Add(deleteItem);
            }
            else
            {
                var restoreItem = new ToolStripMenuItem("♻️  Khôi phục") { ForeColor = Color.FromArgb(100, 200, 100) }; restoreItem.Click += (s, e) => { DataManager.Instance.RestoreBook(book.Id); LoadBooks(); }; menu.Items.Add(restoreItem);
                menu.Items.Add(new ToolStripSeparator { BackColor = clrHover });
                var permanentDeleteItem = new ToolStripMenuItem("⚠️  Xóa vĩnh viễn") { ForeColor = Color.FromArgb(255, 80, 80), Font = new Font("Segoe UI", 9, FontStyle.Bold) }; permanentDeleteItem.Click += (s, e) => { if (MessageBox.Show("Xóa vĩnh viễn sách này? Không thể hoàn tác.", "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes) { DataManager.Instance.PermanentlyDeleteBook(book.Id); LoadBooks(); } }; menu.Items.Add(permanentDeleteItem);
            }
            menu.Show(card, new Point(0, card.Height));
        }

        private void OpenBook(Book book) { try { if (!File.Exists(book.FilePath)) { MessageBox.Show($"File sách không tồn tại:\n{book.FilePath}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; } BookReaderForm readerForm = new BookReaderForm(book); readerForm.ShowDialog(); LoadBooks(); } catch (Exception ex) { MessageBox.Show($"Lỗi mở sách: {ex.Message}"); } }
        private void ApplySort(ref List<Book> books) { switch (currentSortBy) { case "Vừa đọc": case "Recently read": case "Ngày thêm": case "Date": books = sortAscending ? books.OrderBy(b => b.DateAdded).ToList() : books.OrderByDescending(b => b.DateAdded).ToList(); break; case "Tên sách": case "Book name": books = sortAscending ? books.OrderBy(b => b.Title).ToList() : books.OrderByDescending(b => b.Title).ToList(); break; case "Tác giả": case "Author name": books = sortAscending ? books.OrderBy(b => b.Author).ToList() : books.OrderByDescending(b => b.Author).ToList(); break; case "Tiến độ đọc": case "Reading progress": books = sortAscending ? books.OrderBy(b => b.Progress).ToList() : books.OrderByDescending(b => b.Progress).ToList(); break; default: books = books.OrderByDescending(b => b.DateAdded).ToList(); break; } }
        private void DisplayBooks(List<Book> books) { booksPanel.SuspendLayout(); foreach (var book in books) { var bookCard = new BookCard { Book = book, Margin = new Padding(15) }; bookCard.BookClicked += (s, e) => OpenBook(book); bookCard.MenuClicked += (s, e) => ShowBookMenu(book, bookCard); booksPanel.Controls.Add(bookCard); } booksPanel.ResumeLayout(); totalBooksLabel.Text = $"Tổng {books.Count} cuốn"; }
        private void SearchBox_TextChanged(object sender, EventArgs e) { if (searchBox.Text == "Tìm kiếm sách, tác giả...") return; if (currentView == "Highlights") LoadHighlightsView(); else if (currentView == "Notes") LoadNotesView(); else LoadBooks(); }
        private void BtnAddShelf_Click(object sender, EventArgs e) { if (_currentUser == null) { MessageBox.Show("Vui lòng đăng nhập để tạo kệ sách!", "Yêu cầu", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; } using (var dlg = new AddShelfDialog()) { if (dlg.ShowDialog() == DialogResult.OK) { try { DataManager.Instance.AddShelf(dlg.ShelfName, dlg.ShelfDescription); RefreshSidebarShelves(); } catch (Exception ex) { MessageBox.Show(ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error); } } } }
        private void BtnManageShelf_Click(object sender, EventArgs e) { if (_currentUser == null) return; using (var dlg = new ManageShelfDialog()) { dlg.ShowDialog(); RefreshSidebarShelves(); } }

        private void SortButton_Click(object sender, EventArgs e)
        {
            Button btn = (Button)sender; ContextMenuStrip menu = new ContextMenuStrip { BackColor = clrTopBar, ForeColor = Color.White, Renderer = new DarkMenuRenderer() };
            var sortOptions = new Dictionary<string, string> { { "Vừa đọc", "📖" }, { "Tên sách", "📚" }, { "Ngày thêm", "📅" }, { "Tác giả", "✍️" }, { "Tiến độ đọc", "📊" } };
            foreach (var opt in sortOptions) { var item = new ToolStripMenuItem($"{opt.Value}  {opt.Key}") { Checked = currentSortBy == opt.Key, CheckOnClick = false }; item.Click += (s, ev) => { currentSortBy = opt.Key; LoadBooks(); }; menu.Items.Add(item); }
            menu.Items.Add(new ToolStripSeparator { BackColor = clrHover });
            var ascItem = new ToolStripMenuItem("▲  Tăng dần") { Checked = sortAscending }; ascItem.Click += (s, ev) => { sortAscending = true; LoadBooks(); }; menu.Items.Add(ascItem);
            var descItem = new ToolStripMenuItem("▼  Giảm dần") { Checked = !sortAscending }; descItem.Click += (s, ev) => { sortAscending = false; LoadBooks(); }; menu.Items.Add(descItem);
            menu.Show(btn, new Point(0, btn.Height + 5));
        }

        private void ImportButton_Click(object sender, EventArgs e) { if (_currentUser == null) { MessageBox.Show("Vui lòng đăng nhập!", "Yêu cầu", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; } using (OpenFileDialog ofd = new OpenFileDialog { Multiselect = true, Filter = "Ebooks|*.epub;*.pdf;*.txt;*.mobi" }) { if (ofd.ShowDialog() == DialogResult.OK) { var scanner = new BookScannerService(DataManager.Instance); int count = 0; List<string> errorFiles = new List<string>(); foreach (var f in ofd.FileNames) { try { if (DataManager.Instance.IsBookExists(f)) continue; var book = scanner.CreateBookFromFile(f); if (book != null) { DataManager.Instance.AddBook(book); count++; } else { errorFiles.Add(Path.GetFileName(f)); } } catch { errorFiles.Add(Path.GetFileName(f)); } } if (count > 0) MessageBox.Show($"Đã thêm thành công {count} sách!"); if (errorFiles.Count > 0) MessageBox.Show($"Có {errorFiles.Count} file lỗi không thể thêm:\n" + string.Join("\n", errorFiles.Take(5)) + "...", "Lỗi Import", MessageBoxButtons.OK, MessageBoxIcon.Warning); LoadBooks(); } } }
        private void ScanFolderButton_Click(object sender, EventArgs e) { if (_currentUser == null) return; using (FolderBrowserDialog fbd = new FolderBrowserDialog()) { if (fbd.ShowDialog() == DialogResult.OK) { Form progress = new Form { Text = "Đang quét...", Size = new Size(300, 100), StartPosition = FormStartPosition.CenterParent }; Label lbl = new Label { Text = "Đang xử lý...", Location = new Point(20, 20), AutoSize = true }; progress.Controls.Add(lbl); progress.Show(); BackgroundWorker worker = new BackgroundWorker(); worker.DoWork += (s, ev) => { new BookScannerService(DataManager.Instance).ScanFolderAndImport(fbd.SelectedPath, _currentUser.Id, (msg) => { if (lbl.InvokeRequired) lbl.Invoke(new Action(() => lbl.Text = msg)); }); }; worker.RunWorkerCompleted += (s, ev) => { progress.Close(); LoadBooks(); MessageBox.Show("Hoàn tất!"); }; worker.RunWorkerAsync(); } } }
    }

    // ================================================================
    // CUSTOM CLASSES ĐỂ LÀM ĐẸP UI (ModernButton & MenuRenderer)
    // ================================================================

    // Class nút bấm bo tròn hiện đại
    public class ModernButton : Button
    {
        public int BorderRadius { get; set; } = 20;
        public Color BorderColor { get; set; } = Color.Transparent;
        public int BorderSize { get; set; } = 0;
        public Color HoverColor { get; set; } = Color.Gray;

        private Color originalBackColor;

        public ModernButton()
        {
            this.FlatStyle = FlatStyle.Flat;
            this.FlatAppearance.BorderSize = 0;
            this.Size = new Size(150, 40);
            this.BackColor = Color.MediumSlateBlue;
            this.ForeColor = Color.White;
            this.Resize += (s, e) => { if (BorderRadius > this.Height) BorderRadius = this.Height; };
        }

        // Bo tròn góc
        private GraphicsPath GetFigurePath(RectangleF rect, float radius)
        {
            GraphicsPath path = new GraphicsPath();
            path.StartFigure();
            path.AddArc(rect.X, rect.Y, radius, radius, 180, 90);
            path.AddArc(rect.Width - radius, rect.Y, radius, radius, 270, 90);
            path.AddArc(rect.Width - radius, rect.Height - radius, radius, radius, 0, 90);
            path.AddArc(rect.X, rect.Height - radius, radius, radius, 90, 90);
            path.CloseFigure();
            return path;
        }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            base.OnPaint(pevent);
            pevent.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            RectangleF rectSurface = new RectangleF(0, 0, this.Width, this.Height);
            RectangleF rectBorder = new RectangleF(1, 1, this.Width - 0.8f, this.Height - 1);

            if (BorderRadius > 2) // Button bo tròn
            {
                using (GraphicsPath pathSurface = GetFigurePath(rectSurface, BorderRadius))
                using (GraphicsPath pathBorder = GetFigurePath(rectBorder, BorderRadius - 1f))
                using (Pen penSurface = new Pen(this.Parent.BackColor, 2))
                using (Pen penBorder = new Pen(BorderColor, BorderSize))
                {
                    penBorder.Alignment = PenAlignment.Inset;
                    this.Region = new Region(pathSurface);
                    pevent.Graphics.DrawPath(penSurface, pathSurface);
                    if (BorderSize >= 1) pevent.Graphics.DrawPath(penBorder, pathBorder);
                }
            }
            else // Button vuông
            {
                this.Region = new Region(rectSurface);
                if (BorderSize >= 1)
                {
                    using (Pen penBorder = new Pen(BorderColor, BorderSize))
                    {
                        penBorder.Alignment = PenAlignment.Inset;
                        pevent.Graphics.DrawRectangle(penBorder, 0, 0, this.Width - 1, this.Height - 1);
                    }
                }
            }
        }

        // Hiệu ứng Hover
        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            originalBackColor = this.BackColor;
            this.BackColor = HoverColor;
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            this.BackColor = originalBackColor;
        }
    }

    // Class Render Menu tối màu cho đẹp
    public class DarkMenuRenderer : ToolStripProfessionalRenderer
    {
        public DarkMenuRenderer() : base(new DarkMenuColors()) { }
    }

    public class DarkMenuColors : ProfessionalColorTable
    {
        public override Color MenuItemSelected => Color.FromArgb(60, 60, 63);
        public override Color MenuItemBorder => Color.Transparent;
        public override Color MenuBorder => Color.FromArgb(60, 60, 63);
        public override Color MenuItemSelectedGradientBegin => Color.FromArgb(60, 60, 63);
        public override Color MenuItemSelectedGradientEnd => Color.FromArgb(60, 60, 63);
        public override Color MenuItemPressedGradientBegin => Color.FromArgb(40, 40, 42);
        public override Color MenuItemPressedGradientEnd => Color.FromArgb(40, 40, 42);
        public override Color ToolStripDropDownBackground => Color.FromArgb(40, 41, 45);
        public override Color ImageMarginGradientBegin => Color.FromArgb(40, 41, 45);
        public override Color ImageMarginGradientMiddle => Color.FromArgb(40, 41, 45);
        public override Color ImageMarginGradientEnd => Color.FromArgb(40, 41, 45);
    }
}