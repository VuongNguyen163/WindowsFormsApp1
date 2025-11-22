using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using WindowsFormsApp1.Data;

namespace WindowsFormsApp1.Forms
{
    public class ManageShelfDialog : Form
    {
        private FlowLayoutPanel pnlList;
        private Button btnClose;

        public ManageShelfDialog()
        {
            InitializeComponent();
            LoadShelves();
        }

        private void InitializeComponent()
        {
            this.Text = "Manage shelf";
            this.Size = new Size(400, 500);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(32, 32, 32);
            this.ForeColor = Color.White;
            this.FormBorderStyle = FormBorderStyle.None;
            this.Padding = new Padding(2);

            // Title Bar
            Panel titleBar = new Panel { Dock = DockStyle.Top, Height = 40, BackColor = Color.Transparent };
            titleBar.MouseDown += (s, e) => { if (e.Button == MouseButtons.Left) { ReleaseCapture(); SendMessage(Handle, 0xA1, 0x2, 0); } };

            Label lblTitle = new Label
            {
                Text = "Manage shelf",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(20, 10),
                AutoSize = true
            };
            titleBar.Controls.Add(lblTitle);

            btnClose = new Button
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

            // List Panel
            pnlList = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Padding = new Padding(10)
            };
            this.Controls.Add(pnlList);
            this.Controls.Add(titleBar);

            // Border
            this.Paint += (s, e) => { ControlPaint.DrawBorder(e.Graphics, this.ClientRectangle, Color.FromArgb(60, 60, 60), ButtonBorderStyle.Solid); };
        }

        private void LoadShelves()
        {
            pnlList.Controls.Clear();
            var shelves = DataManager.Instance.GetShelvesList();

            foreach (var shelf in shelves)
            {
                pnlList.Controls.Add(CreateShelfItem(shelf));
            }
        }

        private Panel CreateShelfItem(Shelf shelf)
        {
            Panel pnl = new Panel
            {
                Size = new Size(360, 50),
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 0, 5)
            };

            // Name Label
            Label lblName = new Label
            {
                Text = shelf.Name,
                Font = new Font("Segoe UI", 11),
                ForeColor = Color.White,
                Location = new Point(10, 12),
                AutoSize = true,
                MaximumSize = new Size(200, 0)
            };

            // Edit TextBox (Hidden initially)
            TextBox txtEdit = new TextBox
            {
                Text = shelf.Name,
                Font = new Font("Segoe UI", 11),
                Location = new Point(10, 10),
                Width = 200,
                Visible = false,
                BackColor = Color.FromArgb(45, 45, 48),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            // Buttons
            Button btnDelete = CreateIconButton("🗑", 320);
            Button btnEdit = CreateIconButton("✎", 280);
            Button btnSave = CreateIconButton("✔", 280); // Replaces Edit when editing
            btnSave.Visible = false;

            // Events
            btnDelete.Click += (s, e) =>
            {
                if (MessageBox.Show($"Delete shelf '{shelf.Name}'?", "Confirm", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    try
                    {
                        DataManager.Instance.DeleteShelf(shelf.Id);
                        LoadShelves();
                    }
                    catch (Exception ex) { MessageBox.Show(ex.Message); }
                }
            };

            btnEdit.Click += (s, e) =>
            {
                lblName.Visible = false;
                txtEdit.Visible = true;
                btnEdit.Visible = false;
                btnSave.Visible = true;
                txtEdit.Focus();
            };

            btnSave.Click += (s, e) =>
            {
                string newName = txtEdit.Text.Trim();
                if (!string.IsNullOrEmpty(newName))
                {
                    try
                    {
                        DataManager.Instance.RenameShelf(shelf.Id, newName);
                        LoadShelves();
                    }
                    catch (Exception ex) { MessageBox.Show(ex.Message); }
                }
            };

            pnl.Controls.AddRange(new Control[] { lblName, txtEdit, btnDelete, btnEdit, btnSave });
            
            // Hover effect
            pnl.MouseEnter += (s, e) => pnl.BackColor = Color.FromArgb(45, 45, 48);
            pnl.MouseLeave += (s, e) => pnl.BackColor = Color.Transparent;

            return pnl;
        }

        private Button CreateIconButton(string text, int x)
        {
            Button btn = new Button
            {
                Text = text,
                Location = new Point(x, 10),
                Size = new Size(30, 30),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.Gray,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 10),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.MouseEnter += (s, e) => btn.ForeColor = Color.White;
            btn.MouseLeave += (s, e) => btn.ForeColor = Color.Gray;
            return btn;
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool ReleaseCapture();
    }
}
