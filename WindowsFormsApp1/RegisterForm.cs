using System;
using System.Drawing;
using System.Windows.Forms;
using WindowsFormsApp1.Data;

namespace WindowsFormsApp1.Forms
{
    public partial class RegisterForm : Form
    {
        public User RegisteredUser { get; private set; }
        private TextBox txtUser, txtPass, txtConfirm, txtName, txtEmail;

        public RegisterForm()
        {
            InitializeCustomUI();
        }

        private void InitializeCustomUI()
        {
            // --- Form Settings ---
            this.Text = "Koodo Reader - Đăng Ký";
            this.Size = new Size(400, 600);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(32, 32, 32);
            this.ForeColor = Color.White;
            this.FormBorderStyle = FormBorderStyle.None;
            this.Padding = new Padding(2);

            // --- Custom Title Bar ---
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

            // Title
            Label lblTitle = new Label
            {
                Text = "Create Account",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(100, 20)
            };

            int y = 80;
            int gap = 65;

            // 1. Username
            Label lblUser = CreateLabel("USERNAME", y);
            txtUser = CreateStyledTextBox(y + 25);
            mainPanel.Controls.Add(lblUser);
            mainPanel.Controls.Add(txtUser);

            y += gap;
            // 2. Display Name
            Label lblName = CreateLabel("DISPLAY NAME", y);
            txtName = CreateStyledTextBox(y + 25);
            mainPanel.Controls.Add(lblName);
            mainPanel.Controls.Add(txtName);

            y += gap;
            // 3. Email
            Label lblEmail = CreateLabel("EMAIL", y);
            txtEmail = CreateStyledTextBox(y + 25);
            mainPanel.Controls.Add(lblEmail);
            mainPanel.Controls.Add(txtEmail);

            y += gap;
            // 4. Password
            Label lblPass = CreateLabel("PASSWORD", y);
            txtPass = CreateStyledTextBox(y + 25);
            txtPass.PasswordChar = '•';
            mainPanel.Controls.Add(lblPass);
            mainPanel.Controls.Add(txtPass);

            y += gap;
            // 5. Confirm Password
            Label lblConfirm = CreateLabel("CONFIRM PASSWORD", y);
            txtConfirm = CreateStyledTextBox(y + 25);
            txtConfirm.PasswordChar = '•';
            mainPanel.Controls.Add(lblConfirm);
            mainPanel.Controls.Add(txtConfirm);

            y += 80;
            // Register Button
            Button btnReg = new Button
            {
                Text = "REGISTER",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Location = new Point(40, y),
                Size = new Size(320, 45),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnReg.FlatAppearance.BorderSize = 0;
            btnReg.Click += BtnReg_Click;
            mainPanel.Controls.Add(btnReg);

            // Login Link
            Label lblLogin = new Label
            {
                Text = "Already have an account? Login",
                Font = new Font("Segoe UI", 9, FontStyle.Underline),
                ForeColor = Color.Gray,
                AutoSize = true,
                Cursor = Cursors.Hand,
                Location = new Point(105, y + 60)
            };
            lblLogin.Click += (s, e) => { this.DialogResult = DialogResult.Retry; this.Close(); };
            lblLogin.MouseEnter += (s, e) => lblLogin.ForeColor = Color.White;
            lblLogin.MouseLeave += (s, e) => lblLogin.ForeColor = Color.Gray;
            mainPanel.Controls.Add(lblLogin);

            mainPanel.Controls.Add(lblTitle);
            this.Controls.Add(mainPanel);

            // Paint Border
            this.Paint += (s, e) => { ControlPaint.DrawBorder(e.Graphics, this.ClientRectangle, Color.FromArgb(60, 60, 60), ButtonBorderStyle.Solid); };
        }

        private Label CreateLabel(string text, int y)
        {
            return new Label
            {
                Text = text,
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                ForeColor = Color.Gray,
                Location = new Point(40, y),
                AutoSize = true
            };
        }

        private TextBox CreateStyledTextBox(int y)
        {
            return new TextBox
            {
                Location = new Point(40, y),
                Size = new Size(320, 30),
                Font = new Font("Segoe UI", 11),
                BackColor = Color.FromArgb(45, 45, 48),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
        }

        private void BtnReg_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtUser.Text) || string.IsNullOrEmpty(txtPass.Text))
            {
                MessageBox.Show("Vui lòng nhập các trường bắt buộc!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (txtPass.Text != txtConfirm.Text)
            {
                MessageBox.Show("Mật khẩu nhập lại không khớp!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                var user = DataManager.Instance.Register(txtUser.Text, txtPass.Text, txtName.Text, txtEmail.Text);
                RegisteredUser = user;
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Lỗi Đăng Ký", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Drag Form Logic
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool ReleaseCapture();
    }
}