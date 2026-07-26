using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using AutoTrader.SpeedTrading;

[assembly: InternalsVisibleTo("AutoTraderTests")]
namespace AutoTrader
{
    public class AutoTraderLogic
    {
        public bool IsTradingActive { get; set; }

        private readonly ILogicConnector _logicConnector;

        private float _availableInventoryCapacity;
        private int _availablePlayerGold;
        private int _availableMerchantGold;

        private List<string> _soldItems;
        private List<string> _boughtItems;

        // Speed-aware mount trading: per-run, per-category mount budgets
        // (see ComputeMountBudgets / SpeedTrading.PartySpeedAdvisor).
        private int _buyRegularBudget;
        private int _buyPackBudget;
        private int _sellRegularBudget;
        private int _sellWarBudget;
        private int _sellNobleBudget;
        private int _sellPackBudget;
        private int _sellLivestockBudget;

        // Items that only failed to sell because the merchant ran out of gold - exactly what the
        // warehouse is for.
        private List<string> _goldBlockedItems;

        // Post-trade summary counters (units transacted this run).
        private int _unitsBought;
        private int _unitsSold;
        private int _mountsBought;
        private int _mountsSold;
        private int _storedUnits;

        public AutoTraderLogic(ILogicConnector logicConnector)
        {
            AutoTraderHelpers.PrintDebugMessage("####### AutoTrader Initialization #######");
            _logicConnector = logicConnector;
            
        }

        public void OnApplicationTick()
        {
            _logicConnector.TickInventoryDisplayRefresh();
        }

        public void PerformAutoTrade(bool isCaravan = false)
        {
            AutoTraderHelpers.PrintDebugMessage("####### Performing AutoTrade #######");
            _logicConnector.IsCaravan = isCaravan;
            AutoTraderHelpers.PrintDebugMessage(" - isCaravan: " + isCaravan);
            int initialGold = _logicConnector.GetInitialGold();
            int troopWage = _logicConnector.GetTroopWage();
            _availablePlayerGold = initialGold - (AutoTraderConfig.KeepWagesValue * troopWage);
            AutoTraderHelpers.PrintDebugMessage(" - availablePlayerGold: " + _availablePlayerGold.ToString());

            _soldItems = new List<string>();
            _boughtItems = new List<string>();
            _goldBlockedItems = new List<string>();
            _unitsBought = 0;
            _unitsSold = 0;
            _mountsBought = 0;
            _mountsSold = 0;
            _storedUnits = 0;
            int startAvailableGold = _availablePlayerGold;
            _availableMerchantGold = _logicConnector.GetMerchantGold();
            UpdateAvailableInventoryCapacity();
            AutoTraderMcmSettings.Apply();
            LogEffectiveSettings();
            ComputeMountBudgets();

            // Set trading state
            IsTradingActive = true;
            try
            {
                if (!_logicConnector.InitInventory())
                {
                    AutoTraderHelpers.PrintDebugMessage("Failed to initialize the inventory!");
                    return;
                }

                AutoTraderHelpers.PrintDebugMessage("##### Restocking #####");
                BuyProcess(RestockFilter);
                BuyProcess(BuyFilter);
                Sell();
                BuyProcess(BuyFilter);
                DepositUnsoldGoods();
                _logicConnector.BeginInventoryDisplayRefresh();
                PrintTradeSummary(_availablePlayerGold - startAvailableGold);
            } catch ( Exception e)
            {
                AutoTraderHelpers.PrintMessage("My Lord! Something terrible happened to our autotraders! The last we heard of them is:\n" + e.ToString());
            } finally
            {
                // Unset trading state
                IsTradingActive = false;
            }   
        }

        private void UpdateAvailableInventoryCapacity()
        {
            float currentWeight = _logicConnector.GetCurrentWeight();
            float inventoryCapacity = _logicConnector.GetInventoryCapacity();
           
            _availableInventoryCapacity = inventoryCapacity * ((float)AutoTraderConfig.UseInventorySpaceValue / 100.0f);
            _availableInventoryCapacity -= currentWeight;
            AutoTraderHelpers.PrintDebugMessage(" - actual availableInventoryCapacity: " + _availableInventoryCapacity.ToString());

            // Catch some cases where the user got a wrong configuration.
            if (AutoTraderConfig.UseMaxFleetCapacityValue && MobileParty.MainParty.Ships.Count <= 0)
            {
                AutoTraderHelpers.PrintMessage(new TextObject(
                    "{=ATNoFleet}My Lord! Your traders are at a loss, you specifically asked them to load the wares on our ships but we have no fleet! " +
                    "I instructed to load them on our carts but you may want to rethink your orders.").ToString()
                );
            }
        }

