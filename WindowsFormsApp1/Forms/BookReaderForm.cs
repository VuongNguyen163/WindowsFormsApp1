using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using WindowsFormsApp1.Data;

namespace WindowsFormsApp1.Forms
{
    public partial class BookReaderForm : Form
    {
        // --- CORE VARIABLES ---
        private readonly Book _book;
        private readonly BookReaderService _readerService;
        private List<BookChapter> _chapters;
        private int _currentChapterIndex = 0;

        // Navigation targets
        private int? _targetChapter = null;
        private int? _targetPosition = null;

        // Floating Menu
        private ContextMenuStrip _floatingMenu;

        // --- UI CONTROLS ---
        private RichTextBox contentBox;
        private Panel mainContainer;
        private Panel pagePanel;
        private Panel topBar;
        private Panel bottomBar;
        
        // Panels for features
        private Panel pnlSettings;
        private Panel pnlTOC;
        private ListBox lstChapters;

        // Labels & Buttons
        private Label lblTitle;
        private Label lblChapterInfo;
        private Button btnPrev;
        private Button btnNext;

        // Settings
        private float currentFontSize = 14f;
        private string currentFontFamily = "Palatino Linotype";
        private Theme currentTheme = Theme.Dark;

        private enum Theme { Light, Dark, Sepia }

        // --- CONSTRUCTOR ---
        public BookReaderForm(Book book, int? jumpChapter = null, int? jumpPos = null)
        {
            // Form Configuration
            this.DoubleBuffered = true;
            this.AutoScaleMode = AutoScaleMode.None; // Better for custom drawing
            this.ClientSize = new Size(1200, 800);
            this.FormBorderStyle = FormBorderStyle.None; // Full immersive
            this.WindowState = FormWindowState.Maximized;

            _book = book;
            _targetChapter = jumpChapter;
            _targetPosition = jumpPos;

            _readerService = new BookReaderService(DataManager.Instance);

            // 1. Setup UI
            SetupImmersiveUI();

            // 2. Setup Floating Menu
            InitializeFloatingMenu();

            // 3. Events
            this.Load += BookReaderForm_Load;
            this.FormClosing += BookReaderForm_FormClosing;
            this.Resize += BookReaderForm_Resize;
            
            // Key shortcuts
            this.KeyPreview = true;
            this.KeyDown += BookReaderForm_KeyDown;
        }

        // --- UI SETUP ---
        private void SetupImmersiveUI()
        {
            // --- TOP BAR ---
            topBar = new Panel { Dock = DockStyle.Top, Height = 50, Padding = new Padding(10) };
            
            var btnBack = CreateIconButton("←", 40, (s, e) => this.Close());
            btnBack.Dock = DockStyle.Left;

            var btnTOC = CreateIconButton("≡", 40, (s, e) => ToggleTOC());
            btnTOC.Dock = DockStyle.Right;

            var btnSettings = CreateIconButton("Aa", 40, (s, e) => ToggleSettings());
            btnSettings.Dock = DockStyle.Right;

            lblTitle = new Label
            {
                Text = _book.Title,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                AutoEllipsis = true
            };

            topBar.Controls.Add(lblTitle);
            topBar.Controls.Add(btnSettings);
            topBar.Controls.Add(btnTOC);
            topBar.Controls.Add(btnBack);

            // --- BOTTOM BAR ---
            bottomBar = new Panel { Dock = DockStyle.Bottom, Height = 50, Padding = new Padding(10) };

            btnPrev = CreateIconButton("❮", 50, (s, e) => NavigateChapter(-1));
            btnPrev.Dock = DockStyle.Left;

            btnNext = CreateIconButton("❯", 50, (s, e) => NavigateChapter(1));
            btnNext.Dock = DockStyle.Right;

            lblChapterInfo = new Label
            {
                Text = "Loading...",
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10)
            };

            bottomBar.Controls.Add(lblChapterInfo);
            bottomBar.Controls.Add(btnNext);
            bottomBar.Controls.Add(btnPrev);

