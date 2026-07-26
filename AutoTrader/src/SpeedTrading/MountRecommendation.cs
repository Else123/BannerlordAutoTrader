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

        /// <summary>
        /// Applies the mount-management mode. Only the ridable mounts are traded for party speed,
        /// so those drop out when the mode is off; pack animals and livestock have their own
        /// settings and their own reasons (cargo capacity, herd size) and are kept either way.
        ///
        /// This lives here, not in the trade loop, so it can be unit tested - turning the mode off
        /// used to zero every budget, which silently stopped pack animals from being bought too.
        /// </summary>
        public MountRecommendation ForMountMode(bool speedAwareMounts)
        {
            if (speedAwareMounts)
            {
                return this;
            }
            return new MountRecommendation(0, BuyPack, 0, 0, 0, SellPack, SellLivestock, Reason);
        }
    }
}
