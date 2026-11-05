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
    /// <summary>Smoothie는 다른 작성자의 별도 모드입니다. 기능을 막거나 파일을 옮기지 않고 안내만 합니다.</summary>
    public bool Smoothie { get; private set; }
    public List<string> Guids { get; } = new List<string>();
    public bool Any => Memory || Performance || Graphics;

    public const string SmoothieWarningKo =
        "Tushonka Cleaner: Smoothie 모드와 함께 쓰지 마십시오. 같은 게임 기능을 건드려 효과가 겹치거나 오류가 날 수 있습니다. 둘 중 하나만 설치하십시오.";
    public const string SmoothieWarningEn =
        "Tushonka Cleaner: Do not use this mod together with Smoothie. Both change the same game systems, which can cause overlapping effects or errors. Keep only one of them.";

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
            else
            {
                if (EqualsAny(id, "com.tominocz.smoothie", "Smoothie")) result.Smoothie = true;
                continue;
            }
            if (seen.Add(guid)) result.Guids.Add(guid);
        }
        return result;
    }

    private static bool EqualsAny(string value, string a, string b) =>
        string.Equals(value, a, StringComparison.OrdinalIgnoreCase) || string.Equals(value, b, StringComparison.OrdinalIgnoreCase);
}
