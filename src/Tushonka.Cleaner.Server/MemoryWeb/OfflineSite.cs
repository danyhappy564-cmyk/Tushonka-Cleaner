using System.Globalization;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Tushonka.Cleaner.Server.Diagnostics;
using Tushonka.Cleaner.Server.Web;

namespace Tushonka.Cleaner.Server;

/// <summary>
/// 게임이 꺼져 있을 때의 SIC 페이지. 게임 쪽 웹 서버와 같은 HTML, CSS, JS 파일을 그대로 내보내고,
/// 그 파일들이 부르는 주소(api/settings, api/ui)만 게임 대신 응답합니다. 화면은 게임이 켜져 있을 때와 같습니다.
/// 게임 설정은 .cfg에 쓰고 게임을 켤 때 적용되며, 서버 설정은 서버에 바로 적용됩니다.
/// </summary>
internal sealed class OfflineSite
{
    private const string Auto = "자동 · Auto", Korean = "한국어", English = "English";

    private static readonly string[] LevelValues = ["Fastest", "Optimal", "SmallestSize", "NoCompression"];

    private static readonly Dictionary<string, string> MemoryModes = new()
    {
        ["game"] = "자동 정리 · Auto cleanup",
        ["check"] = "간단 확인 · Quick view",
        ["analysis"] = "집중 분석 · Deep analysis",
        ["leak"] = "누수 추적 · Leak hunt",
        ["off"] = "꺼짐 · Off",
    };

    private readonly PluginFiles _files;
    private readonly bool _en;

    public OfflineSite(PluginFiles files)
    {
        _files = files;
        var pref = files.Values.GetValueOrDefault("0. Mode|Language");
        _en = LanguagePolicy.Resolve(pref is Korean or English ? pref : null, null, CultureInfo.InstalledUICulture.Name) == "en";
    }

    private string L(string ko, string en) => _en ? en : ko;

    private static string H(string text) => WebUtility.HtmlEncode(text);

    private static string Asset(string name)
    {
        using var stream = typeof(OfflineSite).Assembly.GetManifestResourceStream("web." + name)
                           ?? throw new InvalidOperationException(name + " is not embedded");
        return new StreamReader(stream, Encoding.UTF8).ReadToEnd();
    }

    // ---------------------------------------------------------------- pages

    /// <summary>주소에 맞는 화면. 모르는 주소면 null입니다.</summary>
    public string? Page(string path) => path switch
    {
        "" or "index.html" => Shell("settings", Asset("index.html"), null, "settings.js"),
        "live" => Shell("live", Asset("live.html"), Strings(
            ("offline", L("게임이 꺼져 있습니다. 게임을 켜면 여기에 실시간 값이 나옵니다.", "The game is closed. Live numbers appear here once it starts.")),
            ("collecting", L("기록을 모으는 중입니다. 2초마다 한 점씩 쌓입니다.", "Collecting data, one point every 2 seconds."))), "dashboard.js"),
        "diagnostics" => Shell("diagnostics", Asset("diagnostics.html"), null, "settings.js"),
        "report" => Report(),
        "history" => History(),
        _ => null,
    };

    private string Nav(string active)
    {
        string Link(string href, string id, string ko, string en) => "<a href=\"" + href + "\"" +
            (active == id ? " aria-current=\"page\"" : "") + ">" + L(ko, en) + "</a>";
        return "<header id=\"site-header\"><div class=\"header-inner\"><a class=\"brand\" href=\"./\"><span class=\"brand-mark\">T</span>Tushonka Cleaner</a><nav class=\"screen-nav\">" +
            Link("./", "settings", "설정", "Settings") + Link("live", "live", "실시간 분석", "Live analysis") +
            Link("report", "report", "레이드 보고서", "Raid report") + Link("history", "history", "지난 기록", "History") +
            "</nav><label class=\"language-picker\">" + L("언어", "Language") +
            "<select data-language aria-label=\"Language\"><option value=\"auto\">" + L("자동", "Auto") +
            "</option><option value=\"ko\">한국어</option><option value=\"en\">English</option></select></label>" +
            "<label class=\"language-picker\">" + L("화면", "Theme") + "<select data-theme-select aria-label=\"" + L("화면", "Theme") + "\"><option value=\"auto\">" + L("자동", "Auto") +
            "</option><option value=\"light\">" + L("밝게", "Light") + "</option><option value=\"dark\">" + L("어둡게", "Dark") + "</option></select></label></div></header>";
    }

