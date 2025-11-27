using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using WindowsFormsApp1.Data;

namespace WindowsFormsApp1.Forms
{
    public partial class BookReaderForm : Form
    {
        private readonly Book _book;
        private readonly BookReaderService _readerService;
        private List<BookChapter> _chapters;
        private int _currentChapterIndex = 0;
        private int? _targetChapter = null;
        private int? _targetPosition = null;
        private ContextMenuStrip _floatingMenu;

        // UI Controls
        private TransparentRichTextBox contentBox;
        private Panel mainContainer;
        private Panel pagePanel;
        private Panel topBar;
        private Panel pnlSettings;

        // Sidebar & TOC
        private Panel pnlTOC;
        private Button btnSideToggle;
        private ListBox lstChapters;
        private ModernScrollBar tocScrollBar;

        // Window Controls
        private Panel pnlWindowControls;
        private Button btnMin, btnMax, btnCloseWin;

        // Navigation & Scroll
        private Button btnPrevFloat, btnNextFloat;
        private Panel progressBar, progressFill;
        private ModernScrollBar pageScrollBar;

        private Label lblTitle;
        private Label lblChapterInfo;
        private float currentFontSize = 14f;
        private string currentFontFamily = "Segoe UI";
        private Theme currentTheme = Theme.Light;

        private enum Theme { Light, Dark, Sepia }

        [DllImport("user32.dll")] static extern bool HideCaret(IntPtr hWnd);

        public BookReaderForm(Book book, int? jumpChapter = null, int? jumpPos = null)
        {
            this.DoubleBuffered = true;
            this.AutoScaleMode = AutoScaleMode.None;
            this.ClientSize = new Size(1200, 800);
            this.FormBorderStyle = FormBorderStyle.None;
            this.WindowState = FormWindowState.Maximized;
            this.ResizeRedraw = true;

            _book = book;
            _targetChapter = jumpChapter;
            _targetPosition = jumpPos;
            _readerService = new BookReaderService(DataManager.Instance);

            SetupModernUI();
            InitializeFloatingMenu();

            this.Load += BookReaderForm_Load;
            this.FormClosing += BookReaderForm_FormClosing;
            this.Resize += BookReaderForm_Resize;
            this.KeyPreview = true;
            this.KeyDown += BookReaderForm_KeyDown;
        }

        // Xử lý kéo giãn cửa sổ
        protected override void WndProc(ref Message m)
        {
            const int WM_NCHITTEST = 0x0084;
            const int HTCLIENT = 1; const int HTTOPLEFT = 13; const int HTTOPRIGHT = 14; const int HTBOTTOMLEFT = 16; const int HTBOTTOMRIGHT = 17; const int HTLEFT = 10; const int HTRIGHT = 11; const int HTTOP = 12; const int HTBOTTOM = 15;
            base.WndProc(ref m);
            if (this.WindowState == FormWindowState.Maximized) return;
            if (m.Msg == WM_NCHITTEST && (int)m.Result == HTCLIENT)
            {
                Point p = PointToClient(new Point(m.LParam.ToInt32()));
                int b = 10;
                if (p.Y <= b) { if (p.X <= b) m.Result = (IntPtr)HTTOPLEFT; else if (p.X >= ClientSize.Width - b) m.Result = (IntPtr)HTTOPRIGHT; else m.Result = (IntPtr)HTTOP; }
                else if (p.Y >= ClientSize.Height - b) { if (p.X <= b) m.Result = (IntPtr)HTBOTTOMLEFT; else if (p.X >= ClientSize.Width - b) m.Result = (IntPtr)HTBOTTOMRIGHT; else m.Result = (IntPtr)HTBOTTOM; }
                else if (p.X <= b) m.Result = (IntPtr)HTLEFT; else if (p.X >= ClientSize.Width - b) m.Result = (IntPtr)HTRIGHT;
            }
        }

        private void SetupModernUI()
        {
            this.Padding = new Padding(2);

            // 1. TOP BAR
            topBar = new Panel { Dock = DockStyle.Top, Height = 50, Padding = new Padding(0), BackColor = Color.Transparent };
            InitializeWindowControls();
            topBar.Controls.Add(pnlWindowControls);
            var btnSettings = CreateIconButton("Aa", 45, (s, e) => ToggleSettings()); btnSettings.Dock = DockStyle.Right; topBar.Controls.Add(btnSettings);
            var btnBack = CreateIconButton("←", 50, (s, e) => this.Close()); btnBack.Dock = DockStyle.Left; topBar.Controls.Add(btnBack);
            lblTitle = new Label { Text = _book.Title.ToUpper(), TextAlign = ContentAlignment.MiddleCenter, Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10, FontStyle.Bold), AutoEllipsis = true, ForeColor = Color.Gray };
            lblTitle.MouseDown += (s, e) => { if (e.Button == MouseButtons.Left) { ReleaseCapture(); SendMessage(Handle, 0xA1, 0x2, 0); } };
            topBar.Controls.Add(lblTitle);

