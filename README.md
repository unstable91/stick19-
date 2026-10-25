# stick19 for Windows 11


A high-performance, lightweight desktop sticky note application designed specifically for **Windows 11** (and Windows 10) that behaves like a native desktop widget attached directly to the Windows desktop shell layer, rather than a floating application window.

---

## Key Highlights

- **Desktop-Only Visibility:** Permanently anchored to the Windows desktop surface. Normal applications (Google Chrome, VS Code, File Explorer, Terminal, etc.) always render in front of the note. The note never floats over other apps.
- **Single Persistent Note:** One fixed note that can be moved anywhere across single or multiple monitors and resized from any of its 8 edges or corners.
- **System Tray Integration:** Runs silently in the background with **zero taskbar clutter** (`WS_EX_TOOLWINDOW`). Provides a system tray icon with options to show/hide, switch themes, change font size, lock position, change attachment modes, toggle Windows startup, and exit.
- **Persistent Storage:** Local JSON storage in `%LOCALAPPDATA%\stick19\note_data.json`. Automatically saves text (debounced), position, dimensions, color theme, and settings. Restores previous state on app launch or Windows restart. Uses atomic file writes to prevent data corruption.
- **Windows Startup Integration:** Toggle auto-start on Windows boot via the tray icon menu.
- **Low Resource Usage:** 
  - **CPU:** 0.0% when idle (event-driven Win32 messages; no background polling threads).
  - **Memory:** ~20–30 MB working set, trimmed on deactivation via `SetProcessWorkingSetSize`.
  - **Binary Size:** Standalone ~62 KB executable with **zero third-party dependencies**.
- **Explorer Restart & Multi-Monitor Resilient:** Listens for `TaskbarCreated` to re-anchor and restore tray icons after Explorer restarts. Handles `WM_DISPLAYCHANGE` to keep the note on-screen if monitors are unplugged.

---

## Technical Architecture & Desktop Shell Integration

### The Desktop Layer in Windows 11

In Windows 11, the desktop shell is maintained by `explorer.exe`:
- **`Progman` (`Program Manager`):** The root desktop window hosting `SHELLDLL_DefView` (the desktop folder view with desktop icons).
- **`WorkerW`:** Helper windows spawned by Explorer when wallpaper animations, desktop composition, or the message `0x052C` are dispatched.

```
+-------------------------------------------------------------+
| Top-Most Windows (Picture-in-Picture, Always-On-Top tools) |
+-------------------------------------------------------------+
| Normal Windows (Chrome, VS Code, Word, File Explorer)       |
+-------------------------------------------------------------+
| [>>> stick19 <<<] (Desktop Layer)                           |
+-------------------------------------------------------------+
| Desktop Icons (SHELLDLL_DefView / SysListView32)            |
+-------------------------------------------------------------+
| Wallpaper & Shell Background (WorkerW / Progman)             |
+-------------------------------------------------------------+
```

### Windows 11 Limitations with Direct `WorkerW` Child Parenting (`SetParent`)

While traditional tools often call `SetParent(hwnd, workerW)`, on Windows 10 and especially **Windows 11**, direct child parenting introduces critical drawbacks:
1. **Keyboard Focus & Input Stealing:** Cross-process child windows of Explorer cannot reliably maintain keyboard focus. Clicking into a child `TextBox` often causes Explorer or the desktop to reclaim focus, dropping keystrokes or breaking the caret.
2. **Multi-Monitor Clipping:** In Windows 11, `Progman` and its child `WorkerW` windows often only span the primary display's resolution. Any child window positioned with negative coordinates (e.g. on a left-hand secondary monitor) is clipped and disappears.
3. **Explorer Crash Lifecycle:** When `explorer.exe` restarts or crashes, child windows of `Progman` or `WorkerW` are destroyed by the Windows kernel window hierarchy teardown.

### The Solution: Dual-Mode Architecture with Robust Fallback

`stick19` provides a hybrid architecture with two selectable attachment modes:

1. **`DesktopBottomLayer` (Recommended & Default for Windows 11):**
   - **Progman Ownership:** The window's owner is assigned to `Progman` via `SetWindowLongPtr(hwnd, GWL_HWNDPARENT, hProgman)`. This informs the Windows Desktop Window Manager (DWM) that the note belongs to the desktop window group.
   - **`Win+D` ("Show Desktop") Preservation:** Because it is owned by `Progman` and has tool window attributes, pressing `Win+D` or `Win+M` minimizes all normal application windows while keeping the sticky note visible on the desktop.
   - **`HWND_BOTTOM` Z-Order:** The note sits at `HWND_BOTTOM`. When another window activates or opens, the note remains below it.
   - **Seamless Input:** When clicked, the note activates natively for typing, selection, undo/redo (`Ctrl+Z`), and emojis (`Win+.`). When the user clicks away, `WM_ACTIVATE (WA_INACTIVE)` immediately returns the note to `HWND_BOTTOM`.
   - **Full Multi-Monitor Support:** Can be positioned anywhere on any monitor across virtual screen coordinates without clipping.

