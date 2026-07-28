using AutoTrader.Trading;
using Xunit;

namespace SpeedTradingTests
{
    public class SellGateTests
    {
        private static SellSettings Settings(int sellUpToTier = 2, bool keepCrafted = true,
            bool collectFodder = false, int hardwoodCount = 0, int hardwoodTarget = 0, int hardwoodUnitValue = 10,
            bool speedAware = true, bool protectPack = false, int keepMountsAbove = 2000,
            int foodDays = 30, int keepFoodDays = 14, int keepGrainsMax = 100, int keepConsumablesMax = 20,
            bool junkCattle = false)
        {
            return new SellSettings(sellUpToTier, keepCrafted, collectFodder, hardwoodCount, hardwoodTarget,
                hardwoodUnitValue, speedAware, protectPack, keepMountsAbove, foodDays, keepFoodDays,
                keepGrainsMax, keepConsumablesMax, junkCattle);
        }

        private static SellBudgets Budgets(int regular = 0, int war = 0, int noble = 0, int pack = 0, int livestock = 0)
        {
            return new SellBudgets(regular, war, noble, pack, livestock);
        }

        private static ItemView Weapon(int tier, int price = 100, bool playerCrafted = false, int smeltYield = 0)
        {
            return new ItemView(price, 1, tier, false, true, playerCrafted, smeltYield, false,
                false, false, false, false, false, false, false);
        }

        private static ItemView Armor(int tier, int price = 100)
        {
            return new ItemView(price, 1, tier, true, false, false, 0, false,
                false, false, false, false, false, false, false);
        }

        private static ItemView Mount(int price, bool pack = false, bool war = false, bool noble = false)
        {
            return new ItemView(price, 1, 1, false, false, false, 0, false,
                true, pack, war, noble, false, false, false);
        }

        private static ItemView Food(int amount, bool grain = false)
        {
            return new ItemView(10, amount, 1, false, false, false, 0, false,
                false, false, false, false, true, grain, false);
        }

        // --- the regression that stopped selling entirely ---------------------

        [Fact]
        public void AVanillaWeaponIsNotTreatedAsPlayerCrafted()
        {
            // Most vanilla weapons have a crafting design; only the player's own smithing counts.
            // Testing the design instead protected nearly the whole armoury and stopped selling.
            var d = SellGate.Evaluate(Weapon(tier: 2, playerCrafted: false), Settings(keepCrafted: true), Budgets());
            Assert.Equal(SellVerdict.Sell, d.Verdict);
        }

        [Fact]
        public void APlayerCraftedWeaponIsKeptWhenAsked()
        {
            var d = SellGate.Evaluate(Weapon(tier: 2, playerCrafted: true), Settings(keepCrafted: true), Budgets());
            Assert.Equal(SellVerdict.Keep, d.Verdict);
        }

        [Fact]
        public void APlayerCraftedWeaponIsSoldWhenCraftingToSell()
        {
            var d = SellGate.Evaluate(Weapon(tier: 2, playerCrafted: true), Settings(keepCrafted: false), Budgets());
            Assert.Equal(SellVerdict.Sell, d.Verdict);
        }

        // --- equipment tier ---------------------------------------------------

        [Fact]
        public void EquipmentAboveTheTierLimitIsKept()
        {
            Assert.Equal(SellVerdict.Keep, SellGate.Evaluate(Weapon(tier: 5), Settings(sellUpToTier: 2), Budgets()).Verdict);
            Assert.Equal(SellVerdict.Keep, SellGate.Evaluate(Armor(tier: 5), Settings(sellUpToTier: 2), Budgets()).Verdict);
        }

        [Fact]
        public void EquipmentAtOrBelowTheTierLimitIsSold()
        {
            Assert.Equal(SellVerdict.Sell, SellGate.Evaluate(Weapon(tier: 2), Settings(sellUpToTier: 2), Budgets()).Verdict);
            Assert.Equal(SellVerdict.Sell, SellGate.Evaluate(Armor(tier: 1), Settings(sellUpToTier: 2), Budgets()).Verdict);
        }

        // --- smelt fodder ------------------------------------------------------

        [Fact]
        public void CheapSmeltFodderIsKeptWhileHardwoodIsShort()
        {
            var item = Weapon(tier: 1, price: 20, smeltYield: 3);
            var d = SellGate.Evaluate(item, Settings(collectFodder: true, hardwoodCount: 0, hardwoodTarget: 100,
                hardwoodUnitValue: 10), Budgets());
            Assert.Equal(SellVerdict.Keep, d.Verdict);
        }

