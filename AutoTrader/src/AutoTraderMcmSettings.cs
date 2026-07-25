using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v2;
using MCM.Abstractions.Base.Global;

namespace AutoTrader
{
    /// <summary>
    /// Mod Configuration Menu (MCM) settings page for AutoTrader. MCM auto-discovers this
    /// attribute-based class and renders the settings screen; <see cref="Apply"/> bridges the
    /// values into the static <see cref="AutoTraderConfig"/> that the trading logic reads.
    /// This is the single configuration surface (the old bespoke config screen was removed).
    /// </summary>
    public sealed class AutoTraderMcmSettings : AttributeGlobalSettings<AutoTraderMcmSettings>
    {
        public override string Id => "AutoTraderSpeed_v1";

        public override string DisplayName => "AutoTrader Speed (Dev)";

        public override string FolderName => "AutoTraderSpeed";

        public override string FormatType => "json2";

        private const string GeneralGroup = "General";
        private const string BuySellGroup = "Buy & Sell";
        private const string PricesGroup = "Prices & Tiers";
        private const string KeepGroup = "Keep Amounts";
        private const string SmithingGroup = "Smithing";
        private const string MountsGroup = "Speed-Aware Mounts";

        // --- General ---------------------------------------------------------

        [SettingPropertyBool("Simple trading AI", RequireRestart = false,
            HintText = "Use the simpler trade-rumour-based decision logic.")]
        [SettingPropertyGroup(GeneralGroup)]
        public bool SimpleTradingAI { get; set; } = true;

        [SettingPropertyBool("Use weighted value", RequireRestart = false,
            HintText = "Weight profitability by item value.")]
        [SettingPropertyGroup(GeneralGroup)]
        public bool UseWeightedValue { get; set; } = false;

        [SettingPropertyInteger("Search radius", 0, 1000, "0", RequireRestart = false,
            HintText = "How far to scan other towns for price comparisons.")]
        [SettingPropertyGroup(GeneralGroup)]
        public int SearchRadius { get; set; } = 300;

        [SettingPropertyInteger("Keep wages (days)", 0, 30, "0", RequireRestart = false,
            HintText = "Reserve enough gold to pay this many days of troop wages.")]
        [SettingPropertyGroup(GeneralGroup)]
        public int KeepWages { get; set; } = 3;

        [SettingPropertyInteger("Max capacity (%)", 0, 100, "0", RequireRestart = false,
            HintText = "Stop buying a good once it occupies this percent of capacity.")]
        [SettingPropertyGroup(GeneralGroup)]
        public int MaxCapacity { get; set; } = 15;

        [SettingPropertyInteger("Use inventory space (%)", 0, 100, "0", RequireRestart = false,
            HintText = "How much of the inventory capacity the trader may fill.")]
        [SettingPropertyGroup(GeneralGroup)]
        public int UseInventorySpace { get; set; } = 90;

        [SettingPropertyBool("Use fleet capacity (War Sails)", RequireRestart = false,
            HintText = "Only count ship cargo capacity for weight limits.")]
        [SettingPropertyGroup(GeneralGroup)]
        public bool UseMaxFleetCapacity { get; set; } = false;

        [SettingPropertyBool("Debug logging", RequireRestart = false,
            HintText = "Write detailed decisions to AutoTrader.log (slows trading down).")]
        [SettingPropertyGroup(GeneralGroup)]
        public bool DebugMode { get; set; } = false;

        // --- Buy & Sell toggles ---------------------------------------------

        [SettingPropertyBool("Buy goods", RequireRestart = false)]
        [SettingPropertyGroup(BuySellGroup)]
        public bool BuyGoods { get; set; } = true;

        [SettingPropertyBool("Sell goods", RequireRestart = false)]
        [SettingPropertyGroup(BuySellGroup)]
        public bool SellGoods { get; set; } = true;

        [SettingPropertyBool("Buy consumables", RequireRestart = false)]
        [SettingPropertyGroup(BuySellGroup)]
        public bool BuyConsumables { get; set; } = true;

