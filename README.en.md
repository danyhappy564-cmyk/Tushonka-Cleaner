<p align="center">
  <a href="README.md"><img alt="한국어" src="https://img.shields.io/badge/%ED%95%9C%EA%B5%AD%EC%96%B4-%ED%95%9C%EA%B5%AD%EC%96%B4%EB%A1%9C%20%EB%B3%B4%EA%B8%B0-6e7681?style=for-the-badge"></a>
  <a href="README.en.md"><img alt="English (this page)" src="https://img.shields.io/badge/English-This%20page-1f6feb?style=for-the-badge"></a>
</p>

# Tushonka Cleaner 1.0.0

Memory cleanup, diagnostics, server optimisation and graphics adjustments for SPT 4.1, integrated by R_F. Tested on SPT 4.1.6. See [NOTICE.md](NOTICE.md) and [LICENSE](LICENSE) for the original authors and licence texts.

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

The full changelog (2 entries) is in the [`changelog/`](changelog/) folder, 10 per file (higher number = newer, latest: [`CHANGELOG-001.md`](changelog/CHANGELOG-001.md)). Per-release summaries: [Releases](../../releases).

## Features

- Preserves the five RAM cleaner modes, custom settings, web pages, reports and diagnostics.
- Preserves CompoundingPerf server features and client fixes C1 (hidden menu videos) and C2 (landed airdrop physics).
- Old mods (RAM Cleaner, CompoundingPerf, DeClutter) are moved to `Tushonka-Cleaner-OldMods` in the game folder when the server starts. Nothing is deleted, so you can restore them. Overlapping features stay off on that first run, so restart the server once. Matching legacy RAM configuration sections and keys are imported once when the new cfg does not exist.
- **Do not use this mod together with Smoothie.** Both change the same game systems, which can cause overlapping effects or errors. If both are installed, a warning appears in the server console and on the menu screen. Smoothie files are left untouched.
- The graphics feature raises FPS with graphics adjustments and hides small decorations that do not affect play. It is off by default, and Hide a bit more is the recommended level. It was written from scratch without other mods' code; the supporting material is in `tests/SceneCleanup/`.

Archived feature descriptions: [memory](docs/ram-cleaner.md), [server](docs/server-optimizations.md). Their legacy installation and build paths do not apply to this package.

## Measured results

Measured on an RTX 4080 at 2560x1440 with no bots on SPT 4.1.6. Each value is the average of three 4-second runs at one of three spots. Results vary by PC and map state.

| Graphics level | Streets (ms) | Lighthouse (ms) | Visual change |
|---|---|---|---|
| Off | 8.4 / 7.8 / 6.8 | 5.7 / 5.2 / 5.6 | None |
| Light touch | 8.4 / 7.9 / 6.9 | 5.7 / 5.2 / 5.6 | Only distant clutter disappears. FPS does not rise. |
| Hide a bit more (recommended) | 7.1 / 6.7 / 6.0 | 5.1 / 4.6 / 5.3 | Distant models and shadows get simpler. |
| Hide more clutter | 5.4 / 5.2 / 4.7 | 5.0 / 4.7 / 5.1 | Shadows turn off, dark areas get brighter, and nearby walls look rougher. |

- FPS gains come from the graphics adjustments. Turning off all 15,696 matching decorations on Streets did not change FPS.
- The frame cost of the memory modes stayed within measurement noise (±0.5 ms).
- Automatic cleanup runs when memory grows by 1.5 GB. In testing it went from 4.49 GB to 3.41 GB in 0.8 seconds, with one 95 ms stutter.
- Server `/client/items` response: Off 489 ms (3.6 MB), Balanced 305 ms (3.7 MB), Recommended 265 ms (5.9 MB).

## Build and tests

Requires the .NET 10 SDK and game reference assemblies. Builds never deploy to the game folder.

```powershell
dotnet build Tushonka.Cleaner.sln -c Release -p:DeployPlugin=false -p:SkipDeploy=true
dotnet test tests/Tushonka.Cleaner.Tests/Tushonka.Cleaner.Tests.csproj -c Release -p:SkipPackage=true -p:DeployPlugin=false -p:SkipDeploy=true
```

Use `-p:SptRoot="path"` to override the game directory. Release builds are configured to create `release/Tushonka-Cleaner-1.0.0.zip` containing:

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

