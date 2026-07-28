using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
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
            CampaignEvents.AfterSettlementEntered.AddNonSerializedListener(this, OnSettlementEntered);
        }

        // The warehouse lives in the vanilla stash, which the campaign already saves.
        public override void SyncData(IDataStore dataStore)
        {
        }

        private void OnDailyTickSettlement(Settlement settlement)
        {
            if (AutoTraderConfig.WarehouseModeValue < AutoTraderConfig.WarehouseConsign)
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

        /// <summary>
        /// A caravan of the player's own arriving at a warehouse town loads what it can pay for
        /// and carry, and takes it to the towns it visits anyway.
        ///
        /// The caravan buys the goods outright, from its own trade gold and minus a commission,
        /// rather than the player being paid later when it resells. That keeps the caravan's own
        /// route and trading untouched (no per-caravan bookkeeping that vanilla could invalidate),
        /// and its trade gold is a natural throttle. Its resale profit reaches the player through
        /// the usual caravan income anyway.
        /// </summary>
        private void OnSettlementEntered(MobileParty party, Settlement settlement, Hero hero)
        {
            if (AutoTraderConfig.WarehouseModeValue < AutoTraderConfig.WarehouseCaravans)
            {
                return;
            }
            if (party == null || !party.IsCaravan)
            {
                return;
            }
            CaravanPartyComponent caravan = party.CaravanPartyComponent;
            if (caravan == null || caravan.Owner != Hero.MainHero)
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

            LoadCaravan(party, settlement);
        }

        private void LoadCaravan(MobileParty party, Settlement settlement)
        {
            Town town = settlement.Town;
            int gold = party.PartyTradeGold;
            float freeCapacity = party.InventoryCapacity - party.TotalWeightCarried;
            if (gold <= 0 || freeCapacity <= 0f)
            {
                return;
            }

            int commission = AutoTraderConfig.CaravanCommissionPercentValue;
            int minPrice = AutoTraderConfig.ConsignmentMinPriceValue;
            int loadedUnits = 0;
            int paid = 0;

            for (int i = settlement.Stash.Count - 1; i >= 0 && gold > 0 && freeCapacity > 0f; i--)
            {
                ItemRosterElement element = settlement.Stash[i];
                ItemObject item = element.EquipmentElement.Item;
                if (item == null || element.Amount <= 0)
                {
                    continue;
                }

                float unitWeight = item.Weight;
                int remaining = element.Amount;
                while (remaining > 0 && gold > 0 && unitWeight <= freeCapacity)
                {
                    int marketPrice = town.GetItemPrice(element.EquipmentElement, party, true);
                    if (marketPrice <= 0 || marketPrice < minPrice)
                    {
                        break;
                    }

                    int payout = marketPrice * (100 - commission) / 100;
                    if (payout <= 0 || payout > gold)
                    {
                        break;
                    }

                    settlement.Stash.AddToCounts(element.EquipmentElement, -1);
                    party.ItemRoster.AddToCounts(element.EquipmentElement, 1);
                    GiveGoldAction.ApplyForPartyToCharacter(party.Party, Hero.MainHero, payout, true);

                    gold -= payout;
                    freeCapacity -= unitWeight;
                    paid += payout;
                    remaining--;
                    loadedUnits++;
                }
            }

            if (loadedUnits > 0)
            {
                AutoTraderHelpers.PrintMessage(
                    $"Warehouse ({settlement.Name}): {party.Name} loaded {loadedUnits} items for {paid} gold, "
                    + $"{CountStash(settlement)} left.");
            }
        }

        private static int CountStash(Settlement settlement)
        {
            int count = 0;
            for (int i = 0; i < settlement.Stash.Count; i++)
            {
                count += settlement.Stash[i].Amount;
            }
            return count;
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
                AutoTraderHelpers.PrintMessage($"Warehouse ({settlement.Name}): consigned {soldUnits} items for {earned} gold, "
                    + $"{CountStash(settlement)} left.");
            }
        }
    }
}
