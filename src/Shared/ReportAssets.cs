#nullable enable
// 게임 쪽 보고서와 웹 화면, 서버의 게임 꺼짐 화면이 함께 쓰는 CSS와 JS입니다.
namespace Tushonka.Cleaner;

internal static class ReportAssets
{
        internal const string ThemeBoot = "<script>try{var t=localStorage.getItem('tc-theme');if(t==='light'||t==='dark')document.documentElement.dataset.theme=t}catch(e){}</script>";

        internal const string Css = @"
.viz-root{color-scheme:light;--page:#f9f9f7;--surface-1:#fcfcfb;--text-primary:#0b0b0b;--text-secondary:#52514e;--muted:#898781;
--grid:#e1e0d9;--axis:#c3c2b7;--border:rgba(11,11,11,.10);--series-1:#2a78d6;--series-2:#eb6834;--series-3:#1baf7a}
@media (prefers-color-scheme:dark){:root:where(:not([data-theme=light])) .viz-root{color-scheme:dark;--page:#0d0d0d;--surface-1:#1a1a19;
--text-primary:#fff;--text-secondary:#c3c2b7;--grid:#2c2c2a;--axis:#383835;--border:rgba(255,255,255,.10);--series-1:#3987e5;--series-2:#d95926;--series-3:#199e70}}
:root[data-theme=dark] .viz-root{color-scheme:dark;--page:#0d0d0d;--surface-1:#1a1a19;--text-primary:#fff;--text-secondary:#c3c2b7;--grid:#2c2c2a;
--axis:#383835;--border:rgba(255,255,255,.10);--series-1:#3987e5;--series-2:#d95926;--series-3:#199e70}
html,body{margin:0}html:lang(ko) .viz-root{word-break:keep-all}body{background:#f9f9f7}@media (prefers-color-scheme:dark){body{background:#0d0d0d}}
.viz-root{background:var(--page);color:var(--text-primary);font:14px/1.5 system-ui,-apple-system,'Segoe UI','Malgun Gothic',sans-serif;
max-width:1180px;margin:0 auto;padding:24px 16px 48px;min-height:100vh;box-sizing:border-box}
h1{font-size:22px;margin:0 0 4px}h2{font-size:17px;margin:0 0 12px}h3{font-size:14px;margin:16px 0 8px;color:var(--text-secondary)}
.sub{color:var(--text-secondary);margin:0 0 20px}section{margin:0 0 28px}
.card{background:var(--surface-1);border:1px solid var(--border);border-radius:12px;padding:16px}
.scroll{overflow-x:auto}table{border-collapse:collapse;width:100%;font-variant-numeric:tabular-nums;background:var(--surface-1)}
th,td{padding:6px 10px;border-bottom:1px solid var(--grid);text-align:left;white-space:nowrap}th{color:var(--text-secondary);font-weight:600}
td.n,th.n{text-align:right}a{color:var(--series-1)}
.tiles{display:grid;grid-template-columns:repeat(auto-fit,minmax(170px,1fr));gap:12px;margin:0 0 16px}
.tile{border:1px solid var(--border);border-radius:10px;padding:10px 12px}.lbl{color:var(--text-secondary);font-size:12px}
.val{font-size:22px;font-weight:600}.note{color:var(--muted);font-size:12px}
.grid2{display:grid;grid-template-columns:repeat(auto-fit,minmax(min(100%,460px),1fr));gap:16px}
.chart{margin:0}figcaption{color:var(--text-secondary);font-size:13px;margin-bottom:4px}
.plot{position:relative}.plot svg{display:block;width:100%;height:auto;overflow:visible}
.legend{display:flex;flex-wrap:wrap;gap:12px;font-size:12px;color:var(--text-secondary);margin:0 0 4px}
.legend i{display:inline-block;width:10px;height:10px;border-radius:2px;margin-right:6px;vertical-align:-1px}
.bars{display:grid;gap:6px}.bar{display:grid;grid-template-columns:minmax(120px,260px) 1fr auto;gap:10px;align-items:center}
.blbl{overflow:hidden;text-overflow:ellipsis;white-space:nowrap}.track{height:10px;background:var(--grid);border-radius:4px;overflow:hidden}
.fill{display:block;height:100%;background:var(--series-1);border-radius:0 4px 4px 0}.bval{color:var(--text-secondary);font-variant-numeric:tabular-nums;white-space:nowrap}
#tip{position:fixed;pointer-events:none;display:none;background:var(--surface-1);color:var(--text-primary);border:1px solid var(--border);
border-radius:8px;padding:8px 10px;font-size:12px;box-shadow:0 4px 16px rgba(0,0,0,.18);z-index:10;font-variant-numeric:tabular-nums}
#tip i{display:inline-block;width:8px;height:8px;border-radius:2px;margin-right:6px}
.nav{display:flex;flex-wrap:wrap;gap:6px;align-items:center;margin:0 0 20px}.nav .brand{font-weight:600;margin-right:8px}
.nav a{padding:6px 12px;border-radius:8px;border:1px solid var(--border);color:var(--text-secondary);text-decoration:none}
.nav a.on{background:var(--series-1);border-color:transparent;color:#fff}
.live{font-size:12px;font-weight:600;color:#fff;background:var(--series-2);border-radius:6px;padding:2px 8px;vertical-align:2px}
.cut{font-size:12px;font-weight:600;color:var(--text-secondary);border:1px solid var(--border);border-radius:6px;padding:1px 8px;vertical-align:2px}
@media (max-width:560px){.bar{grid-template-columns:1fr auto}.track{grid-column:1/-1;order:3}}";

        internal const string Js = @"
(function(){var tip=document.getElementById('tip');var NS='http://www.w3.org/2000/svg';
function el(n,a){var e=document.createElementNS(NS,n);for(var k in a)e.setAttribute(k,a[k]);return e}
function nice(m){if(!(m>0))return 1;var p=Math.pow(10,Math.floor(Math.log10(m)));var f=m/p;return (f<=1?1:f<=2?2:f<=5?5:10)*p}
function fmt(v){return v==null?'-':(Math.abs(v)>=100?v.toFixed(0):v.toFixed(1))}
function draw(box,d){box.innerHTML='';var W=640,H=230,L=44,R=12,T=10,B=26,x=d.x,n=x.length;var max=0;d.series.forEach(function(s){s.v.forEach(function(v){if(v!=null&&v>max)max=v})});
var top=nice(max*1.08);var x0=x[0],x1=x[n-1];if(x1==x0)x1=x0+1;
function sx(v){return L+(v-x0)/(x1-x0)*(W-L-R)}function sy(v){return T+(1-v/top)*(H-T-B)}
if(d.series.length>1){var lg=document.createElement('div');lg.className='legend';d.series.forEach(function(s){var sp=document.createElement('span');
sp.innerHTML='<i style=""background:var(--series-'+s.slot+')""></i>';sp.appendChild(document.createTextNode(s.name));lg.appendChild(sp)});box.appendChild(lg)}
var svg=el('svg',{viewBox:'0 0 '+W+' '+H,role:'img','aria-label':(box.parentNode.querySelector('figcaption')||{}).textContent||''});
for(var i=0;i<=4;i++){var v=top*i/4,y=sy(v);svg.appendChild(el('line',{x1:L,x2:W-R,y1:y,y2:y,stroke:i?'var(--grid)':'var(--axis)','stroke-width':1}));
var t=el('text',{x:L-6,y:y+4,'text-anchor':'end','font-size':11,fill:'var(--muted)'});t.textContent=fmt(v);svg.appendChild(t)}
var ticks=Math.min(6,n);for(var j=0;j<ticks;j++){var xv=x0+(x1-x0)*j/Math.max(1,ticks-1);var tx=el('text',{x:sx(xv),y:H-6,'text-anchor':'middle','font-size':11,fill:'var(--muted)'});
tx.textContent=d.pre+(d.raid?Math.round(xv):xv.toFixed(0))+d.suf;svg.appendChild(tx)}
d.series.forEach(function(s){var p='',pen=false;for(var i=0;i<n;i++){var v=s.v[i];if(v==null){pen=false;continue}
p+=(pen?'L':'M')+sx(x[i]).toFixed(1)+' '+sy(v).toFixed(1);pen=true}
svg.appendChild(el('path',{d:p,fill:'none',stroke:'var(--series-'+s.slot+')','stroke-width':2,'stroke-linejoin':'round','stroke-linecap':'round'}));
if(n<=30)for(var i=0;i<n;i++){if(s.v[i]!=null)svg.appendChild(el('circle',{cx:sx(x[i]),cy:sy(s.v[i]),r:4,fill:'var(--series-'+s.slot+')',stroke:'var(--surface-1)','stroke-width':2}))}});
var cross=el('line',{y1:T,y2:H-B,stroke:'var(--axis)','stroke-width':1,visibility:'hidden'});svg.appendChild(cross);
var hit=el('rect',{x:L,y:0,width:W-L-R,height:H,fill:'transparent'});svg.appendChild(hit);box.appendChild(svg);
function show(ev){var r=svg.getBoundingClientRect();var px=(ev.clientX-r.left)/r.width*W;var best=0,bd=1e9;
for(var i=0;i<n;i++){var dd=Math.abs(sx(x[i])-px);if(dd<bd){bd=dd;best=i}}cross.setAttribute('x1',sx(x[best]));cross.setAttribute('x2',sx(x[best]));
cross.setAttribute('visibility','visible');var h='<b>'+d.pre+(d.raid?Math.round(x[best]):x[best].toFixed(1))+d.suf+'</b>';
d.series.forEach(function(s){h+='<div><i style=""background:var(--series-'+s.slot+')""></i>'+s.name.replace(/</g,'&lt;')+' '+fmt(s.v[best])+' '+d.unit+'</div>'});
tip.innerHTML=h;tip.style.display='block';var tx=ev.clientX+14,ty=ev.clientY+14;if(tx+tip.offsetWidth>innerWidth-8)tx=ev.clientX-tip.offsetWidth-14;
if(ty+tip.offsetHeight>innerHeight-8)ty=ev.clientY-tip.offsetHeight-14;tip.style.left=tx+'px';tip.style.top=ty+'px'}
hit.addEventListener('mousemove',show);hit.addEventListener('mouseleave',function(){tip.style.display='none';cross.setAttribute('visibility','hidden')})}
window.rcChart=draw;document.querySelectorAll('.plot[data-chart]').forEach(function(box){draw(box,JSON.parse(box.getAttribute('data-chart')))})})();";
}
