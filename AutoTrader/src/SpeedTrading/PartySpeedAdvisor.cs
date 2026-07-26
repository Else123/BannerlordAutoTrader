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
        /// <summary>
        /// Cargo capacity one pack animal provides: DefaultInventoryCapacityModel adds
        /// PackAnimalsFactor (10) * _itemAverageWeight (10) per pack animal.
        /// </summary>
        private const float CapacityPerPackAnimal = 100f;

        private readonly bool _allowNobleSell;
        private readonly bool _managePackHerd;
        private readonly bool _manageLivestockHerd;
        private readonly int _livestockReserve;
        private readonly int _cargoUtilizationPercent;

        public PartySpeedAdvisor(bool allowNobleSell = false, bool managePackHerd = false,
            bool manageLivestockHerd = false, int livestockReserve = 0,
            int cargoUtilizationPercent = 100)
        {
            _allowNobleSell = allowNobleSell;
            _managePackHerd = managePackHerd;
            _manageLivestockHerd = manageLivestockHerd;
            _livestockReserve = livestockReserve;
            _cargoUtilizationPercent = cargoUtilizationPercent;
        }

        /// <summary>
        /// Pack animals to buy: only when the cargo does not fit within the configured share of
        /// the capacity, only as many as the shortfall needs, and never so many that the herd
        /// penalty starts (pack + livestock must stay within the party size).
        /// </summary>
        public int PackAnimalsToBuy(in PartySnapshot p)
        {
            int headroom = Math.Max(0, p.MemberCount - p.Livestock - p.PackAnimals);
            if (headroom == 0)
            {
                return 0;
            }

            float usableCapacity = p.InventoryCapacity * _cargoUtilizationPercent / 100f;
            float shortfall = p.InventoryWeight - usableCapacity;
            if (shortfall <= 0f)
            {
                return 0;
            }

            int needed = (int)Math.Ceiling(shortfall / CapacityPerPackAnimal);
            return Math.Min(headroom, needed);
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

            // Pack animals are the party's carriers, so selling them is capacity-guarded below.
            // Ridable mounts beyond the foot-soldier count are not: they add only 20 capacity each
            // while contributing to a herd penalty that scales far higher, and guarding them would
            // deadlock an overburdened party into never shedding its herd. Animals themselves weigh
            // nothing (DefaultInventoryCapacityModel returns 0 for anything with a HorseComponent),
            // so buying them can never overburden the party either.
            float spareCapacity = p.InventoryCapacity - p.InventoryWeight;

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

            // Pack animals and livestock both add to the herd penalty, which starts once the herd
            // exceeds the party size. Shed the excess, livestock first: livestock provides no cargo
            // capacity at all, while every pack animal carries CapacityPerPackAnimal.
            int sellLivestock = 0;
            int sellPack = 0;
            int herdExcess = Math.Max(0, p.PackAnimals + p.Livestock - p.MemberCount);

            if (_manageLivestockHerd && herdExcess > 0)
            {
                int sellableLivestock = Math.Max(0, p.Livestock - _livestockReserve);
                sellLivestock = Math.Min(herdExcess, sellableLivestock);
                herdExcess -= sellLivestock;
            }

            if (_managePackHerd && herdExcess > 0)
            {
                int sellableWithoutOverburden = (int)Math.Floor(spareCapacity / CapacityPerPackAnimal);
                sellPack = Math.Max(0, Math.Min(Math.Min(herdExcess, p.PackAnimals), sellableWithoutOverburden));
            }

            int buyPack = PackAnimalsToBuy(in p);

            string reason;
            if (buyRegular > 0 || buyPack > 0)
            {
                reason = $"Buy reg={buyRegular} pack={buyPack} (ridable {ridable}/{target} foot soldiers; " +
                    $"cargo {p.InventoryWeight:0}/{p.InventoryCapacity:0}, pack {p.PackAnimals} of max {Math.Max(0, p.MemberCount - p.Livestock)}).";
            }
            else if (sellRegular + sellWar + sellNoble + sellPack + sellLivestock > 0)
            {
                reason = $"Sell surplus reg={sellRegular} war={sellWar} noble={sellNoble} pack={sellPack} livestock={sellLivestock} " +
                    $"(ridable {ridable}, target {target}; herd pack={p.PackAnimals}+livestock={p.Livestock} vs members={p.MemberCount}; " +
                    $"spare capacity {p.InventoryCapacity - p.InventoryWeight:0}).";
            }
            else if (herdExcess > 0)
            {
                // Say so instead of claiming everything is fine: the herd is too big but the cargo
                // needs the carriers, so the trader has to sell goods before animals can go.
                reason = $"Herd {p.PackAnimals} pack + {p.Livestock} livestock over {p.MemberCount} members, " +
                    $"but capacity is needed for the cargo ({p.InventoryWeight:0}/{p.InventoryCapacity:0}) - sell goods first.";
            }
            else
            {
                reason = "Animals already balanced for speed, upgrades and capacity.";
            }

            return new MountRecommendation(buyRegular, buyPack, sellRegular, sellWar, sellNoble, sellPack, sellLivestock, reason);
        }
    }
}
