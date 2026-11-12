@echo off
setlocal DisableDelayedExpansion
set "compiler=%SystemRoot%\Microsoft.NET\Framework\v4.0.30319\csc.exe"
if not exist "%compiler%" (
    echo The .NET Framework 4 C# compiler was not found.
    exit /b 1
)

if "%~1"=="" goto build_app
if /i "%~1"=="build" goto build_app

set "build_dir=%TEMP%\ZachSolitairePatch-%RANDOM%-%RANDOM%"
mkdir "%build_dir%" || exit /b 1
"%compiler%" /nologo /optimize+ /out:"%build_dir%\PatchSolitaire.exe" "%~dp0PatchSolitaire.cs"
set "result=%ERRORLEVEL%"
if not "%result%"=="0" goto cleanup

"%build_dir%\PatchSolitaire.exe" "%~dp0Zach\The Zachtronics Solitaire Collection.exe" %*
set "result=%ERRORLEVEL%"

:cleanup
if exist "%build_dir%\PatchSolitaire.exe" del "%build_dir%\PatchSolitaire.exe"
rmdir "%build_dir%"
exit /b %result%

:build_app
"%compiler%" /nologo /optimize+ /target:winexe /main:SolitairePatcherApp ^
    /reference:System.Drawing.dll /reference:System.Windows.Forms.dll ^
    /out:"%~dp0SolitairePatcher.exe" "%~dp0PatchSolitaire.cs" "%~dp0SolitairePatcherApp.cs"
if errorlevel 1 exit /b 1
if /i "%~1"=="build" exit /b 0
start "" "%~dp0SolitairePatcher.exe"
