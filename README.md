[![License: MIT](https://img.shields.io/badge/License-MIT-blueviolet.svg)](https://opensource.org/license/mit)
[![Release Version](https://img.shields.io/github/v/tag/mi5hmash/DeathloopSaveDataChecksumFixer?label=Version)](https://github.com/mi5hmash/DeathloopSaveDataChecksumFixer/releases/latest)
[![Visual Studio 2026](https://custom-icon-badges.demolab.com/badge/Visual%20Studio%202026-F0ECF8.svg?&logo=visual-studio-26)](https://visualstudio.microsoft.com/)
[![.NET10](https://img.shields.io/badge/.NET%2010-512BD4?logo=dotnet&logoColor=fff)](#)

> [!IMPORTANT]
> **This software is free and open source. If someone asks you to pay for it, it's likely a scam.**

# ♾️ DeathloopSaveDataChecksumFixer - What is it :interrobang:
![icon](https://github.com/mi5hmash/DeathloopSaveDataChecksumFixer/blob/main/.resources/images/DLSDCF-logo.png)

This console application can **repair the checksum** of Deathloop SaveData files. It can also **change the save file owner** for the PC version and **convert saves between console and PC platforms**.

# :scream: Is it safe?
The short answer is: **No.** 
> [!CAUTION]
> If you unreasonably edit your SaveData files, you risk corrupting them lose your progress.

> [!IMPORTANT]
> Always back up the files you intend to edit before editing them.

> [!IMPORTANT]
> Disable the Steam Cloud before you replace any SaveData files.

You have been warned, and now that you are completely aware of what might happen, you may proceed to the next chapter.

# :scroll: How to use this tool
## [CLI] - 🪟 Windows | 🐧 Linux | 🍎 macOS
```plaintext
Usage: .\deathloop-savedata-checksum-fixer-cli.exe -p <input_folder_path> [options]

Options:
  -p <input_folder_path>  Path to folder containing SaveData files
  -u <user_id>            New User ID (optional)
  -s                      Switch platform (convert between console and PC SaveData formats)
  -q                      Don't wait for user input to exit after operation completes (auto-close)
  -h                      Show this help message
```

### Examples
#### Fix Checksum
```bash
.\deathloop-savedata-checksum-fixer-cli.exe -p ".\InputDirectory"
```
#### Convert
```bash
.\deathloop-savedata-checksum-fixer-cli.exe -p ".\InputDirectory" -s
```
#### Convert, Update User ID, and Fix Checksum in one operation
```bash
.\deathloop-savedata-checksum-fixer-cli.exe -p ".\InputDirectory" -s -u 76561197960265729
```

> [!NOTE]
> Modified files are being placed in a newly created folder within the ***"DeathloopSaveDataChecksumFixer/_OUTPUT/"*** folder.

# :fire: Issues
All the problems I've encountered during my tests have been fixed on the go. If you find any other issues (which I hope you won't) feel free to report them [there](https://github.com/mi5hmash/DeathloopSaveDataChecksumFixer/issues).