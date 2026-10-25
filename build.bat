@echo off
setlocal enabledelayedexpansion

echo ========================================================
echo Building stick19 for Windows 11
echo Developed by unstable19
echo ========================================================

set "MSBUILD_PATH="

:: 1. Try VS Locator (vswhere) if installed
if exist "%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe" (
    for /f "usebackq tokens=*" %%i in (`"%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe" -latest -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe`) do (
        set "MSBUILD_PATH=%%i"
    )
)

:: 2. Fall back to native Windows .NET Framework 4.8 MSBuild
if not defined MSBUILD_PATH (
    if exist "%SystemRoot%\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe" (
        set "MSBUILD_PATH=%SystemRoot%\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe"
    ) else if exist "%SystemRoot%\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe" (
        set "MSBUILD_PATH=%SystemRoot%\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe"
    )
)

if not defined MSBUILD_PATH (
    echo [ERROR] MSBuild was not found on this system.
    pause
    exit /b 1
)

echo Using MSBuild: "%MSBUILD_PATH%"
echo.

"%MSBUILD_PATH%" "%~dp0DesktopStickyNote.sln" /p:Configuration=Release /p:Platform=x64 /p:MSBuildWarningsAsMessages=MSB3644 /v:minimal /nologo

if %ERRORLEVEL% equ 0 (
    echo.
    echo ========================================================
    echo Build SUCCESSFUL!
    echo Binary: "%~dp0bin\Release\stick19.exe"
    echo ========================================================
) else (
    echo.
    echo [ERROR] Build failed with error code %ERRORLEVEL%.
    pause
    exit /b %ERRORLEVEL%
)
