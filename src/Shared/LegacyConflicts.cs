#nullable disable
using System;
using System.Collections.Generic;

namespace Tushonka.Cleaner;

/// <summary>구형 모드의 식별자를 기능별 중복 상태로 분류합니다.</summary>
public sealed class LegacyConflicts
{
    public bool Memory { get; private set; }
    public bool Performance { get; private set; }
    public bool Graphics { get; private set; }
    public List<string> Guids { get; } = new List<string>();
    public bool Any => Memory || Performance || Graphics;

    public static LegacyConflicts Detect(IEnumerable<string> identifiers)
    {
        var result = new LegacyConflicts();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var id in identifiers)
        {
            string guid;
            if (EqualsAny(id, "com.cactuspie.ramcleanerinterval", "CactusPie.RamCleanerInterval"))
            {
                result.Memory = true;
                guid = "com.cactuspie.ramcleanerinterval";
            }
            else if (EqualsAny(id, "com.cactuspie.ramcleanerinterval.server", "RamCleanerInterval.Server"))
            {
                result.Memory = true;
                guid = "com.cactuspie.ramcleanerinterval.server";
            }
            else if (EqualsAny(id, "com.echostarz.compoundingperf.client", "CompoundingPerf.Client"))
            {
                result.Performance = true;
                guid = "com.echostarz.compoundingperf.client";
            }
            else if (EqualsAny(id, "com.echostarz.compoundingperf", "CompoundingPerf"))
            {
                result.Performance = true;
                guid = "com.echostarz.compoundingperf";
            }
            else if (EqualsAny(id, "com.TYR.DeClutter", "TYR_DeClutterer"))
            {
                result.Graphics = true;
                guid = "com.TYR.DeClutter";
            }
            else continue;
            if (seen.Add(guid)) result.Guids.Add(guid);
        }
        return result;
    }

    private static bool EqualsAny(string value, string a, string b) =>
        string.Equals(value, a, StringComparison.OrdinalIgnoreCase) || string.Equals(value, b, StringComparison.OrdinalIgnoreCase);
}