        [SettingPropertyBool("Sell consumables", RequireRestart = false)]
        [SettingPropertyGroup(BuySellGroup)]
        public bool SellConsumables { get; set; } = true;

        [SettingPropertyBool("Buy weapons", RequireRestart = false)]
        [SettingPropertyGroup(BuySellGroup)]
        public bool BuyWeapons { get; set; } = false;

        [SettingPropertyBool("Sell weapons", RequireRestart = false)]
        [SettingPropertyGroup(BuySellGroup)]
        public bool SellWeapons { get; set; } = true;

        [SettingPropertyBool("Buy armor", RequireRestart = false)]
        [SettingPropertyGroup(BuySellGroup)]
        public bool BuyArmor { get; set; } = false;

        [SettingPropertyBool("Sell armor", RequireRestart = false)]
        [SettingPropertyGroup(BuySellGroup)]
        public bool SellArmor { get; set; } = true;

        [SettingPropertyBool("Buy livestock", RequireRestart = false)]
        [SettingPropertyGroup(BuySellGroup)]
        public bool BuyLivestock { get; set; } = false;

        [SettingPropertyBool("Sell livestock", RequireRestart = false)]
        [SettingPropertyGroup(BuySellGroup)]
        public bool SellLivestock { get; set; } = true;

        [SettingPropertyBool("Buy pack animals", RequireRestart = false,
            HintText = "Buy pack animals (mules/sumpters) for carry capacity.")]
        [SettingPropertyGroup(BuySellGroup)]
        public bool BuyHorses { get; set; } = true;

        [SettingPropertyBool("Protect pack animals", RequireRestart = false,
            HintText = "Do not sell pack animals.")]
        [SettingPropertyGroup(BuySellGroup)]
        public bool SellHorses { get; set; } = false;

        // --- Prices & Tiers -------------------------------------------------

        [SettingPropertyInteger("Buy threshold (%)", 0, 200, "0", RequireRestart = false,
            HintText = "Buy when the price is at or below this percent of the average.")]
        [SettingPropertyGroup(PricesGroup)]
        public int BuyThreshold { get; set; } = 90;

        [SettingPropertyInteger("Sell threshold (%)", 0, 300, "0", RequireRestart = false,
            HintText = "Sell when the price is at or above this percent of the average.")]
        [SettingPropertyGroup(PricesGroup)]
        public int SellThreshold { get; set; } = 100;

        [SettingPropertyInteger("Weapons/armor tier", 1, 6, "0", RequireRestart = false,
            HintText = "Sell weapons/armor up to this tier.")]
        [SettingPropertyGroup(PricesGroup)]
        public int WeaponsArmorTier { get; set; } = 2;

        // --- Keep Amounts ----------------------------------------------------

        [SettingPropertyInteger("Keep grains min", 0, 500, "0", RequireRestart = false)]
        [SettingPropertyGroup(KeepGroup)]
        public int KeepGrainsMin { get; set; } = 10;

        [SettingPropertyInteger("Keep grains max", 0, 500, "0", RequireRestart = false)]
        [SettingPropertyGroup(KeepGroup)]
        public int KeepGrainsMax { get; set; } = 100;

        [SettingPropertyInteger("Keep consumables min", 0, 100, "0", RequireRestart = false)]
        [SettingPropertyGroup(KeepGroup)]
        public int KeepConsumablesMin { get; set; } = 4;

        [SettingPropertyInteger("Keep consumables max", 0, 200, "0", RequireRestart = false)]
        [SettingPropertyGroup(KeepGroup)]
        public int KeepConsumablesMax { get; set; } = 20;

        [SettingPropertyBool("Resupply consumables", RequireRestart = false,
            HintText = "Rebuy food/consumables down to the minimum.")]
        [SettingPropertyGroup(KeepGroup)]
        public bool Resupply { get; set; } = true;

        [SettingPropertyBool("Resupply hardwood", RequireRestart = false,
            HintText = "Keep hardwood stocked for smithing.")]
        [SettingPropertyGroup(KeepGroup)]
        public bool ResupplyHardwood { get; set; } = false;