        // Records what was actually configured for this run, so a log can be read on its own
        // without also needing the settings file.
        private void LogEffectiveSettings()
        {
            AutoTraderHelpers.PrintDebugMessage(" - [config] pricing=" + (AutoTraderConfig.SimpleTradingAI ? "rumours" : "thresholds")
                + " buy/sell=" + AutoTraderConfig.BuyThresholdValue + "/" + AutoTraderConfig.SellThresholdValue
                + " scan=" + (AutoTraderConfig.UseWeightedValue ? "all" : "radius " + AutoTraderConfig.SearchRadiusValue)
                + " capacity=" + AutoTraderConfig.UseInventorySpaceValue + "% perGood=" + AutoTraderConfig.MaxCapacityValue + "%"
                + " fleet=" + AutoTraderConfig.UseMaxFleetCapacityValue);
            AutoTraderHelpers.PrintDebugMessage(" - [config] mounts=" + AutoTraderConfig.SpeedAwareMountsValue
                + " reserveUpgrades=" + AutoTraderConfig.ReserveUpgradeMountsValue
                + " keepAbove=" + AutoTraderConfig.KeepMountsAboveValueValue
                + " sellNoble=" + AutoTraderConfig.SellNobleMountsValue
                + " buyPack=" + AutoTraderConfig.BuyHorsesValue
                + " packHerd=" + AutoTraderConfig.ManagePackAnimalHerdValue
                + " protectPack=" + AutoTraderConfig.ProtectPackAnimalsValue
                + " livestockHerd=" + AutoTraderConfig.ManageLivestockHerdValue
                + " livestockReserve=" + AutoTraderConfig.KeepLivestockReserveValue);
            AutoTraderHelpers.PrintDebugMessage(" - [config] hardwood buy=" + AutoTraderConfig.ResupplyHardwoodValue
                + " smelt=" + AutoTraderConfig.BuySmeltablesForHardwoodValue
                + " target=" + AutoTraderConfig.SmeltHardwoodTargetValue
                + " | warehouse=" + AutoTraderConfig.WarehouseModeValue
                + " share=" + AutoTraderConfig.ConsignmentSharePercentValue + "%");
        }

        // Speed-aware mount trading: determines once per run how many mounts to buy or sell,
        // per category, keeping war/noble mounts needed for troop upgrades. The pure decision
        // logic lives engine-free in SpeedTrading.PartySpeedAdvisor.
        private void ComputeMountBudgets()
        {
            _buyRegularBudget = 0;
            _buyPackBudget = 0;
            _sellRegularBudget = 0;
            _sellWarBudget = 0;
            _sellNobleBudget = 0;
            _sellPackBudget = 0;
            _sellLivestockBudget = 0;

            int warReserve = AutoTraderConfig.ReserveUpgradeMountsValue ? _logicConnector.GetWarMountUpgradeReserve() : 0;
            int nobleReserve = AutoTraderConfig.ReserveUpgradeMountsValue ? _logicConnector.GetNobleMountUpgradeReserve() : 0;

            PartySnapshot snapshot = new PartySnapshot(
                _logicConnector.GetNumPartyMembers(),
                _logicConnector.GetNumFootTroops(),
                _logicConnector.GetNumRegularRidingMounts(),
                _logicConnector.GetNumWarMounts(),
                _logicConnector.GetNumNobleMounts(),
                _logicConnector.GetNumPackAnimals(),
                _logicConnector.GetNumLivestockAnimals(),
                warReserve,
                nobleReserve,
                _logicConnector.GetCurrentWeight(),
                _logicConnector.GetInventoryCapacity());

            AutoTraderHelpers.PrintDebugMessage(" - [mount] snapshot members=" + snapshot.MemberCount + " foot=" + snapshot.FootTroopCount
                + " reg=" + snapshot.RegularMounts + " war=" + snapshot.WarMounts + " noble=" + snapshot.NobleMounts
                + " pack=" + snapshot.PackAnimals + " livestock=" + snapshot.Livestock
                + " reserves w/n=" + warReserve + "/" + nobleReserve);

            PartySpeedAdvisor advisor = new PartySpeedAdvisor(
                AutoTraderConfig.SellNobleMountsValue,
                AutoTraderConfig.ManagePackAnimalHerdValue && !AutoTraderConfig.ProtectPackAnimalsValue,
                AutoTraderConfig.ManageLivestockHerdValue,
                AutoTraderConfig.KeepLivestockReserveValue,
                AutoTraderConfig.UseInventorySpaceValue);
            MountRecommendation recommendation = advisor.Recommend(snapshot)
                .ForMountMode(AutoTraderConfig.SpeedAwareMountsValue);

            _buyRegularBudget = recommendation.BuyRegular;
            _buyPackBudget = recommendation.BuyPack;
            _sellRegularBudget = recommendation.SellRegular;
            _sellWarBudget = recommendation.SellWar;
            _sellNobleBudget = recommendation.SellNoble;
            _sellPackBudget = recommendation.SellPack;
            _sellLivestockBudget = recommendation.SellLivestock;
            AutoTraderHelpers.PrintDebugMessage(" - mount plan: " + recommendation.Reason);
        }

