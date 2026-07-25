namespace AutoTrader.SpeedTrading
{
    /// <summary>Result of the speed optimization: how many mounts to buy/sell.</summary>
    public readonly struct MountRecommendation
    {
        /// <summary>Number of mounts that should additionally be bought (>= 0).</summary>
        public readonly int BuyCount;

        /// <summary>Number of surplus mounts that should be sold (>= 0).</summary>
        public readonly int SellCount;

        /// <summary>Human-readable rationale (for log/debug).</summary>
        public readonly string Reason;

        public MountRecommendation(int buyCount, int sellCount, string reason)
        {
            BuyCount = buyCount;
            SellCount = sellCount;
            Reason = reason;
        }

        public bool HasAction => BuyCount > 0 || SellCount > 0;
    }
}
