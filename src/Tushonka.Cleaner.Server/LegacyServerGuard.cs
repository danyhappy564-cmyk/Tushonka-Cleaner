using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Tushonka.Cleaner.Server;

/// <summary>시작 시 설치 목록만 확인하고 구형 모드와 겹치는 기능의 실행을 막습니다.</summary>
internal static class LegacyServerGuard
{
    internal static LegacyConflicts Conflicts { get; private set; } = new();
    internal static string Warning =>
        "Tushonka Cleaner: 구형 모드가 함께 설치되어 겹치는 기능을 중지했습니다. 구형 모드를 삭제하십시오. " +
        string.Join(", ", Conflicts.Guids);

    internal static LegacyConflicts Scan(string modPath, Action<string> logWarning)
    {
        var identifiers = AppDomain.CurrentDomain.GetAssemblies()
            .Select(a => a.GetName().Name ?? "").ToList();
        var ownDirectory = Path.GetFullPath(modPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var modsRoot = Directory.GetParent(ownDirectory)?.FullName;
        if (modsRoot != null)
        {
            // 아직 로드되지 않은 DLL도 이름으로 찾습니다. 외부 모드의 코드나 설정을 읽지 않습니다.
            identifiers.AddRange(Directory.EnumerateFiles(modsRoot, "*.dll", SearchOption.AllDirectories)
                .Select(Path.GetFileNameWithoutExtension).OfType<string>());
        }
        Conflicts = LegacyConflicts.Detect(identifiers);
        if (Conflicts.Any) logWarning(Warning);
        return Conflicts;
    }

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
