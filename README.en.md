<p align="center">
  <a href="README.md"><img alt="한국어" src="https://img.shields.io/badge/%ED%95%9C%EA%B5%AD%EC%96%B4-%ED%95%9C%EA%B5%AD%EC%96%B4%EB%A1%9C%20%EB%B3%B4%EA%B8%B0-6e7681?style=for-the-badge"></a>
  <a href="README.en.md"><img alt="English (this page)" src="https://img.shields.io/badge/English-This%20page-1f6feb?style=for-the-badge"></a>
</p>

# Tushonka Cleaner 3.0.0

Memory cleanup, diagnostics and server optimisation for SPT 4.1.5, integrated by R_F. See [NOTICE.md](NOTICE.md) and [LICENSE](LICENSE) for the original authors and licence texts.

1. **CactusPie — SPT-RamCleanerInterval**
   - Original: https://github.com/CactusPie/SPT-RamCleanerInterval
   - Licence: GPL-3.0
2. **matsix — SPTVRAMCleaner**
   - Original: https://github.com/matsixx/SPTVRAMCleaner
   - Licence: GPL-3.0
3. **EchoStarz — CompoundingPerf**
   - Original: https://forge.sp-tarkov.com/mod/2415/compoundingperf
   - Licence: MIT

### ⚠️ IMPORTANT NOTICE / DISCLAIMER

1. **Reflection & Take-Downs:** I deeply reflect on the ECOT incident. As an AI-assisted "vibe coder," I will immediately delete files if the original authors ask.
2. **No Re-Distribution:** These ported builds are unverified, temporary fixes. Please do NOT re-upload or share them anywhere else.
3. **Do Not Pester Original Authors:** Never report bugs or pester original modders regarding issues from my unofficial ports.
4. **Full Credit & Respect:** I will always credit original creators on GitHub and prioritize their decisions above all else.
5. **Support Original Creators:** Instead of using my ports, please visit the original authors' Forge pages to leave kind words or tips.

## Changelog

The full changelog (1 entry) is in the [`changelog/`](changelog/) folder, 10 per file (higher number = newer, latest: [`CHANGELOG-001.md`](changelog/CHANGELOG-001.md)). Per-release summaries: [Releases](../../releases).

## Features

- Preserves the five RAM cleaner modes, custom settings, web pages, reports and diagnostics.
- Preserves CompoundingPerf server features and client fixes C1 (hidden menu videos) and C2 (landed airdrop physics).
- Old mods (RAM Cleaner, CompoundingPerf, DeClutter) are moved to `Tushonka-Cleaner-OldMods` in the game folder when the server starts. Nothing is deleted, so you can restore them. Overlapping features stay off on that first run, so restart the server once. Matching legacy RAM configuration sections and keys are imported once when the new cfg does not exist.
- **Do not use this mod together with Smoothie.** Both change the same game systems, which can cause overlapping effects or errors. If both are installed, a warning appears in the server console and on the menu screen. Smoothie files are left untouched.
- The graphics feature hides small decorations that do not affect play. It is off by default. It was written from scratch without other mods' code; the supporting material is in `tests/SceneCleanup/`.

Archived feature descriptions: [memory](docs/ram-cleaner.md), [server](docs/server-optimizations.md). Their legacy installation and build paths do not apply to this package.

## Build and tests

Requires the .NET 10 SDK and game reference assemblies. Builds never deploy to the game folder.

```powershell
dotnet build Tushonka.Cleaner.sln -c Release -p:DeployPlugin=false -p:SkipDeploy=true
dotnet test tests/Tushonka.Cleaner.Tests/Tushonka.Cleaner.Tests.csproj -c Release -p:SkipPackage=true -p:DeployPlugin=false -p:SkipDeploy=true
```

Use `-p:SptRoot="path"` to override the game directory. Release builds are configured to create `release/Tushonka-Cleaner-3.0.0.zip` containing:

```text
BepInEx/plugins/Tushonka-Cleaner/Tushonka.Cleaner.dll
SPT_Runtime/user/mods/Tushonka-Cleaner/Tushonka.Cleaner.Server.dll
SPT_Runtime/user/mods/Tushonka-Cleaner/config.json
LICENSE
NOTICE.md
```

Client configuration: `BepInEx/config/com.zzap.tushonka.cleaner.cfg`. Live web page: `http://127.0.0.1:6977/`. Server pages: `/tushonka-cleaner/` and `/tushonka-cleaner/performance`.

See [INTEGRATE-RESULT.md](INTEGRATE-RESULT.md) for the actual verification results and limitations of this integration.