        private void Sell()
        {
            AutoTraderHelpers.PrintDebugMessage("##### Selling #####");
            _logicConnector.IsBuying = false;
            var itemSellList = new List<KeyValuePair<string, KeyValuePair<float, float>>>();

            foreach( string itemName in _logicConnector.GetPlayerItemRosterNames())
            {
                AutoTraderHelpers.PrintDebugMessage("-------------------------------------------------------------------");
                AutoTraderHelpers.PrintDebugMessage(" - current item: " + itemName);
                _logicConnector.SetCurrentElementByName(itemName);

                // Check if its filtered
                if (_logicConnector.IsItemFiltered(_boughtItems))
                {
                    AutoTraderHelpers.PrintDebugMessage(" - Item is filtered!");
                    continue;
                }

                int amount = _logicConnector.GetItemAmount();
                if (amount < 1)
                {
                    AutoTraderHelpers.PrintDebugMessage(" - skipping because amount is less than 1");
                    continue;
                }

                // TODO: Change on simple AI
                float averagePrice = GetAveragePrice();

                // Sell items one by one
                bool canSell = false;
                do
                {
                    int buyout_price = 0;
                    canSell = CanSell(averagePrice, amount, out buyout_price);
                    if (canSell)
                    {
                        // Update members
                        ProcessTransaction(buyout_price);
                        amount--;
                    }

                } while (canSell && amount > 0);
            }
        }

        private void BuyProcess(Func<bool> filterFunc)
        {
            AutoTraderHelpers.PrintDebugMessage("##### Executing buy process #####");

            _logicConnector.IsBuying = true;
            var itemBuyList = new List<KeyValuePair<string, KeyValuePair<float, float>>>();

            // Loop through all items of merchant
            for (int itemId = 0; itemId < _logicConnector.GetMerchantItemRosterSize(); itemId++)
            {
                AutoTraderHelpers.PrintDebugMessage("-------------------------------------------------------------------");
                AutoTraderHelpers.PrintDebugMessage(" - current item ID: " + itemId.ToString());
                _logicConnector.SetCurrentElementById(itemId);

                if (filterFunc())
                {
                    AutoTraderHelpers.PrintDebugMessage(" - skipping because item is filtered");
                    continue;
                }

                int buyoutPrice = _logicConnector.GetItemPrice();
                float averagePrice = GetAveragePrice();
                float profit = _logicConnector.GetProjectedProfit(buyoutPrice);
                AutoTraderHelpers.PrintDebugMessage(" - average price: " + averagePrice.ToString());
                AutoTraderHelpers.PrintDebugMessage(" - projected profit: " + profit.ToString());
                if (profit == buyoutPrice)
                    profit = averagePrice - (float)buyoutPrice;
                AutoTraderHelpers.PrintDebugMessage(" - final profit: " + profit.ToString());

                // Purpose-driven buys (mounts for speed, smelt fodder) are not bought for resale
                // profit, so the profit sort is meaningless for them. Rank them first instead -
                // their own gates in CanBuy still decide whether anything is actually bought.
                if (_logicConnector.IsHorse()
                    || (AutoTraderConfig.BuySmeltablesForHardwoodValue && _logicConnector.IsWeapon()))
                {
                    profit = float.MaxValue;
                    AutoTraderHelpers.PrintDebugMessage(" - purpose-driven buy -> prioritized in buy list");
                }

                AutoTraderHelpers.PrintDebugMessage(" --> adding to buy list!");
                itemBuyList.Add(new KeyValuePair<string, KeyValuePair<float, float>>(_logicConnector.GetItemName(), new KeyValuePair<float, float>(averagePrice, profit)));
            }

            // Sort the list
            itemBuyList.Sort((pair1, pair2) => pair2.Value.Value.CompareTo(pair1.Value.Value));

            // Buy items in order of profit
            AutoTraderHelpers.PrintDebugMessage("### Actual buy loop ###");
            foreach (var element in itemBuyList)
            {
                AutoTraderHelpers.PrintDebugMessage("-------------------------------------------------------------------");

                string itemName = element.Key;
                float averagePrice = element.Value.Key;
                AutoTraderHelpers.PrintDebugMessage(" - current item Name: " + itemName.ToString());
                AutoTraderHelpers.PrintDebugMessage(" - averagePrice: " + averagePrice.ToString());

                _logicConnector.SetCurrentElementByName(itemName);                
                int amount = _logicConnector.GetItemAmount();
                int ownAmount = _logicConnector.GetItemAmountInPlayerRoster();

                // Buy items one by one
                bool canBuy = false;
                do
                {
                    int buyout_price = 0;
                    canBuy = CanBuy(averagePrice, amount, ownAmount, out buyout_price);

                    if (canBuy)
                    {
                        // Update members
                        ProcessTransaction(buyout_price);
                        amount--;
                        ownAmount++;
                    }

                } while (canBuy && amount > 0);
            }
        }

        private bool RestockFilter()
        {
            // Returns true if filtered
            if (!AutoTraderConfig.ResupplyValue)
            {
                AutoTraderHelpers.PrintDebugMessage("- Skipping resupply due to config");
                return true;
            }
            if (_logicConnector.GetItemAmount() <= 0)
            {
                AutoTraderHelpers.PrintDebugMessage(" - skipping because out of stock");
                return true;
            }
            if (!_logicConnector.IsConsumable())
            {
                AutoTraderHelpers.PrintDebugMessage(" - skipping because it is not a consumable");
                return true;
            }
            if (!AutoTraderSpecialRules.NeedsMoreFood(_logicConnector))
            {
                AutoTraderHelpers.PrintDebugMessage(" - skipping restock: food reserve is covered");
                return true;
            }
            return false;
        }

