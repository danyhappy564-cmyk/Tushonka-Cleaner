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
      const en = language === 'en';
      const source = playing ? (en ? animation.dataset.animationEn : animation.dataset.animation) : (en ? animation.dataset.posterEn : animation.dataset.poster);
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


  // 1.1.0: 움직임과 맵 탐색기
  function enhance() {
    const reduced = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    const bar = document.createElement('div');
    bar.className = 'progress';
    document.body.append(bar);
    const onScroll = () => {
      const h = document.documentElement.scrollHeight - innerHeight;
      bar.style.setProperty('--p', h > 0 ? scrollY / h : 0);
      root.classList.toggle('scrolled', scrollY > 10);
    };
    addEventListener('scroll', onScroll, { passive: true });
    onScroll();

    const targets = document.querySelectorAll('.section-heading, .feature, .stat, .level-card, .conditions, .map-explorer, .summary-table, .table-note, .evidence, .inline-note, .mode-row, .cleanup-report, .mode-keys, .server-table, .server-explanation, .notice-card, .screen-role, .screen-figure, .web-shots, .install-steps li, .migration, .credit, .disclaimer');
    targets.forEach((el, i) => { el.classList.add('rv'); el.style.setProperty('--d', (i % 4) * 0.06 + 's'); });
    const countUp = el => {
      const to = Number(el.dataset.count);
      if (reduced || !to) { el.textContent = to; return; }
      const start = performance.now();
      const step = now => {
        const k = Math.min(1, (now - start) / 1100);
        el.textContent = Math.round(to * (1 - Math.pow(1 - k, 3)));
        if (k < 1) requestAnimationFrame(step);
      };
      requestAnimationFrame(step);
    };
    const seen = new IntersectionObserver(entries => entries.forEach(e => {
      if (!e.isIntersecting) return;
      e.target.classList.add('in');
      e.target.querySelectorAll('[data-count]').forEach(countUp);
      if (e.target.classList.contains('map-explorer')) activeGo();
      seen.unobserve(e.target);
    }), { threshold: 0.08, rootMargin: '0px 0px -6% 0px' });
    targets.forEach(el => seen.observe(el));

    // 맵 탐색기
    const tabs = [...document.querySelectorAll('.map-tab')];
    const panels = [...document.querySelectorAll('[data-map-panel]')];
    const activeGo = () => {
      const p = panels.find(x => !x.hidden);
      if (p) requestAnimationFrame(() => requestAnimationFrame(() => p.classList.add('go')));
    };
    const pick = name => {
      tabs.forEach(b => b.setAttribute('aria-selected', String(b.dataset.map === name)));
      panels.forEach(p => { p.hidden = p.dataset.mapPanel !== name; p.classList.remove('go'); });
      activeGo();
    };
    tabs.forEach(b => b.addEventListener('click', () => pick(b.dataset.map)));
    if (tabs[0]) pick(tabs[0].dataset.map);
    document.querySelectorAll('.map-jump').forEach(b => b.addEventListener('click', () => {
      pick(b.dataset.map);
      document.getElementById('maps').scrollIntoView({ behavior: reduced ? 'auto' : 'smooth', block: 'start' });
    }));

    // 모드 사진과 번호
    const shots = [...document.querySelectorAll('[data-shot-panel]')];
    document.querySelectorAll('.mode-tab').forEach(b => b.addEventListener('click', () => {
      document.querySelectorAll('.mode-tab').forEach(x => x.setAttribute('aria-selected', String(x === b)));
      shots.forEach(p => { p.hidden = p.dataset.shotPanel !== b.dataset.shot; });
    }));
    document.querySelectorAll('.anno-pin').forEach(b => b.addEventListener('click', () => {
      const row = document.querySelector(`.gloss-row[data-n="${b.dataset.pin}"]`);
      if (!row) return;
      row.scrollIntoView({ behavior: reduced ? 'auto' : 'smooth', block: 'center' });
      row.classList.add('flash');
      setTimeout(() => row.classList.remove('flash'), 1800);
    }));

    // 기능 검증 표 거르기
    const vt = document.querySelector('.verify-table');
    if (vt) {
      const box = document.createElement('div');
      box.className = 'verify-filter';
      const names = { all: ['전체', 'All'], keep: ['유지', 'Kept'], cut: ['1.1.0 제거', 'Removed in 1.1.0'], gone: ['4.1이 해결', 'Solved by 4.1'] };
      Object.entries(names).forEach(([key, [ko, en]], i) => {
        const b = document.createElement('button');
        b.type = 'button'; b.className = 'mode-tab'; b.setAttribute('aria-selected', String(i === 0));
        b.innerHTML = `<span class="ko">${ko}</span><span class="en" lang="en">${en}</span>`;
        b.addEventListener('click', () => {
          box.querySelectorAll('button').forEach(x => x.setAttribute('aria-selected', String(x === b)));
          vt.querySelectorAll('tbody tr').forEach(tr => { tr.hidden = key !== 'all' && !tr.classList.contains('v-' + key); });
        });
        box.append(b);
      });
      vt.closest('.table-scroll').before(box);
    }

    // 현재 구역 표시와 대문 사진 기울기
    const links = [...document.querySelectorAll('.main-nav a')];
    const spy = new IntersectionObserver(entries => entries.forEach(e => {
      if (e.isIntersecting) links.forEach(a => a.classList.toggle('is-here', a.getAttribute('href') === '#' + e.target.id));
    }), { rootMargin: '-45% 0px -50% 0px' });
    links.forEach(a => { const t = document.querySelector(a.getAttribute('href')); if (t) spy.observe(t); });
    const sheet = document.querySelector('.hero-media .media-sheet');
    if (sheet && !reduced) document.querySelector('.hero').addEventListener('mousemove', e => {
      const r = sheet.getBoundingClientRect();
      sheet.style.setProperty('--ry', ((e.clientX - r.left - r.width / 2) / r.width * 6).toFixed(2) + 'deg');
      sheet.style.setProperty('--rx', (-(e.clientY - r.top - r.height / 2) / r.height * 4).toFixed(2) + 'deg');
    });
  }

  const start = () => { initialize(); enhance(); };
  if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', start, { once: true });
  else start();
})();
