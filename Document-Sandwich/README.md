# Document Sandwich

A Windows desktop application for combining PDF files, Word documents and images into one PDF. All processing happens locally.

## Features

- Three starting layers, with a **+** button to add more.
- Drag and drop files, or use **Browse**. Supported types: PDF, DOC/DOCX, PNG, JPG/JPEG, BMP, GIF and TIFF.
- Move whole layers with **up/down** arrows. Layers combine from top to bottom; files within a layer keep their displayed order.
- **Remove** removes the selected attachment, or the last attachment if none is selected. **Delete layer** removes the whole box. Neither action deletes source files.
- Enter an output name and click **Combine**, or press **Enter** in the name box. Blank names are blocked.
- **Clear** removes every attachment while keeping layers, the name and settings.
- Save As opens by default. **Settings** can select a remembered fixed save folder to skip the dialog. Replacing an existing PDF requires confirmation.

## Install and run

Run `Document-Sandwich-Setup-1.4.0.exe` from the release downloads. Setup requests administrator permission, installs into `C:\Program Files\Document Sandwich`, and creates desktop and Start menu shortcuts. Uninstall through Windows Settings > Apps.

The self-contained Windows x64 application includes .NET. Desktop Microsoft Word must be installed and activated to convert Word documents. PDF and image inputs do not need Word.

Upgrading PDF Sandwich keeps the existing installation directory, replaces the old executable and shortcuts, and reads existing save-folder preferences. New preferences are saved in `%LOCALAPPDATA%/Document Sandwich/settings.json`.

## Build

Requires Windows and the .NET 10 SDK:

```powershell
.\build.ps1
```

The portable application is written to `dist/Document Sandwich.exe`.

To build the installer, install Inno Setup 6, then run:

```powershell
.\build-installer.ps1
```

The installer and SHA256 checksum are written to `dist/`. Builds restore PDFsharp 6.2.4 and its dependencies from NuGet. Third-party notices are included with the installer. The original sandwich artwork can be regenerated with `assets/make-icon.ps1`.

## Checks

```powershell
dotnet run --project tests/UiSmoke/UiSmoke.csproj -c Release
```

Checks cover layer ordering, removal, dynamic layers, filename validation, initial layout, image formats, TIFF pages, mixed PDF/image order, and preserving an existing output on failure. Word conversion also has a separate `--self-test` mode requiring installed desktop Word; it is not part of the normal UI check.

## Image and PDF behaviour

Images are centred on A4 pages with a small margin and their proportions preserved. Landscape images use landscape pages. TIFF includes every page; GIF uses the first frame. JPEG EXIF orientation is applied.

Word documents are opened read-only with macros disabled. Password-protected or damaged files may need manual export first. Temporary conversion files are cleaned up after processing. The completed PDF is staged before replacing an existing output.

Combining copies pages into a new document. Original digital signatures are not retained as signatures on the combined PDF. Preservation of forms, bookmarks and document-level interactive features is not guaranteed.

## GitHub packaging

After building the installer, run `./export-github.ps1`. Upload the contents of `GitHub-Package/Document-Sandwich/` as repository source. Attach the installer and checksum from `GitHub-Package/Release-Assets/` to a GitHub Release. Local settings, build caches, test output and previous versions are excluded. The packaging script does not publish anything.