        private bool BuyFilter()
        {
            var result = _logicConnector.IsItemFiltered(_soldItems);
            if(result)
                AutoTraderHelpers.PrintDebugMessage(" - skipping because item is filtered");
            return result;
        }

        // Stores what the local merchant could not afford in this town's warehouse, so the
        // surplus stops travelling with the party. Consignment drains it over the next days.
        private void DepositUnsoldGoods()
        {
            _storedUnits = 0;
            if (AutoTraderConfig.WarehouseModeValue == AutoTraderConfig.WarehouseOff)
            {
                return;
            }
            if (_goldBlockedItems.Count == 0 || !_logicConnector.IsInOwnedTown())
            {
                return;
            }

            foreach (string itemName in _goldBlockedItems)
            {
                _storedUnits += _logicConnector.DepositItemToStash(itemName);
            }
        }

        // Shows a concise on-screen summary of what the trade run did.
        private void PrintTradeSummary(int netGold)
        {
            if (_unitsBought == 0 && _unitsSold == 0 && _storedUnits == 0)
            {
                return;
            }

            string goldText = (netGold >= 0 ? "+" : "") + netGold.ToString();
            string summary = $"AutoTrader: sold {_unitsSold}, bought {_unitsBought} items, net {goldText} gold.";
            if (_mountsBought > 0 || _mountsSold > 0)
            {
                summary += $" Mounts +{_mountsBought}/-{_mountsSold}.";
            }
            if (_storedUnits > 0)
            {
                summary += $" Stored {_storedUnits} in the warehouse (merchant out of gold).";
            }
            AutoTraderHelpers.PrintMessage(summary);
        }

        private void ProcessTransaction(int buyoutPrice)
        {
            AutoTraderHelpers.PrintDebugMessage("### Processing transaction ###");
            // Update available gold
            _availablePlayerGold += _logicConnector.IsBuying ? -buyoutPrice : buyoutPrice;
            _availableMerchantGold += _logicConnector.IsBuying ? buyoutPrice : -buyoutPrice;
            bool isMount = _logicConnector.IsHorse();
            if (_logicConnector.IsBuying)
            {
                _unitsBought++;
                if (isMount)
                {
                    _mountsBought++;
                }
            }
            else
            {
                _unitsSold++;
                if (isMount)
                {
                    _mountsSold++;
                }
            }

            // Mark the item
            string name = _logicConnector.GetItemName();
            if (_logicConnector.IsBuying)
            {
                if (!_boughtItems.Exists(x => x == name))
                    _boughtItems.Add(name);
            } else
            {
                if (!_soldItems.Exists(x => x == name))
                    _soldItems.Add(name);
            }

            // Update available weight
            UpdateAvailableInventoryCapacity();

            _logicConnector.TransferItem();
        }

        // Buys the current weapon only if it is a cost-effective hardwood source and the
        // party is below its hardwood target (coupled to smithing material need).
        private bool DecideSmeltablePurchase(int amount, int buyoutPrice)
        {
            if (!AutoTraderConfig.BuySmeltablesForHardwoodValue || !_logicConnector.IsWeapon())
            {
                return false;
            }

            int hardwood = _logicConnector.GetHardwoodCount();
            int target = AutoTraderConfig.SmeltHardwoodTargetValue;
            int hardwoodYield = _logicConnector.GetCurrentItemHardwoodSmeltYield();
            AutoTraderHelpers.PrintDebugMessage(" - [smelt] '" + _logicConnector.GetItemName() + "' price=" + buyoutPrice
                + " hardwood=" + hardwood + "/" + target + " yield=" + hardwoodYield);

            if (hardwood >= target)
            {
                AutoTraderHelpers.PrintDebugMessage(" - [smelt] hardwood target reached -> skip");
                return false;
            }
            if (hardwoodYield <= 0)
            {
                return false;
            }
            // Only worth it if the weapon costs no more than the hardwood it yields is worth.
            if (buyoutPrice > hardwoodYield * _logicConnector.GetHardwoodUnitValue())
            {
                AutoTraderHelpers.PrintDebugMessage(" - [smelt] too expensive as a hardwood source -> skip");
                return false;
            }
            AutoTraderHelpers.PrintDebugMessage(" - [smelt] BUY smeltable weapon for hardwood");
            return CheckBasicBuyRequirements(amount, buyoutPrice);
        }

        // Names the current mount's category for logging.
        private string DescribeMountCategory()
        {
            if (_logicConnector.IsPackAnimal())
            {
                return "pack";
            }
            if (_logicConnector.IsNobleMount())
            {
                return "noble";
            }
            if (_logicConnector.IsWarMount())
            {
                return "war";
            }
            return "regular";
        }

