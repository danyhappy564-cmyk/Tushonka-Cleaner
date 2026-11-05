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
            PerformanceModule.Instance.SetLanguage(Loc.En ? LanguageEnglish : LanguageKorean);
        }

        private static IEnumerable<WebSetting> CompoundingPerfSettings()
        {
            var module = PerformanceModule.Instance;
            if (module == null || !module.Ready) yield break;
            foreach (var entry in module.Settings)
            {
                var attributes = entry.Description.Tags?.OfType<ConfigurationManagerAttributes>().FirstOrDefault();
                if (attributes == null || attributes.CustomDrawer != null || entry.Definition.Section == "Language") continue;
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

    }
}
