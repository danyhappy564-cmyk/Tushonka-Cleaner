/* 화면의 값은 실제 설정에서 읽고 저장 뒤 게임이 보관한 값을 다시 받습니다. */
(function () {
  'use strict';
  const I = window.TC_I18N, t = I.t, esc = window.rcEsc, $ = id => document.getElementById(id);
  const page = document.body.dataset.page;
  const keys = {
    waitCombat: '5. Timing|Wait for quiet', afterRaid: '3. General|After raid cleanup', growth: '1. Auto GC|Growth trigger (GB)', budget: '1. Auto GC|Work per frame (ms)',
    workingSet: '2. Working set|Enabled', assets: '4. Assets|Auto in raid', raidStartAssets: '4. Assets|At raid start', decals: 'Graphics.Categories|Decals',
    gcGap: '1. Auto GC|Minimum gap (s)', gcBlocking: '1. Auto GC|Allow blocking fallback', trimLow: '2. Working set|Only when RAM is low', trimPct: '2. Working set|Low RAM threshold (%)', trimGap: '2. Working set|Interval (s)', trimAfter: '2. Working set|After GC',
    assetGrowth: '4. Assets|Native growth trigger (GB)', assetGap: '4. Assets|Minimum gap (min)', assetGc: '4. Assets|GC first', assetDelay: '4. Assets|Start delay (s)', quiet: '5. Timing|Quiet seconds', defer: '5. Timing|GC max wait (s)', inventory: '5. Timing|Run on inventory', afterDelay: '3. General|After raid delay (s)', onlyRaid: '3. General|Only in raid',
    warnOn: '9. Warnings|Enabled', warnPct: '9. Warnings|Commit free below (%)', vramWarn: '9. Warnings|VRAM warning', vramPct: '9. Warnings|VRAM full at (%)', vramSec: '9. Warnings|VRAM full for (s)', vramSpill: '9. Warnings|VRAM spill at (GB)', warnGame: '9. Warnings|In-game notification',
    fcOn: '15. Forecast|Enabled', fcMin: '15. Forecast|Warn below (min)', fcRestart: '15. Forecast|Restart notification', fixSpawn: '19. Game memory fixes|Bot spawn data after raid', fixBrain: '19. Game memory fixes|Dead bot brains', fixCoroutine: '19. Game memory fixes|Per-bot coroutine garbage',
    overlayOn: '3. General|Show overlay', overlayFps: '3. General|Overlay FPS', overlayCount: '3. General|Overlay mod bar count', webOn: '18. Web page|Enabled', webPort: '18. Web page|Port', webLan: '18. Web page|Allow LAN',
    s8: 'S8 RagfairCalmUpdates|Enabled', s9: 'S9 FastCompression|Enabled', s9Level: 'S9 FastCompression|Level', s12: 'S12 IsolatedBotRandomisation|Enabled', s13: 'S13 CalmNotifier|Enabled', s15: 'S15 RaidStartGc|Enabled', s15Mode: 'S15 RaidStartGc|Mode', s16: 'S16 PostRaidCleanup|Enabled', s16Delay: 'S16 PostRaidCleanup|DelaySeconds', s16Quiet: 'S16 PostRaidCleanup|QuietSeconds', s16Max: 'S16 PostRaidCleanup|MaxWaitSeconds', s16Min: 'S16 PostRaidCleanup|MinCommittedMb', s11: 'S11 SaveDirtyTracking|Enabled', s11Gap: 'S11 SaveDirtyTracking|ForceSaveIntervalSeconds',
    c1: 'C1 MenuVideo|Enabled', c2: 'C2 AirdropPhysics|Enabled', debug: 'Debug|Enabled', debugGap: 'Debug|SummaryIntervalMinutes', debugSlow: 'Debug|SlowRequestMs', debugKeep: 'Debug|KeepFiles',
    gfxLitter: 'Graphics.Categories|Litter', gfxRubble: 'Graphics.Categories|Rubble', gfxCasings: 'Graphics.Categories|Casings', gfxFood: 'Graphics.Categories|Food', gfxPuddles: 'Graphics.Categories|Puddles', gfxGlass: 'Graphics.Categories|Glass', gfxParticles: 'Graphics.Tuning|ReduceParticles', gfxSoftParticles: 'Graphics.Tuning|SoftParticles', gfxSoftVeg: 'Graphics.Tuning|SoftVegetation', gfxReflect: 'Graphics.Tuning|RealtimeReflections', gfxShadows: 'Graphics.Tuning|Shadows', gfxLights: 'Graphics.Tuning|PixelLights', gfxLod: 'Graphics.Tuning|Lod', gfxMuzzle: 'Graphics.Tuning|OtherMuzzleShadows', gfxShellCap: 'Graphics.Tuning|ShellCap', gfxDelay: 'Graphics.Advanced|StartDelaySeconds', gfxBudget: 'Graphics.Advanced|FrameBudgetMs', gfxDistance: 'Graphics.Advanced|HideDistanceMeters', gfxChanges: 'Graphics.Advanced|MaxVisibilityChangesPerFrame'
  };
  const shortcut = '3. General|Diagnostic mode key';
  const omit = new Set([...Object.values(keys), shortcut, '0. Mode|Language', '0. Mode|Preset', '0. Mode|Server level', 'Graphics|Level']);
  const lists = { s9Level: ['fast', 'balanced', 'small', 'none'], s15Mode: ['background', 'skip', 'vanilla'] };
  let entries = new Map(), groups = [], state = window.TC_BOOT, saving = Promise.resolve();
  const message = text => { if ($('action-message')) $('action-message').textContent = text; };
  const read = key => entries.get(key)?.v;
  const queue = work => (saving = saving.then(work).catch(error => message(error.message)));
  async function load() {
    const response = await fetch('api/settings', { cache: 'no-store' }), data = await response.json();
    if (!response.ok || data.error) throw new Error(data.error || String(response.status));
    groups = data.groups; entries = new Map(groups.flatMap(g => g.items).map(it => [it.key, it])); state = data.state;
    if (page === 'diagnostics') renderDiagnostics(); else render();
  }
  async function save(key, value) {
    message(I.language() === 'ko' ? '저장 중입니다.' : 'Saving.');
    await window.rcPost('api/settings', { key, value: String(value) });
    if (key === keys.webPort && !location.pathname.includes('/tushonka-cleaner/')) {
      const next = new URL(location.href); next.port = String(value); setTimeout(() => location.assign(next.href), 1200); return;
    }
    if (key === keys.webOn && value === false) { message(I.language() === 'ko' ? '웹 페이지를 껐습니다. 게임을 끄고 설정 파일에서 다시 켤 수 있습니다.' : 'Web settings are off. Close the game and enable the web page in the settings file to turn it back on.'); return; }
    await load(); message(I.language() === 'ko' ? '게임에 적용했습니다.' : 'Applied in the game.');
  }
  async function ui(key, value) { await window.rcPost('api/ui', { key, value }); await load(); message(t('changed')); }
  function bindControls() {
    document.querySelectorAll('[data-setting], [data-adv]').forEach(el => {
      const name = el.dataset.setting || el.dataset.adv;
      if (['memoryMode', 'serverLevel', 'graphicsLevel'].includes(name)) { el.addEventListener('change', () => queue(() => ui(name, el.value))); return; }
      const key = keys[name]; if (!key) { el.closest('.setting-row')?.remove(); return; }
      el.addEventListener('change', () => {
        const it = entries.get(key); if (!it || !el.checkValidity()) { render(); return; }
        const value = lists[name] ? it.opts[lists[name].indexOf(el.value)] : el.type === 'checkbox' ? el.checked : el.value;
        queue(() => save(key, value));
      });
    });
  }
  function renderComparison() {
    const host = $('mode-comparison'); if (!host) return;
    const modes = ['game','check','analysis','leak','off'], labels = ['modeGame','modeCheck','modeAnalysis','modeLeak','modeOff'];
    const rows = [['compareCleanup',['yes','yes','yes','yes','no']],['compareOverlay',['no','yes','yes','leakOverlay','no']],['compareReports',['no','yes','yes','noStutterAlert','no']],['compareMod',['no','no','continuous','periodic','no']],['compareCause',['no','no','yes','no','no']],['compareHeavy',['no','no','yes','yes','no']],['compareLeak',['no','no','no','yes','no']],['compareLog',['no','seconds120','seconds30','seconds60','no']]];
    const costs = modes.map(mode => ((window.TC_MEASURED || {})['compareCost.' + mode] || {})[I.language()] || '');
    const cell = (value, i, tag = 'td') => '<' + tag + (modes[i] === state.memoryMode ? ' class="current-mode-column"' : '') + '>' + esc(value) + '</' + tag + '>';
    host.innerHTML = '<thead><tr><th>' + esc(t('compareSetting')) + '</th>' + labels.map((key,i) => cell(t(key),i,'th')).join('') + '</tr></thead><tbody>' + rows.map(([label,values]) => '<tr><th>' + esc(t(label)) + '</th>' + values.map((key,i) => cell(t(key),i)).join('') + '</tr>').join('') + (costs.every(Boolean) ? '<tr><th>' + esc(I.language() === 'ko' ? '게임 부담' : 'Game overhead') + '</th>' + costs.map((v,i) => cell(v,i)).join('') + '</tr>' : '') + '</tbody>';
  }
  function render() {
    const flags = { memory: state.memoryMode !== 'off', server: state.serverLevel !== 'off', graphics: state.graphicsLevel !== 'off' };
    const recommended = state.memoryMode === 'game' && state.serverLevel === 'recommended' && state.graphicsLevel === 'more' && ['waitCombat','afterRaid','workingSet'].every(k => read(keys[k])?.toLowerCase() === 'true') && ['assets','raidStartAssets'].every(k => read(keys[k])?.toLowerCase() === 'false') && Number(read(keys.growth)) === 1.5 && Number(read(keys.budget)) === 2;
    const setup = Object.values(flags).every(x => !x) ? 'off' : recommended ? 'recommended' : 'custom';
    document.querySelectorAll('[data-setting], [data-adv]').forEach(el => {
      const name = el.dataset.setting || el.dataset.adv;
      if (['memoryMode','serverLevel','graphicsLevel'].includes(name)) { el.checked = el.value === state[name]; el.disabled = name === 'serverLevel' ? !state.serverReady : name === 'graphicsLevel' ? !state.graphicsAvailable : !state.memoryAvailable; return; }
      const it = entries.get(keys[name]); el.closest('.setting-row')?.toggleAttribute('hidden', !it); if (!it) return;
      const value = lists[name] ? lists[name][it.opts.indexOf(it.v)] : it.type === 'bool' ? it.v.toLowerCase() === 'true' : it.v;
      if (el.type === 'checkbox') el.checked = value; else el.value = value;
      if (el.type === 'number') { if (it.min != null) el.min = it.min; if (it.max != null) el.max = it.max; el.step = it.type === 'int' ? '1' : 'any'; }
      const feature = el.dataset.parentFeature;
      el.disabled = feature === 'server' ? !state.serverReady : feature === 'graphics' ? !state.graphicsAvailable || !flags.graphics : !state.memoryAvailable || !flags.memory;
    });
    document.querySelectorAll('[data-feature-toggle]').forEach(el => { const f = el.dataset.featureToggle; el.checked = flags[f]; el.disabled = f === 'server' ? !state.serverReady : f === 'graphics' ? !state.graphicsAvailable : !state.memoryAvailable; });
    document.querySelectorAll('[data-preset]').forEach(el => el.setAttribute('aria-pressed', String(setup === el.dataset.preset)));
    document.querySelectorAll('[data-preset-selected]').forEach(el => { el.hidden = setup !== el.dataset.presetSelected; });
    document.querySelectorAll('[data-current-setup]').forEach(el => { el.textContent = t({ off:'offSetup', custom:'custom', recommended:'recommended' }[setup]); });
    document.querySelectorAll('[data-current-features]').forEach(el => { const names = Object.keys(flags).filter(k => flags[k]).map(k => t({ memory:'memFeature', server:'serverFeature', graphics:'graphicsFeature' }[k])); el.textContent = names.length ? t('currentFeatures', { names:names.join(' · ') }) : t('noneSelected'); });
    if ($('leak-note')) $('leak-note').hidden = state.memoryMode !== 'leak'; if ($('analysis-note')) $('analysis-note').hidden = state.memoryMode !== 'analysis';
    document.querySelectorAll('[data-timing]').forEach(el => { el.textContent = t('gameTimingOn'); });
    document.querySelectorAll('[data-server-summary]').forEach(el => { el.textContent = t({ recommended:'serverSummaryRecommended', balanced:'serverSummaryBalanced', off:'serverSummaryOff', custom:'serverSummaryCustom' }[state.serverLevel]); });
    if ($('server-connection')) $('server-connection').textContent = (state.serverStatus || '') + (state.serverResult ? '\n' + state.serverResult : '');
    document.querySelectorAll('[data-shortcut-value]').forEach(el => { el.textContent = read(shortcut) || t('noKey'); });
    document.querySelectorAll('[data-i18n="modeCost"]').forEach((el,i) => { el.dataset.measure = ['game','check','analysis','leak','off'][i]; });
    document.querySelectorAll('[data-i18n="gfxEffect"]').forEach((el,i) => { el.dataset.measure = ['light','more','most'][i]; });
    I.apply(); renderComparison(); if ($('cleanup-button')) $('cleanup-button').disabled = !state.memoryAvailable;
  }
  function control(it) {
    const label = ' aria-label="' + esc(it.name) + '"';
    if (it.type === 'bool') return '<input type="checkbox"' + label + (it.v.toLowerCase() === 'true' ? ' checked' : '') + '>';
    if (it.type === 'list') return '<select' + label + '>' + it.opts.map(o => '<option' + (o === it.v ? ' selected' : '') + '>' + esc(o) + '</option>').join('') + '</select>';
    return '<input type="' + (['float','int'].includes(it.type) ? 'number' : 'text') + '"' + label + ' value="' + esc(it.v) + '"' + (it.min != null ? ' min="' + it.min + '" max="' + it.max + '"' : '') + ' step="' + (it.type === 'int' ? '1' : 'any') + '">';
  }
  function renderDiagnostics() {
    const host = $('diag-list'); host.replaceChildren();
    for (const group of groups) {
      const items = group.items.filter(it => !omit.has(it.key)); if (!items.length) continue;
      const box = document.createElement('details'); box.className = 'setting-group'; box.open = true; box.innerHTML = '<summary>' + esc(group.cat) + '</summary><div class="setting-group-body"></div>';
      for (const it of items) {
        const row = document.createElement('div'); row.className = 'setting-row'; row.dataset.search = (group.cat+' '+it.name+' '+it.desc+' '+it.key).toLowerCase();
        row.innerHTML = '<div><strong>' + esc(it.name) + '</strong><p>' + esc(it.desc) + '</p></div><div class="diagnostic-control">' + control(it) + '<button type="button">' + esc(t('advReset')) + '</button></div>';
        const input = row.querySelector('input, select'); input.addEventListener('change', () => { if (input.checkValidity()) queue(() => save(it.key, input.type === 'checkbox' ? input.checked : input.value)); });
        row.querySelector('button').addEventListener('click', () => queue(() => save(it.key,it.def))); box.querySelector('.setting-group-body').append(row);
      }
      host.append(box);
    }
    filterDiagnostics();
  }
  function filterDiagnostics() {
    const q = ($('diag-query')?.value || '').toLowerCase(); let count = 0;
    document.querySelectorAll('[data-search]').forEach(row => { row.hidden = !row.dataset.search.includes(q); if (!row.hidden) count++; });
    document.querySelectorAll('#diag-list .setting-group').forEach(group => { group.hidden = ![...group.querySelectorAll('[data-search]')].some(row => !row.hidden); });
    if ($('diag-empty')) $('diag-empty').hidden = count > 0;
  }
  function setupShortcut() {
    const map = { Backquote:'BackQuote', Minus:'Minus', Equal:'Equals', BracketLeft:'LeftBracket', BracketRight:'RightBracket', Backslash:'Backslash', Semicolon:'Semicolon', Quote:'Quote', Comma:'Comma', Period:'Period', Slash:'Slash', Space:'Space', Enter:'Return', Backspace:'Backspace', Delete:'Delete', Tab:'Tab', Home:'Home', End:'End', PageUp:'PageUp', PageDown:'PageDown', ArrowUp:'UpArrow', ArrowDown:'DownArrow', ArrowLeft:'LeftArrow', ArrowRight:'RightArrow', Insert:'Insert' };
    let cancel = null;
    document.querySelectorAll('[data-shortcut-capture]').forEach(button => button.addEventListener('click', () => {
      if (cancel) cancel(); button.textContent = t('shortcutWaiting');
      const cleanup = () => { document.removeEventListener('keydown', handler, true); button.textContent = t('shortcutChange'); cancel = null; };
      const handler = event => {
        event.preventDefault(); event.stopPropagation(); if (event.repeat) return; if (event.code === 'Escape') { cleanup(); return; }
        let key = /^Key[A-Z]$/.test(event.code) ? event.code.slice(3) : /^Digit[0-9]$/.test(event.code) ? 'Alpha'+event.code.slice(5) : /^F([1-9]|1[0-5])$/.test(event.code) ? event.code : map[event.code];
        if (/^Numpad[0-9]$/.test(event.code)) key = 'Keypad'+event.code.slice(6); if (!key) return;
        const mods = []; if (event.ctrlKey) mods.push('LeftControl'); if (event.shiftKey) mods.push('LeftShift'); if (event.altKey) mods.push('LeftAlt'); if (event.metaKey) mods.push('LeftCommand'); cleanup(); queue(() => save(shortcut,[key,...mods].join(' + ')));
      };
      cancel = cleanup; document.addEventListener('keydown',handler,true);
    }));
    document.querySelectorAll('[data-shortcut-clear]').forEach(button => button.addEventListener('click', () => { if (cancel) cancel(); queue(() => save(shortcut,'None')); }));
  }
  if (page === 'settings') {
    window.TC_ADVANCED.render(); bindControls(); setupShortcut();
    const tabs = [...document.querySelectorAll('[role="tab"]')];
    function activate(tab) { tabs.forEach(item => { const on = item===tab; item.setAttribute('aria-selected',String(on)); item.tabIndex = on?0:-1; $(item.getAttribute('aria-controls')).hidden=!on; }); }
    tabs.forEach((tab,i) => { tab.addEventListener('click',()=>activate(tab)); tab.addEventListener('keydown',event=>{ const n = event.key==='ArrowRight'?(i+1)%tabs.length:event.key==='ArrowLeft'?(i+tabs.length-1)%tabs.length:event.key==='Home'?0:event.key==='End'?tabs.length-1:-1; if(n>=0){event.preventDefault();activate(tabs[n]);tabs[n].focus();} }); });
    document.querySelectorAll('[data-preset]').forEach(b=>b.addEventListener('click',()=>queue(()=>ui('quick',b.dataset.preset))));
    document.querySelectorAll('[data-feature-toggle]').forEach(el=>el.addEventListener('change',()=>queue(()=>ui('feature',el.dataset.featureToggle+'/'+(el.checked?'on':'off')))));
    document.querySelectorAll('[data-adv-reset]').forEach(b=>b.addEventListener('click',()=>queue(async()=>{
      const names = [...document.querySelector('[data-advanced="'+b.dataset.advReset+'"]').querySelectorAll('[data-setting], [data-adv]')].map(el=>keys[el.dataset.setting||el.dataset.adv]);
      for (const key of new Set(names)) { const it=entries.get(key); if(it) await window.rcPost('api/settings',{key,value:it.def}); } await load();message(t('advResetDone'));
    })));
    $('cleanup-button').addEventListener('click',()=>queue(async()=>{ const reply=await window.rcPost('api/action',{name:'gc'});message(reply.msg); }));
  } else $('diag-query')?.addEventListener('input',filterDiagnostics);
  load().catch(error=>message(error.message));
})();
