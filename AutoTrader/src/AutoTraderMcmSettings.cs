using System;
using System.Collections.Generic;
using MCM.Abstractions;
using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v2;
using MCM.Abstractions.Base;
using MCM.Abstractions.Base.Global;
using MCM.Common;

namespace AutoTrader
{
    /// <summary>
    /// Mod Configuration Menu (MCM) settings page for AutoTrader - the single configuration
    /// surface. This is the user-facing model; <see cref="AutoTraderConfig"/> stays the internal
    /// effective configuration that the trading logic reads, and <see cref="Apply"/> translates
    /// between them.
    ///
    /// Design rules for this page:
    ///  - One decision has exactly one owner. Where several flags used to govern the same thing
    ///    (livestock, hardwood supply, pack animals) there is now a single mode selector, so no
    ///    setting can silently cancel another.
    ///  - Mode selectors instead of booleans that quietly disable other settings. Where a value
    ///    only applies in one mode, the hint says so.
    ///  - All animal decisions live in the Animals section, not spread across Buy and Sell.
    /// </summary>
    public sealed class AutoTraderMcmSettings : AttributeGlobalSettings<AutoTraderMcmSettings>
    {
        public override string Id => "AutoTraderSpeed_v2";

        public override string DisplayName => "AutoTrader Speed (Dev)";

        public override string FolderName => "AutoTraderSpeed";

        public override string FormatType => "json2";

        private const string PricingGroup = "1. Pricing";
        private const string BudgetGroup = "2. Budget & Capacity";
        private const string GoodsGroup = "3. Goods & Equipment";
        private const string SuppliesGroup = "4. Supplies";
        private const string AnimalsGroup = "5. Animals";
        private const string WarehouseGroup = "6. Warehouse";
        private const string DiagnosticsGroup = "7. Diagnostics";

        // Mode option lists. Mapping uses SelectedIndex, so the labels can be reworded freely.
        private static readonly string[] PricingModes = { "Trade rumours (smart)", "Fixed thresholds" };
        private static readonly string[] ScanModes = { "Nearby towns only", "All towns (weighted)" };
        private static readonly string[] CargoBases = { "Party inventory", "Fleet cargo only (War Sails)" };
        private static readonly string[] MountModes = { "Off (manual toggles)", "Speed-optimal" };
        private static readonly string[] PackPolicies = { "Keep all", "Sell surplus" };
        private static readonly string[] LivestockPolicies = { "Keep", "Sell surplus", "Sell all (junk)" };
        private static readonly string[] HardwoodSupplies = { "Off", "Buy hardwood", "Buy smeltable weapons", "Both" };
        private static readonly string[] WeaponSellPolicies = { "Never", "Looted only (keep crafted)", "All (including crafted)" };
        private static readonly string[] WarehouseModes = { "Off", "Store what the merchant cannot afford", "Store and sell locally each day" };

        private static Dropdown<string> Choice(string[] values, int index)
        {
            return new Dropdown<string>(values, index);
        }

        // --- 1. Pricing ------------------------------------------------------

        [SettingPropertyDropdown("Pricing mode", Order = 0, RequireRestart = false,
            HintText = "Trade rumours: judge prices from what your party knows. Fixed thresholds: use the two sliders below.")]
        [SettingPropertyGroup(PricingGroup, GroupOrder = 0)]
        public Dropdown<string> PricingMode { get; set; } = Choice(PricingModes, 0);

        [SettingPropertyInteger("Buy threshold (%)", 0, 200, "0", Order = 1, RequireRestart = false,
            HintText = "Only used in 'Fixed thresholds' mode: buy at or below this percent of the average price.")]
        [SettingPropertyGroup(PricingGroup, GroupOrder = 0)]
        public int BuyThreshold { get; set; } = 90;

