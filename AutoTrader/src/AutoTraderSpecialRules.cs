
namespace AutoTrader
{
    public static class AutoTraderSpecialRules
    {
        // Capacity
        public static bool CheckBuyMaxCapacityRule(ILogicConnector logicConnector, int baseCapacity, int amount)
        {
            // Checks if we have the item too often
            var rosterWeight = logicConnector.GetItemWeight() * amount;
            AutoTraderHelpers.PrintDebugMessage("- weight in roster: " + rosterWeight.ToString());
            if (rosterWeight >= (float)baseCapacity * ((float)AutoTraderConfig.MaxCapacityValue / 100f))
            {
                return false;
            }
            return true;
        }

        // Cattle
        public static bool CheckBuyCattleCondition(ILogicConnector logicConnector)
        {
            return AutoTraderConfig.BuyLivestockValue && logicConnector.IsLivestock();
        }

        public static bool CheckBuyCattleRule(ILogicConnector logicConnector)
        {
            return logicConnector.GetNumPartyMembers() >= logicConnector.GetNumLivestockAnimals();
        }

        // Hardwood stock, governed by one target (SmeltHardwoodTargetValue) for both supply routes.
        // Note both rules compare the PARTY's hardwood, not the amount on offer.

        /// <summary>
        /// The hardwood the party should hold: the configured floor, raised in proportion to the
        /// ore and ingots it carries, since those are what burn charcoal.
        /// </summary>
        public static int EffectiveHardwoodTarget(ILogicConnector logicConnector)
        {
            return Smithing.HardwoodTarget.Effective(
                AutoTraderConfig.SmeltHardwoodTargetValue,
                logicConnector.GetRefinableMaterialCount(),
                AutoTraderConfig.HardwoodPerMaterialPercentValue);
        }

        /// <summary>Buy hardwood directly while below the target ("Buy hardwood" / "Both" modes).</summary>
        public static bool ShouldBuyHardwood(ILogicConnector logicConnector)
        {
            if (!AutoTraderConfig.ResupplyHardwoodValue || !logicConnector.IsItemHardwood())
                return false;
            return logicConnector.GetHardwoodCount() < EffectiveHardwoodTarget(logicConnector);
        }

        /// <summary>Keep hardwood while any supply mode is active and the target is not reached.</summary>
        public static bool ShouldKeepHardwood(ILogicConnector logicConnector)
        {
            if (!logicConnector.IsItemHardwood())
                return false;
            if (!AutoTraderConfig.ResupplyHardwoodValue && !AutoTraderConfig.BuySmeltablesForHardwoodValue)
                return false;
            return logicConnector.GetHardwoodCount() < EffectiveHardwoodTarget(logicConnector);
        }

        /// <summary>
        /// Whether the party is short of food. Measured in days of marching, which scales with
        /// party size - a fixed item count starves a large party and overstocks a small one.
        /// </summary>
        public static bool NeedsMoreFood(ILogicConnector logicConnector)
        {
            return logicConnector.GetFoodDaysRemaining() < AutoTraderConfig.KeepFoodDaysValue;
        }

        /// <summary>Food may only be sold once the reserve is comfortably covered.</summary>
        public static bool MaySellFood(ILogicConnector logicConnector)
        {
            return logicConnector.GetFoodDaysRemaining() > AutoTraderConfig.KeepFoodDaysValue;
        }

    }
}
