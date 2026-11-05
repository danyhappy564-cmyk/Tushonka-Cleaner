# Scene Cleanup 수정 1 결과

**결론:** 측정된 스트리트 계층의 장식이 모형 시험에서 후보가 되고, Off로 돌리면 정확히 복원됩니다. `tools/verify.ps1`은 통과했습니다. 게임에서는 아직 돌리지 않았습니다.

## 원인

보고된 두 증상 뒤에 원인이 일곱 가지 있었습니다.

| 증상 | 원인 | 조치 |
|---|---|---|
| 후보 0 | 조상 전체에 허용 목록을 적용해 `MultisceneSharedOccluder`에서 전부 탈락 | 조상 전용 판정 추가 |
| 후보 0 (가려졌던 문제) | 스캔이 `renderer.enabled`로 걸렀습니다. 게임이 꺼 둔 구역은 후보에서 빠져 스폰 위치에 따라 결과가 달라집니다 | 필터 제거 |
| 복원 오류 (가려졌던 문제) | 게임이 `Renderer.enabled`를 계속 씁니다. 숨겨도 게임이 다시 켜고, 복원하면 게임이 꺼 둔 물체를 켭니다 | 물체의 `activeSelf`로 숨김 |
| 데칼 0 | 규칙은 렌더러 이름 CSV로 만들었는데 `StaticDeferredDecal`은 렌더러가 없어 CSV에 없습니다. 이름이 맞을 근거가 없었습니다 | 타입으로 판정 |
| 데칼 0 | 단계를 첫 등록 때 고정했습니다. 맵 전에 불린 등록이 있으면 레이드 끝까지 그 단계로 남습니다 | 고정 제거, 등록 때의 단계 사용 |
| 데칼 0 | 조상 검사 탈락, 폴더 형제 32개 제한, 로딩 한 프레임의 2ms 예산 소진이 겹쳤습니다 | 데칼 전용 검사로 분리 |
| 데칼 개수 소실 위험 | 은신처 `Dispose`도 `EndRaid`를 불러 로딩 중 판정과 개수를 지울 수 있었습니다 | 은신처 인스턴스는 무시 |

## 바꾼 파일

| 파일 | 내용 |
|---|---|
| `src/SceneCleanup/SceneSafety.cs` | 조상 판정 분리, 그룹 루트에서 검사 종료, LOD 묶음 바깥 콜라이더 검사, 로딩 데칼 검사 |
| `src/SceneCleanup/RuntimeState.cs` | `activeSelf` 숨김, 스캔·적용 분리, 재검사 4개/프레임, 저장된 상자로 거리 계산, 데칼 단계 고정 제거 |
| `src/SceneCleanup/Core/Rules.cs` | `ContainerRules`, `MatchesName`, `DecalSizeLimit` |
| `src/SceneCleanup/Core/State.cs` | `DistancePolicy.SquaredToBox` |
| `tests/RuntimeChecks.cs`, `tests/RuntimeStubs.cs`, `tests/Program.cs` | 실제 계층 모형, 가림 공존, 데칼 로딩, 대규모 시험 |
| `INTEGRATION.md` | 데칼 정책과 상한 문구 |
| `evidence/fix1/`, `evidence/verification-fix1.log` | 새로 뽑은 게임 코드, 검증 로그 |

`GameHooks.cs`와 `data/scene-rules.json`은 바꾸지 않았습니다. `SceneSafety`의 `PlainNode`, `AncestorsSafe`, `SmallGroupSafe` 시그니처도 그대로라 기존 진단 도구가 쓰던 호출은 유지됩니다.

## 판단 근거가 된 게임 코드