2. **`WorkerWChild` (Alternative Shell Attachment Mode):**
   - Sends `0x052C` to `Progman` using `SendMessageTimeout` to force the creation of the wallpaper `WorkerW`.
   - Locates the `WorkerW` window and attaches via `SetParent(hwnd, workerW)`.
   - **Automatic Fallback:** If `WorkerW` is not found or fails on Windows 11, it automatically falls back to `DesktopBottomLayer`.

Both modes can be switched on the fly from the system tray menu (`Desktop Layer Mode`).

---

## Explorer Restart & Multi-Monitor Handling

1. **Explorer Crashes or Restarts:**
   - The application registers for the system broadcast message `TaskbarCreated` via `RegisterWindowMessage("TaskbarCreated")`.
   - When Explorer restarts, the note intercepts this message, re-queries `Progman`, re-establishes the desktop layer attachment, and recreates the system tray icon so it never disappears.
2. **Monitor Disconnect / Resolution Changes:**
   - Intercepts `WM_DISPLAYCHANGE`.
   - `DesktopManager.EnsureOnScreen()` checks the note's coordinates against `SystemParameters.VirtualScreen`.
   - If a disconnected monitor leaves the note off-screen, it automatically centers on the primary display.

---

## UI Features & Controls

| Feature | Description |
|---|---|
| **Draggable Header** | Left-click and drag the top bar to move the note anywhere across displays. |
| **8-Point Resizing** | Grab any border edge (Left, Right, Top, Bottom) or corner (TopLeft, TopRight, BottomLeft, BottomRight) to resize. |
| **Settings Gear Icon (⚙)** | Located beside the Lock button on the header. Opens a comprehensive popup menu containing all configuration options identical to the system tray, styled dynamically to match the user's active theme. |
| **Theme-Matching Settings Menu** | Full configuration at your fingertips: switch themes, font sizes, transparency (40% to 100%), glossy sheen, auto-save, lock, desktop layer mode, Windows startup, and centering. Menu background, borders, highlights, and text automatically adapt to whatever color theme is selected. |
| **Controllable Transparency & Glossy Effect** | Choose from presets (100% Solid down to 40% Ultra Glass) and toggle a polished glass reflection sheen overlay (`LinearGradientBrush`) for an authentic Windows Fluent glass look. |
| **Rich Text Formatting (B, I, U)** | Interactive **Bold** (`B`), *Italic* (`I`), and <u>Underline</u> (`U`) toggle buttons in the lower footer bar. Supports text selection and standard keyboard shortcuts (`Ctrl+B`, `Ctrl+I`, `Ctrl+U`). Buttons do not steal keyboard focus. |
| **Sleek Minimalist Scrollbar** | Replaced default blocky white Aero scrollbars with an ultra-thin 6px transparent track and theme-matching pill thumb (eliminates the jarring white block on dark backgrounds). |
| **No Cut/Copy/Paste Context Menu** | Right-clicking inside the note text area suppresses the default context menu. Standard keyboard shortcuts (`Ctrl+X`, `Ctrl+C`, `Ctrl+V`, `Ctrl+A`) remain fully functional. |
| **Configurable Auto-Save Option** | Toggle Auto-Save from the settings menu or system tray. When enabled, text saves automatically (debounced). When disabled, an interactive "Save" button and "Unsaved" indicator appear in the footer. |
| **Footer Status: Saved / Unsaved** | Replaces distracting counters with a clean, minimalist status showing only **Saved** or **Unsaved**. |
| **Lock Note (Button, Menu & Tray)** | Pins the note in place to prevent accidental dragging/resizing. Triggered cleanly via the header padlock icon, Settings menu, or system tray menu (the `Ctrl+L` keyboard shortcut was removed). |
| **Color Themes** | 6 curated Windows 11 Fluent palettes: **Classic Yellow**, **Soft Mint**, **Sky Blue**, **Lavender**, **Warm Peach**, **Charcoal Dark**. Scroll thumbs and Settings menus dynamically match each palette. |
| **Keyboard Shortcuts** | `Ctrl+B` (Bold), `Ctrl+I` (Italic), `Ctrl+U` (Underline), `Ctrl+S` (Save Now), `Ctrl++` / `Ctrl+-` (Adjust Font Size), `Escape` (Drop focus to desktop). |
| **Hide to Tray** | Click the header close button (`✕`) or select "Hide Note" from the tray or settings menu to tuck it away. |

---

## Project Structure

