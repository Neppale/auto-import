<div align="center">

# AutoImport

![AutoImport Icon](icon.png)

 A Playnite library plugin that automatically scans your folders for game executables

</div>
<br>

## Features

- Scans each configured folder and its immediate subfolders for `.exe` files.
- Opens a selection window so you can import or ignore each result.
- Skips games already present in the Playnite library.
- Skips common non-game executables whose names contain `uninstall`, `setup`, `config`, or `crash`.
- Names a game from its folder, with the executable name as a fallback, and strips versions, tags, and common suffixes.
- Keeps a block list of folder paths, file paths, and file-name patterns so ignored items stay out of later scans.

## Installation

1. Download the latest release from the [Releases](https://github.com/Neppale/AutoImport/releases) page.
2. Extract the archive and copy the `AutoImport` folder into the Playnite extensions directory:
  - Desktop mode: `%AppData%\Playnite\Extensions\`
  - Portable: `<PlayniteInstallPath>\Extensions\`
3. Restart Playnite.

## Configuration

Open **Extensions → Extension settings → Libraries → AutoImport**.

**Scan folders:** Enter one full path per line, or separate paths with commas. Only that folder and the folders directly inside it are scanned.

```
C:\Games
D:\GOG Games
```

A scan runs when Playnite starts. To scan again, use **Reload games list → AutoImport**. Check **Import** or **Ignore** for each result, then click **Import Selected**. **Cancel** leaves the library and the block list unchanged. 

**Block list:** Type an entry and click **Add**. A path with a drive letter is stored as a path. Anything else is stored as a regular expression and matched against the file name only, case-insensitively. Invalid patterns are rejected. Remove an entry from the list to scan it again.

Block a whole folder. Every executable directly in that folder is skipped:

```
C:\Games\Redist
```

Block one file:

```
C:\Games\Hades\CrashHandler.exe
```

Block every matching file name under the scan folders. For example, `unins\d+` matches `unins000.exe` and `unins001.exe` wherever they are found:

```
unins\d+
```
You can know more about Regexes [here](https://docs.microsoft.com/en-us/dotnet/standard/base-types/regular-expression-language-quick-reference).
