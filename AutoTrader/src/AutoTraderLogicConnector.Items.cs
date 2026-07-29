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
    // Part of AutoTraderLogicConnector: Items.
    partial class AutoTraderLogicConnector
    {
        // Current-item category checks (for CanBuy/CanSell), analogous to IsPackAnimal().
        public bool IsWarMount()
        {
            ItemObject item = _currentItemRosterElement.EquipmentElement.Item;
            return item != null && item.HorseComponent != null && item.HorseComponent.IsMount
                && item.ItemCategory == DefaultItemCategories.WarHorse;
        }

        public bool IsNobleMount()
        {
            ItemObject item = _currentItemRosterElement.EquipmentElement.Item;
            return item != null && item.HorseComponent != null && item.HorseComponent.IsMount
                && item.ItemCategory == DefaultItemCategories.NobleHorse;
        }

        public List<string> GetLocks()
        {
            var locksEnumerable = Campaign.Current.GetCampaignBehavior<IViewDataTracker>().GetInventoryLocks();
            if (locksEnumerable != null)
            {
                return locksEnumerable.ToList<string>();
            }
            return new List<string>();
        }

        public bool IsItemLocked()
        {
            var locks = GetLocks();
            var itemStringId = _currentItemRosterElement.EquipmentElement.Item.StringId;
            if (_currentItemRosterElement.EquipmentElement.ItemModifier != null)
            {
                itemStringId += _currentItemRosterElement.EquipmentElement.ItemModifier.StringId;
            }
            AutoTraderHelpers.PrintDebugMessage(" - IsLocked: " + locks.Contains(itemStringId).ToString());
            return locks.Contains(itemStringId);
        }

        public bool IsItemTradeGood()
        {
            AutoTraderHelpers.PrintDebugMessage(" - IsTradeGood: " + _currentItemRosterElement.EquipmentElement.Item.IsTradeGood.ToString());
            return _currentItemRosterElement.EquipmentElement.Item.IsTradeGood;
        }

        public int GetItemAmount()
        {
            try
            {
                AutoTraderHelpers.PrintDebugMessage(" - ItemAmount: " + _currentItemRosterElement.Amount.ToString());
                return _currentItemRosterElement.Amount;
            }
            catch (Exception e)
            {
                AutoTraderHelpers.PrintMessage("GetItemAmount crashed: " + e.ToString());
                return 0;
            }
        }

        public string GetItemName()
        {
            AutoTraderHelpers.PrintDebugMessage(" - ItemName: " + _currentItemRosterElement.EquipmentElement.Item.Name.ToString());
            return _currentItemRosterElement.EquipmentElement.Item.Name.ToString();
        }

        public float GetItemWeight()
        {
            AutoTraderHelpers.PrintDebugMessage(" - ItemWeight: " + _currentItemRosterElement.EquipmentElement.Item.Weight.ToString());
            return _currentItemRosterElement.EquipmentElement.Item.Weight;
        }

        /// <summary>
        /// Whether the player smithed this weapon. Note this is NOT the same as having a weapon
        /// design: ItemObject.IsCraftedWeapon is true for most vanilla weapons, because they are
        /// defined from crafting pieces, so testing that would protect nearly the whole armoury.
        /// </summary>
        /// <summary>Tier of the current item as a plain number (Tier1 == 1).</summary>
        public int GetItemTier()
        {
            ItemObject item = _currentItemRosterElement.EquipmentElement.Item;
            return item == null ? 1 : (int)item.Tier + 1;
        }

        public bool IsPlayerCraftedWeapon()
        {
            ItemObject item = _currentItemRosterElement.EquipmentElement.Item;
            bool result = item != null && item.IsCraftedByPlayer;
            AutoTraderHelpers.PrintDebugMessage(" - IsPlayerCraftedWeapon: " + result.ToString());
            return result;
        }

        public bool IsPackAnimal()
        {
            var result = _currentItemRosterElement.EquipmentElement.Item.HorseComponent.IsPackAnimal;
            AutoTraderHelpers.PrintDebugMessage(" - IsPackAnimal: " + result.ToString());
            return result;
        }

        public bool IsItemGrain()
        {
            var result = _currentItemRosterElement.EquipmentElement.Item == DefaultItems.Grain;
            AutoTraderHelpers.PrintDebugMessage(" - IsItemGrain: " + result.ToString());
            return result;
        }

        public bool IsItemHardwood()
        {
            var result = _currentItemRosterElement.EquipmentElement.Item == DefaultItems.HardWood;
            AutoTraderHelpers.PrintDebugMessage(" - IsItemHardwood: " + result.ToString());
            return result;
        }

        public int GetPartyHardwoodIndex()
        {
            var result = PartyBase.MainParty.ItemRoster.FindIndexOfItem(DefaultItems.HardWood);
            AutoTraderHelpers.PrintDebugMessage(" - HardwoodIndex: " + result.ToString());
            return result;
        }

        public int GetHardwoodUnitValue()
        {
            return DefaultItems.HardWood.Value;
        }

        // Hardwood a weapon would yield when smelted (0 for non-weapons). Wood == index 7.
        public int GetCurrentItemHardwoodSmeltYield()
        {
            ItemObject item = _currentItemRosterElement.EquipmentElement.Item;
            if (item == null || !AutoTraderHelpers.IsWeapon(item))
            {
                return 0;
            }
            int[] output = Campaign.Current.Models.SmithingModel.GetSmeltingOutputForItem(item);
            return output[(int)CraftingMaterials.Wood];
        }

        public float GetRosterElementWeight()
        {
            var result = _currentItemRosterElement.GetRosterElementWeight();
            if (!_isBuying)
                AutoTraderHelpers.PrintDebugMessage("GetRosterElementWeigth: Not buying!");
            AutoTraderHelpers.PrintDebugMessage(" - RosterElementWeight: " + result.ToString());
            return result;
        }

        public bool IsItemTierLowerThan(ItemObject.ItemTiers tier)
        {
            var result = _currentItemRosterElement.EquipmentElement.Item.Tier < tier;
            AutoTraderHelpers.PrintDebugMessage(" - IsItemTierLowerThan: " + result.ToString());
            return result;
        }

        public bool IsItemFiltered(List<string> doneItems = null)
        {
            ItemObject itemObject = _currentItemRosterElement.EquipmentElement.Item;
            if (itemObject == null)
            {
                return true;
            }

            FilterItem item = new FilterItem(
                _currentItemRosterElement.Amount,
                IsItemLocked(),
                doneItems != null && doneItems.Exists(x => x == itemObject.Name.ToString()),
                AutoTraderHelpers.IsSmithingMaterial(itemObject),
                AutoTraderHelpers.IsHorse(itemObject),
                AutoTraderHelpers.IsArmor(itemObject),
                AutoTraderHelpers.IsWeapon(itemObject),
                AutoTraderHelpers.IsLivestock(itemObject),
                AutoTraderHelpers.IsTradeGood(itemObject),
                AutoTraderHelpers.IsConsumable(itemObject));

            FilterSettings settings = new FilterSettings(
                AutoTraderConfig.SellSmithingValue,
                AutoTraderConfig.SpeedAwareMountsValue,
                _isBuying ? AutoTraderConfig.BuyHorsesValue : AutoTraderConfig.SellHorsesValue,
                _isBuying ? AutoTraderConfig.BuyArmorValue : AutoTraderConfig.SellArmorValue,
                _isBuying ? AutoTraderConfig.BuyWeaponsValue : AutoTraderConfig.SellWeaponsValue,
                AutoTraderConfig.BuySmeltablesForHardwoodValue,
                _isBuying ? AutoTraderConfig.BuyLivestockValue : AutoTraderConfig.SellLivestockValue,
                _isBuying ? AutoTraderConfig.BuyGoodsValue : AutoTraderConfig.SellGoodsValue,
                _isBuying ? AutoTraderConfig.BuyConsumablesValue : AutoTraderConfig.SellConsumablesValue);

            string reason;
            bool filtered = ItemFilter.IsFiltered(item, settings, _isBuying, out reason);
            if (filtered)
            {
                AutoTraderHelpers.PrintDebugMessage(" - filtered: " + reason);
            }
            return filtered;
        }

        /// Helper Wrapper
        public bool IsArmor()
        {
            var itemRosterElement = _currentItemRosterElement;
            return AutoTraderHelpers.IsArmor(itemRosterElement.EquipmentElement.Item);
        }

        public bool IsWeapon()
        {
            var itemRosterElement = _currentItemRosterElement;
            return AutoTraderHelpers.IsWeapon(itemRosterElement.EquipmentElement.Item);
        }

        public bool IsHorse()
        {
            var itemRosterElement = _currentItemRosterElement;
            return AutoTraderHelpers.IsHorse(itemRosterElement.EquipmentElement.Item);
        }

        public bool IsConsumable()
        {
            var itemRosterElement = _currentItemRosterElement;
            return AutoTraderHelpers.IsConsumable(itemRosterElement.EquipmentElement.Item);
        }

        public bool IsLivestock()
        {
            var itemRosterElement = _currentItemRosterElement;
            return AutoTraderHelpers.IsLivestock(itemRosterElement.EquipmentElement.Item);
        }

    }
}
