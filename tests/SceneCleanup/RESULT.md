# Scene Cleanup 구현 결과

`src/SceneCleanup/` 8개 C# 파일, `SceneCleanup.csproj`, `data/scene-rules.json`, `tests/`, `tools/`를 만들었습니다. 진입점은 `SceneCleanupModule.Init / Tick / SetLevel / GetStatus / Shutdown`이며 연결 방법은 [INTEGRATION.md](INTEGRATION.md)에 있습니다. 결과 DLL은 [bin/Release/net471/Tushonka.Cleaner.SceneCleanup.dll](bin/Release/net471/Tushonka.Cleaner.SceneCleanup.dll)입니다.

규칙은 이름 CSV 78,821행을 직접 분석하여 만들었습니다. 분류별 이름군·크기 상한으로 후보를 고르고, 어느 맵에서든 콜라이더 비율이 0보다 큰 이름을 전체 제외했습니다. 관측된 이름 176개를 전체 이름 패턴으로 저장합니다. 실제 적용 시 조상·근접 형제의 콜라이더와 허용 목록 밖 컴포넌트, 자식이 있는 렌더러, 영속 씬을 추가 보호합니다. 평균 크기와 정적 비율만으로 안전성을 단정하지 않습니다.

아래는 CSV의 렌더러 개수 합계이며 실제 숨김 수는 아닙니다. 합계 36,472개이며 안전 검사·사용자 제외·시간 예산으로 더 줄어듭니다. 데칼 수는 Balanced/Max 로딩 등록 후보이며 LightTouch에서는 유지합니다.

| 맵 | 쓰레기 | 잔해 | 탄피 | 장식 음식 | 물웅덩이 | 유리 조각 | 데칼 |
|---|---:|---:|---:|---:|---:|---:|---:|
| Interchange | 2439 | 0 | 6 | 87 | 0 | 24 | 677 |
| Lighthouse | 1430 | 0 | 16 | 74 | 14 | 382 | 266 |
| RezervBase | 484 | 0 | 14 | 0 | 43 | 83 | 525 |
| Sandbox | 1667 | 988 | 27 | 69 | 82 | 481 | 578 |
| Shoreline | 571 | 0 | 20 | 0 | 1 | 9 | 28 |
| TarkovStreets | 8878 | 1146 | 122 | 470 | 368 | 2599 | 3004 |
| Woods | 80 | 0 | 4 | 10 | 1 | 0 | 178 |
| bigmap | 418 | 0 | 0 | 0 | 0 | 0 | 1217 |
| factory4_day | 372 | 306 | 42 | 3 | 0 | 1 | 5026 |
| laboratory | 367 | 0 | 0 | 0 | 4 | 1 | 770 |

위험 이름 처리: `Door_Glass_LOD`, `glass_window`는 유리 규칙에 넣지 않았습니다. `item_food_mayo_LOD`, `Food_barrel_closed_LOD`, `BreadRack_LOD`, `Vegetable_basket_LOD`도 제외했습니다. `Debris` 파티클은 렌더러 종류로 제외했습니다. `rifle_shells_grp`는 맵에서 콜라이더가 관측되어 제외했습니다. 원자료의 부모 정보·시각적 의미는 없으므로 잘못 숨길 위험이 완전히 없다고 단정하지 않습니다. 재현 도구는 [tools/analyze_names.py](tools/analyze_names.py), 상세 집계는 [data/rule-audit.json](data/rule-audit.json)입니다.

| 게임 지점 | 정적 분석으로 확인한 역할과 사용 이유 |
|---|---|
| `EFT.GameWorld.OnGameStarted / Dispose` | 실제 레이드 시작 이후 지연 검사, 종료 전 복원·강한 참조 해제에 사용합니다. 로컬/네트워크 파생 클래스도 확인했습니다. |
| `StaticDeferredDecalRenderer.RegisterDecal / AddEarlyDecals` | 초기 등록이 모이는 지점입니다. 여기를 거부하여 등록 후 버퍼 재생성을 피합니다. `RemoveDecal`은 미등록 물체를 무시합니다. |
| `CameraManager.Camera`, `OpticCameraManager.Camera / IsAnyOpticCameraRendering` | 주 카메라와 스코프의 현재 시야각을 매 Tick 읽습니다. |
| `MuzzleLight.SetIntensity`, `EFT.Player.IsYourPlayer` | 강도는 바꾸지 않고 다른 플레이어의 그림자만 큐로 조정합니다. 소유자가 불명확하면 원래 상태를 유지합니다. |
| `AmmoPoolObject.SetUsed / ReturnToPool / ShouldBeDestroyed` | 사용 완료·분리된 탄피만 제한하고 게임의 기존 반환 경로에 맡깁니다. `Firearms.ThrowPatronAsLoot`의 실제 탄약 경로와 구분했습니다. |
| `DeferredDecalRenderer.DrawDecal`, `DecalSystem.Add` | 기존 버퍼를 재생성하지 않고 새 총알 자국 등록을 제한합니다. |

