using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using WindowsFormsApp1.Data;

namespace WindowsFormsApp1.Forms
{
    public partial class BookReaderForm : Form
    {
        // ======================= CORE DATA =======================
        private readonly Book _book;
        private readonly BookReaderService _readerService;
        private readonly GeminiService _geminiService;
        private List<BookChapter> _chapters = new List<BookChapter>();

        // [QUAN TRỌNG] Biến cờ xác định đây có phải sách PDF không
        private bool _isPdf = false;

        // ======================= STATE =======================
        private int _currentChapterIndex = 0;
        private int? _targetChapter = null;
        private int? _targetPosition = null;

        // ======================= UI CONTROLS =======================
        private TransparentRichTextBox contentBox;
        private Panel mainContainer;
        private Panel pagePanel;
        private Panel topBar;
        private Label lblTitle;
        private Panel pnlSettings;
        private Panel progressBar, progressFill;
        private ModernScrollBar pageScrollBar;
        private Button btnPrevFloat, btnNextFloat;
        private Panel pnlTOC;
        private Button btnSideToggle;
        private ListBox lstChapters;
        private ModernScrollBar tocScrollBar;
        private Panel pnlWindowControls;
        private Button btnMin, btnMax, btnCloseWin;
        private Button btnGeminiFloat;
        private Panel pnlChatBox;
        private RichTextBox rtbChatHistory;
        private TextBox txtChatInput;
        private Button btnSendChat;
        private bool isChatOpen = false;
        private float currentFontSize = 14f;
        private string currentFontFamily = "Segoe UI";
        private Theme currentTheme = Theme.Light;
        private enum Theme { Light, Dark, Sepia }
        private ContextMenuStrip _floatingMenu;

