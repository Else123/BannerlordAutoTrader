using System;
using System.Collections.Generic;
using MCM.Abstractions;
using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v2;
using MCM.Abstractions.Base;
using MCM.Abstractions.Base.Global;

namespace AutoTrader
{
    /// <summary>
    /// Mod Configuration Menu (MCM) settings page for AutoTrader. MCM auto-discovers this
    /// attribute-based class and renders the settings screen; <see cref="Apply"/> bridges the
    /// values into the static <see cref="AutoTraderConfig"/> that the trading logic reads.
    /// This is the single configuration surface (the old bespoke config screen was removed).
    ///
    /// GroupOrder controls the order of the sections; Order sorts the settings within a
    /// section (MCM sorts alphabetically otherwise). MCM renders one setting per row - a
    /// multi-column/compact layout is not configurable from here.
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

        // --- General (section 0) --------------------------------------------

        [SettingPropertyBool("Simple trading AI", Order = 0, RequireRestart = false,
            HintText = "Use the simpler trade-rumour-based decision logic.")]
        [SettingPropertyGroup(GeneralGroup, GroupOrder = 0)]
        public bool SimpleTradingAI { get; set; } = true;

        [SettingPropertyBool("Use weighted value", Order = 1, RequireRestart = false,
            HintText = "Weight profitability by item value.")]
        [SettingPropertyGroup(GeneralGroup, GroupOrder = 0)]
        public bool UseWeightedValue { get; set; } = false;

        [SettingPropertyInteger("Search radius", 0, 1000, "0", Order = 2, RequireRestart = false,
            HintText = "How far to scan other towns for price comparisons.")]
        [SettingPropertyGroup(GeneralGroup, GroupOrder = 0)]
        public int SearchRadius { get; set; } = 300;

        [SettingPropertyInteger("Keep wages (days)", 0, 30, "0", Order = 3, RequireRestart = false,
            HintText = "Reserve enough gold to pay this many days of troop wages.")]
        [SettingPropertyGroup(GeneralGroup, GroupOrder = 0)]
        public int KeepWages { get; set; } = 3;

        [SettingPropertyInteger("Max capacity (%)", 0, 100, "0", Order = 4, RequireRestart = false,
            HintText = "Stop buying a good once it occupies this percent of capacity.")]
        [SettingPropertyGroup(GeneralGroup, GroupOrder = 0)]
        public int MaxCapacity { get; set; } = 15;

        [SettingPropertyInteger("Use inventory space (%)", 0, 100, "0", Order = 5, RequireRestart = false,
            HintText = "How much of the inventory capacity the trader may fill.")]
        [SettingPropertyGroup(GeneralGroup, GroupOrder = 0)]
        public int UseInventorySpace { get; set; } = 90;

        [SettingPropertyBool("Use fleet capacity (War Sails)", Order = 6, RequireRestart = false,
            HintText = "Only count ship cargo capacity for weight limits.")]
        [SettingPropertyGroup(GeneralGroup, GroupOrder = 0)]
        public bool UseMaxFleetCapacity { get; set; } = false;

        [SettingPropertyBool("Debug logging", Order = 7, RequireRestart = false,
            HintText = "Write detailed decisions to AutoTrader.log (slows trading down).")]
        [SettingPropertyGroup(GeneralGroup, GroupOrder = 0)]
        public bool DebugMode { get; set; } = false;

        // --- Buy & Sell (section 1), paired buy/sell ------------------------

        [SettingPropertyBool("Buy goods", Order = 0, RequireRestart = false)]
        [SettingPropertyGroup(BuySellGroup, GroupOrder = 1)]
        public bool BuyGoods { get; set; } = true;

        [SettingPropertyBool("Sell goods", Order = 1, RequireRestart = false)]
        [SettingPropertyGroup(BuySellGroup, GroupOrder = 1)]
        public bool SellGoods { get; set; } = true;

        [SettingPropertyBool("Buy consumables", Order = 2, RequireRestart = false)]
        [SettingPropertyGroup(BuySellGroup, GroupOrder = 1)]
        public bool BuyConsumables { get; set; } = true;

        [SettingPropertyBool("Sell consumables", Order = 3, RequireRestart = false)]
        [SettingPropertyGroup(BuySellGroup, GroupOrder = 1)]
        public bool SellConsumables { get; set; } = true;

