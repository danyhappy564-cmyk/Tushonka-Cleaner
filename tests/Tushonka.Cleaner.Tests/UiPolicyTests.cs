using Xunit;
using Tushonka.Cleaner;

namespace Tushonka.Cleaner.Tests;

public class UiPolicyTests
{
    [Theory]
    [InlineData("recommended", true, true, "Fastest")]
    [InlineData("balanced", true, false, "Optimal")]
    [InlineData("off", false, false, "Fastest")]
    public void ServerStagesRoundTrip(string level, bool common, bool raid, string compression)
    {
        var stage = ServerLevelPolicy.Get(level);
        Assert.Equal(common, stage.S8);
        Assert.Equal(common, stage.S9);
        Assert.Equal(common, stage.S12);
        Assert.Equal(common, stage.S13);
        Assert.Equal(common, stage.C1);
        Assert.Equal(common, stage.C2);
        Assert.Equal(raid, stage.S15);
        Assert.Equal(raid, stage.S16);
        Assert.False(stage.S11);
        Assert.Equal(compression, stage.Compression);
        Assert.Equal("Background", stage.StartGcMode);
        Assert.Equal(level, ServerLevelPolicy.Detect(stage));
    }

    [Fact]
    public void ServerDetectsCustomAndIgnoresInactiveParameters()
    {
        Assert.Equal("custom", ServerLevelPolicy.Detect(ServerLevelPolicy.Get("recommended") with { S11 = true }));
        Assert.Equal("custom", ServerLevelPolicy.Detect(ServerLevelPolicy.Get("recommended") with { C2 = false }));
        Assert.Equal("custom", ServerLevelPolicy.Detect(ServerLevelPolicy.Get("recommended") with { Compression = "Optimal" }));
        Assert.Equal("custom", ServerLevelPolicy.Detect(ServerLevelPolicy.Get("recommended") with { StartGcMode = "Skip" }));
        Assert.Equal("off", ServerLevelPolicy.Detect(ServerLevelPolicy.Get("off") with { Compression = "SmallestSize", StartGcMode = "Vanilla" }));
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
