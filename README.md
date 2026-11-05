<p align="center">
  <a href="README.md"><img alt="한국어 (현재 문서)" src="https://img.shields.io/badge/%ED%95%9C%EA%B5%AD%EC%96%B4-%ED%98%84%EC%9E%AC%20%EB%AC%B8%EC%84%9C-1f6feb?style=for-the-badge"></a>
  <a href="README.en.md"><img alt="English" src="https://img.shields.io/badge/English-Read%20in%20English-6e7681?style=for-the-badge"></a>
</p>

# Tushonka Cleaner 3.0.0

SPT 4.1.5의 메모리 정리·진단과 서버 최적화를 합친 모드입니다. 작성자는 R_F이며, 원작자·출처·라이선스 전문은 [NOTICE.md](NOTICE.md)와 [LICENSE](LICENSE)에 있습니다.

1. **CactusPie — SPT-RamCleanerInterval**
   - 원본: https://github.com/CactusPie/SPT-RamCleanerInterval
   - 라이선스: GPL-3.0
2. **matsix — SPTVRAMCleaner**
   - 원본: https://github.com/matsixx/SPTVRAMCleaner
   - 라이선스: GPL-3.0
3. **EchoStarz — CompoundingPerf**
   - 원본: https://forge.sp-tarkov.com/mod/2415/compoundingperf
   - 라이선스: MIT

### ⚠️ IMPORTANT NOTICE / DISCLAIMER

1. **Reflection & Take-Downs:** I deeply reflect on the ECOT incident. As an AI-assisted "vibe coder," I will immediately delete files if the original authors ask.
2. **No Re-Distribution:** These ported builds are unverified, temporary fixes. Please do NOT re-upload or share them anywhere else.
3. **Do Not Pester Original Authors:** Never report bugs or pester original modders regarding issues from my unofficial ports.
4. **Full Credit & Respect:** I will always credit original creators on GitHub and prioritize their decisions above all else.
5. **Support Original Creators:** Instead of using my ports, please visit the original authors' Forge pages to leave kind words or tips.

## 변경 이력

전체 변경 이력 1개는 [`changelog/`](changelog/) 폴더에 10개씩 나눠 있습니다 (번호가 클수록 최신, 최신 파일: [`CHANGELOG-001.md`](changelog/CHANGELOG-001.md)). 릴리즈별 요약은 [Releases](../../releases).

## 포함 기능

- RAM 클리너의 자동 정리·간단 확인·집중 분석·누수 추적·꺼짐 모드와 직접 설정, 웹·보고서·진단 기능을 유지합니다.
- CompoundingPerf의 서버 기능과 클라이언트 C1(숨겨진 메뉴 영상 일시정지)·C2(착지한 에어드랍 물리 정리)를 유지합니다.
- 구형 모드가 함께 있으면 겹치는 기능을 중지하고 삭제 안내를 표시합니다. 새 cfg가 없을 때 구형 RAM cfg의 같은 절·키를 한 번 이전합니다.
- 그래픽 기능은 `Graphics/ISceneCleanup.cs`와 무동작 기본 구현만 있습니다. 실제 그래픽 모듈은 다음 단계입니다.

통합 전의 상세 기능 설명은 [메모리 기능](docs/ram-cleaner.md)과 [서버 기능](docs/server-optimizations.md)에 보존했습니다. 그 문서의 구형 설치·빌드 경로는 현재 배포 경로로 사용하지 않습니다.

## 빌드와 테스트

게임 참조 DLL이 있는 SPT 경로와 .NET 10 SDK가 필요합니다. 빌드는 게임 폴더로 복사하지 않습니다.

```powershell
dotnet build Tushonka.Cleaner.sln -c Release -p:DeployPlugin=false -p:SkipDeploy=true
dotnet test tests/Tushonka.Cleaner.Tests/Tushonka.Cleaner.Tests.csproj -c Release -p:SkipPackage=true -p:DeployPlugin=false -p:SkipDeploy=true
```

다른 게임 경로는 `-p:SptRoot="경로"`로 지정합니다. Release 빌드는 `release/Tushonka-Cleaner-3.0.0.zip`을 생성하도록 구성되어 있습니다.

```text
BepInEx/plugins/Tushonka-Cleaner/Tushonka.Cleaner.dll
SPT_Runtime/user/mods/Tushonka-Cleaner/Tushonka.Cleaner.Server.dll
SPT_Runtime/user/mods/Tushonka-Cleaner/config.json
LICENSE
NOTICE.md
```

설정 파일은 `BepInEx/config/com.zzap.tushonka.cleaner.cfg`입니다. 웹 페이지는 기존처럼 게임 실행 중 `http://127.0.0.1:6977/`에서 열립니다. 서버 웹 패널 주소는 `/tushonka-cleaner/`, 서버 최적화 전용 화면은 `/tushonka-cleaner/performance`입니다.

이번 작업의 실제 빌드·테스트 여부와 제한은 [INTEGRATE-RESULT.md](INTEGRATE-RESULT.md)를 확인하십시오.
