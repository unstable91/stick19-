# stick19 for Windows 11

**A lightweight, customizable desktop sticky note that behaves like a native Windows widget.**

stick19 keeps your notes attached to the desktop layer rather than floating over your applications. Open Chrome, VS Code, File Explorer, or any other window, and the note stays behind it. Press `Win + D` to show your desktop and access your note.

## Features

- **Desktop Integration:** Stays behind normal application windows without remaining always on top.
- **Move & Resize:** Position the note anywhere across multiple monitors and resize it from any edge or corner.
- **Automatic Saving:** Saves note content, position, dimensions, theme, and settings locally, then restores them on the next launch.
- **System Tray:** Runs in the background without a taskbar button. Use the tray icon to show or hide the note, change settings, or exit.
- **Customization:** Choose from six color themes, adjust font size and transparency, enable a glass effect, and format text using bold, italic, and underline.
- **Convenient Controls:** Lock the note's position, adjust settings from the header, and use keyboard shortcuts for formatting and saving.
- **Windows Startup:** Optionally launch stick19 automatically when Windows starts.
- **Reliable Desktop Behavior:** Handles Explorer restarts, display changes, and monitor disconnections.
- **Lightweight:** Uses event-driven Win32 messaging, requires no third-party dependencies, and has minimal idle resource usage.

## How to Run

**You can use stick19 directly—no compilation or installation process is needed.**

1. Download or clone this repository.
2. Open the `bin/Release/` directory.
3. Double-click `stick19.exe`.

The application runs in the background, and the note appears on your desktop. Access its controls from the note header or the system tray near the clock.

> If the compiled executable is not included in your downloaded repository, build it first using `build.bat` or `build.ps1`. The project targets .NET Framework 4.8, which is included with Windows 10 and Windows 11.

## Build from Source (Optional)

Developers can build the application from the project directory using either command:

```cmd
build.bat
```

Or in PowerShell:

```powershell
.\build.ps1
```

The executable is generated at `bin\Release\stick19.exe`.

## Data & Privacy

stick19 stores note content and settings locally in:

```text
%LOCALAPPDATA%\stick19\note_data.json
```

Your notes are saved on your computer; the application does not require a cloud account or an external service.

## Technology

Built with C#, WPF, .NET Framework 4.8, and native Win32 APIs. It uses Windows desktop-shell integration to keep the note behind regular application windows while preserving text input and interaction.

**Platform:** Windows 10 and Windows 11 (64-bit)

**Project:** stick19 — a simple, persistent desktop note designed to stay where you need it.
