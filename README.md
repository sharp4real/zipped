# Zipped

![Version](https://img.shields.io/badge/version-1.0-blue)
![License](https://img.shields.io/badge/license-MIT-green)
![Platform](https://img.shields.io/badge/platform-Windows-lightgrey)
![.NET](https://img.shields.io/badge/.NET-Framework-purple)

A secure, lightweight file compression and encryption tool for Windows with AES-256 encryption and DEFLATE compression. Completely offline and privacy-focused.

![Zipped Screenshot](https://github.com/sharp4real/zipped/blob/main/Screenshot%202025-12-28%20195645.png?raw=true)

## Features

- **🔒 Military-Grade Encryption** - AES-256 encryption with PBKDF2 key derivation (100,000 iterations)
- **📦 Real Compression** - DEFLATE algorithm for optimal file size reduction
- **🖱️ Windows Integration** - Right-click context menu for files and folders
- **🎯 Drag & Drop** - Simply drag files onto the executable
- **📂 Folder Support** - Compress entire directories with preserved structure
- **🔐 Password Protection** - Optional password encryption with minimum 2 characters
- **📊 Progress Tracking** - Real-time progress bars for all operations
- **💻 100% Offline** - No internet connection required, complete privacy
- **🎨 Interactive Tour** - Built-in walkthrough for first-time users

## Installation

1. Download the latest release of `Zipped.exe`
2. Run the executable (requires Administrator privileges for Windows integration)
3. The app will automatically set up:
   - `.zipped` file association
   - Right-click context menu entries
   - File icons and shell integration

## Usage

### Interactive Mode

Launch `Zipped.exe` without arguments to access the main menu:

```
[1] COMPRESS File or Folder
[2] EXTRACT Archive
[3] Take the Tour
[4] Exit
```

### Drag & Drop

Simply drag any file or folder onto `Zipped.exe` to compress it, or drag a `.zipped` file to extract it.

### Right-Click Integration

- Right-click any file or folder → **"Compress with Zipped"**
- Double-click any `.zipped` file → Automatically extracts

### Command Line

```bash
# Compress a file or folder
Zipped.exe "C:\path\to\file.txt"

# Extract an archive
Zipped.exe "C:\path\to\archive.zipped"
```

## Security

- **Encryption**: AES-256-CBC
- **Key Derivation**: PBKDF2 with SHA-256 (100,000 iterations)
- **Random Salt & IV**: Unique 32-byte salt and 16-byte IV per archive
- **File Format**: Custom `ZIPPED20` format with embedded encryption metadata

### Archive Structure

```
[8 bytes]  - "ZIPPED20" header
[32 bytes] - Salt
[16 bytes] - IV (Initialization Vector)
[remaining] - Encrypted compressed data
```

## Technical Details

- **Language**: C#
- **Framework**: .NET Framework
- **Compression**: System.IO.Compression (DEFLATE)
- **Encryption**: System.Security.Cryptography (AES)
- **Platform**: Windows (requires Administrator for registry operations)

## File Format

The `.zipped` format is a custom archive format that:
1. Compresses files/folders using ZIP/DEFLATE
2. Optionally encrypts the entire compressed archive with AES-256
3. Stores encryption metadata (salt, IV) in the file header

## Requirements

- Windows 7 or later
- .NET Framework 4.5 or higher
- Administrator privileges (for initial setup only)

## Building from Source

```bash
# Using .NET Framework
csc /target:exe /out:Zipped.exe Program.cs
```

Or open in Visual Studio and build the solution.

## Configuration

Application data is stored in:
```
%APPDATA%\Zipped\
```

This includes:
- `tour_completed.flag` - Tracks first-run tour completion

## Privacy

Zipped is completely offline and privacy-focused:
- No telemetry or analytics
- No internet connection required
- No data collection
- All operations are local

## Known Limitations

- Windows only (uses Windows Registry for integration)
- Requires Administrator privileges for shell integration
- Password minimum is 2 characters (for user convenience)

## License

MIT License - See LICENSE file for details

## Contributing

Contributions are welcome! Please feel free to submit pull requests or open issues for bugs and feature requests.

## Roadmap

- [ ] Multi-language support
- [ ] Batch processing mode
- [ ] Archive integrity verification
- [ ] Split archive support for large files
- [ ] Cross-platform support (Linux/macOS)

## Disclaimer

This tool uses strong encryption, but no security system is perfect. Always keep backups of important files and remember your passwords - there is no password recovery mechanism.

---

**Made with ❤️ for secure, private file compression**
