using AutoTrader.Trading;
using Xunit;

namespace SpeedTradingTests
{
    public class BuyGateTests
    {
        private static BuySettings Settings(bool buyPack = true, bool speedAware = true, bool fleet = false,
            float availableCapacity = 1000f, float itemWeight = 1f, bool buyWeapons = false,
            bool collectFodder = false, bool buyHardwood = false, int hardwoodCount = 0, int hardwoodTarget = 0,
            int hardwoodUnitValue = 10, bool restockFood = true, int foodDays = 30, int keepFoodDays = 14,
            int keepGrainsMax = 100, int keepConsumablesMax = 20)
        {
            return new BuySettings(buyPack, speedAware, fleet, availableCapacity, itemWeight, buyWeapons,
                collectFodder, buyHardwood, hardwoodCount, hardwoodTarget, hardwoodUnitValue, restockFood,
                foodDays, keepFoodDays, keepGrainsMax, keepConsumablesMax);
        }

        private static ItemView Mount(bool pack = false, bool war = false, bool noble = false, int price = 100)
        {
            return new ItemView(price, 1, 1, false, false, false, 0, false,
                true, pack, war, noble, false, false, false);
        }

        private static ItemView Weapon(int price = 100, int smeltYield = 0)
        {
            return new ItemView(price, 1, 1, false, true, false, smeltYield, false,
                false, false, false, false, false, false, false);
        }

        private static ItemView Hardwood(int price = 10)
        {
            return new ItemView(price, 1, 1, false, false, false, 0, true,
                false, false, false, false, false, false, false);
        }

        private static ItemView Food(bool grain = false)
        {
            return new ItemView(10, 1, 1, false, false, false, 0, false,
                false, false, false, false, true, grain, false);
        }

        // --- the regression that bought 900 mules -----------------------------

        [Fact]
        public void PackAnimalsAreOnlyBoughtAgainstTheirBudget()
        {
            // The inherited rule was unbounded, so it bought hundreds in a single run.
            Assert.Equal(BuyVerdict.Skip,
                BuyGate.Evaluate(Mount(pack: true), Settings(), new BuyBudgets(0, 0), 0).Verdict);

            var d = BuyGate.Evaluate(Mount(pack: true), Settings(), new BuyBudgets(0, 5), 0);
            Assert.Equal(BuyVerdict.Buy, d.Verdict);
            Assert.Equal(BuyBudget.PackAnimal, d.Budget);
        }

        [Fact]
        public void PackAnimalsCanBeTurnedOffEntirely()
        {
            Assert.Equal(BuyVerdict.Skip,
                BuyGate.Evaluate(Mount(pack: true), Settings(buyPack: false), new BuyBudgets(0, 5), 0).Verdict);
        }

        // --- riding mounts ------------------------------------------------------

        [Fact]
        public void RidingMountsAreBoughtUpToTheSpeedBudget()
        {
            var d = BuyGate.Evaluate(Mount(), Settings(), new BuyBudgets(3, 0), 0);
            Assert.Equal(BuyBudget.RegularMount, d.Budget);
            Assert.Equal(BuyVerdict.Skip, BuyGate.Evaluate(Mount(), Settings(), new BuyBudgets(0, 0), 0).Verdict);
        }

        [Fact]
        public void WarAndNobleMountsAreNotBoughtForSpeed()
        {
            Assert.Equal(BuyVerdict.Skip,
                BuyGate.Evaluate(Mount(war: true), Settings(), new BuyBudgets(9, 0), 0).Verdict);
            Assert.Equal(BuyVerdict.Skip,
                BuyGate.Evaluate(Mount(noble: true), Settings(), new BuyBudgets(9, 0), 0).Verdict);
        }

        [Fact]
        public void WithMountManagementOffNoRidingMountIsBought_ButPackAnimalsStillAre()
        {
            // Zeroing every budget for manual mode once stopped pack animals from being bought too.
            Assert.Equal(BuyVerdict.Skip,
                BuyGate.Evaluate(Mount(), Settings(speedAware: false), new BuyBudgets(9, 9), 0).Verdict);
            Assert.Equal(BuyVerdict.Buy,
                BuyGate.Evaluate(Mount(pack: true), Settings(speedAware: false), new BuyBudgets(0, 9), 0).Verdict);
        }