```
c:\Users\ununs\Documents\stick\
├── DesktopStickyNote.sln         # Visual Studio Solution file
├── build.bat                     # One-click Windows build script (Batch)
├── build.ps1                     # PowerShell build script
├── README.md                     # Documentation
├── src\
│   ├── DesktopStickyNote.csproj  # Main project file (.NET Framework 4.8 / WPF)
│   ├── App.xaml                  # Application entry definition
│   ├── App.xaml.cs               # Single-instance Mutex & session lifecycle
│   ├── MainWindow.xaml           # Fluent sticky note UI layout
│   ├── MainWindow.xaml.cs        # UI interaction, shortcuts, resizing logic
│   ├── Interop\
│   │   └── NativeMethods.cs      # Win32 APIs, P/Invoke, constants, and structs
│   ├── Core\
│   │   ├── DesktopManager.cs     # Progman/WorkerW attachment & Z-order enforcer
│   │   ├── TrayIconManager.cs    # System tray icon, context menu, and balloon tips
│   │   ├── StorageManager.cs     # Atomic, debounced JSON persistence
│   │   ├── NoteSettings.cs       # Settings model & defaults
│   │   └── StartupManager.cs     # Windows Run registry integration
│   ├── Controls\
│   │   └── ColorThemes.cs        # Fluent color palettes & brushes
│   ├── Properties\
│   │   └── AssemblyInfo.cs       # Application metadata & author branding
│   └── Resources\
│       └── app.ico               # High-DPI 256x256 application icon
├── tests\
│   ├── DesktopStickyNote.Tests.csproj  # Test suite project
│   └── TestRunner.cs                   # Automated verification tests
└── bin\
    ├── Release\
    │   └── stick19.exe           # Compiled standalone release binary (~62 KB)
    └── Tests\
        └── DesktopStickyNote.Tests.exe # Test suite executable
```

---

## Build Instructions

### Prerequisites
- **Operating System:** Windows 10 or Windows 11 (64-bit).
- **Runtime:** .NET Framework 4.8 (pre-installed natively on Windows 10 and Windows 11; no runtime downloads required).
- **Compiler:** `MSBuild.exe` (included out of the box with Windows at `C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe`, or Visual Studio 2019 / 2022).

### Compiling via Batch File
Open Command Prompt or PowerShell in the project directory and run:
```cmd
build.bat
```

### Compiling via PowerShell
```powershell
.\build.ps1
```

The output executable will be placed in:
```
bin\Release\stick19.exe
```

---

## Running the Automated Tests

To run the built-in automated test suite:
```cmd
bin\Tests\DesktopStickyNote.Tests.exe
```

Test coverage:
- Predefined theme palette completeness and fallback handling.
- JSON serialization/deserialization roundtrip with complex multiline strings, quotes, escapes, and Unicode symbols.
- Registry startup reading/writing resilience.
- Coordinate persistence and boundary validation.

---

## Acceptance Test Verification

Follow these steps to verify all core requirements:

1. **Desktop-Only Visibility:**
   - Launch `bin\Release\stick19.exe`.
   - The note appears on your desktop.
   - Open **Google Chrome**, **VS Code**, or **File Explorer** and move that window over the note.
   - *Result:* The application window completely covers the note. The note never floats over any active application.
2. **Interact & Edit:**
   - Click directly onto the note.
   - Type or edit text. Notice full support for text selection, caret, undo/redo (`Ctrl+Z`), and emojis (`Win+.`).
   - Click back onto another application.
   - *Result:* The note immediately settles back beneath other applications.
3. **Minimize All Windows (`Win+D`):**
   - Open multiple applications (e.g. Chrome, Notepad, File Explorer).
   - Press `Win+D` (Show Desktop).
   - *Result:* All regular application windows minimize, and the sticky note remains visible on your desktop beside your icons.
4. **Move and Resize:**
   - Drag the note's top header bar to reposition it anywhere on your screen or across secondary monitors.
   - Move your cursor to any of the 4 borders or 4 corners; drag to resize the note.
   - Click the header padlock button or tray menu item to lock the note; notice dragging/resizing is disabled to prevent accidental movement. Click again to unlock.
5. **System Tray Integration:**
   - Verify that there is **no taskbar button** on the Windows taskbar.
   - Locate the sticky note icon in the system tray (near the clock).
   - Left-click the tray icon to toggle Hide/Show.
   - Right-click the tray icon to change color themes, adjust font size, center the note, or toggle "Launch at Windows Startup".
6. **Persistence & Restart:**
   - Type some test notes into the note and reposition it.
   - Right-click the tray icon and select "Exit", or restart Windows.
   - Relaunch the application.
   - *Result:* The note opens at the exact same screen coordinates, dimensions, color theme, font size, and text content.
