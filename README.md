# Minimal PDF Reader

A lightweight, fast, and feature-rich standalone PDF reader application built entirely in C# (Windows Forms) using the high-performance `pdfium` rendering engine.

## Features

- **Blazing Fast Performance:** Powered by the native PDFium library, rendering pages instantaneously and managing resources efficiently through robust memory recovery techniques.
- **Dark Mode Support:** Enjoy a stylish, easy-to-read dark UI with correctly preserved document and image colors—everything outside the document gets inverted to dark mode gracefully.
- **Smart Zooming:** Zoom in and out of your PDF accurately. Resizing the application or maximizing it automatically centers your document to optimally fit the window without losing your view tracking.
- **Continuous Navigation:** Seamlessly browse documents using the scroll wheel. Hit the absolute bottom or top of a page? Keep scrolling to automatically jump precisely to the next or previous page.
- **Arrow Key Support:** Navigate quickly and manually using the `Up` and `Down` arrow keys to scroll, and the `Left` and `Right` arrow keys to jump between pages.
- **Password Protection:** Full support for opening encrypted and password-protected PDFs. You are safely given 3 attempts to input the correct password before the file tab aborts.
- **Tabbed Interface:** Easily juggle multiple different PDFs using sleek top navigation tabs. 

## Folder Structure

The repository has been neatly organized for source control and everyday development:

- `src/` - Contains the core application logic (`MinimalPdfReader.cs`).
- `assets/` - Contains the application resources like your generated sleek Red Generic PDF logo icon (`app_icon.ico`) and the script to recreate it.
- `bin/` - The destination directory where the application is built securely. Your required `.dll`'s such as `pdfium.dll` also live here.
- `tests/` - A place to stash experimental or debug files (such as `test_tabs.cs`).
- `legacy_python/` - Contains the older iteration of the PDF reader written in Python using `pypdfium` libraries, safely archived.

## Building the Application

If you make modifications to `src/MinimalPdfReader.cs`, you can easily re-compile the C# application into an executable using the provided batch script. No massive Visual Studio installations required; it leverages the built-in Microsoft .NET Framework compiler.

Just run the following command in the root folder:
```cmd
build.bat
```
Upon success, `MinimalPdfReader.exe` will be safely written into the `bin/` folder wrapped up with its `.ico` icon logo.

## Running the Application

To launch your compiled PDF reader, you can simply run:
```cmd
run.bat
```
This will automatically target the executable living in your `bin/` directory and launch the app in a new window!
