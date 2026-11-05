using Tushonka.Cleaner.Server;
using Xunit;

namespace Tushonka.Cleaner.Tests;

public sealed class LegacyInstallCleanupTests : IDisposable
{
    private readonly string _game = Path.Combine(Path.GetTempPath(), "tushonka-cleanup-" + Guid.NewGuid().ToString("N"));
    private string Mods => Path.Combine(_game, "SPT_Runtime", "user", "mods");
    private string Plugins => Path.Combine(_game, "BepInEx", "plugins");
    private string Own => Path.Combine(Mods, "Tushonka-Cleaner");
    private string Backup => Path.Combine(_game, LegacyInstallCleanup.BackupFolderName);

    public LegacyInstallCleanupTests()
    {
        Directory.CreateDirectory(Own);
        Directory.CreateDirectory(Plugins);
    }

    public void Dispose() => Directory.Delete(_game, true);

    private static string Touch(params string[] parts)
    {
        var path = Path.Combine(parts);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, "x");
        return path;
    }

    private LegacyCleanupResult Run() => LegacyInstallCleanup.Run(_game, Mods, Own);

    [Fact]
    public void Moves_every_old_mod_folder_and_keeps_its_layout()
    {
        Touch(Mods, "RamCleanerInterval.Server", "RamCleanerInterval.Server.dll");
        Touch(Mods, "CompoundingPerf", "CompoundingPerf.dll");
        Touch(Mods, "CompoundingPerf", "config.json");
        Touch(Plugins, "CactusPie.RamCleanerInterval", "CactusPie.RamCleanerInterval.dll");
        Touch(Plugins, "CompoundingPerf.Client", "CompoundingPerf.Client.dll");
        Touch(Plugins, "TYR_DeClutterer", "TYR_DeClutterer.dll");
        Touch(Plugins, "TYR_DeClutterer", "exclude.txt");

        var result = Run();

        Assert.Equal(5, result.Moved.Count);
        Assert.Empty(result.Failed);
        Assert.False(Directory.Exists(Path.Combine(Plugins, "TYR_DeClutterer")));
        Assert.False(Directory.Exists(Path.Combine(Mods, "CompoundingPerf")));
        Assert.True(File.Exists(Path.Combine(Backup, "BepInEx", "plugins", "TYR_DeClutterer", "exclude.txt")));
        Assert.True(File.Exists(Path.Combine(Backup, "SPT_Runtime", "user", "mods", "CompoundingPerf", "config.json")));
    }

    [Fact]
    public void Leaves_other_mods_and_Smoothie_alone()
    {
        var other = Touch(Plugins, "SAIN", "SAIN.dll");
        var smoothie = Touch(Plugins, "Smoothie", "Smoothie.dll");
        var own = Touch(Own, "Tushonka.Cleaner.Server.dll");
        var config = Touch(_game, "BepInEx", "config", "com.TYR.DeClutter.cfg");

        var result = Run();

        Assert.Empty(result.Moved);
        Assert.Empty(result.Failed);
        Assert.True(File.Exists(other) && File.Exists(smoothie) && File.Exists(own) && File.Exists(config));
        Assert.False(Directory.Exists(Backup));
    }

    [Fact]
    public void Moves_only_the_dll_when_the_folder_holds_another_mod()
    {
        var old = Touch(Plugins, "Pack", "TYR_DeClutterer.dll");
        var other = Touch(Plugins, "Pack", "Other.dll");

        var result = Run();

        Assert.Single(result.Moved);
        Assert.False(File.Exists(old));
        Assert.True(File.Exists(other));
    }

    [Fact]
    public void Moves_a_loose_dll_in_the_plugins_root()
    {
        var old = Touch(Plugins, "TYR_DeClutterer.dll");

        Run();

        Assert.False(File.Exists(old));
        Assert.True(File.Exists(Path.Combine(Backup, "BepInEx", "plugins", "TYR_DeClutterer.dll")));
    }

    [Fact]
    public void A_second_install_does_not_overwrite_the_first_backup()
    {
        Touch(Plugins, "TYR_DeClutterer", "TYR_DeClutterer.dll");
        Run();
        Touch(Plugins, "TYR_DeClutterer", "TYR_DeClutterer.dll");

        var result = Run();

        Assert.Single(result.Moved);
        Assert.True(Directory.Exists(Path.Combine(Backup, "BepInEx", "plugins", "TYR_DeClutterer")));
        Assert.True(Directory.Exists(Path.Combine(Backup, "BepInEx", "plugins", "TYR_DeClutterer-2")));
    }

    [Fact]
    public void Reports_a_folder_it_cannot_move_and_keeps_it()
    {
        var dll = Touch(Plugins, "TYR_DeClutterer", "TYR_DeClutterer.dll");

        using (new FileStream(dll, FileMode.Open, FileAccess.Read, FileShare.None))
        {
            var result = Run();
            Assert.Empty(result.Moved);
            Assert.Single(result.Failed);
        }

        Assert.True(File.Exists(dll));
    }

    [Fact]
    public void Works_without_a_client_folder()
    {
        Directory.Delete(Plugins, true);
        Touch(Mods, "CompoundingPerf", "CompoundingPerf.dll");

        Assert.Single(Run().Moved);
    }
}
