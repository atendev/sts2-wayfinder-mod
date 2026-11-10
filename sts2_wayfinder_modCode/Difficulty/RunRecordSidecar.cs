using System.Text.Json;
using System.Text.Json.Nodes;

namespace sts2_wayfinder_mod.sts2_wayfinder_modCode.Difficulty;

/// <summary>The per-run difficulty record that sits beside the run history, for <c>scripts/run_report.py</c>. It lives in a <c>wayfinder</c> subfolder because the game lists every file in <c>history/</c> as a run.</summary>
internal static class RunRecordSidecar
{
    public const int SchemaVersion = 1;
    public const string ModVersion = "v0.2.0";
    public const string FolderName = "wayfinder";

    public static string FileName(long startTime) => $"{startTime}.json";

    public static string ToJson(DifficultyOptions options)
    {
        var node = JsonNode.Parse(options.ToJson())!.AsObject();
        var record = new JsonObject
        {
            ["schema_version"] = SchemaVersion,
            ["mod_version"] = ModVersion,
            ["preset"] = options.Preset.ToString(),
        };
        foreach (var (key, value) in node)
            record[key] = value?.DeepClone();

        return record.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
    }
}
