namespace AutoTrader.SpeedTrading
{
    /// <summary>
    /// Per-category mount trading plan produced by <see cref="PartySpeedAdvisor"/>.
    /// Only regular riding horses are bought (for speed); war/noble mounts are protected
    /// by their upgrade reserve and only sold as true surplus.
    /// </summary>
    public readonly struct MountRecommendation
    {
        /// <summary>Regular riding horses to buy for the speed target (>= 0).</summary>
        public readonly int BuyRegular;

        /// <summary>Pack animals to buy because the cargo does not fit (>= 0).</summary>
        public readonly int BuyPack;

        /// <summary>Regular riding horses to sell as surplus (>= 0).</summary>
        public readonly int SellRegular;

        /// <summary>War mounts to sell as surplus, above the upgrade reserve (>= 0).</summary>
        public readonly int SellWar;

        /// <summary>Noble mounts to sell as surplus, above the upgrade reserve (>= 0).</summary>
        public readonly int SellNoble;

        /// <summary>Pack animals (mules) to sell as herd surplus (>= 0).</summary>
        public readonly int SellPack;

        /// <summary>Livestock (cattle/sheep) to sell as herd surplus (>= 0).</summary>
        public readonly int SellLivestock;

        /// <summary>Human-readable rationale (for log/debug).</summary>
        public readonly string Reason;

        public MountRecommendation(int buyRegular, int buyPack, int sellRegular, int sellWar, int sellNoble,
            int sellPack, int sellLivestock, string reason)
        {
            BuyRegular = buyRegular;
            BuyPack = buyPack;
            SellRegular = sellRegular;
            SellWar = sellWar;
            SellNoble = sellNoble;
            SellPack = sellPack;
            SellLivestock = sellLivestock;
            Reason = reason;
        }

        public bool HasAction => BuyRegular > 0 || BuyPack > 0 || SellRegular > 0 || SellWar > 0
            || SellNoble > 0 || SellPack > 0 || SellLivestock > 0;
    }
}
