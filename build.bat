@echo off
echo Compiling C# PDF Reader...
"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /unsafe /target:winexe /win32icon:assets\app_icon.ico /out:bin\MinimalPdfReader.exe src\MinimalPdfReader.cs
if %ERRORLEVEL% equ 0 (
    echo Compilation Successful!
) else (
    echo Compilation Failed.
)
