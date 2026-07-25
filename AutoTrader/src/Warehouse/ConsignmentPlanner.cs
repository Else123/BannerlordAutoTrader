using System;

namespace AutoTrader.Warehouse
{
    /// <summary>
    /// Decides how much of a town warehouse may be sold into the local market per day.
    /// Engine-free so it can be unit tested without a running game.
    ///
    /// The point of consignment is throughput, not a better price: a town merchant only has
    /// so much gold, so the warehouse drains over several days instead of in one dump. The
    /// daily budget is a share of the merchant's gold, which also lets the town recover.
    /// </summary>
    public sealed class ConsignmentPlanner
    {
        private readonly int _dailySharePercent;
        private readonly int _minimumUnitPrice;

        public ConsignmentPlanner(int dailySharePercent, int minimumUnitPrice = 0)
        {
            _dailySharePercent = dailySharePercent;
            _minimumUnitPrice = minimumUnitPrice;
        }

        /// <summary>Gold the town may spend on the warehouse today.</summary>
        public int DailyBudget(int townGold)
        {
            if (townGold <= 0 || _dailySharePercent <= 0)
            {
                return 0;
            }
            return Math.Max(0, townGold / 100 * _dailySharePercent);
        }

        /// <summary>
        /// Whether one more unit at this price may be sold from the remaining daily budget.
        /// Junk below the minimum price is left in the warehouse rather than flooding the market.
        /// </summary>
        public bool CanSellUnit(int unitPrice, int remainingBudget)
        {
            if (unitPrice <= 0 || unitPrice < _minimumUnitPrice)
            {
                return false;
            }
            return unitPrice <= remainingBudget;
        }
    }
}