        [SettingPropertyBool("Buy weapons", Order = 4, RequireRestart = false)]
        [SettingPropertyGroup(BuySellGroup, GroupOrder = 1)]
        public bool BuyWeapons { get; set; } = false;

        [SettingPropertyBool("Sell weapons", Order = 5, RequireRestart = false)]
        [SettingPropertyGroup(BuySellGroup, GroupOrder = 1)]
        public bool SellWeapons { get; set; } = true;

        [SettingPropertyBool("Buy armor", Order = 6, RequireRestart = false)]
        [SettingPropertyGroup(BuySellGroup, GroupOrder = 1)]
        public bool BuyArmor { get; set; } = false;

        [SettingPropertyBool("Sell armor", Order = 7, RequireRestart = false)]
        [SettingPropertyGroup(BuySellGroup, GroupOrder = 1)]
        public bool SellArmor { get; set; } = true;

        [SettingPropertyBool("Buy livestock", Order = 8, RequireRestart = false)]
        [SettingPropertyGroup(BuySellGroup, GroupOrder = 1)]
        public bool BuyLivestock { get; set; } = false;

        [SettingPropertyBool("Sell livestock", Order = 9, RequireRestart = false)]
        [SettingPropertyGroup(BuySellGroup, GroupOrder = 1)]
        public bool SellLivestock { get; set; } = true;

        [SettingPropertyBool("Buy pack animals", Order = 10, RequireRestart = false,
            HintText = "Buy pack animals (mules/sumpters) for carry capacity.")]
        [SettingPropertyGroup(BuySellGroup, GroupOrder = 1)]
        public bool BuyHorses { get; set; } = true;

        [SettingPropertyBool("Sell horses (legacy)", Order = 11, RequireRestart = false,
            HintText = "Allow selling horses when speed-aware mount trading is OFF. With it on, the mount plan decides.")]
        [SettingPropertyGroup(BuySellGroup, GroupOrder = 1)]
        public bool SellHorses { get; set; } = false;

        // --- Prices & Tiers (section 2) -------------------------------------

        [SettingPropertyInteger("Buy threshold (%)", 0, 200, "0", Order = 0, RequireRestart = false,
            HintText = "Buy when the price is at or below this percent of the average.")]
        [SettingPropertyGroup(PricesGroup, GroupOrder = 2)]
        public int BuyThreshold { get; set; } = 90;

        [SettingPropertyInteger("Sell threshold (%)", 0, 300, "0", Order = 1, RequireRestart = false,
            HintText = "Sell when the price is at or above this percent of the average.")]
        [SettingPropertyGroup(PricesGroup, GroupOrder = 2)]
        public int SellThreshold { get; set; } = 100;

        [SettingPropertyInteger("Weapons/armor tier", 1, 6, "0", Order = 2, RequireRestart = false,
            HintText = "Sell weapons/armor up to this tier.")]
        [SettingPropertyGroup(PricesGroup, GroupOrder = 2)]
        public int WeaponsArmorTier { get; set; } = 2;

        // --- Keep Amounts (section 3) ---------------------------------------

        [SettingPropertyInteger("Keep grains min", 0, 500, "0", Order = 0, RequireRestart = false)]
        [SettingPropertyGroup(KeepGroup, GroupOrder = 3)]
        public int KeepGrainsMin { get; set; } = 10;

        [SettingPropertyInteger("Keep grains max", 0, 500, "0", Order = 1, RequireRestart = false)]
        [SettingPropertyGroup(KeepGroup, GroupOrder = 3)]
        public int KeepGrainsMax { get; set; } = 100;

        [SettingPropertyInteger("Keep consumables min", 0, 100, "0", Order = 2, RequireRestart = false)]
        [SettingPropertyGroup(KeepGroup, GroupOrder = 3)]
        public int KeepConsumablesMin { get; set; } = 4;

        [SettingPropertyInteger("Keep consumables max", 0, 200, "0", Order = 3, RequireRestart = false)]
        [SettingPropertyGroup(KeepGroup, GroupOrder = 3)]
        public int KeepConsumablesMax { get; set; } = 20;

        [SettingPropertyBool("Resupply consumables", Order = 4, RequireRestart = false,
            HintText = "Rebuy food/consumables down to the minimum.")]
        [SettingPropertyGroup(KeepGroup, GroupOrder = 3)]
        public bool Resupply { get; set; } = true;

