using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace AutoTrader.Warehouse
{
    /// <summary>
    /// Sells goods stored in the warehouse of a player-owned town into that town's market, a
    /// slice per day. The warehouse is the vanilla <see cref="Settlement.Stash"/>, so it is
    /// saved with the campaign and the player can inspect it with the normal stash screen -
    /// this behavior needs no persistence of its own.
    ///
    /// The transfer mirrors what SellItemsAction does for a settlement buying from a party:
    /// take the item out of the seller's roster, put it into the town's roster (which raises
    /// the town's inventory-updated hook, so prices react), and have the settlement pay.
    /// </summary>
    public sealed class WarehouseBehavior : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.DailyTickSettlementEvent.AddNonSerializedListener(this, OnDailyTickSettlement);
        }

        // The warehouse lives in the vanilla stash, which the campaign already saves.
        public override void SyncData(IDataStore dataStore)
        {
        }

        private void OnDailyTickSettlement(Settlement settlement)
        {
            if (AutoTraderConfig.WarehouseModeValue != AutoTraderConfig.WarehouseConsign)
            {
                return;
            }
            if (settlement == null || !settlement.IsTown || settlement.Town == null)
            {
                return;
            }
            if (settlement.OwnerClan != Clan.PlayerClan)
            {
                return;
            }
            if (settlement.Stash == null || settlement.Stash.Count == 0)
            {
                return;
            }

            ConsignSlice(settlement);
        }

        private void ConsignSlice(Settlement settlement)
        {
            Town town = settlement.Town;
            ConsignmentPlanner planner = new ConsignmentPlanner(
                AutoTraderConfig.ConsignmentSharePercentValue,
                AutoTraderConfig.ConsignmentMinPriceValue);

            int budget = planner.DailyBudget(town.Gold);
            if (budget <= 0)
            {
                return;
            }

            int soldUnits = 0;
            int earned = 0;

            // Walk the stash backwards so removing a depleted stack cannot skip an entry.
            for (int i = settlement.Stash.Count - 1; i >= 0 && budget > 0; i--)
            {
                ItemRosterElement element = settlement.Stash[i];
                ItemObject item = element.EquipmentElement.Item;
                if (item == null || element.Amount <= 0)
                {
                    continue;
                }

                int remaining = element.Amount;
                while (remaining > 0 && budget > 0)
                {
                    int unitPrice = town.GetItemPrice(element.EquipmentElement, null, true);
                    if (!planner.CanSellUnit(unitPrice, budget))
                    {
                        break;
                    }

                    settlement.Stash.AddToCounts(element.EquipmentElement, -1);
                    settlement.ItemRoster.AddToCounts(element.EquipmentElement, 1);
                    GiveGoldAction.ApplyForSettlementToCharacter(settlement, Hero.MainHero, unitPrice, true);

                    budget -= unitPrice;
                    earned += unitPrice;
                    remaining--;
                    soldUnits++;
                }
            }

            if (soldUnits > 0)
            {
                AutoTraderHelpers.PrintMessage($"Warehouse ({settlement.Name}): consigned {soldUnits} items for {earned} gold.");
            }
        }
    }
}
