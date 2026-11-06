using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
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
        Check(QualitySettings.pixelLightCount==4 && QualitySettings.LodBiasTouches==0,"quality restored");
        QualitySettings.pixelLightCount=6;
        SceneCleanupModule.SetLevel(SceneCleanupLevel.Max);RunTicks(state);
        Check(QualitySettings.pixelLightCount==3,"Max halves the fresh original");
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
        GraphicsTable(root);
        GraphicsEdges(root);
        GraphicsFlags(root);
        MuzzleLevels(root);
        ShellCaps(root);
        ConfigKeys(root);
        SourceAudit(root);
        var weak=LeakProbe(root);
        GC.Collect();GC.WaitForPendingFinalizers();GC.Collect();
        foreach(var reference in weak)Check(!reference.IsAlive,"actual runtime raid reference release");
        SceneCleanupModule.Shutdown();
        ExpectUntouched("whole runtime suite");
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
    private static readonly SceneCleanupLevel[] Levels={SceneCleanupLevel.Off,SceneCleanupLevel.LightTouch,SceneCleanupLevel.Balanced,SceneCleanupLevel.Max};
    private static void Originals(int budget=256,ShadowQuality shadows=ShadowQuality.All,int cascades=4,int lights=5)
    {
        QualitySettings.particleRaycastBudget=budget;QualitySettings.SetGameShadows(shadows);QualitySettings.shadowCascades=cascades;QualitySettings.pixelLightCount=lights;
        QualitySettings.softParticles=true;QualitySettings.softVegetation=true;QualitySettings.realtimeReflectionProbes=true;
    }
    private static void ExpectQuality(string label,int budget,bool soft,ShadowQuality shadows,int cascades,int lights)
    {
        Check(QualitySettings.particleRaycastBudget==budget,label+": particle budget "+QualitySettings.particleRaycastBudget);
        Check(QualitySettings.softParticles==soft && QualitySettings.softVegetation==soft && QualitySettings.realtimeReflectionProbes==soft,label+": soft and reflections");
        Check(QualitySettings.shadows==shadows,label+": shadows "+QualitySettings.shadows);
        Check(QualitySettings.shadowCascades==cascades,label+": cascades "+QualitySettings.shadowCascades);
        Check(QualitySettings.pixelLightCount==lights,label+": pixel lights "+QualitySettings.pixelLightCount);
    }
    private static void ExpectUntouched(string label)
    {
        Check(QualitySettings.LodBiasTouches==0,label+": lodBias never read or written");
        Check(QualitySettings.ShadowResolutionTouches==0 && QualitySettings.ShadowDistanceTouches==0,label+": shadow resolution and distance left to the game");
        Check(QualitySettings.DisableWrites==0,label+": ShadowQuality.Disable never written");
    }
    private static void ExpectRow(SceneCleanupLevel level,string label)
    {
        // 지시서 표의 값을 그대로 적습니다. 원래 값은 256 / All / 4 / 5 입니다.
        switch(level)
        {
            case SceneCleanupLevel.Max: ExpectQuality(label,64,false,ShadowQuality.HardOnly,2,2); break;
            case SceneCleanupLevel.Balanced: ExpectQuality(label,128,false,ShadowQuality.All,4,5); break;
            default: ExpectQuality(label,256,true,ShadowQuality.All,4,5); break;
        }
        ExpectUntouched(label);
    }
    private static void GraphicsTable(string root)
    {
        Originals();
        var state=Start(root,out var config,out var world);
        foreach(var from in Levels) foreach(var to in Levels)
        {
            string label=from+" to "+to;
            SceneCleanupModule.SetLevel(from);RunTicks(state);ExpectRow(from,label+" (from)");
            SceneCleanupModule.SetLevel(to);RunTicks(state);ExpectRow(to,label);
        }
        foreach(var level in Levels)
        {
            SceneCleanupModule.SetLevel(level);RunTicks(state);
            state.EndRaid(world);ExpectRow(SceneCleanupLevel.Off,level+" raid end restores");
            world=new object();state.BeginRaid(world);RunTicks(state);ExpectRow(level,level+" next raid applies");
        }
        // 적용이 나뉘어 도는 도중에 레이드가 끝나거나 단계가 바뀌어도 원래 값이 남습니다.
        SceneCleanupModule.SetLevel(SceneCleanupLevel.Off);RunTicks(state);
        SceneCleanupModule.SetLevel(SceneCleanupLevel.Max);RunTicks(state,3);
        state.EndRaid(world);ExpectRow(SceneCleanupLevel.Off,"raid end during partial apply");
        world=new object();state.BeginRaid(world);RunTicks(state);ExpectRow(SceneCleanupLevel.Max,"raid after partial apply");
        SceneCleanupModule.SetLevel(SceneCleanupLevel.LightTouch);RunTicks(state,2);
        SceneCleanupModule.SetLevel(SceneCleanupLevel.Max);RunTicks(state,2);
        SceneCleanupModule.SetLevel(SceneCleanupLevel.Off);RunTicks(state);ExpectRow(SceneCleanupLevel.Off,"Off after interrupted changes");
        SceneCleanupModule.SetLevel(SceneCleanupLevel.Max);RunTicks(state);
        SceneCleanupModule.Shutdown();ExpectRow(SceneCleanupLevel.Off,"shutdown restores");
        SceneManager.Scenes.Clear();
    }
    private static void GraphicsEdges(string root)
    {
        var cases=new[]{
            new{Budget=6,Shadows=ShadowQuality.HardOnly,Cascades=2,Lights=1,Balanced=4,MaxBudget=4,MaxShadows=ShadowQuality.HardOnly,MaxCascades=2,MaxLights=0},
            new{Budget=17,Shadows=ShadowQuality.All,Cascades=1,Lights=-3,Balanced=8,MaxBudget=4,MaxShadows=ShadowQuality.HardOnly,MaxCascades=1,MaxLights=0},
            new{Budget=4096,Shadows=ShadowQuality.All,Cascades=0,Lights=0,Balanced=2048,MaxBudget=1024,MaxShadows=ShadowQuality.HardOnly,MaxCascades=0,MaxLights=0},
            new{Budget=2,Shadows=ShadowQuality.Disable,Cascades=3,Lights=7,Balanced=4,MaxBudget=4,MaxShadows=ShadowQuality.Disable,MaxCascades=2,MaxLights=3},
        };
        foreach(var c in cases)
        {
            Originals(c.Budget,c.Shadows,c.Cascades,c.Lights);
            var state=Start(root,out var config,out var world);
            string label="original budget "+c.Budget+" shadows "+c.Shadows;
            SceneCleanupModule.SetLevel(SceneCleanupLevel.LightTouch);RunTicks(state);ExpectQuality(label+" LightTouch",c.Budget,true,c.Shadows,c.Cascades,c.Lights);
            SceneCleanupModule.SetLevel(SceneCleanupLevel.Balanced);RunTicks(state);ExpectQuality(label+" Balanced",c.Balanced,false,c.Shadows,c.Cascades,c.Lights);
            SceneCleanupModule.SetLevel(SceneCleanupLevel.Max);RunTicks(state);ExpectQuality(label+" Max",c.MaxBudget,false,c.MaxShadows,c.MaxCascades,c.MaxLights);
            SceneCleanupModule.SetLevel(SceneCleanupLevel.Off);RunTicks(state);ExpectQuality(label+" Off",c.Budget,true,c.Shadows,c.Cascades,c.Lights);
            SceneCleanupModule.SetLevel(SceneCleanupLevel.Max);RunTicks(state);
            state.EndRaid(world);ExpectQuality(label+" raid end",c.Budget,true,c.Shadows,c.Cascades,c.Lights);
            // 게임 값이 Disable이어도 이 모드는 Disable을 쓰지 않습니다.
            ExpectUntouched(label);
            SceneCleanupModule.Shutdown();SceneManager.Scenes.Clear();
        }
        Originals();
    }
    private static void GraphicsFlags(string root)
    {
        Originals();
        var state=Start(root,out var config,out var world);
        ConfigEntry<bool> Flag(string key)=>config.Bind("Graphics.Tuning",key,true,"");
        SceneCleanupModule.SetLevel(SceneCleanupLevel.Max);RunTicks(state);
        Flag("ReduceParticles").Value=false;RunTicks(state);
        Check(QualitySettings.particleRaycastBudget==256 && !QualitySettings.softParticles,"particle flag restores only budget");
        Flag("ReduceParticles").Value=true;RunTicks(state);Check(QualitySettings.particleRaycastBudget==64,"particle flag reapplies");
        Flag("SoftParticles").Value=false;RunTicks(state);
        Check(QualitySettings.softParticles && !QualitySettings.softVegetation,"soft particle flag restores only itself");
        Flag("SoftParticles").Value=true;RunTicks(state);Check(!QualitySettings.softParticles,"soft particle flag reapplies");
        Flag("SoftVegetation").Value=false;RunTicks(state);
        Check(QualitySettings.softVegetation && !QualitySettings.realtimeReflectionProbes,"soft vegetation flag restores only itself");
        Flag("SoftVegetation").Value=true;RunTicks(state);Check(!QualitySettings.softVegetation,"soft vegetation flag reapplies");
        Flag("RealtimeReflections").Value=false;RunTicks(state);
        Check(QualitySettings.realtimeReflectionProbes && !QualitySettings.softVegetation,"reflection flag restores only itself");
        Flag("RealtimeReflections").Value=true;RunTicks(state);Check(!QualitySettings.realtimeReflectionProbes,"reflection flag reapplies");
        Flag("Shadows").Value=false;RunTicks(state);
        Check(QualitySettings.shadows==ShadowQuality.All && QualitySettings.shadowCascades==4 && QualitySettings.pixelLightCount==2,"shadow flag restores shadows and cascades only");
        Flag("Shadows").Value=true;RunTicks(state);Check(QualitySettings.shadows==ShadowQuality.HardOnly && QualitySettings.shadowCascades==2,"shadow flag reapplies");
        Flag("PixelLights").Value=false;RunTicks(state);
        Check(QualitySettings.pixelLightCount==5 && QualitySettings.shadows==ShadowQuality.HardOnly,"light flag restores only itself");
        Flag("PixelLights").Value=true;RunTicks(state);Check(QualitySettings.pixelLightCount==2,"light flag reapplies");
        ExpectRow(SceneCleanupLevel.Max,"after every flag toggle");
        SceneCleanupModule.Shutdown();SceneManager.Scenes.Clear();
    }
    private static void MuzzleLevels(string root)
    {
        var scene=CreateScene();
        var state=Start(root,out var config,out var world);
        var remote=new GameObject("remote"){scene=scene};remote.Add<EFT.Player>();
        var muzzle=remote.Add<MuzzleLight>();var light=remote.Add<Light>();muzzle.Lights=new[]{light};
        SceneCleanupModule.SetLevel(SceneCleanupLevel.LightTouch);RunTicks(state);
        state.OnMuzzle(muzzle);RunTicks(state);Check(light.shadows==LightShadows.Soft,"LightTouch keeps other muzzle shadows");
        SceneCleanupModule.SetLevel(SceneCleanupLevel.Balanced);RunTicks(state);
        state.OnMuzzle(muzzle);RunTicks(state);Check(light.shadows==LightShadows.None,"Balanced removes other muzzle shadows");
        SceneCleanupModule.SetLevel(SceneCleanupLevel.LightTouch);RunTicks(state);Check(light.shadows==LightShadows.Soft,"lower level restores muzzle shadows");
        SceneCleanupModule.SetLevel(SceneCleanupLevel.Max);RunTicks(state);
        state.OnMuzzle(muzzle);RunTicks(state);Check(light.shadows==LightShadows.None,"Max removes other muzzle shadows");
        state.EndRaid(world);Check(light.shadows==LightShadows.Soft,"raid end restores muzzle shadows");
        SceneCleanupModule.Shutdown();SceneManager.Scenes.Clear();
    }
    private static EFT.AssetsManager.AmmoPoolObject[] Fire(RuntimeState state,Scene scene,int count)
    {
        var shells=new EFT.AssetsManager.AmmoPoolObject[count];
        for(int i=0;i<count;i++) { shells[i]=new GameObject("used shell"){scene=scene}.Add<EFT.AssetsManager.AmmoPoolObject>(); state.OnShell(shells[i],true); }
        return shells;
    }
    private static int Flagged(EFT.AssetsManager.AmmoPoolObject[] shells)
    { int n=0; foreach(var s in shells) if(s.ShouldBeDestroyed) ++n; return n; }
    private static void ShellCaps(string root)
    {
        var scene=CreateScene();
        var state=Start(root,out var config,out var world);
        foreach(var (level,cap) in new[]{(SceneCleanupLevel.LightTouch,100),(SceneCleanupLevel.Balanced,60),(SceneCleanupLevel.Max,30)})
        {
            SceneCleanupModule.SetLevel(level);
            var full=Fire(state,scene,cap);
            Check(Flagged(full)==0,level+" keeps exactly "+cap+" shells");
            var extra=Fire(state,scene,2);
            Check(full[0].ShouldBeDestroyed && full[1].ShouldBeDestroyed && !full[2].ShouldBeDestroyed && Flagged(full)==2 && Flagged(extra)==0,level+" retires oldest over "+cap);
            state.EndRaid(world);world=new object();state.BeginRaid(world);
            var fresh=Fire(state,scene,cap);
            Check(Flagged(fresh)==0,level+" raid end empties the count");
            state.EndRaid(world);world=new object();state.BeginRaid(world);
        }
        SceneCleanupModule.SetLevel(SceneCleanupLevel.Off);
        Check(Flagged(Fire(state,scene,300))==0,"Off keeps every shell");
        SceneCleanupModule.SetLevel(SceneCleanupLevel.LightTouch);
        config.Bind("Graphics.Tuning","ShellCap",true,"").Value=false;
        Check(Flagged(Fire(state,scene,300))==0,"ShellCap off keeps every shell");
        config.Bind("Graphics.Tuning","ShellCap",true,"").Value=true;
        // 대상 조건은 그대로입니다.
        var live=new GameObject("live ammo"){scene=scene}.Add<EFT.AssetsManager.AmmoPoolObject>();
        var held=new GameObject("held"){scene=scene}.Add<EFT.AssetsManager.AmmoPoolObject>();new GameObject("magazine"){scene=scene}.transform.Add(held.transform);
        var disabledShell=new GameObject("disabled"){scene=scene}.Add<EFT.AssetsManager.AmmoPoolObject>();disabledShell.enabled=false;
        var inactive=new GameObject("inactive"){scene=scene}.Add<EFT.AssetsManager.AmmoPoolObject>();inactive.gameObject.activeSelf=false;
        state.OnShell(live,false);state.OnShell(held,true);state.OnShell(disabledShell,true);state.OnShell(inactive,true);
        var counted=Fire(state,scene,100);
        Check(Flagged(counted)==0 && !live.ShouldBeDestroyed && !held.ShouldBeDestroyed && !disabledShell.ShouldBeDestroyed && !inactive.ShouldBeDestroyed,"ineligible shells are not counted");
        // 같은 탄피를 다시 알려도 한 개이고, 게임이 돌려받은 탄피는 자리를 비웁니다.
        state.OnShell(counted[0],true);Check(Flagged(counted)==0,"repeat report counts once");
        state.ForgetShell(counted[0]);
        var after=Fire(state,scene,1);Check(Flagged(counted)==0 && Flagged(after)==0,"returned shell frees its place");
        Fire(state,scene,1);Check(counted[1].ShouldBeDestroyed && !counted[2].ShouldBeDestroyed && !counted[0].ShouldBeDestroyed,"oldest live shell retires next");
        state.EndRaid(world);world=new object();state.BeginRaid(world);
        // 단계를 낮추면 상한도 바로 줄고, 높이면 미리 없애지 않습니다.
        SceneCleanupModule.SetLevel(SceneCleanupLevel.LightTouch);
        var many=Fire(state,scene,100);
        SceneCleanupModule.SetLevel(SceneCleanupLevel.Max);
        var one=Fire(state,scene,1);
        Check(Flagged(many)==71 && many[70].ShouldBeDestroyed && !many[71].ShouldBeDestroyed && Flagged(one)==0,"Max leaves 30 after LightTouch");
        SceneCleanupModule.SetLevel(SceneCleanupLevel.LightTouch);
        var more=Fire(state,scene,70);
        Check(Flagged(many)==71 && Flagged(one)==0 && Flagged(more)==0,"LightTouch does not retire below 100");
        Fire(state,scene,1);Check(Flagged(many)==72 && many[71].ShouldBeDestroyed,"LightTouch retires only over 100");
        SceneCleanupModule.Shutdown();SceneManager.Scenes.Clear();
    }
    private static void ConfigKeys(string root)
    {
        var config=new ConfigFile();
        SceneCleanupModule.Init(config,new ManualLogSource(),Path.Combine(root,"data"));
        foreach(var key in new[]{"ReduceParticles","SoftParticles","SoftVegetation","RealtimeReflections","Shadows","PixelLights","ShellCap","OtherMuzzleShadows"})
            Check(config.Has("Graphics.Tuning",key),"setting kept: "+key);
        foreach(var key in new[]{"Lod","ImpactCap","ShellLimit","ImpactLimit"})
            Check(!config.Has("Graphics.Tuning",key),"setting removed: "+key);
        SceneCleanupModule.Shutdown();
    }
    // GameHooks는 게임 DLL이 있어야 컴파일되어 이 시험에 들어오지 않습니다. 패치 부재는 소스로 확인합니다.
    private static void SourceAudit(string root)
    {
        string client=Path.GetFullPath(Path.Combine(root,"..","..","src","Tushonka.Cleaner.Client"));
        var forbidden=new Regex(@"lodBias|\bLod\b|gfxLod|ImpactCap|ImpactLimit|AllowImpact|ImpactDecal|\bimpacts\b|gfxImpact|ShellLimit|gfxShellLimit|DrawDecal|DecalSystem");
        var files=new System.Collections.Generic.List<string>(Directory.GetFiles(Path.Combine(client,"SceneCleanup"),"*.cs",SearchOption.AllDirectories));
        files.Add(Path.Combine(client,"CleanerPlugin.SimpleUi.cs"));
        foreach(var web in new[]{"advanced.js","settings.js","i18n.js"}) files.Add(Path.Combine(client,"Web",web));
        foreach(var file in files)
        {
            var match=forbidden.Match(File.ReadAllText(file));
            Check(!match.Success,Path.GetFileName(file)+" still has "+match.Value);
        }
        string hooks=File.ReadAllText(Path.Combine(client,"SceneCleanup","GameHooks.cs"));
        Check(hooks.Contains("\"RegisterDecal\"") && hooks.Contains("\"SetUsed\"") && hooks.Contains("\"ReturnToPool\"") && hooks.Contains("\"SetIntensity\""),"other game patches kept");
        Check(new Regex(@"harmony\.Patch\(").Matches(hooks).Count==1,"only the shared patch helper calls Harmony");
        string advanced=File.ReadAllText(Path.Combine(client,"Web","advanced.js"));
        string settings=File.ReadAllText(Path.Combine(client,"Web","settings.js"));
        string i18n=File.ReadAllText(Path.Combine(client,"Web","i18n.js"));
        Check(advanced.Contains("sw('gfxShellCap'") && settings.Contains("gfxShellCap: 'Graphics.Tuning|ShellCap'") && i18n.Contains("gfxShellCap:"),"ShellCap toggle kept");
        foreach(Match row in new Regex(@"(?:sw|num)\('(gfx\w+)'").Matches(advanced))
        {
            string key=row.Groups[1].Value;
            Check(settings.Contains(key+":") && i18n.Contains(key+":"),"advanced row wired: "+key);
        }
    }
}
