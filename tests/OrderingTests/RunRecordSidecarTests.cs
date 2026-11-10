using System.Text.Json;
using sts2_wayfinder_mod.sts2_wayfinder_modCode.Difficulty;
using Xunit;

namespace OrderingTests;

public class RunRecordSidecarTests
{
    [Fact]
    public void RecordsPresetVersionsAndEveryTuningValue()
    {
        using var doc = JsonDocument.Parse(RunRecordSidecar.ToJson(DifficultyOptions.PowerCurve));
        var root = doc.RootElement;

        Assert.Equal("PowerCurve", root.GetProperty("preset").GetString());
        Assert.Equal(RunRecordSidecar.SchemaVersion, root.GetProperty("schema_version").GetInt32());
        Assert.Equal(RunRecordSidecar.ModVersion, root.GetProperty("mod_version").GetString());
        Assert.Equal(4, root.GetProperty("CardChoices").GetInt32());
        Assert.True(root.GetProperty("ScaleBosses").GetBoolean());
        Assert.Equal("Balanced", root.GetProperty("ChestOdds").GetString());
    }

    [Fact]
    public void HandEditedOptionsAreRecordedAsCustom()
    {
        var options = DifficultyOptions.PowerCurve with { CardChoices = 5 };
        using var doc = JsonDocument.Parse(RunRecordSidecar.ToJson(options));
        Assert.Equal("Custom", doc.RootElement.GetProperty("preset").GetString());
    }

    [Fact]
    public void FileIsNamedAfterTheRunStartTime() =>
        Assert.Equal("1794162390.json", RunRecordSidecar.FileName(1794162390));
}
