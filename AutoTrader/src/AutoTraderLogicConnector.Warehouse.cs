using AutoTrader.Trading;
using AutoTrader.Warsails;
using Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Inventory;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace AutoTrader
{
    // Part of AutoTraderLogicConnector: Warehouse.
    partial class AutoTraderLogicConnector
    {
        /// <summary>True while trading in a town owned by the player clan.</summary>
        public bool IsInOwnedTown()
        {
            Settlement settlement = Settlement.CurrentSettlement;
            return settlement != null && settlement.IsTown && settlement.OwnerClan == Clan.PlayerClan;
        }

        /// <summary>Items currently held in this settlement's warehouse (the vanilla stash).</summary>
        public int GetStashItemCount()
        {
            Settlement settlement = Settlement.CurrentSettlement;
            if (settlement == null || settlement.Stash == null)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < settlement.Stash.Count; i++)
            {
                count += settlement.Stash[i].Amount;
            }
            return count;
        }

        /// <summary>
        /// Moves the whole remaining stack of the named item from the party into this town's
        /// warehouse (the vanilla stash) and returns how many units were stored.
        /// </summary>
        public int DepositItemToStash(string itemName)
        {
            Settlement settlement = Settlement.CurrentSettlement;
            if (settlement == null || settlement.Stash == null)
            {
                return 0;
            }

            ItemRoster partyRoster = PartyBase.MainParty.ItemRoster;
            for (int i = 0; i < partyRoster.Count; i++)
            {
                ItemRosterElement element = partyRoster[i];
                ItemObject item = element.EquipmentElement.Item;
                if (item == null || item.Name.ToString() != itemName)
                {
                    continue;
                }

                int amount = element.Amount;
                if (amount <= 0)
                {
                    return 0;
                }

                settlement.Stash.AddToCounts(element.EquipmentElement, amount);
                partyRoster.AddToCounts(element.EquipmentElement, -amount);
                AutoTraderHelpers.PrintDebugMessage(" - [warehouse] stored " + amount + "x " + itemName);
                return amount;
            }
            return 0;
        }

    }
}
