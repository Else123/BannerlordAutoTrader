namespace AutoTrader
{
    /// <summary>
    /// The effective configuration the trading logic reads.
    ///
    /// These are plain in-memory values: MCM owns the settings and
    /// <see cref="AutoTraderMcmSettings.Apply"/> writes every field here, at session start and
    /// again before each trade run. The XML file this class used to read and write is gone -
    /// nothing had saved user changes to it since MCM took over, so it only survived as a third
    /// place where every new setting had to be declared, and forgetting one failed silently.
    /// </summary>
    public static class AutoTraderConfig
    {
        // This fork is built and tested against v1.4.7 (War Sails).
        public static string AutoTraderGameVersion { get; } = "v1.4.7";

        public static int BuyThresholdValue { get; set; } = 90;
        public static int SellThresholdValue { get; set; } = 100;
        public static bool SimpleTradingAI { get; set; } = true;
        public static bool UseWeightedValue { get; set; } = false;
        public static int MaxCapacityValue { get; set; } = 15;
        // Days of marching the party must always have food for. Replaces the old fixed item
        // minimums, which starved a large party and overstocked a small one.
        public static int KeepFoodDaysValue { get; set; } = 14;
        public static int KeepGrainsMinValue { get; set; } = 10;
        public static int KeepGrainsMaxValue { get; set; } = 100;
        public static int KeepConsumablesMinValue { get; set; } = 4;
        public static int KeepConsumablesMaxValue { get; set; } = 20;
        public static int UseInventorySpaceValue { get; set; } = 90;
        public static bool UseMaxFleetCapacityValue { get; set; } = false;
        public static int KeepWagesValue { get; set; } = 3;
        public static int SearchRadiusValue { get; set; } = 300;
        public static int WeaponsArmorTierValue { get; set; } = 2;
        // Derive the equipment tier limit from what the heroes wear instead of the fixed number,
        // so it keeps up with the campaign on its own.
        public static bool MatchEquipmentToHeroesValue { get; set; } = true;

        public static bool SellSmithingValue { get; set; } = false;
        public static bool KeepSmeltingValue { get; set; } = false;
        // Buy cheap smeltable weapons as a hardwood source for smithing (coupled to need).
        public static bool BuySmeltablesForHardwoodValue { get; set; } = true;
        // Floor for the hardwood stock; the effective target scales above it with the ore and
        // ingots carried, because those are what burn charcoal.
        public static int SmeltHardwoodTargetValue { get; set; } = 100;
        public static int HardwoodPerMaterialPercentValue { get; set; } = 50;
        // Ceiling for the scaled target, so a huge stockpile cannot ask for more wood than the
        // party can carry. 0 removes the ceiling.
        public static int HardwoodTargetMaxValue { get; set; } = 2000;
        public static bool ResupplyHardwoodValue { get; set; } = true;
        public static bool ResupplyValue { get; set; } = true;
        public static bool JunkCattleValue { get; set; } = false;

        public static bool BuyHorsesValue { get; set; } = true;
        // Legacy toggle: allow selling horses at all. Only relevant when speed-aware mount trading
        // is off - with it on, the per-category mount plan governs selling instead.
        public static bool SellHorsesValue { get; set; } = false;
        // Speed-aware mode: never sell pack animals, not even as herd surplus.
        public static bool ProtectPackAnimalsValue { get; set; } = false;

        // Speed-aware mount trading: trade mounts so party speed stays optimal.
        public static bool SpeedAwareMountsValue { get; set; } = true;
        // Keep war/noble mounts needed for pending troop upgrades instead of selling them.
        public static bool ReserveUpgradeMountsValue { get; set; } = true;
        // Allow selling surplus noble mounts (off by default: noble mounts are the most valuable).
        public static bool SellNobleMountsValue { get; set; } = false;
        // Sell pack animals above the herd allowance (they slow the party down).
        public static bool ManagePackAnimalHerdValue { get; set; } = true;
        // Never sell a mount priced at or above this value (protects unique/named mounts whose
        // item category is not war_horse/noble_horse). 0 disables the guard.
        public static int KeepMountsAboveValueValue { get; set; } = 2000;
        // Sell livestock (cattle/sheep) above the herd allowance; they slow the party down and,
        // unlike pack animals, provide no cargo capacity.
        public static bool ManageLivestockHerdValue { get; set; } = true;
        // Livestock to keep regardless of the herd (food reserve).
        public static int KeepLivestockReserveValue { get; set; } = 5;
        public static bool BuyWeaponsValue { get; set; } = false;
        public static bool SellWeaponsValue { get; set; } = true;
        public static bool BuyArmorValue { get; set; } = false;
        public static bool SellArmorValue { get; set; } = true;
        public static bool BuyGoodsValue { get; set; } = true;
        public static bool SellGoodsValue { get; set; } = true;
        public static bool BuyConsumablesValue { get; set; } = true;
        public static bool SellConsumablesValue { get; set; } = true;
        public static bool BuyLivestockValue { get; set; } = false;
        public static bool SellLivestockValue { get; set; } = true;

        // Town warehouse (the vanilla settlement stash).
        public const int WarehouseOff = 0;
        public const int WarehouseDeposit = 1;
        public const int WarehouseConsign = 2;
        public const int WarehouseCaravans = 3;
        // The cut a player-owned caravan takes for hauling warehouse goods away.
        public static int CaravanCommissionPercentValue { get; set; } = 15;
        // Test-build default: the whole warehouse chain is on, see AutoTraderMcmSettings.
        public static int WarehouseModeValue { get; set; } = WarehouseCaravans;
        // Share of the town's gold that may be spent on the warehouse per day.
        public static int ConsignmentSharePercentValue { get; set; } = 25;
        // Items below this unit price stay in the warehouse instead of flooding the market.
        public static int ConsignmentMinPriceValue { get; set; } = 0;

        // On by default while this fork is in testing: the decision log is what makes in-game
        // behaviour diagnosable afterwards.
        public static bool DebugMode { get; set; } = true;

    }
}
