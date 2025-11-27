using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using WindowsFormsApp1.Data;

namespace WindowsFormsApp1.Forms
{
    // Thêm từ khóa 'partial' để tránh lỗi nếu bạn chưa kịp xóa file Designer
    public partial class PasswordPromptForm : Form
    {
        public bool IsVerified { get; private set; } = false;
        private TextBox txtPass;
        private Button btnOK, btnCancel;

        public PasswordPromptForm()
        {
            // Cấu hình Form
            this.Text = "Xác thực bảo mật";
            this.Size = new Size(400, 220);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.None; // Không viền hệ thống
            this.BackColor = Color.FromArgb(45, 45, 48); // Màu nền xám đậm
            this.ForeColor = Color.White;
            this.DoubleBuffered = true;

            InitializeModernUI();
        }

        private void InitializeModernUI()
        {
            // 1. Title Bar (Thanh tiêu đề tự vẽ)
            Panel pnlHeader = new Panel { Dock = DockStyle.Top, Height = 40, BackColor = Color.FromArgb(30, 30, 30) };
            pnlHeader.MouseDown += (s, e) => { if (e.Button == MouseButtons.Left) { ReleaseCapture(); SendMessage(Handle, 0xA1, 0x2, 0); } };

            Label lblTitle = new Label
            {
                Text = "🔒 Xác thực bảo mật",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.LightGray,
                Location = new Point(15, 10),
                AutoSize = true
            };

            Button btnClose = new Button { Text = "✕", Dock = DockStyle.Right, Width = 40, FlatStyle = FlatStyle.Flat, ForeColor = Color.Gray, Cursor = Cursors.Hand };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += (s, e) => this.Close();
            btnClose.MouseEnter += (s, e) => { btnClose.BackColor = Color.Red; btnClose.ForeColor = Color.White; };
            btnClose.MouseLeave += (s, e) => { btnClose.BackColor = Color.Transparent; btnClose.ForeColor = Color.Gray; };

            pnlHeader.Controls.Add(lblTitle);
            pnlHeader.Controls.Add(btnClose);

            // 2. Nội dung chính
            Label lblInstruction = new Label
            {
                Text = "Để bảo vệ tài khoản, vui lòng nhập mật khẩu hiện tại của bạn để tiếp tục.",
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.Gainsboro,
                Location = new Point(25, 60),
                Size = new Size(350, 40)
            };

            // Panel chứa TextBox để tạo viền đẹp
            Panel pnlTxtBg = new Panel { Location = new Point(25, 110), Size = new Size(350, 35), BackColor = Color.FromArgb(30, 30, 30) };
            pnlTxtBg.Paint += (s, e) => ControlPaint.DrawBorder(e.Graphics, pnlTxtBg.ClientRectangle, Color.FromArgb(100, 100, 100), ButtonBorderStyle.Solid);

            txtPass = new TextBox
            {
                Location = new Point(10, 8), // Căn giữa panel
                Width = 330,
                BorderStyle = BorderStyle.None,
                BackColor = Color.FromArgb(30, 30, 30),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11),
                PasswordChar = '●',
                UseSystemPasswordChar = true
            };
            pnlTxtBg.Controls.Add(txtPass);

            // 3. Các nút bấm
            btnOK = CreateModernButton("Xác Nhận", 180, 165, true);
            btnOK.Click += BtnOK_Click;

            btnCancel = CreateModernButton("Hủy Bỏ", 290, 165, false);
            btnCancel.Click += (s, e) => this.Close();

            // Thêm controls vào Form
            this.Controls.Add(pnlHeader);
            this.Controls.Add(lblInstruction);
            this.Controls.Add(pnlTxtBg);
            this.Controls.Add(btnOK);
            this.Controls.Add(btnCancel);

            this.AcceptButton = btnOK;
            this.CancelButton = btnCancel;

            // Vẽ viền ngoài cho Form
            this.Paint += (s, e) => ControlPaint.DrawBorder(e.Graphics, this.ClientRectangle, Color.FromArgb(0, 122, 204), ButtonBorderStyle.Solid);
        }

        private Button CreateModernButton(string text, int x, int y, bool isPrimary)
        {
            Button btn = new Button
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(95, 35),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Cursor = Cursors.Hand,
                BackColor = isPrimary ? Color.FromArgb(0, 122, 204) : Color.FromArgb(60, 60, 60),
                ForeColor = Color.White
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        private void BtnOK_Click(object sender, EventArgs e)
        {
            int currentUid = DataManager.Instance.GetCurrentUser();
            if (DataManager.Instance.VerifyCurrentPassword(currentUid, txtPass.Text))
            {
                IsVerified = true;
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                MessageBox.Show("Mật khẩu không đúng!", "Lỗi xác thực", MessageBoxButtons.OK, MessageBoxIcon.Error);
                txtPass.SelectAll();
                txtPass.Focus();
            }
        }

        // Kéo thả Form không viền
        [System.Runtime.InteropServices.DllImport("user32.dll")] public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [System.Runtime.InteropServices.DllImport("user32.dll")] public static extern bool ReleaseCapture();
    }
}