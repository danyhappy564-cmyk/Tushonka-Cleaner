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
        private const int MaxRecords=65536, MaxSteps=512, MaxSceneRoots=8192, MaxDepth=128;
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
        private readonly ReferenceLedger<Light,LightShadows> lights=new ReferenceLedger<Light,LightShadows>(2048);
        private readonly Dictionary<int,int> lightSlots=new Dictionary<int,int>(2048);
        private readonly MuzzleLight[] muzzleQueue=new MuzzleLight[256];
        private readonly Dictionary<int,byte> queuedMuzzles=new Dictionary<int,byte>(256);
        private readonly AmmoPoolObject[] shells=new AmmoPoolObject[256];
        private readonly List<GameObject> roots=new List<GameObject>(MaxSceneRoots);
        private readonly Transform[] stack=new Transform[MaxDepth];
        private readonly int[] childIndexes=new int[MaxDepth];
        private readonly int[] hiddenCounts=new int[7];
        private object world;
        private bool raidActive,decalsFrozen,decalsEnabled,graphicsDirty,scanning,capacityReached;
        private SceneCleanupLevel decalLevel;
        private float readyAt;
        private int sceneIndex,rootIndex,depth=-1,applyIndex,scanned,graphicsSlot,lightIndex,shellCursor,impacts,decalsSkipped;
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
                LastApplyMilliseconds=lastMs,Scanning=scanning,RaidActive=raidActive,DecalsApplyNextRaid=true,CapacityReached=capacityReached }; }
        }
        internal void SetLevel(SceneCleanupLevel level) { settings.Level.Value=level; }
        private void SettingChanged(object sender,SettingChangedEventArgs args)
        {
            if(!args.ChangedSetting.Definition.Section.StartsWith("Graphics",StringComparison.Ordinal)) return;
            graphicsDirty=true; graphicsSlot=0; applyIndex=0; lightIndex=0;
        }
        internal void BeginRaid(object instance)
        {
            // 은신처도 GameWorld 계열이므로 게임 타입으로 제외합니다.
            for(Type t=instance.GetType();t!=null;t=t.BaseType) if(t.Name=="HideoutGameWorld") return;
            if(ReferenceEquals(world,instance)) return;
            if(world!=null) EndRaid(world);
            world=instance; raidActive=true; readyAt=Time.realtimeSinceStartup+settings.Delay.Value;
            graphicsDirty=true; graphicsSlot=0; scanning=true; sceneIndex=0; rootIndex=0; depth=-1;
            FreezeDecals();
        }
        internal void EndRaid(object instance)
        {
            if(world!=null && !ReferenceEquals(world,instance)) return;
            // 게임이 풀과 씬을 정리하기 전에 원래 값을 돌려주고 모든 강한 참조를 비웁니다.
            RestoreAllRenderers(); RestoreAllLights();
            graphics.Restore();
            ClearReferences();
            world=null; raidActive=false; decalsFrozen=false; decalsEnabled=false; decalsSkipped=0; impacts=0;
        }
        private void FreezeDecals()
        {
            if(decalsFrozen) return;
            decalLevel=settings.Level.Value; decalsEnabled=settings.Categories[6].Value;
            decalsFrozen=true;
        }
        internal bool SkipDecal(StaticDeferredDecal decal)
        {
            if(decal==null) return false;
            int id=decal.GetInstanceID(); bool previous;
            if(decalDecisions.TryGetValue(id,out previous)) return previous;
            if(raidActive || decalDecisions.Count>=MaxRecords) return false;
            long started=Stopwatch.GetTimestamp();
            // 로딩 프레임에서도 Tick과 등록 훅이 같은 예산을 공유합니다. 소진하면 원래 등록을 허용합니다.
            if(OpenBudget()<Stopwatch.Frequency/10000) { decalDecisions.Add(id,false); return false; }
            try
            {
                bool skip=SkipLoadingDecal(decal);
                decalDecisions.Add(id,skip);
                if(skip) ++decalsSkipped;
                return skip;
            }
            finally { budget.Spend(Stopwatch.GetTimestamp()-started); }
        }
        private bool SkipLoadingDecal(StaticDeferredDecal decal)
        {
            // 등록 후 다시 만지면 버퍼 재생성 비용이 발생하므로 레이드 중 등록·해제를 건드리지 않습니다.
            if(decal==null || !decal.gameObject.scene.IsValid()) return false;
            var scene=decal.gameObject.scene;
            if(scene.name=="DontDestroyOnLoad" || scene.name.IndexOf("hideout",StringComparison.OrdinalIgnoreCase)>=0) return false;
            FreezeDecals();
            if(decalLevel==SceneCleanupLevel.Off || !decalsEnabled) return false;
            // LightTouch는 로딩 시점에 거리를 알 수 없습니다. 가까운 데칼을 숨기지 않도록 유지합니다.
            if(decalLevel==SceneCleanupLevel.LightTouch) return false;
            string name=decal.name;
            if(exclusions.Matches(name) || !safety.SmallGroupSafe(decal.transform,true)) return false;
            Vector3 s=decal.transform.lossyScale;
            float size=Mathf.Max(Mathf.Abs(s.x),Mathf.Max(Mathf.Abs(s.y),Mathf.Abs(s.z)));
            if(rules.Classify(name,"MeshRenderer",size,false)!=CleanupCategory.Decals) return false;
            return true;
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
                if(graphicsSlot==10)
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
            bool scanTurn=true;
            int applied=0,lightsChecked=0;
            while(++steps<=MaxSteps && Stopwatch.GetTimestamp()<deadline)
            {
                bool work=false;
                if(scanning && Time.realtimeSinceStartup>=readyAt && scanTurn) { ScanStep(); work=true; }
                else if(muzzleRead!=muzzleWrite && steps%3==0) { ApplyMuzzleStep(); work=true; }
                else if(lightsChecked<lights.Count && steps%3==1) { RestoreLightStep(); ++lightsChecked; work=true; }
                else if(applied<renderers.Count && changes<settings.ChangesPerFrame.Value)
                {
                    if(ApplyOne(position,range,valid)) ++changes;
                    ++applied;
                    work=true;
                }
                scanTurn=!scanTurn;
                if(!work && !(scanning && Time.realtimeSinceStartup>=readyAt) && muzzleRead==muzzleWrite && lightsChecked>=lights.Count) break;
            }
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
        private bool ApplyOne(Vector3 position,float range,bool valid)
        {
            if(applyIndex>=renderers.Count) applyIndex=0;
            int i=applyIndex++;
            MeshRenderer renderer=renderers.Target(i);
            if(renderer==null) { SetHidden(i,false); return false; }
            bool target=false;
            var category=(CleanupCategory)renderers.Category(i);
            SceneCleanupLevel level=settings.Level.Value;
            if(level!=SceneCleanupLevel.Off && settings.Categories[(int)category].Value && renderer.gameObject.scene.handle==sceneHandles[i] && renderer.transform.parent==parents[i] && renderer.transform.root==rootsAtScan[i])
            {
                target=level>=SceneCleanupLevel.Balanced || DistancePolicy.Hidden((renderer.bounds.ClosestPoint(position)-position).sqrMagnitude,range,hidden[i],valid);
            }
            // 숨길 때 현재 콜라이더·게임 컴포넌트를 다시 확인합니다. 안전하지 않게 바뀌었으면 원상복원합니다.
            if(target && !safety.SmallGroupSafe(renderer.transform,false)) target=false;
            bool desired=renderers.Original(i) && !target;
            bool changed=renderer.enabled!=desired;
            if(changed) renderer.enabled=desired;
            SetHidden(i,target && renderers.Original(i));
            return changed;
        }
        private void SetHidden(int index,bool value)
        {
            if(hidden[index]==value) return;
            hidden[index]=value;
            hiddenCounts[renderers.Category(index)]+=value?1:-1;
        }
        private void Inspect(Transform node)
        {
            ++scanned;
            if(node.childCount!=0 || !node.gameObject.scene.IsValid() || node.gameObject.scene.name=="DontDestroyOnLoad") return;
            MeshRenderer renderer=node.GetComponent<MeshRenderer>();
            if(renderer==null || !renderer.enabled) return;
            string name=node.name;
            if(exclusions.Matches(name)) return;
            Vector3 size=renderer.bounds.size;
            var category=rules.Classify(name,"MeshRenderer",Mathf.Max(size.x,Mathf.Max(size.y,size.z)),false);
            // MeshRenderer 데칼도 레이드 중 토글하지 않습니다. 등록 훅만 사용합니다.
            if(category==CleanupCategory.None || category==CleanupCategory.Decals || !safety.SmallGroupSafe(node,false)) return;
            int index=renderers.Count;
            if(!renderers.Add(renderer,renderer.enabled,(int)category)) { capacityReached=true; return; }
            parents[index]=node.parent; rootsAtScan[index]=node.root; sceneHandles[index]=node.gameObject.scene.handle;
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
        internal bool AllowImpact()
        {
            if(!raidActive) return true;
            if(settings.Level.Value<SceneCleanupLevel.Balanced || !settings.ImpactCap.Value) return true;
            int cap=settings.Level.Value==SceneCleanupLevel.Max?Math.Max(1,settings.ImpactLimit.Value/2):settings.ImpactLimit.Value;
            if(impacts>=cap) return false;
            ++impacts; return true;
        }
        internal void OnShell(AmmoPoolObject shell,bool used)
        {
            if(!raidActive || !used || shell==null || !shell.enabled || !shell.gameObject.activeInHierarchy || shell.transform.parent!=null || settings.Level.Value<SceneCleanupLevel.Balanced || !settings.ShellCap.Value) return;
            int cap=settings.Level.Value==SceneCleanupLevel.Max?Math.Max(1,settings.ShellLimit.Value/2):settings.ShellLimit.Value;
            for(int i=0;i<shells.Length;i++) if(shells[i]==shell) return;
            shellCursor%=cap;
            AmmoPoolObject previous=shells[shellCursor];
            if(previous!=null && previous!=shell) previous.ShouldBeDestroyed=true; // 게임의 원래 반환 경로가 처리합니다.
            shells[shellCursor]=shell; shellCursor=(shellCursor+1)%cap;
        }
        internal void ForgetShell(AmmoPoolObject shell)
        { for(int i=0;i<shells.Length;i++) if(shells[i]==shell) shells[i]=null; }
        private void RestoreAllRenderers()
        { for(int i=0;i<renderers.Count;i++) if(renderers.Target(i)!=null) renderers.Target(i).enabled=renderers.Original(i); }
        private void RestoreAllLights()
        { for(int i=0;i<lights.Count;i++) if(lights.Target(i)!=null) lights.Target(i).shadows=lights.Original(i); }
        private void ClearReferences()
        {
            renderers.Clear(); lights.Clear(); lightSlots.Clear(); queuedMuzzles.Clear(); decalDecisions.Clear(); roots.Clear(); safety.Clear();
            Array.Clear(muzzleQueue,0,muzzleQueue.Length); muzzleRead=0; muzzleWrite=0; muzzleLightIndex=0;
            Array.Clear(parents,0,parents.Length); Array.Clear(rootsAtScan,0,rootsAtScan.Length); Array.Clear(hidden,0,hidden.Length);
            Array.Clear(sceneHandles,0,sceneHandles.Length);
            Array.Clear(stack,0,stack.Length); Array.Clear(childIndexes,0,childIndexes.Length); Array.Clear(shells,0,shells.Length); Array.Clear(hiddenCounts,0,hiddenCounts.Length);
            depth=-1; applyIndex=0; lightIndex=0; scanned=0; scanning=false; capacityReached=false; shellCursor=0;
        }
        internal void Shutdown()
        {
            config.SettingChanged-=SettingChanged;
            RestoreAllRenderers(); RestoreAllLights(); graphics.Restore(); ClearReferences(); world=null; raidActive=false;
        }
    }
}
