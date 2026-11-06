<p align="center">
  <a href="README.md"><img alt="한국어 (현재 문서)" src="https://img.shields.io/badge/%ED%95%9C%EA%B5%AD%EC%96%B4-%ED%98%84%EC%9E%AC%20%EB%AC%B8%EC%84%9C-1f6feb?style=for-the-badge"></a>
  <a href="README.en.md"><img alt="English" src="https://img.shields.io/badge/English-Read%20in%20English-6e7681?style=for-the-badge"></a>
</p>

<p align="center">
  <a href="https://danyhappy564-cmyk.github.io/Tushonka-Cleaner/"><img src="site/img/og-ko.jpg" alt="Tushonka Cleaner 소개 사이트" width="820"></a>
</p>

<h2 align="center">📖 <a href="https://danyhappy564-cmyk.github.io/Tushonka-Cleaner/">설명은 전부 소개 사이트에 있습니다 →</a></h2>
<p align="center">맵별 성능 비교 · 왼쪽 위 창 읽는 법 · 기능별 검증 · 설치와 주의사항을 GIF와 함께 한 페이지에 정리했습니다.</p>

<p align="center">
  <a href="https://danyhappy564-cmyk.github.io/Tushonka-Cleaner/"><img alt="소개 사이트 열기" src="https://img.shields.io/badge/%EC%86%8C%EA%B0%9C%20%EC%82%AC%EC%9D%B4%ED%8A%B8-%EC%97%B4%EA%B8%B0-913e27?style=for-the-badge"></a>
  <a href="https://github.com/danyhappy564-cmyk/Tushonka-Cleaner/releases/latest"><img alt="최신 버전 받기" src="https://img.shields.io/badge/%EC%B5%9C%EC%8B%A0%20%EB%B2%84%EC%A0%84-%EB%B0%9B%EA%B8%B0-2ea043?style=for-the-badge&logo=github&logoColor=white"></a>
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
  - **Note:** 작은 장식을 가린다는 착상을 참고했습니다. 코드는 사용하지 않았습니다.
- **License of This Build:** GPL-3.0. See [NOTICE.md](NOTICE.md) and [LICENSE](LICENSE).
- **This Port By:** R_F (danyhappy564-cmyk) — unofficial, AI-assisted port. Not affiliated with or endorsed by the original authors.

1. **Reflection & Take-Downs:** I deeply reflect on the ECOT incident. As an AI-assisted "vibe coder," I will immediately delete files if the original authors ask.
2. **No Re-Distribution:** These ported builds are unverified, temporary fixes. Please do NOT re-upload or share them anywhere else.
3. **Do Not Pester Original Authors:** Never report bugs or pester original modders regarding issues from my unofficial ports.
4. **Full Credit & Respect:** I will always credit original creators on GitHub and prioritize their decisions above all else.
5. **Support Original Creators:** Instead of using my ports, please visit the original authors' Forge pages to leave kind words or tips.

---

## 변경 이력

전체 변경 이력 5개는 [`changelog/`](changelog/) 폴더에 10개씩 나눠 있습니다 (번호가 클수록 최신, 최신 파일: [`CHANGELOG-001.md`](changelog/CHANGELOG-001.md)). 릴리즈별 요약은 [Releases](../../releases).

## 포함 기능

- RAM 클리너의 자동 정리·간단 확인·집중 분석·누수 추적·꺼짐 모드와 직접 설정, 웹·보고서·진단 기능을 유지합니다.
- CompoundingPerf에서는 측정으로 효과가 확인된 S9(응답 압축), S12(봇 장비 오류 수정), C1(숨겨진 메뉴 영상 일시정지), C2(착지한 에어드랍 물리 정리)만 유지합니다. 1.1.0에서 효과가 없거나 위험한 기능을 뺐으며, 기능별 수치는 [서버 기능](docs/server-optimizations.md)과 [메모리 기능](docs/ram-cleaner.md) 맨 위에 있습니다.
- 이전 모드(RAM Cleaner, CompoundingPerf, DeClutter)는 서버를 켤 때 게임 폴더의 `Tushonka-Cleaner-OldMods`로 자동으로 옮깁니다. 지우지 않으므로 필요하면 되돌릴 수 있습니다. 옮긴 첫 실행에서는 겹치는 기능이 멈추므로 서버를 한 번 다시 켜야 합니다. 새 cfg가 없을 때 구형 RAM cfg의 같은 절·키를 한 번 이전합니다.
- **Smoothie 모드와 함께 쓰지 마십시오.** 같은 게임 기능을 건드려 효과가 겹치거나 오류가 날 수 있습니다. 함께 설치되어 있으면 서버 콘솔과 메뉴 화면에 경고를 표시합니다. Smoothie 파일은 건드리지 않습니다.
- 그래픽 기능은 게임 진행과 무관한 작은 장식을 가리고 그래픽 값을 조정합니다. 기본값은 꺼짐이고 권장 단계는 없습니다. 프레임을 크게 올리는 단계는 많이 가림입니다. 다른 모드의 코드 없이 새로 작성했으며 근거 자료는 `tests/SceneCleanup/`에 있습니다.

게임이 꺼져 있어도 웹 설정 페이지는 켜져 있을 때와 같은 화면이며, 그 자리에서 설정을 바꿀 수 있습니다.

통합 전의 상세 기능 설명은 [메모리 기능](docs/ram-cleaner.md)과 [서버 기능](docs/server-optimizations.md)에 보존했습니다. 그 문서의 구형 설치·빌드 경로는 현재 배포 경로로 사용하지 않습니다.

## 측정 결과

맵별 FPS 표, GIF, 비교 사진, 기능별 검증 표는 [소개 사이트](https://danyhappy564-cmyk.github.io/Tushonka-Cleaner/)에 있습니다. 그래픽은 취향을 많이 타서 권장 단계가 없습니다. 사이트의 비교를 보고 고르십시오.

## 빌드와 테스트

게임 참조 DLL이 있는 SPT 경로와 .NET 10 SDK가 필요합니다. 빌드는 게임 폴더로 복사하지 않습니다.

```powershell
dotnet build Tushonka.Cleaner.sln -c Release -p:DeployPlugin=false -p:SkipDeploy=true
dotnet test tests/Tushonka.Cleaner.Tests/Tushonka.Cleaner.Tests.csproj -c Release -p:SkipPackage=true -p:DeployPlugin=false -p:SkipDeploy=true
```

다른 게임 경로는 `-p:SptRoot="경로"`로 지정합니다. Release 빌드는 `release/Tushonka-Cleaner-1.1.0.zip`을 생성하도록 구성되어 있습니다.

```text
BepInEx/plugins/Tushonka-Cleaner/Tushonka.Cleaner.dll
BepInEx/plugins/Tushonka-Cleaner/scene-rules.json
BepInEx/plugins/Tushonka-Cleaner/tushonka-exclude.example.txt
SPT_Runtime/user/mods/Tushonka-Cleaner/Tushonka.Cleaner.Server.dll
SPT_Runtime/user/mods/Tushonka-Cleaner/config.json
LICENSE
NOTICE.md
```

설정 파일은 `BepInEx/config/com.zzap.tushonka.cleaner.cfg`입니다. 웹 페이지는 게임 실행 중 `http://127.0.0.1:6977/`에서 열립니다. 게임이 꺼져 있으면 SPT 서버의 `/tushonka-cleaner/`에서 같은 화면이 열립니다, 서버 최적화 전용 화면은 `/tushonka-cleaner/performance`입니다.

