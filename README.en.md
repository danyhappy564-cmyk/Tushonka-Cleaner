<p align="center">
  <a href="README.md"><img alt="한국어" src="https://img.shields.io/badge/%ED%95%9C%EA%B5%AD%EC%96%B4-%ED%95%9C%EA%B5%AD%EC%96%B4%EB%A1%9C%20%EB%B3%B4%EA%B8%B0-6e7681?style=for-the-badge"></a>
  <a href="README.en.md"><img alt="English (this page)" src="https://img.shields.io/badge/English-This%20page-1f6feb?style=for-the-badge"></a>
</p>

<p align="center">
  <a href="https://danyhappy564-cmyk.github.io/Tushonka-Cleaner/?lang=en"><img src="site/img/og-en.jpg" alt="Tushonka Cleaner project site" width="820"></a>
</p>

<h2 align="center">📖 <a href="https://danyhappy564-cmyk.github.io/Tushonka-Cleaner/?lang=en">Everything is explained on the project site →</a></h2>
<p align="center">Per-map performance · how to read the top-left window · feature audit · install and cautions, with GIFs, on one page.</p>

<p align="center">
  <a href="https://danyhappy564-cmyk.github.io/Tushonka-Cleaner/?lang=en"><img alt="Open the project site" src="https://img.shields.io/badge/Project%20site-Open-913e27?style=for-the-badge"></a>
  <a href="https://github.com/danyhappy564-cmyk/Tushonka-Cleaner/releases/latest"><img alt="Download the latest release" src="https://img.shields.io/badge/Latest%20release-Download-2ea043?style=for-the-badge&logo=github&logoColor=white"></a>
</p>

---

### ⚠️ IMPORTANT NOTICE / DISCLAIMER

- **Original Author:** CactusPie
  - **Original Repository:** SPT-RamCleanerInterval
  - **Original Link:** https://github.com/CactusPie/SPT-RamCleanerInterval
  - **License:** GPL-3.0
- **Original Author:** matsix
  - **Original Repository:** SPTVRAMCleaner
  - **Original Link:** https://github.com/matsixx/SPTVRAMCleaner
  - **License:** GPL-3.0
- **Original Author:** EchoStarz
  - **Original Repository:** CompoundingPerf
  - **Original Link:** https://forge.sp-tarkov.com/mod/2415/compoundingperf
  - **License:** MIT
- **Idea Source:** Tyrian (Xenoxia8953)
  - **Original Repository:** Tyrian-DeClutterer
  - **Original Link:** https://github.com/Xenoxia8953/Tyrian-DeClutterer
  - **Note:** The idea of hiding small decorations came from this mod. No code was used.
- **License of This Build:** GPL-3.0. See [NOTICE.md](NOTICE.md) and [LICENSE](LICENSE).
- **This Port By:** R_F (danyhappy564-cmyk) — unofficial, AI-assisted port. Not affiliated with or endorsed by the original authors.

1. **Reflection & Take-Downs:** I deeply reflect on the ECOT incident. As an AI-assisted "vibe coder," I will immediately delete files if the original authors ask.
2. **No Re-Distribution:** These ported builds are unverified, temporary fixes. Please do NOT re-upload or share them anywhere else.
3. **Do Not Pester Original Authors:** Never report bugs or pester original modders regarding issues from my unofficial ports.
4. **Full Credit & Respect:** I will always credit original creators on GitHub and prioritize their decisions above all else.
5. **Support Original Creators:** Instead of using my ports, please visit the original authors' Forge pages to leave kind words or tips.

---

## Changelog

The full changelog (5 entries) is in the [`changelog/`](changelog/) folder, 10 per file (higher number = newer, latest: [`CHANGELOG-001.md`](changelog/CHANGELOG-001.md)). Per-release summaries: [Releases](../../releases).

## Features

- Preserves the five RAM cleaner modes, custom settings, web pages, reports and diagnostics.
- From CompoundingPerf only the features with a measured or confirmed effect remain: S9 (response compression), S12 (bot gear bug fix), C1 (pause hidden menu videos) and C2 (landed airdrop physics). 1.1.0 removed what had no effect or carried risk; per-feature numbers are at the top of the two documents below.
- Old mods (RAM Cleaner, CompoundingPerf, DeClutter) are moved to `Tushonka-Cleaner-OldMods` in the game folder when the server starts. Nothing is deleted, so you can restore them. Overlapping features stay off on that first run, so restart the server once. Matching legacy RAM configuration sections and keys are imported once when the new cfg does not exist.
- **Do not use this mod together with Smoothie.** Both change the same game systems, which can cause overlapping effects or errors. If both are installed, a warning appears in the server console and on the menu screen. Smoothie files are left untouched.
- The graphics feature hides small decorations that do not affect play and adjusts graphics values. It is off by default and has no recommended level. Hide more clutter is the level that raises FPS by a large amount. It was written from scratch without other mods' code; the supporting material is in `tests/SceneCleanup/`.

The web settings page looks the same with the game closed and open, and settings can be changed with the game closed.

Archived feature descriptions: [memory](docs/ram-cleaner.md), [server](docs/server-optimizations.md). Their legacy installation and build paths do not apply to this package.

## Measured results

Per-map FPS tables, GIFs, comparison images and the per-feature audit are on the [project site](https://danyhappy564-cmyk.github.io/Tushonka-Cleaner/?lang=en). Graphics come down to taste, so there is no recommended level. Compare on the site, then choose.

## Build and tests

Requires the .NET 10 SDK and game reference assemblies. Builds never deploy to the game folder.

```powershell
dotnet build Tushonka.Cleaner.sln -c Release -p:DeployPlugin=false -p:SkipDeploy=true
dotnet test tests/Tushonka.Cleaner.Tests/Tushonka.Cleaner.Tests.csproj -c Release -p:SkipPackage=true -p:DeployPlugin=false -p:SkipDeploy=true
```

Use `-p:SptRoot="path"` to override the game directory. Release builds are configured to create `release/Tushonka-Cleaner-1.1.0.zip` containing:

```text
BepInEx/plugins/Tushonka-Cleaner/Tushonka.Cleaner.dll
BepInEx/plugins/Tushonka-Cleaner/scene-rules.json
BepInEx/plugins/Tushonka-Cleaner/tushonka-exclude.example.txt
SPT_Runtime/user/mods/Tushonka-Cleaner/Tushonka.Cleaner.Server.dll
SPT_Runtime/user/mods/Tushonka-Cleaner/config.json
LICENSE
NOTICE.md
```

Client configuration: `BepInEx/config/com.zzap.tushonka.cleaner.cfg`. Live web page: `http://127.0.0.1:6977/`. Server pages: `/tushonka-cleaner/` and `/tushonka-cleaner/performance`.