        [SettingPropertyInteger("Sell threshold (%)", 0, 300, "0", Order = 2, RequireRestart = false,
            HintText = "Only used in 'Fixed thresholds' mode: sell at or above this percent of the average price.")]
        [SettingPropertyGroup(PricingGroup, GroupOrder = 0)]
        public int SellThreshold { get; set; } = 100;

        [SettingPropertyDropdown("Price scan", Order = 3, RequireRestart = false,
            HintText = "Which settlements are compared for prices. 'All towns' ignores the radius below.")]
        [SettingPropertyGroup(PricingGroup, GroupOrder = 0)]
        public Dropdown<string> ScanMode { get; set; } = Choice(ScanModes, 0);

        [SettingPropertyInteger("Search radius", 0, 999, "0", Order = 4, RequireRestart = false,
            HintText = "Only used in 'Nearby towns only' mode.")]
        [SettingPropertyGroup(PricingGroup, GroupOrder = 0)]
        public int SearchRadius { get; set; } = 300;

        // --- 2. Budget & Capacity -------------------------------------------

        [SettingPropertyInteger("Keep wages (days)", 0, 30, "0", Order = 0, RequireRestart = false,
            HintText = "Reserve enough gold to pay this many days of troop wages.")]
        [SettingPropertyGroup(BudgetGroup, GroupOrder = 1)]
        public int KeepWages { get; set; } = 3;

        [SettingPropertyInteger("Max total capacity used (%)", 0, 100, "0", Order = 1, RequireRestart = false,
            HintText = "How much of the cargo capacity the trader may fill in total.")]
        [SettingPropertyGroup(BudgetGroup, GroupOrder = 1)]
        public int MaxTotalCapacity { get; set; } = 90;

        [SettingPropertyInteger("Max share per good (%)", 0, 100, "0", Order = 2, RequireRestart = false,
            HintText = "Stop buying a single good once it occupies this share of the capacity.")]
        [SettingPropertyGroup(BudgetGroup, GroupOrder = 1)]
        public int MaxSharePerGood { get; set; } = 15;

        [SettingPropertyDropdown("Cargo basis", Order = 3, RequireRestart = false,
            HintText = "Which capacity counts for weight limits. Fleet mode requires ships (War Sails).")]
        [SettingPropertyGroup(BudgetGroup, GroupOrder = 1)]
        public Dropdown<string> CargoBasis { get; set; } = Choice(CargoBases, 0);

        // --- 3. Goods & Equipment -------------------------------------------

        [SettingPropertyBool("Buy trade goods", Order = 0, RequireRestart = false)]
        [SettingPropertyGroup(GoodsGroup, GroupOrder = 2)]
        public bool BuyGoods { get; set; } = true;

        [SettingPropertyBool("Sell trade goods", Order = 1, RequireRestart = false)]
        [SettingPropertyGroup(GoodsGroup, GroupOrder = 2)]
        public bool SellGoods { get; set; } = true;

        [SettingPropertyBool("Buy weapons", Order = 2, RequireRestart = false,
            HintText = "Buy weapons for resale. Buying smelt fodder is configured under Supplies.")]
        [SettingPropertyGroup(GoodsGroup, GroupOrder = 2)]
        public bool BuyWeapons { get; set; } = false;

        [SettingPropertyDropdown("Sell weapons", Order = 3, RequireRestart = false,
            HintText = "Owns the whole weapon-selling decision. 'Looted only' protects your crafted weapons; " +
                "'All' is for crafting to sell. Cheap smelt fodder is kept separately while below the hardwood target.")]
        [SettingPropertyGroup(GoodsGroup, GroupOrder = 2)]
        public Dropdown<string> WeaponSellPolicy { get; set; } = Choice(WeaponSellPolicies, 1);

        [SettingPropertyBool("Buy armor", Order = 4, RequireRestart = false)]
        [SettingPropertyGroup(GoodsGroup, GroupOrder = 2)]
        public bool BuyArmor { get; set; } = false;

        [SettingPropertyBool("Sell armor", Order = 5, RequireRestart = false)]
        [SettingPropertyGroup(GoodsGroup, GroupOrder = 2)]
        public bool SellArmor { get; set; } = true;

