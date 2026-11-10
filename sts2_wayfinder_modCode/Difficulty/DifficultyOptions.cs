using System.Text.Json;

namespace sts2_wayfinder_mod.sts2_wayfinder_modCode.Difficulty;

internal readonly record struct EnemyScalingToggles(bool ScaleBosses, bool ScalePathHp, bool ScalePathDamage);

internal enum DifficultyPreset
{
    Off,
    BossesOnly,
    PowerCurve,
    Custom,
}

internal enum ChestOdds
{
    Generous,
    Balanced,
    Stingy,
}

internal enum GoldRate
{
    Lean,
    Vanilla,
    Rich,
}

/// <summary>Every player-facing difficulty option, as snapshotted when a run starts.</summary>
internal sealed record DifficultyOptions(
    bool ScaleBosses,
    bool ScalePathHp,
    bool ScalePathDamage,
    bool AlternateCardRewards,
    int CardChoices,
    bool UseChestChances,
    ChestOdds ChestOdds,
    bool LowerEliteRelicRarity,
    GoldRate GoldRate)
{
    public const int MinCardChoices = 3;
    public const int MaxCardChoices = 5;

    public static readonly DifficultyOptions Off =
        new(false, false, false, false, 3, false, ChestOdds.Balanced, false, GoldRate.Vanilla);

    public static readonly DifficultyOptions BossesOnly = Off with { ScaleBosses = true };

    public static readonly DifficultyOptions PowerCurve =
        new(true, true, true, true, 4, true, ChestOdds.Balanced, true, GoldRate.Vanilla);

    private static readonly JsonSerializerOptions Json = new() { Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() } };

    public static DifficultyOptions For(DifficultyPreset preset) => preset switch
    {
        DifficultyPreset.BossesOnly => BossesOnly,
        DifficultyPreset.PowerCurve => PowerCurve,
        _ => Off,
    };

    /// <summary>The preset these options match exactly, or Custom. Chest odds only count while chest chances are on, so a hidden Chest odds choice never makes a run Custom.</summary>
    public DifficultyPreset Preset
    {
        get
        {
            foreach (var preset in new[] { DifficultyPreset.Off, DifficultyPreset.BossesOnly, DifficultyPreset.PowerCurve })
                if (Normalized == For(preset).Normalized)
                    return preset;

            return DifficultyPreset.Custom;
        }
    }

    public EnemyScalingToggles EnemyScaling => new(ScaleBosses, ScalePathHp, ScalePathDamage);

    public TreasureOdds Treasure => !UseChestChances ? TreasureOdds.Off : ChestOdds switch
    {
        ChestOdds.Generous => TreasureOdds.Generous,
        ChestOdds.Stingy => TreasureOdds.Stingy,
        _ => TreasureOdds.Balanced,
    };

    /// <summary>Gold rate as a multiplier on vanilla gold.</summary>
    public decimal GoldMultiplier => GoldRate switch
    {
        GoldRate.Lean => 0.75m,
        GoldRate.Rich => 1.25m,
        _ => 1m,
    };

    private DifficultyOptions Normalized => UseChestChances ? this : this with { ChestOdds = Off.ChestOdds };

    public string ToJson() => JsonSerializer.Serialize(this, Json);

    /// <summary>Reads a snapshot, treating a missing or unreadable one as Off so a run without saved settings stays vanilla.</summary>
    public static DifficultyOptions FromJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return Off;

        try
        {
            var options = JsonSerializer.Deserialize<DifficultyOptions>(json, Json);
            return options == null ? Off : options with { CardChoices = Math.Clamp(options.CardChoices, MinCardChoices, MaxCardChoices) };
        }
        catch (JsonException)
        {
            return Off;
        }
    }
}
