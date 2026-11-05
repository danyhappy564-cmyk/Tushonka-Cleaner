// 기존 실시간 API와 보고서의 그래프를 사용합니다.
(function () {
  'use strict';
  const I=window.TC_I18N, T=window.RC||{}, $=id=>document.getElementById(id), esc=window.rcEsc, hover={};
  let online=false;
  ['memChart','fpsChart'].forEach(id=>{ $(id).addEventListener('mouseenter',()=>{hover[id]=true;});$(id).addEventListener('mouseleave',()=>{hover[id]=false;}); });
  function bars(id,list) {
    const box=$(id); $(''+id+'Card').hidden=false;
    box.innerHTML=!list?.length?'<p class="small muted">'+esc(I.t(id==='hitch'?'needView':'needProfile'))+'</p>':list.map(b=>'<div class="bar"><span class="blbl">'+esc(b.l)+'</span><span class="track"><span class="fill" style="width:'+Math.max(1,Math.min(100,b.s*100)).toFixed(1)+'%"></span></span><span class="bval">'+esc(b.v)+'</span></div>').join('');
  }
  function chart(id,data) { if(!data?.x||data.x.length<2) $(id).innerHTML='<p class="small muted">'+esc(T.collecting)+'</p>';else if(!hover[id]) window.rcChart($(id),data); }
  function disconnected(text) {
    online=false;$('sub').textContent=text;
    document.querySelectorAll('[data-act]').forEach(b=>{b.disabled=true;});
    $('raid-freed').textContent='—';$('tiles').replaceChildren();
  }
  function render(d) {
    if(d.busy)return;if(d.error){disconnected(d.error);return;}
    online=true;$('sub').textContent=d.sub;window.tcModeBanner(d.state);
    $('raid-freed').textContent=d.inRaid?Number(d.raidFreedGb).toFixed(2)+' GB':'—';
    $('alerts').innerHTML=(d.alerts||[]).map(a=>'<div class="alert">'+esc(a)+'</div>').join('');
    $('tiles').innerHTML=d.tiles.map(v=>'<article class="tile lv'+v.lv+'"><p class="metric-label">'+esc(v.k)+'</p><strong class="tile-value">'+esc(v.v)+'</strong><p class="small muted">'+esc(v.n)+'</p></article>').join('');
    chart('memChart',d.mem);chart('fpsChart',d.fps);['hitch','mods','alloc','heavy'].forEach(id=>bars(id,d[id]));$('status').textContent=d.status;
    document.querySelectorAll('[data-act]').forEach(b=>{b.disabled=!d.state.memoryAvailable;});
  }
  async function tick() {
    try {const response=await fetch('api/live',{cache:'no-store'});render(await response.json());}
    catch(error){disconnected(T.offline||error.message);}
    finally{setTimeout(tick,document.hidden?5000:1000);}
  }
  document.querySelectorAll('[data-act]').forEach(b=>{b.disabled=true;b.addEventListener('click',async()=>{
    if(!online)return;b.disabled=true;try{const r=await window.rcPost('api/action',{name:b.dataset.act});$('actmsg').textContent=r.msg||'';}catch(error){$('actmsg').textContent=error.message;}finally{b.disabled=!online;}
  });});
  tick();
})();
