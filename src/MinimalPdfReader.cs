using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Security;

// --- Native Wrapper ---
public static class PdfNative
{
    [DllImport("user32.dll")] 
    public static extern bool SetProcessDPIAware();

    [DllImport("pdfium.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern void FPDF_InitLibrary();

    [DllImport("pdfium.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern void FPDF_DestroyLibrary();

    [DllImport("pdfium.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr FPDF_LoadDocument(string path, string password);

    [DllImport("pdfium.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern void FPDF_CloseDocument(IntPtr document);

    [DllImport("pdfium.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern int FPDF_GetPageCount(IntPtr document);

    [DllImport("pdfium.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr FPDF_LoadPage(IntPtr document, int page_index);

    [DllImport("pdfium.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern void FPDF_ClosePage(IntPtr page);

    [DllImport("pdfium.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern double FPDF_GetPageWidth(IntPtr page);

    [DllImport("pdfium.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern double FPDF_GetPageHeight(IntPtr page);

    [DllImport("pdfium.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr FPDFBitmap_Create(int width, int height, int alpha);

    [DllImport("pdfium.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern void FPDFBitmap_FillRect(IntPtr bitmap, int left, int top, int width, int height, int color);

    [DllImport("pdfium.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern void FPDFBitmap_Destroy(IntPtr bitmap);

    [DllImport("pdfium.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr FPDFBitmap_GetBuffer(IntPtr bitmap);

    [DllImport("pdfium.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern int FPDFBitmap_GetStride(IntPtr bitmap);

    [DllImport("pdfium.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern void FPDF_RenderPageBitmap(IntPtr bitmap, IntPtr page, int start_x, int start_y, int size_x, int size_y, int rotate, int flags);

    [DllImport("pdfium.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern uint FPDF_GetLastError();

    [DllImport("pdfium.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr FPDFText_LoadPage(IntPtr page);

    [DllImport("pdfium.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern void FPDFText_ClosePage(IntPtr text_page);

    [DllImport("pdfium.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern int FPDFText_GetCharIndexAtPos(IntPtr text_page, double x, double y, double xTolerance, double yTolerance);

    [DllImport("pdfium.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern int FPDFText_GetText(IntPtr text_page, int start_index, int count, byte[] result);

    [DllImport("pdfium.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern int FPDFText_CountRects(IntPtr text_page, int start_index, int count);

    [DllImport("pdfium.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool FPDFText_GetRect(IntPtr text_page, int rect_index, out double left, out double top, out double right, out double bottom);

    [DllImport("pdfium.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern void FPDF_DeviceToPage(IntPtr page, int start_x, int start_y, int size_x, int size_y, int rotate, int device_x, int device_y, out double page_x, out double page_y);

    [DllImport("pdfium.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern void FPDF_PageToDevice(IntPtr page, int start_x, int start_y, int size_x, int size_y, int rotate, double page_x, double page_y, out int device_x, out int device_y);
}

public class RoundButton : Button
{
    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        using (System.Drawing.Drawing2D.GraphicsPath path = new System.Drawing.Drawing2D.GraphicsPath())
        {
            path.AddEllipse(new Rectangle(0, 0, this.Width, this.Height));
            this.Region = new Region(path);
        }
    }
}

// --- Individual PDF View Control ---
public class PdfViewer : UserControl
{
    private IntPtr _doc = IntPtr.Zero;
    private int _pageCount = 0;
    private int _currentPage = 0;
    private float _zoomLevel = 1.0f;
    private IntPtr _currentPdfBitmap = IntPtr.Zero;
    private IntPtr _currentPdfPage = IntPtr.Zero;
    private IntPtr _currentTextPage = IntPtr.Zero;
    
    // Selection
    private int _selStartIndex = -1;
    private int _selEndIndex = -1;
    private bool _isSelecting = false;
    private bool _isDarkMode = false;
    private Timer _resizeTimer;

    public bool IsDarkMode
    {
        get { return _isDarkMode; }
        set {
            _isDarkMode = value;
            ApplyTheme();
            if (_currentPage >= 0) ShowPage(_currentPage);
        }
    }

    public event EventHandler LoadFailed;

    // UI Elements
    private Panel _toolbar;
    private Button _btnPrev;
    private Button _btnNext;
    private Button _btnZoomIn;
    private Button _btnZoomOut;
    private Label _lblPage;
    private Panel _canvasPanel;
    private PictureBox _canvas;

    public PdfViewer()
    {
        InitializeUI();
    }

    private void InitializeUI()
    {
        // Dock setting
        this.Dock = DockStyle.Fill;

        Font largeFont = new Font(Control.DefaultFont.FontFamily, Control.DefaultFont.Size * 3);
        Font mediumFont = new Font(Control.DefaultFont.FontFamily, Control.DefaultFont.Size * 1.5f);

        _toolbar = new Panel { Dock = DockStyle.Top, Height = 70 };
        
        FlowLayoutPanel centerPanel = new FlowLayoutPanel {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            WrapContents = false,
            Anchor = AnchorStyles.None,
            BackColor = Color.Transparent
        };

        _btnZoomOut = new RoundButton { Text = "-", Size = new Size(50, 50), Font = mediumFont, Margin = new Padding(10, 15, 10, 10), FlatStyle = FlatStyle.Flat };
        _btnZoomOut.FlatAppearance.BorderSize = 0;
        
        _btnPrev = new Button { Text = "< Prev", Width = 200, Height = 50, Enabled = false, Font = mediumFont, Margin = new Padding(10, 15, 10, 10) };
        _lblPage = new Label { Text = "0 / 0", Width = 250, Height = 50, TextAlign = ContentAlignment.MiddleCenter, Font = mediumFont, Margin = new Padding(10, 15, 10, 10) };
        _btnNext = new Button { Text = "Next >", Width = 200, Height = 50, Enabled = false, Font = mediumFont, Margin = new Padding(10, 15, 10, 10) };

        _btnZoomIn = new RoundButton { Text = "+", Size = new Size(50, 50), Font = mediumFont, Margin = new Padding(10, 15, 10, 10), FlatStyle = FlatStyle.Flat };
        _btnZoomIn.FlatAppearance.BorderSize = 0;

        _btnPrev.Click += (s, e) => PrevPage();
        _btnNext.Click += (s, e) => NextPage();
        _btnZoomOut.Click += (s, e) => { _zoomLevel = Math.Max(0.2f, _zoomLevel - 0.2f); ShowPage(_currentPage); };
        _btnZoomIn.Click += (s, e) => { _zoomLevel = Math.Min(5.0f, _zoomLevel + 0.2f); ShowPage(_currentPage); };

        centerPanel.Controls.Add(_btnZoomOut);
        centerPanel.Controls.Add(_btnPrev);
        centerPanel.Controls.Add(_lblPage);
        centerPanel.Controls.Add(_btnNext);
        centerPanel.Controls.Add(_btnZoomIn);

        _toolbar.Controls.Add(centerPanel);
        _toolbar.Resize += (s, e) => {
            if (_toolbar.Width > 0 && centerPanel.Width > 0)
            {
                centerPanel.Left = (_toolbar.Width - centerPanel.Width) / 2;
                centerPanel.Top = (_toolbar.Height - centerPanel.Height) / 2;
            }
        };

        _canvasPanel = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
        _canvas = new PictureBox { Location = new Point(0, 0), Size = new Size(1, 1), SizeMode = PictureBoxSizeMode.AutoSize };
        
        ApplyTheme();

        // Mouse Events for Selection
        _canvas.MouseDown += (s, e) => {
            _isSelecting = true;
            _selStartIndex = GetCharIndexAt(e.X, e.Y);
            _selEndIndex = _selStartIndex;
            _canvas.Invalidate();
        };

        _canvas.MouseMove += (s, e) => {
            if (_isSelecting) {
                _selEndIndex = GetCharIndexAt(e.X, e.Y);
                _canvas.Invalidate();
            }
            // Update cursor if over text?
            int idx = GetCharIndexAt(e.X, e.Y);
            _canvas.Cursor = (idx >= 0) ? Cursors.IBeam : Cursors.Default;
        };

        _canvas.MouseUp += (s, e) => { _isSelecting = false; };
        _canvas.Paint += OnCanvasPaint;

        _canvasPanel.Controls.Add(_canvas);

        this.Controls.Add(_canvasPanel);
        this.Controls.Add(_toolbar);

        // Events
        _canvasPanel.MouseWheel += (s, e) => DoScroll(0, -e.Delta);

        _resizeTimer = new Timer { Interval = 150 };
        _resizeTimer.Tick += (s, e) => {
            _resizeTimer.Stop();
            if (_currentPage >= 0 && _doc != IntPtr.Zero)
            {
                ShowPage(_currentPage);
            }
        };

        Size lastSize = _canvasPanel.Size;
        _canvasPanel.Resize += (s, e) => {
            CenterCanvas();
            if (_canvasPanel.Width != lastSize.Width)
            {
                lastSize = _canvasPanel.Size;
                _resizeTimer.Stop();
                _resizeTimer.Start();
            }
            else
            {
                lastSize = _canvasPanel.Size;
            }
        };
    }

    private void CenterCanvas()
    {
        if (_canvas.Image != null)
        {
            int cw = _canvasPanel.ClientSize.Width;
            int ch = _canvasPanel.ClientSize.Height;
            int iw = _canvas.Width;
            int ih = _canvas.Height;
            _canvas.Location = new Point(Math.Max(0, (cw - iw) / 2), Math.Max(0, (ch - ih) / 2));
        }
    }

    private string PromptForPassword(string text, string caption)
    {
        Form prompt = new Form()
        {
            ClientSize = new Size(400, 180),
            FormBorderStyle = FormBorderStyle.FixedDialog,
            Text = caption,
            StartPosition = FormStartPosition.CenterScreen,
            MaximizeBox = false,
            MinimizeBox = false
        };
        Label textLabel = new Label() { Left = 30, Top = 30, Width = 340, Text = text, AutoSize = true };
        TextBox textBox = new TextBox() { Left = 30, Top = 70, Width = 340, UseSystemPasswordChar = true };
        Button confirmation = new Button() { Text = "Ok", Left = 270, Width = 100, Height = 35, Top = 120, DialogResult = DialogResult.OK };
        Button cancel = new Button() { Text = "Cancel", Left = 150, Width = 100, Height = 35, Top = 120, DialogResult = DialogResult.Cancel };
        prompt.Controls.Add(textLabel);
        prompt.Controls.Add(textBox);
        prompt.Controls.Add(confirmation);
        prompt.Controls.Add(cancel);
        prompt.AcceptButton = confirmation;
        prompt.CancelButton = cancel;

        string result = null;
        if (prompt.ShowDialog() == DialogResult.OK)
            result = textBox.Text;
            
        prompt.Dispose();
        return result;
    }

    public void LoadFile(string path)
    {
        CloseFile(); // Cleanup any existing document

        try
        {
            _doc = PdfNative.FPDF_LoadDocument(path, null);
            int attempts = 0;

            while (_doc == IntPtr.Zero)
            {
                uint err = PdfNative.FPDF_GetLastError();
                if (err == 4) // Password required
                {
                    if (attempts >= 3)
                    {
                        MessageBox.Show("Maximum password attempts reached.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        if (LoadFailed != null) LoadFailed(this, EventArgs.Empty);
                        return;
                    }

                    string password = PromptForPassword("Enter password for PDF:", "Password Required");
                    if (password == null) // User cancelled
                    {
                        if (LoadFailed != null) LoadFailed(this, EventArgs.Empty);
                        return;
                    }

                    _doc = PdfNative.FPDF_LoadDocument(path, password);
                    attempts++;
                }
                else
                {
                    string msg = "Failed to open document.";
                    if (err == 1) msg = "Unknown error.";
                    else if (err == 2) msg = "File not found or could not be opened.";
                    else if (err == 3) msg = "File not in PDF format or corrupted.";
                    else if (err == 5) msg = "Unsupported security scheme.";
                    else if (err == 6) msg = "Page not found or content error.";

                    MessageBox.Show(msg, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    if (LoadFailed != null) LoadFailed(this, EventArgs.Empty);
                    return;
                }
            }

            _pageCount = PdfNative.FPDF_GetPageCount(_doc);
            _currentPage = 0;
            ShowPage(_currentPage);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Error loading PDF: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            if (LoadFailed != null) LoadFailed(this, EventArgs.Empty);
        }
    }

    public void CloseFile()
    {
        CleanupCurrentPage();
        if (_doc != IntPtr.Zero)
        {
            PdfNative.FPDF_CloseDocument(_doc);
            _doc = IntPtr.Zero;
        }
    }

    private void ApplyTheme()
    {
        if (_isDarkMode)
        {
            this.BackColor = Color.FromArgb(30, 30, 30);
            _toolbar.BackColor = Color.FromArgb(45, 45, 48);
            _lblPage.ForeColor = Color.White;
            _toolbar.ForeColor = Color.White;
            _canvasPanel.BackColor = Color.FromArgb(30,30,30);
        }
        else
        {
            this.BackColor = SystemColors.Control;
            _toolbar.BackColor = SystemColors.Control;
            _lblPage.ForeColor = SystemColors.ControlText;
            _toolbar.ForeColor = SystemColors.ControlText;
            _canvasPanel.BackColor = SystemColors.Control;
        }

        if (_btnPrev != null && _btnNext != null && _btnZoomIn != null && _btnZoomOut != null)
        {
            foreach (Button b in new Button[] { _btnPrev, _btnNext, _btnZoomIn, _btnZoomOut })
            {
                if (_isDarkMode)
                {
                    b.BackColor = Color.FromArgb(60, 60, 60);
                    b.ForeColor = Color.White;
                }
                else
                {
                    b.UseVisualStyleBackColor = true;
                    b.ForeColor = SystemColors.ControlText;
                    if (b is RoundButton) b.BackColor = SystemColors.Control;
                }
            }
        }
    }

    private void CleanupCurrentPage()
    {
        if (_canvas.Image != null)
        {
            _canvas.Image.Dispose();
            _canvas.Image = null;
        }

        if (_currentPdfBitmap != IntPtr.Zero)
        {
            PdfNative.FPDFBitmap_Destroy(_currentPdfBitmap);
            _currentPdfBitmap = IntPtr.Zero;
        }

        if (_currentTextPage != IntPtr.Zero)
        {
            PdfNative.FPDFText_ClosePage(_currentTextPage);
            _currentTextPage = IntPtr.Zero;
        }

        if (_currentPdfPage != IntPtr.Zero)
        {
            PdfNative.FPDF_ClosePage(_currentPdfPage);
            _currentPdfPage = IntPtr.Zero;
        }
        
        _selStartIndex = -1;
        _selEndIndex = -1;
    }

    [HandleProcessCorruptedStateExceptions]
    [SecurityCritical]
    private void ShowPage(int index)
    {
        if (_doc == IntPtr.Zero || index < 0 || index >= _pageCount) return;

        CleanupCurrentPage();

        _currentPage = index;
        _currentPdfPage = PdfNative.FPDF_LoadPage(_doc, index);
        if (_currentPdfPage == IntPtr.Zero) return;

        try
        {
            double pageWidth = PdfNative.FPDF_GetPageWidth(_currentPdfPage);
            double pageHeight = PdfNative.FPDF_GetPageHeight(_currentPdfPage);

            int viewWidth = _canvasPanel.Width - 30;
            if (viewWidth < 100) viewWidth = 600;

            float scale = (float)(viewWidth / pageWidth) * _zoomLevel;
            int width = (int)(pageWidth * scale);
            int height = (int)(pageHeight * scale);

            _currentPdfBitmap = PdfNative.FPDFBitmap_Create(width, height, 1);
            if (_currentPdfBitmap == IntPtr.Zero) throw new Exception("Failed to create bitmap");

            PdfNative.FPDFBitmap_FillRect(_currentPdfBitmap, 0, 0, width, height, unchecked((int)0xFFFFFFFF));
            PdfNative.FPDF_RenderPageBitmap(_currentPdfBitmap, _currentPdfPage, 0, 0, width, height, 0, 0x10);

            IntPtr buffer = PdfNative.FPDFBitmap_GetBuffer(_currentPdfBitmap);
            int stride = PdfNative.FPDFBitmap_GetStride(_currentPdfBitmap);
            
            if (buffer != IntPtr.Zero && stride >= width * 4)
            {
                unsafe
                {
                    byte* ptr = (byte*)buffer.ToPointer();
                    for (int y = 0; y < height; y++)
                    {
                        byte* row = ptr + (y * stride);
                        for (int x = 0; x < width; x++)
                        {
                            int offset = x * 4;
                            // Read original BGR(A)
                            byte b = row[offset];
                            byte g = row[offset + 1];
                            byte r = row[offset + 2];
                            
                            // Swap B and R (to RGB or BGR depending on what it was vs what we want)
                            // Original code swapped 0 and 2. 
                            row[offset] = r;
                            row[offset + 1] = g;
                            row[offset + 2] = b;
                        }
                    }
                }
            }

            Bitmap wrapper = new Bitmap(width, height, stride, PixelFormat.Format32bppArgb, buffer);
            _canvas.Image = wrapper;
        }
        catch(Exception ex)
        {
             MessageBox.Show("Error rendering page: " + ex.Message);
        }

        // Load Text Page
        try
        {
            _currentTextPage = PdfNative.FPDFText_LoadPage(_currentPdfPage);
        }
        catch (Exception) { _currentTextPage = IntPtr.Zero; }

        UpdateUI();
        _canvasPanel.AutoScrollPosition = new Point(0, 0);
        CenterCanvas();
        
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }

    private void PrevPage()
    {
        if (_currentPage > 0) ShowPage(_currentPage - 1);
    }

    private void NextPage()
    {
        if (_currentPage < _pageCount - 1) ShowPage(_currentPage + 1);
    }
    
    private void UpdateUI()
    {
        _lblPage.Text = string.Format("{0} / {1}", _currentPage + 1, _pageCount);
        _btnNext.Enabled = _currentPage < _pageCount - 1;
        _btnPrev.Enabled = _currentPage > 0;
    }

    private void DoScroll(int dx, int dy)
    {
        Point current = _canvasPanel.AutoScrollPosition;
        int currentY = Math.Abs(current.Y);
        int currentX = Math.Abs(current.X);
        
        int maxY = Math.Max(0, _canvas.Height - _canvasPanel.ClientSize.Height);
        
        int y = currentY + dy;
        int x = currentX + dx;
        
        if (dy > 0 && currentY >= maxY)
        {
             if (_currentPage < _pageCount - 1)
             {
                  NextPage();
                  _canvasPanel.AutoScrollPosition = new Point(x, 0);
             }
             return;
        }
        else if (dy < 0 && currentY <= 0)
        {
             if (_currentPage > 0)
             {
                  PrevPage();
                  _canvasPanel.AutoScrollPosition = new Point(x, short.MaxValue);
             }
             return;
        }

        _canvasPanel.AutoScrollPosition = new Point(x, y);
    }

    private int GetCharIndexAt(int x, int y)
    {
        if (_currentTextPage == IntPtr.Zero || _currentPage < 0) return -1;
        
        // Need current page handle to convert coords
        // Calling LoadPage again is transient and okay for simple check or we can cache parameters
        // Better to recover the scale/size params from the current view state
        // We know the canvas image size.
        if (_canvas.Image == null) return -1;
        
        // We use _currentPdfPage now
        if (_currentPdfPage == IntPtr.Zero) return -1;

        try
        {
            int width = _canvas.Image.Width;
            int height = _canvas.Image.Height;
            double pageX, pageY;
            PdfNative.FPDF_DeviceToPage(_currentPdfPage, 0, 0, width, height, 0, x, y, out pageX, out pageY);
            
            // Tolerance? 10 pts
            return PdfNative.FPDFText_GetCharIndexAtPos(_currentTextPage, pageX, pageY, 10, 10);
        }
        catch { return -1; }
    }

    private void OnCanvasPaint(object sender, PaintEventArgs e)
    {
        if (_currentTextPage == IntPtr.Zero || _selStartIndex < 0 || _selEndIndex < 0) return;
        
        int start = Math.Min(_selStartIndex, _selEndIndex);
        int end = Math.Max(_selStartIndex, _selEndIndex);
        int count = end - start + 1;
        
        if (count <= 0) return;

        if (_currentPdfPage == IntPtr.Zero) return;

        try
        {
            int width = _canvas.Image.Width;
            int height = _canvas.Image.Height;

            int rectCount = PdfNative.FPDFText_CountRects(_currentTextPage, start, count);
            using (Brush b = new SolidBrush(Color.FromArgb(100, 0, 0, 255)))
            {
                for (int i = 0; i < rectCount; i++)
                {
                    double left, top, right, bottom;
                    if (!PdfNative.FPDFText_GetRect(_currentTextPage, i, out left, out top, out right, out bottom))
                        continue;
                    
                    int x1, y1, x2, y2;
                    PdfNative.FPDF_PageToDevice(_currentPdfPage, 0, 0, width, height, 0, left, top, out x1, out y1);
                    PdfNative.FPDF_PageToDevice(_currentPdfPage, 0, 0, width, height, 0, right, bottom, out x2, out y2);
                    
                    Rectangle r = Rectangle.FromLTRB(Math.Min(x1, x2), Math.Min(y1, y2), Math.Max(x1, x2), Math.Max(y1, y2));
                    e.Graphics.FillRectangle(b, r);
                }
            }
        }
        catch {}
    }

    private void CopySelection()
    {
        if (_currentTextPage == IntPtr.Zero || _selStartIndex < 0 || _selEndIndex < 0) return;
        
        int start = Math.Min(_selStartIndex, _selEndIndex);
        int end = Math.Max(_selStartIndex, _selEndIndex);
        int count = end - start + 1;
        
        // +2 for null terminator (unicode is 2 bytes)
        byte[] buffer = new byte[(count + 1) * 2];
        int written = PdfNative.FPDFText_GetText(_currentTextPage, start, count, buffer);
        if (written > 0)
        {
            string text = System.Text.Encoding.Unicode.GetString(buffer);
            // Remove null chars
            text = text.Replace("\0", "");
            if (!string.IsNullOrEmpty(text))
                 Clipboard.SetText(text);
        }
    }


    // Process arrow keys for navigation if the control has focus
    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == Keys.Right) 
        {
            NextPage();
            return true;
        }
        if (keyData == Keys.Left) 
        {
            PrevPage();
            return true;
        }
        if (keyData == Keys.Up)
        {
            DoScroll(0, -120);
            return true;
        }
        if (keyData == Keys.Down)
        {
            DoScroll(0, 120);
            return true;
        }
        if (msg.Msg == 0x20a) // WM_MOUSEWHEEL
        {
             if ((Control.ModifierKeys & Keys.Control) == Keys.Control)
             {
                 if (keyData == (Keys.Control | Keys.C))
                 {
                     CopySelection();
                     return true;
                 }
                long wParam = msg.WParam.ToInt64(); // 64-bit safe
                int delta = (int)(wParam >> 16);
                if (delta > 0) _btnZoomIn.PerformClick();
                else _btnZoomOut.PerformClick();
                return true;
             }
        }
        return base.ProcessCmdKey(ref msg, keyData);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (_resizeTimer != null)
            {
                _resizeTimer.Dispose();
                _resizeTimer = null;
            }
            CloseFile(); // Essential to release FPDF document
        }
        base.Dispose(disposing);
    }
}

// --- Main Form Window ---
public class MinimalPdfReader : Form
{
    private class MainTabControl : TabControl
    {
        private bool _isDarkMode = false;
        public bool IsDarkMode
        {
            get { return _isDarkMode; }
            set { _isDarkMode = value; Invalidate(); }
        }

        public MainTabControl()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Color bgColor = _isDarkMode ? Color.FromArgb(45, 45, 48) : SystemColors.Control;
            e.Graphics.Clear(bgColor);

            for (int i = 0; i < TabCount; i++)
            {
                var rect = GetTabRect(i);
                
                Color tabBg = _isDarkMode ? Color.FromArgb(45, 45, 48) : Color.White;
                Color textCol = _isDarkMode ? Color.LightGray : Color.Black;
                Color selBg = _isDarkMode ? Color.FromArgb(60, 60, 60) : SystemColors.ControlLight;

                if (SelectedIndex == i)
                {
                    tabBg = selBg;
                    textCol = _isDarkMode ? Color.White : Color.Black;
                }

                using (Brush b = new SolidBrush(tabBg))
                    e.Graphics.FillRectangle(b, rect);

                TextRenderer.DrawText(e.Graphics, TabPages[i].Text, this.Font, rect, textCol, TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter | TextFormatFlags.EndEllipsis);
            }
        }

        protected override void OnPaintBackground(PaintEventArgs pevent) { }
    }

    private MainTabControl _tabs;
    private bool _isDarkMode = false;
    private MenuStrip _menu;

    public MinimalPdfReader()
    {
        this.Text = "PDF Reader";
        this.Size = new Size(1000, 700);
        this.AutoScaleMode = AutoScaleMode.Dpi;
        this.AllowDrop = true;

        try { this.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch {}

        // Init PDFLib
        try { PdfNative.FPDF_InitLibrary(); } catch { MessageBox.Show("Could not load pdfium.dll"); }

        InitializeComponents();
        InitializeEvents();
    }

    private void InitializeComponents()
    {
        // Menu Strip
        MenuStrip menu = new MenuStrip();
        ToolStripMenuItem fileMenu = new ToolStripMenuItem("File");
        ToolStripMenuItem openItem = new ToolStripMenuItem("Open...", null, (s, e) => OpenPdfDialog());
        ToolStripMenuItem darkItem = new ToolStripMenuItem("Dark Mode", null, (s, e) => ToggleDarkMode());
        ToolStripMenuItem exitItem = new ToolStripMenuItem("Exit", null, (s, e) => this.Close());
        
        fileMenu.DropDownItems.Add(openItem);
        fileMenu.DropDownItems.Add(new ToolStripSeparator());
        fileMenu.DropDownItems.Add(darkItem);
        fileMenu.DropDownItems.Add(new ToolStripSeparator());
        fileMenu.DropDownItems.Add(exitItem);
        menu.Items.Add(fileMenu);

        ToolStripMenuItem windowMenu = new ToolStripMenuItem("Window");
        windowMenu.DropDownOpening += (s, e) => {
            windowMenu.DropDownItems.Clear();
            foreach (TabPage p in _tabs.TabPages)
            {
                var item = new ToolStripMenuItem(p.Text, null, (sender, arg) => _tabs.SelectedTab = p);
                 item.Checked = (_tabs.SelectedTab == p);
                 windowMenu.DropDownItems.Add(item);
            }
        };
        menu.Items.Add(windowMenu);

        _menu = menu;
        
        this.MainMenuStrip = menu;
        this.Controls.Add(menu);
        
        // Tab Control
        _tabs = new MainTabControl { 
            Dock = DockStyle.Fill, 
            SizeMode = TabSizeMode.Fixed, 
            ItemSize = new Size(150, 35),
            Font = new Font(Control.DefaultFont.FontFamily, Control.DefaultFont.Size * 1.5f)
        };
        _tabs.SelectedIndexChanged += (s, e) => UpdateTitle();
        this.Controls.Add(_tabs);
        _tabs.BringToFront(); // Ensure tabs are below menu but visible
    }

    private void InitializeEvents()
    {
        this.DragEnter += (s, e) => {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy;
        };

        this.DragDrop += (s, e) => {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files != null)
            {
                foreach (var f in files)
                {
                    if (f.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                    {
                        AddTab(f);
                    }
                }
            }
        };
    }

    private void OpenPdfDialog()
    {
        using (OpenFileDialog ofd = new OpenFileDialog { Filter = "PDF Files|*.pdf", Multiselect = true })
        {
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                foreach (var f in ofd.FileNames)
                {
                    AddTab(f);
                }
            }
        }
    }

    private void AddTab(string filePath)
    {
        TabPage page = new TabPage(Path.GetFileName(filePath));
        page.Padding = new Padding(0);
        
        PdfViewer viewer = new PdfViewer();
        if (_isDarkMode) viewer.IsDarkMode = true;
        
        page.Controls.Add(viewer); // viewer is Dock.Fill by default constructor
        
        _tabs.TabPages.Add(page);
        _tabs.SelectedTab = page;
        UpdateTitle();

        // Load content
        viewer.LoadFailed += (s, e) => {
             // Close this tab
             _tabs.TabPages.Remove(page);
             viewer.Dispose();
        };

        viewer.LoadFile(filePath);
        if (_tabs.TabPages.Contains(page)) // Only focus if not closed
            viewer.Focus();
    }

    private void ToggleDarkMode()
    {
        _isDarkMode = !_isDarkMode;
        ApplyTheme();
    }

    private void ApplyTheme()
    {
        if (_isDarkMode)
        {
            this.BackColor = Color.FromArgb(30, 30, 30);
            _tabs.BackColor = Color.FromArgb(30, 30, 30);
            _menu.BackColor = Color.FromArgb(45, 45, 48);
            _menu.ForeColor = Color.White;
        }
        else
        {
             this.BackColor = SystemColors.Control;
             _tabs.BackColor = SystemColors.Control;
             _menu.BackColor = SystemColors.MenuBar;
             _menu.ForeColor = SystemColors.MenuText;
        }

        _tabs.IsDarkMode = _isDarkMode;

        foreach (TabPage p in _tabs.TabPages)
        {
            p.BackColor = _isDarkMode ? Color.FromArgb(30, 30, 30) : Color.White;
            foreach (Control c in p.Controls)
            {
                PdfViewer v = c as PdfViewer;
                if (v != null) v.IsDarkMode = _isDarkMode;
            }
        }
        _tabs.Invalidate(); // Redraw tabs
    }
    
    private void UpdateTitle()
    {
        string title = "PDF Reader";
        if (_tabs.SelectedTab != null)
        {
            title += " - " + _tabs.SelectedTab.Text;
        }
        this.Text = title;
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        // Dispose all viewers
        foreach (TabPage p in _tabs.TabPages)
        {
            foreach (Control c in p.Controls)
            {
                PdfViewer v = c as PdfViewer;
                if (v != null) v.Dispose();
            }
        }

        PdfNative.FPDF_DestroyLibrary();
        base.OnFormClosed(e);
    }

    [STAThread]
    static void Main()
    {
        if (Environment.OSVersion.Version.Major >= 6) PdfNative.SetProcessDPIAware();
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new MinimalPdfReader());
    }
}