        // Decides whether to buy the current horse item. Pack animals keep the original
        // resupply rule; only regular riding horses are bought, up to the speed budget.
        private bool DecideHorsePurchase(int amount, int buyoutPrice)
        {
            AutoTraderHelpers.PrintDebugMessage(" - [mount] consider BUY '" + _logicConnector.GetItemName() + "' ["
                + DescribeMountCategory() + "] price=" + buyoutPrice + " buyRegularBudget=" + _buyRegularBudget);
            if (_logicConnector.IsPackAnimal())
            {
                // Need-based: only while the cargo does not fit and the herd still has headroom.
                // (The legacy rule here was unbounded - it compared the party size against the
                // LIVESTOCK count, so it always said yes and bought hundreds of mules.)
                if (AutoTraderConfig.BuyHorsesValue && _buyPackBudget > 0
                    && CheckBasicBuyRequirements(amount, buyoutPrice))
                {
                    _buyPackBudget--;
                    return true;
                }
                AutoTraderHelpers.PrintDebugMessage(" - do not buy pack animal (cargo fits, or herd limit reached)");
                return false;
            }

            // Riding mounts are only bought to reach the speed target, so nothing is bought here
            // while mount management is off - pack animals above keep their own setting.
            if (!AutoTraderConfig.SpeedAwareMountsValue)
            {
                AutoTraderHelpers.PrintDebugMessage(" - do not buy riding mount: mount management is off");
                return false;
            }

            // Only regular horses count for the speed target. War/noble mounts are upgrade
            // material and are not bought for speed here.
            bool isRegularMount = !_logicConnector.IsWarMount() && !_logicConnector.IsNobleMount();
            if (isRegularMount && _buyRegularBudget > 0)
            {
                // In fleet mode mounts occupy ship cargo -> respect capacity like other goods.
                if (AutoTraderConfig.UseMaxFleetCapacityValue
                    && _logicConnector.GetItemWeight() > _availableInventoryCapacity)
                {
                    AutoTraderHelpers.PrintDebugMessage(" - do not buy mount: fleet capacity reached");
                    return false;
                }
                if (CheckBasicBuyRequirements(amount, buyoutPrice))
                {
                    _buyRegularBudget--;
                    return true;
                }
                return false;
            }

            AutoTraderHelpers.PrintDebugMessage(" - do not buy mount (enough for speed, or war/noble kept for upgrades)");
            return false;
        }

        private bool CanBuy(float averagePrice, int amount, int ownAmount, out int buyoutPrice)
        {
            AutoTraderHelpers.PrintDebugMessage("### Can buy check ###");
            // TODO: Refactor, too many return statements

            // Retrieve price
            buyoutPrice = _logicConnector.GetCostOfRosterElement();

            // Special Rules
            // Horses
            if (_logicConnector.IsHorse())
            {
                return DecideHorsePurchase(amount, buyoutPrice);
            }

            // Cheap smeltable weapons as a hardwood source for smithing (only when needed).
            if (DecideSmeltablePurchase(amount, buyoutPrice))
            {
                return true;
            }
            // A weapon only reaches here to be considered as a smeltable; do not buy it via the
            // generic price logic unless normal weapon buying is enabled.
            if (_logicConnector.IsWeapon() && !AutoTraderConfig.BuyWeaponsValue)
            {
                AutoTraderHelpers.PrintDebugMessage(" - [smelt] weapon not a smeltable buy and BuyWeapons off -> skip");
                return false;
            }

            // Hardwood
            if (AutoTraderSpecialRules.ShouldBuyHardwood(_logicConnector))
            {
                AutoTraderHelpers.PrintDebugMessage("- buying hardwood to resupply");
                return CheckBasicBuyRequirements(amount, buyoutPrice); ;
            }

            // Consumables
            if (_logicConnector.IsConsumable())
            {
                int maxAmount = _logicConnector.IsItemGrain() ? AutoTraderConfig.KeepGrainsMaxValue : AutoTraderConfig.KeepConsumablesMaxValue;

                if (ownAmount >= maxAmount)
                {
                    AutoTraderHelpers.PrintDebugMessage(" --> not buying because we have enough of this item");
                    return false;
                }
                if (AutoTraderConfig.ResupplyValue && AutoTraderSpecialRules.NeedsMoreFood(_logicConnector))
                {
                    AutoTraderHelpers.PrintDebugMessage(" - [food] restocking: "
                        + _logicConnector.GetFoodDaysRemaining() + " days left, reserve is "
                        + AutoTraderConfig.KeepFoodDaysValue);
                    return CheckBasicBuyRequirements(amount, buyoutPrice);
                }

            }

            // Price niveau
            if (AutoTraderConfig.SimpleTradingAI)
            {
                // Case: buying
                float averagePriceFactorItemCategory = _logicConnector.GetAveragePriceFactorItemCategory();
                AutoTraderHelpers.PrintDebugMessage(" - average price factor item category: " + averagePriceFactorItemCategory.ToString());

                if (averagePriceFactorItemCategory != -99.0)
                {
                    float priceFactor = _logicConnector.GetCurrentTownPriceFactor();
                    AutoTraderHelpers.PrintDebugMessage(" - price factor: " + priceFactor.ToString());
                    if (priceFactor > averagePriceFactorItemCategory * 0.8f)
                    {
                        AutoTraderHelpers.PrintDebugMessage(" - do not buy because the price factor is too bad (" + priceFactor.ToString() + " > " + (averagePriceFactorItemCategory * 0.8f).ToString() + ")");
                        return false;
                    }
                        
                } else {
                    AutoTraderHelpers.PrintDebugMessage("- no average price found for " + _logicConnector.GetItemName());
                    return false;
                }
            }
            else
            {
                // Check threshold
                float priceFactor = (float)buyoutPrice / averagePrice;
                AutoTraderHelpers.PrintDebugMessage(" - price factor: " + priceFactor.ToString());
                if (priceFactor > (float)AutoTraderConfig.BuyThresholdValue / 100.0f)
                {
                    AutoTraderHelpers.PrintDebugMessage(" - do not buy because priceFactor is above threshold (" + priceFactor.ToString() + " > " + ((float)AutoTraderConfig.BuyThresholdValue / 100.0f).ToString() + ")");
                    return false;
                }
            }

            // Specials rules after price check
            // Check if we have enough cattle
            if (AutoTraderSpecialRules.CheckBuyCattleCondition(_logicConnector))
            {
                if (AutoTraderSpecialRules.CheckBuyCattleRule(_logicConnector))
                    return CheckBasicBuyRequirements(amount, buyoutPrice);
                else
                {
                    AutoTraderHelpers.PrintDebugMessage("- do not buy because we need no more cattle");
                    return false; // Don't buy more cattle   
                }
            }

            // Check weight
            if (AutoTraderSpecialRules.CheckBuyMaxCapacityRule(_logicConnector, (int)_logicConnector.GetInventoryCapacity(), ownAmount))
                return CheckBasicBuyRequirements(amount, buyoutPrice);
            else
            {
                AutoTraderHelpers.PrintDebugMessage("- do not buy because we are at capacity");
                return false;
            }
        }

