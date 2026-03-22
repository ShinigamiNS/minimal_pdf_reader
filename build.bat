@echo off
echo Compiling PDF Reader (external pdfium.dll)...
"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe" ^
    /unsafe /target:winexe ^
    /win32icon:assets\app_icon.ico ^
    /out:bin\MinimalPdfReader.exe ^
    src\MinimalPdfReader.cs
if %ERRORLEVEL% equ 0 (
    echo.
    echo  Compilation successful!
    echo  Run:  bin\MinimalPdfReader.exe
    echo  NOTE: bin\pdfium.dll must remain next to the exe.
) else (
    echo  Compilation failed.
)
