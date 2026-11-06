/* Tushonka Cleaner — buildless, offline-capable page. */
(() => {
  'use strict';

  const root = document.documentElement;
  const storageKey = 'tushonka-cleaner-language';
  const supported = value => value === 'ko' || value === 'en';
  const copy = {
    ko: {
      title: 'Tushonka Cleaner — SPT 메모리·서버·그래픽 관리',
      description: 'SPT 4.1의 메모리 정리·진단, 서버 최적화, 그래픽 조정을 한곳에서 합니다. 측정 결과와 화면 차이, 설치 방법과 주의사항을 확인합니다.',
      pause: '움직임 멈추기', play: 'GIF 재생',
      playing: '같은 자리에서 그래픽 단계를 바꾼 실제 화면입니다.',
      paused: 'GIF의 첫 화면입니다. 재생 버튼으로 단계 변화를 볼 수 있습니다.',
      reduced: '움직임 줄이기 설정에 따라 GIF의 첫 화면을 표시합니다.'
    },
    en: {
      title: 'Tushonka Cleaner — Memory, server & graphics tools for SPT',
      description: 'Memory cleanup and diagnostics, server optimizations, and graphics adjustments for SPT 4.1. Compare measured results, see the visual trade-offs, and learn how to install.',
      pause: 'Pause animation', play: 'Play GIF',
      playing: 'Actual gameplay, switching graphics levels from the same spot.',
      paused: 'The first frame of the GIF. Press play to see the levels change.',
      reduced: 'Showing the first frame to respect your reduced-motion setting.'
    }
  };

  let language;
  // A link can ask for a language with ?lang=en. It wins for this visit and is not saved.
  const requested = new URLSearchParams(location.search).get('lang');
  if (supported(requested)) language = requested;
  try {
    const saved = localStorage.getItem(storageKey);
    if (!language && supported(saved)) language = saved;
  } catch { /* Storage can be unavailable, including when opened as a file. */ }
  if (!language) {
    const browserLanguage = navigator.languages?.[0] || navigator.language || 'en';
    language = browserLanguage.toLowerCase().startsWith('ko') ? 'ko' : 'en';
  }

  // Runs in the head so the right language is selected before content is painted.
  root.lang = language;
  root.dataset.lang = language;
  document.title = copy[language].title;

  let refreshMotion = () => {};

  function setLanguage(next, remember = false) {
    if (!supported(next)) return;
    language = next;
    root.lang = next;
    root.dataset.lang = next;
    document.title = copy[next].title;

    for (const selector of ['meta[name="description"]', 'meta[property="og:description"]', 'meta[name="twitter:description"]']) {
      document.querySelector(selector)?.setAttribute('content', copy[next].description);
    }
    for (const selector of ['meta[property="og:title"]', 'meta[name="twitter:title"]']) {
      document.querySelector(selector)?.setAttribute('content', copy[next].title);
    }
    document.querySelector('meta[property="og:locale"]')?.setAttribute('content', next === 'ko' ? 'ko_KR' : 'en_US');
    document.querySelector('meta[property="og:locale:alternate"]')?.setAttribute('content', next === 'ko' ? 'en_US' : 'ko_KR');

    for (const [data, attribute] of [['alt', 'alt'], ['label', 'aria-label'], ['column', 'data-column']]) {
      document.querySelectorAll(`[data-${data}-${next}]`).forEach(element => {
        element.setAttribute(attribute, element.getAttribute(`data-${data}-${next}`));
      });
    }
    document.querySelectorAll('[data-language]').forEach(button => {
      button.setAttribute('aria-pressed', String(button.dataset.language === next));
    });
    refreshMotion();

    if (remember) {
      try { localStorage.setItem(storageKey, next); } catch { /* The page still works without persistence. */ }
    }
  }

  function initialize() {
    document.querySelectorAll('[data-language]').forEach(button => {
      button.addEventListener('click', () => setLanguage(button.dataset.language, true));
    });

    const animation = document.getElementById('graphics-animation');
    const motionButton = document.getElementById('motion-toggle');
    const motionNote = document.getElementById('motion-note');
    const reducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)');
    let paused = false;

    refreshMotion = () => {
      const playing = !reducedMotion.matches && !paused;
      const source = playing ? animation.dataset.animation : animation.dataset.poster;
      if (animation.getAttribute('src') !== source) animation.setAttribute('src', source);
      motionButton.hidden = reducedMotion.matches;
      motionButton.setAttribute('aria-pressed', String(playing));
      motionButton.textContent = copy[language][playing ? 'pause' : 'play'];
      motionNote.textContent = copy[language][reducedMotion.matches ? 'reduced' : playing ? 'playing' : 'paused'];
    };
    motionButton.addEventListener('click', () => {
      paused = !paused;
      refreshMotion();
    });
    if (reducedMotion.addEventListener) reducedMotion.addEventListener('change', refreshMotion);
    else reducedMotion.addListener(refreshMotion);
    setLanguage(language);
  }

  if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', initialize, { once: true });
  else initialize();
})();
