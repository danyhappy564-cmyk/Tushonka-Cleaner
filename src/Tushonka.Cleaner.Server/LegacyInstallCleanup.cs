namespace Tushonka.Cleaner.Server;

/// <summary>이동 결과입니다. 경로는 게임 폴더 기준 상대 경로입니다.</summary>
public sealed class LegacyCleanupResult
{
    public string BackupRoot { get; init; } = "";
    public List<string> Moved { get; } = new();
    public List<string> Failed { get; } = new();
}

/// <summary>
/// Tushonka Cleaner로 합쳐진 이전 모드를 지우지 않고 게임 폴더의 백업 폴더로 옮깁니다.
/// 서버가 이미 불러온 DLL은 삭제할 수 없지만 폴더째 옮길 수는 있어서, 서버 시작 때 한 번에 처리합니다.
/// 정확히 일치하는 DLL 이름만 대상이며 다른 모드와 cfg는 건드리지 않습니다.
/// </summary>
public static class LegacyInstallCleanup
{
    public const string BackupFolderName = "Tushonka-Cleaner-OldMods";

    private static readonly HashSet<string> LegacyDlls = new(StringComparer.OrdinalIgnoreCase)
    {
        "RamCleanerInterval.Server.dll",
        "CompoundingPerf.dll",
        "CactusPie.RamCleanerInterval.dll",
        "CompoundingPerf.Client.dll",
        "TYR_DeClutterer.dll",
    };

    private static readonly EnumerationOptions Recursive = new()
    {
        RecurseSubdirectories = true,
        IgnoreInaccessible = true,
    };

    public static LegacyCleanupResult Run(string gameRoot, string modsRoot, string ownDirectory)
    {
        var result = new LegacyCleanupResult { BackupRoot = Path.Combine(gameRoot, BackupFolderName) };
        MoveLegacyFrom(modsRoot, gameRoot, ownDirectory, result);
        MoveLegacyFrom(Path.Combine(gameRoot, "BepInEx", "plugins"), gameRoot, ownDirectory, result);
        return result;
    }

    private static void MoveLegacyFrom(string root, string gameRoot, string ownDirectory, LegacyCleanupResult result)
    {
        if (!Directory.Exists(root)) return;
        var targets = Directory.EnumerateFiles(root, "*.dll", Recursive)
            .Where(path => LegacyDlls.Contains(Path.GetFileName(path)))
            .Select(path => PickTarget(root, path))
            .Where(path => !IsSameOrInside(path, ownDirectory))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var target in targets)
        {
            var relative = Path.GetRelativePath(gameRoot, target);
            try
            {
                var destination = UniquePath(Path.Combine(result.BackupRoot, relative));
                Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                if (Directory.Exists(target)) Directory.Move(target, destination);
                else File.Move(target, destination);
                result.Moved.Add(relative);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                result.Failed.Add(relative);
            }
        }
    }

    /// <summary>모드 폴더를 통째로 옮기되, 다른 DLL이 같이 있는 폴더면 이전 모드의 DLL만 옮깁니다.</summary>
    private static string PickTarget(string root, string dll)
    {
        var relative = Path.GetRelativePath(root, dll);
        var separator = relative.IndexOfAny(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar });
        if (separator < 0) return dll;
        var modFolder = Path.Combine(root, relative[..separator]);
        var sharedFolder = Directory.EnumerateFiles(modFolder, "*.dll", Recursive)
            .Any(path => !LegacyDlls.Contains(Path.GetFileName(path)));
        return sharedFolder ? dll : modFolder;
    }

    private static bool IsSameOrInside(string path, string directory)
    {
        var relative = Path.GetRelativePath(directory, path);
        return relative == "." || (!relative.StartsWith("..", StringComparison.Ordinal) && !Path.IsPathRooted(relative));
    }

    private static string UniquePath(string path)
    {
        var candidate = path;
        for (var n = 2; Directory.Exists(candidate) || File.Exists(candidate); n++) candidate = path + "-" + n;
        return candidate;
    }
}
