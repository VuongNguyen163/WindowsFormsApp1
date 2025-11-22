using System;
using System.Drawing;
using System.Windows.Forms;
using System.Linq;
using WindowsFormsApp1.Data;

namespace WindowsFormsApp1.Forms
{
    public partial class AddToShelfDialog : Form
    {
        public int SelectedShelfId { get; private set; } = -1;
        public string NewShelfName { get; private set; } = null;

        private ComboBox cmbShelves;
        private TextBox txtNewShelf;
        private Button btnConfirm;
        private Button btnCancel;

        public AddToShelfDialog()
        {
            InitializeCustomUI();
            LoadShelves();
        }

        private void InitializeCustomUI()
        {
            // Form Settings
            this.Text = "Add to shelf";
            this.Size = new Size(400, 220);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.None; // Không viền giống popup
            this.BackColor = Color.FromArgb(45, 45, 48); // Màu nền tối

            // Viền form
            this.Paint += (s, e) => {
                ControlPaint.DrawBorder(e.Graphics, this.ClientRectangle, Color.Gray, ButtonBorderStyle.Solid);
            };

            // Label Title
            Label lblTitle = new Label
            {
                Text = "Add to shelf",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(20, 15),
                AutoSize = true
            };

            // Label Select
            Label lblSelect = new Label
            {
                Text = "Select",
                ForeColor = Color.Silver,
                Location = new Point(30, 60),
                Size = new Size(50, 25),
                Font = new Font("Segoe UI", 10)
            };

            // ComboBox Existing Shelves
            cmbShelves = new ComboBox
            {
                Location = new Point(90, 58),
                Size = new Size(260, 25),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(60, 60, 63),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10)
            };

            // Label New
            Label lblNew = new Label
            {
                Text = "New",
                ForeColor = Color.Silver,
                Location = new Point(30, 100),
                Size = new Size(50, 25),
                Font = new Font("Segoe UI", 10)
            };

            // TextBox New Shelf
            txtNewShelf = new TextBox
            {
                Location = new Point(90, 98),
                Size = new Size(260, 25),
                BackColor = Color.FromArgb(60, 60, 63),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10)
            };
            // Placeholder logic
            txtNewShelf.Enter += (s, e) => { if (txtNewShelf.Text == "") txtNewShelf.ForeColor = Color.White; };

            // Button Confirm
            btnConfirm = new Button
            {
                Text = "Confirm",
                Location = new Point(250, 160),
                Size = new Size(100, 35),
                BackColor = Color.FromArgb(60, 60, 63), // Màu xám đậm
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnConfirm.FlatAppearance.BorderSize = 0;
            btnConfirm.Click += BtnConfirm_Click;

            // Button Cancel
            btnCancel = new Button
            {
                Text = "Cancel",
                Location = new Point(140, 160),
                Size = new Size(100, 35),
                BackColor = Color.Transparent,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderSize = 1;
            btnCancel.FlatAppearance.BorderColor = Color.Gray;
            btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

            // Add Controls
            this.Controls.Add(lblTitle);
            this.Controls.Add(lblSelect);
            this.Controls.Add(cmbShelves);
            this.Controls.Add(lblNew);
            this.Controls.Add(txtNewShelf);
            this.Controls.Add(btnConfirm);
            this.Controls.Add(btnCancel);
        }

        private void LoadShelves()
        {
            var shelves = DataManager.Instance.GetShelvesList();
            // Thêm option mặc định
            shelves.Insert(0, new Shelf { Id = -1, Name = "Choose existing shelf..." });

            cmbShelves.DataSource = shelves;
            cmbShelves.DisplayMember = "Name";
            cmbShelves.ValueMember = "Id";
        }

        private void BtnConfirm_Click(object sender, EventArgs e)
        {
            string newName = txtNewShelf.Text.Trim();

            // Ưu tiên 1: Nếu có nhập tên mới -> Tạo shelf mới
            if (!string.IsNullOrEmpty(newName))
            {
                NewShelfName = newName;
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            // Ưu tiên 2: Nếu chọn shelf có sẵn
            else if (cmbShelves.SelectedIndex > 0) // > 0 để bỏ qua dòng "Choose..."
            {
                SelectedShelfId = (int)cmbShelves.SelectedValue;
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                MessageBox.Show("Vui lòng chọn một kệ hoặc nhập tên kệ mới.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}