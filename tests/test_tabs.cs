using System;
using System.Drawing;
using System.Windows.Forms;

public class TestForm : Form
{
    private class MainTabControl : TabControl
    {
        public bool IsDarkMode { get; set; } = true;
        
        public MainTabControl()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Color bgColor = IsDarkMode ? Color.FromArgb(45, 45, 48) : SystemColors.Control;
            e.Graphics.Clear(bgColor);
            
            for (int i = 0; i < TabCount; i++)
            {
                var rect = GetTabRect(i);
                
                Color tabBg = IsDarkMode ? Color.FromArgb(45, 45, 48) : Color.White;
                Color textCol = IsDarkMode ? Color.LightGray : Color.Black;
                Color selBg = IsDarkMode ? Color.FromArgb(60, 60, 60) : SystemColors.ControlLight;
                
                if (SelectedIndex == i)
                {
                    tabBg = selBg;
                    textCol = IsDarkMode ? Color.White : Color.Black;
                }
                
                using (Brush b = new SolidBrush(tabBg))
                    e.Graphics.FillRectangle(b, rect);
                
                TextRenderer.DrawText(e.Graphics, TabPages[i].Text, this.Font, rect, textCol, TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter | TextFormatFlags.EndEllipsis);
            }
        }
        
        protected override void OnPaintBackground(PaintEventArgs pevent) { }
    }

    public TestForm()
    {
        this.BackColor = Color.FromArgb(30,30,30);
        
        MainTabControl tab1 = new MainTabControl {
            Location = new Point(10, 10),
            Size = new Size(300, 200),
            ItemSize = new Size(80, 25)
        };
        tab1.TabPages.Add("Tab 1");
        tab1.TabPages.Add("Tab 2");

        this.Controls.Add(tab1);
    }

    static void Main()
    {
        Application.Run(new TestForm());
    }
}
