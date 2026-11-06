// SPDX-License-Identifier: GPL-3.0-only
using System;
using System.Collections.Generic;
using System.Diagnostics;
using BepInEx.Configuration;
using BepInEx.Logging;
using EFT.AssetsManager;
using EFT.CameraControl;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Tushonka.Cleaner.SceneCleanup
{
    internal sealed class RuntimeState
    {
        // 후보는 CSV 기준 최대 맵(Streets)이 약 1.4만 개이므로 MaxRecords는 네 배 이상 여유가 있습니다.
        // 스캔은 노드마다 약 3단계라 Streets의 노드 119만 개에 단계 상한이 낮으면 수 분이 걸립니다. 시간 예산으로 끊습니다.
        private const int MaxRecords=65536, MaxSteps=512, MaxScanSteps=16384, MaxSceneRoots=8192, MaxDepth=128, AuditPerFrame=4;
        private readonly ConfigFile config;
        private readonly CleanupSettings settings;
        private readonly RuleBook rules;
        private readonly ExclusionSet exclusions;
        private readonly ManualLogSource log;
        private readonly SceneSafety safety=new SceneSafety();
        private readonly GraphicsState graphics=new GraphicsState();
        private readonly WorkBudget budget=new WorkBudget();
        private readonly Dictionary<int,bool> decalDecisions=new Dictionary<int,bool>(MaxRecords);
        private readonly ReferenceLedger<MeshRenderer,bool> renderers=new ReferenceLedger<MeshRenderer,bool>(MaxRecords);
        private readonly Transform[] parents=new Transform[MaxRecords], rootsAtScan=new Transform[MaxRecords];
        private readonly int[] sceneHandles=new int[MaxRecords];
        private readonly bool[] hidden=new bool[MaxRecords];
        private readonly Vector3[] centers=new Vector3[MaxRecords], extents=new Vector3[MaxRecords];
        private readonly ReferenceLedger<Light,LightShadows> lights=new ReferenceLedger<Light,LightShadows>(2048);
        private readonly Dictionary<int,int> lightSlots=new Dictionary<int,int>(2048);
        private readonly MuzzleLight[] muzzleQueue=new MuzzleLight[256];
        private readonly Dictionary<int,byte> queuedMuzzles=new Dictionary<int,byte>(256);
        private readonly AmmoPoolObject[] shells=new AmmoPoolObject[256];
        private readonly long[] shellOrder=new long[256];
        private long shellSequence;
        private readonly List<GameObject> roots=new List<GameObject>(MaxSceneRoots);
        private readonly Transform[] stack=new Transform[MaxDepth];
        private readonly int[] childIndexes=new int[MaxDepth];
        private readonly int[] hiddenCounts=new int[7];
        private object world;
        private bool raidActive,graphicsDirty,scanning,capacityReached;
        private float readyAt;
        private int sceneIndex,rootIndex,depth=-1,applyIndex,auditIndex,scanned,graphicsSlot,lightIndex,lightAudit,decalsSkipped;
        private int muzzleRead,muzzleWrite,muzzleLightIndex;
        private float lastMs;
        internal RuntimeState(ConfigFile config,ManualLogSource log,CleanupSettings settings,RuleBook rules,ExclusionSet exclusions)
        {
            this.config=config; this.log=log; this.settings=settings; this.rules=rules; this.exclusions=exclusions;
            config.SettingChanged+=SettingChanged;
        }
        internal SceneCleanupStatus Status
        {
            get { return new SceneCleanupStatus { Level=settings.Level.Value,Candidates=renderers.Count,ScannedNodes=scanned,
                LitterHidden=hiddenCounts[0],RubbleHidden=hiddenCounts[1],CasingsHidden=hiddenCounts[2],FoodHidden=hiddenCounts[3],
                PuddlesHidden=hiddenCounts[4],GlassHidden=hiddenCounts[5],DecalsSkipped=decalsSkipped,
                LastApplyMilliseconds=lastMs,Scanning=scanning&&settings.Level.Value!=SceneCleanupLevel.Off,GrassHidden=GrassHidden,RaidActive=raidActive,DecalsApplyNextRaid=true,CapacityReached=capacityReached }; }
        }
        // 레이드 안에서 많이 가림일 때만 풀을 그리지 않습니다. 값을 저장하지 않아 복원할 것이 없습니다.
        internal bool GrassHidden { get { return raidActive && settings.Level.Value==SceneCleanupLevel.Max && settings.Grass.Value; } }
        internal void SetLevel(SceneCleanupLevel level) { settings.Level.Value=level; }
        private void SettingChanged(object sender,SettingChangedEventArgs args)
        {
            if(!args.ChangedSetting.Definition.Section.StartsWith("Graphics",StringComparison.Ordinal)) return;
            graphicsDirty=true; graphicsSlot=0; applyIndex=0; lightIndex=0; lightAudit=lights.Count;
        }
        // 은신처도 GameWorld 계열이므로 게임 타입으로 제외합니다.
        private static bool IsHideout(object instance)
        {
            for(Type t=instance.GetType();t!=null;t=t.BaseType) if(t.Name=="HideoutGameWorld") return true;
            return false;
        }
        internal void BeginRaid(object instance)
        {
            if(IsHideout(instance)) return;
            if(ReferenceEquals(world,instance)) return;
            if(world!=null) EndRaid(world);
            world=instance; raidActive=true; readyAt=Time.realtimeSinceStartup+settings.Delay.Value;
            graphicsDirty=true; graphicsSlot=0; scanning=true; sceneIndex=0; rootIndex=0; depth=-1;
        }
        internal void EndRaid(object instance)
        {
            // 은신처 정리는 다음 맵을 불러오는 도중일 수 있습니다. 로딩 중 데칼 판정을 지우면 안 됩니다.
            if(IsHideout(instance)) return;
            if(world!=null && !ReferenceEquals(world,instance)) return;
            // 게임이 풀과 씬을 정리하기 전에 원래 값을 돌려주고 모든 강한 참조를 비웁니다.
            RestoreAllRenderers(); RestoreAllLights();
            graphics.Restore();
            ClearReferences();
            world=null; raidActive=false; decalsSkipped=0;
        }
        // 맵 데칼 등록(StaticDeferredDecalRenderer.AddEarlyDecals)은 레이드 시작 전 한 프레임에 몰립니다.
        // 판정은 등록 때의 단계를 읽습니다. 메뉴에서 먼저 불린 등록이 단계를 고정하면 안 됩니다.
        // 프레임 예산으로 끊지 않습니다. 거부 판정이 원래 등록(문자열 해시·목록 선형 검색)보다 싸기 때문입니다.
        internal bool SkipDecal(StaticDeferredDecal decal)
        {
            if(decal==null) return false;
            int id=decal.GetInstanceID(); bool previous;
            if(decalDecisions.TryGetValue(id,out previous)) return previous;
            if(raidActive || decalDecisions.Count>=MaxRecords) return false;
            bool skip=SkipLoadingDecal(decal);
            decalDecisions.Add(id,skip);
            if(skip) ++decalsSkipped;
            return skip;
        }
        private bool SkipLoadingDecal(StaticDeferredDecal decal)
        {
            // LightTouch는 로딩 시점에 거리를 알 수 없습니다. 가까운 데칼을 숨기지 않도록 유지합니다.
            if(settings.Level.Value<SceneCleanupLevel.Balanced || !settings.Categories[(int)CleanupCategory.Decals].Value) return false;
            // 등록 후 다시 만지면 버퍼 재생성 비용이 발생하므로 레이드 중 등록·해제를 건드리지 않습니다.
            var scene=decal.gameObject.scene;
            if(!scene.IsValid()) return false;
            string sceneName=scene.name;
            if(sceneName=="DontDestroyOnLoad" || sceneName.IndexOf("hideout",StringComparison.OrdinalIgnoreCase)>=0) return false;
            // 얼룩, 먼지, 핏자국처럼 재질로 확인한 맵 데칼만 숨깁니다. 표지, 안내문, 차량 그림자는 그대로 남습니다.
            Material material=decal.DecalMaterial;
            string decalName=decal.name;
            if(material==null || !rules.MatchesDecal(decalName,material.name) || exclusions.Matches(decalName)) return false;
            Vector3 s=decal.transform.lossyScale;
            float size=Mathf.Max(Mathf.Abs(s.x),Mathf.Max(Mathf.Abs(s.y),Mathf.Abs(s.z)));
            if(!(size>0) || size>rules.DecalSizeLimit) return false;
            return safety.LoadingDecalSafe(decal.transform);
        }
        private long OpenBudget() {return budget.Open(Time.frameCount,(long)(settings.Budget.Value*Stopwatch.Frequency/1000.0));}
        internal void Tick()
        {
            if(!raidActive) return;
            long start=Stopwatch.GetTimestamp();
            long remaining=OpenBudget();
            if(remaining<=0) return;
            long deadline=start+remaining;
            int steps=0,changes=0;
            if(graphicsDirty && Stopwatch.GetTimestamp()<deadline)
            {
                if(settings.Level.Value>=SceneCleanupLevel.Balanced) graphics.Capture();
                graphics.Apply(graphicsSlot++,settings,settings.Level.Value);
                if(graphicsSlot==GraphicsState.SlotCount)
                {
                    graphicsDirty=false; graphicsSlot=0;
                    if(settings.Level.Value<SceneCleanupLevel.Balanced) graphics.ForgetSnapshot();
                }
            }
            Camera camera=CameraManager.instance==null?null:CameraManager.instance.Camera;
            bool valid=camera!=null && camera.isActiveAndEnabled;
            Vector3 position=valid?camera.transform.position:default(Vector3);
            float range=valid?DistancePolicy.Range(settings.Distance.Value,60,camera.fieldOfView):0;
            if(valid && CameraManager.instance.OpticCameraManager!=null)
            {
                var opticManager=CameraManager.instance.OpticCameraManager;
                Camera optic=opticManager.Camera;
                if(opticManager.IsAnyOpticCameraRendering && optic!=null && optic.isActiveAndEnabled)
                    range=Mathf.Max(range,DistancePolicy.Range(settings.Distance.Value,60,optic.fieldOfView));
            }
            // 총은 풀에서 재사용됩니다. 설정이 바뀌면 전부, 평소에는 프레임당 한 개씩만 소유자를 다시 확인합니다.
            if(lightAudit==0 && lights.Count>0) lightAudit=1;
            int applied=0,visitLimit=VisitLimit(settings.Level.Value),changeLimit=settings.ChangesPerFrame.Value;
            while(++steps<=MaxSteps && Stopwatch.GetTimestamp()<deadline)
            {
                bool work=false;
                if(muzzleRead!=muzzleWrite && steps%3==0) { ApplyMuzzleStep(); work=true; }
                else if(lightAudit>0 && steps%3==1) { RestoreLightStep(); --lightAudit; work=true; }
                else if(applied<visitLimit && changes<changeLimit)
                {
                    if(ApplyNext(position,range,valid,applied<AuditPerFrame)) ++changes;
                    ++applied;
                    work=true;
                }
                if(!work && muzzleRead==muzzleWrite && lightAudit<=0) break;
            }
            // 스캔은 변경 작업이 쓰고 남은 시간을 전부 씁니다. 끔에서는 돌지 않고, 단계를 올리는 순간 시작합니다.
            if(scanning && settings.Level.Value!=SceneCleanupLevel.Off && Time.realtimeSinceStartup>=readyAt)
                for(int i=0;i<MaxScanSteps && scanning && Stopwatch.GetTimestamp()<deadline;i++) ScanStep();
            lastMs=(float)((Stopwatch.GetTimestamp()-start)*1000.0/Stopwatch.Frequency);
            budget.Spend(Stopwatch.GetTimestamp()-start);
        }
        private void ScanStep()
        {
            if(depth>=0)
            {
                Transform node=stack[depth];
                if(node==null) { stack[depth--]=null; return; }
                int next=childIndexes[depth];
                if(next<0)
                {
                    childIndexes[depth]=0;
                    Inspect(node);
                    return;
                }
                if(next<node.childCount)
                {
                    childIndexes[depth]=next+1;
                    if(depth+1>=MaxDepth) { capacityReached=true; return; }
                    ++depth; stack[depth]=node.GetChild(next); childIndexes[depth]=-1;
                }
                else { stack[depth--]=null; return; }
                return;
            }
            if(rootIndex<roots.Count)
            {
                var root=roots[rootIndex++];
                if(root!=null) { stack[0]=root.transform; childIndexes[0]=-1; depth=0; }
                return;
            }
            roots.Clear(); rootIndex=0;
            if(sceneIndex>=SceneManager.sceneCount) { scanning=false; return; }
            Scene scene=SceneManager.GetSceneAt(sceneIndex++);
            if(!scene.IsValid() || !scene.isLoaded || scene.name=="DontDestroyOnLoad") return;
            if(scene.rootCount>MaxSceneRoots) { capacityReached=true; return; }
            scene.GetRootGameObjects(roots);
        }
        // 전체 적용 중이면 빠르게 돌고, 끝난 뒤에는 할 일만 합니다.
        // Balanced·Max는 거리와 무관하므로 감사만, LightTouch는 저장된 상자로 거리만 재므로 비용이 작습니다.
        private int VisitLimit(SceneCleanupLevel level)
        {
            int count=renderers.Count;
            if(applyIndex<count) return Math.Min(count,MaxSteps);
            if(level==SceneCleanupLevel.Off) return 0;
            return Math.Min(count,level==SceneCleanupLevel.LightTouch?MaxSteps:AuditPerFrame);
        }
        private bool ApplyNext(Vector3 position,float range,bool valid,bool auditTurn)
        {
            if(applyIndex<renderers.Count) return ApplyOne(applyIndex++,position,range,valid,false);
            if(auditIndex>=renderers.Count) auditIndex=0;
            return ApplyOne(auditIndex++,position,range,valid,auditTurn);
        }
        // 숨김은 Renderer.enabled가 아니라 물체 자신의 활성 상태로 합니다.
        // 게임의 PerfectCullingBakeGroup.Toggle이 enabled를 계속 덮어쓰기 때문입니다. 비활성 물체에는 그 쓰기가 값만 남기므로
        // 숨긴 동안 게임이 켜지 못하고, 복원하면 게임이 마지막으로 쓴 가림 상태가 그대로 적용됩니다.
        private bool ApplyOne(int i,Vector3 position,float range,bool valid,bool audit)
        {
            MeshRenderer renderer=renderers.Target(i);
            if(renderer==null) { SetHidden(i,false); return false; }
            bool target=false;
            SceneCleanupLevel level=settings.Level.Value;
            if(level!=SceneCleanupLevel.Off && settings.Categories[renderers.Category(i)].Value)
            {
                Vector3 c=centers[i],e=extents[i];
                target=level>=SceneCleanupLevel.Balanced || DistancePolicy.Hidden(DistancePolicy.SquaredToBox(position.x,position.y,position.z,c.x,c.y,c.z,e.x,e.y,e.z),range,hidden[i],valid);
            }
            // 바뀔 일이 없으면 네이티브 호출 없이 끝냅니다. 감사 차례만 현재 상태를 다시 읽습니다.
            if(target==hidden[i] && !audit) return false;
            GameObject obj=renderer.gameObject;
            // 숨길 때 현재 위치·콜라이더·게임 컴포넌트를 다시 확인합니다. 안전하지 않게 바뀌었으면 원상복원합니다.
            if(target && !(StillInPlace(i,obj,renderer.transform) && safety.SmallGroupSafe(renderer.transform,false))) target=false;
            bool active=obj.activeSelf;
            if(target)
            {
                // 게임이 먼저 끈 물체는 소유하지 않습니다. 복원할 때 켜면 원래 상태가 틀어집니다.
                if(!hidden[i] && !active) return false;
                SetHidden(i,true);
                if(!active) return false;
                obj.SetActive(false);
                return true;
            }
            if(!hidden[i]) return false;
            SetHidden(i,false);
            if(active==renderers.Original(i)) return false;
            obj.SetActive(renderers.Original(i));
            return true;
        }
        private bool StillInPlace(int i,GameObject obj,Transform transform)
        { return obj.scene.handle==sceneHandles[i] && transform.parent==parents[i] && transform.root==rootsAtScan[i]; }
        private void SetHidden(int index,bool value)
        {
            if(hidden[index]==value) return;
            hidden[index]=value;
            hiddenCounts[renderers.Category(index)]+=value?1:-1;
        }
        private void Inspect(Transform node)
        {
            ++scanned;
            if(node.childCount!=0) return;
            MeshRenderer renderer=node.GetComponent<MeshRenderer>();
            if(renderer==null) return;
            GameObject obj=renderer.gameObject;
            // enabled로 거르면 안 됩니다. 게임 가림 처리가 안 보이는 구역을 꺼 두므로 스폰 위치에 따라 후보가 달라집니다.
            if(!obj.activeInHierarchy) return;
            string name=node.name;
            if(!rules.MatchesName(name) || exclusions.Matches(name)) return;
            Bounds bounds=renderer.bounds;
            Vector3 size=bounds.size;
            var category=rules.Classify(name,"MeshRenderer",Mathf.Max(size.x,Mathf.Max(size.y,size.z)),false);
            // MeshRenderer 데칼도 레이드 중 토글하지 않습니다. 등록 훅만 사용합니다.
            if(category==CleanupCategory.None || category==CleanupCategory.Decals || !safety.SmallGroupSafe(node,false)) return;
            int index=renderers.Count;
            if(!renderers.Add(renderer,obj.activeSelf,(int)category)) { capacityReached=true; return; }
            parents[index]=node.parent; rootsAtScan[index]=node.root; sceneHandles[index]=obj.scene.handle;
            centers[index]=bounds.center; extents[index]=bounds.extents;
        }
        internal void OnMuzzle(MuzzleLight muzzle)
        {
            if(!raidActive || muzzle==null || muzzle.Lights==null || muzzle.Lights.Length>64) return;
            int next=(muzzleWrite+1)%muzzleQueue.Length;
            if(next==muzzleRead) return;
            int muzzleId=muzzle.GetInstanceID();
            if(queuedMuzzles.ContainsKey(muzzleId)) return;
            queuedMuzzles.Add(muzzleId,0);
            // 훅에서는 고정 큐에 넣기만 합니다. 광원 조회와 적용은 Tick의 공통 예산을 사용합니다.
            muzzleQueue[muzzleWrite]=muzzle; muzzleWrite=next;
        }
        private void ApplyMuzzleStep()
        {
            MuzzleLight muzzle=muzzleQueue[muzzleRead];
            if(muzzle==null || muzzle.Lights==null || muzzleLightIndex>=muzzle.Lights.Length)
            {
                if(!ReferenceEquals(muzzle,null)) queuedMuzzles.Remove(muzzle.GetInstanceID());
                muzzleQueue[muzzleRead]=null; muzzleRead=(muzzleRead+1)%muzzleQueue.Length; muzzleLightIndex=0; return;
            }
            EFT.Player owner=FindOwner(muzzle.transform);
            Light light=muzzle.Lights[muzzleLightIndex++];
            if(light==null) return;
            int i=FindLight(light);
            // 소유자를 확정하지 못하면 자신의 광원일 수 있으므로 원래 상태로 유지합니다.
            bool reduce=settings.Level.Value>=SceneCleanupLevel.Balanced && settings.MuzzleShadows.Value && owner!=null && !owner.IsYourPlayer;
            if(reduce)
            {
                if(i<0)
                {
                    i=lights.Count;
                    if(!lights.Add(light,light.shadows,0)) return;
                    lightSlots.Add(light.GetInstanceID(),i);
                }
                light.shadows=LightShadows.None;
            }
            else if(i>=0) light.shadows=lights.Original(i);
        }
        private int FindLight(Light light)
        { int index; return lightSlots.TryGetValue(light.GetInstanceID(),out index)?index:-1; }
        private static EFT.Player FindOwner(Transform transform)
        {
            int depth=0;
            for(Transform t=transform;t!=null && depth++<64;t=t.parent)
            {
                EFT.Player owner=t.GetComponent<EFT.Player>();
                if(owner!=null) return owner;
            }
            return null;
        }
        private void RestoreLightStep()
        {
            if(lightIndex>=lights.Count) lightIndex=0;
            int i=lightIndex++;
            Light light=lights.Target(i);
            if(light==null) return;
            EFT.Player owner=FindOwner(light.transform);
            if(settings.Level.Value<SceneCleanupLevel.Balanced || !settings.MuzzleShadows.Value || owner==null || owner.IsYourPlayer) light.shadows=lights.Original(i);
        }
        private static int ShellCapFor(SceneCleanupLevel level)
        {
            switch(level)
            {
                case SceneCleanupLevel.LightTouch: return 100;
                case SceneCleanupLevel.Balanced: return 60;
                case SceneCleanupLevel.Max: return 30;
                default: return 0;
            }
        }
        internal void OnShell(AmmoPoolObject shell)
        {
            if(!raidActive || shell==null || !settings.ShellCap.Value) return;
            int cap=ShellCapFor(settings.Level.Value);
            if(cap==0) return;
            int live=0,free=-1;
            for(int i=0;i<shells.Length;i++)
            {
                if(shells[i]==null) { if(free<0) free=i; continue; }
                if(shells[i]==shell) return;
                ++live;
            }
            // 일찍 돌아간 탄피가 빈칸을 남기므로 칸 위치가 아니라 등록 순서로 가장 오래된 것을 고릅니다. 단계를 낮추면 여기서 한 번에 줄어듭니다.
            while(live>=cap)
            {
                int oldest=-1;
                for(int i=0;i<shells.Length;i++) if(shells[i]!=null && (oldest<0 || shellOrder[i]<shellOrder[oldest])) oldest=i;
                if(oldest<0) break;
                shells[oldest].ShouldBeDestroyed=true; // 게임의 원래 반환 경로가 처리합니다.
                shells[oldest]=null; --live;
                if(free<0) free=oldest;
            }
            if(free<0) return;
            shells[free]=shell; shellOrder[free]=++shellSequence;
        }
        internal void ForgetShell(AmmoPoolObject shell)
        { for(int i=0;i<shells.Length;i++) if(shells[i]==shell) shells[i]=null; }
        private void RestoreAllRenderers()
        { for(int i=0;i<renderers.Count;i++) if(hidden[i] && renderers.Target(i)!=null) renderers.Target(i).gameObject.SetActive(renderers.Original(i)); }
        private void RestoreAllLights()
        { for(int i=0;i<lights.Count;i++) if(lights.Target(i)!=null) lights.Target(i).shadows=lights.Original(i); }
        private void ClearReferences()
        {
            renderers.Clear(); lights.Clear(); lightSlots.Clear(); queuedMuzzles.Clear(); decalDecisions.Clear(); roots.Clear(); safety.Clear();
            Array.Clear(muzzleQueue,0,muzzleQueue.Length); muzzleRead=0; muzzleWrite=0; muzzleLightIndex=0;
            Array.Clear(parents,0,parents.Length); Array.Clear(rootsAtScan,0,rootsAtScan.Length); Array.Clear(hidden,0,hidden.Length);
            Array.Clear(sceneHandles,0,sceneHandles.Length);
            Array.Clear(stack,0,stack.Length); Array.Clear(childIndexes,0,childIndexes.Length); Array.Clear(shells,0,shells.Length); Array.Clear(hiddenCounts,0,hiddenCounts.Length);
            depth=-1; applyIndex=0; auditIndex=0; lightIndex=0; lightAudit=0; scanned=0; scanning=false; capacityReached=false; shellSequence=0;
        }
        internal void Shutdown()
        {
            config.SettingChanged-=SettingChanged;
            RestoreAllRenderers(); RestoreAllLights(); graphics.Restore(); ClearReferences(); world=null; raidActive=false;
        }
    }
}
