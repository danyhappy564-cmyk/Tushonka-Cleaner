using Xunit;

namespace Tushonka.Cleaner.Tests;

public class IntegrationPolicyTests
{
    [Fact]
    public void Existing_new_config_prevents_import()
    {
        Assert.Empty(LegacyConfigMigration.Parse(true, ["[0. Mode]", "Language = English"]));
    }

    [Fact]
    public void Legacy_values_are_matched_by_section_and_key()
    {
        var values = LegacyConfigMigration.Parse(false,
            ["# comment", "[0. Mode]", "Language = English", "Unknown = ignored", "[Other]", "Language = 한국어"]);
        Assert.True(LegacyConfigMigration.TryGet(values, "0. Mode", "Language", out var value));
        Assert.Equal("English", value);
        Assert.False(LegacyConfigMigration.TryGet(values, "Missing", "Language", out _));
        Assert.Equal("한국어", values["Other|Language"]);
    }

    [Fact]
    public void Legacy_parser_preserves_equals_inside_values_and_ignores_malformed_lines()
    {
        var values = LegacyConfigMigration.Parse(false,
            ["orphan = ignored", "[General]", "broken", "# x = ignored", "Token = a=b", "Empty ="]);
        Assert.Equal(2, values.Count);
        Assert.Equal("a=b", values["General|Token"]);
        Assert.Equal("", values["General|Empty"]);
    }

    [Theory]
    [InlineData("com.cactuspie.ramcleanerinterval", true, false, false)]
    [InlineData("com.TYR.DeClutter", false, false, true)]
    [InlineData("com.echostarz.compoundingperf.client", false, true, false)]
    [InlineData("com.echostarz.compoundingperf", false, true, false)]
    [InlineData("com.cactuspie.ramcleanerinterval.server", true, false, false)]
    [InlineData("com.zzap.tushonka.cleaner", false, false, false)]
    public void Conflict_disables_only_the_overlapping_feature(string guid, bool memory, bool performance, bool graphics)
    {
        var result = LegacyConflicts.Detect([guid]);
        Assert.Equal(memory, result.Memory);
        Assert.Equal(performance, result.Performance);
        Assert.Equal(graphics, result.Graphics);
    }

    [Fact]
    public void Conflict_matching_is_case_insensitive_and_combines_all_modules()
    {
        var result = LegacyConflicts.Detect(["COM.CACTUSPIE.RAMCLEANERINTERVAL", "com.TYR.DeClutter", "com.echostarz.compoundingperf.client"]);
        Assert.True(result.Memory && result.Performance && result.Graphics);
        Assert.Equal(3, result.Guids.Count);
    }
}