        private bool CanSell(float averagePrice, int amount, out int buyoutPrice)
        {
            AutoTraderHelpers.PrintDebugMessage("### Can sell check ###");
            // Retrieve price
            buyoutPrice = _logicConnector.GetCostOfRosterElement();

            // Sell all Armor and Weapons
            if (_logicConnector.IsArmor())
            {
                if (_logicConnector.IsItemTierLowerThan((ItemObject.ItemTiers)AutoTraderConfig.WeaponsArmorTierValue))
                {
                    return CheckBasicSellRequirements(amount, buyoutPrice);
                }
                else
                {
                    AutoTraderHelpers.PrintDebugMessage("- do not sell because tier is too high");
                    return false;
                }
            } else if (_logicConnector.IsWeapon())
            {
                // Keep handmade weapons
                if (AutoTraderConfig.KeepSmeltingValue && !_logicConnector.IsWeaponDesignEmpty())
                {
                    AutoTraderHelpers.PrintDebugMessage("- do not sell because its crafted");
                    return false;
                }

                // Keep only cheap smelt-fodder we are collecting for hardwood - not valuable
                // weapons that merely happen to yield hardwood (those should still be sold).
                if (AutoTraderConfig.BuySmeltablesForHardwoodValue
                    && _logicConnector.GetHardwoodCount() < AutoTraderConfig.SmeltHardwoodTargetValue)
                {
                    int hardwoodYield = _logicConnector.GetCurrentItemHardwoodSmeltYield();
                    if (hardwoodYield > 0 && buyoutPrice <= hardwoodYield * _logicConnector.GetHardwoodUnitValue())
                    {
                        AutoTraderHelpers.PrintDebugMessage("- keep cheap weapon: collecting for hardwood smelting");
                        return false;
                    }
                }

                if (_logicConnector.IsItemTierLowerThan((ItemObject.ItemTiers)AutoTraderConfig.WeaponsArmorTierValue))
                {
                    return CheckBasicSellRequirements(amount, buyoutPrice);
                }
                else
                {
                    AutoTraderHelpers.PrintDebugMessage("- do not sell because tier is too high");
                    return false;
                }
            }
                
            // Special horse rule
            if (_logicConnector.IsHorse())
            {
                if (_logicConnector.IsPackAnimal())
                {
                    // Honor the explicit "protect pack animals" setting even in speed-aware mode.
                    if (AutoTraderConfig.ProtectPackAnimalsValue)
                    {
                        AutoTraderHelpers.PrintDebugMessage("- keep pack animal (protected by setting)");
                        return false;
                    }

                    // Sell pack animals only as herd surplus; otherwise keep them (carry capacity)
                    // and never let the generic price logic dump them.
                    if (_sellPackBudget > 0 && CheckBasicSellRequirements(amount, buyoutPrice))
                    {
                        _sellPackBudget--;
                        AutoTraderHelpers.PrintDebugMessage(" - [mount] SELL pack animal as herd surplus");
                        return true;
                    }
                    AutoTraderHelpers.PrintDebugMessage("- keep pack animal (carry capacity)");
                    return false;
                }

                // Speed-aware: protect ridable mounts, sell only true surplus per category.
                if (AutoTraderConfig.SpeedAwareMountsValue && !_logicConnector.IsPackAnimal())
                {
                    AutoTraderHelpers.PrintDebugMessage(" - [mount] consider SELL '" + _logicConnector.GetItemName() + "' ["
                        + DescribeMountCategory() + "] price=" + buyoutPrice
                        + " sellBudget r/w/n=" + _sellRegularBudget + "/" + _sellWarBudget + "/" + _sellNobleBudget);

                    // Value guard: unique/named mounts are not necessarily in the war/noble item
                    // categories, so category alone would treat them as ordinary riding horses.
                    // Never sell a mount worth more than the configured value.
                    if (AutoTraderConfig.KeepMountsAboveValueValue > 0
                        && buyoutPrice >= AutoTraderConfig.KeepMountsAboveValueValue)
                    {
                        AutoTraderHelpers.PrintDebugMessage("- keep valuable mount (price >= keep-above value)");
                        return false;
                    }

                    bool isNoble = _logicConnector.IsNobleMount();
                    bool isWar = !isNoble && _logicConnector.IsWarMount();
                    int categoryBudget = isNoble ? _sellNobleBudget : (isWar ? _sellWarBudget : _sellRegularBudget);

                    if (categoryBudget > 0 && CheckBasicSellRequirements(amount, buyoutPrice))
                    {
                        if (isNoble)
                        {
                            _sellNobleBudget--;
                        }
                        else if (isWar)
                        {
                            _sellWarBudget--;
                        }
                        else
                        {
                            _sellRegularBudget--;
                        }
                        return true;
                    }
                    AutoTraderHelpers.PrintDebugMessage("- keep mount (needed for party speed or troop upgrades)");
                    return false;
                }

                // Riding mounts in manual mode fall through to the generic price logic, gated by
                // the "Sell horses (manual mode)" toggle in the item filter.
            }

            // Food: the days-of-food reserve is the hard floor, so a big party cannot be sold
            // down to a handful of items. Only the surplus above it may go.
            if (_logicConnector.IsConsumable())
            {
                if (!AutoTraderSpecialRules.MaySellFood(_logicConnector))
                {
                    AutoTraderHelpers.PrintDebugMessage("- [food] keep: only "
                        + _logicConnector.GetFoodDaysRemaining() + " days left, reserve is "
                        + AutoTraderConfig.KeepFoodDaysValue);
                    return false;
                }

                int maxAmountToKeep = _logicConnector.IsItemGrain() ?
                    AutoTraderConfig.KeepGrainsMaxValue : AutoTraderConfig.KeepConsumablesMaxValue;
                if (maxAmountToKeep < amount)
                {
                    AutoTraderHelpers.PrintDebugMessage("- selling because we have too much of this consumable");
                    return CheckBasicSellRequirements(amount, buyoutPrice);
                }
            }

            // Livestock
            if (_logicConnector.IsLivestock())
            {
                // Sell if its treated as junk
                if (AutoTraderConfig.JunkCattleValue)
                    return CheckBasicSellRequirements(amount, buyoutPrice);

                // Otherwise shed only the herd surplus: livestock slows the party down and, unlike
                // pack animals, adds no cargo capacity. The consumables minimum above already
                // protects the food reserve.
                if (_sellLivestockBudget > 0 && CheckBasicSellRequirements(amount, buyoutPrice))
                {
                    _sellLivestockBudget--;
                    AutoTraderHelpers.PrintDebugMessage(" - [mount] SELL livestock as herd surplus");
                    return true;
                }
            }

            // Special hardwood rule
            if (AutoTraderSpecialRules.ShouldKeepHardwood(_logicConnector))
            {
                AutoTraderHelpers.PrintDebugMessage("- do not sell because we dont have enough hardwood");
                return false;                
            }

            if (AutoTraderConfig.SimpleTradingAI)
            {
                int weighted_profit = buyoutPrice - _logicConnector.GetProjectedProfit(buyoutPrice);
                AutoTraderHelpers.PrintDebugMessage(" - weighted_profit: " + weighted_profit.ToString());
                // Check case of no trade rumours
                if (weighted_profit == 0)
                {
                    AutoTraderHelpers.PrintDebugMessage(" - no trade rumours!");
                    float priceFactor = (float)buyoutPrice / averagePrice;
                    if (priceFactor <= 1.2f)
                    {
                        AutoTraderHelpers.PrintDebugMessage("- do not sell because price factor is too low: " + priceFactor.ToString());
                        return false;
                    }

                }
                else if (weighted_profit > buyoutPrice * 0.8f)
                {
                    AutoTraderHelpers.PrintDebugMessage("- do not sell because weighted profit (" + weighted_profit.ToString() + ") is higher than " + (buyoutPrice * 0.8f).ToString());
                    return false;
                }
                    
            }
            else
            {
                // Check threshold
                float priceFactor = (float)buyoutPrice / averagePrice;
                if (priceFactor < (float)AutoTraderConfig.SellThresholdValue / 100.0f)
                {
                    AutoTraderHelpers.PrintDebugMessage("- do not sell because price factor is less than sell threshold: " + priceFactor.ToString());
                    return false;
                }
                    
            }
            
            return CheckBasicSellRequirements(amount, buyoutPrice);
        }

