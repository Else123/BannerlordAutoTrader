using System;

namespace AutoTrader.SpeedTrading
{
    /// <summary>
    /// Decides how many mounts to buy/sell so party map speed stays optimal while troop
    /// upgrades are not starved of war/noble mounts.
    ///
    /// Calibrated against DefaultPartySpeedCalculatingModel (v1.4.7):
    ///  - Spare mounts up to the number of foot soldiers are "free": they mount the infantry
    ///    (a speed bonus) and do NOT count toward the herd penalty.
    ///  - Every spare mount beyond the foot-soldier count is pure herd penalty with no upside.
    ///  - Pack animals and livestock add to the herd independently; selling mounts cannot
    ///    offset that, so they are out of scope here.
    /// The optimum is therefore exactly: ridable spare mounts == foot soldiers.
    /// </summary>
    public sealed class PartySpeedAdvisor
    {
        private readonly bool _allowNobleSell;
        private readonly bool _managePackHerd;

        public PartySpeedAdvisor(bool allowNobleSell = false, bool managePackHerd = false)
        {
            _allowNobleSell = allowNobleSell;
            _managePackHerd = managePackHerd;
        }

        /// <summary>Ridable spare mounts wanted: one per foot soldier (herd-free and gives the bonus).</summary>
        public int SpeedTarget(in PartySnapshot p)
        {
            return p.FootTroopCount;
        }

        /// <summary>Derives a per-category buy/sell plan from the snapshot.</summary>
        public MountRecommendation Recommend(in PartySnapshot p)
        {
            int target = SpeedTarget(in p);
            int ridable = p.RidableMounts;

            // Buy regular horses up to the foot-soldier count (gold/capacity handled in the gate).
            int buyRegular = Math.Max(0, target - ridable);

            // Sell mounts beyond the foot-soldier count: regular first, then war above its upgrade
            // reserve, and noble last and only if explicitly allowed.
            int surplus = Math.Max(0, ridable - target);
            int sellRegular = Math.Min(p.RegularMounts, surplus);
            int rest = surplus - sellRegular;

            int sellWar = Math.Min(Math.Max(0, p.WarMounts - p.WarUpgradeReserve), rest);
            rest -= sellWar;

            int sellNoble = 0;
            if (_allowNobleSell)
            {
                sellNoble = Math.Min(Math.Max(0, p.NobleMounts - p.NobleUpgradeReserve), rest);
                rest -= sellNoble;
            }

            // Pack animals (mules) add to the herd penalty independently. When enabled, keep pack
            // animals only up to the herd allowance (party size minus livestock) and sell the rest.
            int sellPack = 0;
            if (_managePackHerd)
            {
                int packAllowance = Math.Max(0, p.MemberCount - p.Livestock);
                sellPack = Math.Max(0, p.PackAnimals - packAllowance);
            }

            string reason;
            if (buyRegular > 0)
            {
                reason = $"Buy {buyRegular} regular mounts (ridable {ridable}/{target} foot soldiers).";
            }
            else if (sellRegular + sellWar + sellNoble + sellPack > 0)
            {
                reason = $"Sell surplus mounts reg={sellRegular} war={sellWar} noble={sellNoble} pack={sellPack} " +
                    $"(ridable {ridable}, target {target}; reserves war={p.WarUpgradeReserve} noble={p.NobleUpgradeReserve}).";
            }
            else
            {
                reason = "Mounts already balanced for speed and upgrades.";
            }

            return new MountRecommendation(buyRegular, sellRegular, sellWar, sellNoble, sellPack, reason);
        }
    }
}
