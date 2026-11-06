using System.Net.Sockets;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Tushonka.Cleaner.Server;

/// <summary>
/// The RAM cleaner's launcher mod page, <c>/tushonka-cleaner/</c> on the SPT server.
/// <list type="bullet">
///   <item>Game running: forwards to the page the plugin serves inside the game (<c>http://127.0.0.1:&lt;port&gt;/...</c>) —
///     live numbers, session report with the raid in progress, settings applied at once. Its links are relative, so
///     they work unchanged under this prefix.</item>
///   <item>Game closed (like the other mod pages, which only need the server): this server's own page — every
///     setting, edited straight in the plugin's .cfg (used at the next game start), and the past session reports.
///     It switches to the live page by itself once the game answers.</item>
/// </list>
/// Same PC only (it reads BepInEx next to SPT_Runtime). Viewing follows the SPT web panel's login rules; changes
/// (POST) need an administrator, like SPT's own config editor, plus the page's own header.
/// </summary>
[Route("tushonka-cleaner")]
[LegacyMemoryGuard]
[Authorize]
[IgnoreAntiforgeryToken]
public sealed class PageProxyController : ControllerBase
{
    private const string ChangeHeader = "X-RamCleaner";

    // The game answers within its own 5 s main-thread timeout; keep a little more than that.
    private static readonly HttpClient Client = new(new SocketsHttpHandler
    {
        UseProxy = false,
        AllowAutoRedirect = false,
        ConnectTimeout = TimeSpan.FromSeconds(2),
    })
    {
        Timeout = TimeSpan.FromSeconds(7),
    };

    [HttpGet("")]
    public Task<IActionResult> Root() => Request.Path.Value?.EndsWith('/') == true
        ? Forward(string.Empty)
        : Task.FromResult<IActionResult>(Redirect("/tushonka-cleaner/")); // relative links need the trailing slash

    [HttpGet("{**path}")]
    public Task<IActionResult> Get(string path) => Forward(path);

    [HttpPost("{**path}")]
    [Authorize(Policy = "Administrator")]
    public Task<IActionResult> Post(string path) => Forward(path);

    // ---------------------------------------------------------------- game closed: this server's own page

    /// <summary>게임이 꺼진 화면이 게임이 켜졌는지 확인하는 주소.</summary>
    [HttpGet("offline/api/online")]
    public async Task<IActionResult> OfflineOnline() => Json(new { online = (await Probe(PluginFiles.Read())).online });

    /// <summary>Past session reports (BepInEx\RamCleaner\RamCleaner-report-*.html) — readable without the game.</summary>
    [HttpGet("reports/{name}")]
    public IActionResult Report(string name)
    {
        var dir = PluginFiles.Read().DataDir;
        if (dir is null || Path.GetFileName(name) != name || !name.StartsWith("RamCleaner-report-", StringComparison.Ordinal) ||
            !name.EndsWith(".html", StringComparison.Ordinal) || !System.IO.File.Exists(Path.Combine(dir, name)))
        {
            return NotFound();
        }

        Response.Headers.CacheControl = "no-store";
        return PhysicalFile(Path.Combine(dir, name), "text/html; charset=utf-8");
    }

    // ---------------------------------------------------------------- game running: forward

    private async Task<IActionResult> Forward(string path)
    {
        Response.Headers.CacheControl = "no-store";
        var files = PluginFiles.Read();
        bool api = path.StartsWith("api/", StringComparison.Ordinal);
        if (!files.Enabled)
        {
            return await Offline(path, files, null);
        }

        using var request = new HttpRequestMessage(new HttpMethod(Request.Method), $"http://127.0.0.1:{files.Port}/{path}{Request.QueryString}");
        if (HttpMethods.IsPost(Request.Method))
        {
            if (Request.Headers[ChangeHeader] != "1")
            {
                return StatusCode(403, "Changes are only accepted from the RAM cleaner page.");
            }

            request.Headers.Add(ChangeHeader, "1");
            if (Request.HasFormContentType)
            {
                // MVC's form value provider has already read the body while binding {path}: re-encode the parsed form.
                var form = await Request.ReadFormAsync(HttpContext.RequestAborted);
                request.Content = new FormUrlEncodedContent(form.SelectMany(field => field.Value.Select(value =>
                    new KeyValuePair<string, string>(field.Key, value ?? string.Empty))));
            }
            else
            {
                using var body = new MemoryStream();
                await Request.Body.CopyToAsync(body, HttpContext.RequestAborted);
                request.Content = new ByteArrayContent(body.ToArray());
                request.Content.Headers.TryAddWithoutValidation("Content-Type", Request.ContentType ?? "application/octet-stream");
            }
        }

        try
        {
            using var reply = await Client.SendAsync(request, HttpContext.RequestAborted);
            var bytes = await reply.Content.ReadAsByteArrayAsync(HttpContext.RequestAborted);
            Response.StatusCode = (int)reply.StatusCode;
            Response.ContentType = reply.Content.Headers.ContentType?.ToString() ?? "text/plain; charset=utf-8";
            await Response.Body.WriteAsync(bytes, HttpContext.RequestAborted);
            return new EmptyResult();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return await Offline(path, files, Reason(ex, files));
        }
    }

