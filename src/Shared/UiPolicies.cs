#nullable enable
using System;
using System.Collections.Generic;

namespace Tushonka.Cleaner;

/// <summary>화면과 F12에서 함께 사용하는 서버 단계 표입니다.</summary>
public sealed record ServerStage
{
    public bool S8 { get; init; }
    public bool S9 { get; init; }
    public bool S11 { get; init; }
    public bool S12 { get; init; }
    public bool S13 { get; init; }
    public bool S15 { get; init; }
    public bool S16 { get; init; }
    public bool C1 { get; init; }
    public bool C2 { get; init; }
    public string Compression { get; init; } = "Fastest";
    public string StartGcMode { get; init; } = "Background";
}

public static class ServerLevelPolicy
{
    private static readonly IReadOnlyDictionary<string, ServerStage> Stages = new Dictionary<string, ServerStage>
    {
        ["recommended"] = new ServerStage { S8 = true, S9 = true, S12 = true, S13 = true, S15 = true, S16 = true, C1 = true, C2 = true },
        ["balanced"] = new ServerStage { S8 = true, S9 = true, S12 = true, S13 = true, C1 = true, C2 = true, Compression = "Optimal" },
        ["off"] = new ServerStage(),
    };

    public static ServerStage Get(string level) => Stages.TryGetValue(level, out var stage)
        ? stage : throw new ArgumentException("Unknown server level", nameof(level));

    public static string Detect(ServerStage value)
    {
        foreach (var pair in Stages)
        {
            var s = pair.Value;
            if (s.S8 == value.S8 && s.S9 == value.S9 && s.S11 == value.S11 && s.S12 == value.S12 &&
                s.S13 == value.S13 && s.S15 == value.S15 && s.S16 == value.S16 && s.C1 == value.C1 && s.C2 == value.C2 &&
                (!s.S9 || string.Equals(s.Compression, value.Compression, StringComparison.OrdinalIgnoreCase)) &&
                (!s.S15 || string.Equals(s.StartGcMode, value.StartGcMode, StringComparison.OrdinalIgnoreCase))) return pair.Key;
        }
        return "custom";
    }
}

public static class LanguagePolicy
{
    public static string Resolve(string? preference, string? game, string? system)
    {
        if (preference == "한국어" || preference == "ko") return "ko";
        if (preference == "English" || preference == "en") return "en";
        var source = string.IsNullOrWhiteSpace(game) ? system : game;
        return source != null && (source.Equals("ko", StringComparison.OrdinalIgnoreCase) ||
            source.StartsWith("ko-", StringComparison.OrdinalIgnoreCase) || source.StartsWith("ko_", StringComparison.OrdinalIgnoreCase) ||
            source.Equals("Korean", StringComparison.OrdinalIgnoreCase) || source == "한국어") ? "ko" : "en";
    }
}

public static class GraphicsLevelPolicy
{
    public static string ToStored(string choice) => choice switch
    {
        "off" => "Off", "light" => "LightTouch", "more" => "Balanced", "most" => "Max",
        _ => throw new ArgumentException("Unknown graphics level", nameof(choice)),
    };
    public static string FromStored(string value) => value switch
    {
        "LightTouch" => "light", "Balanced" => "more", "Max" => "most", _ => "off",
    };
}
