// 상대 주소로 게임과 서버 프록시에서 같은 기능을 제공합니다.
window.rcPost = async function (url, fields) {
  const response = await fetch(url, { method: 'POST', cache: 'no-store', headers: { 'X-RamCleaner': '1', 'Content-Type': 'application/x-www-form-urlencoded' }, body: new URLSearchParams(fields).toString() });
  const data = await response.json();
  if (!response.ok || data.error || data.ok === false) throw new Error(data.error || String(response.status));
  return data;
};
window.rcEsc = value => String(value == null ? '' : value).replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));
(function () {
  'use strict';
  const boot = window.TC_BOOT || {}, values = { auto: '자동 · Auto', ko: '한국어', en: 'English' };
  document.querySelectorAll('[data-language]').forEach(select => {
    select.value = boot.language || 'auto';
    select.addEventListener('change', async () => {
      select.disabled = true;
      try { await window.rcPost('api/settings', { key: '0. Mode|Language', value: values[select.value] }); location.reload(); }
      catch (error) { select.disabled = false; select.value = boot.language || 'auto'; window.alert(error.message); }
    });
  });
  const I = window.TC_I18N;
  if (I) I.apply();
  window.tcModeBanner = function (state) {
    const host = document.getElementById('mode-banner'); if (!host || !I) return;
    const modes = { game: 'modeGame', check: 'modeCheck', analysis: 'modeAnalysis', leak: 'modeLeak', off: 'modeOff' };
    let select = host.querySelector('select');
    if (!select) {
      host.className = 'mode-banner'; host.innerHTML = '<label>' + window.rcEsc(I.t('modeLegend')) + ' <select></select></label><p class="small muted"></p>';
      select = host.querySelector('select'); Object.entries(modes).forEach(([value, label]) => select.add(new Option(I.t(label), value)));
      select.addEventListener('change', async () => {
        select.disabled = true;
        try { const reply = await window.rcPost('api/ui', { key: 'memoryMode', value: select.value }); window.tcModeBanner(reply.state); }
        catch (error) { document.getElementById('sub').textContent = error.message; }
        finally { select.disabled = !state.memoryAvailable; }
      });
    }
    if (state.memoryMode === 'custom' && !select.querySelector('[value="custom"]')) select.add(new Option(I.t('custom'), 'custom'));
    select.value = state.memoryMode; select.disabled = !state.memoryAvailable;
    const keys = { game: 'bannerGame', check: 'bannerCheck', analysis: 'bannerAnalysis', leak: 'bannerLeak', off: 'bannerOff' };
    const note = host.querySelector('p'); note.textContent = keys[state.memoryMode] ? I.t(keys[state.memoryMode]) : I.t('advModeNote'); note.hidden = !note.textContent;
  };
})();