            // 2. MAIN CONTAINER
            mainContainer = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0) };

            // 3. PAGE PANEL
            pagePanel = new Panel { Anchor = AnchorStyles.Top | AnchorStyles.Bottom, Padding = new Padding(50, 40, 20, 40), BackColor = Color.White };
            pagePanel.Paint += PagePanel_Paint;

            contentBox = new TransparentRichTextBox { Dock = DockStyle.Fill, BorderStyle = BorderStyle.None, ReadOnly = true, ScrollBars = RichTextBoxScrollBars.None, HideSelection = false, ShortcutsEnabled = true, Cursor = Cursors.IBeam, BackColor = Color.White };
            contentBox.MouseUp += ContentBox_MouseUp;
            contentBox.GotFocus += (s, e) => HideCaret(contentBox.Handle);
            contentBox.Click += (s, e) => HideCaret(contentBox.Handle);

            // --- [MODERN SCROLLBAR] ---
            pageScrollBar = new ModernScrollBar
            {
                Dock = DockStyle.Right,
                Width = 16, // Rộng hơn chút để dễ bấm
                Visible = true,
                ThumbColor = Color.Gray // Màu mặc định đậm hơn
            };
            pageScrollBar.Scroll += (s, e) => contentBox.SetVerticalScroll(pageScrollBar.Value);
            contentBox.CustomMouseWheel += (s, e) => UpdatePageScrollbar();

            pagePanel.Controls.Add(contentBox);
            pagePanel.Controls.Add(pageScrollBar);
            mainContainer.Controls.Add(pagePanel);

            // 4. NAVIGATION
            btnPrevFloat = CreateFloatingNavButton("❮", true);
            btnNextFloat = CreateFloatingNavButton("❯", false);
            progressBar = new Panel { Dock = DockStyle.Bottom, Height = 4, BackColor = Color.FromArgb(230, 230, 230) };
            progressFill = new Panel { Dock = DockStyle.Left, Width = 0, BackColor = Color.FromArgb(0, 120, 215) };
            progressBar.Controls.Add(progressFill);

            // 5. PANELS
            InitializeSettingsPanel();
            InitializeTOCPanel();
            InitializeSideToggleButton();

            this.Controls.Add(pnlSettings);
            this.Controls.Add(btnSideToggle);
            this.Controls.Add(pnlTOC);
            mainContainer.Controls.Add(btnPrevFloat); mainContainer.Controls.Add(btnNextFloat);
            this.Controls.Add(mainContainer);
            this.Controls.Add(progressBar);
            this.Controls.Add(topBar);

            ApplyTheme(Theme.Light);
        }

        private void UpdatePageScrollbar()
        {
            int min = 0;
            int max = contentBox.GetVerticalScrollRange();
            int pos = contentBox.GetVerticalScrollPosition();
            int largeChange = contentBox.ClientSize.Height;

            // Nếu nội dung ngắn hơn trang giấy, ẩn thanh cuộn
            if (max <= largeChange) pageScrollBar.Visible = false;
            else
            {
                pageScrollBar.Visible = true;
                pageScrollBar.SetScrollValues(pos, min, max, largeChange);
            }
        }

        private void PagePanel_Paint(object sender, PaintEventArgs e)
        {
            int radius = 20;
            Rectangle r = pagePanel.ClientRectangle; r.Width -= 1; r.Height -= 1;
            GraphicsPath path = new GraphicsPath();
            path.AddArc(r.X, r.Y, radius, radius, 180, 90);
            path.AddArc(r.Right - radius, r.Y, radius, radius, 270, 90);
            path.AddArc(r.Right - radius, r.Bottom - radius, radius, radius, 0, 90);
            path.AddArc(r.X, r.Bottom - radius, radius, radius, 90, 90);
            path.CloseFigure();
            pagePanel.Region = new Region(path);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (Pen p = new Pen(Color.FromArgb(30, 0, 0, 0), 1)) e.Graphics.DrawPath(p, path);
        }

        private void InitializeTOCPanel()
        {
            pnlTOC = new Panel { Width = 300, Dock = DockStyle.Right, Visible = false, BorderStyle = BorderStyle.FixedSingle, Padding = new Padding(1) };
            var lblHeader = new Label { Text = "MỤC LỤC", Dock = DockStyle.Top, Height = 40, TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = Color.Black };
            Panel listContainer = new Panel { Dock = DockStyle.Fill };
            lstChapters = new ListBox { Dock = DockStyle.Fill, BorderStyle = BorderStyle.None, Font = new Font("Segoe UI", 10), ItemHeight = 30, DrawMode = DrawMode.OwnerDrawFixed, ForeColor = Color.Black };
            lstChapters.DrawItem += LstChapters_DrawItem;
            lstChapters.SelectedIndexChanged += (s, e) => { if (lstChapters.SelectedIndex >= 0) { SaveCurrentProgress(); _currentChapterIndex = lstChapters.SelectedIndex; DisplayFullChapter(_currentChapterIndex); ToggleTOC(); } };
            tocScrollBar = new ModernScrollBar { Dock = DockStyle.Right, Width = 8 };
            tocScrollBar.Scroll += (s, e) => { if (lstChapters.Items.Count > 0) { int index = (int)((float)tocScrollBar.Value / tocScrollBar.Maximum * lstChapters.Items.Count); lstChapters.TopIndex = Math.Min(index, lstChapters.Items.Count - 1); } };
            listContainer.Controls.Add(lstChapters); listContainer.Controls.Add(tocScrollBar);
            pnlTOC.Controls.AddRange(new Control[] { listContainer, lblHeader });
        }

        private void InitializeSettingsPanel() { pnlSettings = new Panel { Size = new Size(250, 180), Visible = false, BorderStyle = BorderStyle.FixedSingle }; var flow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, Padding = new Padding(10) }; var lblSize = new Label { Text = "Cỡ chữ", AutoSize = true, ForeColor = Color.Gray }; var pnlSize = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight }; pnlSize.Controls.Add(CreateSmallButton("-", (s, e) => ChangeFontSize(-2))); pnlSize.Controls.Add(CreateSmallButton("+", (s, e) => ChangeFontSize(2))); var lblTheme = new Label { Text = "Giao diện", AutoSize = true, ForeColor = Color.Gray, Margin = new Padding(0, 10, 0, 0) }; var pnlTheme = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight }; pnlTheme.Controls.Add(CreateSmallButton("Dark", (s, e) => ApplyTheme(Theme.Dark))); pnlTheme.Controls.Add(CreateSmallButton("Light", (s, e) => ApplyTheme(Theme.Light))); pnlTheme.Controls.Add(CreateSmallButton("Sepia", (s, e) => ApplyTheme(Theme.Sepia))); flow.Controls.AddRange(new Control[] { lblSize, pnlSize, lblTheme, pnlTheme }); pnlSettings.Controls.Add(flow); }
        private Button CreateIconButton(string text, int width, EventHandler onClick) { var btn = new Button { Text = text, Width = width, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI Symbol", 14, FontStyle.Regular), Cursor = Cursors.Hand, Dock = DockStyle.Left }; btn.FlatAppearance.BorderSize = 0; btn.Click += onClick; return btn; }
        private Button CreateSmallButton(string text, EventHandler onClick) { var btn = new Button { Text = text, Size = new Size(60, 30), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Margin = new Padding(3) }; btn.Click += onClick; return btn; }
        private void LstChapters_DrawItem(object sender, DrawItemEventArgs e) { if (e.Index < 0) return; e.DrawBackground(); bool isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected; if (isSelected) e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(0, 120, 215)), e.Bounds); Color textColor = isSelected ? Color.White : Color.Black; TextRenderer.DrawText(e.Graphics, lstChapters.Items[e.Index].ToString(), e.Font, e.Bounds, textColor, TextFormatFlags.VerticalCenter | TextFormatFlags.Left); e.DrawFocusRectangle(); }
        private void ToggleSettings() { pnlSettings.Location = new Point(topBar.Width - pnlSettings.Width - 10, topBar.Height + 5); pnlSettings.Visible = !pnlSettings.Visible; pnlSettings.BringToFront(); }
        private void ToggleTOC() { pnlTOC.Visible = !pnlTOC.Visible; pnlTOC.BringToFront(); UpdateSideTogglePosition(); }

        private void ApplyTheme(Theme theme)
        {
            currentTheme = theme;
            Color uiBg, paperBg, fg, accent;
            // Màu thanh cuộn
            Color thumbColor, thumbHover;

            switch (theme)
            {
                case Theme.Light:
                    uiBg = Color.FromArgb(243, 243, 243); paperBg = Color.White; fg = Color.FromArgb(30, 30, 30); accent = Color.LightGray; this.BackColor = Color.Gray;
                    thumbColor = Color.FromArgb(180, 180, 180); thumbHover = Color.FromArgb(100, 100, 100);
                    break;
                case Theme.Sepia:
                    uiBg = Color.FromArgb(238, 232, 222); paperBg = Color.FromArgb(250, 244, 232); fg = Color.FromArgb(95, 75, 50); accent = Color.FromArgb(210, 200, 180); this.BackColor = Color.FromArgb(180, 170, 150);
                    thumbColor = Color.FromArgb(200, 190, 170); thumbHover = Color.FromArgb(160, 150, 130);
                    break;
                case Theme.Dark:
                default:
                    uiBg = Color.FromArgb(25, 25, 25); paperBg = Color.FromArgb(40, 40, 40); fg = Color.FromArgb(220, 220, 220); accent = Color.FromArgb(60, 60, 60); this.BackColor = Color.FromArgb(60, 60, 60);
                    thumbColor = Color.FromArgb(80, 80, 80); thumbHover = Color.FromArgb(150, 150, 150);
                    break;
            }
            mainContainer.BackColor = uiBg; topBar.BackColor = uiBg; topBar.ForeColor = fg;
            pagePanel.BackColor = paperBg; contentBox.BackColor = paperBg; contentBox.ForeColor = fg;

            // Cập nhật Scrollbar
            pageScrollBar.ThumbColor = thumbColor;
            pageScrollBar.ThumbHoverColor = thumbHover;
            pageScrollBar.BackColor = paperBg;

            if (tocScrollBar != null) { tocScrollBar.ThumbColor = thumbColor; tocScrollBar.ThumbHoverColor = thumbHover; tocScrollBar.BackColor = Color.Transparent; }

            if (btnPrevFloat != null) btnPrevFloat.ForeColor = (theme == Theme.Dark) ? Color.Gray : Color.DarkGray;
            if (btnNextFloat != null) btnNextFloat.ForeColor = (theme == Theme.Dark) ? Color.Gray : Color.DarkGray;
            if (progressBar != null) progressBar.BackColor = (theme == Theme.Dark) ? Color.FromArgb(50, 50, 50) : Color.FromArgb(220, 220, 220);
            if (pnlSettings != null) { pnlSettings.BackColor = paperBg; pnlSettings.ForeColor = fg; foreach (Control c in pnlSettings.Controls[0].Controls) if (c is FlowLayoutPanel fp) foreach (Control b in fp.Controls) if (b is Button btn) { btn.BackColor = accent; btn.ForeColor = fg; btn.FlatAppearance.BorderSize = 0; } }
            if (pnlTOC != null) { pnlTOC.BackColor = Color.FromArgb(250, 250, 250); lstChapters.BackColor = Color.FromArgb(250, 250, 250); }
            UpdateButtonStyle(topBar, fg); UpdateThemeColors();
            if (_chapters != null && _chapters.Count > 0) DisplayFullChapter(_currentChapterIndex, contentBox.SelectionStart);
        }

        private void UpdateButtonStyle(Panel p, Color fg) { foreach (Control c in p.Controls) if (c is Button b) { b.ForeColor = fg; b.FlatAppearance.MouseOverBackColor = Color.FromArgb(20, 128, 128, 128); } }
        private void ChangeFontSize(float delta) { currentFontSize = Math.Max(10, Math.Min(30, currentFontSize + delta)); DisplayFullChapter(_currentChapterIndex, contentBox.SelectionStart); }
        private Button CreateFloatingNavButton(string text, bool isLeft) { Button btn = new Button { Text = text, Size = new Size(50, 50), FlatStyle = FlatStyle.Flat, BackColor = Color.Transparent, ForeColor = Color.Gray, Font = new Font("Segoe UI", 16, FontStyle.Regular), Cursor = Cursors.Hand, Anchor = isLeft ? (AnchorStyles.Left | AnchorStyles.Left) : (AnchorStyles.Right | AnchorStyles.Right) }; btn.FlatAppearance.BorderSize = 0; btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(20, 0, 0, 0); btn.FlatAppearance.MouseDownBackColor = Color.FromArgb(40, 0, 0, 0); GraphicsPath path = new GraphicsPath(); path.AddEllipse(0, 0, 50, 50); btn.Region = new Region(path); btn.Click += (s, e) => NavigateChapter(isLeft ? -1 : 1); btn.MouseEnter += (s, e) => btn.ForeColor = Color.Black; btn.MouseLeave += (s, e) => btn.ForeColor = Color.Gray; return btn; }
        private void UpdateFloatingButtonsPosition() { if (btnPrevFloat == null || btnNextFloat == null) return; int y = (mainContainer.Height - 50) / 2; btnPrevFloat.Location = new Point(20, y); btnNextFloat.Location = new Point(mainContainer.Width - 70, y); }
        private void InitializeWindowControls() { pnlWindowControls = new Panel { Dock = DockStyle.Right, Width = 150, BackColor = Color.Transparent }; btnCloseWin = CreateStyledWindowButton("✕", 50, (s, e) => this.Close()); btnCloseWin.MouseEnter += (s, e) => { btnCloseWin.BackColor = Color.Red; btnCloseWin.ForeColor = Color.White; }; btnCloseWin.MouseLeave += (s, e) => { btnCloseWin.BackColor = Color.Transparent; UpdateThemeColors(); }; string maxIcon = (this.WindowState == FormWindowState.Maximized) ? "❐" : "☐"; btnMax = CreateStyledWindowButton(maxIcon, 50, (s, e) => ToggleMaximize()); btnMin = CreateStyledWindowButton("—", 50, (s, e) => this.WindowState = FormWindowState.Minimized); pnlWindowControls.Controls.Clear(); pnlWindowControls.Controls.Add(btnCloseWin); pnlWindowControls.Controls.Add(CreateSeparator()); pnlWindowControls.Controls.Add(btnMax); pnlWindowControls.Controls.Add(CreateSeparator()); pnlWindowControls.Controls.Add(btnMin); }
        private Button CreateStyledWindowButton(string text, int width, EventHandler onClick) { var btn = new Button { Text = text, Dock = DockStyle.Right, Width = width, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 11), Cursor = Cursors.Hand, BackColor = Color.Transparent }; btn.FlatAppearance.BorderSize = 0; btn.Click += onClick; return btn; }
        private Panel CreateSeparator() { return new Panel { Width = 1, Dock = DockStyle.Right, BackColor = Color.FromArgb(50, 128, 128, 128), Padding = new Padding(0, 12, 0, 12) }; }
        private void ToggleMaximize() { if (this.WindowState == FormWindowState.Maximized) { this.WindowState = FormWindowState.Normal; btnMax.Text = "☐"; } else { this.WindowState = FormWindowState.Maximized; btnMax.Text = "❐"; } }
        private void UpdateThemeColors() { Color fg = (currentTheme == Theme.Light || currentTheme == Theme.Sepia) ? Color.Black : Color.White; if (btnMin != null) btnMin.ForeColor = fg; if (btnMax != null) btnMax.ForeColor = fg; if (btnCloseWin != null && btnCloseWin.BackColor != Color.Red) btnCloseWin.ForeColor = fg; }
        private void InitializeSideToggleButton() { btnSideToggle = new Button { Text = "‹", Size = new Size(24, 60), BackColor = Color.FromArgb(200, 200, 200), ForeColor = Color.Black, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Font = new Font("Segoe UI", 14, FontStyle.Bold), TextAlign = ContentAlignment.MiddleCenter, Anchor = AnchorStyles.Right | AnchorStyles.Top }; btnSideToggle.FlatAppearance.BorderSize = 0; GraphicsPath path = new GraphicsPath(); int r = 10; path.AddArc(0, 0, r, r, 180, 90); path.AddLine(r, 0, btnSideToggle.Width, 0); path.AddLine(btnSideToggle.Width, btnSideToggle.Height, r, btnSideToggle.Height); path.AddArc(0, btnSideToggle.Height - r, r, r, 90, 90); path.CloseFigure(); btnSideToggle.Region = new Region(path); btnSideToggle.Click += (s, e) => ToggleTOC(); }
        private void UpdateSideTogglePosition() { int y = (this.ClientSize.Height - btnSideToggle.Height) / 2; int x = this.ClientSize.Width - btnSideToggle.Width; if (pnlTOC.Visible) { x -= pnlTOC.Width; btnSideToggle.Text = "›"; } else { btnSideToggle.Text = "‹"; } btnSideToggle.Location = new Point(x, y); btnSideToggle.BringToFront(); }
        private void InitializeFloatingMenu() { _floatingMenu = new ContextMenuStrip(); var colors = new[] { (Name: "Vàng", Color: Color.FromArgb(255, 228, 100)), (Name: "Xanh", Color: Color.FromArgb(135, 238, 144)), (Name: "Đỏ", Color: Color.FromArgb(255, 160, 160)), (Name: "Tím", Color: Color.FromArgb(200, 170, 250)) }; foreach (var c in colors) { var item = new ToolStripMenuItem(c.Name); Bitmap bmp = new Bitmap(16, 16); using (Graphics g = Graphics.FromImage(bmp)) { g.Clear(c.Color); g.DrawRectangle(Pens.Silver, 0, 0, 15, 15); } item.Image = bmp; item.Click += (s, e) => CreateHighlight(c.Color); _floatingMenu.Items.Add(item); } _floatingMenu.Items.Add(new ToolStripSeparator()); var btnNote = new ToolStripMenuItem("📝 Thêm Ghi Chú"); btnNote.Click += (s, e) => HandleNoteInput(); _floatingMenu.Items.Add(btnNote); }
        private void ContentBox_MouseUp(object sender, MouseEventArgs e) { if (e.Button == MouseButtons.Left && contentBox.SelectionLength > 0) _floatingMenu.Show(Cursor.Position); else { if (pnlSettings.Visible && !pnlSettings.Bounds.Contains(PointToClient(Cursor.Position))) pnlSettings.Visible = false; } }
        private void CreateHighlight(Color color, string noteContent = "") { if (contentBox.SelectionLength == 0) return; try { var hl = new Highlight { BookId = _book.Id, UserId = DataManager.Instance.GetCurrentUser(), ChapterIndex = _currentChapterIndex, StartIndex = contentBox.SelectionStart, Length = contentBox.SelectionLength, SelectedText = contentBox.SelectedText, Note = noteContent, ColorHex = ColorTranslator.ToHtml(color) }; DataManager.Instance.AddHighlight(hl); contentBox.SelectionBackColor = color; contentBox.SelectionLength = 0; } catch (Exception ex) { MessageBox.Show("Lỗi: " + ex.Message); } }
        private void HandleNoteInput() { if (contentBox.SelectionLength == 0) return; using (var dlg = new NoteDialog()) { if (dlg.ShowDialog() == DialogResult.OK) CreateHighlight(Color.Orange, dlg.NoteText); } }
        private void BookReaderForm_Load(object sender, EventArgs e) { BookReaderForm_Resize(this, null); LoadBookDataInitial(); UpdateSideTogglePosition(); }
        private void BookReaderForm_Resize(object sender, EventArgs e) { if (mainContainer == null || pagePanel == null) return; int maxWidth = 850; int w = Math.Min(maxWidth, mainContainer.Width - 100); pagePanel.Size = new Size(w, mainContainer.Height - 40); pagePanel.Location = new Point((mainContainer.Width - w) / 2, 20); pagePanel.Invalidate(); if (pnlTOC != null) pnlTOC.Height = this.Height - topBar.Height; UpdateSideTogglePosition(); UpdateFloatingButtonsPosition(); UpdateProgressBar(); UpdatePageScrollbar(); }
        private void LoadBookDataInitial() { BackgroundWorker worker = new BackgroundWorker(); worker.DoWork += (s, args) => { try { _chapters = _readerService.ReadBookContent(_book) ?? new List<BookChapter>(); } catch { _chapters = new List<BookChapter>(); } }; worker.RunWorkerCompleted += (s, args) => { if (_chapters.Count == 0) _chapters.Add(new BookChapter { ChapterTitle = "Lỗi", Content = "Không thể tải nội dung." }); lstChapters.Items.Clear(); foreach (var c in _chapters) lstChapters.Items.Add(c.ChapterTitle); if (_targetChapter.HasValue && _targetPosition.HasValue) { _currentChapterIndex = Math.Min(Math.Max(0, _targetChapter.Value), _chapters.Count - 1); DisplayFullChapter(_currentChapterIndex, _targetPosition.Value); } else { var pos = _readerService.GetReadingPosition(_book.Id, DataManager.Instance.GetCurrentUser()); _currentChapterIndex = Math.Min(Math.Max(0, pos.chapter), _chapters.Count - 1); DisplayFullChapter(_currentChapterIndex, pos.position); } }; worker.RunWorkerAsync(); }
        private void DisplayFullChapter(int index, int scrollPosition = 0) { if (index < 0 || index >= _chapters.Count) return; this.SuspendLayout(); var chapter = _chapters[index]; contentBox.Clear(); contentBox.SelectionFont = new Font(currentFontFamily, currentFontSize + 8, FontStyle.Bold); contentBox.SelectionColor = currentTheme == Theme.Light ? Color.Black : (currentTheme == Theme.Dark ? Color.White : Color.FromArgb(60, 40, 20)); contentBox.SelectionAlignment = HorizontalAlignment.Center; contentBox.AppendText("\n" + chapter.ChapterTitle + "\n\n\n"); contentBox.SelectionFont = new Font(currentFontFamily, currentFontSize, FontStyle.Regular); contentBox.SelectionColor = contentBox.ForeColor; contentBox.SelectionAlignment = HorizontalAlignment.Left; contentBox.SelectionRightIndent = 10; contentBox.SelectionIndent = 10; string text = (chapter.Content ?? "").Replace("\n", "\n\n"); contentBox.AppendText(text); contentBox.AppendText("\n\n\n"); ReloadHighlights(index); lblTitle.Text = chapter.ChapterTitle.ToUpper(); if (index < lstChapters.Items.Count) lstChapters.SelectedIndex = index; contentBox.Select(0, 0); if (scrollPosition > 0 && scrollPosition < contentBox.TextLength) contentBox.SelectionStart = scrollPosition; contentBox.ScrollToCaret(); UpdateProgressBar(); this.ResumeLayout(); contentBox.Focus(); UpdatePageScrollbar(); }
        private void UpdateProgressBar() { if (_chapters == null || _chapters.Count == 0 || progressFill == null) return; double progress = (double)(_currentChapterIndex + 1) / _chapters.Count; progressFill.Width = (int)(progressBar.Width * progress); }
        private void ReloadHighlights(int chapterIndex) { var highlights = DataManager.Instance.GetHighlightsForBook(_book.Id); int originalStart = contentBox.SelectionStart; foreach (var hl in highlights) { if (hl.ChapterIndex == chapterIndex) { if (hl.StartIndex >= 0 && (hl.StartIndex + hl.Length) <= contentBox.TextLength) { contentBox.Select(hl.StartIndex, hl.Length); try { contentBox.SelectionBackColor = ColorTranslator.FromHtml(hl.ColorHex); } catch { contentBox.SelectionBackColor = Color.Yellow; } } } } contentBox.Select(originalStart, 0); }
        private void NavigateChapter(int step) { int newIndex = _currentChapterIndex + step; if (newIndex >= 0 && newIndex < _chapters.Count) { SaveCurrentProgress(); _currentChapterIndex = newIndex; DisplayFullChapter(_currentChapterIndex); } }
        private void SaveCurrentProgress() { if (_chapters == null || _chapters.Count == 0) return; int currentPos = contentBox.GetCharIndexFromPosition(new Point(10, 10)); _readerService.SaveReadingPosition(_book.Id, DataManager.Instance.GetCurrentUser(), _currentChapterIndex, currentPos); }
        private void BookReaderForm_FormClosing(object sender, FormClosingEventArgs e) { SaveCurrentProgress(); }
        private void BookReaderForm_KeyDown(object sender, KeyEventArgs e) { if (e.KeyCode == Keys.Left) NavigateChapter(-1); else if (e.KeyCode == Keys.Right) NavigateChapter(1); else if (e.KeyCode == Keys.Escape) this.Close(); }

        [System.Runtime.InteropServices.DllImport("user32.dll")] public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [System.Runtime.InteropServices.DllImport("user32.dll")] public static extern bool ReleaseCapture();
    }

    // --- [ĐÃ SỬA LỖI] MODERN SCROLLBAR ---
    public class ModernScrollBar : Control
    {
        public int Value { get; private set; } = 0;
        public int Maximum { get; private set; } = 100;
        public int Minimum { get; private set; } = 0;
        public int LargeChange { get; private set; } = 10;

        public Color ThumbColor { get; set; } = Color.Silver;
        public Color ThumbHoverColor { get; set; } = Color.Gray;

        public event EventHandler Scroll;
        private bool isDragging = false;
        private bool isHovered = false;
        private int clickY, thumbY;

        public ModernScrollBar()
        {
            // [FIX] Cho phép nền trong suốt
            SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            this.DoubleBuffered = true;
            this.Width = 12;
            this.BackColor = Color.Transparent;
        }

        public void SetScrollValues(int val, int min, int max, int largeChange)
        {
            this.Value = val; this.Minimum = min; this.Maximum = max; this.LargeChange = largeChange; this.Invalidate();
        }

        protected override void OnMouseEnter(EventArgs e) { isHovered = true; Invalidate(); }
        protected override void OnMouseLeave(EventArgs e) { isHovered = false; Invalidate(); }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (Maximum <= 0) return;

            int thumbHeight = Math.Max(40, (int)((float)LargeChange / Maximum * Height));
            int trackHeight = Height - thumbHeight;
            int thumbPos = (int)((float)Value / (Maximum - LargeChange + 1) * trackHeight);
            thumbY = Math.Max(0, Math.Min(trackHeight, thumbPos));

            // Chọn màu: đậm hơn khi hover/kéo
            Color c = (isDragging || isHovered) ? ThumbHoverColor : ThumbColor;

            using (SolidBrush brush = new SolidBrush(c))
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                // Vẽ viên thuốc bo tròn
                Rectangle thumbRect = new Rectangle(2, thumbY, Width - 4, thumbHeight);
                GraphicsPath path = new GraphicsPath();
                int r = thumbRect.Width;
                path.AddArc(thumbRect.X, thumbRect.Y, r, r, 180, 180);
                path.AddArc(thumbRect.X, thumbRect.Bottom - r, r, r, 0, 180);
                path.CloseFigure();
                e.Graphics.FillPath(brush, path);
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            int thumbHeight = Math.Max(40, (int)((float)LargeChange / Maximum * Height));
            if (e.Y >= thumbY && e.Y <= thumbY + thumbHeight) { isDragging = true; clickY = e.Y - thumbY; }
            else
            {
                float ratio = (float)e.Y / (Height - thumbHeight);
                Value = (int)(ratio * (Maximum - LargeChange));
                Value = Math.Max(Minimum, Math.Min(Maximum - LargeChange, Value));
                Scroll?.Invoke(this, EventArgs.Empty); Invalidate();
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (isDragging)
            {
                int thumbHeight = Math.Max(40, (int)((float)LargeChange / Maximum * Height));
                int trackHeight = Height - thumbHeight;
                int newThumbY = e.Y - clickY;
                newThumbY = Math.Max(0, Math.Min(trackHeight, newThumbY));
                float ratio = (float)newThumbY / trackHeight;
                Value = (int)(ratio * (Maximum - LargeChange));
                Value = Math.Max(Minimum, Math.Min(Maximum - LargeChange, Value));
                Scroll?.Invoke(this, EventArgs.Empty); Invalidate();
            }
        }
        protected override void OnMouseUp(MouseEventArgs e) { isDragging = false; Invalidate(); }
    }

    // --- [FIX] TRANSPARENT RICHTEXTBOX (Hỗ trợ lăn chuột) ---
    public class TransparentRichTextBox : RichTextBox
    {
        public event EventHandler CustomMouseWheel;
        [DllImport("user32.dll")] static extern int GetScrollPos(IntPtr hWnd, int nBar);
        [DllImport("user32.dll")] static extern int SetScrollPos(IntPtr hWnd, int nBar, int nPos, bool bRedraw);
        [DllImport("user32.dll")] static extern bool GetScrollRange(IntPtr hWnd, int nBar, out int lpMinPos, out int lpMaxPos);
        [DllImport("user32.dll")] static extern int SendMessage(IntPtr hWnd, int wMsg, int wParam, int lParam);

        private const int SB_VERT = 1;
        private const int WM_VSCROLL = 0x0115;
        private const int WM_MOUSEWHEEL = 0x020A;
        private const int SB_LINEUP = 0;
        private const int SB_LINEDOWN = 1;

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_MOUSEWHEEL)
            {
                // [FIX] Tự xử lý sự kiện lăn chuột
                short delta = (short)((m.WParam.ToInt64() >> 16) & 0xFFFF);
                int direction = (delta > 0) ? SB_LINEUP : SB_LINEDOWN;

                // Cuộn 3 dòng cho mượt
                for (int i = 0; i < 3; i++) SendMessage(this.Handle, WM_VSCROLL, direction, 0);

                // Cập nhật thanh cuộn ảo
                CustomMouseWheel?.Invoke(this, EventArgs.Empty);
                return;
            }
            base.WndProc(ref m);
        }

        public int GetVerticalScrollPosition() { return GetScrollPos(this.Handle, SB_VERT); }
        public int GetVerticalScrollRange() { int min, max; GetScrollRange(this.Handle, SB_VERT, out min, out max); return max; }
        public void SetVerticalScroll(int value) { SetScrollPos(this.Handle, SB_VERT, value, true); SendMessage(this.Handle, WM_VSCROLL, 4 + 0x10000 * value, 0); }
    }
}