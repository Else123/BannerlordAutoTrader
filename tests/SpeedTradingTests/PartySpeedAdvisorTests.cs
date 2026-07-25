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
            int warReserve = 0, int nobleReserve = 0)
        {
            return new PartySnapshot(members, foot, regular, war, noble, pack, livestock,
                warReserve, nobleReserve, 0f, 0f);
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
