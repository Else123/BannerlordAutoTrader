using AutoTrader.SpeedTrading;
using Xunit;

namespace SpeedTradingTests
{
    public class PartySpeedAdvisorTests
    {
        // Convenience wrapper so tests read clearly; mirrors the v2 snapshot constructor.
        private static PartySnapshot Snapshot(
            int members, int foot,
            int regular = 0, int war = 0, int noble = 0, int pack = 0, int livestock = 0,
            int warReserve = 0, int nobleReserve = 0,
            float weight = 0f, float capacity = 100000f)
        {
            return new PartySnapshot(members, foot, regular, war, noble, pack, livestock,
                warReserve, nobleReserve, weight, capacity);
        }

        [Fact]
        public void EmptyParty_DoesNothing()
        {
            var advisor = new PartySpeedAdvisor();
            var plan = advisor.Recommend(Snapshot(members: 0, foot: 0));
            Assert.False(plan.HasAction);
        }

        [Fact]
        public void SpeedTarget_EqualsFootSoldierCount()
        {
            var advisor = new PartySpeedAdvisor();
            Assert.Equal(150, advisor.SpeedTarget(Snapshot(members: 200, foot: 150)));
        }

        [Fact]
        public void FootTroopsAndNoMounts_BuysRegularUpToFootCount()
        {
            var advisor = new PartySpeedAdvisor();
            var plan = advisor.Recommend(Snapshot(members: 100, foot: 50));
            Assert.Equal(50, plan.BuyRegular);
            Assert.Equal(0, plan.SellRegular);
        }

        [Fact]
        public void BuyTarget_IsNotCappedByPartySize()
        {
            var advisor = new PartySpeedAdvisor();
            // Almost the whole party is on foot; mounting them all is herd-free, so buy all 55.
            var plan = advisor.Recommend(Snapshot(members: 60, foot: 55));
            Assert.Equal(55, plan.BuyRegular);
        }

        [Fact]
        public void ExcessRegularMounts_SellsRegularSurplus()
        {
            var advisor = new PartySpeedAdvisor();
            // target 50, have 60 regular -> sell 10.
            var plan = advisor.Recommend(Snapshot(members: 100, foot: 50, regular: 60));
            Assert.Equal(0, plan.BuyRegular);
            Assert.Equal(10, plan.SellRegular);
        }

        [Fact]
        public void PackAnimals_DoNotForceMountSales()
        {
            var advisor = new PartySpeedAdvisor();
            // Ridable mounts already equal foot soldiers (50). Even with a huge pack herd, selling
            // mounts would not reduce the herd penalty (pack animals cause it) and would cost the
            // mounted-footmen bonus -> keep the mounts.
            var plan = advisor.Recommend(Snapshot(members: 100, foot: 50, regular: 50, pack: 80));
            Assert.False(plan.HasAction);
            Assert.Equal(0, plan.SellPack); // pack management off by default in this advisor
        }

        [Fact]
        public void PackHerdManagement_SellsPackAnimalsAboveAllowance()
        {
            var advisor = new PartySpeedAdvisor(managePackHerd: true);
            // allowance = members - livestock = 100; 130 pack animals -> sell 30, mounts untouched.
            var plan = advisor.Recommend(Snapshot(members: 100, foot: 50, regular: 50, pack: 130));
            Assert.Equal(30, plan.SellPack);
            Assert.Equal(0, plan.SellRegular);
        }

        [Fact]
        public void HerdManagement_ShedsLivestockBeforePackAnimals()
        {
            var advisor = new PartySpeedAdvisor(managePackHerd: true, manageLivestockHerd: true);
            // herd = 70 pack + 40 livestock = 110 vs 100 members -> excess 10. Livestock carries no
            // cargo capacity, so it is shed first and the pack animals stay.
            var plan = advisor.Recommend(Snapshot(members: 100, foot: 50, regular: 50, pack: 70, livestock: 40));
            Assert.Equal(10, plan.SellLivestock);
            Assert.Equal(0, plan.SellPack);
        }

        [Fact]
        public void LivestockHerdManagement_RespectsTheFoodReserve()
        {
            var advisor = new PartySpeedAdvisor(manageLivestockHerd: true, livestockReserve: 20);
            // excess 30, but only 25 livestock may go (45 - 20 reserve).
            var plan = advisor.Recommend(Snapshot(members: 100, foot: 50, pack: 85, livestock: 45));
            Assert.Equal(25, plan.SellLivestock);
        }

        [Fact]
        public void LivestockHerdManagement_PackAnimalsCoverTheRemainingExcess()
        {
            var advisor = new PartySpeedAdvisor(managePackHerd: true, manageLivestockHerd: true, livestockReserve: 20);
            // excess 30: 25 livestock (down to the reserve) then 5 pack animals; capacity is ample.
            var plan = advisor.Recommend(Snapshot(members: 100, foot: 50, pack: 85, livestock: 45));
            Assert.Equal(25, plan.SellLivestock);
            Assert.Equal(5, plan.SellPack);
        }

