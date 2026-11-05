# 연결 방법

이 DLL은 BepInEx 플러그인이 호출하는 그래픽 모듈입니다. 자체 플러그인·설치·자동 배포 코드는 없습니다. 호출은 모두 Unity 주 스레드에서 수행합니다.

```csharp
// 플러그인 초기화: map 로딩 전에 실행합니다.
SceneCleanupModule.Init(Config, Logger, dataDirectory);
// 플러그인 Update:
SceneCleanupModule.Tick();
// 사용자 단계 선택:
SceneCleanupModule.SetLevel(SceneCleanupLevel.Balanced);
// UI 표시용 값 형식:
SceneCleanupStatus status = SceneCleanupModule.GetStatus();
// 플러그인 종료:
SceneCleanupModule.Shutdown();
```

네임스페이스는 `Tushonka.Cleaner.SceneCleanup`입니다. `dataDirectory`에는 `data/scene-rules.json`을 `scene-rules.json`이라는 이름으로 둡니다. 선택적 `tushonka-exclude.txt`도 같은 폴더에 둡니다. 제외 패턴은 대소문자를 구별하지 않는 전체 이름 패턴이며 `*`, `?`를 지원합니다. 빈 줄과 `#`로 시작하는 줄은 무시합니다. 제외 파일은 Init 시 읽으며 다시 읽으려면 종료 후 초기화합니다.

기본 단계는 Off입니다. 설정 섹션은 `Graphics`, `Graphics.Advanced`, `Graphics.Categories`, `Graphics.Tuning`입니다. 데칼은 맵 로딩 중 등록될 때의 단계를 읽어 판정하며, 레이드가 시작되면 새 판정을 하지 않으므로 변경은 다음 레이드부터 적용됩니다. 대상은 `StaticDeferredDecal` 타입 전체이고 크기 상한(규칙의 데칼 상한 6m), 제외 파일, 상호작용 조상 검사만 적용합니다. LightTouch에서는 가까운 데칼까지 사라지는 것을 피하려고 데칼을 유지합니다. 기존 데칼을 비활성화하거나 등록 해제하지 않습니다.

총알 자국은 레이드에서 **새로 등록하는 횟수**를 제한합니다. 이전에 생성된 자국을 지우거나 버퍼를 다시 만들지 않습니다. Off는 이후 등록을 원래대로 허용하며 이미 수명이 끝난 탄피나 생성하지 않은 자국을 되살리지 않습니다. 탄피는 버려진 사용 완료 탄피만 게임의 원래 반환 경로로 정리하며 무기 안의 탄약·불발탄·전리품에는 적용하지 않습니다.

재현 명령은 `powershell -NoProfile -ExecutionPolicy Bypass -File tools/build.ps1`(Release), 같은 명령 뒤 `-Tests`(게임 없는 시험), `python tools/analyze_names.py`(규칙·집계 생성)입니다. 원격 NuGet 소스는 비어 있으며 캐시·임시 파일은 작업 폴더 안에 둡니다.

숨김은 `Renderer.enabled`가 아니라 물체의 `activeSelf`로 합니다. 게임 가림 처리가 `enabled`를 계속 쓰기 때문입니다. 프레임 예산은 기본 2ms이며 조준에 따른 물체 변경은 기본 24개/프레임입니다. 후보 방문 512개/프레임, 스캔 16,384단계/프레임(시간 예산이 먼저 끊음), 정상 상태 재검사 4개/프레임, 트리 깊이 128, 루트 8,192개/씬, 후보 65,536개를 상한으로 둡니다. 한도를 넘으면 추가 물체를 변경하지 않습니다. Unity의 단일 호출·게임 종료 시 복원은 중간에 선점할 수 없으므로 실제 16ms 이하 여부와 초기 이름 읽기의 할당량은 게임 프로파일링이 필요합니다.
