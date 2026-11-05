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
        Check(!normal.enabled,"safe renderer hidden");
        Check(collision.enabled && door.enabled && playerObject.enabled && sibling.enabled,"protected renderers unchanged");
        Check(decals.enabled,"no decal toggle in raid");
        Check(!disabled.enabled,"initially disabled unchanged");
        Check(state.Status.Candidates==1,"scan candidates");
        Check(state.Status.LitterHidden==1,"hidden count");
        SceneCleanupModule.SetLevel(SceneCleanupLevel.Off);RunTicks(state);
        Check(normal.enabled && !disabled.enabled,"Off restores exact state");
        Check(QualitySettings.pixelLightCount==4 && QualitySettings.lodBias==2,"quality restored");
        QualitySettings.pixelLightCount=6;
        SceneCleanupModule.SetLevel(SceneCleanupLevel.Max);RunTicks(state);
        SceneCleanupModule.SetLevel(SceneCleanupLevel.Off);RunTicks(state);
        Check(QualitySettings.pixelLightCount==6,"reenable captures fresh original");
        var cameraObject=new GameObject("FPS Camera"){scene=scene};
        var camera=cameraObject.Add<Camera>();
        EFT.CameraControl.CameraManager.instance=new EFT.CameraControl.CameraManager{Camera=camera};
        SceneCleanupModule.SetLevel(SceneCleanupLevel.LightTouch);RunTicks(state);
        Check(!normal.enabled,"far hide");
        camera.fieldOfView=11;RunTicks(state);
        Check(normal.enabled,"scope current FOV restores far object");
        camera.fieldOfView=60;RunTicks(state);Check(!normal.enabled,"wider FOV hides again");
        var opticObject=new GameObject("optic"){scene=scene};var optic=opticObject.Add<Camera>();optic.fieldOfView=11;
        EFT.CameraControl.CameraManager.instance.OpticCameraManager.Camera=optic;
        EFT.CameraControl.CameraManager.instance.OpticCameraManager.IsAnyOpticCameraRendering=true;
        RunTicks(state);Check(normal.enabled,"independent optic FOV protects target");
        EFT.CameraControl.CameraManager.instance.OpticCameraManager.IsAnyOpticCameraRendering=false;
        config.Bind("Graphics.Categories","Litter",true,"").Value=false;RunTicks(state);
        Check(normal.enabled,"category disable restores");
        config.Bind("Graphics.Categories","Litter",true,"").Value=true;RunTicks(state);
        Check(!normal.enabled,"category reenable applies");
        normal.gameObject.scene=new Scene{Data=new SceneData{Name="DontDestroyOnLoad"}};
        RunTicks(state);Check(normal.enabled,"scene migration restores persistent object");
        normal.gameObject.scene=scene;RunTicks(state);
        normal.gameObject.Add<Collider>();RunTicks(state);Check(normal.enabled,"new collider restores renderer");
        SceneCleanupModule.SetLevel(SceneCleanupLevel.Off);RunTicks(state);
        long before=GC.GetAllocatedBytesForCurrentThread();
        RunTicks(state,200);
        Check(GC.GetAllocatedBytesForCurrentThread()==before,"steady Tick no managed allocation in harness");
        MuzzleAndLimits(state,config,scene);
        SceneCleanupModule.Shutdown(); SceneManager.Scenes.Clear(); EFT.CameraControl.CameraManager.instance=null;
        DecalPolicy(root);
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
        Check(SceneCleanupModule.GetStatus().Candidates==1 && !renderer.enabled,"scan starts after delay");
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
