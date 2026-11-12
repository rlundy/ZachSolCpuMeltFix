@echo off
setlocal DisableDelayedExpansion
set "compiler=%SystemRoot%\Microsoft.NET\Framework\v4.0.30319\csc.exe"
if not exist "%compiler%" (
    echo The .NET Framework 4 C# compiler was not found.
    exit /b 1
)

"%compiler%" /nologo /optimize+ /target:winexe /main:SolitairePatcherApp ^
    /reference:System.Drawing.dll /reference:System.Windows.Forms.dll ^
    /out:"%~dp0SolitairePatcher.exe" "%~dp0PatchSolitaire.cs" "%~dp0SolitairePatcherApp.cs"
exit /b %ERRORLEVEL%
