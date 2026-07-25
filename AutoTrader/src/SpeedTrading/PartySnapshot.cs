namespace AutoTrader.SpeedTrading
{
    /// <summary>
    /// Pure data snapshot of the party metrics relevant to speed decisions.
    /// Deliberately engine-free so the logic in <see cref="PartySpeedAdvisor"/> stays
    /// testable without a running game.
    /// </summary>
    public readonly struct PartySnapshot
    {
        /// <summary>Total number of men in the party (mounted + on foot).</summary>
        public readonly int MemberCount;

        /// <summary>Foot soldiers that could be mounted by a spare mount.</summary>
        public readonly int FootTroopCount;

        /// <summary>Spare mounts in the inventory that can let infantry mount up.</summary>
        public readonly int SpareMountCount;

        /// <summary>Current inventory weight.</summary>
        public readonly float InventoryWeight;

        /// <summary>Party carry capacity.</summary>
        public readonly float InventoryCapacity;

        public PartySnapshot(int memberCount, int footTroopCount, int spareMountCount,
            float inventoryWeight, float inventoryCapacity)
        {
            MemberCount = memberCount;
            FootTroopCount = footTroopCount;
            SpareMountCount = spareMountCount;
            InventoryWeight = inventoryWeight;
            InventoryCapacity = inventoryCapacity;
        }
    }
}
