
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

        // Horses
        public static bool CheckBuyHorsesRules(ILogicConnector logicConnector, int buyoutPrice, int availablePlayerGold)
        {
            if (logicConnector.IsPackAnimal() && AutoTraderConfig.BuyHorsesValue)
            {
                // Buy pack horses rule
                if (logicConnector.GetNumPartyMembers() > logicConnector.GetNumLivestockAnimals() && buyoutPrice * 2 < availablePlayerGold)
                    return true;

                // TODO: Add max herding setting
            }
            return false;
        }

        // Hardwood stock, governed by one target (SmeltHardwoodTargetValue) for both supply routes.
        // Note both rules compare the PARTY's hardwood, not the amount on offer.

        /// <summary>Buy hardwood directly while below the target ("Buy hardwood" / "Both" modes).</summary>
        public static bool ShouldBuyHardwood(ILogicConnector logicConnector)
        {
            if (!AutoTraderConfig.ResupplyHardwoodValue || !logicConnector.IsItemHardwood())
                return false;
            return logicConnector.GetHardwoodCount() < AutoTraderConfig.SmeltHardwoodTargetValue;
        }

        /// <summary>Keep hardwood while any supply mode is active and the target is not reached.</summary>
        public static bool ShouldKeepHardwood(ILogicConnector logicConnector)
        {
            if (!logicConnector.IsItemHardwood())
                return false;
            if (!AutoTraderConfig.ResupplyHardwoodValue && !AutoTraderConfig.BuySmeltablesForHardwoodValue)
                return false;
            return logicConnector.GetHardwoodCount() < AutoTraderConfig.SmeltHardwoodTargetValue;
        }

        /// <summary>
        /// Resupply
        /// </summary>
        /// <param name="itemId"></param>
        /// <returns></returns>
        public static bool CheckBuyConsumablesRules(ILogicConnector logicConnector, int currentAmount)
        {
            if (AutoTraderConfig.ResupplyValue)
                return CheckBuyResupplyRule(logicConnector, currentAmount);
            return false;
        }

        /// <returns>
        /// True if the given item is below the restock value
        /// False if not
        /// </returns>
        public static bool CheckBuyResupplyRule(ILogicConnector logicConnector, int currentAmount)
        {
            // Find item stack in current inventory

            // Resupply grain
            if (logicConnector.IsItemGrain())
            {
                if (currentAmount < AutoTraderConfig.KeepGrainsMinValue)
                {
                    return true;
                }
                return false;
            }
            else
            {
                if (currentAmount < AutoTraderConfig.KeepConsumablesMinValue)
                {
                    return true;
                }
                return false;
            }
        }

    }
}