        [SettingPropertyBool("Junk cattle", RequireRestart = false,
            HintText = "Treat cattle as junk and sell them.")]
        [SettingPropertyGroup(KeepGroup)]
        public bool JunkCattle { get; set; } = false;

        // --- Smithing --------------------------------------------------------

        [SettingPropertyBool("Sell smithing materials", RequireRestart = false)]
        [SettingPropertyGroup(SmithingGroup)]
        public bool SellSmithing { get; set; } = false;

        [SettingPropertyBool("Keep crafted weapons (for smelting)", RequireRestart = false,
            HintText = "Do not sell player-crafted weapons.")]
        [SettingPropertyGroup(SmithingGroup)]
        public bool KeepSmelting { get; set; } = false;

        // --- Speed-Aware Mounts ---------------------------------------------

        [SettingPropertyBool("Enable speed-aware mount trading", RequireRestart = false,
            HintText = "Trade mounts so party speed stays optimal without over-buying animals.")]
        [SettingPropertyGroup(MountsGroup)]
        public bool SpeedAwareMounts { get; set; } = true;

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
            AutoTraderMcmSettings s = Instance;
            if (s == null)
            {
                return;
            }

            AutoTraderConfig.SimpleTradingAI = s.SimpleTradingAI;
            AutoTraderConfig.UseWeightedValue = s.UseWeightedValue;
            AutoTraderConfig.SearchRadiusValue = s.SearchRadius;
            AutoTraderConfig.KeepWagesValue = s.KeepWages;
            AutoTraderConfig.MaxCapacityValue = s.MaxCapacity;
            AutoTraderConfig.UseInventorySpaceValue = s.UseInventorySpace;
            AutoTraderConfig.UseMaxFleetCapacityValue = s.UseMaxFleetCapacity;
            AutoTraderConfig.DebugMode = s.DebugMode;

            AutoTraderConfig.BuyGoodsValue = s.BuyGoods;
            AutoTraderConfig.SellGoodsValue = s.SellGoods;
            AutoTraderConfig.BuyConsumablesValue = s.BuyConsumables;
            AutoTraderConfig.SellConsumablesValue = s.SellConsumables;
            AutoTraderConfig.BuyWeaponsValue = s.BuyWeapons;
            AutoTraderConfig.SellWeaponsValue = s.SellWeapons;
            AutoTraderConfig.BuyArmorValue = s.BuyArmor;
            AutoTraderConfig.SellArmorValue = s.SellArmor;
            AutoTraderConfig.BuyLivestockValue = s.BuyLivestock;
            AutoTraderConfig.SellLivestockValue = s.SellLivestock;
            AutoTraderConfig.BuyHorsesValue = s.BuyHorses;
            AutoTraderConfig.SellHorsesValue = s.SellHorses;

            AutoTraderConfig.BuyThresholdValue = s.BuyThreshold;
            AutoTraderConfig.SellThresholdValue = s.SellThreshold;
            AutoTraderConfig.WeaponsArmorTierValue = s.WeaponsArmorTier;

            AutoTraderConfig.KeepGrainsMinValue = s.KeepGrainsMin;
            AutoTraderConfig.KeepGrainsMaxValue = s.KeepGrainsMax;
            AutoTraderConfig.KeepConsumablesMinValue = s.KeepConsumablesMin;
            AutoTraderConfig.KeepConsumablesMaxValue = s.KeepConsumablesMax;
            AutoTraderConfig.ResupplyValue = s.Resupply;
            AutoTraderConfig.ResupplyHardwoodValue = s.ResupplyHardwood;
            AutoTraderConfig.JunkCattleValue = s.JunkCattle;

            AutoTraderConfig.SellSmithingValue = s.SellSmithing;
            AutoTraderConfig.KeepSmeltingValue = s.KeepSmelting;

            AutoTraderConfig.SpeedAwareMountsValue = s.SpeedAwareMounts;
            AutoTraderConfig.ReserveUpgradeMountsValue = s.ReserveUpgradeMounts;
            AutoTraderConfig.SellNobleMountsValue = s.SellNobleMounts;
        }
    }
}
