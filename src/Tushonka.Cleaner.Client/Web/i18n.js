/* 모든 문구를 로컬에서 한/영으로 전환합니다. */
(function () {
  'use strict';
  window.gameLanguage = window.gameLanguage ?? null;
  const strings = {
    brand: ['Tushonka Cleaner', 'Tushonka Cleaner'],
    pageHome: ['웹 설정', 'Web settings'],
    pageFirst: ['첫 실행 안내', 'First launch'],
    pageF12: ['F12 설정', 'F12 settings'],
    
    navigation: ['화면 이동', 'Navigation'],
    navDiag: ['진단 세부 설정', 'Diagnostic settings'], openDiag: ['진단 세부 설정 열기', 'Open diagnostic settings'], diagTitle: ['기록과 분석의 세부 값을 조정합니다.', 'Fine-tune logging and analysis.'], diagLead: ['보통은 바꿀 필요가 없습니다. 메모리 모드를 고르면 이 값들이 함께 바뀌며, 모드를 다시 고르면 직접 바꾼 값은 모드 기본값으로 돌아갑니다.', 'Most players never need these. Picking a memory mode sets them for you, and picking a mode again replaces your changes.'], diagSearch: ['설정 검색', 'Search settings'], diagSearchHint: ['예: 끊김, 간격, 서버', 'For example: stutter, interval, server'], diagEmpty: ['찾는 설정이 없습니다.', 'No settings match your search.'], diagBack: ['설정으로 돌아가기', 'Back to settings'],
    liveAlert: ['주의: 모드 B의 메모리가 레이드 중 계속 늘고 있습니다.', 'Warning: Mod B keeps using more memory during this raid.'],
    gfxLightUse: ['멀리 있는 작은 장식만 가립니다.', 'Hides small clutter only at a distance.'], gfxLightDoes: ['쓰레기, 잔해, 탄피 같은 장식이 먼 거리에서만 사라지고, 가까이 가면 다시 보입니다. 화면 정리용이며 대부분의 맵에서 프레임은 늘지 않습니다.', 'Litter, rubble, and casings disappear only far away and return as you get close. This tidies the view and does not raise FPS on most maps.'],
    gfxMoreUse: ['작은 장식을 거리와 관계없이 가립니다.', 'Hides small clutter at any distance.'], gfxMoreDoes: ['가까이 가도 장식이 보이지 않습니다. 파티클 계산과 부드러운 효과, 실시간 반사를 줄입니다.', 'Clutter stays hidden up close. Particle work, soft effects, and realtime reflections are reduced.'],
    gfxMostUse: ['조금 더 가림에 강한 그래픽 조정을 더합니다.', 'Adds stronger graphics reductions to the level above.'], gfxMostDoes: ['그림자가 단순해지고 조명 수가 줄며, 먼 모델이 더 빨리 단순해집니다. 맵의 풀도 그리지 않습니다. 봇의 시야 계산은 바뀌지 않습니다. 맵 모습이 가장 많이 달라지므로 프레임이 부족할 때 고릅니다.', 'Shadows get simpler, fewer lights are used, and distant models simplify sooner. Grass is not drawn. Bot vision does not change. The map changes the most, so choose this when FPS is short.'],
    gfxNoneUse: ['맵을 원래대로 봅니다.', 'Keeps the map as it is.'], gfxNoneDoes: ['아무것도 가리지 않고 그래픽도 바꾸지 않습니다.', 'Hides nothing and changes no graphics.'], gfxNoneEffect: ['효과: 없음', 'Effect: none'],
    gfxEffect: ['프레임 변화: {측정값}', 'FPS change: {측정값}'],
    graphicsLead: ['프레임 향상은 대부분 많이 가림에서 나옵니다. 게임 진행과 무관한 작은 장식은 화면 정리를 위해 가리며, 이것만으로는 대부분의 맵에서 프레임이 늘지 않습니다. 상자, 문, 아이템, 총알을 막는 물체는 가리지 않습니다.', 'Most of the FPS gain comes from the top level. Small decorations that do not affect play are hidden to tidy the view, which by itself does not raise FPS on most maps. Containers, doors, items, and anything that stops bullets stay visible.'],
    modeCompareOpen: ['모드별 차이 자세히 보기', 'See how the modes differ'],
    shortcutChange: ['바꾸기', 'Change'], shortcutClear: ['없음', 'None'], shortcutWaiting: ['키를 누르십시오 (Esc 취소)', 'Press a key (Esc cancels)'],
    f12MemoryHelp: ['메모리 모드를 고릅니다. 꺼짐을 고르면 이 기능만 꺼집니다.', 'Choose the memory mode. Off turns off only this feature.'], f12ServerHelp: ['서버 최적화 단계를 고릅니다.', 'Choose the server optimization level.'], f12GraphicsHelp: ['작은 장식을 가리는 정도를 고릅니다.', 'Choose how much small clutter to hide.'], f12PickHelp: ['아래에서 기능마다 단계를 따로 고를 수 있습니다.', 'You can set each feature separately below.'],
    navSettings: ['설정', 'Settings'], navLive: ['실시간 분석', 'Live analysis'], navReport: ['레이드 보고서', 'Raid report'], navHistory: ['지난 기록', 'History'], 
    
    tileGame: ['게임 메모리', 'Game memory'], tileGameNote: ['실제 RAM {ram} GB · 관리 메모리 {managed} GB', 'RAM in use {ram} GB · managed {managed} GB'],
    tileRam: ['시스템 여유 RAM', 'System free RAM'], tileRamNote: ['전체 {total} GB 중 {pct}%', '{pct}% of {total} GB'],
    tileCommit: ['튕김 한도까지 여유', 'Room before a crash'], tileCommitNote: ['한도 {limit} GB · 경고선 {line} GB', 'Limit {limit} GB · warning line {line} GB'],
    tileFps: ['현재 프레임', 'Current FPS'], tileFpsNote: ['레이드 평균 {avg} · 1% 저점 {low}', 'Raid average {avg} · 1% low {low}'],
    tileHitch: ['끊김 (이번 레이드)', 'Stutters (this raid)'], tileHitchNote: ['최대 {ms} ms · 이 모드 때문 {ours}회', 'Worst {ms} ms · {ours} caused by this mod'],
    tileVram: ['VGA 메모리', 'VRAM'], tileVramNote: ['그래픽카드 {card} GB · 공유 {shared} GB', 'Graphics card {card} GB · shared {shared} GB'],
    tileServer: ['SPT 서버 메모리', 'SPT server memory'], tileServerNote: ['실제 RAM {ram} GB · SPT.Server', 'RAM in use {ram} GB · SPT.Server'],
    tileServerBasic: ['SPT 서버', 'SPT server'], tileServerBasicNote: ['서버 메모리는 간단 확인 이상에서 표시합니다.', 'Server memory appears in Quick view and above.'],
    tileLeft: ['여유 예상', 'Time left'], tileLeftValue: ['2.1시간', '2.1 h'], tileLeftNote: ['RAM 부족까지 · 분당 -{drop}MB', 'Until low RAM · -{drop} MB/min'],
    tileRestart: ['재시작 판단', 'Restart advice'], tileRestartValue: ['약 3판 더', '~3 more raids'], tileRestartNote: ['지금 속도라면 3판 뒤 재시작을 권장합니다.', 'At this rate, restart after about 3 raids.'],
    tileOffline: ['게임이 꺼져 있어 값을 받지 못합니다.', 'The game is off, so no reading is available.'],
    chartMem: ['메모리 (GB, 최근 10분)', 'Memory (GB, last 10 min)'], chartFps: ['프레임 (FPS, 최근 10분)', 'FPS (last 10 min)'], chartCollecting: ['기록을 모으는 중입니다. 2초마다 한 점씩 쌓입니다.', 'Collecting data, one point every 2 seconds.'],
    minutesAgo: ['{n}분 전', '{n} min ago'], now: ['지금', 'Now'], seriesGame: ['게임 메모리', 'Game memory'], seriesFree: ['시스템 여유 RAM', 'System free RAM'], seriesServer: ['SPT 서버', 'SPT server'], seriesFps: ['프레임', 'FPS'], seriesAvg: ['평균 프레임', 'Average FPS'], seriesLow: ['1% 저점', '1% low'], seriesPeak: ['게임 최고 메모리', 'Game peak memory'],
    liveTitle: ['게임의 지금 상태를 자세히 봅니다.', 'See what the game is doing right now.'], liveLead: ['메모리, 프레임, 끊김 원인, 모드별 부담을 실시간으로 표시합니다.', 'Memory, FPS, stutter causes, and per-mod cost, updated live.'],
    liveNow: ['지금 수치', 'Current readings'], liveTrend: ['최근 10분 흐름', 'Last 10 minutes'], liveActions: ['직접 실행', 'Run manually'], liveActionsHelp: ['게임이 켜져 있을 때 바로 실행합니다.', 'Runs at once while the game is on.'], liveCauses: ['원인 분석', 'Cause analysis'],
    liveDetail: ['자세한 상태', 'Full status'], liveDetailHint: ['기록용 전체 값을 글로 봅니다.', 'Read every value as plain text.'], liveRefresh: ['이 페이지는 1초마다 게임에서 숫자를 받아 옵니다. 탭을 숨기면 5초마다로 줄어듭니다.', 'This page reads the game once a second, or every 5 seconds while the tab is hidden.'],
    actGc: ['메모리 정리', 'Clean memory'], actAssets: ['그림·모델 정리 (1~3초 멈춤)', 'Clean textures and models (1-3 s pause)'], actLeak: ['누수 기록 남기기', 'Take a leak snapshot'], actDone: ['{name}: 게임에 보냈습니다.', '{name}: sent to the game.'],
    barHitch: ['끊김 원인 (이번 레이드)', 'Stutter causes (this raid)'], barMods: ['모드별 프레임 부담 (ms/프레임)', 'Per-mod frame cost (ms/frame)'], barAlloc: ['모드별 메모리 생성 (MB/분)', 'Per-mod memory creation (MB/min)'], barHeavy: ['[실험] 처음 불러올 때 메모리를 많이 쓴 모드', '[Experimental] Mods using the most memory on first load'], barHeavyNote: ['여러 판에서 반복해서 큰 것만 믿습니다.', 'Trust only what stays large across raids.'],
    causeGc: ['메모리 정리', 'Memory cleanup'], causeLoad: ['맵·아이템 불러오기', 'Map and item loading'], causeBot: ['봇 등장', 'Bot spawning'], causeUnknown: ['원인 불명', 'Unknown'], modA: ['모드 A', 'Mod A'], modB: ['모드 B', 'Mod B'], modC: ['모드 C', 'Mod C'], modD: ['모드 D', 'Mod D'],
    needView: ['간단 확인, 집중 분석, 누수 추적에서 표시합니다.', 'Shown in Quick view, Deep analysis, and Leak hunt.'], needProfile: ['집중 분석, 누수 추적에서 표시합니다.', 'Shown in Deep analysis and Leak hunt.'], turnOnAnalysis: ['집중 분석 켜기', 'Turn on Deep analysis'], turnOnCheck: ['간단 확인 켜기', 'Turn on Quick view'], turnOnLeak: ['누수 추적 켜기', 'Turn on Leak hunt'], turnOnGame: ['자동 정리로 돌아가기', 'Back to Automatic cleanup'],
    bannerGame: ['기본 수치와 그래프만 표시합니다. 끊김 원인과 모드별 부담은 분석 모드에서 기록합니다.', 'Shows basic readings and charts. Stutter causes and per-mod cost need an analysis mode.'], bannerCheck: ['끊김 횟수와 원인, 레이드 보고서를 기록합니다. 모드별 부담은 집중 분석에서 봅니다.', 'Records stutters, their causes, and raid reports. Use Deep analysis for per-mod cost.'], bannerAnalysis: ['모드별 부담과 끊김 원인을 계속 기록합니다. 게임 부담: {측정값}', 'Records per-mod cost and stutter causes continuously. Game overhead: {측정값}'], bannerLeak: ['몇 분마다 모드별 메모리와 남은 물체를 확인합니다. 확인할 때 잠시 멈출 수 있습니다.', 'Checks per-mod memory and leftover objects every few minutes. Each check can pause the game briefly.'], bannerOff: ['메모리 기능이 꺼져 있어 기본 수치만 표시합니다.', 'The memory feature is off, so only basic readings appear.'],
    reportPageTitle: ['레이드마다 무엇이 달랐는지 비교합니다.', 'Compare what changed from raid to raid.'], reportLead: ['이번 세션의 레이드 기록입니다. 판이 끝날 때마다 저장합니다.', 'Raids from this session, saved after each raid.'], reportAuto: ['자동 새로고침 (레이드 중 10초마다)', 'Auto refresh (every 10 s in raid)'], reportPast: ['지난 세션 기록 보기', 'Past sessions'], reportUpdated: ['자동 갱신: {time}', 'Updated: {time}'], reportOffNote: ['지금 모드에서는 보고서를 기록하지 않습니다. 아래는 화면 예시입니다.', 'This mode does not record reports. The page below is an example.'],
    reportSummary: ['세션 요약', 'Session summary'], reportRaids: ['레이드별 요약', 'Raids'], reportCompare: ['판별 비교', 'Raid by raid'], reportEach: ['판별 자세히', 'Each raid'], sumRaids: ['레이드 수', 'Raids'], sumFps: ['평균 프레임', 'Average FPS'], sumHitch: ['끊김 합계', 'Total stutters'], sumPeak: ['게임 최고 메모리', 'Game peak memory'], sumFreed: ['정리한 메모리', 'Memory cleaned'], sumFpsNote: ['1% 저점 {low}', '1% low {low}'], sumHitchNote: ['최대 {ms} ms', 'Worst {ms} ms'], sumPeakNote: ['{map}', '{map}'], sumFreedNote: ['이번 세션 합계', 'This session'], sumRaidsNote: ['세션 시작 {time}', 'Session started {time}'],
    colStart: ['시작', 'Start'], colMap: ['맵', 'Map'], colMin: ['분', 'Min'], colAvg: ['평균 FPS', 'Avg FPS'], colLow: ['1% 저점', '1% low'], colHitch: ['끊김', 'Stutters'], colPeak: ['게임 최고(GB)', 'Game peak (GB)'], colFree: ['시스템 최저 여유(GB)', 'Lowest system free (GB)'], colDeaths: ['사망', 'Deaths'], colPerDeath: ['사망당(MB)', 'Per death (MB)'], colServer: ['서버(GB)', 'Server (GB)'], colKept: ['레이드 후 남은 메모리', 'Memory kept after the raid'], kept01: ['+0.4 GB', '+0.4 GB'], kept02: ['+0.2 GB', '+0.2 GB'], kept03: ['+0.6 GB', '+0.6 GB'],
    mapCustoms: ['세관', 'Customs'], mapStreets: ['스트리트', 'Streets'], mapLighthouse: ['등대', 'Lighthouse'], raidHeading: ['{n}판 · {map} · {min}분', 'Raid {n} · {map} · {min} min'], shortestLeft: ['가장 짧았던 여유 예상', 'Shortest time left'], shortestLeftNote: ['메모리 한도까지', 'Until the memory limit'], perDeath: ['사망당 {mb} MB', 'Per death {mb} MB'], chartMemRaid: ['메모리 (GB)', 'Memory (GB)'], chartFpsRaid: ['프레임 (10초 평균)', 'FPS (10 s average)'], chartCmpFps: ['평균 프레임 (판별)', 'Average FPS per raid'], chartCmpMem: ['게임 최고 메모리 (GB, 판별)', 'Game peak memory per raid (GB)'], raidLabel: ['{n}판', 'Raid {n}'], minuteLabel: ['{n}분', '{n} min'], notesTitle: ['메모', 'Notes'], note1: ['레이드 시작 3분 동안 끊김이 몰렸습니다.', 'Stutters clustered in the first 3 minutes.'], note2: ['메뉴로 돌아온 뒤 메모리 0.9 GB를 정리했습니다.', 'Cleaned 0.9 GB after returning to the menu.'],
    heavyTitle: ['[실험] 처음 불러올 때 메모리를 많이 쓴 모드', '[Experimental] Mods using the most memory on first load'], heavyHelp: ['집중 분석, 누수 추적에서 기록합니다. 여러 판에서 반복해서 큰 것만 믿습니다.', 'Recorded in Deep analysis and Leak hunt. Trust only what stays large across raids.'], colMod: ['모드', 'Mod'], colMbLoad: ['1회당 MB', 'MB per load'], colLoads: ['불러온 횟수', 'Loads'],
    leakTitle: ['누수 범인 후보', 'Leak culprits'], leakHelp: ['누수 추적에서 기록합니다. 같은 맵으로 2판 이상 돌린 뒤 보는 것이 정확합니다.', 'Recorded in Leak hunt. Most reliable after two or more raids on the same map.'], colVerdict: ['판정', 'Verdict'], colEvidence: ['근거', 'Evidence'], verdictSure: ['누수 확실', 'Leaking'], verdictSuspect: ['누수 의심', 'Suspect'], verdictHint: ['참고', 'Hint'], evidenceSure: ['레이드가 끝난 뒤에도 지난 레이드의 물체를 붙잡고 있습니다.', 'Still holds objects from the finished raid.'], evidenceSuspect: ['레이드 중 물체 수가 계속 늘었습니다.', 'Its object count kept growing during the raid.'], evidenceHint: ['메모리를 많이 만들지만 대부분 곧 치워집니다.', 'Creates a lot of memory, but most is cleaned up soon.'],
    historyTitle: ['지난 세션의 보고서를 다시 봅니다.', 'Open reports from earlier sessions.'], historyLead: ['게임을 켠 동안을 세션 하나로 봅니다. 레이드가 끝날 때마다 저장하고 최근 15개까지 보관합니다.', 'Each game launch is one session. Reports are saved after every raid, and the latest 15 are kept.'], historyList: ['저장된 보고서', 'Saved reports'], colSession: ['세션 시작', 'Session started'], colSaved: ['마지막 저장', 'Last saved'], thisSession: ['이번 세션', 'This session'], historyEmptyNote: ['저장 위치: BepInEx\\RamCleaner\\ · 저장된 보고서가 없으면 레이드 한 판이 끝난 뒤에 생깁니다.', 'Saved in BepInEx\\RamCleaner\\. A report appears after your first finished raid.'],
    advMemoryHint: ['메모리 정리의 세부 기준입니다.', 'Detailed memory cleanup options.'], advServerHint: ['서버 최적화의 항목별 설정입니다.', 'Individual server optimization options.'], advGraphicsHint: ['가릴 물체와 그래픽 조정 항목입니다.', 'What to hide and which graphics to adjust.'], advReset: ['이 기능을 기본값으로', 'Reset this feature'], advResetDone: ['기본값으로 되돌렸습니다.', 'Reset to defaults.'], advModeNote: ['기록·진단 항목은 위의 사용 모드가 정합니다.', 'The mode above decides which logs and diagnostics run.'],
    grpGc: ['자동 메모리 정리', 'Automatic memory cleanup'], grpAssets: ['그림·모델 정리 (VGA)', 'Texture and model cleanup (VRAM)'], grpTiming: ['전투 중 미루기', 'Wait during combat'], grpAfter: ['레이드 뒤 정리', 'After-raid cleanup'], grpWarn: ['메모리 위험 알림', 'Memory warnings'], grpForecast: ['남은 시간 예측', 'Time-left forecast'], grpFixes: ['게임 메모리 보정', 'Game memory fixes'], grpOverlay: ['왼쪽 위 상태 표시', 'Top-left status display'], grpWeb: ['웹 페이지', 'Web page'], grpModes: ['모드별 동작 비교', 'Compare mode behavior'],
    optGcGap: ['자동 정리 최소 간격 (초)', 'Minimum gap between cleanups (s)'], optGcBlocking: ['단계 정리가 안 될 때 전체 정리 허용', 'Allow full cleanup when stepped cleanup is unavailable'], optGcBlockingHelp: ['전체 정리는 게임을 잠시 멈춥니다.', 'A full cleanup pauses the game briefly.'], 
    optAssetGc: ['정리 전에 메모리 정리 먼저', 'Clean memory first'], optQuiet: ['조용한 순간 기준 (초)', 'Quiet time needed (s)'], optDefer: ['메모리 정리 최대 대기 (초, 0은 무제한)', 'Longest cleanup wait (s, 0 means no limit)'], optInventory: ['인벤토리를 열면 밀린 정리 실행', 'Run pending cleanup when the inventory opens'], optAfterDelay: ['레이드 끝난 뒤 정리까지 대기 (초)', 'Wait after a raid before cleanup (s)'], optOnlyRaid: ['레이드 중에만 자동 실행', 'Run automatically only in raid'],
    optWarnOn: ['메모리 위험 경고 켜기', 'Turn on memory warnings'], optWarnPct: ['튕김 한도 여유 기준 (%)', 'Crash-limit room threshold (%)'], optVramWarn: ['VGA 메모리 가득 참 경고', 'VRAM full warning'], optVramPct: ['가득 참 기준 (%)', 'Full at (%)'], optVramSec: ['가득 찬 상태 지속 시간 (초)', 'Full for (s)'], optVramSpill: ['넘침 기준 (GB, 0은 확인 안 함)', 'Spill threshold (GB, 0 turns it off)'], optWarnGame: ['게임 안 알림으로 표시', 'Show in-game notifications'], optFcOn: ['남은 시간 예측 켜기', 'Turn on the forecast'], optFcMin: ['여유 경고 기준 (분)', 'Warn below (min)'], optFcRestart: ['재시작 권장 알림', 'Restart notifications'],
    optFixSpawn: ['레이드 끝난 봇 스폰 기록 놓아주기', 'Release bot spawn data after the raid'], optFixBrain: ['죽은 봇 AI 목록 비우기', 'Clear dead bot AI lists'], optFixCoroutine: ['봇마다 매 프레임 생기는 쓰레기 없애기', 'Remove per-bot garbage created every frame'], optOverlayOn: ['왼쪽 위 상태 표시 켜기', 'Show the top-left status display'], optOverlayFps: ['프레임 줄 표시', 'Show the FPS line'], optOverlayCount: ['막대 개수', 'Bar count'], optWebOn: ['웹 페이지 켜기', 'Turn on the web page'], optWebPort: ['포트 번호', 'Port'], optWebLan: ['같은 네트워크의 다른 기기 접속 허용', 'Allow other devices on the same network'],
    grpSrvResponse: ['응답 처리', 'Response handling'], grpSrvFix: ['오류 수정', 'Fixes'], grpSrvClient: ['게임 쪽 낭비 줄이기', 'Less waste in the game'], grpSrvLog: ['서버 기록', 'Server logging'], advServerNote: ['서버 단계를 고르면 아래 값이 함께 바뀝니다. 항목을 직접 바꾸면 단계가 직접 설정으로 바뀝니다.', 'Picking a server level updates these values. Changing one yourself switches the level to custom.'], serverSummaryCustom: ['직접 설정: 고급 설정에서 고른 항목만 켭니다.', 'Custom: only the options selected in Advanced settings are on.'],
    optS9On: ['응답 압축 빠르게', 'Faster response compression'], optS9Level: ['압축 수준', 'Compression level'], lvlFast: ['빠름', 'Fastest'], lvlBalanced: ['균형', 'Optimal'], lvlSmall: ['최소 크기 (원래 방식)', 'Smallest size (original)'], lvlNone: ['압축 안 함', 'No compression'], optS12: ['봇 장비 오류 수정', 'Fix bot gear errors'], optC1: ['숨겨진 메뉴 영상 멈추기', 'Pause hidden menu videos'], optC2: ['착지한 에어드랍 물리 멈추기', 'Stop physics on landed airdrops'], optDebug: ['서버 기록 켜기', 'Turn on server logging'], optDebugGap: ['요약 기록 간격 (분)', 'Summary interval (min)'], optDebugSlow: ['느린 요청 기준 (ms)', 'Slow request threshold (ms)'], optDebugKeep: ['보관할 파일 수', 'Files to keep'],
    grpGfxHide: ['가릴 물체', 'What to hide'], grpGfxTune: ['그래픽 조정', 'Graphics adjustments'], grpGfxCaps: ['개수 상한', 'Count limits'], grpGfxRun: ['작동 방식', 'How it runs'], gfxTuneNote: ['조금 더 가림, 많이 가림에서 적용합니다. 끄면 게임 원래 설정을 씁니다.', 'Applies to the two stronger levels. Turning one off keeps the game setting.'], gfxLitter: ['바닥 쓰레기', 'Floor litter'], gfxRubble: ['잔해 더미', 'Rubble piles'], gfxCasings: ['맵에 고정된 탄피', 'Map-placed casings'], gfxFood: ['먹을 수 없는 장식 음식', 'Decorative food'], gfxPuddles: ['물웅덩이', 'Puddles'], gfxGlass: ['깨진 유리 조각', 'Broken glass'], gfxParticles: ['파티클 부담 줄이기', 'Reduce particle cost'], gfxSoftParticles: ['부드러운 파티클 끄기', 'Turn off soft particles'], gfxSoftVeg: ['부드러운 식생 끄기', 'Turn off soft vegetation'], gfxReflect: ['실시간 반사 끄기', 'Turn off realtime reflections'], gfxShadows: ['그림자 품질 줄이기', 'Reduce shadow quality'], gfxLights: ['픽셀 조명 수 줄이기', 'Reduce pixel lights'], gfxLod: ['먼 모델 단순화 (많이 가림만)', 'Simplify distant models (top level only)'], gfxGrass: ['풀 끄기 (많이 가림만)', 'Turn off grass (top level only)'], gfxMuzzle: ['다른 사람의 총구 화염 그림자 끄기', "Turn off other players' muzzle flash shadows"], gfxShellCap: ['버려진 탄피 수 제한', 'Limit ejected shells'], gfxDelay: ['레이드 시작 후 검사 대기 (초)', 'Wait after raid start (s)'], gfxBudget: ['프레임당 작업 시간 (ms)', 'Work per frame (ms)'], gfxDistance: ['간단하게만 가림 기준 거리 (m)', 'Distance for the lightest level (m)'], gfxDistanceHelp: ['조준 배율에 따라 자동으로 늘어납니다.', 'Grows automatically with scope zoom.'], gfxChanges: ['조준 때 한 프레임에 바꿀 물체 수', 'Objects changed per frame while aiming'], gfxExclude: ['제외할 이름은 tushonka-exclude.txt에 한 줄씩 적습니다.', 'List names to exclude in tushonka-exclude.txt, one per line.'],
    language: ['언어', 'Language'], auto: ['자동', 'Auto'], korean: ['한국어', '한국어'], english: ['English', 'English'],
    javascriptRequired: ['설정 화면을 사용하려면 JavaScript를 켜야 합니다.', 'Enable JavaScript to use settings.'],
    example: ['예시', 'Example'], proposal: ['구성 제안', 'Proposed setup'],
    selected: ['선택됨', 'Selected'],
    homeTitle: ['원하는 기능만 골라 사용합니다.', 'Choose what works for your raids.'],
    homeIntro: ['메모리 및 VGA, 서버, 그래픽을 한곳에서 설정합니다.', 'Set up Memory & VRAM, Server, and Graphics in one place.'],
    
    quickTitle: ['빠른 설정', 'Quick setup'],
    quickIntro: ['권장으로 모두 켜거나, 원하는 기능만 직접 고릅니다.', 'Turn everything on with Recommended, or pick just the features you want.'],
    recommended: ['권장', 'Recommended'], offPreset: ['끄기', 'Turn off'], custom: ['직접 설정', 'Custom settings'], suggested: ['메모리와 서버를 한 번에 켭니다.', 'Memory and server, one click.'],
    recommendedDesc: ['메모리와 서버를 권장값으로 켭니다. 그래픽은 바꾸지 않습니다.', 'Enable memory and server at recommended values. Graphics stay as they are.'],
    offDesc: ['세 기능과 분석·알림을 끕니다. 웹을 열어 둔 동안 기본 수치는 계속 표시합니다.', 'Turn off all three features, analysis, and alerts. Basic readings remain visible while this page is open.'],
    pickFeatures: ['사용할 기능을 고릅니다. 함께 골라도 됩니다.', 'Choose your features. Pick one or both.'],
    pickHelp: ['각 선택은 독립적입니다. 개별 기능을 켜면 그 기능만 권장값으로 시작합니다.', 'Each choice is independent. Turning a feature on starts it at its recommended settings.'],
    memFeature: ['메모리 및 VGA', 'Memory & VRAM'], serverFeature: ['서버', 'Server'], graphicsFeature: ['그래픽', 'Graphics'],
    quickMemory: ['자동 정리로 시작합니다.', 'Start with automatic cleanup.'], quickServer: ['안전한 최적화를 켭니다.', 'Enable the safe optimizations.'], quickGraphics: ['취향을 많이 타서 권장이 없습니다. 비교해 보고 그래픽 탭에서 고릅니다.', 'A matter of taste, so no recommendation. Compare, then choose in the Graphics tab.'],
    currentSetup: ['현재 설정', 'Current setup'], currentFeatures: ['선택한 기능: {names}', 'Selected: {names}'], noneSelected: ['선택한 기능이 없습니다.', 'No features selected.'],
    setupApplied: ['{name} 설정을 적용했습니다.', '{name} settings applied.'], changed: ['설정을 저장했습니다.', 'Settings saved.'],
    offSetup: ['모두 꺼짐', 'All off'], allDisabled: ['세 기능을 모두 껐습니다.', 'All three features are off.'],
    localOnly: ['이 브라우저에만 선택을 기억합니다.', 'Choices are saved only in this browser.'],
    storageUnavailable: ['브라우저 저장이 차단되어 있습니다. 화면을 닫으면 선택과 안내 기록이 사라질 수 있습니다.', 'Browser storage is blocked. Your choices and dismissed prompts may reset when you leave.'],
    statusTitle: ['지금 상태', 'At a glance'], features: ['사용하는 기능', 'Enabled features'], featureCount: ['{count} / 3개', '{count} / 3'], on: ['켜짐', 'On'], off: ['꺼짐', 'Off'],
    gameMemory: ['게임 메모리', 'Game memory'], currentFrames: ['현재 프레임', 'Current frame rate'], serverStatus: ['서버 상태', 'Server status'], connected: ['연결됨', 'Connected'],
    raidFreed: ['이번 판 정리한 메모리', 'Memory freed this raid'],
    raidFreedValue: ['1.2 GB', '1.2 GB'], raidFreedHelp: ['이번 레이드에서 정리한 메모리의 합계입니다.', 'Memory cleaned during this raid.'],
    memoryValue: ['12.4 GB', '12.4 GB'], framesValue: ['84 FPS', '84 FPS'], serverMemory: ['서버 메모리: {측정값} GB', 'Server memory: {측정값} GB'],
    vramUsage: ['VGA 메모리 사용량', 'VRAM usage'], vramValue: ['6.2 / 8 GB', '6.2 / 8 GB'],
    vramHelp: ['VRAM은 그래픽카드의 메모리입니다. 게임이 쓰는 그래픽카드의 전체 사용량을 표시합니다. 다른 프로그램이 쓰는 양도 포함합니다.', 'VRAM is your graphics card’s memory. This shows total usage on the card running the game, including other programs.'],
    basicReadings: ['웹을 열어 둔 동안 메모리·프레임·VRAM의 기본 수치를 표시합니다. 분석 모드를 켤 필요는 없습니다.', 'Basic memory, FPS, and VRAM readings stay visible while this page is open. No analysis mode is needed.'],
    lastReadingExample: ['마지막으로 확인한 값의 예시입니다.', 'Example of the last recorded reading.'],
    waitingGame: ['게임을 켜면 표시합니다.', 'Available when the game is running.'], trackingOff: ['측정·알림이 꺼져 있습니다.', 'Monitoring and alerts are off.'],
    measurementExample: ['현재 값의 예시입니다.', 'Example current reading.'],
    onlineBanner: ['게임이 켜진 상태의 예시입니다. 실제 적용 시점은 각 기능 옆에 표시합니다.', 'Previewing a running game. Each feature shows when its changes take effect.'],
    
    tabLabel: ['기능별 세부 설정', 'Feature details'], gameTimingOn: ['게임 실행 중 적용', 'Applies while running'], gameTimingOff: ['다음 게임 실행 때 적용', 'Applies next game launch'], nextRaid: ['일부 적용: 다음 레이드', 'Some changes: next raid'],
    memoryLead: ['메모리 정리 모드와 그래픽 자원 정리를 설정합니다. 웹의 기본 수치는 모드와 관계없이 표시합니다.', 'Choose a memory cleanup mode and when to unload graphics resources. Basic web readings are visible in every mode.'],
    modeLegend: ['메모리 모드', 'Memory mode'], modeIntro: ['권장은 자동 정리입니다. 끊김이나 메모리 문제를 찾을 때만 다른 모드로 바꿉니다.', 'Automatic cleanup is recommended. Switch only when looking for stutters or memory problems.'],
    modeGame: ['자동 정리(게임 모드)', 'Automatic cleanup (game mode)'], modeCheck: ['간단 확인', 'Quick check'], modeAnalysis: ['집중 분석', 'Deep analysis'], modeLeak: ['누수 추적', 'Leak tracking'], modeOff: ['꺼짐', 'Off'],
    modeGameUse: ['평소 게임할 때 사용합니다.', 'For everyday raids.'],
    modeGameDoes: ['자동 정리와 메모리 부족 알림을 켭니다. 게임 안 상태 표시와 분석은 끄고, 웹의 기본 수치는 계속 표시합니다.', 'Cleanup and low-memory alerts stay on. The in-game overlay and analysis stay off. Basic readings remain visible on the web.'],
    modeCheckUse: ['플레이하면서 상태만 살펴볼 때 사용합니다.', 'For keeping an eye on things while you play.'],
    modeCheckDoes: ['정리·알림에 왼쪽 위 상태 표시와 레이드 기록을 더합니다.', 'Adds a top-left overlay and raid reports to cleanup and alerts.'],
    modeAnalysisUse: ['끊김을 일으키는 원인을 찾을 때 사용합니다.', 'For tracking down stutters.'],
    modeAnalysisDoes: ['모드별 부담과 끊김 원인을 계속 기록합니다. 누수 추적은 끕니다.', 'Continuously records mod overhead and stutter causes. Leak tracking stays off.'],
    modeLeakUse: ['레이드가 끝날 때마다 메모리가 쌓일 때 사용합니다.', 'For memory that keeps building up between raids.'],
    modeLeakDoes: ['몇 분마다 모드별 메모리와 남은 물체를 확인합니다. 끊김 원인 추적은 끕니다.', 'Checks mod memory and lingering objects every few minutes. Stutter tracing stays off.'],
    modeOffUse: ['이 기능을 사용하지 않을 때 고릅니다.', 'When you want this feature off.'],
    modeOffDoes: ['메모리·자원 정리와 게임 안 상태 표시·분석·알림을 끕니다. 웹의 기본 수치는 계속 표시합니다.', 'Disables memory and resource cleanup, the in-game overlay, analysis, and alerts. Basic web readings remain visible.'],
    modeCost: ['게임 부담: {측정값}', 'Game overhead: {측정값}'],
    modeCaution: ['누수 추적은 도중에 끊길 수 있습니다. 같은 맵에서 두 판 정도 확인한 뒤 자동 정리로 돌아갑니다.', 'Leak tracking can cause hitches. Check about two raids on the same map, then switch back to automatic cleanup.'],
    analysisCaution: ['집중 분석을 끝내면 측정을 멈춥니다. 분석 준비 작업까지 완전히 없애려면 게임을 다시 켭니다.', 'Leaving Deep analysis stops recording. Restart the game to fully remove its instrumentation.'],
    waitCombat: ['전투 중에는 미루기', 'Wait while fighting'], waitHelp: ['사격·조준 직후에는 정리를 미룹니다. 너무 오래 기다리면 실행할 수 있습니다.', 'Delay cleanup after shooting or aiming. It can still run after a long wait.'],
    graphicsResources: ['쓰지 않는 그래픽 자원 정리', 'Unload unused graphics resources'],
    graphicsResourcesHelp: ['쓰지 않는 그림·모델을 메모리에서 내립니다. 정리할 때 잠시 멈출 수 있으며, 비워지는 양은 상황에 따라 다릅니다.', 'Unload textures and models that are no longer in use. This can briefly freeze the game. The amount freed varies.'],
    raidStartAssets: ['레이드 시작 때 정리', 'Clean up at raid start'],
    raidStartAssetsHelp: ['메뉴와 로딩에서 쓰고 남은 그림·모델을 한 번 정리합니다. 시작 직후 멈출 수 있어 권장에서는 끕니다.', 'Unload textures and models left over from menus and loading once. Recommended leaves this off because it can freeze the game just after a raid starts.'],
    afterRaid: ['메뉴 복귀 때 정리', 'Clean up on return to menu'], afterRaidHelp: ['레이드가 끝나고 메뉴로 돌아오면 쓰지 않는 그림·모델과 메모리를 정리합니다. 다음 로딩 때 일부를 다시 읽을 수 있습니다. 권장에서는 켭니다.', 'Unload unused textures and models and clean up memory after returning from a raid. Some resources may need to load again next time. Recommended keeps this on.'],
    cleanNow: ['지금 정리', 'Clean up now'], requiresGame: ['게임과 메모리 및 VGA 기능이 켜져 있어야 합니다.', 'Requires the game and Memory & VRAM to be on.'], cleanupPreview: ['정리한 메모리: 1.2 GB · 예시입니다.', 'Memory freed: 1.2 GB · Example.'],
    serverLead: ['안전한 최적화를 켜거나, 원래 서버 동작으로 돌아갑니다.', 'Enable the safe optimizations or keep the original server behavior.'],
    serverLegend: ['서버 단계를 고릅니다.', 'Choose a server level.'], serverRecommended: ['모든 안전한 최적화를 켭니다.', 'Enable all safe optimizations.'], serverOff: ['추가 최적화를 끄고 원래 서버 동작을 사용합니다.', 'Disable the added optimizations and use the original server behavior.'],
    serverSummaryRecommended: ['권장: 응답 압축 단축, 봇 장비 오류 수정, 게임 쪽 낭비 줄이기를 켭니다.', 'Recommended: faster response compression, the bot gear fix, and less waste in the game.'],
    
    serverSummaryOff: ['끄기: 추가 서버 최적화를 끄고 원래 서버 동작을 유지합니다.', 'Off: disable the added server optimizations and keep the original server behavior.'],
    serverProposal: ['효과·부담: {측정값}입니다.', 'Benefits and overhead: {측정값}.'],
    serverAvailable: ['서버 연결 중', 'Server connected'],
    
    graphicsAmount: ['가리는 정도', 'How much to hide'], graphicsAmountHelp: ['취향을 많이 타는 설정이라 권장 단계를 두지 않습니다. 맵별 성능과 화면을 보고 고릅니다. 강할수록 맵 모습도 많이 달라집니다.', 'This comes down to taste, so there is no recommended level. Compare performance and visuals per map, then choose. Stronger levels change the map more.'], gfxCompareLink: ['맵별 성능·화면 비교 보기 ↗', 'See per-map performance and visuals ↗'],
    graphicsNone: ['가리지 않음', 'Hide nothing'], graphicsLight: ['간단하게만 가림', 'Light touch'], graphicsMore: ['조금 더 가림', 'Hide a bit more'], graphicsMost: ['많이 가림', 'Hide more clutter'],
    simplerGraphics: ['반사·그림자도 줄이기', 'Reduce reflections and shadows too'], graphicsHelp: ['별도로 켤 수 있습니다. 화면 품질이 달라질 수 있습니다.', 'An optional extra. This can change visual quality.'],
    marksTiming: ['낙서·얼룩은 다음 레이드부터 바뀝니다. 다른 작은 장식은 레이드 중에도 바꿀 수 있습니다.', 'Graffiti and stain changes take effect next raid. Other small clutter can change during a raid.'],
    advanced: ['고급 설정', 'Advanced settings'], advancedHint: ['세부 기준과 모드별 차이를 확인합니다.', 'Fine-tune settings and compare modes.'],
    advancedMemory: ['메모리 및 VGA 세부 기준', 'Memory & VRAM details'], exampleSettings: ['아래 숫자는 조정 방식의 예시이며 성능 측정값이 아닙니다.', 'The numbers below are example settings, not performance measurements.'],
    growth: ['GC 시작 기준: 메모리 증가량 (GB)', 'GC trigger: memory growth (GB)'], growthHelp: ['GC는 쓰지 않는 메모리를 치우는 작업입니다. 낮추면 더 자주 정리합니다.', 'GC clears unused memory. A lower threshold runs cleanup more often.'],
    gcBudget: ['GC 프레임당 작업 시간 (ms)', 'GC work per frame (ms)'], budgetHelp: ['한 화면을 그리는 동안 정리에 쓸 시간입니다. 마지막 작업은 끊김을 일으킬 수 있습니다.', 'Time allowed for cleanup during each frame. The final step can still cause a hitch.'],
    
    
    modeCompare: ['모드별 동작 비교', 'Compare mode behavior'], comparisonHint: ['현재 모드는 강조해서 표시합니다. 좁은 화면에서는 표를 옆으로 움직여 봅니다.', 'The current mode is highlighted. On narrow screens, scroll the table sideways.'],
    compareSetting: ['하는 일', 'What it does'], compareCleanup: ['자동 정리·위험 알림', 'Cleanup and low-memory alerts'], compareOverlay: ['왼쪽 위 상태 표시', 'Top-left overlay'], compareReports: ['끊김 확인·레이드 기록·보고서', 'Stutter checks, raid records, reports'], compareMod: ['모드별 부담 확인', 'Check mod overhead'], compareCause: ['끊김 원인 추적·요약 기록', 'Trace stutters and write summaries'], compareHeavy: ['무거운 아이템 확인(실험)', 'Check heavy items (experimental)'], compareLeak: ['판마다 남은 메모리·물체 추적', 'Track memory and objects left between raids'], compareLog: ['상태 기록 간격', 'Status logging interval'], compareCost: ['게임 부담', 'Game overhead'],
    yes: ['켬', 'On'], no: ['끔', 'Off'], continuous: ['계속 확인합니다.', 'Continuously'], periodic: ['몇 분마다 확인합니다.', 'Every few minutes'], leakOverlay: ['상태만 표시합니다.', 'Status only'], noStutterAlert: ['기록은 켭니다. 끊김 알림만 끕니다.', 'Records on; stutter alerts off.'], seconds120: ['120초', '120 seconds'], seconds30: ['30초', '30 seconds'], seconds60: ['60초', '60 seconds'], measure: ['{측정값}', '{측정값}'],
    advancedGraphics: ['그래픽 세부 기준', 'Graphics details'], decals: ['맵 데칼 숨기기', 'Hide map decals'], decalsHelp: ['데칼은 낙서·얼룩 같은 표면 그림입니다. 변경은 다음 레이드부터 적용합니다.', 'Decals are surface details such as graffiti and stains. Changes apply next raid.'], experimentNote: ['물리·날씨·풀 거리 등 실험 기능은 권장 설정에서 끕니다.', 'Recommended leaves experimental physics, weather, and grass-distance changes off.'],
    advancedServer: ['서버 단계별 세부 설정', 'Server level details'], serverTableHint: ['고른 단계에 따라 아래 구성을 함께 바꿉니다. 끄기는 원래 동작을 뜻하며, 압축을 없애는 설정이 아닙니다.', 'Choosing a level updates the settings below together. Off means original behavior; it does not mean uncompressed responses.'],
    
    serverS9: ['S9 · 응답 압축 수준', 'S9 · Response compression level'], serverS9Help: ['전송 자료의 크기를 줄이는 작업입니다. 빠름은 처리 속도를, 균형은 자료 크기와 처리 부담을 함께 고려합니다.', 'Compression shrinks outgoing data. Fast favors processing speed; Balanced also considers transfer size.'],
    serverS12: ['S12 · 봇 장비 오류 수정', 'S12 · Bot gear fix'], serverS12Help: ['봇 장비 처리의 오류를 수정합니다.', 'Fixes an error in bot gear handling.'], 
    
    
    original: ['원래 동작', 'Original behavior'], fast: ['빠름', 'Fast'], compressionBalanced: ['균형', 'Balanced'], background: ['뒤에서 정리', 'Background cleanup'], preserveSaves: ['정상 저장 유지', 'Keep normal saving'],
    diagnostics: ['문제 찾기', 'Troubleshooting'], diagnosticHint: ['확인·분석 모드와 레이드 보고서를 봅니다.', 'Check monitoring modes and raid reports.'], currentMode: ['현재 메모리 모드', 'Current memory mode'], diagnosticsHelp: ['메모리 및 VGA에서 확인·분석 모드를 고릅니다. 여기서 고른 모드도 같은 선택에 반영됩니다.', 'Choose a monitoring mode under Memory & VRAM, or select one here. Both controls use the same mode.'],
    openMemory: ['메모리 모드 보기', 'View memory modes'], 
    firstBadge: ['처음 한 번만 안내합니다.', 'A one-time setup.'], firstTitle: ['Tushonka Cleaner를 설정합니다.', 'Set up Tushonka Cleaner.'], firstIntro: ['권장을 누르면 세 기능을 함께 켭니다. 직접 고르면 원하는 기능만 켭니다.', 'Use Recommended to turn on all three features, or choose the ones you want.'],
    firstMemoryDesc: ['게임 중 메모리를 정리하고 부족할 때 알립니다.', 'Clean up memory while you play and warn when it runs low.'], firstServerDesc: ['안전한 서버 최적화를 함께 켭니다.', 'Enable the safe server optimizations.'], firstGraphicsDesc: ['그래픽을 조금 줄여 프레임을 올리고 작은 장식을 가립니다.', 'Lower graphics a little for more FPS and hide small clutter.'],
    webDiscovery: ['자세한 설정은 웹에서 엽니다.', 'Fine-tune your setup on the web.'], webDiscoveryDesc: ['이 안내의 웹 설정 열기 버튼이나 F12의 같은 버튼으로 엽니다.', 'Use Open web settings here or the same button in F12.'], webAddress: ['웹 설정 주소: /tushonka/ (자리표시자)', 'Web settings: /tushonka/ (placeholder)'],
    enableRecommended: ['메모리·서버 권장으로 켜기', 'Enable memory and server'], chooseMyself: ['직접 고르기', 'Let me choose'], later: ['나중에', 'Not now'], laterHelp: ['나중에를 눌러도 다시 묻지 않습니다. 현재 설정을 유지하며 이후 F12에서 웹 설정을 엽니다.', 'Not now keeps your current settings and dismisses this prompt for good. You can open web settings from F12 later.'], chooseTitle: ['사용할 기능을 고릅니다.', 'Choose your features.'], saveChoices: ['선택한 기능만 켜기', 'Enable my choices'], back: ['돌아가기', 'Back'], setupComplete: ['선택을 기억했습니다.', 'Your choices are saved.'], setupLater: ['현재 설정을 유지했습니다.', 'Your current settings are unchanged.'], noRepeat: ['이 안내는 다시 띄우지 않습니다. 이후에는 F12에서 웹 설정을 열어 바꿉니다.', 'This prompt will not appear again. Open web settings from F12 to make changes later.'], openWeb: ['웹 설정 열기', 'Open web settings'],
    
    
    
    close: ['닫기', 'Close'], 
    f12Title: ['자주 쓰는 설정만 남겼습니다.', 'Just the essentials.'], f12Intro: ['권장을 고르거나 기능마다 단계를 고릅니다. 자세한 설정은 웹에서 합니다.', 'Pick Recommended or set each feature. Open web settings for the details.'], f12Panel: ['F12 설정 · 예시', 'F12 settings · Example'], f12LanguageHelp: ['자동은 게임 언어를 따릅니다. 게임 언어가 없으면 브라우저 언어를 따릅니다.', 'Auto follows your game language, then falls back to your browser language.'],
    shortcut: ['메모리 모드 바꾸기 단축키', 'Cycle memory modes'], shortcutHelp: ['자동 정리 → 간단 확인 → 집중 분석 → 누수 추적 → 꺼짐 순서입니다. 서버·그래픽 선택은 유지합니다.', 'Cycle Automatic cleanup → Quick check → Deep analysis → Leak tracking → Off. Server and Graphics stay as chosen.'], noKey: ['지정하지 않음', 'Unbound'], shortcutDemo: ['단축키 설정 예시입니다.', 'Example shortcut settings.']
  };

  let preference = (window.TC_BOOT || {}).language || 'auto';
  const language = () => (window.TC_BOOT || {}).resolvedLanguage || 'en';
  function t(key, values = {}, context = '') {
    if (!strings[key]) throw new Error('Missing translation: ' + key);
    let result = strings[key][language() === 'ko' ? 0 : 1];
    if (result.includes('{측정값}')) {
      const measured = (window.TC_MEASURED || {})[context ? key + '.' + context : key];
      if (!measured) return '';
      const value = measured[language()];
      if (!value || String(value).includes('{측정값}')) return '';
      result = String(value);
    }
    Object.entries(values).forEach(([name, value]) => { result = result.replaceAll('{' + name + '}', String(value)); });
    return result;
  }
  function apply(root = document) {
    document.documentElement.lang = language();
    root.querySelectorAll('[data-i18n]').forEach(el => {
      const value = t(el.dataset.i18n, {}, el.dataset.measure || '');
      el.textContent = value;
      if (strings[el.dataset.i18n].some(x => x.includes('{측정값}'))) el.hidden = !value;
    });
    root.querySelectorAll('[data-i18n-aria]').forEach(el => el.setAttribute('aria-label', t(el.dataset.i18nAria)));
    root.querySelectorAll('[data-language]').forEach(el => { el.value = preference; });
  }
  window.TC_I18N = { strings, t, apply, language };
})();
