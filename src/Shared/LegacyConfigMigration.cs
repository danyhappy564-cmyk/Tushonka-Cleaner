#nullable disable
using System;
using System.Collections.Generic;

namespace Tushonka.Cleaner;

/// <summary>구형 cfg를 파싱하고 실제로 바인딩되는 동일 절·키에만 값을 제공합니다.</summary>
public static class LegacyConfigMigration
{
    public static Dictionary<string, string> Parse(bool newConfigExists, IEnumerable<string> lines)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        if (newConfigExists) return result;
        var section = "";
        foreach (var raw in lines)
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal) || line.StartsWith(";", StringComparison.Ordinal)) continue;
            if (line.StartsWith("[", StringComparison.Ordinal) && line.EndsWith("]", StringComparison.Ordinal))
            {
                section = line.Substring(1, line.Length - 2);
                continue;
            }
            var equal = line.IndexOf('=');
            if (section.Length == 0 || equal <= 0) continue;
            var key = line.Substring(0, equal).Trim();
            if (key.Length > 0) result[section + "|" + key] = line.Substring(equal + 1).Trim();
        }
        return result;
    }

    public static bool TryGet(IReadOnlyDictionary<string, string> values, string section, string key, out string value) =>
        values.TryGetValue(section + "|" + key, out value);
}
