using System;
using System.Drawing;
using System.Windows.Forms;

namespace WindowsFormsApp1.Forms
{
    public class AddShelfDialog : Form
    {
        private TextBox txtName;
        private TextBox txtDescription;
        private Button btnCreate;
        private Button btnCancel;

        public string ShelfName => txtName.Text.Trim();
        public string ShelfDescription => txtDescription.Text.Trim();

        public AddShelfDialog()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Tạo Kệ Sách Mới";
            this.Size = new Size(400, 250);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // Label Name
            Label lblName = new Label();
            lblName.Text = "Tên Kệ Sách:";
            lblName.Location = new Point(20, 20);
            lblName.AutoSize = true;
            this.Controls.Add(lblName);

            // TextBox Name
            txtName = new TextBox();
            txtName.Location = new Point(20, 45);
            txtName.Width = 340;
            this.Controls.Add(txtName);

            // Label Description
            Label lblDesc = new Label();
            lblDesc.Text = "Mô Tả (Tùy chọn):";
            lblDesc.Location = new Point(20, 80);
            lblDesc.AutoSize = true;
            this.Controls.Add(lblDesc);

            // TextBox Description
            txtDescription = new TextBox();
            txtDescription.Location = new Point(20, 105);
            txtDescription.Width = 340;
            txtDescription.Height = 50;
            txtDescription.Multiline = true;
            this.Controls.Add(txtDescription);

            // Button Create
            btnCreate = new Button();
            btnCreate.Text = "Tạo";
            btnCreate.DialogResult = DialogResult.OK;
            btnCreate.Location = new Point(200, 170);
            btnCreate.Click += BtnCreate_Click;
            this.Controls.Add(btnCreate);

            // Button Cancel
            btnCancel = new Button();
            btnCancel.Text = "Hủy";
            btnCancel.DialogResult = DialogResult.Cancel;
            btnCancel.Location = new Point(285, 170);
            this.Controls.Add(btnCancel);

            this.AcceptButton = btnCreate;
            this.CancelButton = btnCancel;
        }

        private void BtnCreate_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show("Vui lòng nhập tên kệ sách!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                this.DialogResult = DialogResult.None; // Prevent closing
                txtName.Focus();
            }
        }
    }
}
