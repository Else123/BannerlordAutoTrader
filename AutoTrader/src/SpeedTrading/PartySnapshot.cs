namespace AutoTrader.SpeedTrading
{
    /// <summary>
    /// Reiner Datenschnappschuss der fuer Speed-Entscheidungen relevanten
    /// Party-Kennzahlen. Bewusst engine-frei, damit die Logik in
    /// <see cref="PartySpeedAdvisor"/> ohne laufendes Spiel testbar ist.
    /// </summary>
    public readonly struct PartySnapshot
    {
        /// <summary>Gesamtzahl Mann in der Party (beritten + zu Fuss).</summary>
        public readonly int MemberCount;

        /// <summary>Fusssoldaten, die durch ein freies Reittier beritten werden koennten.</summary>
        public readonly int FootTroopCount;

        /// <summary>Freie Reittiere im Inventar, die Infanterie aufsitzen lassen koennen.</summary>
        public readonly int SpareMountCount;

        /// <summary>Aktuelles Inventargewicht.</summary>
        public readonly float InventoryWeight;

        /// <summary>Tragekapazitaet der Party.</summary>
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