        private bool CheckBasicBuyRequirements(int amount, int price)
        {
            AutoTraderHelpers.PrintDebugMessage("### Checking basic buy requirements ###");
            if (price >= _availablePlayerGold)
            {
                AutoTraderHelpers.PrintDebugMessage("- do not buy because not enough gold: " + price.ToString() + " >= " + _availablePlayerGold.ToString());
                return false;
            }

            if (amount <= 0)
            {
                AutoTraderHelpers.PrintDebugMessage("- do not buy because not enough stock");
                return false;
            }


            // ToDo: Only for carry horses? Nop, also for livestock.
            if (!_logicConnector.IsHorse() && !_logicConnector.IsLivestock() && _logicConnector.GetItemWeight() > _availableInventoryCapacity)
            {
                AutoTraderHelpers.PrintDebugMessage("- do not buy because not enough capacity: " + _logicConnector.GetItemWeight().ToString() + " > " + _availableInventoryCapacity.ToString());
                return false;
            }

            AutoTraderHelpers.PrintDebugMessage("- fulfills basic buy requirements");
            return true;
        }

        private bool CheckBasicSellRequirements(int amount, int price)
        {
            AutoTraderHelpers.PrintDebugMessage("### Checking basic sell requirements ###");
            if (price >= _availableMerchantGold)
            {
                AutoTraderHelpers.PrintDebugMessage(" - not selling because not enough merchant gold");
                string blockedName = _logicConnector.GetItemName();
                if (!_goldBlockedItems.Contains(blockedName))
                {
                    _goldBlockedItems.Add(blockedName);
                }
                return false;
            }
                

            if (amount <= 0)
            {
                AutoTraderHelpers.PrintDebugMessage(" - not selling because not enough stock");
                return false;
            }

            AutoTraderHelpers.PrintDebugMessage("- fulfills basic sell requirements");
            return true;
        }