            // --- MAIN AREA ---
            mainContainer = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0) };
            
            pagePanel = new Panel
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom,
                Padding = new Padding(40, 20, 40, 20) // Page margins
            };

            contentBox = new RichTextBox
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.None,
                ReadOnly = true,
                ScrollBars = RichTextBoxScrollBars.None,
                HideSelection = false,
                ShortcutsEnabled = true,
                Cursor = Cursors.IBeam
            };
            contentBox.ScrollBars = RichTextBoxScrollBars.Vertical;
            contentBox.MouseUp += ContentBox_MouseUp;

            pagePanel.Controls.Add(contentBox);
            mainContainer.Controls.Add(pagePanel);

            // --- PANELS (Overlays) ---
            InitializeSettingsPanel();
            InitializeTOCPanel();

            this.Controls.Add(pnlSettings); // Add on top
            this.Controls.Add(pnlTOC);      // Add on top
            this.Controls.Add(mainContainer);
            this.Controls.Add(bottomBar);
            this.Controls.Add(topBar);

            // Apply Theme LAST (after all controls are created)
            ApplyTheme(Theme.Dark);
        }

        private void InitializeSettingsPanel()
        {
            pnlSettings = new Panel
            {
                Size = new Size(250, 180),
                Visible = false,
                BorderStyle = BorderStyle.FixedSingle
            };

            var flow = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, Padding = new Padding(10) };
            
            // Font Size
            var lblSize = new Label { Text = "Cỡ chữ", AutoSize = true, ForeColor = Color.Gray };
            var pnlSize = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight };
            var btnDec = CreateSmallButton("-", (s, e) => ChangeFontSize(-2));
            var btnInc = CreateSmallButton("+", (s, e) => ChangeFontSize(2));
            pnlSize.Controls.AddRange(new Control[] { btnDec, btnInc });

            // Theme
            var lblTheme = new Label { Text = "Giao diện", AutoSize = true, ForeColor = Color.Gray, Margin = new Padding(0, 10, 0, 0) };
            var pnlTheme = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight };
            var btnDark = CreateSmallButton("Dark", (s, e) => ApplyTheme(Theme.Dark));
            var btnLight = CreateSmallButton("Light", (s, e) => ApplyTheme(Theme.Light));
            var btnSepia = CreateSmallButton("Sepia", (s, e) => ApplyTheme(Theme.Sepia));
            pnlTheme.Controls.AddRange(new Control[] { btnDark, btnLight, btnSepia });

            flow.Controls.AddRange(new Control[] { lblSize, pnlSize, lblTheme, pnlTheme });
            pnlSettings.Controls.Add(flow);
        }

        private void InitializeTOCPanel()
        {
            pnlTOC = new Panel
            {
                Width = 300,
                Dock = DockStyle.Right,
                Visible = false,
                BorderStyle = BorderStyle.FixedSingle
            };

            var lblHeader = new Label 
            { 
                Text = "MỤC LỤC", 
                Dock = DockStyle.Top, 
                Height = 40, 
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };

            lstChapters = new ListBox
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 10),
                ItemHeight = 30,
                DrawMode = DrawMode.OwnerDrawFixed
            };
            lstChapters.DrawItem += LstChapters_DrawItem;
            lstChapters.SelectedIndexChanged += (s, e) =>
            {
                if (lstChapters.SelectedIndex >= 0)
                {
                    SaveCurrentProgress();
                    _currentChapterIndex = lstChapters.SelectedIndex;
                    DisplayFullChapter(_currentChapterIndex);
                    ToggleTOC(); // Close after selection
                }
            };

            pnlTOC.Controls.Add(lstChapters);
            pnlTOC.Controls.Add(lblHeader);
        }

        private void LstChapters_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;
            e.DrawBackground();
            bool isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            
            // Custom selection color
            if (isSelected)
            {
                e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(0, 120, 215)), e.Bounds);
            }

            string text = lstChapters.Items[e.Index].ToString();
            TextRenderer.DrawText(e.Graphics, text, e.Font, e.Bounds, isSelected ? Color.White : lstChapters.ForeColor, TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
            e.DrawFocusRectangle();
        }

        // --- HELPER CONTROLS ---
        private Button CreateIconButton(string text, int width, EventHandler onClick)
        {
            var btn = new Button
            {
                Text = text,
                Width = width,
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0 },
                Font = new Font("Segoe UI Symbol", 14, FontStyle.Regular),
                Cursor = Cursors.Hand,
                Dock = DockStyle.Left
            };
            btn.Click += onClick;
            return btn;
        }

        private Button CreateSmallButton(string text, EventHandler onClick)
        {
            var btn = new Button
            {
                Text = text,
                Size = new Size(60, 30),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Margin = new Padding(3)
            };
            btn.Click += onClick;
            return btn;
        }

        // --- THEME & STYLING ---
        private void ApplyTheme(Theme theme)
        {
            currentTheme = theme;
            Color bg, fg, panelBg, accent;

            switch (theme)
            {
                case Theme.Light:
                    bg = Color.FromArgb(245, 245, 245);
                    fg = Color.FromArgb(20, 20, 20);
                    panelBg = Color.White;
                    accent = Color.FromArgb(230, 230, 230);
                    break;
                case Theme.Sepia:
                    bg = Color.FromArgb(244, 236, 216);
                    fg = Color.FromArgb(95, 75, 50);
                    panelBg = Color.FromArgb(250, 245, 230);
                    accent = Color.FromArgb(225, 215, 190);
                    break;
                case Theme.Dark:
                default:
                    bg = Color.FromArgb(18, 18, 18);
                    fg = Color.FromArgb(200, 200, 200);
                    panelBg = Color.FromArgb(30, 30, 30);
                    accent = Color.FromArgb(45, 45, 45);
                    break;
            }

            this.BackColor = bg;
            mainContainer.BackColor = bg;
            
            topBar.BackColor = panelBg;
            topBar.ForeColor = fg;
            
            bottomBar.BackColor = panelBg;
            bottomBar.ForeColor = fg;

            pagePanel.BackColor = panelBg;
            contentBox.BackColor = panelBg;
            contentBox.ForeColor = fg;

            // Update Settings Panel
            if (pnlSettings != null)
            {
                pnlSettings.BackColor = panelBg;
                pnlSettings.ForeColor = fg;
                foreach (Control c in pnlSettings.Controls[0].Controls)
                {
                    if (c is FlowLayoutPanel fp)
                    {
                        foreach (Control b in fp.Controls)
                        {
                            if (b is Button btn)
                            {
                                btn.BackColor = accent;
                                btn.ForeColor = fg;
                                btn.FlatAppearance.BorderSize = 0;
                            }
                        }
                    }
                }
            }

            // Update TOC Panel
            if (pnlTOC != null)
            {
                pnlTOC.BackColor = panelBg;
                pnlTOC.ForeColor = fg;
                lstChapters.BackColor = panelBg;
                lstChapters.ForeColor = fg;
            }

            // Update Buttons
            UpdateButtonStyle(topBar, fg);
            UpdateButtonStyle(bottomBar, fg);

            // Refresh content to apply colors to text
            if (_chapters != null && _chapters.Count > 0)
                DisplayFullChapter(_currentChapterIndex, contentBox.SelectionStart);
        }

        private void UpdateButtonStyle(Panel p, Color fg)
        {
            foreach (Control c in p.Controls)
            {
                if (c is Button b)
                {
                    b.ForeColor = fg;
                    b.FlatAppearance.MouseOverBackColor = Color.FromArgb(50, 128, 128, 128);
                }
            }
        }

        private void ChangeFontSize(float delta)
        {
            currentFontSize = Math.Max(10, Math.Min(30, currentFontSize + delta));
            DisplayFullChapter(_currentChapterIndex, contentBox.SelectionStart);
        }

        private void ToggleSettings()
        {
            pnlSettings.Location = new Point(topBar.Width - pnlSettings.Width - 10, topBar.Height + 5);
            pnlSettings.Visible = !pnlSettings.Visible;
            pnlSettings.BringToFront();
        }

        private void ToggleTOC()
        {
            pnlTOC.Height = mainContainer.Height;
            pnlTOC.Visible = !pnlTOC.Visible;
            pnlTOC.BringToFront();
        }

        // --- LOGIC: FLOATING MENU ---
        private void InitializeFloatingMenu()
        {
            _floatingMenu = new ContextMenuStrip();
            var colors = new[] {
                (Name: "Vàng", Color: Color.Yellow),
                (Name: "Xanh Lá", Color: Color.LightGreen),
                (Name: "Hồng", Color: Color.Pink),
                (Name: "Xanh Dương", Color: Color.LightBlue)
            };

            foreach (var c in colors)
            {
                var item = new ToolStripMenuItem(c.Name);
                Bitmap bmp = new Bitmap(16, 16);
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.Clear(c.Color);
                    g.DrawRectangle(Pens.Gray, 0, 0, 15, 15);
                }
                item.Image = bmp;
                item.Click += (s, e) => CreateHighlight(c.Color);
                _floatingMenu.Items.Add(item);
            }

            _floatingMenu.Items.Add(new ToolStripSeparator());
            var btnNote = new ToolStripMenuItem("📝 Thêm Ghi Chú");
            btnNote.Click += (s, e) => HandleNoteInput();
            _floatingMenu.Items.Add(btnNote);
        }

        private void ContentBox_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && contentBox.SelectionLength > 0)
            {
                _floatingMenu.Show(Cursor.Position);
            }
            else
            {
                // Hide panels if clicking outside
                if (pnlSettings.Visible && !pnlSettings.Bounds.Contains(PointToClient(Cursor.Position)))
                    pnlSettings.Visible = false;
                if (pnlTOC.Visible && !pnlTOC.Bounds.Contains(PointToClient(Cursor.Position)))
                    pnlTOC.Visible = false;
            }
        }

        private void CreateHighlight(Color color, string noteContent = "")
        {
            if (contentBox.SelectionLength == 0) return;
            try
            {
                var hl = new Highlight
                {
                    BookId = _book.Id,
                    UserId = DataManager.Instance.GetCurrentUser(),
                    ChapterIndex = _currentChapterIndex,
                    StartIndex = contentBox.SelectionStart,
                    Length = contentBox.SelectionLength,
                    SelectedText = contentBox.SelectedText,
                    Note = noteContent,
                    ColorHex = ColorTranslator.ToHtml(color)
                };
                DataManager.Instance.AddHighlight(hl);
                contentBox.SelectionBackColor = color;
                contentBox.SelectionLength = 0;
            }
            catch (Exception ex) { MessageBox.Show("Lỗi: " + ex.Message); }
        }

        private void HandleNoteInput()
        {
            if (contentBox.SelectionLength == 0) return;
            using (var dlg = new NoteDialog())
            {
                if (dlg.ShowDialog() == DialogResult.OK)
                    CreateHighlight(Color.Orange, dlg.NoteText);
            }
        }

        // --- LOGIC: LOADING & NAVIGATION ---
        private void BookReaderForm_Load(object sender, EventArgs e)
        {
            BookReaderForm_Resize(this, null);
            LoadBookDataInitial();
        }

        private void BookReaderForm_Resize(object sender, EventArgs e)
        {
            if (mainContainer == null || pagePanel == null) return;

            // Responsive Page Layout
            int maxWidth = 900;
            int w = Math.Min(maxWidth, mainContainer.Width - 40);
            pagePanel.Size = new Size(w, mainContainer.Height);
            pagePanel.Location = new Point((mainContainer.Width - w) / 2, 0);
            
            if (pnlTOC != null) pnlTOC.Height = this.Height - topBar.Height;
        }

        private void LoadBookDataInitial()
        {
            lblChapterInfo.Text = "Đang tải...";
            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += (s, args) =>
            {
                try { _chapters = _readerService.ReadBookContent(_book) ?? new List<BookChapter>(); }
                catch { _chapters = new List<BookChapter>(); }
            };

            worker.RunWorkerCompleted += (s, args) =>
            {
                if (_chapters.Count == 0) _chapters.Add(new BookChapter { ChapterTitle = "Lỗi", Content = "Không thể tải nội dung." });

                lstChapters.Items.Clear();
                foreach (var c in _chapters) lstChapters.Items.Add(c.ChapterTitle);

                if (_targetChapter.HasValue && _targetPosition.HasValue)
                {
                    _currentChapterIndex = Math.Min(Math.Max(0, _targetChapter.Value), _chapters.Count - 1);
                    DisplayFullChapter(_currentChapterIndex, _targetPosition.Value);
                }
                else
                {
                    var pos = _readerService.GetReadingPosition(_book.Id, DataManager.Instance.GetCurrentUser());
                    _currentChapterIndex = Math.Min(Math.Max(0, pos.chapter), _chapters.Count - 1);
                    DisplayFullChapter(_currentChapterIndex, pos.position);
                }
            };
            worker.RunWorkerAsync();
        }

        private void DisplayFullChapter(int index, int scrollPosition = 0)
        {
            if (index < 0 || index >= _chapters.Count) return;
            this.SuspendLayout();

            var chapter = _chapters[index];
            contentBox.Clear();

            // Title Style
            contentBox.SelectionFont = new Font(currentFontFamily, currentFontSize + 8, FontStyle.Bold);
            contentBox.SelectionColor = currentTheme == Theme.Light ? Color.FromArgb(0, 120, 215) : Color.FromArgb(100, 180, 255);
            contentBox.SelectionAlignment = HorizontalAlignment.Center;
            contentBox.AppendText("\n" + chapter.ChapterTitle + "\n\n\n");

            // Body Style
            contentBox.SelectionFont = new Font(currentFontFamily, currentFontSize, FontStyle.Regular);
            contentBox.SelectionColor = contentBox.ForeColor;
            contentBox.SelectionAlignment = HorizontalAlignment.Left;
            contentBox.SelectionRightIndent = 10;
            contentBox.SelectionIndent = 10;
            
            string text = (chapter.Content ?? "").Replace("\n", "\n\n");
            contentBox.AppendText(text);
            contentBox.AppendText("\n\n\n");

            ReloadHighlights(index);

            lblChapterInfo.Text = $"Chương {index + 1} / {_chapters.Count}";
            if (index < lstChapters.Items.Count) lstChapters.SelectedIndex = index;

            contentBox.Select(0, 0);
            if (scrollPosition > 0 && scrollPosition < contentBox.TextLength)
                contentBox.SelectionStart = scrollPosition;
            contentBox.ScrollToCaret();

            this.ResumeLayout();
            contentBox.Focus();
        }

        private void ReloadHighlights(int chapterIndex)
        {
            var highlights = DataManager.Instance.GetHighlightsForBook(_book.Id);
            int originalStart = contentBox.SelectionStart;
            foreach (var hl in highlights)
            {
                if (hl.ChapterIndex == chapterIndex)
                {
                    if (hl.StartIndex >= 0 && (hl.StartIndex + hl.Length) <= contentBox.TextLength)
                    {
                        contentBox.Select(hl.StartIndex, hl.Length);
                        try { contentBox.SelectionBackColor = ColorTranslator.FromHtml(hl.ColorHex); }
                        catch { contentBox.SelectionBackColor = Color.Yellow; }
                    }
                }
            }
            contentBox.Select(originalStart, 0);
        }

        private void NavigateChapter(int step)
        {
            int newIndex = _currentChapterIndex + step;
            if (newIndex >= 0 && newIndex < _chapters.Count)
            {
                SaveCurrentProgress();
                _currentChapterIndex = newIndex;
                DisplayFullChapter(_currentChapterIndex);
            }
        }

        private void SaveCurrentProgress()
        {
            if (_chapters == null || _chapters.Count == 0) return;
            int currentPos = contentBox.GetCharIndexFromPosition(new Point(10, 10));
            _readerService.SaveReadingPosition(_book.Id, DataManager.Instance.GetCurrentUser(), _currentChapterIndex, currentPos);
        }

        private void BookReaderForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            SaveCurrentProgress();
        }

        private void BookReaderForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Left) NavigateChapter(-1);
            else if (e.KeyCode == Keys.Right) NavigateChapter(1);
            else if (e.KeyCode == Keys.Escape) this.Close();
        }
    }
}