using BaseLib.Config;
using sts2_wayfinder_mod.sts2_wayfinder_modCode.Difficulty;

namespace sts2_wayfinder_mod.sts2_wayfinder_modCode;

internal class WayfinderConfig : SimpleModConfig
{
    private static bool _applyingPreset;
    private static DifficultyPreset _shownPreset;

    [ConfigHoverTip]
    public static bool EnableWayfinder { get; set; } = true;

    [ConfigHoverTip]
    public static DifficultyPreset Preset { get; set; } = DifficultyPreset.PowerCurve;

    [ConfigSection("EnemyScaling")]
    [ConfigHoverTip]
    public static bool ScaleBosses { get; set; } = DifficultyOptions.PowerCurve.ScaleBosses;

    [ConfigHoverTip]
    public static bool ScalePathHp { get; set; } = DifficultyOptions.PowerCurve.ScalePathHp;

    [ConfigHoverTip]
    public static bool ScalePathDamage { get; set; } = DifficultyOptions.PowerCurve.ScalePathDamage;

    [ConfigSection("CardRewards")]
    [ConfigHoverTip]
    public static bool AlternateCardRewards { get; set; } = DifficultyOptions.PowerCurve.AlternateCardRewards;

    [ConfigHoverTip]
    [ConfigSlider(DifficultyOptions.MinCardChoices, DifficultyOptions.MaxCardChoices, 1)]
    public static int CardChoices { get; set; } = DifficultyOptions.PowerCurve.CardChoices;

    [ConfigSection("TreasureChests")]
    [ConfigHoverTip]
    public static bool UseChestChances { get; set; } = DifficultyOptions.PowerCurve.UseChestChances;

    [ConfigHoverTip]
    [ConfigVisibleIf(nameof(UseChestChances))]
    public static ChestOdds ChestOdds { get; set; } = DifficultyOptions.PowerCurve.ChestOdds;

    [ConfigSection("EliteRelics")]
    [ConfigHoverTip]
    public static bool LowerEliteRelicRarity { get; set; } = DifficultyOptions.PowerCurve.LowerEliteRelicRarity;

    [ConfigSection("Gold")]
    [ConfigHoverTip]
    public static GoldRate GoldRate { get; set; } = DifficultyOptions.PowerCurve.GoldRate;

    public WayfinderConfig()
    {
        // A hand-edited cfg can disagree with its preset. Load fires no event, so reconcile it here.
        if (Preset != DifficultyPreset.Custom && CurrentOptions().Preset != Preset)
            Preset = DifficultyPreset.Custom;

        _shownPreset = Preset;
        ConfigChanged += OnConfigChanged;
    }

    public static DifficultyOptions CurrentOptions() => new(ScaleBosses, ScalePathHp, ScalePathDamage,
        AlternateCardRewards, CardChoices, UseChestChances, ChestOdds, LowerEliteRelicRarity, GoldRate);

    private static void Apply(DifficultyOptions options)
    {
        ScaleBosses = options.ScaleBosses;
        ScalePathHp = options.ScalePathHp;
        ScalePathDamage = options.ScalePathDamage;
        AlternateCardRewards = options.AlternateCardRewards;
        CardChoices = options.CardChoices;
        UseChestChances = options.UseChestChances;
        ChestOdds = options.ChestOdds;
        LowerEliteRelicRarity = options.LowerEliteRelicRarity;
        GoldRate = options.GoldRate;
    }

    /// <summary>Picking a preset sets every option. Moving an option off what the preset sets lands the dropdown on Custom, or on the preset the options now match.</summary>
    private void OnConfigChanged(object? sender, EventArgs args)
    {
        if (_applyingPreset)
            return;

        _applyingPreset = true;
        try
        {
            if (Preset != _shownPreset)
            {
                if (Preset != DifficultyPreset.Custom)
                    Apply(DifficultyOptions.For(Preset));
            }
            else
            {
                Preset = CurrentOptions().Preset;
            }

            _shownPreset = Preset;
            ConfigReloaded();
        }
        finally
        {
            _applyingPreset = false;
        }
    }
}
