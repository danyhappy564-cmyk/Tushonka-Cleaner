/* 2026-11-06 실측값입니다. RTX 4080, 2560x1440, 봇 없음, 스트리트와 등대 본맵 각 3곳.
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
  'gfxEffect.light': { ko: '프레임 변화: 없음(스트리트, 등대)', en: 'FPS change: none (Streets, Lighthouse)' },
  'gfxEffect.more': { ko: '프레임 시간: 스트리트 12~15% 감소, 등대 5~12% 감소', en: 'Frame time: 12-15% lower on Streets, 5-12% lower on Lighthouse' },
  'gfxEffect.most': { ko: '프레임 시간: 스트리트 31~36% 감소, 등대 9~12% 감소', en: 'Frame time: 31-36% lower on Streets, 9-12% lower on Lighthouse' },
  serverProposal: { ko: '아이템 목록 응답이 끔 489ms에서 권장 265ms, 균형 305ms로 줄었습니다. 전송 크기는 끔 3.6MB, 권장 5.9MB, 균형 3.7MB입니다.', en: 'Item list responses dropped from 489 ms with Off to 265 ms with Recommended and 305 ms with Balanced. Transfer size is 3.6 MB with Off, 5.9 MB with Recommended, and 3.7 MB with Balanced.' },
  bannerAnalysis: { ko: '모드별 부담과 끊김 원인을 계속 기록합니다. 게임 부담: 측정 오차 안(±0.5ms)', en: 'Records per-mod cost and stutter causes continuously. Game overhead: within measurement noise (±0.5 ms)' }
});