        [SettingPropertyBool("Resupply hardwood", Order = 5, RequireRestart = false,
            HintText = "Keep hardwood stocked for smithing.")]
        [SettingPropertyGroup(KeepGroup, GroupOrder = 3)]
        public bool ResupplyHardwood { get; set; } = false;

        [SettingPropertyBool("Junk cattle", Order = 6, RequireRestart = false,
            HintText = "Treat cattle as junk and sell them.")]
        [SettingPropertyGroup(KeepGroup, GroupOrder = 3)]
        public bool JunkCattle { get; set; } = false;

        // --- Smithing (section 4) -------------------------------------------

        [SettingPropertyBool("Sell smithing materials", Order = 0, RequireRestart = false)]
        [SettingPropertyGroup(SmithingGroup, GroupOrder = 4)]
        public bool SellSmithing { get; set; } = false;

        [SettingPropertyBool("Keep crafted weapons (for smelting)", Order = 1, RequireRestart = false,
            HintText = "Do not sell player-crafted weapons.")]
        [SettingPropertyGroup(SmithingGroup, GroupOrder = 4)]
        public bool KeepSmelting { get; set; } = false;

        [SettingPropertyBool("Buy smeltable weapons for hardwood", Order = 2, RequireRestart = false,
            HintText = "Buy cheap weapons that smelt into hardwood, but only while hardwood is below the target.")]
        [SettingPropertyGroup(SmithingGroup, GroupOrder = 4)]
        public bool BuySmeltablesForHardwood { get; set; } = false;

        [SettingPropertyInteger("Hardwood stock target", 0, 500, "0", Order = 3, RequireRestart = false,
            HintText = "Buy smeltable weapons until party hardwood reaches this amount.")]
        [SettingPropertyGroup(SmithingGroup, GroupOrder = 4)]
        public int SmeltHardwoodTarget { get; set; } = 100;

        // --- Speed-Aware Mounts (section 5) ---------------------------------

        [SettingPropertyBool("Enable speed-aware mount trading", Order = 0, RequireRestart = false,
            HintText = "Trade mounts so party speed stays optimal without over-buying animals.")]
        [SettingPropertyGroup(MountsGroup, GroupOrder = 5)]
        public bool SpeedAwareMounts { get; set; } = true;

        [SettingPropertyBool("Reserve mounts for troop upgrades", Order = 1, RequireRestart = false,
            HintText = "Keep war/noble mounts that pending troop upgrades need instead of selling them.")]
        [SettingPropertyGroup(MountsGroup, GroupOrder = 5)]
        public bool ReserveUpgradeMounts { get; set; } = true;

        [SettingPropertyBool("Sell surplus noble mounts", Order = 2, RequireRestart = false,
            HintText = "Allow selling surplus noble mounts (off: noble mounts are the most valuable).")]
        [SettingPropertyGroup(MountsGroup, GroupOrder = 5)]
        public bool SellNobleMounts { get; set; } = false;

        [SettingPropertyBool("Sell surplus pack animals", Order = 3, RequireRestart = false,
            HintText = "Sell mules/sumpters above the herd allowance - too many animals slow the party down.")]
        [SettingPropertyGroup(MountsGroup, GroupOrder = 5)]
        public bool ManagePackAnimalHerd { get; set; } = true;

        [SettingPropertyInteger("Keep mounts worth at least", 0, 20000, "0", Order = 4, RequireRestart = false,
            HintText = "Never sell a mount priced at or above this value - protects unique/named mounts. 0 disables.")]
        [SettingPropertyGroup(MountsGroup, GroupOrder = 5)]
        public int KeepMountsAboveValue { get; set; } = 2000;

        [SettingPropertyBool("Protect pack animals", Order = 7, RequireRestart = false,
            HintText = "Never sell pack animals (mules/sumpters), not even as herd surplus.")]
        [SettingPropertyGroup(MountsGroup, GroupOrder = 5)]
        public bool ProtectPackAnimals { get; set; } = false;

        [SettingPropertyBool("Sell surplus livestock", Order = 5, RequireRestart = false,
            HintText = "Sell cattle/sheep above the herd allowance - they slow the party down and carry nothing.")]
        [SettingPropertyGroup(MountsGroup, GroupOrder = 5)]
        public bool ManageLivestockHerd { get; set; } = true;