        [DllImport("user32.dll")] static extern bool HideCaret(IntPtr hWnd);
        [DllImport("user32.dll")] public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [DllImport("user32.dll")] public static extern bool ReleaseCapture();
        [DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        private static extern IntPtr CreateRoundRectRgn(int nLeftRect, int nTopRect, int nRightRect, int nBottomRect, int nWidthEllipse, int nHeightEllipse);

        public BookReaderForm(Book book, int? jumpChapter = null, int? jumpPos = null)
        {
            this.DoubleBuffered = true;
            this.AutoScaleMode = AutoScaleMode.None;
            this.ClientSize = new Size(1280, 800);
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.WindowState = FormWindowState.Maximized;

            _book = book;
            // [FIX] Xác định chắc chắn đây là PDF ngay từ đầu
            if (_book != null && !string.IsNullOrEmpty(_book.FilePath))
            {
                _isPdf = _book.FilePath.Trim().ToLower().EndsWith(".pdf");
            }

            _targetChapter = jumpChapter;
            _targetPosition = jumpPos;

            _readerService = new BookReaderService(DataManager.Instance);
            _geminiService = new GeminiService();

            SetupModernUI();
            InitializeFloatingMenu();
            InitializeGeminiChatUI();

            this.Load += BookReaderForm_Load;
            this.FormClosing += BookReaderForm_FormClosing;
            this.Resize += BookReaderForm_Resize;
            this.KeyPreview = true;
            this.KeyDown += BookReaderForm_KeyDown;
        }

        protected override void WndProc(ref Message m) { const int WM_NCHITTEST = 0x0084; const int HTCLIENT = 1; const int HTTOPLEFT = 13; const int HTTOPRIGHT = 14; const int HTBOTTOMLEFT = 16; const int HTBOTTOMRIGHT = 17; const int HTLEFT = 10; const int HTRIGHT = 11; const int HTTOP = 12; const int HTBOTTOM = 15; base.WndProc(ref m); if (this.WindowState == FormWindowState.Maximized) return; if (m.Msg == WM_NCHITTEST && (int)m.Result == HTCLIENT) { Point p = PointToClient(new Point(m.LParam.ToInt32())); int b = 10; if (p.Y <= b) { if (p.X <= b) m.Result = (IntPtr)HTTOPLEFT; else if (p.X >= ClientSize.Width - b) m.Result = (IntPtr)HTTOPRIGHT; else m.Result = (IntPtr)HTTOP; } else if (p.Y >= ClientSize.Height - b) { if (p.X <= b) m.Result = (IntPtr)HTBOTTOMLEFT; else if (p.X >= ClientSize.Width - b) m.Result = (IntPtr)HTBOTTOMRIGHT; else m.Result = (IntPtr)HTBOTTOM; } else if (p.X <= b) m.Result = (IntPtr)HTLEFT; else if (p.X >= ClientSize.Width - b) m.Result = (IntPtr)HTRIGHT; } }

        // --- CÁC HÀM XỬ LÝ CHÍNH (ĐÃ SỬA) ---

        private void DisplayFullChapter(int index, int scrollPosition = 0)
        {
            if (_chapters == null || index < 0 || index >= _chapters.Count) return;
            this.SuspendLayout();

            var chapter = _chapters[index];
            contentBox.Clear();

            // Tiêu đề
            contentBox.SelectionAlignment = HorizontalAlignment.Center;
            contentBox.SelectionFont = new Font(currentFontFamily, currentFontSize + 14, FontStyle.Bold);
            contentBox.SelectionColor = currentTheme == Theme.Light ? Color.FromArgb(20, 20, 20) : (currentTheme == Theme.Dark ? Color.White : Color.FromArgb(60, 40, 20));
            contentBox.AppendText("\n" + chapter.ChapterTitle + "\n\n");

            // Nội dung
            contentBox.SelectionAlignment = HorizontalAlignment.Left;
            contentBox.SelectionFont = new Font(currentFontFamily, currentFontSize, FontStyle.Regular);
            contentBox.SelectionColor = contentBox.ForeColor;
            contentBox.SelectionIndent = 20;
            contentBox.SelectionRightIndent = 20;

            if (chapter.Images != null && chapter.Images.Count > 0)
            {
                string[] parts = Regex.Split(chapter.Content, @"(\{\{IMG:[a-f0-9-]+\}\})");
                foreach (var part in parts)
                {
                    if (chapter.Images.ContainsKey(part))
                    {
                        // Gọi hàm chèn ảnh đã nâng cấp
                        InsertImageToRichTextBox(chapter.Images[part]);
                    }
                    else
                    {
                        contentBox.AppendText(part);
                    }
                }
            }
            else
            {
                contentBox.AppendText(chapter.Content);
            }

            contentBox.AppendText("\n\n\n\n");
            ReloadHighlights(index);
            lblTitle.Text = chapter.ChapterTitle.ToUpper();
            if (index < lstChapters.Items.Count) lstChapters.SelectedIndex = index;
            contentBox.Select(0, 0);
            if (scrollPosition > 0 && scrollPosition < contentBox.TextLength)
            {
                contentBox.SelectionStart = scrollPosition;
                contentBox.ScrollToCaret();
            }
            UpdateProgressBar();
            this.ResumeLayout();
            contentBox.Focus();
            UpdatePageScrollbar();
        }

        // [QUAN TRỌNG] Hàm chèn ảnh: Đảm bảo PDF luôn Full Width
        private void InsertImageToRichTextBox(Image img)
        {
            try
            {
                // 1. Tính toán kích thước hiển thị
                int containerWidth = contentBox.ClientSize.Width;
                int targetWidth;
                int targetHeight;

                // LOGIC QUYẾT ĐỊNH:
                // Nếu là file PDF -> Mặc định là Fit Width (phóng to hết cỡ).
                // Nếu không phải PDF nhưng ảnh rất to (> 300px) -> Cũng Fit Width luôn cho dễ đọc.
                bool shouldFitWidth = _isPdf || (img.Width > 300);

                if (shouldFitWidth)
                {
                    // Trừ lề rất nhỏ để ảnh to nhất có thể
                    int pagePadding = 10;
                    targetWidth = containerWidth - pagePadding;
                    // Tính chiều cao theo tỷ lệ
                    targetHeight = (int)((double)img.Height / img.Width * targetWidth);
                }
                else
                {
                    // Ảnh nhỏ (icon) trong Epub -> Giữ nguyên
                    targetWidth = img.Width;
                    targetHeight = img.Height;
                }

                // 2. Vẽ lại ảnh (High Quality)
                Bitmap resizedBitmap = new Bitmap(targetWidth, targetHeight);
                resizedBitmap.SetResolution(96, 96); // Fix lỗi DPI WinForms

                using (Graphics g = Graphics.FromImage(resizedBitmap))
                {
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.CompositingQuality = CompositingQuality.HighQuality;
                    g.SmoothingMode = SmoothingMode.HighQuality;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    g.DrawImage(img, 0, 0, targetWidth, targetHeight);
                }

                // 3. Paste vào khung
                Clipboard.SetImage(resizedBitmap);

                // Căn chỉnh lề
                if (shouldFitWidth)
                {
                    contentBox.SelectionIndent = 0; // Bỏ lề để ảnh tràn viền
                    contentBox.SelectionRightIndent = 0;
                    contentBox.SelectionAlignment = HorizontalAlignment.Center;
                }
                else
                {
                    contentBox.SelectionAlignment = HorizontalAlignment.Center;
                }

                contentBox.ReadOnly = false;
                if (contentBox.CanPaste(DataFormats.GetFormat(DataFormats.Bitmap)))
                {
                    contentBox.Paste();
                }
                contentBox.ReadOnly = true;

                // Khôi phục lề cho text sau đó
                contentBox.SelectionAlignment = HorizontalAlignment.Left;
                contentBox.SelectionIndent = 20;
                contentBox.SelectionRightIndent = 20;
                contentBox.AppendText("\n");
            }
            catch
            {
                contentBox.AppendText("\n[Ảnh lỗi]\n");
            }
        }

        // --- CÁC HÀM UI KHÁC (GIỮ NGUYÊN) ---
        private void InitializeGeminiChatUI() { btnGeminiFloat = new Button { Text = "✨", Size = new Size(56, 56), BackColor = Color.FromArgb(255, 110, 64), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI Emoji", 20, FontStyle.Regular), Cursor = Cursors.Hand, TextAlign = ContentAlignment.MiddleCenter }; btnGeminiFloat.FlatAppearance.BorderSize = 0; GraphicsPath path = new GraphicsPath(); path.AddEllipse(0, 0, 56, 56); btnGeminiFloat.Region = new Region(path); btnGeminiFloat.Click += (s, e) => ToggleChatWindow(); btnGeminiFloat.MouseEnter += (s, e) => btnGeminiFloat.BackColor = Color.FromArgb(255, 138, 101); btnGeminiFloat.MouseLeave += (s, e) => btnGeminiFloat.BackColor = Color.FromArgb(255, 110, 64); pnlChatBox = new Panel { Size = new Size(380, 500), BackColor = Color.White, Visible = false, Padding = new Padding(1) }; pnlChatBox.Paint += (s, e) => { e.Graphics.SmoothingMode = SmoothingMode.AntiAlias; using (Pen p = new Pen(Color.LightGray, 1)) { Rectangle r = pnlChatBox.ClientRectangle; r.Width -= 1; r.Height -= 1; int rad = 12; GraphicsPath gp = GetRoundedPath(r, rad); e.Graphics.DrawPath(p, gp); } }; pnlChatBox.Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, pnlChatBox.Width, pnlChatBox.Height, 12, 12)); Panel pnlHeader = new Panel { Dock = DockStyle.Top, Height = 45, BackColor = Color.FromArgb(255, 110, 64) }; Label lblChatTitle = new Label { Text = "Gemini AI Assistant", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, ForeColor = Color.White, Font = new Font("Segoe UI", 11, FontStyle.Bold), Padding = new Padding(15, 0, 0, 0) }; Button btnCloseChat = new Button { Text = "✕", Dock = DockStyle.Right, Width = 40, FlatStyle = FlatStyle.Flat, ForeColor = Color.White, Font = new Font("Arial", 10), Cursor = Cursors.Hand }; btnCloseChat.FlatAppearance.BorderSize = 0; btnCloseChat.Click += (s, e) => ToggleChatWindow(); pnlHeader.Controls.Add(lblChatTitle); pnlHeader.Controls.Add(btnCloseChat); rtbChatHistory = new RichTextBox { Dock = DockStyle.Fill, BackColor = Color.FromArgb(250, 250, 250), BorderStyle = BorderStyle.None, ReadOnly = true, Font = new Font("Segoe UI", 10), Padding = new Padding(15, 10, 15, 10), ScrollBars = RichTextBoxScrollBars.Vertical }; Panel pnlInputContainer = new Panel { Dock = DockStyle.Bottom, Height = 60, BackColor = Color.White, Padding = new Padding(10) }; Panel pnlInputBg = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(240, 240, 240), Padding = new Padding(10, 8, 5, 5) }; pnlInputBg.Paint += (s, e) => { e.Graphics.SmoothingMode = SmoothingMode.AntiAlias; Rectangle r = pnlInputBg.ClientRectangle; r.Width--; r.Height--; using (GraphicsPath gp = GetRoundedPath(r, 15)) using (Pen p = new Pen(Color.FromArgb(220, 220, 220))) { e.Graphics.DrawPath(p, gp); } }; pnlInputBg.Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, 360, 40, 15, 15)); txtChatInput = new TextBox { Dock = DockStyle.Fill, BorderStyle = BorderStyle.None, BackColor = Color.FromArgb(240, 240, 240), Font = new Font("Segoe UI", 11), Height = 24 }; txtChatInput.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) PerformChat(); }; btnSendChat = new Button { Text = "➤", Dock = DockStyle.Right, Width = 40, FlatStyle = FlatStyle.Flat, BackColor = Color.Transparent, ForeColor = Color.FromArgb(255, 110, 64), Cursor = Cursors.Hand, Font = new Font("Segoe UI Symbol", 12) }; btnSendChat.FlatAppearance.BorderSize = 0; btnSendChat.Click += (s, e) => PerformChat(); pnlInputBg.Controls.Add(txtChatInput); pnlInputBg.Controls.Add(btnSendChat); pnlInputContainer.Controls.Add(pnlInputBg); pnlChatBox.Controls.Add(rtbChatHistory); pnlChatBox.Controls.Add(pnlInputContainer); pnlChatBox.Controls.Add(pnlHeader); this.Controls.Add(pnlChatBox); this.Controls.Add(btnGeminiFloat); UpdateChatUIPosition(); }
        private void UpdateChatUIPosition() { if (btnGeminiFloat == null) return; int margin = 25; btnGeminiFloat.Location = new Point(this.ClientSize.Width - btnGeminiFloat.Width - margin, this.ClientSize.Height - btnGeminiFloat.Height - margin - 20); pnlChatBox.Location = new Point(this.ClientSize.Width - pnlChatBox.Width - margin, btnGeminiFloat.Top - pnlChatBox.Height - 15); btnGeminiFloat.BringToFront(); pnlChatBox.BringToFront(); }
        private void ToggleChatWindow() { isChatOpen = !isChatOpen; pnlChatBox.Visible = isChatOpen; if (isChatOpen) { txtChatInput.Focus(); if (contentBox.SelectionLength > 0) { AppendChatMessage("HỆ THỐNG", "Đã chọn văn bản. Hãy hỏi gì đó...", Color.Gray, true); } } }
        private async void PerformChat() { string question = txtChatInput.Text.Trim(); if (string.IsNullOrEmpty(question)) return; AppendChatMessage("BẠN", question, Color.Black, false); txtChatInput.Clear(); string context = ""; if (contentBox.SelectionLength > 0) { context = contentBox.SelectedText; } else { string fullText = contentBox.Text; context = fullText.Length > 5000 ? fullText.Substring(0, 5000) + "..." : fullText; } Label lblTyping = new Label { Text = "Gemini đang suy nghĩ...", ForeColor = Color.Gray, Font = new Font("Segoe UI", 9, FontStyle.Italic), AutoSize = true, BackColor = rtbChatHistory.BackColor }; pnlChatBox.Controls.Add(lblTyping); lblTyping.Location = new Point(20, pnlChatBox.Height - 75); lblTyping.BringToFront(); try { string answer = await _geminiService.AskGemini(context, question); pnlChatBox.Controls.Remove(lblTyping); AppendChatMessage("GEMINI", answer, Color.FromArgb(0, 100, 0), true); } catch (Exception ex) { pnlChatBox.Controls.Remove(lblTyping); AppendChatMessage("LỖI", ex.Message, Color.Red, true); } }
        private void AppendChatMessage(string sender, string message, Color color, bool isLeft) { rtbChatHistory.SelectionStart = rtbChatHistory.TextLength; rtbChatHistory.SelectionLength = 0; rtbChatHistory.SelectionAlignment = isLeft ? HorizontalAlignment.Left : HorizontalAlignment.Right; rtbChatHistory.SelectionColor = Color.Gray; rtbChatHistory.SelectionFont = new Font("Segoe UI", 8, FontStyle.Regular); rtbChatHistory.AppendText(sender + "\n"); rtbChatHistory.SelectionStart = rtbChatHistory.TextLength; rtbChatHistory.SelectionLength = 0; rtbChatHistory.SelectionIndent = isLeft ? 0 : 50; rtbChatHistory.SelectionRightIndent = isLeft ? 50 : 0; rtbChatHistory.SelectionAlignment = isLeft ? HorizontalAlignment.Left : HorizontalAlignment.Right; rtbChatHistory.SelectionColor = color; rtbChatHistory.SelectionFont = new Font("Segoe UI", 10, FontStyle.Regular); if (!isLeft) rtbChatHistory.SelectionColor = Color.FromArgb(0, 120, 215); rtbChatHistory.AppendText(message + "\n\n"); rtbChatHistory.ScrollToCaret(); }
        private void SetupModernUI() { this.Padding = new Padding(0); this.BackColor = Color.FromArgb(240, 240, 240); topBar = new Panel { Dock = DockStyle.Top, Height = 50, Padding = new Padding(10, 0, 10, 0), BackColor = Color.White }; InitializeWindowControls(); topBar.Controls.Add(pnlWindowControls); var btnSettings = CreateIconButton("⚙", 45, (s, e) => ToggleSettings()); btnSettings.Dock = DockStyle.Right; topBar.Controls.Add(btnSettings); var btnBack = CreateIconButton("🡠", 50, (s, e) => this.Close()); btnBack.Dock = DockStyle.Left; topBar.Controls.Add(btnBack); lblTitle = new Label { Text = _book.Title.ToUpper(), TextAlign = ContentAlignment.MiddleCenter, Dock = DockStyle.Fill, Font = new Font("Segoe UI", 11, FontStyle.Bold), AutoEllipsis = true, ForeColor = Color.FromArgb(64, 64, 64) }; lblTitle.MouseDown += (s, e) => { if (e.Button == MouseButtons.Left) { ReleaseCapture(); SendMessage(Handle, 0xA1, 0x2, 0); } }; topBar.Controls.Add(lblTitle); mainContainer = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0) }; pagePanel = new Panel { Anchor = AnchorStyles.Top | AnchorStyles.Bottom, BackColor = Color.White, Padding = new Padding(60, 40, 30, 40) }; pagePanel.Paint += PagePanel_Paint; contentBox = new TransparentRichTextBox { Dock = DockStyle.Fill, BorderStyle = BorderStyle.None, ReadOnly = true, ScrollBars = RichTextBoxScrollBars.None, Cursor = Cursors.IBeam, BackColor = Color.White, Font = new Font(currentFontFamily, currentFontSize) }; contentBox.MouseUp += ContentBox_MouseUp; contentBox.GotFocus += (s, e) => HideCaret(contentBox.Handle); contentBox.Click += (s, e) => HideCaret(contentBox.Handle); pageScrollBar = new ModernScrollBar { Dock = DockStyle.Right, Width = 12, Visible = true, ThumbColor = Color.FromArgb(200, 200, 200) }; pageScrollBar.Scroll += (s, e) => contentBox.SetVerticalScroll(pageScrollBar.Value); contentBox.CustomMouseWheel += (s, e) => UpdatePageScrollbar(); pagePanel.Controls.Add(contentBox); pagePanel.Controls.Add(pageScrollBar); mainContainer.Controls.Add(pagePanel); btnPrevFloat = CreateFloatingNavButton("❮", true); btnNextFloat = CreateFloatingNavButton("❯", false); progressBar = new Panel { Dock = DockStyle.Bottom, Height = 4, BackColor = Color.FromArgb(230, 230, 230) }; progressFill = new Panel { Dock = DockStyle.Left, Width = 0, BackColor = Color.FromArgb(255, 110, 64) }; progressBar.Controls.Add(progressFill); InitializeSettingsPanel(); InitializeTOCPanel(); InitializeSideToggleButton(); this.Controls.Add(pnlSettings); this.Controls.Add(btnSideToggle); this.Controls.Add(pnlTOC); mainContainer.Controls.Add(btnPrevFloat); mainContainer.Controls.Add(btnNextFloat); this.Controls.Add(mainContainer); this.Controls.Add(progressBar); this.Controls.Add(topBar); ApplyTheme(Theme.Light); }
        private void PagePanel_Paint(object sender, PaintEventArgs e) { e.Graphics.SmoothingMode = SmoothingMode.AntiAlias; Rectangle r = pagePanel.ClientRectangle; r.Width -= 1; r.Height -= 1; using (GraphicsPath path = GetRoundedPath(r, 10)) { pagePanel.Region = new Region(path); using (Pen p = new Pen(Color.FromArgb(20, 0, 0, 0), 1)) { e.Graphics.DrawPath(p, path); } } }
        private GraphicsPath GetRoundedPath(Rectangle rect, int radius) { GraphicsPath path = new GraphicsPath(); int d = radius * 2; path.AddArc(rect.X, rect.Y, d, d, 180, 90); path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90); path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90); path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90); path.CloseFigure(); return path; }
        private void UpdatePageScrollbar() { int min = 0; int max = contentBox.GetVerticalScrollRange(); int pos = contentBox.GetVerticalScrollPosition(); int largeChange = contentBox.ClientSize.Height; if (max <= largeChange) pageScrollBar.Visible = false; else { pageScrollBar.Visible = true; pageScrollBar.SetScrollValues(pos, min, max, largeChange); } }
        private Button CreateFloatingNavButton(string text, bool isLeft) { Button btn = new Button { Text = text, Size = new Size(60, 100), FlatStyle = FlatStyle.Flat, BackColor = Color.Transparent, ForeColor = Color.FromArgb(150, 150, 150), Font = new Font("Segoe UI Light", 24, FontStyle.Regular), Cursor = Cursors.Hand, Anchor = isLeft ? (AnchorStyles.Left | AnchorStyles.Left) : (AnchorStyles.Right | AnchorStyles.Right) }; btn.FlatAppearance.BorderSize = 0; btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(10, 0, 0, 0); btn.FlatAppearance.MouseDownBackColor = Color.FromArgb(20, 0, 0, 0); btn.Click += (s, e) => NavigateChapter(isLeft ? -1 : 1); btn.MouseEnter += (s, e) => btn.ForeColor = Color.FromArgb(80, 80, 80); btn.MouseLeave += (s, e) => btn.ForeColor = Color.FromArgb(150, 150, 150); return btn; }
        private void ApplyTheme(Theme theme) { currentTheme = theme; Color uiBg, paperBg, fg, accent, scrollThumb; switch (theme) { case Theme.Sepia: uiBg = Color.FromArgb(244, 236, 216); paperBg = Color.FromArgb(253, 248, 235); fg = Color.FromArgb(91, 70, 54); accent = Color.FromArgb(215, 205, 185); scrollThumb = Color.FromArgb(210, 195, 175); this.BackColor = Color.FromArgb(235, 225, 205); break; case Theme.Dark: uiBg = Color.FromArgb(32, 32, 32); paperBg = Color.FromArgb(45, 45, 45); fg = Color.FromArgb(220, 220, 220); accent = Color.FromArgb(60, 60, 60); scrollThumb = Color.FromArgb(80, 80, 80); this.BackColor = Color.FromArgb(25, 25, 25); break; case Theme.Light: default: uiBg = Color.White; paperBg = Color.White; fg = Color.FromArgb(33, 33, 33); accent = Color.FromArgb(240, 240, 240); scrollThumb = Color.FromArgb(200, 200, 200); this.BackColor = Color.FromArgb(242, 242, 242); break; } topBar.BackColor = uiBg; topBar.ForeColor = fg; lblTitle.ForeColor = fg; pagePanel.BackColor = paperBg; contentBox.BackColor = paperBg; contentBox.ForeColor = fg; pageScrollBar.ThumbColor = scrollThumb; pageScrollBar.BackColor = paperBg; if (pnlTOC != null) { pnlTOC.BackColor = uiBg; lstChapters.BackColor = uiBg; lstChapters.ForeColor = fg; } foreach (Control c in topBar.Controls) if (c is Button b) b.ForeColor = fg; if (pnlSettings != null) { pnlSettings.BackColor = paperBg; foreach (Control c in pnlSettings.Controls[0].Controls) if (c is FlowLayoutPanel fp) foreach (Control b in fp.Controls) if (b is Button btn) { btn.BackColor = accent; btn.ForeColor = fg; } } UpdateThemeColors(); if (_chapters != null && _chapters.Count > 0) DisplayFullChapter(_currentChapterIndex, contentBox.SelectionStart); }
        private void InitializeTOCPanel() { pnlTOC = new Panel { Width = 300, Dock = DockStyle.Right, Visible = false, BorderStyle = BorderStyle.FixedSingle, Padding = new Padding(1) }; var lblHeader = new Label { Text = "MỤC LỤC", Dock = DockStyle.Top, Height = 50, TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 11, FontStyle.Bold), ForeColor = Color.Black, BackColor = Color.Transparent }; Panel listContainer = new Panel { Dock = DockStyle.Fill }; lstChapters = new ListBox { Dock = DockStyle.Fill, BorderStyle = BorderStyle.None, Font = new Font("Segoe UI", 11), ItemHeight = 36, DrawMode = DrawMode.OwnerDrawFixed }; lstChapters.DrawItem += LstChapters_DrawItem; lstChapters.SelectedIndexChanged += (s, e) => { if (lstChapters.SelectedIndex >= 0) { SaveCurrentProgress(); _currentChapterIndex = lstChapters.SelectedIndex; DisplayFullChapter(_currentChapterIndex); ToggleTOC(); } }; tocScrollBar = new ModernScrollBar { Dock = DockStyle.Right, Width = 8 }; tocScrollBar.Scroll += (s, e) => { if (lstChapters.Items.Count > 0) { int index = (int)((float)tocScrollBar.Value / tocScrollBar.Maximum * lstChapters.Items.Count); lstChapters.TopIndex = Math.Min(index, lstChapters.Items.Count - 1); } }; listContainer.Controls.Add(lstChapters); listContainer.Controls.Add(tocScrollBar); pnlTOC.Controls.AddRange(new Control[] { listContainer, lblHeader }); }
        private void InitializeSettingsPanel() { pnlSettings = new Panel { Size = new Size(260, 200), Visible = false, BorderStyle = BorderStyle.FixedSingle }; var flow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, Padding = new Padding(15) }; var lblSize = new Label { Text = "Cỡ chữ", AutoSize = true, ForeColor = Color.Gray, Font = new Font("Segoe UI", 10) }; var pnlSize = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight, Margin = new Padding(0, 5, 0, 15) }; pnlSize.Controls.Add(CreateSmallButton("A-", (s, e) => ChangeFontSize(-2))); pnlSize.Controls.Add(CreateSmallButton("A+", (s, e) => ChangeFontSize(2))); var lblTheme = new Label { Text = "Giao diện", AutoSize = true, ForeColor = Color.Gray, Font = new Font("Segoe UI", 10) }; var pnlTheme = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight, Margin = new Padding(0, 5, 0, 0) }; pnlTheme.Controls.Add(CreateSmallButton("Dark", (s, e) => ApplyTheme(Theme.Dark))); pnlTheme.Controls.Add(CreateSmallButton("Light", (s, e) => ApplyTheme(Theme.Light))); pnlTheme.Controls.Add(CreateSmallButton("Sepia", (s, e) => ApplyTheme(Theme.Sepia))); flow.Controls.AddRange(new Control[] { lblSize, pnlSize, lblTheme, pnlTheme }); pnlSettings.Controls.Add(flow); }
        private Button CreateIconButton(string text, int width, EventHandler onClick) { var btn = new Button { Text = text, Width = width, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI Symbol", 14, FontStyle.Regular), Cursor = Cursors.Hand, Dock = DockStyle.Left, BackColor = Color.Transparent }; btn.FlatAppearance.BorderSize = 0; btn.Click += onClick; return btn; }
        private Button CreateSmallButton(string text, EventHandler onClick) { var btn = new Button { Text = text, Size = new Size(70, 35), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Margin = new Padding(0, 0, 5, 0), Font = new Font("Segoe UI", 9) }; btn.Click += onClick; return btn; }
        private void LstChapters_DrawItem(object sender, DrawItemEventArgs e) { if (e.Index < 0) return; e.DrawBackground(); bool isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected; if (isSelected) using (Brush b = new SolidBrush(Color.FromArgb(20, 0, 0, 0))) e.Graphics.FillRectangle(b, e.Bounds); Color textColor = isSelected ? Color.FromArgb(255, 110, 64) : (currentTheme == Theme.Dark ? Color.White : Color.Black); TextRenderer.DrawText(e.Graphics, lstChapters.Items[e.Index].ToString(), e.Font, e.Bounds, textColor, TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.LeftAndRightPadding); }
        private void ToggleSettings() { pnlSettings.Location = new Point(topBar.Width - pnlSettings.Width - 15, topBar.Height + 5); pnlSettings.Visible = !pnlSettings.Visible; pnlSettings.BringToFront(); }
        private void ToggleTOC() { pnlTOC.Visible = !pnlTOC.Visible; pnlTOC.BringToFront(); UpdateSideTogglePosition(); }
        private void UpdateButtonStyle(Panel p, Color fg) { foreach (Control c in p.Controls) if (c is Button b) { b.ForeColor = fg; b.FlatAppearance.MouseOverBackColor = Color.FromArgb(20, 128, 128, 128); } }
        private void ChangeFontSize(float delta) { currentFontSize = Math.Max(12, Math.Min(36, currentFontSize + delta)); DisplayFullChapter(_currentChapterIndex, contentBox.SelectionStart); }
        private void UpdateFloatingButtonsPosition() { if (btnPrevFloat == null || btnNextFloat == null) return; int y = (mainContainer.Height - 100) / 2; btnPrevFloat.Location = new Point(10, y); btnNextFloat.Location = new Point(mainContainer.Width - 70, y); }
        private void InitializeWindowControls() { pnlWindowControls = new Panel { Dock = DockStyle.Right, Width = 140, BackColor = Color.Transparent }; btnCloseWin = CreateStyledWindowButton("✕", 45, (s, e) => this.Close()); btnCloseWin.MouseEnter += (s, e) => { btnCloseWin.BackColor = Color.FromArgb(232, 17, 35); btnCloseWin.ForeColor = Color.White; }; btnCloseWin.MouseLeave += (s, e) => { btnCloseWin.BackColor = Color.Transparent; UpdateThemeColors(); }; btnMax = CreateStyledWindowButton("◻", 45, (s, e) => ToggleMaximize()); btnMin = CreateStyledWindowButton("─", 45, (s, e) => this.WindowState = FormWindowState.Minimized); pnlWindowControls.Controls.Clear(); pnlWindowControls.Controls.Add(btnCloseWin); pnlWindowControls.Controls.Add(btnMax); pnlWindowControls.Controls.Add(btnMin); }
        private Button CreateStyledWindowButton(string text, int width, EventHandler onClick) { var btn = new Button { Text = text, Dock = DockStyle.Right, Width = width, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10), Cursor = Cursors.Default, BackColor = Color.Transparent }; btn.FlatAppearance.BorderSize = 0; btn.Click += onClick; return btn; }
        private void ToggleMaximize() { if (this.WindowState == FormWindowState.Maximized) { this.WindowState = FormWindowState.Normal; btnMax.Text = "◻"; } else { this.WindowState = FormWindowState.Maximized; btnMax.Text = "❐"; } }
        private void UpdateThemeColors() { Color fg = (currentTheme == Theme.Light || currentTheme == Theme.Sepia) ? Color.Black : Color.White; if (btnMin != null) btnMin.ForeColor = fg; if (btnMax != null) btnMax.ForeColor = fg; if (btnCloseWin != null && btnCloseWin.BackColor != Color.Red) btnCloseWin.ForeColor = fg; }
        private void InitializeSideToggleButton() { btnSideToggle = new Button { Text = "‹", Size = new Size(24, 60), BackColor = Color.FromArgb(230, 230, 230), ForeColor = Color.DimGray, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Font = new Font("Segoe UI", 12, FontStyle.Bold), TextAlign = ContentAlignment.MiddleCenter, Anchor = AnchorStyles.Right | AnchorStyles.Top }; btnSideToggle.FlatAppearance.BorderSize = 0; GraphicsPath path = new GraphicsPath(); int r = 5; path.AddArc(0, 0, r, r, 180, 90); path.AddLine(r, 0, btnSideToggle.Width, 0); path.AddLine(btnSideToggle.Width, btnSideToggle.Height, r, btnSideToggle.Height); path.AddArc(0, btnSideToggle.Height - r, r, r, 90, 90); path.CloseFigure(); btnSideToggle.Region = new Region(path); btnSideToggle.Click += (s, e) => ToggleTOC(); }
        private void UpdateSideTogglePosition() { int y = (this.ClientSize.Height - btnSideToggle.Height) / 2; int x = this.ClientSize.Width - btnSideToggle.Width; if (pnlTOC.Visible) { x -= pnlTOC.Width; btnSideToggle.Text = "›"; } else { btnSideToggle.Text = "‹"; } btnSideToggle.Location = new Point(x, y); btnSideToggle.BringToFront(); }
        private void InitializeFloatingMenu() { _floatingMenu = new ContextMenuStrip(); _floatingMenu.Renderer = new ToolStripProfessionalRenderer(new ModernColorTable()); var colors = new[] { (Name: "Vàng", Color: Color.FromArgb(255, 235, 59)), (Name: "Xanh lá", Color: Color.FromArgb(178, 255, 89)), (Name: "Hồng", Color: Color.FromArgb(255, 128, 171)), (Name: "Tím", Color: Color.FromArgb(209, 196, 233)) }; foreach (var c in colors) { var item = new ToolStripMenuItem(c.Name); Bitmap bmp = new Bitmap(16, 16); using (Graphics g = Graphics.FromImage(bmp)) { g.Clear(c.Color); g.DrawRectangle(Pens.Gray, 0, 0, 15, 15); } item.Image = bmp; item.Click += (s, e) => CreateHighlight(c.Color); _floatingMenu.Items.Add(item); } _floatingMenu.Items.Add(new ToolStripSeparator()); var btnNote = new ToolStripMenuItem("📝 Thêm Ghi Chú"); btnNote.Click += (s, e) => HandleNoteInput(); _floatingMenu.Items.Add(btnNote); }
        private void ContentBox_MouseUp(object sender, MouseEventArgs e) { if (e.Button == MouseButtons.Left && contentBox.SelectionLength > 0) _floatingMenu.Show(Cursor.Position); else { if (pnlSettings.Visible && !pnlSettings.Bounds.Contains(PointToClient(Cursor.Position))) pnlSettings.Visible = false; } }
        private void CreateHighlight(Color color, string noteContent = "") { if (contentBox.SelectionLength == 0) return; try { var hl = new Highlight { BookId = _book.Id, UserId = DataManager.Instance.GetCurrentUser(), ChapterIndex = _currentChapterIndex, StartIndex = contentBox.SelectionStart, Length = contentBox.SelectionLength, SelectedText = contentBox.SelectedText, Note = noteContent, ColorHex = ColorTranslator.ToHtml(color) }; DataManager.Instance.AddHighlight(hl); contentBox.SelectionBackColor = color; contentBox.SelectionLength = 0; } catch (Exception ex) { MessageBox.Show("Lỗi: " + ex.Message); } }
        private void HandleNoteInput() { if (contentBox.SelectionLength == 0) return; using (var dlg = new NoteDialog()) { if (dlg.ShowDialog() == DialogResult.OK) CreateHighlight(Color.Orange, dlg.NoteText); } }
        private void BookReaderForm_Load(object sender, EventArgs e) { BookReaderForm_Resize(this, null); LoadBookDataInitial(); UpdateSideTogglePosition(); UpdateChatUIPosition(); }
        private void BookReaderForm_Resize(object sender, EventArgs e) { if (mainContainer == null || pagePanel == null) return; int maxWidth = 900; int w = Math.Min(maxWidth, mainContainer.Width - 60); pagePanel.Size = new Size(w, mainContainer.Height - 30); pagePanel.Location = new Point((mainContainer.Width - w) / 2, 15); pagePanel.Invalidate(); if (pnlTOC != null) pnlTOC.Height = this.Height - topBar.Height; UpdateSideTogglePosition(); UpdateFloatingButtonsPosition(); UpdateProgressBar(); UpdatePageScrollbar(); UpdateChatUIPosition(); }
        private void LoadBookDataInitial() { BackgroundWorker worker = new BackgroundWorker(); worker.DoWork += (s, args) => { try { _chapters = _readerService.ReadBookContent(_book) ?? new List<BookChapter>(); } catch { _chapters = new List<BookChapter>(); } }; worker.RunWorkerCompleted += (s, args) => { if (_chapters.Count == 0) _chapters.Add(new BookChapter { ChapterTitle = "Lỗi", Content = "Không thể tải nội dung." }); lstChapters.Items.Clear(); foreach (var c in _chapters) lstChapters.Items.Add(c.ChapterTitle); if (_targetChapter.HasValue && _targetPosition.HasValue) { _currentChapterIndex = Math.Min(Math.Max(0, _targetChapter.Value), _chapters.Count - 1); DisplayFullChapter(_currentChapterIndex, _targetPosition.Value); } else { var pos = _readerService.GetReadingPosition(_book.Id, DataManager.Instance.GetCurrentUser()); _currentChapterIndex = Math.Min(Math.Max(0, pos.chapter), _chapters.Count - 1); DisplayFullChapter(_currentChapterIndex, pos.position); } }; worker.RunWorkerAsync(); }
        private void UpdateProgressBar() { if (_chapters == null || _chapters.Count == 0 || progressFill == null) return; double progress = (double)(_currentChapterIndex + 1) / _chapters.Count; progressFill.Width = (int)(progressBar.Width * progress); }
        private void ReloadHighlights(int chapterIndex) { var highlights = DataManager.Instance.GetHighlightsForBook(_book.Id); int originalStart = contentBox.SelectionStart; foreach (var hl in highlights) { if (hl.ChapterIndex == chapterIndex) { if (hl.StartIndex >= 0 && (hl.StartIndex + hl.Length) <= contentBox.TextLength) { contentBox.Select(hl.StartIndex, hl.Length); try { contentBox.SelectionBackColor = ColorTranslator.FromHtml(hl.ColorHex); } catch { contentBox.SelectionBackColor = Color.Yellow; } } } } contentBox.Select(originalStart, 0); }
        private void NavigateChapter(int step) { int newIndex = _currentChapterIndex + step; if (_chapters != null && newIndex >= 0 && newIndex < _chapters.Count) { SaveCurrentProgress(); _currentChapterIndex = newIndex; DisplayFullChapter(_currentChapterIndex); } }
        private void SaveCurrentProgress() { if (_chapters == null || _chapters.Count == 0) return; int currentPos = contentBox.GetCharIndexFromPosition(new Point(10, 10)); _readerService.SaveReadingPosition(_book.Id, DataManager.Instance.GetCurrentUser(), _currentChapterIndex, currentPos); }
        private void BookReaderForm_FormClosing(object sender, FormClosingEventArgs e) { SaveCurrentProgress(); }
        private void BookReaderForm_KeyDown(object sender, KeyEventArgs e) { if (e.KeyCode == Keys.Left) NavigateChapter(-1); else if (e.KeyCode == Keys.Right) NavigateChapter(1); else if (e.KeyCode == Keys.Escape) this.Close(); }
    }

    public class ModernColorTable : ProfessionalColorTable { public override Color MenuItemSelected => Color.FromArgb(230, 230, 230); public override Color MenuItemBorder => Color.Transparent; public override Color MenuBorder => Color.LightGray; }
    public class ModernScrollBar : Control { public int Value { get; private set; } = 0; public int Maximum { get; private set; } = 100; public int Minimum { get; private set; } = 0; public int LargeChange { get; private set; } = 10; public Color ThumbColor { get; set; } = Color.Silver; public Color ThumbHoverColor { get; set; } = Color.Gray; public event EventHandler Scroll; private bool isDragging = false; private bool isHovered = false; private int clickY, thumbY; public ModernScrollBar() { SetStyle(ControlStyles.SupportsTransparentBackColor, true); this.DoubleBuffered = true; this.Width = 12; this.BackColor = Color.Transparent; } public void SetScrollValues(int val, int min, int max, int largeChange) { this.Value = val; this.Minimum = min; this.Maximum = max; this.LargeChange = largeChange; this.Invalidate(); } protected override void OnMouseEnter(EventArgs e) { isHovered = true; Invalidate(); } protected override void OnMouseLeave(EventArgs e) { isHovered = false; Invalidate(); } protected override void OnPaint(PaintEventArgs e) { base.OnPaint(e); if (Maximum <= 0) return; int thumbHeight = Math.Max(40, (int)((float)LargeChange / Maximum * Height)); int trackHeight = Height - thumbHeight; int thumbPos = (int)((float)Value / (Maximum - LargeChange + 1) * trackHeight); thumbY = Math.Max(0, Math.Min(trackHeight, thumbPos)); Color c = (isDragging || isHovered) ? ThumbHoverColor : ThumbColor; using (SolidBrush brush = new SolidBrush(c)) { e.Graphics.SmoothingMode = SmoothingMode.AntiAlias; Rectangle thumbRect = new Rectangle(2, thumbY, Width - 4, thumbHeight); GraphicsPath path = new GraphicsPath(); int r = thumbRect.Width; path.AddArc(thumbRect.X, thumbRect.Y, r, r, 180, 180); path.AddArc(thumbRect.X, thumbRect.Bottom - r, r, r, 0, 180); path.CloseFigure(); e.Graphics.FillPath(brush, path); } } protected override void OnMouseDown(MouseEventArgs e) { int thumbHeight = Math.Max(40, (int)((float)LargeChange / Maximum * Height)); if (e.Y >= thumbY && e.Y <= thumbY + thumbHeight) { isDragging = true; clickY = e.Y - thumbY; } else { float ratio = (float)e.Y / (Height - thumbHeight); Value = (int)(ratio * (Maximum - LargeChange)); Value = Math.Max(Minimum, Math.Min(Maximum - LargeChange, Value)); Scroll?.Invoke(this, EventArgs.Empty); Invalidate(); } } protected override void OnMouseMove(MouseEventArgs e) { if (isDragging) { int thumbHeight = Math.Max(40, (int)((float)LargeChange / Maximum * Height)); int trackHeight = Height - thumbHeight; int newThumbY = e.Y - clickY; newThumbY = Math.Max(0, Math.Min(trackHeight, newThumbY)); float ratio = (float)newThumbY / trackHeight; Value = (int)(ratio * (Maximum - LargeChange)); Value = Math.Max(Minimum, Math.Min(Maximum - LargeChange, Value)); Scroll?.Invoke(this, EventArgs.Empty); Invalidate(); } } protected override void OnMouseUp(MouseEventArgs e) { isDragging = false; Invalidate(); } }
    public class TransparentRichTextBox : RichTextBox { public event EventHandler CustomMouseWheel; [DllImport("user32.dll")] static extern int GetScrollPos(IntPtr hWnd, int nBar); [DllImport("user32.dll")] static extern int SetScrollPos(IntPtr hWnd, int nBar, int nPos, bool bRedraw); [DllImport("user32.dll")] static extern bool GetScrollRange(IntPtr hWnd, int nBar, out int lpMinPos, out int lpMaxPos); [DllImport("user32.dll")] static extern int SendMessage(IntPtr hWnd, int wMsg, int wParam, int lParam); private const int SB_VERT = 1; private const int WM_VSCROLL = 0x0115; private const int WM_MOUSEWHEEL = 0x020A; private const int SB_LINEUP = 0; private const int SB_LINEDOWN = 1; protected override void WndProc(ref Message m) { if (m.Msg == WM_MOUSEWHEEL) { short delta = (short)((m.WParam.ToInt64() >> 16) & 0xFFFF); int direction = (delta > 0) ? SB_LINEUP : SB_LINEDOWN; for (int i = 0; i < 3; i++) SendMessage(this.Handle, WM_VSCROLL, direction, 0); CustomMouseWheel?.Invoke(this, EventArgs.Empty); return; } base.WndProc(ref m); } public int GetVerticalScrollPosition() { return GetScrollPos(this.Handle, SB_VERT); } public int GetVerticalScrollRange() { int min, max; GetScrollRange(this.Handle, SB_VERT, out min, out max); return max; } public void SetVerticalScroll(int value) { SetScrollPos(this.Handle, SB_VERT, value, true); SendMessage(this.Handle, WM_VSCROLL, 4 + 0x10000 * value, 0); } }
}