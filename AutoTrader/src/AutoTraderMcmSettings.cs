using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v2;
using MCM.Abstractions.Base.Global;

namespace AutoTrader
{
    /// <summary>
    /// Mod Configuration Menu (MCM) settings page for the speed-aware mount trading feature.
    /// MCM auto-discovers this attribute-based settings class and renders a standard settings
    /// screen; <see cref="Apply"/> bridges the values into the existing <see cref="AutoTraderConfig"/>
    /// that the trading logic reads. Only the mount settings are exposed here for now.
    /// </summary>
    public sealed class AutoTraderMcmSettings : AttributeGlobalSettings<AutoTraderMcmSettings>
    {
        public override string Id => "AutoTraderSpeed_v1";

        public override string DisplayName => "AutoTrader Speed (Dev)";

        public override string FolderName => "AutoTraderSpeed";

        public override string FormatType => "json2";

        private const string MountsGroup = "Speed-Aware Mounts";

        [SettingPropertyBool("Enable speed-aware mount trading", RequireRestart = false,
            HintText = "Trade mounts so party speed stays optimal without over-buying animals.")]
        [SettingPropertyGroup(MountsGroup)]
        public bool SpeedAwareMounts { get; set; } = true;

        [SettingPropertyInteger("Herd threshold (% of party size)", 80, 150, "0",
            RequireRestart = false,
            HintText = "Animals above this percent of the party size cause a speed penalty.")]
        [SettingPropertyGroup(MountsGroup)]
        public int HerdThresholdPercent { get; set; } = 105;

        [SettingPropertyBool("Reserve mounts for troop upgrades", RequireRestart = false,
            HintText = "Keep war/noble mounts that pending troop upgrades need instead of selling them.")]
        [SettingPropertyGroup(MountsGroup)]
        public bool ReserveUpgradeMounts { get; set; } = true;

        [SettingPropertyBool("Sell surplus noble mounts", RequireRestart = false,
            HintText = "Allow selling surplus noble mounts (off: noble mounts are the most valuable).")]
        [SettingPropertyGroup(MountsGroup)]
        public bool SellNobleMounts { get; set; } = false;

        /// <summary>
        /// Copies the MCM values into <see cref="AutoTraderConfig"/> when MCM is available.
        /// Safe no-op if MCM has not registered the settings (Instance is null).
        /// </summary>
        public static void Apply()
        {
            AutoTraderMcmSettings settings = Instance;
            if (settings == null)
            {
                return;
            }

            AutoTraderConfig.SpeedAwareMountsValue = settings.SpeedAwareMounts;
            AutoTraderConfig.HerdThresholdPercentValue = settings.HerdThresholdPercent;
            AutoTraderConfig.ReserveUpgradeMountsValue = settings.ReserveUpgradeMounts;
            AutoTraderConfig.SellNobleMountsValue = settings.SellNobleMounts;
        }
    }
}