        private float GetAveragePrice()
        {
            AutoTraderHelpers.PrintDebugMessage("### Getting average price ###");
            float averagePrice = 0;
            float actualDistance = 0;
            float count = 0.0f;

            for (int townId = 0; townId < _logicConnector.GetTownListSize(); townId++)
            {
                bool isInRange = _logicConnector.IsTownInRange(townId, out actualDistance);
                if (AutoTraderConfig.UseWeightedValue // Consider weighted value
                    || AutoTraderConfig.SearchRadiusValue > 999 // Consider the maximum setting
                    || isInRange)
                {
                    if (AutoTraderConfig.UseWeightedValue)
                    {
                        // If its the current town, skip
                        if (_logicConnector.IsCurrentTown(townId))
                        {
                            AutoTraderHelpers.PrintDebugMessage("- continue because current town");
                            continue;
                        }

                        // Weight by distance
                        try
                        {
                            averagePrice += _logicConnector.GetTownItemPrice(townId, true) / actualDistance;
                            averagePrice += _logicConnector.GetTownItemPrice(townId, false) / actualDistance;
                            count += 2.0f / actualDistance;
                        }
                        catch (Exception e)
                        {
                            AutoTraderHelpers.PrintDebugMessage("ERROR: Could not retrieve average price for town: " + e.ToString());
                        }
                    }
                    else
                    {
                        try
                        {
                            averagePrice += _logicConnector.GetTownItemPrice(townId, true);
                            averagePrice += _logicConnector.GetTownItemPrice(townId, false);
                            count += 2.0f;
                        }
                        catch (Exception e)
                        {
                            AutoTraderHelpers.PrintDebugMessage("ERROR: Could not retrieve average price for town: " + e.ToString());
                        }

                    }
                }
            }

            // ToDo: Why the restriction?
            if (_logicConnector.IsItemTradeGood())
            {
                for (int villageId = 0; villageId < _logicConnector.GetVillageListSize(); villageId++)
                {
                    bool isInRange = _logicConnector.IsVillageInRange(villageId, out actualDistance);
                    if (AutoTraderConfig.UseWeightedValue // Consider weighted value
                        || AutoTraderConfig.SearchRadiusValue > 999 // Consider the maximum setting
                        || isInRange)
                    {
                        if (AutoTraderConfig.UseWeightedValue)
                        {
                            // If its the current town, skip
                            if (_logicConnector.IsCurrentVillage(villageId))
                                continue;
                            // Weight by distance
                            try
                            {
                                averagePrice += _logicConnector.GetVillageItemPrice(villageId, true) / actualDistance;
                                averagePrice += _logicConnector.GetVillageItemPrice(villageId, false) / actualDistance;
                                count += 2.0f / actualDistance;
                            }
                            catch (Exception e)
                            {
                                AutoTraderHelpers.PrintDebugMessage("ERROR: Could not retrieve average price for village: " + e.ToString());
                            }
                        }
                        else
                        {
                            try
                            {
                                averagePrice += _logicConnector.GetVillageItemPrice(villageId, true);
                                averagePrice += _logicConnector.GetVillageItemPrice(villageId, false);
                                count += 2.0f;
                            }
                            catch (Exception e)
                            {
                                AutoTraderHelpers.PrintDebugMessage("ERROR: Could not retrieve average price for village: " + e.ToString());
                            }
                        }
                    }
                }
            }

            if (count == 0.0f)
                return _logicConnector.GetAveragePriceFallback();

            averagePrice /= count;
            AutoTraderHelpers.PrintDebugMessage(" - average price: " + averagePrice.ToString());
            return averagePrice > 0 ? averagePrice : _logicConnector.GetAveragePriceFallback();
        }

    }
}