| 지점 | 확인한 내용 | 판단 |
|---|---|---|
| `MultisceneSharedOccluder` | 설정값만 있고 로직이 없습니다 | 조상에 있어도 안전 |
| `PerfectCullingCrossSceneGroup.GetGroupRenderers` | 이 표시는 그룹 루트의 **직계 자식**에서만 읽습니다. 그룹은 씬 단위입니다 | 그룹 루트를 맵 구조의 경계로 사용. 위쪽은 검사하지 않음 |
| `GuidComponent`, `LocationScene.Awake` | 고유 번호, 씬별 목록 등록만 합니다 | 조상 허용 |
| `InteractableObject` 파생(`Door`, `LootableContainer`, `Switch`, `LootItem` 등), `ExfiltrationPoint` | 허용 목록에 없습니다 | 처음 보는 타입과 함께 보호 |
| `PerfectCullingBakeGroup.Toggle` | `Renderer.enabled`를 직접 씁니다. `UpdateSwitchQueues`가 매 프레임 호출합니다 | `enabled`로 숨기면 충돌 |
| `PerfectCullingBakeGroup.RuntimeSetForceRenderingOff` | `forceRenderingOff`도 씁니다 | 이 값도 충돌 |
| 어셈블리 전체 검색 | 가림 코드의 활성 상태 읽기는 베이크용뿐이고, `RendererObj`는 그룹 안 오류 기록에만 쓰입니다 | `activeSelf`는 게임 가림과 겹치지 않음 |
| `AutocullLODGroupCell`, `runtimeLodGroup`, `SetLODs` 호출부 | 게임의 LOD 코드는 `lodGroup.enabled`와 생성기만 만지고 렌더러 활성 상태는 쓰지 않습니다 | LOD 전환이 물체를 켜지 않음 |
| `StaticDeferredDecalRenderer.AddEarlyDecals`, `AddDecal` | 초기 등록은 한 프레임 반복문입니다. 등록마다 문자열 해시와 목록 선형 검색을 합니다 | 거부 판정이 등록보다 싸므로 예산으로 끊지 않음 |
| `ComputeDecalMinMaxPoints` | 단위 상자에 변환 행렬을 곱합니다 | `lossyScale`이 곧 데칼 크기 |
| `UpdateInstancesBuffers`, `ConstructQuadTree` | 데칼이 0개인 경우를 처리합니다 | 전부 거부해도 안전 |
| `TarkovApplication`의 `localGame.Run` 뒤 `OnGameStarted` | 맵 로딩이 레이드 시작 판정보다 먼저입니다 | 등록 훅은 `raidActive=false`에서 동작 |

## 상한 검토

- **후보 65,536:** 유지합니다. CSV 기준 최대 맵인 Streets의 비데칼 후보가 13,583개입니다. 게임 데칼 상한 60,000개도 판정 표에 들어갑니다.
- **스캔 시간:** 수정 전에는 프레임당 약 256단계라 노드 119만 개에 약 14,000프레임(60fps에서 약 4분)이 필요했습니다. 이제 2ms 예산이 끝날 때까지 돕니다. 렌더러 크기 조회도 이름이 맞는 물체만 합니다.
- **정상 상태 비용:** 수정 전에는 후보가 많으면 안전 검사가 매 프레임 2ms를 다 썼습니다. 이제 적용이 끝난 뒤의 재검사는 프레임당 4개입니다. LightTouch의 거리 계산은 네이티브 호출이 없습니다.
- **변경 속도:** 24개/프레임을 유지했습니다. 1.4만 개를 모두 숨기거나 복원하는 데 60fps에서 약 10초가 걸립니다.

## 시험 결과

`pwsh -NoProfile -File tools/verify.ps1` 종료 코드 0입니다. 로그는 `evidence/verification-fix1.log`입니다.

- Release net471 빌드 경고 0, 오류 0
- 규칙·저장소 시험 47개, Unity 모형 시험 93개 통과. CSV 78,821행과 집계 70개 일치. 규칙 재생성 해시 일치
- 모형에 측정된 계층을 그대로 만들었습니다. 그 물체는 후보가 되고, 문·상자 아래 물체, 콜라이더가 있는 물체, 콜라이더가 형제나 LOD 묶음 바깥에 있는 물체, 처음 보는 컴포넌트 아래 물체는 후보가 되지 않습니다.
- 코드를 일부러 망가뜨린 11건 중 10건을 시험이 잡았습니다. 원래 버그 재현도 그중 하나입니다. 남은 1건은 값 비교만 더 하는 변형이라 동작 차이가 없습니다.
- 기존 시험 기대값 하나를 바꿨습니다. `enabled=false` 렌더러도 후보가 되므로 후보 수가 1에서 2로 늘었습니다. 그 렌더러의 `enabled`는 바뀌지 않습니다.

## 게임 없이 확인 못 한 점

