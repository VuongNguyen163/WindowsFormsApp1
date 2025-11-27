using System.Drawing;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    // Custom Menu Renderer để tạo giao diện đẹp cho ContextMenuStrip
    public class CustomMenuRenderer : ToolStripProfessionalRenderer
    {
        public CustomMenuRenderer() : base(new CustomColorTable()) { }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            if (e.Item.Selected)
            {
                // Màu khi hover
                e.Graphics.FillRectangle(
                    new SolidBrush(Color.FromArgb(60, 60, 63)),
                    new Rectangle(Point.Empty, e.Item.Size)
                );
            }
            else
            {
                // Màu nền mặc định
                e.Graphics.FillRectangle(
                    new SolidBrush(Color.FromArgb(40, 40, 42)),
                    new Rectangle(Point.Empty, e.Item.Size)
                );
            }
        }

        protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
        {
            // Vẽ dấu check đẹp hơn
            e.Graphics.FillRectangle(
                new SolidBrush(Color.FromArgb(0, 120, 215)),
                new Rectangle(e.ImageRectangle.Left - 2, e.ImageRectangle.Top - 2,
                             e.ImageRectangle.Width + 4, e.ImageRectangle.Height + 4)
            );
            
            using (Pen pen = new Pen(Color.White, 2))
            {
                e.Graphics.DrawString("✓", new Font("Segoe UI", 10, FontStyle.Bold),
                    Brushes.White, e.ImageRectangle.Left - 3, e.ImageRectangle.Top - 3);
            }
        }
    }

    // Custom Color Table cho menu
    public class CustomColorTable : ProfessionalColorTable
    {
        public override Color MenuItemSelected => Color.FromArgb(60, 60, 63);
        public override Color MenuItemSelectedGradientBegin => Color.FromArgb(60, 60, 63);
        public override Color MenuItemSelectedGradientEnd => Color.FromArgb(60, 60, 63);
        public override Color MenuItemBorder => Color.FromArgb(70, 70, 73);
        public override Color MenuBorder => Color.FromArgb(70, 70, 73);
        public override Color ImageMarginGradientBegin => Color.FromArgb(40, 40, 42);
        public override Color ImageMarginGradientMiddle => Color.FromArgb(40, 40, 42);
        public override Color ImageMarginGradientEnd => Color.FromArgb(40, 40, 42);
        public override Color ToolStripDropDownBackground => Color.FromArgb(40, 40, 42);
        public override Color SeparatorDark => Color.FromArgb(60, 60, 63);
        public override Color SeparatorLight => Color.FromArgb(60, 60, 63);
    }
}
