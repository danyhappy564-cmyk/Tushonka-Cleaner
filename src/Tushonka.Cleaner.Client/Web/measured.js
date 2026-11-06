/* 1.1.0 실측값입니다(2026-11-07). RTX 4080, 2560x1440, 봇 없음, 11개 맵 각 3곳.
 * 메모리 모드 부담은 1.0.0 측정값입니다.
 * 값이 없으면 해당 줄을 숨깁니다. */
window.TC_MEASURED = Object.freeze({
  'modeCost.game': { ko: '게임 부담: 측정 오차 안(±0.5ms)', en: 'Game overhead: within measurement noise (±0.5 ms)' },
  'modeCost.check': { ko: '게임 부담: 측정 오차 안(±0.5ms)', en: 'Game overhead: within measurement noise (±0.5 ms)' },
  'modeCost.analysis': { ko: '게임 부담: 측정 오차 안(±0.5ms)', en: 'Game overhead: within measurement noise (±0.5 ms)' },
  'modeCost.leak': { ko: '게임 부담: 측정 오차 안(±0.5ms). 스트리트에서는 시작 때 약 4초 멈춥니다.', en: 'Game overhead: within measurement noise (±0.5 ms). On Streets it pauses for about 4 seconds at the start.' },
  'modeCost.off': { ko: '게임 부담: 없음', en: 'Game overhead: none' },
  'compareCost.game': { ko: '오차 안', en: 'Within noise' },
  'compareCost.check': { ko: '오차 안', en: 'Within noise' },
  'compareCost.analysis': { ko: '오차 안', en: 'Within noise' },
  'compareCost.leak': { ko: '오차 안, 시작 때 약 4초 멈춤', en: 'Within noise, about 4 s pause at start' },
  'compareCost.off': { ko: '없음', en: 'None' },
  'gfxEffect.light': { ko: '프레임 변화: 거의 모든 자리에서 없음(연구소 한 곳만 +19%)', en: 'FPS change: none at nearly every spot (+19% at one Labs spot)' },
  'gfxEffect.more': { ko: '프레임 변화: 거의 모든 자리에서 -4~+5%(연구소 한 곳만 +17%). 봇 없는 측정이라 교전 중 효과는 재지 못했습니다.', en: 'FPS change: -4 to +5% at nearly every spot (+17% at one Labs spot). Measured without bots, so firefights are not covered.' },
  'gfxEffect.most': { ko: '프레임 변화: 해안·리저브·삼림·등대·연구소 +12~35%, 세관 +1~22%, 스트리트·그라운드 제로 +4~14%, 인터체인지 +2~9%, 공장·미궁 0~7%', en: 'FPS change: +12-35% on Shoreline, Reserve, Woods, Lighthouse and Labs; +1-22% on Customs; +4-14% on Streets and Ground Zero; +2-9% on Interchange; 0-7% on Factory and Labyrinth' },
  serverProposal: { ko: '서버 응답 압축(S9): 게임 시작 때 응답 9개 합계가 원래 636ms에서 균형 440ms로 줄었고 크기는 4.9MB로 같습니다. 빠름은 351ms지만 7.8MB로 커집니다.', en: 'Response compression (S9): nine start-up responses took 636 ms in total with the original setting and 440 ms with Balanced, at the same 4.9 MB. Fast takes 351 ms but grows to 7.8 MB.' },
  bannerAnalysis: { ko: '모드별 부담과 끊김 원인을 계속 기록합니다. 게임 부담: 측정 오차 안(±0.5ms)', en: 'Records per-mod cost and stutter causes continuously. Game overhead: within measurement noise (±0.5 ms)' }
});