- **실제 후보 수:** 형제 검사와 새 바깥 콜라이더 검사는 그대로 엄격합니다. 조상 검사에 막혀 지금까지 이 단계까지 온 적이 없어 얼마나 걸러질지 모릅니다. 규칙 일치 16,033개에서 메시 데칼(CSV 기준 약 3,000개)을 뺀 약 1.3만 개가 상한입니다. `gdiag`의 `sibling:` 항목으로 확인해야 합니다.
- **데칼 0의 실제 원인:** 위 표의 데칼 항목은 코드를 읽어 찾은 결함입니다. 어느 것이 측정된 0을 만들었는지는 게임에서 가리지 못했습니다. 모두 고쳤습니다.
- **스트리트 외 맵:** 조상 구조 측정은 스트리트뿐입니다. 그룹 루트가 없는 계층에 다른 컴포넌트가 있으면 보호 쪽으로 탈락합니다.
- **Unity 네이티브 동작:** 비활성 물체를 LODGroup이 그리지 않는 점, `enabled=false` 렌더러의 `bounds`가 유효한 점은 어셈블리로 볼 수 없습니다. `gdiag`의 규칙 일치 수가 CSV와 비슷한 것은 두 번째 점의 간접 근거입니다.
- **프레임 시간:** `SetActive` 비용, 16ms 상한, 프레임 향상은 미측정입니다. 레이드 종료의 일괄 복원이 최대 1.4만 개로 늘어 그 프레임이 길어질 수 있습니다.
- **다른 스크립트가 쓰는 활성 상태:** 게임의 다른 스크립트가 숨긴 물체를 끄면 복원 때 켜집니다. 대상이 장식 이름이고 조상에 그런 스크립트가 있으면 탈락하므로 드물다고 판단합니다.
- **LOD 일부만 일치:** 묶음에서 규칙에 맞는 단계만 숨기므로 나머지 단계는 멀리서 보일 수 있습니다. 안전 문제는 아닙니다.

## 명세와 달라진 점 (승인 필요)

1. **데칼 대상:** 명세는 이름과 크기 규칙을 말하지만, 등록 데칼은 이름 자료가 없어 타입 전체(Streets 11,459개)를 대상으로 했습니다. 크기 상한 6m, 제외 파일, 분류 스위치, 상호작용 조상 보호는 적용합니다. Balanced에서 지워지는 범위가 예상보다 넓을 수 있습니다.
2. **로딩 프레임 예산:** 데칼 판정은 2ms 예산을 적용하지 않습니다. 추가 비용보다 줄어드는 등록 비용이 크다고 판단했으나 실측은 아닙니다.
3. **메시 데칼:** `decal_drip` 같은 일반 메시(Streets CSV 3,004개)는 지시대로 레이드 중 건드리지 않습니다. 등록 단계가 없어 이번에도 그대로 보입니다. 이 물체는 데칼 버퍼와 무관하므로 다른 장식처럼 숨길 수 있습니다. 숨길지는 결정이 필요합니다.

## 출처 선언

- 근거는 이 폴더의 `SPEC.md`, `RESULT.md`, `INTEGRATION.md`, `src/`, `data/`, `scene-names/`, `tests/`, `tools/`, `evidence/`와 `E:\SPT 4.1\EscapeFromTarkov_Data\Managed\Assembly-CSharp.dll`, 그리고 `dotnet build`가 참조한 같은 폴더의 DLL과 `BepInEx\core`입니다.
- `Assembly-CSharp.dll`은 `ilspycmd --disable-updatecheck`로 읽었습니다. 전체 디컴파일은 세션 임시 폴더에 두었고 필요한 타입만 `evidence/fix1/`에 남겼습니다.
- 금지 경로(`F:\코딩\레포\`, `Index\tools\decomp\`, 협업방의 이 폴더 밖, `Index\클로드방\`, `BepInEx\plugins\`, `BepInEx\config\`, `.claude\projects\` 아래 메모리와 대화 기록)와 `codex.log`는 열거나 검색하지 않았습니다. 인터넷에서 자료를 찾지 않았습니다.
- 세션 시작 때 도구가 자동으로 넣은 사용자 지침과 메모리 색인 한 줄 요약은 맥락에 들어왔습니다. 코드나 이름 목록은 없었고 구현 근거로 쓰지 않았습니다. 긴 명령 출력 일부를 도구가 `.claude\projects\` 아래에 자동 저장했으나 그 파일은 읽지 않았습니다.
- 게임 설치·실행, git 커밋·푸시, 하위 에이전트 호출은 하지 않았습니다.
