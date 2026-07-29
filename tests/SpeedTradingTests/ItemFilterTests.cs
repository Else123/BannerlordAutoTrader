using AutoTrader.Trading;
using Xunit;

namespace SpeedTradingTests
{
    public class ItemFilterTests
    {
        private const bool Buying = true;
        private const bool Selling = false;

        private static FilterSettings Settings(bool sellSmithing = false, bool speedAware = true,
            bool horses = false, bool armor = true, bool weapons = true, bool smeltFodder = false,
            bool livestock = true, bool goods = true, bool consumables = true)
        {
            return new FilterSettings(sellSmithing, speedAware, horses, armor, weapons, smeltFodder,
                livestock, goods, consumables);
        }

        private static FilterItem Item(int amount = 1, bool locked = false, bool traded = false,
            bool smithing = false, bool horse = false, bool armor = false, bool weapon = false,
            bool livestock = false, bool tradeGood = false, bool consumable = false)
        {
            return new FilterItem(amount, locked, traded, smithing, horse, armor, weapon, livestock,
                tradeGood, consumable);
        }

        private static bool Filtered(FilterItem item, FilterSettings s, bool buying)
        {
            string reason;
            return ItemFilter.IsFiltered(item, s, buying, out reason);
        }

        // --- the two failures this gate has caused ---------------------------

        [Fact]
        public void HorsesReachTheMountRulesWhileSpeedAwareTradingIsOn()
        {
            // Excluding horses here made speed-aware mount purchases impossible: the per-mount
            // rules were never reached, whatever the settings said.
            Assert.False(Filtered(Item(horse: true), Settings(speedAware: true, horses: false), Buying));
            Assert.False(Filtered(Item(horse: true), Settings(speedAware: true, horses: false), Selling));
        }

        [Fact]
        public void InManualModeTheHorseToggleDecides()
        {
            Assert.True(Filtered(Item(horse: true), Settings(speedAware: false, horses: false), Buying));
            Assert.False(Filtered(Item(horse: true), Settings(speedAware: false, horses: true), Buying));
        }

        [Fact]
        public void WeaponsReachTheBuyRulesWhileCollectingSmeltFodder()
        {
            // With weapon buying off but fodder collection on, weapons must still get through -
            // otherwise the hardwood feature can never see a candidate.
            Assert.False(Filtered(Item(weapon: true), Settings(weapons: false, smeltFodder: true), Buying));
            Assert.True(Filtered(Item(weapon: true), Settings(weapons: false, smeltFodder: false), Buying));
        }

        [Fact]
        public void SmeltFodderCollectionDoesNotAffectSelling()
        {
            Assert.True(Filtered(Item(weapon: true), Settings(weapons: false, smeltFodder: true), Selling));
        }

        // --- the plain gates ---------------------------------------------------

        [Fact]
        public void OutOfStockLockedAndAlreadyTradedItemsAreSkipped()
        {
            Assert.True(Filtered(Item(amount: 0), Settings(), Buying));
            Assert.True(Filtered(Item(locked: true), Settings(), Selling));
            Assert.True(Filtered(Item(traded: true), Settings(), Selling));
        }

        [Fact]
        public void SmithingMaterialsAreKeptForTheForgeUnlessSellingThemIsAllowed()
        {
            Assert.True(Filtered(Item(smithing: true), Settings(sellSmithing: false), Selling));
            Assert.False(Filtered(Item(smithing: true), Settings(sellSmithing: true), Selling));
            // Buying materials is not governed by that setting.
            Assert.False(Filtered(Item(smithing: true, tradeGood: true), Settings(sellSmithing: false), Buying));
        }

        [Fact]
        public void CategoryTogglesApply()
        {
            Assert.True(Filtered(Item(armor: true), Settings(armor: false), Buying));
            Assert.True(Filtered(Item(livestock: true), Settings(livestock: false), Buying));
            Assert.True(Filtered(Item(tradeGood: true), Settings(goods: false), Buying));
            Assert.True(Filtered(Item(consumable: true), Settings(consumables: false), Buying));
        }

        [Fact]
        public void FoodIsNotCaughtByTheTradeGoodToggle()
        {
            // Food counts as a trade good; turning goods off must not starve the restock path.
            Assert.False(Filtered(Item(tradeGood: true, consumable: true),
                Settings(goods: false, consumables: true), Buying));
        }
    }
}