배율 정책은 `기준 거리 × clamp(tan(60°/2) / tan(현재 시야각/2), 1, 12)`입니다. 주 카메라와 실제 스코프 중 더 긴 거리를 사용하므로 11° 시야각에서는 기본 80m가 약 480m로 늘어납니다. ±10% 거리 여유, 기본 24개/프레임 변경 상한, 공통 2ms 예산으로 조준 시 한꺼번에 켜지는 작업을 나눕니다.

검증: `tools/verify.ps1` 종료 코드 0입니다. Release net471 빌드 **경고 0 / 오류 0**, 규칙·복원 저장소 시험 **38개**, 실제 실행 코드를 사용하는 Unity/BepInEx 모형 시험 **48개**, CSV 전체 **78,821행 / 맵·분류 집계 70개 일치**, 규칙 재생성 해시 일치를 확인했습니다. 렌더러·월드·광원·큐·탄피·Transform 6종의 약한 참조 회수와 준비 완료 후 반복 경로 0바이트 할당도 모형에서 확인했습니다. [검증 로그](evidence/verification.log)를 남겼습니다.

**제한·미검증:** 실제 Harmony 연결·인게임 숨김·프레임 향상은 실행하지 않았습니다. 단일 Unity 호출 및 종료 시 일괄 복원은 선점할 수 없어 모든 프레임 16ms 이하를 입증하지 못했습니다. 초기 Unity 이름 읽기·내부 동작의 0할당도 보장하지 않습니다. 총알 자국은 새 등록 횟수를 제한하며 이미 생긴 자국을 지우지 않습니다. Off는 저장한 설정·렌더러·광원을 복원하지만 수명이 끝난 탄피나 생성하지 않은 자국을 되살리지는 않습니다.

**출처 선언:** 동작 근거는 아래 SPEC·CSV·허용 게임 DLL뿐입니다. 다른 모드 코드·이름 목록을 기억에서 옮기지 않았으며, 사용자가 지정한 금지 경로는 열지 않았습니다. git 커밋·푸시, 게임 폴더 쓰기, 게임·서버 실행, 하위 에이전트 호출은 하지 않았습니다. 전체 경로·입력 해시는 [evidence/source-manifest.json](evidence/source-manifest.json)에 기록했습니다.

**네트워크 관련 예외:** 초기 `ilspycmd` 호출에 자동 업데이트 확인 차단 옵션이 빠졌습니다. 자동 접속 시도 여부는 확인하지 못했습니다. 외부 모드 자료나 웹 응답을 읽거나 구현 근거로 사용하지는 않았습니다. 재현 스크립트는 `--disable-updatecheck`로 차단하며 핵심 게임 근거를 이 옵션으로 다시 추출했습니다.

<details>
<summary>읽은 파일·폴더 전체 범위</summary>