        [SettingPropertyInteger("Sell equipment up to tier", 1, 6, "0", Order = 6, RequireRestart = false,
            HintText = "Weapons and armor above this tier are kept.")]
        [SettingPropertyGroup(GoodsGroup, GroupOrder = 2)]
        public int SellUpToTier { get; set; } = 2;

        // --- 4. Supplies -----------------------------------------------------

        [SettingPropertyBool("Buy food", Order = 0, RequireRestart = false)]
        [SettingPropertyGroup(SuppliesGroup, GroupOrder = 3)]
        public bool BuyConsumables { get; set; } = true;

        [SettingPropertyBool("Sell surplus food", Order = 1, RequireRestart = false,
            HintText = "Sell food above the maximum amounts below.")]
        [SettingPropertyGroup(SuppliesGroup, GroupOrder = 3)]
        public bool SellConsumables { get; set; } = true;

        [SettingPropertyBool("Restock food automatically", Order = 2, RequireRestart = false,
            HintText = "Rebuy food up to the minimum amounts below.")]
        [SettingPropertyGroup(SuppliesGroup, GroupOrder = 3)]
        public bool Resupply { get; set; } = true;

        [SettingPropertyInteger("Keep food for (days)", 0, 60, "0", Order = 3, RequireRestart = false,
            HintText = "The food floor: never sell food below this many days of marching, and restock up to it. " +
                "Scales with the party, unlike a fixed item count.")]
        [SettingPropertyGroup(SuppliesGroup, GroupOrder = 3)]
        public int KeepFoodDays { get; set; } = 14;

        [SettingPropertyInteger("Max grain per stack", 0, 500, "0", Order = 4, RequireRestart = false,
            HintText = "Anti-hoarding cap for grain, applied only above the food reserve.")]
        [SettingPropertyGroup(SuppliesGroup, GroupOrder = 3)]
        public int KeepGrainsMax { get; set; } = 100;

        [SettingPropertyInteger("Max other food per stack", 0, 200, "0", Order = 5, RequireRestart = false,
            HintText = "Anti-hoarding cap per other food item, applied only above the food reserve.")]
        [SettingPropertyGroup(SuppliesGroup, GroupOrder = 3)]
        public int KeepConsumablesMax { get; set; } = 20;

        [SettingPropertyDropdown("Hardwood supply", Order = 7, RequireRestart = false,
            HintText = "How to keep hardwood stocked for smithing: buy it directly, buy cheap weapons that smelt into it, or both.")]
        [SettingPropertyGroup(SuppliesGroup, GroupOrder = 3)]
        public Dropdown<string> HardwoodSupply { get; set; } = Choice(HardwoodSupplies, 0);

        [SettingPropertyInteger("Hardwood target", 0, 500, "0", Order = 8, RequireRestart = false,
            HintText = "Keep buying hardwood (or smelt fodder) until the party holds this much.")]
        [SettingPropertyGroup(SuppliesGroup, GroupOrder = 3)]
        public int HardwoodTarget { get; set; } = 100;

        [SettingPropertyBool("Sell smithing materials", Order = 9, RequireRestart = false,
            HintText = "Off keeps ore, ingots, charcoal and hardwood for the forge.")]
        [SettingPropertyGroup(SuppliesGroup, GroupOrder = 3)]
        public bool SellSmithing { get; set; } = false;

        // --- 5. Animals (single owner of every animal decision) --------------

        [SettingPropertyDropdown("Mount management", Order = 0, RequireRestart = false,
            HintText = "Speed-optimal keeps one spare mount per foot soldier and sheds the rest. Off falls back to the manual toggles below.")]
        [SettingPropertyGroup(AnimalsGroup, GroupOrder = 4)]
        public Dropdown<string> MountManagement { get; set; } = Choice(MountModes, 1);

