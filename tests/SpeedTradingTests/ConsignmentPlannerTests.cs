using AutoTrader.Warehouse;
using Xunit;

namespace SpeedTradingTests
{
    public class ConsignmentPlannerTests
    {
        [Fact]
        public void DailyBudget_IsTheConfiguredShareOfTownGold()
        {
            var planner = new ConsignmentPlanner(dailySharePercent: 25);
            Assert.Equal(2500, planner.DailyBudget(10000));
        }

        [Fact]
        public void DailyBudget_IsZeroWithoutGoldOrShare()
        {
            Assert.Equal(0, new ConsignmentPlanner(25).DailyBudget(0));
            Assert.Equal(0, new ConsignmentPlanner(0).DailyBudget(10000));
        }

        [Fact]
        public void CanSellUnit_StopsWhenTheBudgetIsSpent()
        {
            var planner = new ConsignmentPlanner(25);
            Assert.True(planner.CanSellUnit(unitPrice: 100, remainingBudget: 100));
            Assert.False(planner.CanSellUnit(unitPrice: 101, remainingBudget: 100));
        }

        [Fact]
        public void CanSellUnit_KeepsItemsBelowTheMinimumPrice()
        {
            var planner = new ConsignmentPlanner(25, minimumUnitPrice: 50);
            Assert.False(planner.CanSellUnit(unitPrice: 49, remainingBudget: 10000));
            Assert.True(planner.CanSellUnit(unitPrice: 50, remainingBudget: 10000));
        }

        [Fact]
        public void CanSellUnit_RejectsWorthlessItems()
        {
            var planner = new ConsignmentPlanner(25);
            Assert.False(planner.CanSellUnit(unitPrice: 0, remainingBudget: 10000));
        }
    }
}
