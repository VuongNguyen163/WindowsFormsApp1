using System;
using System.Drawing;
using System.Windows.Forms;

namespace WindowsFormsApp1.Forms // Chú ý namespace phải khớp với folder
{
    public partial class NoteDialog : Form
    {
        public string NoteText { get; private set; }
        private TextBox txtNote;

        public NoteDialog()
        {
            // Thiết lập giao diện cửa sổ nhập Note
            this.Text = "Thêm Ghi Chú";
            this.Size = new Size(350, 220);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(45, 45, 48);
            this.ForeColor = Color.White;
            this.FormBorderStyle = FormBorderStyle.FixedToolWindow;

            Label lbl = new Label { Text = "Nhập suy nghĩ của bạn:", Location = new Point(15, 15), AutoSize = true, Font = new Font("Segoe UI", 10) };

            txtNote = new TextBox
            {
                Location = new Point(15, 45),
                Size = new Size(300, 80),
                Multiline = true,
                BackColor = Color.FromArgb(60, 60, 63),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10)
            };

            Button btnSave = new Button
            {
                Text = "Lưu Note",
                Location = new Point(215, 140),
                Size = new Size(100, 30),
                BackColor = Color.FromArgb(0, 120, 215),
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.OK,
                Cursor = Cursors.Hand
            };
            btnSave.Click += (s, e) => { NoteText = txtNote.Text; this.Close(); };

            Button btnCancel = new Button
            {
                Text = "Hủy",
                Location = new Point(105, 140),
                Size = new Size(100, 30),
                BackColor = Color.Gray,
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.Cancel,
                Cursor = Cursors.Hand
            };

            this.Controls.AddRange(new Control[] { lbl, txtNote, btnSave, btnCancel });
        }
    }
}