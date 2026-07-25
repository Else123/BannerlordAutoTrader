namespace AutoTrader.SpeedTrading
{
    /// <summary>Ergebnis der Speed-Optimierung: wie viele Reittiere kaufen/verkaufen.</summary>
    public readonly struct MountRecommendation
    {
        /// <summary>Anzahl Reittiere, die zusaetzlich gekauft werden sollten (>= 0).</summary>
        public readonly int BuyCount;

        /// <summary>Anzahl ueberzaehliger Reittiere, die verkauft werden sollten (>= 0).</summary>
        public readonly int SellCount;

        /// <summary>Menschenlesbare Begruendung (fuer Log/Debug).</summary>
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