    /// <summary>
    /// 게임이 응답하지 않을 때. 같은 화면 파일을 서버가 내보내고, 그 화면이 부르는 주소에 게임 대신 답합니다.
    /// 게임이 로딩 중이면 게임이 곧 설정 파일을 덮어쓰므로 바꾸지 않습니다.
    /// </summary>
    private async Task<IActionResult> Offline(string path, PluginFiles files, string? reason)
    {
        var site = new OfflineSite(files);
        bool post = HttpMethods.IsPost(Request.Method);
        bool busy = System.Diagnostics.Process.GetProcessesByName("EscapeFromTarkov").Length > 0;
        if (path.StartsWith("api/", StringComparison.Ordinal))
        {
            ContentResult Reply(string json, int status = 200) => new() { StatusCode = status, ContentType = "application/json; charset=utf-8", Content = json };
            string Message(string ko, string en) => JsonSerializer.Serialize(new { error = files.English ? en : ko });
            if (post && busy)
            {
                return Reply(Message("게임이 로딩 중입니다. 잠시 뒤 다시 해 주세요.", "The game is loading. Please try again in a moment."), 503);
            }

            var form = post ? await Request.ReadFormAsync(HttpContext.RequestAborted) : null;
            try
            {
                switch (path)
                {
                    case "api/settings":
                        return Reply(post ? site.Set(form!["key"].ToString(), form["value"].ToString()) : site.SettingsJson());
                    case "api/ui" when post:
                        return Reply(site.ApplyUi(form!["key"].ToString(), form["value"].ToString()));
                    case "api/action" when post:
                        return Reply(Message("게임이 켜져 있을 때만 할 수 있습니다.", "This needs the game to be running."), 503);
                    default:
                        return Reply(JsonSerializer.Serialize(new { error = reason ?? "the game is not running" }), 503);
                }
            }
            catch (Exception ex) when (ex is FormatException or ArgumentException or InvalidOperationException or IOException or UnauthorizedAccessException)
            {
                return Reply(JsonSerializer.Serialize(new { error = ex.Message }), 503);
            }
        }

        if (path is "settings" or "server")
        {
            return Redirect("./");
        }

        if (path == "favicon.ico")
        {
            return StatusCode(204);
        }

        var html = site.Page(path);
        return html is null ? NotFound() : new ContentResult { StatusCode = 200, ContentType = "text/html; charset=utf-8", Content = html };
    }

    private static async Task<(bool online, string reason)> Probe(PluginFiles files)
    {
        if (!files.Enabled)
        {
            return (false, files.English ? "the game's web page is turned off (F12 → RAM Cleaner → 18)" : "게임 쪽 웹 페이지가 꺼져 있음(F12 → RAM 클리너 → 18번)");
        }

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            using var reply = await Client.GetAsync($"http://127.0.0.1:{files.Port}/favicon.ico", cts.Token);
            return (true, string.Empty);
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException)
        {
            return (false, Reason(ex, files));
        }
    }

    /// <summary>Refused = nothing listens (game closed, or its page failed to start); timeout = the game is busy.</summary>
    private static string Reason(Exception ex, PluginFiles files) => ex is HttpRequestException
        ? files.English ? $"no game page on 127.0.0.1:{files.Port} (the game is closed, or its page failed to start)" : $"127.0.0.1:{files.Port}에 게임 쪽 페이지 없음 (게임이 꺼져 있거나 게임 쪽 페이지가 시작되지 못함)"
        : files.English ? $"127.0.0.1:{files.Port} did not answer in time (the game may be loading a raid)" : $"127.0.0.1:{files.Port}이(가) 제때 응답하지 않음 (레이드 로딩 중일 수 있음)";

    private ContentResult Json(object value)
    {
        Response.Headers.CacheControl = "no-store";
        return Content(JsonSerializer.Serialize(value), "application/json; charset=utf-8");
    }
}
