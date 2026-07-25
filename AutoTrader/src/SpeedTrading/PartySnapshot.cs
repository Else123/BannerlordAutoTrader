namespace AutoTrader.SpeedTrading
{
    /// <summary>
    /// Pure data snapshot of the party metrics relevant to mount decisions.
    /// Deliberately engine-free so the logic in <see cref="PartySpeedAdvisor"/> stays
    /// testable without a running game.
    /// </summary>
    public readonly struct PartySnapshot
    {
        /// <summary>Total number of men in the party (mounted + on foot).</summary>
        public readonly int MemberCount;

        /// <summary>Foot soldiers that could be mounted by a spare mount.</summary>
        public readonly int FootTroopCount;

        /// <summary>Spare regular riding horses in the inventory.</summary>
        public readonly int RegularMounts;

        /// <summary>Spare war mounts (also used to upgrade cavalry troops).</summary>
        public readonly int WarMounts;

        /// <summary>Spare noble mounts (rarest, used to upgrade elite troops).</summary>
        public readonly int NobleMounts;

        /// <summary>Pack animals (mules/sumpters): carry weight, but count toward the herd penalty.</summary>
        public readonly int PackAnimals;

        /// <summary>Livestock (cattle/sheep): not ridable, count toward the herd penalty.</summary>
        public readonly int Livestock;

        /// <summary>War mounts to keep aside for pending troop upgrades.</summary>
        public readonly int WarUpgradeReserve;

        /// <summary>Noble mounts to keep aside for pending troop upgrades.</summary>
        public readonly int NobleUpgradeReserve;

        /// <summary>Current inventory weight.</summary>
        public readonly float InventoryWeight;

        /// <summary>Party carry capacity.</summary>
        public readonly float InventoryCapacity;

        public PartySnapshot(int memberCount, int footTroopCount,
            int regularMounts, int warMounts, int nobleMounts, int packAnimals, int livestock,
            int warUpgradeReserve, int nobleUpgradeReserve,
            float inventoryWeight, float inventoryCapacity)
        {
            MemberCount = memberCount;
            FootTroopCount = footTroopCount;
            RegularMounts = regularMounts;
            WarMounts = warMounts;
            NobleMounts = nobleMounts;
            PackAnimals = packAnimals;
            Livestock = livestock;
            WarUpgradeReserve = warUpgradeReserve;
            NobleUpgradeReserve = nobleUpgradeReserve;
            InventoryWeight = inventoryWeight;
            InventoryCapacity = inventoryCapacity;
        }

        /// <summary>Ridable spare mounts (regular + war + noble) - all grant the speed bonus.</summary>
        public int RidableMounts => RegularMounts + WarMounts + NobleMounts;

        /// <summary>All animals that count toward the herd penalty.</summary>
        public int TotalAnimals => RidableMounts + PackAnimals + Livestock;
    }
}
