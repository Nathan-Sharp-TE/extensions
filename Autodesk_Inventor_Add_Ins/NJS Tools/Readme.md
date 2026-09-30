# NJS Tools for Autodesk Inventor

**NJS Tools** is an Autodesk Inventor 2026 add-in written in C# for 64-bit .NET 8. The add-in adds an **NJS Tools** panel to the **Tools** tab in the Part, Assembly, Drawing, and Presentation environments.

## Quick Install (Single-Line Command)

1. Close Autodesk Inventor 2026.
2. Open 64-bit Windows Terminal, Command Prompt, or PowerShell.
3. Copy and execute the `curl.exe` command below:

```bat
curl.exe -fsSL "https://raw.githubusercontent.com/nathan-sharp/extensions/main/Autodesk_Inventor_Add_Ins/NJS%20Tools/Install-Addin.ps1" | powershell -NoProfile -ExecutionPolicy Bypass -Command -
```

Alternatively, in PowerShell, execute the `Invoke-RestMethod` (`irm`) command:

```powershell
irm "https://raw.githubusercontent.com/nathan-sharp/extensions/main/Autodesk_Inventor_Add_Ins/NJS%20Tools/Install-Addin.ps1" | iex
```

The installer downloads the latest release package (or builds from source via `dotnet build`), registers the Component Object Model (COM) server under `HKCU\Software\Classes`, writes the add-in manifest to `%APPDATA%\Autodesk\Inventor 2026\Addins`, and removes all temporary download files.

## Quick Uninstall (Single-Line Command)

1. Close Autodesk Inventor 2026.
2. Execute the `curl.exe` command below in 64-bit Command Prompt or PowerShell:

```bat
curl.exe -fsSL "https://raw.githubusercontent.com/nathan-sharp/extensions/main/Autodesk_Inventor_Add_Ins/NJS%20Tools/Uninstall-Addin.ps1" | powershell -NoProfile -ExecutionPolicy Bypass -Command -
```

Alternatively, in PowerShell, execute:

```powershell
irm "https://raw.githubusercontent.com/nathan-sharp/extensions/main/Autodesk_Inventor_Add_Ins/NJS%20Tools/Uninstall-Addin.ps1" | iex
```

## Features

| Command | Environments | Description |
| :--- | :--- | :--- |
| **Active Document Info** | Part, Assembly, Drawing, Presentation | Displays the active document name, document type, and file path. |
| **Generate BoM** | Assembly | Exports the structured Bill of Materials (BoM) into a copy of the configured Excel template (`BoM Template` sheet, `tblBOM` table) saved as `<AssemblyNumber>-BOM-Rev-<Revision>.xlsx`. |
| **Print Draft** | Drawing | Adds a temporary `DRAFT` watermark with username and Coordinated Universal Time (UTC) timestamp to each sheet, exports a Portable Document Format (PDF) file, and rolls back the watermark transaction. |
| **Digital Image** | Part, Assembly | Sets the camera to Top orientation, optionally applies a configured material appearance inside a rollback transaction, and exports a transparent-background Portable Network Graphics (PNG) image. |
| **Add-On Settings** | Part, Assembly, Drawing, Presentation | Configures the Excel BoM template path, BoM draft output folder, digital image output folder, and optional digital image appearance override. |

## Prerequisites

- 64-bit Windows 10 or Windows 11.
- 64-bit Autodesk Inventor Professional 2026.
- Microsoft Excel desktop application (required for template-based BoM generation).
- .NET 8 Software Development Kit (SDK) to build the project.
- .NET 8 Desktop Runtime (x64) to load the add-in inside Autodesk Inventor.
- 64-bit Windows PowerShell to run the installation and removal scripts.
- Autodesk Inventor 2026 Application Programming Interface (API) interop assembly at `C:\Program Files\Autodesk\Inventor 2026\Bin\Autodesk.Inventor.Interop.dll`.

## Build Procedure

1. Close Autodesk Inventor.
2. Open 64-bit PowerShell in the project folder containing `NJS.InventorAddIn.csproj`.
3. Run the Release build command:

   ```powershell
   dotnet build .\NJS.InventorAddIn.csproj -c Release
   ```

4. If Autodesk Inventor resides in a custom directory, specify `InventorBinPath`:

   ```powershell
   dotnet build .\NJS.InventorAddIn.csproj -c Release -p:InventorBinPath="D:\Autodesk\Inventor 2026\Bin"
   ```

## Installation Procedure (Per-User)

The installer copies the build output to `%LOCALAPPDATA%`, creates the add-in manifest in `%APPDATA%`, and registers the Component Object Model (COM) class under `HKCU\Software\Classes`. Administrator privileges are not required.

1. Complete the Release build procedure.
2. Close Autodesk Inventor.
3. Open 64-bit PowerShell in the project directory.
4. Allow script execution for the active PowerShell process if blocked:

   ```powershell
   Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass
   ```

5. Execute the installation script:

   ```powershell
   .\Install-Addin.ps1
   ```

6. Start Autodesk Inventor 2026 and open the **Tools** tab to access the **NJS Tools** panel.

### Installed File Locations

| Artifact | Path |
| :--- | :--- |
| Add-in binaries | `%LOCALAPPDATA%\NJSTools\InventorAddIn\<build-hash>` |
| Add-in manifest | `%APPDATA%\Autodesk\Inventor 2026\Addins\NJS.InventorAddIn.addin` |
| User settings | `%LOCALAPPDATA%\NJSTools\InventorAddIn\settings.json` |
| Activation log | `%LOCALAPPDATA%\NJSTools\InventorAddIn\activation.log` |
| COM registration | `HKCU\Software\Classes\CLSID\{D4E8B219-63C7-4A92-9B15-8E24F610C3A7}` |

## Removal Procedure

1. Close Autodesk Inventor.
2. Open 64-bit PowerShell in the project directory.
3. Execute the removal script:

   ```powershell
   .\Uninstall-Addin.ps1
   ```

## Project Files

- `NJS.InventorAddIn.csproj`: .NET 8 Windows x64 project configuration and Inventor interop references.
- `StandardAddInServer.cs`: COM-visible add-in server, ribbon UI definitions, and command handlers.
- `AddInSettings.cs`: JSON settings persistence and Windows Forms configuration dialog.
- `NJS.InventorAddIn.addin.template`: Inventor add-in manifest template.
- `Install-Addin.ps1`: Per-user installation and COM registration script.
- `Uninstall-Addin.ps1`: Per-user uninstallation and COM cleanup script.
