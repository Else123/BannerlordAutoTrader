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
    // Part of AutoTraderLogicConnector: Market.
    partial class AutoTraderLogicConnector
    {
        public int GetMerchantItemRosterSize()
        {
            if (_isCaravan)
            {
                AutoTraderHelpers.PrintDebugMessage(" - MerchantItemRosterSize (Caravan): " + MobileParty.ConversationParty.ItemRoster.Count.ToString());
                return MobileParty.ConversationParty.ItemRoster.Count;
            }
            AutoTraderHelpers.PrintDebugMessage(" - MerchantItemRosterSize (Town): " + Settlement.CurrentSettlement.ItemRoster.Count.ToString());
            return Settlement.CurrentSettlement.ItemRoster.Count;
        }

        private MerchantType GetMerchantType()
        {
            MerchantType merchantType;
            if (_isCaravan)
            {
                merchantType = MerchantType.Caravan;

                // Make sure its opened through conversation
                if (MobileParty.ConversationParty == null)
                {
                    AutoTraderHelpers.PrintDebugMessage("Caravan trading but not through a conversation!");
                }
            }
            else
                merchantType = Settlement.CurrentSettlement.IsTown ? MerchantType.Town : MerchantType.Village;
            return merchantType;
        }

        public int GetMerchantGold()
        {
            var result = 0;
            var merchantType = GetMerchantType();
            switch (merchantType)
            {
                case MerchantType.Town:
                    result = Settlement.CurrentSettlement.Town.Gold;
                    break;
                case MerchantType.Village:
                    result = Settlement.CurrentSettlement.Village.Gold;
                    break;
                case MerchantType.Caravan:
                    result = MobileParty.ConversationParty.PartyTradeGold;
                    break;
            }
            AutoTraderHelpers.PrintDebugMessage(" - MerchantGold: " + result.ToString());
            return result;
        }

        public int GetProjectedProfit(int buyoutPrice)
        {
            IPlayerTradeBehavior campaignBehavior = Campaign.Current.GetCampaignBehavior<IPlayerTradeBehavior>();
            var result = campaignBehavior.GetProjectedProfit(_currentItemRosterElement, buyoutPrice);
            AutoTraderHelpers.PrintDebugMessage(" - ProjectedProfit: " + result.ToString());
            return result;
        }

        public int GetItemPrice()
        {
            var result = _inventoryLogic.GetItemPrice(_currentItemRosterElement.EquipmentElement, _isBuying);
            AutoTraderHelpers.PrintDebugMessage(" - ItemPrice: " + result.ToString());
            return result;
        }

        public float GetAveragePriceFallback()
        {
            var result = _currentItemRosterElement.EquipmentElement.Item.Value;
            AutoTraderHelpers.PrintDebugMessage(" - AveragePriceFallback: " + result.ToString());
            return result;
        }

        public int GetCostOfRosterElement()
        {
            var result = _inventoryLogic.GetCostOfItemRosterElement(_currentItemRosterElement, _isBuying ? InventoryLogic.InventorySide.OtherInventory : InventoryLogic.InventorySide.PlayerInventory);
            AutoTraderHelpers.PrintDebugMessage(" - CostOfElement: " + result.ToString());
            return result;
        }

        public float GetAveragePriceFactorItemCategory()
        {
            var result = _inventoryLogic.GetAveragePriceFactorItemCategory(_currentItemRosterElement.EquipmentElement.Item.ItemCategory);
            AutoTraderHelpers.PrintDebugMessage(" - AveragePriceFactorItemCategory: " + result.ToString());
            return result;
        }

        /// Towns
        public int GetTownListSize()
        {
            var result = Town.AllTowns.Count();
            return result;
        }

        private Town GetTownById(int townId)
        {
            return Town.AllTowns.ElementAt(townId);
        }

        public bool IsTownInRange(int townId, out float actualDistance)
        {
            var town = GetTownById(townId);
            float estimatedLandRatio;
            //TODO: take into consideration naval distance
            actualDistance = Campaign.Current.Models.MapDistanceModel.GetDistance(MobileParty.MainParty, town.Settlement, false, MobileParty.NavigationType.Default, out estimatedLandRatio);
            return actualDistance < (float)AutoTraderConfig.SearchRadiusValue;
        }

        public bool IsCurrentTown(int townId)
        {
            if (_isCaravan)
                return false;
            var town = GetTownById(townId);
            var result = Settlement.CurrentSettlement.IsTown && town == Settlement.CurrentSettlement.Town;
            AutoTraderHelpers.PrintDebugMessage(" - IsCurrentTown: " + result.ToString());
            return result;
        }

        public float GetTownItemPrice(int townId, bool isSelling)
        {
            var town = GetTownById(townId);
            var result = town.MarketData.GetPrice(_currentItemRosterElement.EquipmentElement.Item, PartyBase.MainParty.MobileParty, isSelling);
            return result;
        }

        public float GetCurrentTownPriceFactor()
        {
            Town town = Settlement.CurrentSettlement.IsVillage ? Settlement.CurrentSettlement.Village.Bound.Town : Settlement.CurrentSettlement.Town;
            var result = town.MarketData.GetPriceFactor(_currentItemRosterElement.EquipmentElement.Item.ItemCategory);
            AutoTraderHelpers.PrintDebugMessage(" - CurrentTownPriceFactor: " + result.ToString());
            return result;
        }

        /// Villages
        public int GetVillageListSize()
        {
            var result = Village.All.Count();
            return result;
        }

        private Village GetVillageById(int villageId)
        {
            return Village.All.ElementAt(villageId);
        }

        public bool IsVillageInRange(int villageId, out float actualDistance)
        {
            var village = GetVillageById(villageId);
            float estimatedLandRatio;

            // TODO: take into consideration naval distance
            actualDistance = Campaign.Current.Models.MapDistanceModel.GetDistance(MobileParty.MainParty, village.Settlement, false, MobileParty.NavigationType.Default, out estimatedLandRatio);
            return actualDistance < (float)AutoTraderConfig.SearchRadiusValue; ;
        }

        public bool IsCurrentVillage(int townId)
        {
            if (_isCaravan)
                return false;
            var village = GetVillageById(townId);
            var result = Settlement.CurrentSettlement.IsVillage && village == Settlement.CurrentSettlement.Village;
            AutoTraderHelpers.PrintDebugMessage(" - IsCurrentVillage: " + result.ToString());
            return result;
        }

        public float GetVillageItemPrice(int townId, bool isSelling)
        {
            var town = GetVillageById(townId);
            var result = town.MarketData.GetPrice(_currentItemRosterElement.EquipmentElement.Item, PartyBase.MainParty.MobileParty, isSelling, null);
            return result;
        }

    }
}
