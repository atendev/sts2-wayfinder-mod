using BaseLib.Config;

namespace sts2_wayfinder_mod.sts2_wayfinder_modCode;

internal class WayfinderConfig : SimpleModConfig
{
    [ConfigHoverTip]
    public static bool EnableWayfinder { get; set; } = true;
}