- 작업 루트: `SPEC.md`. `BRIEF.md`, `codex.log`는 이름·크기만 나열했으며 내용은 읽지 않았습니다.
- `scene-names/`: `names-bigmap.csv`, `names-factory4_day.csv`, `names-Interchange.csv`, `names-laboratory.csv`, `names-Lighthouse.csv`, `names-RezervBase.csv`, `names-Sandbox.csv`, `names-Shoreline.csv`, `names-TarkovStreets.csv`, `names-Woods.csv`, `types-Woods.csv`. `types-Woods.csv`는 앞 12줄만 확인했습니다. 나머지 types CSV는 파일명만 확인했습니다.
- `E:\SPT 4.1\EscapeFromTarkov_Data\Managed\`: `Assembly-CSharp.dll`, `UnityEngine.dll`, `UnityEngine.CoreModule.dll`, `UnityEngine.PhysicsModule.dll`, `UnityEngine.ParticleSystemModule.dll`, `netstandard.dll`. 나머지 Unity 관련 DLL은 파일명 목록만 확인했습니다.
- `E:\SPT 4.1\BepInEx\core\`: `BepInEx.dll`, `0Harmony.dll`을 빌드 참조로 읽었습니다. 폴더의 나머지 항목은 이름만 확인했습니다.
- 위 DLL에서 이 폴더에 새로 추출한 `evidence/`: `ClientLocalGameWorld.cs`, `ClientNetworkGameWorld.cs`, `DecalSystem.cs`, `DeferredDecalRenderer.cs`, `DeferredDecals.DeferredDecalRenderer.cs`, `EffectsController.cs`, `EFT.AssetsManager.AmmoPoolObject.cs`, `EFT.AssetsManager.LoadSceneOperation.cs`, `EFT.Ballistics.BallisticCollider.cs`, `EFT.CameraControl.CameraClass.cs`, `EFT.CameraControl.CameraManager.cs`, `EFT.CameraControl.OpticCameraManager.cs`, `EFT.CameraControl.OpticSight.cs`, `EFT.Firearms.cs`, `EFT.Interactive.InteractableObject.cs`, `EFT.Interactive.ObservedLootItem.cs`, `EFT.Interactive.WorldInteractiveObject.cs`, `EFT.Player.cs`, `EFT.PoolSystem.PoolableObject.cs`, `EFT.Shell.cs`, `EFTHardSettings.cs`, `GameWorld.cs`, `MuzzleLight.cs`, `MuzzleManager.cs`, `StaticDeferredDecal.cs`, `StaticDeferredDecalRenderer.cs`, `Systems.Effects.Effects.cs`, `game-types.txt`. 파일 전체를 출력하지 않고 필요한 구간을 검색·확인했습니다. 크기 0인 두 파일은 타입 검색 실패 기록입니다.
- 직접 만든 `src/SceneCleanup/`, `tests/`, `tools/`의 소스·프로젝트 파일, `data/`의 JSON·집계, `PLAN.md`, `INTEGRATION.md`, `NuGet.Config`, `obj/`, `tests/obj/`, `.dotnet-home/`의 빌드 생성 파일 및 검증 로그를 읽었습니다. 정확한 소스·도구 목록은 출처 JSON에 있습니다.
- 빌드 도구 기반 자료: `C:\Program Files\dotnet\sdk\10.0.401\NuGet.targets`, .NET SDK 10.0.401, .NET Framework v4.7.1 참조 어셈블리 폴더입니다. 실제 컴파일 참조 전체는 [evidence/compile-references.txt](evidence/compile-references.txt)에 있습니다. 시스템 NuGet.Config는 접근이 거부되어 내용 읽기에 실패했으며 로컬 전용 구성으로 빌드했습니다.
- 다음 파일은 작업 절차 지침으로만 읽었으며 동작·규칙·이름의 근거로 사용하지 않았습니다.

- `C:\Users\Real\.codex\plugins\cache\claude-cowork\superpowers\6.4.2\skills\using-superpowers\SKILL.md`
- `C:\Users\Real\.codex\plugins\cache\claude-cowork\superpowers\6.4.2\skills\brainstorming\SKILL.md`
- `C:\Users\Real\.codex\plugins\cache\claude-cowork\superpowers\6.4.2\skills\writing-plans\SKILL.md`
- `C:\Users\Real\.codex\plugins\cache\claude-cowork\superpowers\6.4.2\skills\test-driven-development\SKILL.md`
- `C:\Users\Real\.codex\plugins\cache\claude-cowork\superpowers\6.4.2\skills\verification-before-completion\SKILL.md`
- `C:\Users\Real\.codex\plugins\cache\claude-cowork\superpowers\6.4.2\skills\executing-plans\SKILL.md`
- `C:\Users\Real\.codex\plugins\cache\claude-cowork\superpowers\6.4.2\skills\systematic-debugging\SKILL.md`
- `C:\Users\Real\.codex\plugins\cache\claude-cowork\superpowers\6.4.2\skills\requesting-code-review\SKILL.md`
- `C:\Users\Real\.codex\plugins\cache\claude-cowork\superpowers\6.4.2\skills\using-superpowers\references\codex-tools.md`
- `C:\Users\Real\.agents\skills\reverse-engineer-anything\SKILL.md`
- `C:\Users\Real\.agents\skills\reverse-engineer-anything\references\native-and-artifacts.md`

</details>