        [Fact]
        public void AValuableWeaponIsSoldEvenThoughItWouldYieldHardwood()
        {
            // 3 wood at 10 each is worth 30; a 900 gold weapon is not smelt fodder.
            var item = Weapon(tier: 1, price: 900, smeltYield: 3);
            var d = SellGate.Evaluate(item, Settings(collectFodder: true, hardwoodCount: 0, hardwoodTarget: 100,
                hardwoodUnitValue: 10), Budgets());
            Assert.Equal(SellVerdict.Sell, d.Verdict);
        }

        // --- food ---------------------------------------------------------------

        [Fact]
        public void FoodIsKeptWhileTheReserveIsNotCovered()
        {
            var d = SellGate.Evaluate(Food(amount: 500), Settings(foodDays: 10, keepFoodDays: 14), Budgets());
            Assert.Equal(SellVerdict.Keep, d.Verdict);
        }

        [Fact]
        public void SurplusFoodIsSoldOnceTheReserveIsCovered()
        {
            var d = SellGate.Evaluate(Food(amount: 500), Settings(foodDays: 30, keepFoodDays: 14,
                keepConsumablesMax: 20), Budgets());
            Assert.Equal(SellVerdict.Sell, d.Verdict);
        }

        [Fact]
        public void FoodWithinTheKeepAmountFallsThroughToThePriceLogic()
        {
            var d = SellGate.Evaluate(Food(amount: 10), Settings(foodDays: 30, keepConsumablesMax: 20), Budgets());
            Assert.Equal(SellVerdict.CheckPrice, d.Verdict);
        }

        // --- animals -------------------------------------------------------------

        [Fact]
        public void AValuableMountIsNeverSold()
        {
            var d = SellGate.Evaluate(Mount(price: 5000), Settings(keepMountsAbove: 2000), Budgets(regular: 10));
            Assert.Equal(SellVerdict.Keep, d.Verdict);
        }

        [Fact]
        public void SurplusMountsAreSoldAgainstTheirOwnBudget()
        {
            Assert.Equal(SellBudget.RegularMount,
                SellGate.Evaluate(Mount(100), Settings(), Budgets(regular: 1)).Budget);
            Assert.Equal(SellBudget.WarMount,
                SellGate.Evaluate(Mount(100, war: true), Settings(), Budgets(war: 1)).Budget);
            Assert.Equal(SellBudget.NobleMount,
                SellGate.Evaluate(Mount(100, noble: true), Settings(), Budgets(noble: 1)).Budget);
        }

        [Fact]
        public void MountsWithoutBudgetAreKept()
        {
            Assert.Equal(SellVerdict.Keep, SellGate.Evaluate(Mount(100), Settings(), Budgets()).Verdict);
        }

        [Fact]
        public void PackAnimalsAreKeptUnlessTheyAreHerdSurplus()
        {
            Assert.Equal(SellVerdict.Keep,
                SellGate.Evaluate(Mount(100, pack: true), Settings(), Budgets()).Verdict);
            Assert.Equal(SellVerdict.Keep,
                SellGate.Evaluate(Mount(100, pack: true), Settings(protectPack: true), Budgets(pack: 5)).Verdict);
            Assert.Equal(SellBudget.PackAnimal,
                SellGate.Evaluate(Mount(100, pack: true), Settings(), Budgets(pack: 5)).Budget);
        }

        [Fact]
        public void InManualModeRidingMountsGoToThePriceLogic()
        {
            var d = SellGate.Evaluate(Mount(100), Settings(speedAware: false), Budgets());
            Assert.Equal(SellVerdict.CheckPrice, d.Verdict);
        }

        [Fact]
        public void PackAnimalsStayManagedInManualMode()
        {
            // Their own settings govern them, not the mount mode.
            var d = SellGate.Evaluate(Mount(100, pack: true), Settings(speedAware: false), Budgets(pack: 3));
            Assert.Equal(SellBudget.PackAnimal, d.Budget);
        }

        // --- ordinary goods -------------------------------------------------------

        [Fact]
        public void PlainTradeGoodsAreLeftToThePriceLogic()
        {
            var goods = new ItemView(50, 10, 1, false, false, false, 0, false,
                false, false, false, false, false, false, false);
            Assert.Equal(SellVerdict.CheckPrice, SellGate.Evaluate(goods, Settings(), Budgets()).Verdict);
        }
    }
}
