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
    class AutoTraderLogicConnector: ILogicConnector
    {

        public bool _isCaravan = false;
        public bool _isBuying = false;

        private ItemRosterElement _currentItemRosterElement;
        private InventoryLogic _inventoryLogic;
        private int _inventoryDisplayRefreshTicks;

        bool ILogicConnector.IsCaravan { get { return _isCaravan; } set { _isCaravan = value; } }
        bool ILogicConnector.IsBuying { get { return _isBuying; } set { _isBuying = value; } }

        public enum MerchantType
        {
            Town,
            Village,
            Caravan
        }

        public void SetCurrentElementById(int itemId)
        {
            _currentItemRosterElement = GetItemRoster()[itemId];
            AutoTraderHelpers.PrintDebugMessage(" - Current Item: " + _currentItemRosterElement.EquipmentElement.Item.Name.ToString());
        }

        public int GetInitialGold()
        {
            // TODO: Use "MobileParty" instead since 1.3?
            AutoTraderHelpers.PrintDebugMessage(" - InitialGold: " + PartyBase.MainParty.Owner.Gold.ToString());
            return PartyBase.MainParty.Owner.Gold;
        }
        public int GetTroopWage()
        {
            // ToDo: Whole daily wage
            AutoTraderHelpers.PrintDebugMessage(" - TroopWage: " + PartyBase.MainParty.MobileParty.TotalWage.ToString());
            return PartyBase.MainParty.MobileParty.TotalWage;
        }
        /// <summary>
        /// Decides once whether the fleet figures apply, and returns both together.
        ///
        /// Weight and capacity must come from the same source: an empty hold legitimately reports
        /// zero weight, so deciding per value (the previous "use it if it is above zero") could
        /// return a land weight while the capacity still came from the fleet.
        /// </summary>
        private static bool TryGetFleetFigures(out float weight, out float capacity)
        {
            weight = 0f;
            capacity = 0f;
            if (!AutoTraderConfig.UseMaxFleetCapacityValue || !WarsailsDetector.IsWarsailsDLCAvailable())
            {
                return false;
            }

            MobileParty party = MobileParty.MainParty;
            if (party == null || party.Ships == null || party.Ships.Count == 0)
            {
                return false;
            }

            int fleetCapacity = WarsailsHelper.GetFleetCargoCapacity(party);
            if (fleetCapacity <= 0)
            {
                return false;
            }

            weight = WarsailsHelper.GetFleetTotalWeightCarried(party);
            capacity = fleetCapacity;
            return true;
        }

        public float GetCurrentWeight()
        {
            float weight;
            float capacity;
            if (TryGetFleetFigures(out weight, out capacity))
            {
                AutoTraderHelpers.PrintDebugMessage(" - Using FleetTotalWeightCarried: " + weight.ToString());
                return weight;
            }

            AutoTraderHelpers.PrintDebugMessage(" - CurrentWeight: " + MobileParty.MainParty.TotalWeightCarried.ToString());
            return MobileParty.MainParty.TotalWeightCarried;
        }
        public float GetInventoryCapacity()
        {
            float weight;
            float capacity;
            if (TryGetFleetFigures(out weight, out capacity))
            {
                AutoTraderHelpers.PrintDebugMessage(" - Using FleetCargoCapacity: " + capacity.ToString());
                return capacity;
            }

            AutoTraderHelpers.PrintDebugMessage(" - InventoryCapacity: " + PartyBase.MainParty.MobileParty.InventoryCapacity.ToString());
            return PartyBase.MainParty.MobileParty.InventoryCapacity;
        }
        public int GetPlayerItemRosterSize()
        {
            AutoTraderHelpers.PrintDebugMessage(" - PartyItemRosterSize: " + PartyBase.MainParty.ItemRoster.Count.ToString());
            return PartyBase.MainParty.MobileParty.ItemRoster.Count;
        }
        public int GetNumPartyMembers()
        {
            // TODO: Use "MobileParty" instead since 1.3?
            AutoTraderHelpers.PrintDebugMessage(" - NumPartyMembers: " + PartyBase.MainParty.NumberOfAllMembers.ToString());
            return PartyBase.MainParty.NumberOfAllMembers;
        }
        public int GetNumLivestockAnimals()
        {
            AutoTraderHelpers.PrintDebugMessage(" - NumLivestockAnimals: " + PartyBase.MainParty.ItemRoster.NumberOfLivestockAnimals.ToString());
            return PartyBase.MainParty.MobileParty.ItemRoster.NumberOfLivestockAnimals;
        }

        // Foot soldiers a spare mount can mount - exactly what the vanilla speed model
        // (DefaultPartySpeedCalculatingModel) uses for the mounted-footmen bonus.
        public int GetNumFootTroops()
        {
            int count = PartyBase.MainParty.NumberOfMenWithoutHorse;
            AutoTraderHelpers.PrintDebugMessage(" - NumFootTroops (menWithoutHorse): " + count.ToString());
            return count;
        }

        // --- Speed-aware mount trading: per-category inventory counts + upgrade reserves ---

        private static int CountInventory(Func<ItemObject, bool> predicate)
        {
            int count = 0;
            ItemRoster roster = PartyBase.MainParty.MobileParty.ItemRoster;
            for (int i = 0; i < roster.Count; i++)
            {
                ItemRosterElement e = roster[i];
                ItemObject item = e.EquipmentElement.Item;
                if (item != null && predicate(item))
                {
                    count += e.Amount;
                }
            }
            return count;
        }

        private static bool IsRegularMount(ItemObject item)
        {
            return item.HorseComponent != null && item.HorseComponent.IsMount
                && item.ItemCategory != DefaultItemCategories.WarHorse
                && item.ItemCategory != DefaultItemCategories.NobleHorse;
        }

        public int GetNumRegularRidingMounts()
        {
            return CountInventory(IsRegularMount);
        }

        public int GetNumWarMounts()
        {
            return CountInventory(item => item.HorseComponent != null && item.HorseComponent.IsMount
                && item.ItemCategory == DefaultItemCategories.WarHorse);
        }

        public int GetNumNobleMounts()
        {
            return CountInventory(item => item.HorseComponent != null && item.HorseComponent.IsMount
                && item.ItemCategory == DefaultItemCategories.NobleHorse);
        }

        public int GetNumPackAnimals()
        {
            return CountInventory(item => item.HorseComponent != null && item.HorseComponent.IsPackAnimal);
        }

        // Mounts of the given category that pending troop upgrades would consume.
        private static int CountUpgradeDemand(ItemCategory category)
        {
            int count = 0;
            TroopRoster roster = PartyBase.MainParty.MemberRoster;
            for (int i = 0; i < roster.Count; i++)
            {
                CharacterObject c = roster.GetCharacterAtIndex(i);
                if (c == null || c.IsHero)
                {
                    continue;
                }
                if (c.UpgradeRequiresItemFromCategory == category)
                {
                    count += roster.GetElementNumber(i);
                }
            }
            return count;
        }

        public int GetWarMountUpgradeReserve()
        {
            return CountUpgradeDemand(DefaultItemCategories.WarHorse);
        }

        public int GetNobleMountUpgradeReserve()
        {
            return CountUpgradeDemand(DefaultItemCategories.NobleHorse);
        }

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

        public bool IsWeaponDesignEmpty()
        {
            var result = _currentItemRosterElement.EquipmentElement.Item.WeaponDesign == null;
            AutoTraderHelpers.PrintDebugMessage(" - IsWeaponDesignEmpty: " + result.ToString());
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

        /// <summary>
        /// Days the party can keep marching on the food it carries - the same figure the game
        /// shows the player. Scales with party size, unlike a fixed item count.
        /// </summary>
        public int GetFoodDaysRemaining()
        {
            int result = MobileParty.MainParty.GetNumDaysForFoodToLast();
            AutoTraderHelpers.PrintDebugMessage(" - FoodDaysRemaining: " + result.ToString());
            return result;
        }

        // --- Warehouse: store goods the local merchant could not afford ------

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

        // --- Smithing supply: cheap smeltable weapons as a hardwood source ---

        public int GetHardwoodCount()
        {
            return PartyBase.MainParty.ItemRoster.GetItemNumber(DefaultItems.HardWood);
        }

        /// <summary>
        /// Ore and ingots the party carries - the materials whose refining and smelting burns
        /// charcoal, and therefore what the hardwood target should scale with.
        /// </summary>
        public int GetRefinableMaterialCount()
        {
            ItemRoster roster = PartyBase.MainParty.MobileParty.ItemRoster;
            int count = 0;
            for (int i = 0; i < roster.Count; i++)
            {
                ItemRosterElement e = roster[i];
                ItemObject item = e.EquipmentElement.Item;
                if (item == null)
                {
                    continue;
                }
                if (item == DefaultItems.IronOre
                    || item == DefaultItems.IronIngot1 || item == DefaultItems.IronIngot2
                    || item == DefaultItems.IronIngot3 || item == DefaultItems.IronIngot4
                    || item == DefaultItems.IronIngot5 || item == DefaultItems.IronIngot6)
                {
                    count += e.Amount;
                }
            }
            AutoTraderHelpers.PrintDebugMessage(" - RefinableMaterials: " + count.ToString());
            return count;
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

        public bool InitInventory()
        {
            var merchantType = GetMerchantType();
            if (merchantType == MerchantType.Town)
            {
                InventoryScreenHelper.OpenScreenAsTrade(Settlement.CurrentSettlement.ItemRoster, Settlement.CurrentSettlement.Town,
                    InventoryScreenHelper.InventoryCategoryType.None, null);
            }
            else if (merchantType == MerchantType.Village)
            {
                InventoryScreenHelper.OpenScreenAsTrade(Settlement.CurrentSettlement.ItemRoster, Settlement.CurrentSettlement.Village, InventoryScreenHelper.InventoryCategoryType.None, null);
            }
            else if (merchantType == MerchantType.Caravan)
            {
                InventoryScreenHelper.OpenTradeWithCaravanOrAlleyParty(MobileParty.ConversationParty, InventoryScreenHelper.InventoryCategoryType.None);
            }
            else
            {
                return false;
            }

            return TryPrepareInventoryLogic();
        }

        /// <summary>
        /// In 1.4.6, TransactionDebt invokes TotalAmountChange without a null-check.
        /// Autotrading runs before the trade UI wires its handler, so we provide a temporary no-op.
        /// </summary>
        private bool TryPrepareInventoryLogic()
        {
            _inventoryLogic = InventoryScreenHelper.GetActiveInventoryState()?.InventoryLogic;
            if (_inventoryLogic == null)
            {
                AutoTraderHelpers.PrintDebugMessage("Failed to get active inventory logic!");
                return false;
            }

            if (_inventoryLogic.TotalAmountChange == null)
            {
                _inventoryLogic.TotalAmountChange = _ => { };
            }
            return true;
        }

        public void BeginInventoryDisplayRefresh()
        {
            _inventoryDisplayRefreshTicks = 15;
        }

        public void TickInventoryDisplayRefresh()
        {
            if (_inventoryDisplayRefreshTicks <= 0)
            {
                return;
            }

            if (InventoryScreenHelper.GetActiveInventoryState() == null)
            {
                _inventoryDisplayRefreshTicks = 0;
                return;
            }

            _inventoryLogic = InventoryScreenHelper.GetActiveInventoryState()?.InventoryLogic ?? _inventoryLogic;
            _inventoryLogic?.TotalAmountChange?.Invoke(_inventoryLogic.TotalAmount);
            _inventoryDisplayRefreshTicks--;
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

        private ItemRoster GetItemRoster()
        {
            if (_isBuying)
            {
                if (GetMerchantType() == MerchantType.Caravan)
                    return MobileParty.ConversationParty.ItemRoster;
                return Settlement.CurrentSettlement.ItemRoster;
                
            } else
                return PartyBase.MainParty.ItemRoster;
        }

        public bool IsItemTierLowerThan(ItemObject.ItemTiers tier)
        {
            var result = _currentItemRosterElement.EquipmentElement.Item.Tier < tier;
            AutoTraderHelpers.PrintDebugMessage(" - IsItemTierLowerThan: " + result.ToString());
            return result;
        }

        public bool IsItemFiltered(List<string> doneItems=null)
        {
            var itemRosterElement = _currentItemRosterElement;
            ItemObject itemObject = itemRosterElement.EquipmentElement.Item;

            // Filter by amount
            if (_currentItemRosterElement.Amount <=0)
            {
                AutoTraderHelpers.PrintDebugMessage(" - filtered because out of stock");
                return true;
            }
            // Filter by lock
            if (IsItemLocked())
                return true;

            // Check if already bought / sold
            if (doneItems != null && doneItems.Exists(x => x == itemRosterElement.EquipmentElement.Item.Name.ToString()))
                return true;

            // Filter by type
            if (!_isBuying && AutoTraderHelpers.IsSmithingMaterial(itemObject))
            {
                AutoTraderHelpers.PrintDebugMessage(" - is smithing material");
                return AutoTraderConfig.SellSmithingValue ? false : true;
            }
            if (AutoTraderHelpers.IsHorse(itemObject))
            {
                // Speed-aware mount trading decides per-mount in CanBuy/CanSell, so let every
                // horse through when it is enabled; otherwise use the plain buy/sell toggles.
                if (!AutoTraderConfig.SpeedAwareMountsValue
                    && (_isBuying ? !AutoTraderConfig.BuyHorsesValue : !AutoTraderConfig.SellHorsesValue))
                {
                    return true;
                }
            }
            if (AutoTraderHelpers.IsArmor(itemObject) && !(_isBuying ? AutoTraderConfig.BuyArmorValue : AutoTraderConfig.SellArmorValue))
                return true;
            if (AutoTraderHelpers.IsWeapon(itemObject))
            {
                // Allow weapons through on buy when collecting smeltables, so DecideSmeltablePurchase
                // can evaluate them; CanBuy still declines non-smeltable weapons unless BuyWeapons is on.
                bool allowWeapon = _isBuying
                    ? (AutoTraderConfig.BuyWeaponsValue || AutoTraderConfig.BuySmeltablesForHardwoodValue)
                    : AutoTraderConfig.SellWeaponsValue;
                if (!allowWeapon)
                {
                    return true;
                }
            }
            if (AutoTraderHelpers.IsLivestock(itemObject) && !(_isBuying ? AutoTraderConfig.BuyLivestockValue : AutoTraderConfig.SellLivestockValue))
                return true;
            if (AutoTraderHelpers.IsTradeGood(itemObject) && !(_isBuying ? AutoTraderConfig.BuyGoodsValue : AutoTraderConfig.SellGoodsValue))
            {
                if (!AutoTraderHelpers.IsConsumable(itemObject))
                    return true;
            }
            if (AutoTraderHelpers.IsConsumable(itemObject) && !(_isBuying ? AutoTraderConfig.BuyConsumablesValue : AutoTraderConfig.SellConsumablesValue))
                return true;

            return false;
        }

        public void TransferItem()
        {
            // Generate command
            TransferCommand transferCommand = TransferCommand.Transfer(1,
                _isBuying ? InventoryLogic.InventorySide.OtherInventory : InventoryLogic.InventorySide.PlayerInventory,
                _isBuying ? InventoryLogic.InventorySide.PlayerInventory : InventoryLogic.InventorySide.OtherInventory,
                _currentItemRosterElement, EquipmentIndex.None, EquipmentIndex.None, CharacterObject.PlayerCharacter);
            _inventoryLogic.AddTransferCommand(transferCommand);
            AutoTraderHelpers.PrintDebugMessage(" - Transfer of item " + GetItemName() + " complete! (" + (_isBuying? "Buy" : "Sell") + ")");
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

        public void SetCurrentElementByName(string itemName)
        {
            bool found = false;
            foreach (ItemRosterElement element in GetItemRoster())
            {
                if (element.EquipmentElement.Item.Name.ToString().Equals(itemName))
                {
                    found = true;
                    AutoTraderHelpers.PrintDebugMessage("Found by name: " + element.EquipmentElement.Item.Name.ToString() + " == " + itemName);
                    _currentItemRosterElement = element;
                    break;
                }
            }
            if (!found)
            {
                AutoTraderHelpers.PrintDebugMessage("!!!!! Could not find element with name : " + itemName + " !!!!!");
            }
        }

        public List<string> GetPlayerItemRosterNames()
        {
            return PartyBase.MainParty.ItemRoster.Select(x => x.EquipmentElement.Item.Name.ToString()).ToList();
        }

        public int GetItemAmountInPlayerRoster()
        {
            int amount = 0;
            foreach (ItemRosterElement element in PartyBase.MainParty.ItemRoster)
            {
                if (element.EquipmentElement.Item.Name.ToString().Equals(GetItemName()))
                {
                    amount = element.Amount;
                    break;
                }
            }
            AutoTraderHelpers.PrintDebugMessage("- amount in own inventory: " + amount.ToString());
            return amount;
        }
    }
}
