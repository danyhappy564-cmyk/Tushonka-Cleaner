using System.Text.Json;
using System.Text.RegularExpressions;
using Xunit;

namespace Tushonka.Cleaner.Tests;

/// <summary>
/// 빌드에 들어가는 설정 목록은 게임을 켜기 전 설정 페이지가 씁니다. 게임이 저장한 파일을 복사해 넣으므로,
/// 버전을 올리거나 설정을 바꾼 뒤 복사를 잊으면 여기서 실패합니다.
/// </summary>
public class SettingsCatalogFileTests
{
    private static string Repo => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    private static JsonElement Read()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(Repo, "src", "Tushonka.Cleaner.Server", "MemoryWeb", "settings-catalog.json")));
        return doc.RootElement.Clone();
    }

    [Fact]
    public void Version_matches_the_mod()
    {
        var props = File.ReadAllText(Path.Combine(Repo, "Directory.Build.props"));
        var version = Regex.Match(props, @"<Version>([^<]+)</Version>").Groups[1].Value;
        Assert.Equal(version, Read().GetProperty("version").GetString());
    }

    [Fact]
    public void Holds_graphics_and_no_removed_settings()
    {
        var ids = Read().GetProperty("entries").EnumerateArray()
            .Select(e => e.GetProperty("key").GetString()!).ToList();
        Assert.Contains("Graphics|Level", ids);
        Assert.Contains("Graphics.Tuning|Grass", ids);
        Assert.Contains("0. Mode|Preset", ids);
        Assert.Contains("C2 AirdropPhysics|Enabled", ids);
        Assert.Contains("S9 FastCompression|Level", ids);
        Assert.DoesNotContain(ids, id => id.StartsWith("2. Working set", StringComparison.Ordinal));
        Assert.DoesNotContain("4. Assets|Auto in raid", ids);
    }
}
