using System.Collections.Generic;
using System.Linq;
using System.Net;
using Tushonka.Cleaner.Client.Performance;

namespace Tushonka.Cleaner.Client
{
    /// <summary>서버 최적화 화면을 같은 DLL의 설정 구성 요소에 직접 연결합니다.</summary>
    public partial class CleanerPlugin
    {
        private bool _cpLanguagePending = true;
        private static bool CompoundingPerfReady => PerformanceModule.Instance?.Ready ?? false;

        private void SyncCompoundingPerfLanguage(float now)
        {
            if (!_cpLanguagePending || !CompoundingPerfReady) return;
            _cpLanguagePending = false;
            PerformanceModule.Instance.SetLanguage(_language.Value);
        }

        private static IEnumerable<WebSetting> CompoundingPerfSettings()
        {
            var module = PerformanceModule.Instance;
            if (module == null || !module.Ready) yield break;
            foreach (var entry in module.Settings)
            {
                var attributes = entry.Description.Tags?.OfType<ConfigurationManagerAttributes>().FirstOrDefault();
                if (attributes == null || attributes.CustomDrawer != null || attributes.Browsable == false) continue;
                var definition = entry.Definition;
                var category = attributes.Category ?? definition.Section;
                yield return new WebSetting
                {
                    Key = definition.Section + "|" + definition.Key,
                    Category = category,
                    SortKey = category.Length >= 3 ? category.Substring(0, 3) : category,
                    Name = attributes.DispName ?? definition.Key,
                    Description = attributes.Description ?? entry.Description.Description ?? string.Empty,
                    Order = attributes.Order ?? 0,
                    Entry = entry,
                };
            }
        }

        private static string CompoundingPerfMissing => Loc.L(
            "구형 서버 최적화 클라이언트가 감지되어 이 기능을 중지했습니다. 구형 모드를 삭제하고 게임을 다시 시작하십시오.",
            "The legacy server optimisation client was detected. Remove it and restart the game to enable this feature.");

        private string BuildCompoundingPerfSettingsJson() => CompoundingPerfReady
            ? SettingsJson(CompoundingPerfSettings()) : "{\"error\":" + WebServer.Str(CompoundingPerfMissing) + "}";

        private string ApplyCompoundingPerfSetting(string key, string value) => CompoundingPerfReady
            ? ApplySetting(CompoundingPerfSettings(), key, value) : "{\"ok\":false,\"error\":" + WebServer.Str(CompoundingPerfMissing) + "}";

        private string BuildCompoundingPerfStatusJson(bool refresh)
        {
            var module = PerformanceModule.Instance;
            if (module == null || !module.Ready) return "{\"error\":" + WebServer.Str(CompoundingPerfMissing) + "}";
            if (refresh) module.Refresh();
            return "{\"connected\":" + WebServer.Bool(module.Connected) +
                   ",\"status\":" + WebServer.Str(module.Status) +
                   ",\"last\":" + WebServer.Str(module.LastResult) +
                   ",\"version\":" + WebServer.Str(ModVersion) + "}";
        }

        private static string ServerPage()
        {
            string H(string text) => WebUtility.HtmlEncode(text);
            string content =
                "<h1>" + H(Loc.L("서버 최적화 (CompoundingPerf)", "Server optimisation (CompoundingPerf)")) + "</h1><p class=\"sub\">" +
                H(Loc.L("SPT 서버 모드 CompoundingPerf의 설정입니다(게임 F12의 'Tushonka Cleaner'와 같은 항목). 바꾸면 0.8초 뒤 서버에 적용되고 서버의 config.json에도 저장됩니다. 서버 재시작은 필요 없습니다.",
                        "Settings of the SPT server mod CompoundingPerf (the same entries as 'Tushonka Cleaner' in F12). Changes reach the server 0.8 s later, apply at once and are saved to its config.json. No server restart needed.")) +
                "</p>";
            if (!CompoundingPerfReady)
            {
                content += "<section class=\"card\"><p>" + H(CompoundingPerfMissing) + "</p></section>";
                return Shell(Loc.L("RAM 클리너 — 서버 최적화", "RAM Cleaner — server optimisation"), "server", content, Strings("offline", Offline), null);
            }

            content +=
                "<section class=\"card\"><h2>" + H(Loc.L("서버 상태", "Server status")) + "</h2><pre id=\"cpStatus\">" + H(Loc.L("읽는 중...", "Reading...")) + "</pre>" +
                "<p class=\"msg\" id=\"cpLast\" role=\"status\"></p><div class=\"actions\"><button type=\"button\" id=\"cpRefresh\">" +
                H(Loc.L("서버에서 다시 읽기", "Read again from the server")) + "</button></div></section>" +
                "<div class=\"toolbar\"><input type=\"search\" id=\"q\" placeholder=\"" + H(Loc.L("설정 검색 (예: 정리, 압축, 로그)", "Search settings (e.g. cleanup, compression, log)")) +
                "\" aria-label=\"" + H(Loc.L("설정 검색", "Search settings")) + "\"></div><p class=\"msg\" id=\"msg\" role=\"status\"></p><div id=\"list\"></div>";
            return Shell(Loc.L("RAM 클리너 — 서버 최적화", "RAM Cleaner — server optimisation"), "server", content,
                SettingsStrings("api/cp/settings", "api/cp/status",
                    Loc.L("게임에 적용됨 — 서버 설정은 0.8초 뒤 서버로 보냅니다(09번 게임 쪽 항목은 게임에서만). 결과는 위 '서버 상태'에 나옵니다.",
                          "Applied in the game — server settings are sent to the server 0.8 s later (the game-side items in 09 stay in the game). The result shows under 'Server status' above.")),
                "settings.js");
        }
    }
}