        [Fact]
        public void PackHerdManagement_KeepsPackAnimalsWithinAllowance()
        {
            var advisor = new PartySpeedAdvisor(managePackHerd: true);
            var plan = advisor.Recommend(Snapshot(members: 100, foot: 50, regular: 50, pack: 40));
            Assert.Equal(0, plan.SellPack);
        }

        [Fact]
        public void PackAnimals_AreNotBoughtWhileTheCargoFits()
        {
            var advisor = new PartySpeedAdvisor(cargoUtilizationPercent: 90);
            // 1000 of 5000 capacity used - no shortfall, so no mules regardless of headroom.
            var plan = advisor.Recommend(Snapshot(members: 320, foot: 0, pack: 10,
                weight: 1000f, capacity: 5000f));
            Assert.Equal(0, plan.BuyPack);
        }

        [Fact]
        public void PackAnimals_AreBoughtOnlyForTheActualShortfall()
        {
            var advisor = new PartySpeedAdvisor(cargoUtilizationPercent: 100);
            // 250 over capacity, each pack animal carries 100 -> 3 cover it.
            var plan = advisor.Recommend(Snapshot(members: 320, foot: 0, pack: 10,
                weight: 5250f, capacity: 5000f));
            Assert.Equal(3, plan.BuyPack);
        }

        [Fact]
        public void PackAnimals_AreCappedByTheHerdHeadroom()
        {
            var advisor = new PartySpeedAdvisor(cargoUtilizationPercent: 100);
            // Huge shortfall, but pack + livestock may not exceed the 100 members: 90 + 5 -> 5 left.
            var plan = advisor.Recommend(Snapshot(members: 100, foot: 0, pack: 90, livestock: 5,
                weight: 100000f, capacity: 1000f));
            Assert.Equal(5, plan.BuyPack);
        }

        [Fact]
        public void RidableSurplus_IsSoldEvenWhenOverburdened()
        {
            var advisor = new PartySpeedAdvisor();
            // Overburdened (weight > capacity): the surplus mounts are pure herd burden beyond the
            // foot soldiers, so they must still go - otherwise the party can never recover.
            var plan = advisor.Recommend(Snapshot(members: 320, foot: 158, regular: 489,
                weight: 70000f, capacity: 65000f));
            Assert.Equal(331, plan.SellRegular);
        }

        [Fact]
        public void PackHerdManagement_DoesNotSellIntoOverburden()
        {
            var advisor = new PartySpeedAdvisor(managePackHerd: true);
            // 30 pack animals above the allowance, but only 250 spare capacity: each pack animal
            // carries 100, so at most 2 may go before the party would be overburdened.
            var plan = advisor.Recommend(Snapshot(members: 100, foot: 50, regular: 50, pack: 130,
                weight: 750f, capacity: 1000f));
            Assert.Equal(2, plan.SellPack);
        }

        [Fact]
        public void PackHerdManagement_FullCargoBlocksPackSales()
        {
            var advisor = new PartySpeedAdvisor(managePackHerd: true);
            // No spare capacity at all -> keep every pack animal despite the herd penalty.
            var plan = advisor.Recommend(Snapshot(members: 100, foot: 50, regular: 50, pack: 130,
                weight: 1000f, capacity: 1000f));
            Assert.Equal(0, plan.SellPack);
        }

        [Fact]
        public void WarMounts_ProtectedByUpgradeReserve()
        {
            var advisor = new PartySpeedAdvisor();
            // all-cavalry party: foot 0 -> target 0, so all 40 ridable are surplus, but 20 war are
            // reserved for upgrades -> only 10 war sellable. Noble selling off by default.
            var plan = advisor.Recommend(Snapshot(members: 100, foot: 0, war: 30, noble: 10,
                warReserve: 20, nobleReserve: 5));
            Assert.Equal(0, plan.SellRegular);
            Assert.Equal(10, plan.SellWar);
            Assert.Equal(0, plan.SellNoble);
        }

        [Fact]
        public void NobleMounts_SoldOnlyWhenAllowed_AndAboveReserve()
        {
            var advisor = new PartySpeedAdvisor(allowNobleSell: true);
            var plan = advisor.Recommend(Snapshot(members: 100, foot: 0, war: 30, noble: 10,
                warReserve: 20, nobleReserve: 5));
            Assert.Equal(10, plan.SellWar);
            Assert.Equal(5, plan.SellNoble); // 10 noble - 5 reserve
        }

        [Fact]
        public void SellOrder_RegularBeforeWarBeforeNoble()
        {
            var advisor = new PartySpeedAdvisor(allowNobleSell: true);
            // foot 0 -> target 0. ridable = 5 reg + 5 war + 5 noble = 15, all surplus, no reserves.
            var plan = advisor.Recommend(Snapshot(members: 100, foot: 0, regular: 5, war: 5, noble: 5));
            Assert.Equal(5, plan.SellRegular);
            Assert.Equal(5, plan.SellWar);
            Assert.Equal(5, plan.SellNoble);
        }
    }
}
