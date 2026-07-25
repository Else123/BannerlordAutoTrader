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

        /// <summary>
        /// Cargo capacity one spare mount provides: SpareMountsFactor (2) * _itemAverageWeight (10).
        /// </summary>
        private const float CapacityPerSpareMount = 20f;

        private readonly bool _allowNobleSell;
        private readonly bool _managePackHerd;
        private readonly bool _manageLivestockHerd;
        private readonly int _livestockReserve;

        public PartySpeedAdvisor(bool allowNobleSell = false, bool managePackHerd = false,
            bool manageLivestockHerd = false, int livestockReserve = 0)
        {
            _allowNobleSell = allowNobleSell;
            _managePackHerd = managePackHerd;
            _manageLivestockHerd = manageLivestockHerd;
            _livestockReserve = livestockReserve;
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

            // Selling animals also removes the cargo capacity they provide, and the Overburdened
            // penalty is harsher than the herd penalty. Track the spare capacity and never sell so
            // much that the cargo no longer fits.
            float spareCapacity = p.InventoryCapacity - p.InventoryWeight;

            // Sell mounts beyond the foot-soldier count: regular first, then war above its upgrade
            // reserve, and noble last and only if explicitly allowed.
            int surplus = Math.Max(0, ridable - target);
            surplus = Math.Min(surplus, (int)Math.Floor(spareCapacity / CapacityPerSpareMount));
            spareCapacity -= surplus * CapacityPerSpareMount;
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

            string reason;
            if (buyRegular > 0)
            {
                reason = $"Buy {buyRegular} regular mounts (ridable {ridable}/{target} foot soldiers).";
            }
            else if (sellRegular + sellWar + sellNoble + sellPack + sellLivestock > 0)
            {
                reason = $"Sell surplus reg={sellRegular} war={sellWar} noble={sellNoble} pack={sellPack} livestock={sellLivestock} " +
                    $"(ridable {ridable}, target {target}; herd pack={p.PackAnimals}+livestock={p.Livestock} vs members={p.MemberCount}; " +
                    $"spare capacity {p.InventoryCapacity - p.InventoryWeight:0}).";
            }
            else
            {
                reason = "Animals already balanced for speed, upgrades and capacity.";
            }

            return new MountRecommendation(buyRegular, sellRegular, sellWar, sellNoble, sellPack, sellLivestock, reason);
        }
    }
}
