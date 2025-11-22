using System;
using System.Drawing;
using System.Windows.Forms;
using WindowsFormsApp1.Data;

namespace WindowsFormsApp1.Forms
{
    public partial class LoginForm : Form
    {
        public User LoggedInUser { get; private set; }

        private TextBox txtUser;
        private TextBox txtPass;

        public LoginForm()
        {
            InitializeCustomUI();
        }

        private void InitializeCustomUI()
        {
            // --- Form Settings ---
            this.Text = "Koodo Reader - Đăng Nhập";
            this.Size = new Size(400, 450);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(32, 32, 32); // Darker background
            this.ForeColor = Color.White;
            this.FormBorderStyle = FormBorderStyle.None; // Modern borderless look
            this.Padding = new Padding(2); // Border thickness

            // --- Custom Title Bar (Optional, for dragging) ---
            Panel titleBar = new Panel { Dock = DockStyle.Top, Height = 30, BackColor = Color.Transparent };
            titleBar.MouseDown += (s, e) => { if (e.Button == MouseButtons.Left) { ReleaseCapture(); SendMessage(Handle, 0xA1, 0x2, 0); } };
            
            Button btnClose = new Button
            {
                Text = "✕",
                Dock = DockStyle.Right,
                Width = 40,
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.Gray,
                BackColor = Color.Transparent
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += (s, e) => this.Close();
            btnClose.MouseEnter += (s, e) => { btnClose.BackColor = Color.Red; btnClose.ForeColor = Color.White; };
            btnClose.MouseLeave += (s, e) => { btnClose.BackColor = Color.Transparent; btnClose.ForeColor = Color.Gray; };
            titleBar.Controls.Add(btnClose);
            this.Controls.Add(titleBar);

            // --- Main Content Panel ---
            Panel mainPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(40) };
            
            // Logo / Icon
            Label lblIcon = new Label
            {
                Text = "👤",
                Font = new Font("Segoe UI", 40),
                AutoSize = true,
                ForeColor = Color.FromArgb(0, 120, 215),
                Location = new Point(160, 20) // Centered roughly
            };
            
            // Title
            Label lblTitle = new Label
            {
                Text = "Welcome Back",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(110, 90)
            };

            // Username
            Label lblUser = new Label { Text = "USERNAME OR EMAIL", Font = new Font("Segoe UI", 8, FontStyle.Bold), ForeColor = Color.Gray, Location = new Point(40, 150), AutoSize = true };
            txtUser = CreateStyledTextBox(40, 175);
            
            // Password
            Label lblPass = new Label { Text = "PASSWORD", Font = new Font("Segoe UI", 8, FontStyle.Bold), ForeColor = Color.Gray, Location = new Point(40, 220), AutoSize = true };
            txtPass = CreateStyledTextBox(40, 245);
            txtPass.PasswordChar = '•';

            // Login Button
            Button btnLogin = new Button
            {
                Text = "LOGIN",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Location = new Point(40, 310),
                Size = new Size(320, 45),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnLogin.FlatAppearance.BorderSize = 0;
            btnLogin.Click += BtnLogin_Click;

            // Register Link
            Label lblRegister = new Label
            {
                Text = "Don't have an account? Register",
                Font = new Font("Segoe UI", 9, FontStyle.Underline),
                ForeColor = Color.Gray,
                AutoSize = true,
                Cursor = Cursors.Hand,
                Location = new Point(105, 370)
            };
            lblRegister.Click += (s, e) => { this.DialogResult = DialogResult.Retry; this.Close(); }; // Retry indicates switch to register
            lblRegister.MouseEnter += (s, e) => lblRegister.ForeColor = Color.White;
            lblRegister.MouseLeave += (s, e) => lblRegister.ForeColor = Color.Gray;

            mainPanel.Controls.AddRange(new Control[] { lblIcon, lblTitle, lblUser, txtUser, lblPass, txtPass, btnLogin, lblRegister });
            this.Controls.Add(mainPanel);

            // Paint Border
            this.Paint += (s, e) => { ControlPaint.DrawBorder(e.Graphics, this.ClientRectangle, Color.FromArgb(60, 60, 60), ButtonBorderStyle.Solid); };
        }

        private TextBox CreateStyledTextBox(int x, int y)
        {
            return new TextBox
            {
                Location = new Point(x, y),
                Size = new Size(320, 30),
                Font = new Font("Segoe UI", 11),
                BackColor = Color.FromArgb(45, 45, 48),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
        }

        private void BtnLogin_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtUser.Text) || string.IsNullOrEmpty(txtPass.Text))
            {
                MessageBox.Show("Vui lòng nhập đầy đủ thông tin!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var user = DataManager.Instance.Login(txtUser.Text, txtPass.Text);
            if (user != null)
            {
                LoggedInUser = user;
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                MessageBox.Show("Sai tên đăng nhập hoặc mật khẩu!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Drag Form Logic
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool ReleaseCapture();
    }
}