        [SettingPropertyBool("Reserve mounts for troop upgrades", Order = 1, RequireRestart = false,
            HintText = "Keep war/noble mounts that pending troop upgrades need.")]
        [SettingPropertyGroup(AnimalsGroup, GroupOrder = 4)]
        public bool ReserveUpgradeMounts { get; set; } = true;

        [SettingPropertyInteger("Keep mounts worth at least", 0, 20000, "0", Order = 2, RequireRestart = false,
            HintText = "Never sell a mount at or above this price - protects unique and named mounts. 0 disables.")]
        [SettingPropertyGroup(AnimalsGroup, GroupOrder = 4)]
        public int KeepMountsAboveValue { get; set; } = 2000;

        [SettingPropertyBool("Sell surplus noble mounts", Order = 3, RequireRestart = false,
            HintText = "Noble mounts are the rarest upgrade material; off by default.")]
        [SettingPropertyGroup(AnimalsGroup, GroupOrder = 4)]
        public bool SellNobleMounts { get; set; } = false;

        [SettingPropertyBool("Sell horses (manual mode)", Order = 4, RequireRestart = false,
            HintText = "Only used when Mount management is Off.")]
        [SettingPropertyGroup(AnimalsGroup, GroupOrder = 4)]
        public bool SellHorses { get; set; } = false;

        [SettingPropertyBool("Buy pack animals", Order = 5, RequireRestart = false,
            HintText = "Buy mules/sumpters for cargo capacity.")]
        [SettingPropertyGroup(AnimalsGroup, GroupOrder = 4)]
        public bool BuyPackAnimals { get; set; } = true;

        [SettingPropertyDropdown("Pack animals", Order = 6, RequireRestart = false,
            HintText = "Surplus pack animals slow the party down, but each one also carries a lot of cargo - sales stop before the party would be overburdened.")]
        [SettingPropertyGroup(AnimalsGroup, GroupOrder = 4)]
        public Dropdown<string> PackAnimalPolicy { get; set; } = Choice(PackPolicies, 1);

        [SettingPropertyBool("Buy livestock", Order = 7, RequireRestart = false)]
        [SettingPropertyGroup(AnimalsGroup, GroupOrder = 4)]
        public bool BuyLivestock { get; set; } = false;

        [SettingPropertyDropdown("Livestock", Order = 8, RequireRestart = false,
            HintText = "Cattle/sheep add to the herd penalty and carry nothing. 'Sell surplus' keeps the food reserve below.")]
        [SettingPropertyGroup(AnimalsGroup, GroupOrder = 4)]
        public Dropdown<string> LivestockPolicy { get; set; } = Choice(LivestockPolicies, 1);

        [SettingPropertyInteger("Keep livestock (food reserve)", 0, 200, "0", Order = 9, RequireRestart = false)]
        [SettingPropertyGroup(AnimalsGroup, GroupOrder = 4)]
        public int KeepLivestockReserve { get; set; } = 5;

        // --- 6. Warehouse ----------------------------------------------------

        [SettingPropertyDropdown("Warehouse", Order = 0, RequireRestart = false,
            HintText = "In towns your clan owns, store goods the merchant ran out of gold for in the town stash, " +
                "instead of hauling them on. Consignment then sells a slice into that market every day.")]
        [SettingPropertyGroup(WarehouseGroup, GroupOrder = 5)]
        public Dropdown<string> WarehouseMode { get; set; } = Choice(WarehouseModes, 0);

        [SettingPropertyInteger("Daily share of town gold (%)", 0, 100, "0", Order = 1, RequireRestart = false,
            HintText = "How much of the town's gold may go into warehouse sales per day. Lower means slower but gentler on prices.")]
        [SettingPropertyGroup(WarehouseGroup, GroupOrder = 5)]
        public int ConsignmentShare { get; set; } = 25;

