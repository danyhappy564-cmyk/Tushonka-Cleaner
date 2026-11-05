using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Tushonka.Cleaner.Server;

/// <summary>시작 시 설치 목록만 확인하고 구형 모드와 겹치는 기능의 실행을 막습니다.</summary>
internal static class LegacyServerGuard
{
    private static bool _restartClears;

    internal static LegacyConflicts Conflicts { get; private set; } = new();
    internal static string Warning => (_restartClears
        ? "Tushonka Cleaner: 이전 모드를 백업 폴더로 옮겼습니다. 서버를 한 번 다시 켜면 겹쳐서 멈춘 기능이 켜집니다. / " +
          "Old mods were moved to the backup folder. Restart the server once to turn the paused features on. "
        : "Tushonka Cleaner: 이전 모드가 함께 설치되어 겹치는 기능을 중지했습니다. 서버와 게임을 끄고 이전 모드를 지우십시오. / " +
          "Old mods are still installed, so overlapping features are off. Close the server and game, then delete the old mods. ") +
        string.Join(", ", Conflicts.Guids);

    internal static LegacyConflicts Scan(string modPath, Action<string> logWarning)
    {
        var identifiers = AppDomain.CurrentDomain.GetAssemblies()
            .Select(a => a.GetName().Name ?? "").ToList();
        var ownDirectory = Path.GetFullPath(modPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var modsRoot = Directory.GetParent(ownDirectory)?.FullName;
        var cleanupFailed = false;
        if (modsRoot != null)
        {
            var gameRoot = FindGameRoot(modsRoot);
            var cleanup = LegacyInstallCleanup.Run(gameRoot, modsRoot, ownDirectory);
            cleanupFailed = cleanup.Failed.Count > 0;
            if (cleanup.Moved.Count > 0)
                logWarning($"Tushonka Cleaner: 이전 모드 {cleanup.Moved.Count}개를 옮겼습니다. / Moved {cleanup.Moved.Count} old mod(s): " +
                           $"{string.Join(", ", cleanup.Moved)} -> {cleanup.BackupRoot}");
            if (cleanupFailed)
                logWarning("Tushonka Cleaner: 이전 모드를 옮기지 못했습니다. 서버와 게임을 끄고 직접 지우십시오. / " +
                           $"Could not move these old mods. Close the server and game, then delete them: {string.Join(", ", cleanup.Failed)}");

            // 아직 로드되지 않은 DLL도 이름으로 찾습니다. 외부 모드의 코드나 설정을 읽지 않습니다.
            identifiers.AddRange(DllNames(modsRoot));
            identifiers.AddRange(DllNames(Path.Combine(gameRoot, "BepInEx", "plugins")));
        }
        Conflicts = LegacyConflicts.Detect(identifiers);
        _restartClears = Conflicts.Any && !cleanupFailed;
        if (Conflicts.Any) logWarning(Warning);
        if (Conflicts.Smoothie) logWarning(LegacyConflicts.SmoothieWarningKo + " / " + LegacyConflicts.SmoothieWarningEn);
        return Conflicts;
    }

    /// <summary>게임 폴더는 SPT_Runtime 위입니다. 서버만 따로 둔 설치에서는 SPT_Runtime 안에 백업합니다.</summary>
    private static string FindGameRoot(string modsRoot)
    {
        var runtimeRoot = Path.GetFullPath(Path.Combine(modsRoot, "..", ".."));
        var gameRoot = Directory.GetParent(runtimeRoot)?.FullName;
        return gameRoot != null && Directory.Exists(Path.Combine(gameRoot, "BepInEx", "plugins")) ? gameRoot : runtimeRoot;
    }

    private static IEnumerable<string> DllNames(string root) => !Directory.Exists(root)
        ? Enumerable.Empty<string>()
        : Directory.EnumerateFiles(root, "*.dll", SearchOption.AllDirectories)
            .Select(Path.GetFileNameWithoutExtension).OfType<string>();

    internal static ContentResult BlockedPage() => new()
    {
        StatusCode = 409,
        ContentType = "text/html; charset=utf-8",
        Content = "<!doctype html><html lang=\"ko\"><meta charset=\"utf-8\"><title>Tushonka Cleaner</title>" +
                  "<h1>Tushonka Cleaner</h1><p role=\"alert\">" + WebUtility.HtmlEncode(Warning) + "</p>" +
                  (!Conflicts.Performance ? "<p><a href=\"/tushonka-cleaner/performance\">서버 최적화 설정</a></p>" : "") + "</html>"
    };
}

public sealed class LegacyMemoryGuardAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (LegacyServerGuard.Conflicts.Memory) context.Result = LegacyServerGuard.BlockedPage();
    }
}

public sealed class LegacyPerformanceGuardAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (LegacyServerGuard.Conflicts.Performance) context.Result = LegacyServerGuard.BlockedPage();
    }
}
