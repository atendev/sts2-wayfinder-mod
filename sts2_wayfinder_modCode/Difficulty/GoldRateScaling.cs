namespace sts2_wayfinder_mod.sts2_wayfinder_modCode.Difficulty;

internal static class GoldRateScaling
{
    /// <summary>Scales one gold gain, rounding per call so the paid amount stays whole. Zero stays zero, so Ectoplasm still blocks gold.</summary>
    public static decimal Apply(decimal amount, decimal multiplier) =>
        multiplier == 1m || amount <= 0m ? amount : Math.Round(amount * multiplier, MidpointRounding.AwayFromZero);
}