    private string BootScript() => "<script>window.TC_BOOT=" + JsonSerializer.Serialize(State()).Replace("<", "\\u003c") + ";</script>";

    /// <summary>게임이 켜지면 같은 주소가 게임 쪽 화면으로 바뀌므로, 그때 새로 읽습니다.</summary>
    private const string WatchGame = "<script>setInterval(function(){if(document.hidden)return;fetch('offline/api/online',{cache:'no-store'}).then(function(r){return r.json();}).then(function(s){if(s.online)location.reload();}).catch(function(){});},5000);</script>";

    private string Shell(string active, string content, string? strings, string? script)
    {
        var sb = new StringBuilder(64 * 1024);
        sb.Append("<!doctype html><html lang=\"").Append(L("ko", "en")).Append("\"><head><meta charset=\"utf-8\">")
          .Append("<meta name=\"viewport\" content=\"width=device-width,initial-scale=1\"><title>Tushonka Cleaner</title>")
          .Append(ReportAssets.ThemeBoot).Append("<style>").Append(ReportAssets.Css).Append('\n').Append(Asset("web.css")).Append("</style></head><body data-page=\"").Append(active).Append("\">")
          .Append(Nav(active)).Append("<main class=\"shell viz-root\">").Append(content).Append("<div id=\"tip\" role=\"tooltip\"></div></main><footer class=\"shell\">Tushonka Cleaner</footer>")
          .Append(BootScript()).Append("<script>").Append(ReportAssets.Js).Append("</script>");
        if (strings != null) sb.Append("<script>window.RC=").Append(strings).Append(";</script>");
        foreach (var file in new[] { "measured.js", "i18n.js", "common.js" }) sb.Append("<script>").Append(Asset(file)).Append("</script>");
        if (active == "settings") sb.Append("<script>").Append(Asset("advanced.js")).Append("</script>");
        if (script != null) sb.Append("<script>").Append(Asset(script)).Append("</script>");
        return sb.Append(WatchGame).Append("</body></html>").ToString();
    }

    private static string Strings(params (string Key, string Value)[] pairs) =>
        JsonSerializer.Serialize(pairs.ToDictionary(p => p.Key, p => p.Value));

    /// <summary>가장 최근에 저장된 보고서. 게임이 켜져 있을 때의 보고서 화면과 같은 틀에 넣습니다.</summary>
    private string Report()
    {
        var latest = _files.Reports(1).FirstOrDefault();
        if (latest is null)
        {
            return Shell("report", "<h1>" + H(L("레이드 보고서", "Raid report")) + "</h1><section class=\"card\"><p>" +
                H(L("아직 저장된 보고서가 없습니다. 레이드 한 판이 끝나면 생깁니다.", "No saved reports yet. One appears once a raid has finished.")) + "</p></section>", null, null);
        }

        const string marker = "<main class=\"viz-root\">";
        var html = File.ReadAllText(latest.FullName);
        var at = html.IndexOf(marker, StringComparison.Ordinal);
        return at < 0
            ? html
            : html.Insert(at + marker.Length, "<style>" + Asset("web.css") + "</style>" + Nav("report") + BootScript() + "<script>" + Asset("common.js") + "</script>" + WatchGame);
    }

