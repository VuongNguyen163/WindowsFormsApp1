using System;
using System.Drawing;
using System.Windows.Forms;
using WindowsFormsApp1.Data;

namespace WindowsFormsApp1.Forms
{
    public partial class EditProfileForm : Form
    {
        public User UpdatedUser { get; private set; }

        // Đã xóa txtConfirmPass vì không dùng đến
        private TextBox txtDisplay, txtEmail, txtNewPass;

        public EditProfileForm(User currentUser)
        {
            // Cấu hình Form
            this.Text = "Chỉnh sửa hồ sơ";
            this.Size = new Size(450, 520);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.None;
            this.BackColor = Color.FromArgb(32, 32, 32);
            this.ForeColor = Color.White;
            this.DoubleBuffered = true;

            InitializeModernUI(currentUser);
        }

        private void InitializeModernUI(User user)
        {
            // 1. Header
            Panel pnlHeader = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = Color.FromArgb(25, 25, 25) };
            pnlHeader.MouseDown += (s, e) => { if (e.Button == MouseButtons.Left) { ReleaseCapture(); SendMessage(Handle, 0xA1, 0x2, 0); } };

            Label lblTitle = new Label { Text = "👤 Hồ Sơ Người Dùng", Font = new Font("Segoe UI", 12, FontStyle.Bold), ForeColor = Color.White, Location = new Point(20, 15), AutoSize = true };
            Button btnClose = new Button { Text = "✕", Dock = DockStyle.Right, Width = 50, FlatStyle = FlatStyle.Flat, ForeColor = Color.Gray, Cursor = Cursors.Hand };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += (s, e) => this.Close();

            pnlHeader.Controls.Add(lblTitle);
            pnlHeader.Controls.Add(btnClose);
            this.Controls.Add(pnlHeader);

            // 2. Body
            int startY = 70;
            int gap = 75;

            // Tên đăng nhập (Read-only)
            AddLabel("TÊN ĐĂNG NHẬP", 30, startY);
            var txtUser = AddTextBox(30, startY + 25, true);
            txtUser.Text = user.Username;
            txtUser.Enabled = false;

            // Tên hiển thị
            AddLabel("TÊN HIỂN THỊ", 30, startY + gap);
            txtDisplay = AddTextBox(30, startY + gap + 25);
            txtDisplay.Text = user.DisplayName;

            // Email
            AddLabel("EMAIL LIÊN HỆ", 30, startY + gap * 2);
            txtEmail = AddTextBox(30, startY + gap * 2 + 25);
            txtEmail.Text = user.Email;

            // Separator line
            Panel line = new Panel { Location = new Point(30, startY + gap * 3 + 10), Size = new Size(390, 1), BackColor = Color.FromArgb(60, 60, 60) };
            this.Controls.Add(line);

            // Đổi mật khẩu
            AddLabel("MẬT KHẨU MỚI (Bỏ trống nếu không đổi)", 30, startY + gap * 3 + 25);
            txtNewPass = AddTextBox(30, startY + gap * 3 + 50);
            txtNewPass.UseSystemPasswordChar = true;
            txtNewPass.PasswordChar = '●';
            // [ĐÃ SỬA LỖI] Xóa dòng PlaceholderText gây lỗi trên .NET Framework 4.7.2

            // 3. Footer Buttons
            Button btnSave = new Button
            {
                Text = "LƯU THAY ĐỔI",
                Size = new Size(180, 45),
                Location = new Point(240, 450),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += (s, e) => SaveChanges(user.Id);

            Button btnCancel = new Button
            {
                Text = "Hủy",
                Size = new Size(100, 45),
                Location = new Point(130, 450),
                BackColor = Color.Transparent,
                ForeColor = Color.Gray,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10),
                Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Click += (s, e) => this.Close();

            this.Controls.Add(btnSave);
            this.Controls.Add(btnCancel);

            // Viền form
            this.Paint += (s, e) => ControlPaint.DrawBorder(e.Graphics, this.ClientRectangle, Color.FromArgb(60, 60, 60), ButtonBorderStyle.Solid);
        }

        private void AddLabel(string text, int x, int y)
        {
            Label lbl = new Label
            {
                Text = text,
                Location = new Point(x, y),
                AutoSize = true,
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                ForeColor = Color.Gray
            };
            this.Controls.Add(lbl);
        }

        private TextBox AddTextBox(int x, int y, bool isReadOnly = false)
        {
            // Panel bao ngoài để tạo viền màu
            Panel pnl = new Panel
            {
                Location = new Point(x, y),
                Size = new Size(390, 35),
                BackColor = isReadOnly ? Color.FromArgb(40, 40, 40) : Color.FromArgb(45, 45, 48),
                Padding = new Padding(10, 8, 10, 5)
            };

            TextBox txt = new TextBox
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.None,
                BackColor = pnl.BackColor,
                ForeColor = isReadOnly ? Color.Gray : Color.White,
                Font = new Font("Segoe UI", 11)
            };

            pnl.Controls.Add(txt);

            // Hiệu ứng focus: đổi màu viền
            txt.Enter += (s, e) => { pnl.BackColor = Color.FromArgb(55, 55, 60); txt.BackColor = pnl.BackColor; };
            txt.Leave += (s, e) => { pnl.BackColor = Color.FromArgb(45, 45, 48); txt.BackColor = pnl.BackColor; };

            this.Controls.Add(pnl);
            return txt;
        }

        private void SaveChanges(int userId)
        {
            try
            {
                DataManager.Instance.UpdateUserProfile(userId, txtDisplay.Text, txtEmail.Text, txtNewPass.Text);

                // Cập nhật lại object để trả về Main
                UpdatedUser = new User
                {
                    Id = userId,
                    DisplayName = txtDisplay.Text,
                    Email = txtEmail.Text,
                    Username = DataManager.Instance.Login(txtDisplay.Text, "")?.Username ?? "User"
                };

                MessageBox.Show("Đã cập nhật hồ sơ thành công!", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi: " + ex.Message, "Lỗi cập nhật", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")] public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [System.Runtime.InteropServices.DllImport("user32.dll")] public static extern bool ReleaseCapture();
    }
}