        [SettingPropertyInteger("Minimum price to consign", 0, 1000, "0", Order = 2, RequireRestart = false,
            HintText = "Items worth less than this per unit stay in the warehouse.")]
        [SettingPropertyGroup(WarehouseGroup, GroupOrder = 5)]
        public int ConsignmentMinPrice { get; set; } = 0;

        // --- 7. Diagnostics --------------------------------------------------

        [SettingPropertyBool("Debug logging", Order = 0, RequireRestart = false,
            HintText = "On by default while this build is being tested. Writes every decision to AutoTrader.log " +
                "(Configs folder); the previous session is kept as AutoTrader.previous.log. Slows trading down.")]
        [SettingPropertyGroup(DiagnosticsGroup, GroupOrder = 6)]
        public bool DebugMode { get; set; } = true;

        /// <summary>
        /// Translates this page into the effective <see cref="AutoTraderConfig"/> the logic reads.
        /// Safe no-op when MCM has not registered the settings yet.
        /// </summary>
        public static void Apply()
        {
            AutoTraderMcmSettings s = Instance;
            if (s == null)
            {
                return;
            }

            // Pricing: rumour mode is index 0, fixed thresholds index 1.
            AutoTraderConfig.SimpleTradingAI = s.PricingMode.SelectedIndex == 0;
            AutoTraderConfig.BuyThresholdValue = s.BuyThreshold;
            AutoTraderConfig.SellThresholdValue = s.SellThreshold;
            AutoTraderConfig.UseWeightedValue = s.ScanMode.SelectedIndex == 1;
            AutoTraderConfig.SearchRadiusValue = s.SearchRadius;

            // Budget and capacity.
            AutoTraderConfig.KeepWagesValue = s.KeepWages;
            AutoTraderConfig.UseInventorySpaceValue = s.MaxTotalCapacity;
            AutoTraderConfig.MaxCapacityValue = s.MaxSharePerGood;
            AutoTraderConfig.UseMaxFleetCapacityValue = s.CargoBasis.SelectedIndex == 1;

            // Goods and equipment.
            AutoTraderConfig.BuyGoodsValue = s.BuyGoods;
            AutoTraderConfig.SellGoodsValue = s.SellGoods;
            AutoTraderConfig.BuyWeaponsValue = s.BuyWeapons;
            // The weapon policy owns both the sell gate and the crafted-weapon protection, so the
            // two can no longer be set to contradict each other from different sections.
            int weapons = s.WeaponSellPolicy.SelectedIndex;
            AutoTraderConfig.SellWeaponsValue = weapons != 0;
            AutoTraderConfig.KeepSmeltingValue = weapons == 1;
            AutoTraderConfig.BuyArmorValue = s.BuyArmor;
            AutoTraderConfig.SellArmorValue = s.SellArmor;
            AutoTraderConfig.WeaponsArmorTierValue = s.SellUpToTier;

            // Supplies.
            AutoTraderConfig.BuyConsumablesValue = s.BuyConsumables;
            AutoTraderConfig.SellConsumablesValue = s.SellConsumables;
            AutoTraderConfig.ResupplyValue = s.Resupply;
            AutoTraderConfig.KeepFoodDaysValue = s.KeepFoodDays;
            AutoTraderConfig.KeepGrainsMaxValue = s.KeepGrainsMax;
            AutoTraderConfig.KeepConsumablesMaxValue = s.KeepConsumablesMax;

            // One hardwood target, two possible acquisition routes.
            int hardwood = s.HardwoodSupply.SelectedIndex;
            AutoTraderConfig.ResupplyHardwoodValue = hardwood == 1 || hardwood == 3;
            AutoTraderConfig.BuySmeltablesForHardwoodValue = hardwood == 2 || hardwood == 3;
            AutoTraderConfig.SmeltHardwoodTargetValue = s.HardwoodTarget;

            // Animals.
            AutoTraderConfig.SpeedAwareMountsValue = s.MountManagement.SelectedIndex == 1;
            AutoTraderConfig.ReserveUpgradeMountsValue = s.ReserveUpgradeMounts;
            AutoTraderConfig.KeepMountsAboveValueValue = s.KeepMountsAboveValue;
            AutoTraderConfig.SellNobleMountsValue = s.SellNobleMounts;
            AutoTraderConfig.SellHorsesValue = s.SellHorses;
            AutoTraderConfig.BuyHorsesValue = s.BuyPackAnimals;
            AutoTraderConfig.ProtectPackAnimalsValue = s.PackAnimalPolicy.SelectedIndex == 0;
            AutoTraderConfig.ManagePackAnimalHerdValue = s.PackAnimalPolicy.SelectedIndex == 1;
            AutoTraderConfig.BuyLivestockValue = s.BuyLivestock;

            // The livestock policy owns both the sell gate and the herd behaviour, so neither can
            // silently cancel the other any more.
            int livestock = s.LivestockPolicy.SelectedIndex;
            AutoTraderConfig.SellLivestockValue = livestock != 0;
            AutoTraderConfig.ManageLivestockHerdValue = livestock == 1;
            AutoTraderConfig.JunkCattleValue = livestock == 2;
            AutoTraderConfig.KeepLivestockReserveValue = s.KeepLivestockReserve;

            AutoTraderConfig.SellSmithingValue = s.SellSmithing;

            // Warehouse: the mode owns both storing and consigning.
            AutoTraderConfig.WarehouseModeValue = s.WarehouseMode.SelectedIndex;
            AutoTraderConfig.ConsignmentSharePercentValue = s.ConsignmentShare;
            AutoTraderConfig.ConsignmentMinPriceValue = s.ConsignmentMinPrice;

            AutoTraderConfig.DebugMode = s.DebugMode;
        }

