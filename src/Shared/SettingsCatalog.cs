#nullable enable
using System.Collections.Generic;

namespace Tushonka.Cleaner;

/// <summary>
/// Names and descriptions (Korean / English) of every live setting, shared by the client's F12
/// page and the server's web page (the launcher's mod page), so the two always say the same.
/// The values themselves live in <see cref="ServerToggles"/> / <see cref="DebugOptions"/>; ranges
/// here must match <c>LiveConfig.Sanitize</c>.
/// </summary>
public sealed class SettingInfo
{
    public string Section = "";
    public string Key = "";
    public string CategoryKo = "";
    public string CategoryEn = "";
    public int Order;
    public string NameKo = "";
    public string NameEn = "";
    public string DescriptionKo = "";
    public string DescriptionEn = "";

    /// <summary>"bool", "int" (with <see cref="Min"/>/<see cref="Max"/>) or "list" (<see cref="Options"/>).</summary>
    public string Kind = "bool";
    public int Min;
    public int Max;
    public string[] Options = [];
    public string[] OptionsKo = [];
    public string[] OptionsEn = [];

    public string Id => Section + "|" + Key;
}

public static class SettingsCatalog
{
    public static readonly SettingInfo[] All =
    [
                                                                        new SettingInfo
        {
            Section = "S9 FastCompression",
            Key = "Enabled",
            CategoryKo = "04. 응답 압축 (S9)",
            CategoryEn = "04. Response compression (S9)",
            Order = 20,
            NameKo = "사용",
            NameEn = "Enabled",
            DescriptionKo = "서버 응답을 가장 느린 압축(바닐라)으로 보내는 것을 아래 수준으로 바꿉니다. 실측: 주요 응답 9개 합계가 636ms에서 440ms로 줄고 크기는 그대로입니다.",
            DescriptionEn = "Vanilla sends server replies with the slowest compression; this switches to the level below. Measured over nine main replies: 636 ms down to 440 ms at the same size.",
            Kind = "bool",
        },
        new SettingInfo
        {
            Section = "S9 FastCompression",
            Key = "Level",
            CategoryKo = "04. 응답 압축 (S9)",
            CategoryEn = "04. Response compression (S9)",
            Order = 10,
            NameKo = "압축 수준",
            NameEn = "Level",
            DescriptionKo = "균형: 크기는 바닐라와 같고 더 빠름 (추천)\n빠름: 가장 빠르지만 크기가 약 60% 늘어 원격 FIKA에는 불리함\n최소 크기: 바닐라와 같음 (가장 느림)",
            DescriptionEn = "Optimal: same size as vanilla, faster (recommended)\nFastest: quickest, but about 60% larger, worse for remote FIKA players\nSmallestSize: same as vanilla (slowest)",
            Kind = "list",
            Options = new[] { "Fastest", "Optimal", "SmallestSize", "NoCompression" },
            OptionsKo = new[] { "빠름", "균형 (추천)", "최소 크기 (바닐라)", "압축 안 함" },
            OptionsEn = new[] { "Fastest", "Optimal (recommended)", "Smallest size (vanilla)", "No compression" },
        },
        new SettingInfo
        {
            Section = "S12 IsolatedBotRandomisation",
            Key = "Enabled",
            CategoryKo = "05. 봇 장비 버그 수정 (S12)",
            CategoryEn = "05. Bot gear bug fix (S12)",
            Order = 10,
            NameKo = "사용",
            NameEn = "Enabled",
            DescriptionKo = "성능이 아니라 바닐라 버그 수정입니다. 야간 레이드 장비 보정이 공용 설정에 계속 누적되는 문제를 막습니다.",
            DescriptionEn = "A vanilla bug fix, not a speed-up: stops the night-raid gear adjustment from piling up in the shared settings.",
            Kind = "bool",
        },
                                new SettingInfo
        {
            Section = "Debug",
            Key = "Enabled",
            CategoryKo = "08. 디버그 로그",
            CategoryEn = "08. Debug log",
            Order = 40,
            NameKo = "사용",
            NameEn = "Enabled",
            DescriptionKo = "켜면 서버의 SPT_Runtime\\user\\logs\\CompoundingPerf\\ 에 확인용 로그 파일이 새로 생깁니다.\n기능이 제대로 걸렸는지, 레이드마다 서버 메모리·느린 요청·봇 생성 시간이 기록됩니다. 확인이 끝나면 꺼 두세요.",
            DescriptionEn = "On = a new check log file in the server's SPT_Runtime\\user\\logs\\CompoundingPerf\\.\nIt records whether each feature hooked in, and per raid the server memory, slow requests and bot generation time. Turn it off when done.",
            Kind = "bool",
        },
        new SettingInfo
        {
            Section = "Debug",
            Key = "SummaryIntervalMinutes",
            CategoryKo = "08. 디버그 로그",
            CategoryEn = "08. Debug log",
            Order = 30,
            NameKo = "요약 간격 (분)",
            NameEn = "Summary interval (min)",
            DescriptionKo = "이 간격마다 서버 상태 요약 한 줄. 0이면 요약 안 함(레이드 시작/끝 요약은 계속 남음).",
            DescriptionEn = "One server summary line per this interval. 0 = none (raid start/end summaries are still written).",
            Kind = "int",
            Min = 0,
            Max = 60,
        },
        new SettingInfo
        {
            Section = "Debug",
            Key = "SlowRequestMs",
            CategoryKo = "08. 디버그 로그",
            CategoryEn = "08. Debug log",
            Order = 20,
            NameKo = "느린 요청 기준 (ms)",
            NameEn = "Slow request threshold (ms)",
            DescriptionKo = "이보다 오래 걸린 서버 요청은 하나하나 기록합니다.",
            DescriptionEn = "Server requests that take longer than this are logged one by one.",
            Kind = "int",
            Min = 50,
            Max = 5000,
        },
        new SettingInfo
        {
            Section = "Debug",
            Key = "KeepFiles",
            CategoryKo = "08. 디버그 로그",
            CategoryEn = "08. Debug log",
            Order = 10,
            NameKo = "보관할 로그 파일 수",
            NameEn = "Log files to keep",
            DescriptionKo = "로그를 새로 켤 때마다 파일이 하나 생기고, 최근 이만큼만 남깁니다.",
            DescriptionEn = "Each time the log is turned on a new file starts; only this many recent files are kept.",
            Kind = "int",
            Min = 1,
            Max = 50,
        },
    ];

    public static SettingInfo Get(string section, string key)
    {
        foreach (var info in All)
        {
            if (info.Section == section && info.Key == key)
            {
                return info;
            }
        }

        throw new KeyNotFoundException(section + "|" + key);
    }
}
