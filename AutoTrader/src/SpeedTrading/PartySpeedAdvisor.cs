using System;

namespace AutoTrader.SpeedTrading
{
    /// <summary>
    /// Decides how many mounts to buy/sell so party map speed stays optimal while troop
    /// upgrades are not starved of war/noble mounts.
    ///
    /// Vanilla background (DefaultPartySpeedCalculatingModel):
    ///  - Every foot soldier that can mount a spare horse grants a speed bonus, so the
    ///    optimum is ~1 ridable spare mount per foot soldier.
    ///  - Animals above ~ MemberCount * factor create an increasing herd penalty; the cap
    ///    applies to ALL animals (mounts + pack + livestock), not just riding horses.
    /// Figures come from community analyses; verify against decompiled v1.4.7 (README.md).
    /// </summary>
    public sealed class PartySpeedAdvisor
    {
        /// <summary>Safety margin below the herd threshold (number of animals).</summary>
        private const int HerdSafetyMargin = 2;

        private readonly int _herdThresholdPercent;
        private readonly bool _allowNobleSell;

        public PartySpeedAdvisor(int herdThresholdPercent = 105, bool allowNobleSell = false)
        {
            _herdThresholdPercent = herdThresholdPercent;
            _allowNobleSell = allowNobleSell;
        }

        /// <summary>Animal count at/above which the herd penalty kicks in.</summary>
        public int HerdThreshold(in PartySnapshot p)
        {
            // Integer math keeps the threshold exact (float 1.05 rounds 100 members to 104).
            return p.MemberCount * _herdThresholdPercent / 100;
        }

        /// <summary>
        /// Ridable spare mounts wanted for speed: one per foot soldier, capped safely below
        /// the herd threshold.
        /// </summary>
        public int SpeedTarget(in PartySnapshot p)
        {
            int safeCap = Math.Max(0, HerdThreshold(in p) - HerdSafetyMargin);
            return Math.Min(p.FootTroopCount, safeCap);
        }

        /// <summary>Derives a per-category buy/sell plan from the snapshot.</summary>
        public MountRecommendation Recommend(in PartySnapshot p)
        {
            int threshold = HerdThreshold(in p);
            int speedTarget = SpeedTarget(in p);
            int ridable = p.RidableMounts;

            // Buy: only regular horses, up to the speed target, and only while total animals
            // stay below the herd threshold (never make the party slower).
            int buyRegular = Math.Max(0, Math.Min(speedTarget - ridable, threshold - p.TotalAnimals));

            // Sell: shed the ridable surplus above the speed target, or the animal surplus
            // above the herd threshold, whichever is larger (bounded by what we can shed from
            // the ridable pool - pack/livestock selling is out of scope for now).
            int ridableSurplus = Math.Max(0, ridable - speedTarget);
            int herdSurplus = Math.Max(0, p.TotalAnimals - threshold);
            int toShed = Math.Min(ridable, Math.Max(ridableSurplus, herdSurplus));

            // Allocate the sells: regular first, then war above its upgrade reserve, and noble
            // last and only if explicitly allowed.
            int sellRegular = Math.Min(p.RegularMounts, toShed);
            int rest = toShed - sellRegular;

            int sellWar = Math.Min(Math.Max(0, p.WarMounts - p.WarUpgradeReserve), rest);
            rest -= sellWar;

            int sellNoble = 0;
            if (_allowNobleSell)
            {
                sellNoble = Math.Min(Math.Max(0, p.NobleMounts - p.NobleUpgradeReserve), rest);
                rest -= sellNoble;
            }

            string reason;
            if (buyRegular > 0)
            {
                reason = $"Buy {buyRegular} regular mounts (ridable {ridable}/{speedTarget}, animals {p.TotalAnimals}/{threshold}).";
            }
            else if (sellRegular + sellWar + sellNoble > 0)
            {
                reason = $"Sell surplus mounts reg={sellRegular} war={sellWar} noble={sellNoble} " +
                    $"(ridable {ridable}, target {speedTarget}, animals {p.TotalAnimals}/{threshold}; " +
                    $"reserves war={p.WarUpgradeReserve} noble={p.NobleUpgradeReserve}).";
            }
            else
            {
                reason = "Mounts already balanced for speed and upgrades.";
            }

            return new MountRecommendation(buyRegular, sellRegular, sellWar, sellNoble, reason);
        }
    }
}