        // Built-in presets tuned for common playstyles. The player picks one from the preset
        // dropdown and can fine-tune from there. Presets set only what differs from the defaults.
        public override IEnumerable<ISettingsPreset> GetBuiltInPresets()
        {
            foreach (ISettingsPreset preset in base.GetBuiltInPresets())
            {
                yield return preset;
            }

            // Merchant: maximize margin - buy clearly below and sell clearly above average, and
            // scan far for price differences. Needs the fixed-threshold mode to use the numbers.
            yield return new AutoTraderPreset(Id, "merchant", "Merchant (max margin)", () => new AutoTraderMcmSettings
            {
                PricingMode = Choice(PricingModes, 1),
                BuyThreshold = 80,
                SellThreshold = 120,
                SearchRadius = 900,
                MaxTotalCapacity = 100,
                MaxSharePerGood = 25,
                BuyLivestock = true,
                SellUpToTier = 4
            });

            // Warlord: keep the army fed, funded and fast; do not haul trade goods around.
            yield return new AutoTraderPreset(Id, "warlord", "Warlord (army first)", () => new AutoTraderMcmSettings
            {
                KeepWages = 7,
                MaxTotalCapacity = 60,
                BuyGoods = false,
                // An army on campaign should not run out of food far from a town.
                KeepFoodDays = 25,
                KeepGrainsMax = 150,
                KeepConsumablesMax = 40
            });

            // Blacksmith: feed the forge and sell the crafted output (that is where the profit is).
            yield return new AutoTraderPreset(Id, "blacksmith", "Blacksmith (craft & sell)", () => new AutoTraderMcmSettings
            {
                HardwoodSupply = Choice(HardwoodSupplies, 3),
                HardwoodTarget = 150,
                SellSmithing = false,
                // Crafting for profit: sell the crafted output too.
                WeaponSellPolicy = Choice(WeaponSellPolicies, 2),
                SellUpToTier = 6
            });

            // Minimalist: only sell battle loot, never buy for resale or restock.
            yield return new AutoTraderPreset(Id, "minimalist", "Minimalist (sell loot only)", () => new AutoTraderMcmSettings
            {
                BuyGoods = false,
                BuyConsumables = false,
                Resupply = false,
                BuyPackAnimals = false
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
