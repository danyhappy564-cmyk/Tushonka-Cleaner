using System;
using System.IO;
using System.Runtime.CompilerServices;
using Tushonka.Cleaner.SceneCleanup;

internal static class Program
{
    private static int checks;
    private static void Check(bool ok, string label)
    {
        checks++;
        if (!ok) throw new Exception(label);
    }
    private sealed class Fake { public bool Visible; }
    private static bool Read(Fake x) { return x.Visible; }
    private static void Write(Fake x, bool v) { x.Visible = v; }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference LeakProbe(ReferenceLedger<Fake, bool> ledger)
    {
        var x = new Fake { Visible = true };
        ledger.Add(x, true, 0);
        var weak = new WeakReference(x);
        ledger.Clear();
        return weak;
    }
    public static int Main(string[] args)
    {
        try { Run(args); return 0; }
        catch(Exception error) { Console.Error.WriteLine("FAIL "+error.Message); return 1; }
    }
    private static void Run(string[] args)
    {
        string root = args.Length > 0 ? args[0] : Directory.GetCurrentDirectory();
        var rules = RuleBook.Load(File.ReadAllText(Path.Combine(root, "data/scene-rules.json")));
        Check(rules.Classify("garbage_bag_LOD", "MeshRenderer", .8f, false) == CleanupCategory.Litter, "litter");
        Check(rules.Classify("garbage_bag_LOD", "MeshRenderer", 8f, false) == CleanupCategory.None, "size bound");
        Check(rules.Classify("Door_Glass_LOD", "MeshRenderer", .8f, false) == CleanupCategory.None, "door");
        Check(rules.Classify("item_food_mayo_LOD", "MeshRenderer", .2f, false) == CleanupCategory.None, "loot");
        Check(rules.Classify("Food_barrel_closed_LOD", "MeshRenderer", 1f, false) == CleanupCategory.None, "barrel");
        Check(rules.Classify("Debris", "ParticleSystemRenderer", 0f, false) == CleanupCategory.None, "effect");
        Check(rules.Classify("garbage_bag_LOD", "MeshRenderer", .8f, true) == CleanupCategory.None, "unsafe hierarchy");
        Check(rules.Classify("garbage_bag_LOD(Clone)", "MeshRenderer", .8f, false) == CleanupCategory.Litter, "clone name");
        Check(rules.Classify("garbage_bag_LOD", "MeshRenderer", float.NaN, false) == CleanupCategory.None, "NaN");
        Check(rules.Classify("garbage_bag_LOD", "MeshRenderer", -1f, false) == CleanupCategory.None, "negative size");
        Check(rules.MatchesName("garbage_bag_LOD2(Clone)") && !rules.MatchesName("Door_Glass_LOD"), "name prefilter");
        Check(rules.DecalSizeLimit == 6f, "decal size limit from rules");
        Check(ContainerRules.Kind("Koenigz.PerfectCulling.EFT.MultisceneSharedOccluder") == ContainerKind.Inert, "occluder marker inert");
        Check(ContainerRules.Kind("Koenigz.PerfectCulling.EFT.PerfectCullingCrossSceneGroup") == ContainerKind.GroupRoot, "group root");
        Check(ContainerRules.Kind("GuidComponent") == ContainerKind.Inert && ContainerRules.Kind("LocationScene") == ContainerKind.Inert, "scene markers inert");
        Check(ContainerRules.Kind("EFT.Interactive.Door") == ContainerKind.None && ContainerRules.Kind("EFT.Interactive.LootableContainer") == ContainerKind.None, "interactive protected");
        Check(ContainerRules.Kind("UnityEngine.BoxCollider") == ContainerKind.None && ContainerRules.Kind("SomethingNew") == ContainerKind.None && ContainerRules.Kind(null) == ContainerKind.None, "unknown protected");
        Check(DistancePolicy.SquaredToBox(10,0,0, 0,0,0, 2,2,2) == 64f, "distance to box face");
        Check(DistancePolicy.SquaredToBox(1,1,1, 0,0,0, 2,2,2) == 0f, "inside box");
        var excludes = new ExclusionSet(new [] { "# comment", "  garbage_*  ", "door?" });
        Check(excludes.Matches("GARBAGE_BAG_LOD"), "wildcard exclude");
        Check(!excludes.Matches("pizza_LOD"), "exclude isolation");
        Check(new ExclusionSet(new[]{"garbage_bag_LOD"}).Matches("garbage_bag_LOD2(Clone)"), "normalized exclusion");
        Check(Glob.Match("a*b?", "aXXbc"), "glob");
        Check(!Glob.Match("a?", "a"), "glob anchor");
        Check(Math.Abs(DistancePolicy.Range(80f, 60f, 60f) - 80f) < .001f, "base fov");
        float zoom = DistancePolicy.Range(80f, 60f, 11f);
        Check(zoom > 450 && zoom < 500, "six power range");
        Check(DistancePolicy.Range(80, 60, float.NaN) == 960, "invalid fov protects");
        Check(!DistancePolicy.Hidden(100, 80, true, false), "hysteresis restore");
        Check(DistancePolicy.Hidden(10000, 80, false, true), "far hidden");
        var ledger = new ReferenceLedger<Fake, bool>(8);
        var enabled = new Fake { Visible = true };
        var disabled = new Fake { Visible = false };
        ledger.Add(enabled, true, 0); ledger.Add(disabled, false, 1);
        Write(enabled, false); Write(disabled, false);
        for (int i=0; i<ledger.Count; ++i) Write(ledger.Target(i), ledger.Original(i));
        Check(enabled.Visible && !disabled.Visible, "exact restoration");
        ledger.Clear();
        Check(ledger.Count == 0, "reset count");
        var weak = LeakProbe(ledger);
        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
        Check(!weak.IsAlive, "raid references collect after clear");
        for(int i=0;i<8;i++) Check(ledger.Add(new Fake(), true, 0), "capacity available");
        Check(!ledger.Add(new Fake(), true, 0), "capacity bounded");
        ledger.Clear();
        Check(ledger.DebugOccupiedSlots == 0, "entire backing store cleared");
        var budget=new WorkBudget();
        Check(budget.Open(1,100)==100,"new frame budget");
        budget.Spend(70);Check(budget.Open(1,100)==30,"hooks share frame budget");
        budget.Spend(50);Check(budget.Open(1,100)==0,"overrun stops more work");
        Check(budget.Open(2,100)==100,"next frame budget resets");
        // JIT와 문화권 표의 최초 초기화 비용은 반복 경로 측정 전에 준비합니다.
        for(int i=0;i<10000;i++) { Glob.Match("garbage_*", "garbage_bag_LOD"); DistancePolicy.Range(80,60,11); }
        long before = GC.GetAllocatedBytesForCurrentThread();
        for(int i=0;i<10000;i++) { Glob.Match("garbage_*", "garbage_bag_LOD"); DistancePolicy.Range(80,60,11); }
        Check(GC.GetAllocatedBytesForCurrentThread() == before, "hot core no allocations");
        Console.WriteLine("PASS " + checks + " assertions");
        RuntimeChecks.Run(root);
        CsvChecks.Run(root,rules);
    }
}