    private string History()
    {
        var files = _files.Reports(15);
        var sb = new StringBuilder();
        sb.Append("<h1>").Append(H(L("지난 기록", "History"))).Append("</h1><p class=\"sub\">")
          .Append(H(L("세션(게임을 켠 동안)마다 보고서 파일 하나가 레이드가 끝날 때마다 저장됩니다. 최근 15개까지 보관 — BepInEx\\RamCleaner\\",
              "One report file per game session, saved after every finished raid. The latest 15 are kept — BepInEx\\RamCleaner\\")))
          .Append("</p>");
        if (files.Length == 0)
        {
            sb.Append("<section class=\"card\"><p>").Append(H(L("아직 저장된 보고서가 없습니다. 레이드 한 판이 끝나면 생깁니다.",
                "No saved reports yet. One appears once a raid has finished."))).Append("</p></section>");
        }
        else
        {
            sb.Append("<section><div class=\"scroll\"><table class=\"hist\"><thead><tr><th>")
              .Append(H(L("세션 시작", "session started"))).Append("</th><th>")
              .Append(H(L("마지막 저장", "last saved"))).Append("</th><th class=n>KB</th></tr></thead><tbody>");
            foreach (var f in files)
            {
                var stamp = f.Name["RamCleaner-report-".Length..^".html".Length];
                var started = DateTime.TryParseExact(stamp, "yyyyMMdd-HHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var t)
                    ? t.ToString("yyyy-MM-dd HH:mm") : stamp;
                sb.Append("<tr><td><a href=\"reports/").Append(Uri.EscapeDataString(f.Name)).Append("\">").Append(H(started)).Append("</a></td><td>")
                  .Append(f.LastWriteTime.ToString("yyyy-MM-dd HH:mm")).Append("</td><td class=n>").Append(f.Length / 1024).Append("</td></tr>");
            }

            sb.Append("</tbody></table></div></section>");
        }

        return Shell("history", sb.ToString(), null, null)
            .Replace("<title>Tushonka Cleaner</title>", "<title>Tushonka Cleaner — " + H(L("지난 기록", "History")) + "</title>");
    }

    // ---------------------------------------------------------------- state and settings

    private string Value(string key, string fallback) => _files.Values.GetValueOrDefault(key) ?? fallback;

    private ServerStage CurrentStage()
    {
        var s = LiveConfig.Current.Server;
        return new ServerStage
        {
            S9 = s.FastCompression.Enabled,
            S12 = s.IsolatedBotRandomisation.Enabled,
            Compression = s.FastCompression.Level,
            C1 = !string.Equals(Value("C1 MenuVideo|Enabled", "true"), "false", StringComparison.OrdinalIgnoreCase),
            C2 = !string.Equals(Value("C2 AirdropPhysics|Enabled", "true"), "false", StringComparison.OrdinalIgnoreCase),
        };
    }

    /// <summary>게임의 BuildUiState와 같은 모양. gameRunning만 더 있습니다.</summary>
    public object State()
    {
        var preset = Value("0. Mode|Preset", MemoryModes["game"]);
        var language = Value("0. Mode|Language", Auto);
        return new
        {
            memoryMode = MemoryModes.FirstOrDefault(m => m.Value == preset).Key ?? "custom",
            serverLevel = ServerLevelPolicy.Detect(CurrentStage()),
            graphicsLevel = GraphicsLevelPolicy.FromStored(Value("Graphics|Level", "Off")),
            language = language == Korean ? "ko" : language == English ? "en" : "auto",
            resolvedLanguage = _en ? "en" : "ko",
            serverReady = true,
            serverConnected = true,
            serverStatus = L("서버 연결됨", "Server connected"),
            serverResult = "",
            memoryAvailable = true,
            graphicsAvailable = true,
            onboardingDone = string.Equals(Value("Internal|Onboarding completed", "false"), "true", StringComparison.OrdinalIgnoreCase),
            gameRunning = false,
        };
    }

    private static bool OnServer(string key) => key.StartsWith("S9 ", StringComparison.Ordinal) || key.StartsWith("S12 ", StringComparison.Ordinal) || key.StartsWith("Debug|", StringComparison.Ordinal);

    private string CurrentValue(JsonElement entry)
    {
        var key = entry.GetProperty("key").GetString()!;
        if (!OnServer(key))
        {
            return Value(key, entry.GetProperty("def").GetString() ?? "");
        }

        var config = LiveConfig.Current;
        var v = SettingAccess.Get(config.Server, config.Debug, key);
        // 압축 수준은 게임 화면에서 이름표로 다룹니다.
        if (key == "S9 FastCompression|Level" && entry.TryGetProperty("opts", out var opts))
        {
            var i = Array.IndexOf(LevelValues, v);
            if (i >= 0 && i < opts.GetArrayLength()) return opts[i].GetString()!;
        }

        return v;
    }

    /// <summary>게임의 api/settings와 같은 모양으로 답합니다.</summary>
    public string SettingsJson()
    {
        var entries = _files.Catalog()?.GetProperty("entries").EnumerateArray().ToList()
                      ?? throw new InvalidOperationException(L("설정 목록을 읽지 못했습니다. 모드를 다시 설치해 주세요.", "Could not read the settings list. Please reinstall the mod."));
        var field = _en ? "En" : "Ko";
        var groups = entries
            .Select((e, i) => (e, i))
            .OrderBy(x => x.e.GetProperty("sortKey").GetString(), StringComparer.Ordinal)
            .ThenByDescending(x => x.e.GetProperty("order").GetInt32())
            .ThenBy(x => x.i)
            .Select(x => x.e)
            .GroupBy(e => e.GetProperty("sortKey").GetString())
            .Select(g => new Dictionary<string, object?>
            {
                ["cat"] = g.First().GetProperty("cat" + field).GetString(),
                ["items"] = g.Select(e =>
                {
                    var item = new Dictionary<string, object?>
                    {
                        ["key"] = e.GetProperty("key").GetString(),
                        ["name"] = e.GetProperty("name" + field).GetString(),
                        ["desc"] = e.GetProperty("desc" + field).GetString(),
                        ["type"] = e.GetProperty("type").GetString(),
                        ["v"] = CurrentValue(e),
                        ["def"] = e.GetProperty("def").GetString(),
                    };
                    foreach (var extra in new[] { "min", "max", "opts" })
                    {
                        if (e.TryGetProperty(extra, out var value)) item[extra] = value;
                    }

                    return item;
                }).ToList(),
            }).ToList();
        return JsonSerializer.Serialize(new { groups, state = State() });
    }

    public string Set(string key, string value)
    {
        var entry = _files.Catalog()?.GetProperty("entries").EnumerateArray()
            .FirstOrDefault(e => e.GetProperty("key").GetString() == key);
        if (entry is null || entry.Value.ValueKind == JsonValueKind.Undefined)
        {
            return JsonSerializer.Serialize(new { ok = false, error = L("모르는 설정입니다.", "Unknown setting.") });
        }

        try
        {
            if (OnServer(key))
            {
                var stored = value;
                if (key == "S9 FastCompression|Level" && entry.Value.TryGetProperty("opts", out var opts))
                {
                    var labels = opts.EnumerateArray().Select(o => o.GetString()).ToList();
                    var i = labels.IndexOf(value);
                    if (i >= 0) stored = LevelValues[i];
                }

                var config = LiveConfig.Current;
                var (server, debug) = SettingAccess.Set(config.Server, config.Debug, key, stored);
                LiveConfig.Update(server, debug);
                return JsonSerializer.Serialize(new { ok = true, v = CurrentValue(entry.Value) });
            }

            return JsonSerializer.Serialize(new { ok = true, v = _files.Write(entry.Value, value) });
        }
        catch (Exception ex) when (ex is FormatException or IOException or UnauthorizedAccessException)
        {
            return JsonSerializer.Serialize(new { ok = false, error = ex.Message });
        }
    }

    /// <summary>게임의 api/ui와 같은 일을 합니다. 메모리 모드는 게임이 시작할 때 파일 값을 보고 적용합니다.</summary>
    public string ApplyUi(string key, string value)
    {
        switch (key)
        {
            case "memoryMode":
                if (!MemoryModes.TryGetValue(value, out var preset)) throw new FormatException("unknown mode");
                _files.WriteRaw("0. Mode", "Preset", preset);
                break;
            case "graphicsLevel":
                _files.WriteRaw("Graphics", "Level", GraphicsLevelPolicy.ToStored(value));
                break;
            case "serverLevel":
                ApplyServerStage(ServerLevelPolicy.Get(value));
                break;
            case "quick":
                var on = value == "recommended";
                _files.WriteRaw("0. Mode", "Preset", MemoryModes[on ? "game" : "off"]);
                if (on)
                {
                    _files.WriteRaw("1. Auto GC", "Growth trigger (GB)", "1.5");
                    _files.WriteRaw("1. Auto GC", "Work per frame (ms)", "2");
                }

                if (!on) _files.WriteRaw("Graphics", "Level", "Off");
                _files.WriteRaw("Internal", "Onboarding completed", "true");
                ApplyServerStage(ServerLevelPolicy.Get(on ? "recommended" : "off"));
                break;
            default:
                throw new FormatException("unknown setting");
        }

        return JsonSerializer.Serialize(new { ok = true, state = State() });
    }

    private void ApplyServerStage(ServerStage stage)
    {
        var config = LiveConfig.Current;
        var server = config.Server with
        {
            FastCompression = config.Server.FastCompression with { Enabled = stage.S9, Level = stage.Compression },
            IsolatedBotRandomisation = config.Server.IsolatedBotRandomisation with { Enabled = stage.S12 },
        };
        LiveConfig.Update(server, config.Debug);
        _files.WriteRaw("C1 MenuVideo", "Enabled", stage.C1 ? "true" : "false");
        _files.WriteRaw("C2 AirdropPhysics", "Enabled", stage.C2 ? "true" : "false");
    }
}
