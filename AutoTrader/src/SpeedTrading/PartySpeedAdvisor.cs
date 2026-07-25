using System;

namespace AutoTrader.SpeedTrading
{
    /// <summary>
    /// Core of the speed idea: decides purely arithmetically how many spare mounts the
    /// party should keep to maximize map speed -- without running into the herd penalty.
    ///
    /// Underlying vanilla mechanic (DefaultPartySpeedCalculatingModel):
    ///  - Every foot soldier that can mount a spare horse grants a speed bonus, so the
    ///    optimum is ~1 spare mount per foot soldier.
    ///  - Mounts/pack animals above a threshold (~ MemberCount * 1.05) create an
    ///    increasing herd penalty.
    /// Source of the figures: community party-speed analyses; the exact factors should be
    /// verified against decompiled v1.4.7 (see README.md).
    /// </summary>
    public sealed class PartySpeedAdvisor
    {
        /// <summary>Safety margin below the herd threshold (number of animals).</summary>
        private const int HerdSafetyMargin = 2;

        /// <summary>Multiplier for the herd threshold relative to party size.</summary>
        private readonly float _herdThresholdFactor;

        public PartySpeedAdvisor(float herdThresholdFactor = 1.05f)
        {
            _herdThresholdFactor = herdThresholdFactor;
        }

        /// <summary>
        /// Number of spare mounts below which no herd penalty occurs.
        /// </summary>
        public int HerdThreshold(in PartySnapshot p)
        {
            return (int)Math.Floor(p.MemberCount * _herdThresholdFactor);
        }

        /// <summary>
        /// Ideal number of spare mounts: enough to mount all foot soldiers, but safely
        /// below the herd threshold.
        /// </summary>
        public int TargetSpareMounts(in PartySnapshot p)
        {
            int safeCap = Math.Max(0, HerdThreshold(in p) - HerdSafetyMargin);
            int desiredForMounting = p.FootTroopCount;
            return Math.Min(desiredForMounting, safeCap);
        }

        /// <summary>Derives a buy/sell recommendation from the snapshot.</summary>
        public MountRecommendation Recommend(in PartySnapshot p)
        {
            int target = TargetSpareMounts(in p);
            int threshold = HerdThreshold(in p);

            // Too many animals -> herd penalty: shed the surplus above the threshold.
            if (p.SpareMountCount > threshold)
            {
                int sell = p.SpareMountCount - target;
                return new MountRecommendation(0, sell,
                    $"Herd penalty: {p.SpareMountCount} mounts > threshold {threshold}, sell {sell} (target {target}).");
            }

            // Too few animals to mount all foot soldiers -> buy more.
            if (p.SpareMountCount < target)
            {
                int buy = target - p.SpareMountCount;
                return new MountRecommendation(buy, 0,
                    $"Speed bonus: buy {buy} mounts (have {p.SpareMountCount}, target {target} for {p.FootTroopCount} foot soldiers).");
            }

            return new MountRecommendation(0, 0, "Mount count already at speed optimum.");
        }
    }
}
