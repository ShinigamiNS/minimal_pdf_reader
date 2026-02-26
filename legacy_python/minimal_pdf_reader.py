import sys
import tkinter as tk
from tkinter import filedialog, messagebox
import pypdfium2 as pdfium
from PIL import Image, ImageTk

class MinimalPDFReader:
    def __init__(self, root):
        self.root = root
        self.root.title("Minimal PDF Reader")
        self.root.geometry("800x600")

        # Data
        self.doc = None
        self.current_page_num = 0
        self.total_pages = 0
        self.pil_image = None # Keep reference
        self.tk_image = None  # Keep reference to prevent GC

        # UI Setup
        self._setup_ui()
        
        # Bindings
        self.root.bind("<Left>", self.prev_page)
        self.root.bind("<Right>", self.next_page)
        self.root.bind("<Up>", self.scroll_up)
        self.root.bind("<Down>", self.scroll_down)
        self.root.bind("<Control-o>", lambda e: self.open_pdf())
        
        # Window resize event for re-rendering?
        # For simplicity and performance, we won't re-render on resize dynamically.
        # Maybe add a "Refresh" button or re-render when idle.
        # But for now, just render on page load.

    def _setup_ui(self):
        # Toolbar
        toolbar = tk.Frame(self.root, bd=1, relief=tk.RAISED)
        toolbar.pack(side=tk.TOP, fill=tk.X)

        btn_open = tk.Button(toolbar, text="Open PDF", command=self.open_pdf)
        btn_open.pack(side=tk.LEFT, padx=5, pady=5)

        self.btn_prev = tk.Button(toolbar, text="< Prev", command=self.prev_page, state=tk.DISABLED)
        self.btn_prev.pack(side=tk.LEFT, padx=5)

        self.lbl_page = tk.Label(toolbar, text="Page: 0 / 0")
        self.lbl_page.pack(side=tk.LEFT, padx=10)

        self.btn_next = tk.Button(toolbar, text="Next >", command=self.next_page, state=tk.DISABLED)
        self.btn_next.pack(side=tk.LEFT, padx=5)

        # Main Content Area
        self.canvas_frame = tk.Frame(self.root)
        self.canvas_frame.pack(fill=tk.BOTH, expand=True)

        self.v_scroll = tk.Scrollbar(self.canvas_frame, orient=tk.VERTICAL)
        self.h_scroll = tk.Scrollbar(self.canvas_frame, orient=tk.HORIZONTAL)

        self.canvas = tk.Canvas(self.canvas_frame, bg="gray",
                                yscrollcommand=self.v_scroll.set,
                                xscrollcommand=self.h_scroll.set)
        
        self.v_scroll.config(command=self.canvas.yview)
        self.h_scroll.config(command=self.canvas.xview)

        self.v_scroll.pack(side=tk.RIGHT, fill=tk.Y)
        self.h_scroll.pack(side=tk.BOTTOM, fill=tk.X)
        self.canvas.pack(side=tk.LEFT, fill=tk.BOTH, expand=True)

        # Bind MouseWheel for Windows
        self.canvas.bind_all("<MouseWheel>", self._on_mousewheel)

    def _on_mousewheel(self, event):
        # Determine scroll direction and amount
        # Windows event.delta is usually 120 or -120
        self.canvas.yview_scroll(int(-1*(event.delta/120)), "units")

    def open_pdf(self):
        file_path = filedialog.askopenfilename(filetypes=[("PDF Files", "*.pdf")])
        if not file_path:
            return

        try:
            # Load doc
            # pypdfium2 usage:
            try:
                self.doc = pdfium.PdfDocument(file_path)
            except Exception:
                 # Fallback for complex paths or permissions? Usually PdfDocument handles paths fine.
                 # Let's just catch and re-raise to show error
                 raise
            
            self.total_pages = len(self.doc)
            self.current_page_num = 0
            
            self.show_page(0)
            self.update_ui_state()
            self.root.title(f"Minimal PDF Reader - {file_path}")
        except Exception as e:
            messagebox.showerror("Error", f"Could not open PDF: {e}")

    def show_page(self, page_num):
        if not self.doc:
            return
        
        if page_num < 0 or page_num >= self.total_pages:
            return

        self.current_page_num = page_num
        
        # Get page
        page = self.doc[page_num]
        
        # Calculate scale to fit width
        canvas_width = self.canvas.winfo_width()
        if canvas_width < 100: canvas_width = 800
        
        # Page size in points (1/72 inch)
        width, height = page.get_size()
        
        # Scale factor
        # Subtract some padding for scrollbar
        target_width = canvas_width - 25
        # If target width is very small (window minimized), keep reasonable scale
        if target_width < 100: target_width = 100

        scale = target_width / width if width > 0 else 1.0
        
        # Render
        # pypdfium2 renders to a bitmap, then we convert to PIL
        # render() returns a PdfBitmap. We need to close it if we were doing manual memory mgmt, 
        # but pypdfium2 handles it.
        bitmap = page.render(scale=scale) 
        self.pil_image = bitmap.to_pil()  # Convert to PIL Image
        
        # Convert to ImageTk
        self.tk_image = ImageTk.PhotoImage(self.pil_image)
        
        # Update Canvas
        self.canvas.delete("all")
        self.canvas.config(scrollregion=(0, 0, self.tk_image.width(), self.tk_image.height()))
        self.canvas.create_image(0, 0, image=self.tk_image, anchor=tk.NW)
        
        # Update Canvas Frame bindings if needed, but bind_all works globally
        
        self.lbl_page.config(text=f"Page: {self.current_page_num + 1} / {self.total_pages}")
        self.update_ui_state()

        # Reset scroll? Or keep relative? Usually reset for new page.
        self.canvas.yview_moveto(0)
        self.canvas.xview_moveto(0)

    def prev_page(self, event=None):
        if self.doc and self.current_page_num > 0:
            self.show_page(self.current_page_num - 1)

    def next_page(self, event=None):
        if self.doc and self.current_page_num < self.total_pages - 1:
            self.show_page(self.current_page_num + 1)

    def scroll_up(self, event=None):
        self.canvas.yview_scroll(-1, "units")

    def scroll_down(self, event=None):
        self.canvas.yview_scroll(1, "units")

    def update_ui_state(self):
        if not self.doc:
            self.btn_prev.config(state=tk.DISABLED)
            self.btn_next.config(state=tk.DISABLED)
        else:
            self.btn_prev.config(state=tk.NORMAL if self.current_page_num > 0 else tk.DISABLED)
            self.btn_next.config(state=tk.NORMAL if self.current_page_num < self.total_pages - 1 else tk.DISABLED)

if __name__ == "__main__":
    root = tk.Tk()
    app = MinimalPDFReader(root)
    root.mainloop()
