@echo off
echo Building standalone PDF Reader (pdfium.dll embedded)...
echo.

REM Check that pdfium.dll exists
if not exist "bin\pdfium.dll" (
    echo  ERROR: bin\pdfium.dll not found.
    echo  Download pdfium.dll and place it in the bin\ folder first.
    exit /b 1
)

"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe" ^
    /unsafe /target:winexe ^
    /win32icon:assets\app_icon.ico ^
    /res:bin\pdfium.dll,pdfium.dll ^
    /out:bin\MinimalPdfReader_Standalone.exe ^
    src\MinimalPdfReader.cs

if %ERRORLEVEL% equ 0 (
    echo.
    echo  Standalone build successful!
    echo  Output: bin\MinimalPdfReader_Standalone.exe
    echo  The exe is fully self-contained — no pdfium.dll needed alongside it.
    echo  pdfium.dll is extracted to %%TEMP%%\PdfReader_libs\ on first run.
) else (
    echo  Build failed.
)

pause