        [Fact]
        public void FleetModeStopsBuyingMountsThatDoNotFit()
        {
            var d = BuyGate.Evaluate(Mount(), Settings(fleet: true, availableCapacity: 0.5f, itemWeight: 1f),
                new BuyBudgets(5, 0), 0);
            Assert.Equal(BuyVerdict.Skip, d.Verdict);
        }

        // --- smelt fodder and weapons -------------------------------------------

        [Fact]
        public void CheapSmeltFodderIsBoughtWhileHardwoodIsShort()
        {
            var d = BuyGate.Evaluate(Weapon(price: 20, smeltYield: 3),
                Settings(collectFodder: true, hardwoodCount: 0, hardwoodTarget: 100, hardwoodUnitValue: 10),
                new BuyBudgets(0, 0), 0);
            Assert.Equal(BuyVerdict.Buy, d.Verdict);
        }

        [Fact]
        public void SmeltFodderStopsAtTheHardwoodTarget()
        {
            var d = BuyGate.Evaluate(Weapon(price: 20, smeltYield: 3),
                Settings(collectFodder: true, hardwoodCount: 100, hardwoodTarget: 100),
                new BuyBudgets(0, 0), 0);
            Assert.Equal(BuyVerdict.Skip, d.Verdict);
        }

        [Fact]
        public void AWeaponIsNotBoughtByThePriceLogicWhileWeaponBuyingIsOff()
        {
            var d = BuyGate.Evaluate(Weapon(price: 900), Settings(buyWeapons: false), new BuyBudgets(0, 0), 0);
            Assert.Equal(BuyVerdict.Skip, d.Verdict);
        }

        [Fact]
        public void WithWeaponBuyingOnThePriceLogicDecides()
        {
            var d = BuyGate.Evaluate(Weapon(price: 900), Settings(buyWeapons: true), new BuyBudgets(0, 0), 0);
            Assert.Equal(BuyVerdict.CheckPrice, d.Verdict);
        }

        // --- supplies --------------------------------------------------------------

        [Fact]
        public void HardwoodIsRestockedWhileBelowTheTarget()
        {
            Assert.Equal(BuyVerdict.Buy, BuyGate.Evaluate(Hardwood(),
                Settings(buyHardwood: true, hardwoodCount: 10, hardwoodTarget: 100), new BuyBudgets(0, 0), 0).Verdict);
            Assert.Equal(BuyVerdict.CheckPrice, BuyGate.Evaluate(Hardwood(),
                Settings(buyHardwood: true, hardwoodCount: 100, hardwoodTarget: 100), new BuyBudgets(0, 0), 0).Verdict);
        }

        [Fact]
        public void FoodIsRestockedWhileTheReserveIsShort()
        {
            var d = BuyGate.Evaluate(Food(), Settings(foodDays: 6, keepFoodDays: 14), new BuyBudgets(0, 0), 0);
            Assert.Equal(BuyVerdict.Buy, d.Verdict);
        }

        [Fact]
        public void FoodIsNotStockpiledBeyondTheKeepAmount()
        {
            var d = BuyGate.Evaluate(Food(), Settings(foodDays: 6, keepConsumablesMax: 20),
                new BuyBudgets(0, 0), ownAmount: 20);
            Assert.Equal(BuyVerdict.Skip, d.Verdict);
        }

        [Fact]
        public void OrdinaryGoodsGoToThePriceLogic()
        {
            var goods = new ItemView(50, 10, 1, false, false, false, 0, false,
                false, false, false, false, false, false, false);
            Assert.Equal(BuyVerdict.CheckPrice,
                BuyGate.Evaluate(goods, Settings(), new BuyBudgets(0, 0), 0).Verdict);
        }
    }
}