        [SettingPropertyInteger("Keep livestock (food reserve)", 0, 200, "0", Order = 6, RequireRestart = false,
            HintText = "Livestock to keep regardless of the herd penalty.")]
        [SettingPropertyGroup(MountsGroup, GroupOrder = 5)]
        public int KeepLivestockReserve { get; set; } = 5;

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
            AutoTraderConfig.ProtectPackAnimalsValue = s.ProtectPackAnimals;

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
            AutoTraderConfig.BuySmeltablesForHardwoodValue = s.BuySmeltablesForHardwood;
            AutoTraderConfig.SmeltHardwoodTargetValue = s.SmeltHardwoodTarget;

            AutoTraderConfig.SpeedAwareMountsValue = s.SpeedAwareMounts;
            AutoTraderConfig.ReserveUpgradeMountsValue = s.ReserveUpgradeMounts;
            AutoTraderConfig.SellNobleMountsValue = s.SellNobleMounts;
            AutoTraderConfig.ManagePackAnimalHerdValue = s.ManagePackAnimalHerd;
            AutoTraderConfig.KeepMountsAboveValueValue = s.KeepMountsAboveValue;
            AutoTraderConfig.ManageLivestockHerdValue = s.ManageLivestockHerd;
            AutoTraderConfig.KeepLivestockReserveValue = s.KeepLivestockReserve;
        }

        // Built-in presets tuned for common playstyles. The player picks one from the preset
        // dropdown and can fine-tune from there. Only the values that differ from the defaults
        // are set; everything else keeps the default.
        public override IEnumerable<ISettingsPreset> GetBuiltInPresets()
        {
            foreach (ISettingsPreset preset in base.GetBuiltInPresets())
            {
                yield return preset;
            }

            // Merchant: maximize margin - buy clearly below and sell clearly above average,
            // scan far for price differences, and use the hold for cargo.
            yield return new AutoTraderPreset(Id, "merchant", "Merchant (max margin)", () => new AutoTraderMcmSettings
            {
                SearchRadius = 600,
                UseInventorySpace = 100,
                MaxCapacity = 25,
                BuyThreshold = 80,
                SellThreshold = 120,
                BuyLivestock = true,
                WeaponsArmorTier = 4
            });

            // Warlord: keep the army fed, funded and fast; do not haul trade goods.
            yield return new AutoTraderPreset(Id, "warlord", "Warlord", () => new AutoTraderMcmSettings
            {
                KeepWages = 7,
                UseInventorySpace = 60,
                BuyGoods = false,
                KeepGrainsMin = 20,
                KeepGrainsMax = 150,
                KeepConsumablesMin = 10,
                KeepConsumablesMax = 40
            });

            // Blacksmith: feed the forge - collect cheap smelt fodder and keep materials, but do
            // sell the crafted output (crafted weapons are the profit, so KeepSmelting stays off;
            // the cheap fodder is already protected while collecting).
            yield return new AutoTraderPreset(Id, "blacksmith", "Blacksmith (craft & sell)", () => new AutoTraderMcmSettings
            {
                BuySmeltablesForHardwood = true,
                SmeltHardwoodTarget = 150,
                ResupplyHardwood = true,
                SellWeapons = true,
                WeaponsArmorTier = 6
            });

            // Minimalist: only sell battle loot, do not buy for resale or restock.
            yield return new AutoTraderPreset(Id, "minimalist", "Minimalist (sell loot only)", () => new AutoTraderMcmSettings
            {
                BuyGoods = false,
                BuyConsumables = false,
                Resupply = false
            });
        }

        // Lightweight built-in preset backed by a factory that returns a preconfigured instance.
        private sealed class AutoTraderPreset : ISettingsPreset
        {
            private readonly Func<AutoTraderMcmSettings> _factory;

            public AutoTraderPreset(string settingsId, string id, string name, Func<AutoTraderMcmSettings> factory)
            {
                SettingsId = settingsId;
                Id = id;
                Name = name;
                _factory = factory;
            }

            public string SettingsId { get; }

            public string Id { get; }

            public string Name { get; }

            public BaseSettings LoadPreset()
            {
                return _factory();
            }

            public bool SavePreset(BaseSettings settings)
            {
                return false;
            }
        }
    }
}
