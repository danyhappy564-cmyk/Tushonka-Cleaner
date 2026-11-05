using System;
using System.IO;
using System.Runtime.CompilerServices;
using BepInEx.Configuration;
using BepInEx.Logging;
using Tushonka.Cleaner.SceneCleanup;
using UnityEngine;
using UnityEngine.SceneManagement;

internal static class RuntimeChecks
{
    private static int count;
    private static void Check(bool ok,string label) {++count;if(!ok)throw new Exception("runtime: "+label);}
    private static Scene CreateScene()
    {
        var s=new Scene{Data=new SceneData{Name="Sandbox"}};
        SceneManager.Scenes.Add(s); return s;
    }
    private static MeshRenderer AddRenderer(Scene scene,string name,bool enabled=true)
    {
        var obj=new GameObject(name){scene=scene}; scene.Data.Roots.Add(obj);
        var r=obj.Add<MeshRenderer>(); r.enabled=enabled; return r;
    }
    private static void RunTicks(RuntimeState state,int ticks=30) {for(int i=0;i<ticks;i++){Time.frameCount++;state.Tick();}}
    private static RuntimeState Start(string root,out ConfigFile config,out object world)
    {
        config=new ConfigFile();
        SceneCleanupModule.Init(config,new ManualLogSource(),Path.Combine(root,"data"));
        config.Bind("Graphics.Advanced","StartDelaySeconds",0f,"").Value=0;
        world=new object();
        SceneCleanupModule.State.BeginRaid(world); return SceneCleanupModule.State;
    }
    public static int Run(string root)
    {
        var scene=CreateScene();
        var normal=AddRenderer(scene,"garbage_bag_LOD");
        var disabled=AddRenderer(scene,"garbage_bag_LOD",false);
        var collision=AddRenderer(scene,"garbage_bag_LOD");collision.gameObject.Add<Collider>();
        var door=AddRenderer(scene,"Door_Glass_LOD");
        var playerObject=AddRenderer(scene,"garbage_bag_LOD");playerObject.gameObject.Add<EFT.Player>();
        var group=new GameObject("group"){scene=scene};scene.Data.Roots.Add(group);
        var sibling=AddRenderer(scene,"garbage_bag_LOD");scene.Data.Roots.Remove(sibling.gameObject);group.transform.Add(sibling.transform);
        var blocker=new GameObject("blocker"){scene=scene};blocker.Add<Collider>();group.transform.Add(blocker.transform);
        var decals=AddRenderer(scene,"decal_dirt");decals.gameObject.Add<StaticDeferredDecal>();
        var state=Start(root,out var config,out var world);
        SceneCleanupModule.SetLevel(SceneCleanupLevel.Balanced);
        RunTicks(state);
        Check(!state.Status.Scanning,"scan terminates");
        Check(!normal.gameObject.activeSelf && normal.enabled,"safe renderer hidden by object, enabled left to game");
        Check(collision.gameObject.activeSelf && door.gameObject.activeSelf && playerObject.gameObject.activeSelf && sibling.gameObject.activeSelf,"protected objects unchanged");
        Check(decals.gameObject.activeSelf,"no decal toggle in raid");
        Check(!disabled.enabled,"initially disabled unchanged");
        // 끈 주체를 구별할 수 없으므로 enabled=false 렌더러도 후보입니다. 게임 가림 처리가 꺼 둔 구역을 놓치지 않습니다.
        Check(state.Status.Candidates==2,"scan candidates include culled renderers");
        Check(state.Status.LitterHidden==2,"hidden count");
        SceneCleanupModule.SetLevel(SceneCleanupLevel.Off);RunTicks(state);
        Check(normal.gameObject.activeSelf && normal.enabled && disabled.gameObject.activeSelf && !disabled.enabled,"Off restores exact state");
        Check(QualitySettings.pixelLightCount==4 && QualitySettings.lodBias==2,"quality restored");
        QualitySettings.pixelLightCount=6;
        SceneCleanupModule.SetLevel(SceneCleanupLevel.Max);RunTicks(state);
        SceneCleanupModule.SetLevel(SceneCleanupLevel.Off);RunTicks(state);
        Check(QualitySettings.pixelLightCount==6,"reenable captures fresh original");
        var cameraObject=new GameObject("FPS Camera"){scene=scene};
        var camera=cameraObject.Add<Camera>();
        EFT.CameraControl.CameraManager.instance=new EFT.CameraControl.CameraManager{Camera=camera};
        SceneCleanupModule.SetLevel(SceneCleanupLevel.LightTouch);RunTicks(state);
        Check(!normal.gameObject.activeSelf,"far hide");
        camera.fieldOfView=11;RunTicks(state);
        Check(normal.gameObject.activeSelf,"scope current FOV restores far object");
        camera.fieldOfView=60;RunTicks(state);Check(!normal.gameObject.activeSelf,"wider FOV hides again");
        var opticObject=new GameObject("optic"){scene=scene};var optic=opticObject.Add<Camera>();optic.fieldOfView=11;
        EFT.CameraControl.CameraManager.instance.OpticCameraManager.Camera=optic;
        EFT.CameraControl.CameraManager.instance.OpticCameraManager.IsAnyOpticCameraRendering=true;
        RunTicks(state);Check(normal.gameObject.activeSelf,"independent optic FOV protects target");
        EFT.CameraControl.CameraManager.instance.OpticCameraManager.IsAnyOpticCameraRendering=false;
        config.Bind("Graphics.Categories","Litter",true,"").Value=false;RunTicks(state);
        Check(normal.gameObject.activeSelf,"category disable restores");
        config.Bind("Graphics.Categories","Litter",true,"").Value=true;RunTicks(state);
        Check(!normal.gameObject.activeSelf,"category reenable applies");
        normal.gameObject.scene=new Scene{Data=new SceneData{Name="DontDestroyOnLoad"}};
        RunTicks(state);Check(normal.gameObject.activeSelf,"scene migration restores persistent object");
        normal.gameObject.scene=scene;RunTicks(state);
        RunTicks(state);Check(!normal.gameObject.activeSelf,"back in scene hides again");
        normal.gameObject.Add<Collider>();RunTicks(state);Check(normal.gameObject.activeSelf,"new collider restores renderer");
        SceneCleanupModule.SetLevel(SceneCleanupLevel.Off);RunTicks(state);
        long before=GC.GetAllocatedBytesForCurrentThread();
        RunTicks(state,200);
        Check(GC.GetAllocatedBytesForCurrentThread()==before,"steady Tick no managed allocation in harness");
        MuzzleAndLimits(state,config,scene);
        SceneCleanupModule.Shutdown(); SceneManager.Scenes.Clear(); EFT.CameraControl.CameraManager.instance=null;
        DecalPolicy(root);
        DecalLoading(root);
        MapHierarchy(root);
        CullingCoexistence(root);
        LargeMap(root);
        VisibilityBudget(root);
        DelayedScan(root);
        var weak=LeakProbe(root);
        GC.Collect();GC.WaitForPendingFinalizers();GC.Collect();
        foreach(var reference in weak)Check(!reference.IsAlive,"actual runtime raid reference release");
        SceneCleanupModule.Shutdown();
        Console.WriteLine("PASS runtime "+count+" assertions (Unity/BepInEx model)");
        return count;
    }
    private static void DecalPolicy(string root)
    {
        var scene=CreateScene();
        var obj=new GameObject("decal_dirt"){scene=scene};scene.Data.Roots.Add(obj);
        var decal=obj.Add<StaticDeferredDecal>();
        var config=new ConfigFile();
        SceneCleanupModule.Init(config,new ManualLogSource(),Path.Combine(root,"data"));
        SceneCleanupModule.SetLevel(SceneCleanupLevel.Balanced);
        var state=SceneCleanupModule.State;
        Time.frameCount++;
        Check(state.SkipDecal(decal),"loading registration skipped");
        Check(state.SkipDecal(decal) && state.Status.DecalsSkipped==1,"decal counted once");
        var world=new object();state.BeginRaid(world);
        SceneCleanupModule.SetLevel(SceneCleanupLevel.Off);
        Check(state.SkipDecal(decal),"skipped decal stays excluded in raid");
        state.EndRaid(world);Time.frameCount++;
        Check(!state.SkipDecal(decal),"next raid follows new Off setting");
        Check(!state.Status.RaidActive,"decal decisions hold no world");
        SceneCleanupModule.Shutdown();SceneManager.Scenes.Clear();
    }
    private static GameObject Node(Scene scene,string name,GameObject parent=null)
    {
        var obj=new GameObject(name){scene=scene};
        if(parent==null) scene.Data.Roots.Add(obj); else parent.transform.Add(obj.transform);
        return obj;
    }
    private static MeshRenderer Mesh(Scene scene,string name,GameObject parent)
    {
        var obj=Node(scene,name,parent);obj.Add<MeshFilter>();return obj.Add<MeshRenderer>();
    }
    // 스트리트에서 측정한 계층입니다: 조각 / LODGroup / 폴더 / PROPS / OO{가림 표시} / 씬 그룹 루트.
    private static GameObject Props(Scene scene,string rootName)
    {
        var root=Node(scene,rootName);
        root.Add<GuidComponent>();root.Add<Koenigz.PerfectCulling.EFT.PerfectCullingCrossSceneGroup>();
        root.Add<Koenigz.PerfectCulling.EFT.PerfectCullingCrossSceneContentDoors>();root.Add<Koenigz.PerfectCulling.EFT.PerfectCullingCrossSceneContentMeshes>();
        var oo=Node(scene,"OO",root);oo.Add<Koenigz.PerfectCulling.EFT.MultisceneSharedOccluder>();
        return Node(scene,"PROPS",oo);
    }
    private static GameObject Lod(Scene scene,string name,GameObject parent)
    {
        var obj=Node(scene,name,parent);obj.Add<LODGroup>();return obj;
    }
    private static void MapHierarchy(string root)
    {
        var scene=CreateScene();
        var props=Props(scene,"SBG_City_SE_01_courtyard_A");
        var folder=Node(scene,"0.1",props);
        var shard=Mesh(scene,"shards_glass",Lod(scene,"glass (5)",folder));
        var drip=Mesh(scene,"decal_drip",Lod(scene,"block (1)",folder));
        var lodGroup=Lod(scene,"bag",folder);
        var lod0=Mesh(scene,"garbage_bag_LOD0",lodGroup);var lod1=Mesh(scene,"garbage_bag_LOD1",lodGroup);
        // 상호작용 물체와 그 자식
        var doorObject=Node(scene,"door_room",folder);doorObject.Add<EFT.Interactive.Door>();
        var underDoor=Mesh(scene,"shards_glass",Lod(scene,"door glass",doorObject));
        var containerObject=Node(scene,"crate",props);containerObject.Add<EFT.Interactive.LootableContainer>();
        var underContainer=Mesh(scene,"garbage_bag_LOD0",Node(scene,"inner",Node(scene,"deep",containerObject)));
        // 콜라이더: 자신, 같은 LOD 묶음의 형제, LOD 묶음 바깥의 짝
        var selfFolder=Node(scene,"0.2",props);
        var withCollider=Mesh(scene,"shards_glass",Lod(scene,"glass solid",selfFolder));withCollider.gameObject.Add<Collider>();
        var siblingFolder=Node(scene,"0.3",props);
        var siblingGroup=Lod(scene,"glass sibling",siblingFolder);
        var besideSibling=Mesh(scene,"shards_glass",siblingGroup);Node(scene,"box",siblingGroup).Add<Collider>();
        var outerFolder=Node(scene,"0.4",props);
        var beside=Mesh(scene,"shards_glass",Lod(scene,"glass split",outerFolder));Node(scene,"box",outerFolder).Add<Collider>();
        // 근처 조상의 콜라이더와 처음 보는 컴포넌트는 맵 컨테이너가 아닙니다.
        var solidParent=Node(scene,"solid",props);solidParent.Add<Collider>();
        var underSolid=Mesh(scene,"garbage_bag_LOD0",Lod(scene,"bag solid",solidParent));
        var unknownParent=Node(scene,"scripted",props);unknownParent.Add<UnreviewedComponent>();
        var underUnknown=Mesh(scene,"garbage_bag_LOD0",Lod(scene,"bag scripted",unknownParent));
        // 그룹 루트 위는 맵 구조이므로 보지 않습니다. 그룹 루트가 없는 계층은 끝까지 봅니다.
        var overRoot=Node(scene,"LOCATION");overRoot.Add<UnreviewedComponent>();overRoot.Add<LocationScene>();
        var group=Node(scene,"SBG_second",overRoot);group.Add<Koenigz.PerfectCulling.EFT.PerfectCullingCrossSceneGroup>();
        var aboveGroup=Mesh(scene,"shards_glass",Lod(scene,"glass high",group));
        var noGroupRoot=Node(scene,"folder_plain");noGroupRoot.Add<GuidComponent>();noGroupRoot.Add<LocationScene>();
        var plainChain=Mesh(scene,"garbage_bag_LOD0",Lod(scene,"bag plain",noGroupRoot));
        var noGroupUnknown=Node(scene,"folder_unknown");noGroupUnknown.Add<UnreviewedComponent>();
        var unknownChain=Mesh(scene,"garbage_bag_LOD0",Lod(scene,"bag unknown",noGroupUnknown));
        var state=Start(root,out var config,out var world);
        SceneCleanupModule.SetLevel(SceneCleanupLevel.Balanced);RunTicks(state,60);
        Check(!shard.gameObject.activeSelf,"measured Streets chain hidden");
        Check(state.Status.GlassHidden==2,"glass hidden count");
        Check(!lod0.gameObject.activeSelf && !lod1.gameObject.activeSelf,"all matching LOD levels hidden");
        Check(!aboveGroup.gameObject.activeSelf,"unknown map component above group root ignored");
        Check(!plainChain.gameObject.activeSelf,"reviewed containers pass without group root");
        Check(drip.gameObject.activeSelf,"mesh decal not toggled in raid");
        Check(underDoor.gameObject.activeSelf && underContainer.gameObject.activeSelf,"interactive children protected");
        Check(withCollider.gameObject.activeSelf && besideSibling.gameObject.activeSelf && beside.gameObject.activeSelf,"colliders protected");
        Check(underSolid.gameObject.activeSelf && underUnknown.gameObject.activeSelf && unknownChain.gameObject.activeSelf,"unreviewed ancestors protected");
        Check(state.Status.Candidates==5,"exact candidate set");
        SceneCleanupModule.SetLevel(SceneCleanupLevel.Off);RunTicks(state,60);
        Check(shard.gameObject.activeSelf && lod0.gameObject.activeSelf && lod1.gameObject.activeSelf && aboveGroup.gameObject.activeSelf && plainChain.gameObject.activeSelf,"Off restores map chain");
        Check(state.Status.GlassHidden==0 && state.Status.LitterHidden==0,"Off clears counts");
        SceneCleanupModule.Shutdown();SceneManager.Scenes.Clear();
    }
    // 게임의 PerfectCullingBakeGroup.Toggle은 렌더러 enabled를 직접 씁니다.
    private static void CullingCoexistence(string root)
    {
        var scene=CreateScene();
        var props=Props(scene,"SBG_culling");
        var visible=Mesh(scene,"shards_glass",Lod(scene,"glass a",props));
        var culled=Mesh(scene,"shards_glass",Lod(scene,"glass b",props));
        culled.enabled=false;
        var gameOff=Mesh(scene,"garbage_bag_LOD0",Lod(scene,"bag c",props));
        var state=Start(root,out var config,out var world);
        RunTicks(state,30);
        Check(state.Status.Candidates==3,"culled renderer found regardless of spawn");
        gameOff.gameObject.activeSelf=false;
        SceneCleanupModule.SetLevel(SceneCleanupLevel.Balanced);RunTicks(state);
        Check(!visible.gameObject.activeSelf && !culled.gameObject.activeSelf,"hidden");
        Check(state.Status.LitterHidden==0 && gameOff.gameObject.ActiveWrites==0,"object the game disabled is not ours");
        int writes=visible.gameObject.ActiveWrites;
        // 숨긴 동안 게임이 켜고 끕니다. 물체는 계속 꺼져 있고 쓰기 경쟁도 생기지 않아야 합니다.
        visible.enabled=false;culled.enabled=true;RunTicks(state);
        visible.enabled=true;RunTicks(state);visible.enabled=false;RunTicks(state);
        Check(!visible.gameObject.activeInHierarchy && !culled.gameObject.activeInHierarchy,"game culling cannot reveal hidden object");
        Check(visible.gameObject.ActiveWrites==writes,"no write fight with culling");
        // 게임이 물체를 다시 켜면 감사가 되돌립니다.
        culled.gameObject.activeSelf=true;RunTicks(state);
        Check(!culled.gameObject.activeSelf && state.Status.GlassHidden==2,"audit rehides object");
        SceneCleanupModule.SetLevel(SceneCleanupLevel.Off);RunTicks(state);
        Check(visible.gameObject.activeSelf && culled.gameObject.activeSelf,"Off reactivates");
        Check(!visible.enabled && culled.enabled,"Off keeps the state culling wrote last");
        Check(!gameOff.gameObject.activeSelf,"Off leaves game disabled object");
        SceneCleanupModule.SetLevel(SceneCleanupLevel.Balanced);RunTicks(state);
        state.EndRaid(world);
        Check(visible.gameObject.activeSelf && culled.gameObject.activeSelf && !visible.enabled && culled.enabled,"raid end restores same way");
        SceneCleanupModule.Shutdown();SceneManager.Scenes.Clear();
    }
    private static void DecalLoading(string root)
    {
        var scene=CreateScene();
        var props=Props(scene,"SBG_decals");
        var folder=Node(scene,"Decals",props);
        var config=new ConfigFile();
        SceneCleanupModule.Init(config,new ManualLogSource(),Path.Combine(root,"data"));
        var state=SceneCleanupModule.State;
        // 메뉴에서 먼저 등록이 불려도 단계가 고정되지 않아야 합니다.
        var menu=new GameObject("menu_decal"){scene=CreateScene()}.Add<StaticDeferredDecal>();
        Time.frameCount++;Check(!state.SkipDecal(menu),"Off keeps menu registration");
        SceneCleanupModule.SetLevel(SceneCleanupLevel.Balanced);
        // 맵 폴더에 수백 개가 나란히 있고 이름 자료가 없습니다. 한 프레임에 모두 판정합니다.
        var decals=new StaticDeferredDecal[300];
        for(int i=0;i<decals.Length;i++) decals[i]=Node(scene,"stain_wall_"+i,folder).Add<StaticDeferredDecal>();
        Time.frameCount++;
        int skipped=0;
        foreach(var decal in decals) if(state.SkipDecal(decal)) ++skipped;
        Check(skipped==300 && state.Status.DecalsSkipped==300,"whole loading frame skipped under map containers");
        var big=Node(scene,"huge_road_mark",folder);big.transform.lossyScale=new Vector3(30,1,30);var bigDecal=big.Add<StaticDeferredDecal>();
        Check(!state.SkipDecal(bigDecal),"large decal kept");
        var doorObject=Node(scene,"door_room",props);doorObject.Add<EFT.Interactive.Door>();
        Check(!state.SkipDecal(Node(scene,"door_stain",doorObject).Add<StaticDeferredDecal>()),"decal on interactive object kept");
        var solid=Node(scene,"stain_solid",folder);solid.Add<Collider>();
        Check(!state.SkipDecal(solid.Add<StaticDeferredDecal>()),"decal with collider kept");
        var persistent=new Scene{Data=new SceneData{Name="DontDestroyOnLoad"}};
        Check(!state.SkipDecal(new GameObject("persistent"){scene=persistent}.Add<StaticDeferredDecal>()),"persistent decal kept");
        var hideoutScene=new Scene{Data=new SceneData{Name="Hideout_Main"}};
        Check(!state.SkipDecal(new GameObject("hideout"){scene=hideoutScene}.Add<StaticDeferredDecal>()),"hideout decal kept");
        // 은신처 정리가 로딩 중에 와도 판정과 개수가 남아야 합니다.
        state.EndRaid(new HideoutGameWorld());
        Check(state.Status.DecalsSkipped==300 && state.SkipDecal(decals[0]),"hideout dispose keeps loading decisions");
        var world=new object();state.BeginRaid(world);
        Check(state.Status.RaidActive && state.Status.DecalsSkipped==300 && state.SkipDecal(decals[1]),"raid keeps loading skips");
        var late=Node(scene,"late_stain",folder).Add<StaticDeferredDecal>();
        Check(!state.SkipDecal(late),"new registration in raid untouched");
        state.EndRaid(world);
        SceneCleanupModule.SetLevel(SceneCleanupLevel.LightTouch);Time.frameCount++;
        Check(!state.SkipDecal(Node(scene,"light_stain",folder).Add<StaticDeferredDecal>()),"LightTouch keeps decals");
        SceneCleanupModule.SetLevel(SceneCleanupLevel.Balanced);
        config.Bind("Graphics.Categories","Decals",true,"").Value=false;Time.frameCount++;
        Check(!state.SkipDecal(Node(scene,"category_stain",folder).Add<StaticDeferredDecal>()),"category off keeps decals");
        SceneCleanupModule.Shutdown();SceneManager.Scenes.Clear();
    }
    // 후보가 수만 개일 때의 상한을 봅니다. 시간 측정이 아니라 호출 횟수와 할당으로 판정합니다.
    private static void LargeMap(string root)
    {
        var scene=CreateScene();
        var props=Props(scene,"SBG_large");
        const int groups=20000;
        var meshes=new MeshRenderer[groups];
        for(int i=0;i<groups;i++) meshes[i]=Mesh(scene,"shards_glass",Lod(scene,"glass",props));
        var noise=Node(scene,"noise",props);
        for(int i=0;i<60000;i++) Node(scene,"wall_LOD0",noise);
        var state=Start(root,out var config,out var world);
        int ticks=0;
        while(state.Status.Scanning && ticks<2000) {Time.frameCount++;state.Tick();++ticks;}
        Check(!state.Status.Scanning && ticks<1000,"scan is bound by time budget, not a low step cap");
        Check(state.Status.Candidates==groups && !state.Status.CapacityReached,"tens of thousands of candidates fit");
        config.Bind("Graphics.Advanced","MaxVisibilityChangesPerFrame",24,"").Value=24;
        SceneCleanupModule.SetLevel(SceneCleanupLevel.Balanced);
        RunTicks(state,1);
        Check(state.Status.GlassHidden>0 && state.Status.GlassHidden<=24,"visibility quota holds at scale");
        RunTicks(state,1000);
        Check(state.Status.GlassHidden==groups,"full pass completes");
        // 다 숨긴 뒤에는 프레임마다 후보 전체를 돌지 않습니다.
        int before=0;foreach(var m in meshes) before+=m.gameObject.ActiveWrites;
        long allocated=GC.GetAllocatedBytesForCurrentThread(),reads=Component.ComponentReads;
        RunTicks(state,120);
        Check(GC.GetAllocatedBytesForCurrentThread()==allocated,"steady Balanced Tick no managed allocation");
        // 감사는 프레임당 후보 4개, 후보마다 이 계층에서 컴포넌트 읽기 6번입니다.
        Check(Component.ComponentReads-reads<=120*4*6,"steady Balanced audits a few candidates per frame");
        int after=0;foreach(var m in meshes) after+=m.gameObject.ActiveWrites;
        Check(after==before,"steady state writes nothing");
        SceneCleanupModule.SetLevel(SceneCleanupLevel.LightTouch);RunTicks(state,1);
        var cameraObject=new GameObject("FPS Camera"){scene=scene};
        EFT.CameraControl.CameraManager.instance=new EFT.CameraControl.CameraManager{Camera=cameraObject.Add<Camera>()};
        RunTicks(state,1000);
        Check(state.Status.GlassHidden==groups,"LightTouch hides far candidates");
        allocated=GC.GetAllocatedBytesForCurrentThread();reads=Component.ComponentReads;
        RunTicks(state,120);
        Check(GC.GetAllocatedBytesForCurrentThread()==allocated,"steady LightTouch Tick no managed allocation");
        Check(Component.ComponentReads-reads<=120*4*6,"steady LightTouch distance visits need no native checks");
        EFT.CameraControl.CameraManager.instance=null;
        SceneCleanupModule.Shutdown();SceneManager.Scenes.Clear();
    }
    private static void MuzzleAndLimits(RuntimeState state,ConfigFile config,Scene scene)
    {
        SceneCleanupModule.SetLevel(SceneCleanupLevel.Balanced);RunTicks(state);
        var remoteObject=new GameObject("remote"){scene=scene}; remoteObject.Add<EFT.Player>();
        var localObject=new GameObject("local"){scene=scene};localObject.Add<EFT.Player>().IsYourPlayer=true;
        var remoteMuzzle=remoteObject.Add<MuzzleLight>(); var remoteLight=remoteObject.Add<Light>();remoteMuzzle.Lights=new[]{remoteLight};
        var localMuzzle=localObject.Add<MuzzleLight>(); var localLight=localObject.Add<Light>();localMuzzle.Lights=new[]{localLight};
        state.OnMuzzle(remoteMuzzle);state.OnMuzzle(localMuzzle);RunTicks(state);
        Check(remoteLight.shadows==LightShadows.None,"remote muzzle shadows suppressed");
        Check(localLight.shadows==LightShadows.Soft && localLight.intensity==1,"own muzzle light preserved");
        config.Bind("Graphics.Tuning","OtherMuzzleShadows",true,"").Value=false;RunTicks(state);
        Check(remoteLight.shadows==LightShadows.Soft,"muzzle original restored");
        config.Bind("Graphics.Tuning","OtherMuzzleShadows",true,"").Value=true;
        state.OnMuzzle(remoteMuzzle);RunTicks(state);
        remoteObject.transform.GetComponent<EFT.Player>().IsYourPlayer=true;RunTicks(state);
        Check(remoteLight.shadows==LightShadows.Soft,"pooled light reowned by local player restores");
        config.Bind("Graphics.Tuning","ImpactLimit",128,"").Value=3;
        Check(state.AllowImpact() && state.AllowImpact() && state.AllowImpact() && !state.AllowImpact(),"impact cap");
        SceneCleanupModule.SetLevel(SceneCleanupLevel.Off);Check(state.AllowImpact(),"Off permits original impacts");
        SceneCleanupModule.SetLevel(SceneCleanupLevel.Balanced);
        config.Bind("Graphics.Tuning","ShellLimit",64,"").Value=1;
        var first=new GameObject("used shell"){scene=scene}.Add<EFT.AssetsManager.AmmoPoolObject>();
        var second=new GameObject("used shell"){scene=scene}.Add<EFT.AssetsManager.AmmoPoolObject>();
        var live=new GameObject("live ammo"){scene=scene}.Add<EFT.AssetsManager.AmmoPoolObject>();
        state.OnShell(first,true);state.OnShell(live,false);
        Check(!first.ShouldBeDestroyed && !live.ShouldBeDestroyed,"unspent ammunition ignored");
        state.OnShell(second,true);Check(first.ShouldBeDestroyed && !second.ShouldBeDestroyed,"old shell uses game return path");
        state.ForgetShell(second);
    }
    private static void VisibilityBudget(string root)
    {
        var scene=CreateScene();
        for(int i=0;i<200;i++) AddRenderer(scene,"garbage_bag_LOD");
        var state=Start(root,out var config,out var world);
        RunTicks(state,100);
        Check(state.Status.Candidates==200,"large scene discovered");
        config.Bind("Graphics.Advanced","MaxVisibilityChangesPerFrame",24,"").Value=7;
        SceneCleanupModule.SetLevel(SceneCleanupLevel.Balanced);RunTicks(state,1);
        Check(state.Status.LitterHidden>0 && state.Status.LitterHidden<=7,"one frame visibility quota");
        RunTicks(state,100);Check(state.Status.LitterHidden==200,"bounded work eventually completes");
        SceneCleanupModule.SetLevel(SceneCleanupLevel.Off);RunTicks(state,100);
        Check(state.Status.LitterHidden==0,"large scene restores in bounded ticks");
        SceneCleanupModule.Shutdown();SceneManager.Scenes.Clear();
    }
    private static void DelayedScan(string root)
    {
        var scene=CreateScene();var renderer=AddRenderer(scene,"garbage_bag_LOD");
        var config=new ConfigFile();SceneCleanupModule.Init(config,new ManualLogSource(),Path.Combine(root,"data"));
        config.Bind("Graphics.Advanced","StartDelaySeconds",10f,"").Value=5;
        var world=new object();SceneCleanupModule.State.BeginRaid(world);SceneCleanupModule.SetLevel(SceneCleanupLevel.Max);
        RunTicks(SceneCleanupModule.State);Check(SceneCleanupModule.GetStatus().Candidates==0,"start delay honored");
        Time.realtimeSinceStartup+=5;RunTicks(SceneCleanupModule.State);
        Check(SceneCleanupModule.GetStatus().Candidates==1 && !renderer.gameObject.activeSelf,"scan starts after delay");
        SceneCleanupModule.Shutdown();SceneManager.Scenes.Clear();
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference[] LeakProbe(string root)
    {
        var scene=CreateScene();
        var renderer=AddRenderer(scene,"garbage_bag_LOD");
        var state=Start(root,out var config,out var world);
        SceneCleanupModule.SetLevel(SceneCleanupLevel.Balanced);RunTicks(state);
        Check(state.Status.Candidates==1,"leak probe holds actual candidate first");
        var remote=new GameObject("remote"){scene=scene};remote.Add<EFT.Player>();
        var muzzle=remote.Add<MuzzleLight>();var light=remote.Add<Light>();muzzle.Lights=new[]{light};
        state.OnMuzzle(muzzle);RunTicks(state);
        Check(light.shadows==LightShadows.None,"leak probe tracks modified light first");
        var queued=new GameObject("queued"){scene=scene}.Add<MuzzleLight>();queued.Lights=new Light[0];state.OnMuzzle(queued);
        var shell=new GameObject("shell"){scene=scene}.Add<EFT.AssetsManager.AmmoPoolObject>();state.OnShell(shell,true);
        var weak=new[]{new WeakReference(renderer),new WeakReference(world),new WeakReference(light),new WeakReference(queued),new WeakReference(shell),new WeakReference(renderer.transform)};
        state.EndRaid(world);
        Check(light.shadows==LightShadows.Soft,"end raid restores reusable muzzle light");
        Check(!state.Status.RaidActive && state.Status.Candidates==0,"end raid clears status");
        SceneManager.Scenes.Clear();
        return weak;
    }
}
