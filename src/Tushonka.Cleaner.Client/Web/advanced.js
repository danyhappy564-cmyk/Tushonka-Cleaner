/* 승인된 탭별 고급 설정 구성입니다. 값은 실제 설정에서 읽습니다. */
(function(){'use strict';
  const sw = (key, label, value, help) => ({ type: 'switch', key, label, value, help });
  const num = (key, label, value, min, max, step = 1) => ({ type: 'number', key, label, value, min, max, step });
  const pick = (key, label, value, options) => ({ type: 'select', key, label, value, options });
  const bound = (html) => ({ type: 'bound', html });
  const note = key => ({ type: 'note', key });
  const groups = {
    memory: [
      ['grpGc', [
        bound('<div class="setting-row"><div><label for="growth-trigger" data-i18n="growth"></label><p data-i18n="growthHelp"></p></div><input id="growth-trigger" type="number" min="0.25" max="16" step="0.25" data-setting="growth" data-parent-feature="memory"></div>'),
        bound('<div class="setting-row"><div><label for="gc-budget" data-i18n="gcBudget"></label><p data-i18n="budgetHelp"></p></div><input id="gc-budget" type="number" min="0.5" max="10" step="0.5" data-setting="budget" data-parent-feature="memory"></div>'),
        num('gcGap', 'optGcGap', 60, 0, 3600), sw('gcBlocking', 'optGcBlocking', false, 'optGcBlockingHelp')]],
      ['grpAssets', [sw('assetGc', 'optAssetGc', true)]],
      ['grpTiming', [num('quiet', 'optQuiet', 15, 0, 120), num('defer', 'optDefer', 180, 0, 1800), sw('inventory', 'optInventory', true)]],
      ['grpAfter', [num('afterDelay', 'optAfterDelay', 20, 0, 300), sw('onlyRaid', 'optOnlyRaid', true)]],
      ['grpWarn', [sw('warnOn', 'optWarnOn', true), num('warnPct', 'optWarnPct', 10, 1, 50), sw('vramWarn', 'optVramWarn', true), num('vramPct', 'optVramPct', 95, 50, 100), num('vramSec', 'optVramSec', 120, 5, 600), num('vramSpill', 'optVramSpill', 1, 0, 16, .5), sw('warnGame', 'optWarnGame', true)]],
      ['grpForecast', [sw('fcOn', 'optFcOn', true), num('fcMin', 'optFcMin', 15, 1, 120), sw('fcRestart', 'optFcRestart', true)]],
      ['grpFixes', [sw('fixSpawn', 'optFixSpawn', true), sw('fixBrain', 'optFixBrain', true), sw('fixCoroutine', 'optFixCoroutine', true)]],
      ['grpOverlay', [sw('overlayOn', 'optOverlayOn', false), sw('overlayFps', 'optOverlayFps', true), num('overlayCount', 'optOverlayCount', 5, 1, 15)]],
      ['grpWeb', [sw('webOn', 'optWebOn', true), num('webPort', 'optWebPort', 6977, 1024, 65535), sw('webLan', 'optWebLan', false)]]
    ],
    server: [
      ['grpSrvResponse', [sw('s9', 'optS9On', true, 'serverS9Help'), pick('s9Level', 'optS9Level', 'balanced', [['fast', 'lvlFast'], ['balanced', 'lvlBalanced'], ['small', 'lvlSmall'], ['none', 'lvlNone']])]],
      ['grpSrvFix', [sw('s12', 'optS12', true, 'serverS12Help')]],
      ['grpSrvClient', [sw('c1', 'optC1', true), sw('c2', 'optC2', true)]],
      ['grpSrvLog', [sw('debug', 'optDebug', false), num('debugGap', 'optDebugGap', 5, 0, 60), num('debugSlow', 'optDebugSlow', 250, 50, 5000), num('debugKeep', 'optDebugKeep', 10, 1, 50)]]
    ],
    graphics: [
      ['grpGfxHide', [sw('gfxLitter', 'gfxLitter', true), sw('gfxRubble', 'gfxRubble', true), sw('gfxCasings', 'gfxCasings', true), sw('gfxFood', 'gfxFood', true), sw('gfxPuddles', 'gfxPuddles', true), sw('gfxGlass', 'gfxGlass', true),
        bound('<div class="setting-row compact"><div><label for="decals" data-i18n="decals"></label><p data-i18n="decalsHelp"></p></div><label class="switch"><input id="decals" type="checkbox" data-setting="decals" data-parent-feature="graphics" data-i18n-aria="decals"><span></span></label></div>'), note('gfxExclude')]],
      ['grpGfxTune', [note('gfxTuneNote'), sw('gfxParticles', 'gfxParticles', true), sw('gfxSoftParticles', 'gfxSoftParticles', true), sw('gfxSoftVeg', 'gfxSoftVeg', true), sw('gfxReflect', 'gfxReflect', true), sw('gfxShadows', 'gfxShadows', true), sw('gfxLights', 'gfxLights', true), sw('gfxLod', 'gfxLod', true), sw('gfxGrass', 'gfxGrass', true), sw('gfxMuzzle', 'gfxMuzzle', true)]],
      ['grpGfxCaps', [sw('gfxShellCap', 'gfxShellCap', true)]],
      ['grpGfxRun', [num('gfxDelay', 'gfxDelay', 10, 0, 120), num('gfxBudget', 'gfxBudget', 2, .5, 10, .5), { ...num('gfxDistance', 'gfxDistance', 80, 10, 500), help: 'gfxDistanceHelp' }, num('gfxChanges', 'gfxChanges', 24, 1, 256)]]
    ]
  };
  function rowHtml(row, feature) {
    if (row.type === 'bound') return row.html;
    if (row.type === 'note') return `<p class="inline-note" data-i18n="${row.key}"></p>`;
    const id = 'adv-' + row.key;
    const label = `<div><label for="${id}" data-i18n="${row.label}"></label>${row.help ? `<p data-i18n="${row.help}"></p>` : ''}</div>`;
    const common = `id="${id}" data-adv="${row.key}" data-parent-feature="${feature}"`;
    if (row.type === 'switch') return `<div class="setting-row compact">${label}<label class="switch"><input ${common} type="checkbox" data-i18n-aria="${row.label}"><span></span></label></div>`;
    if (row.type === 'number') return `<div class="setting-row">${label}<input ${common} type="number" min="${row.min}" max="${row.max}" step="${row.step}"></div>`;
    return `<div class="setting-row">${label}<select ${common}>${row.options.map(([value, key]) => `<option value="${value}" data-i18n="${key}"></option>`).join('')}</select></div>`;
  }
  function render() { document.querySelectorAll('[data-advanced]').forEach(host => {
    const feature = host.dataset.advanced;
    host.innerHTML = (feature === 'server' ? '<p class="inline-note" data-i18n="advServerNote"></p>' : '') +
      groups[feature].map(([title, rows], index) => `<details class="setting-group"${index === 0 ? ' open' : ''}><summary data-i18n="${title}"></summary><div class="setting-group-body">${rows.map(row => rowHtml(row, feature)).join('')}</div></details>`).join('') +
      `<div class="actions advanced-actions"><button type="button" data-adv-reset="${feature}" data-i18n="advReset"></button>${feature === 'memory' ? '<a class="button" href="diagnostics" data-i18n="openDiag"></a>' : ''}</div>`;
  });


  }
window.TC_ADVANCED = { groups, render };
})();
