// 시험용 주 스레드 모형입니다. 게임/Unity 네이티브 코드나 Harmony 실행을 대체 증명하지 않습니다.
using System;
using System.Collections.Generic;

namespace BepInEx.Configuration
{
    public sealed class ConfigDefinition { public string Section; public string Key; }
    public class ConfigEntryBase { public ConfigDefinition Definition; }
    public sealed class SettingChangedEventArgs:EventArgs { public ConfigEntryBase ChangedSetting; }
    public sealed class ConfigDescription { public ConfigDescription(string s,object range) {} }
    public sealed class AcceptableValueRange<T> { public AcceptableValueRange(T a,T b) {} }
    public sealed class ConfigEntry<T>:ConfigEntryBase
    {
        private T value;
        internal ConfigFile Config;
        public T Value { get { return value; } set { if(EqualityComparer<T>.Default.Equals(this.value,value)) return; this.value=value; Config.Change(this); } }
    }
    public sealed class ConfigFile
    {
        public event EventHandler<SettingChangedEventArgs> SettingChanged;
        private readonly Dictionary<string,ConfigEntryBase> entries=new Dictionary<string,ConfigEntryBase>();
        public ConfigEntry<T> Bind<T>(string section,string key,T value,object description)
        {
            string id=section+"/"+key;
            if(entries.TryGetValue(id,out var old)) return (ConfigEntry<T>)old;
            var entry=new ConfigEntry<T>{Config=this,Definition=new ConfigDefinition{Section=section,Key=key}};
            entry.Value=value; entries[id]=entry; return entry;
        }
        public void Change(ConfigEntryBase entry) { SettingChanged?.Invoke(this,new SettingChangedEventArgs{ChangedSetting=entry}); }
    }
}
namespace BepInEx.Logging { public sealed class ManualLogSource { public void LogInfo(object x) {} public void LogWarning(object x) {} } }
namespace UnityEngine
{
    public class Object { public string name; public int GetInstanceID() {return System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(this);} }
    public class Component:Object
    {
        public GameObject gameObject;
        public Transform transform { get { return gameObject.transform; } }
        public static long ComponentReads;
        public void GetComponents(List<Component> result) { ++ComponentReads; result.Clear(); result.AddRange(gameObject.Components); }
        public T GetComponent<T>() where T:Component
        { for(int i=0;i<gameObject.Components.Count;i++) if(gameObject.Components[i] is T c) return c; return null; }
        public T GetComponentInParent<T>() where T:Component
        { for(Transform t=transform;t!=null;t=t.parent) { var c=t.GetComponent<T>(); if(c!=null) return c; } return null; }
    }
    public class Behaviour:Component { public bool enabled=true; public bool isActiveAndEnabled { get { return enabled && gameObject.activeInHierarchy; } } }
    public class MonoBehaviour:Behaviour {}
    public sealed class GameObject:Object
    {
        public readonly List<Component> Components=new List<Component>();
        public readonly Transform transform;
        public bool activeSelf=true;
        public int ActiveWrites;
        public bool activeInHierarchy { get { for(Transform t=transform;t!=null;t=t.parent) if(!t.gameObject.activeSelf) return false; return true; } }
        public void SetActive(bool value) { activeSelf=value; ++ActiveWrites; }
        public UnityEngine.SceneManagement.Scene scene;
        public GameObject(string name) { this.name=name; transform=new Transform{gameObject=this,name=name}; Components.Add(transform); }
        public T Add<T>() where T:Component,new() { var c=new T{gameObject=this,name=name}; Components.Add(c); return c; }
    }
    public class Transform:Component
    {
        public Transform parent;
        public Vector3 position,lossyScale=new Vector3(1,1,1);
        public readonly List<Transform> Children=new List<Transform>();
        public int childCount { get { return Children.Count; } }
        public Transform root { get { Transform t=this; while(t.parent!=null)t=t.parent; return t; } }
        public Transform GetChild(int i) { return Children[i]; }
        public void Add(Transform child) { Children.Add(child); child.parent=this; child.gameObject.scene=gameObject.scene; }
    }
    public class MeshFilter:Component {}
    public class MeshRenderer:Behaviour { public Bounds bounds=new Bounds(new Vector3(200,0,0),new Vector3(1,1,1)); }
    public class LODGroup:Component {}
    public class Collider:Component {}
    public class Rigidbody:Component {}
    public class Camera:Behaviour { public float fieldOfView=60; }
    public class Light:Behaviour { public LightShadows shadows=LightShadows.Soft; public float intensity=1; }
    public enum LightShadows { None,Hard,Soft }
    public enum ShadowQuality { Disable,HardOnly,All }
    public enum ShadowResolution { Low,Medium,High,VeryHigh }
    public struct Vector3
    {
        public float x,y,z;
        public Vector3(float x,float y,float z) {this.x=x;this.y=y;this.z=z;}
        public float sqrMagnitude {get{return x*x+y*y+z*z;}}
        public static Vector3 operator -(Vector3 a,Vector3 b) { return new Vector3(a.x-b.x,a.y-b.y,a.z-b.z); }
    }
    public struct Bounds
    {
        public Vector3 size,center;
        public Vector3 extents { get { return new Vector3(size.x*.5f,size.y*.5f,size.z*.5f); } }
        public Bounds(Vector3 center,Vector3 size) {this.center=center;this.size=size;}
        public Vector3 ClosestPoint(Vector3 point) { return center; }
    }
    public static class Mathf
    {
        public static float Max(float a,float b) {return Math.Max(a,b);}
        public static float Max(float a,float b,float c) {return Math.Max(a,Math.Max(b,c));}
        public static float Min(float a,float b) {return Math.Min(a,b);}
        public static int Min(int a,int b) {return Math.Min(a,b);}
        public static float Abs(float x) {return Math.Abs(x);}
    }
    public static class Time { public static float realtimeSinceStartup; public static int frameCount; }
    public static class QualitySettings
    {
        public static int particleRaycastBudget=256,pixelLightCount=4,shadowCascades=4;
        public static bool softParticles=true,softVegetation=true,realtimeReflectionProbes=true;
        public static ShadowQuality shadows=ShadowQuality.All;
        public static ShadowResolution shadowResolution=ShadowResolution.High;
        public static float lodBias=2,shadowDistance=150;
    }
}
namespace UnityEngine.SceneManagement
{
    public sealed class SceneData { public string Name; public readonly List<UnityEngine.GameObject> Roots=new List<UnityEngine.GameObject>(); }
    public struct Scene
    {
        public SceneData Data;
        public string name { get { return Data?.Name; } }
        public bool isLoaded {get{return Data!=null;}}
        public int buildIndex {get{return -1;}}
        public int handle {get{return Data==null?0:System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(Data);}}
        public int rootCount {get{return Data.Roots.Count;}}
        public bool IsValid() {return Data!=null;}
        public void GetRootGameObjects(List<UnityEngine.GameObject> roots) {roots.Clear();roots.AddRange(Data.Roots);}
    }
    public static class SceneManager
    {
        public static readonly List<Scene> Scenes=new List<Scene>();
        public static event Action<Scene> sceneUnloaded;
        public static int sceneCount {get{return Scenes.Count;}}
        public static Scene GetSceneAt(int i) {return Scenes[i];}
        public static void Unload(Scene scene) {Scenes.Remove(scene);sceneUnloaded?.Invoke(scene);}
    }
}
public class StaticDeferredDecal:UnityEngine.MonoBehaviour {}
public class GuidComponent:UnityEngine.MonoBehaviour {}
public class LocationScene:UnityEngine.MonoBehaviour {}
public class UnreviewedComponent:UnityEngine.MonoBehaviour {}
public class HideoutGameWorld {}
namespace Koenigz.PerfectCulling.EFT
{
    public class MultisceneSharedOccluder:UnityEngine.MonoBehaviour {}
    public class PerfectCullingCrossSceneGroup:UnityEngine.MonoBehaviour {}
    public class PerfectCullingCrossSceneContentDoors:UnityEngine.MonoBehaviour {}
    public class PerfectCullingCrossSceneContentMeshes:UnityEngine.MonoBehaviour {}
}
namespace EFT.Interactive
{
    public class Door:UnityEngine.MonoBehaviour {}
    public class LootableContainer:UnityEngine.MonoBehaviour {}
}
public class MuzzleLight:UnityEngine.MonoBehaviour {public UnityEngine.Light[] Lights;}
namespace EFT {public class Player:UnityEngine.MonoBehaviour {public bool IsYourPlayer;}}
namespace EFT.AssetsManager {public class AmmoPoolObject:UnityEngine.MonoBehaviour {public bool ShouldBeDestroyed;}}
namespace EFT.CameraControl
{
    public sealed class OpticCameraManager {public UnityEngine.Camera Camera;public bool IsAnyOpticCameraRendering;}
    public sealed class CameraManager {public static CameraManager instance;public UnityEngine.Camera Camera;public OpticCameraManager OpticCameraManager=new OpticCameraManager();}
}
namespace Tushonka.Cleaner.SceneCleanup { internal sealed class GameHooks {internal void Install() {} internal void Remove() {}} }
