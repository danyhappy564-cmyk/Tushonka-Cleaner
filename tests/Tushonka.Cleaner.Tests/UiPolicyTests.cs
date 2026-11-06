using Xunit;
using Tushonka.Cleaner;

namespace Tushonka.Cleaner.Tests;

public class UiPolicyTests
{
    [Theory]
    [InlineData("recommended", true)]
    [InlineData("off", false)]
    public void ServerStagesRoundTrip(string level, bool on)
    {
        var stage = ServerLevelPolicy.Get(level);
        Assert.Equal(on, stage.S9);
        Assert.Equal(on, stage.S12);
        Assert.Equal(on, stage.C1);
        Assert.Equal(on, stage.C2);
        Assert.Equal("Optimal", stage.Compression);
        Assert.Equal(level, ServerLevelPolicy.Detect(stage));
    }

    [Fact]
    public void ServerDetectsCustomAndIgnoresInactiveParameters()
    {
        Assert.Equal("custom", ServerLevelPolicy.Detect(ServerLevelPolicy.Get("recommended") with { C2 = false }));
        Assert.Equal("custom", ServerLevelPolicy.Detect(ServerLevelPolicy.Get("recommended") with { Compression = "SmallestSize" }));
        // 예전 기본값 Fastest를 쓰는 사람도 권장으로 봅니다.
        Assert.Equal("recommended", ServerLevelPolicy.Detect(ServerLevelPolicy.Get("recommended") with { Compression = "Fastest" }));
        Assert.Equal("off", ServerLevelPolicy.Detect(ServerLevelPolicy.Get("off") with { Compression = "SmallestSize" }));
        Assert.Throws<ArgumentException>(() => ServerLevelPolicy.Get("unknown"));
    }

    [Theory]
    [InlineData("auto", "ko-KR", "en-US", "ko")]
    [InlineData("auto", "en", "ko-KR", "en")]
    [InlineData("auto", null, "ko-KR", "ko")]
    [InlineData("auto", null, "de-DE", "en")]
    [InlineData("한국어", "en", "en-US", "ko")]
    [InlineData("English", "ko", "ko-KR", "en")]
    [InlineData("auto", "Korean", "en", "ko")]
    [InlineData("auto", "kr", "en", "ko")]
    [InlineData("auto", "kr-en", "en", "ko")]
    public void LanguageFollowsExplicitThenGameThenSystem(string preference, string? game, string system, string expected)
        => Assert.Equal(expected, LanguagePolicy.Resolve(preference, game, system));

    [Theory]
    [InlineData("off", "Off")]
    [InlineData("light", "LightTouch")]
    [InlineData("more", "Balanced")]
    [InlineData("most", "Max")]
    public void GraphicsChoicesRoundTrip(string choice, string stored)
    {
        Assert.Equal(stored, GraphicsLevelPolicy.ToStored(choice));
        Assert.Equal(choice, GraphicsLevelPolicy.FromStored(stored));
    }

    [Fact]
    public void UnknownGraphicsChoiceIsRejected()
        => Assert.Throws<ArgumentException>(() => GraphicsLevelPolicy.ToStored("unknown"));
}
