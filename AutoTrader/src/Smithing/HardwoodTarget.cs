using System;

namespace AutoTrader.Smithing
{
    /// <summary>
    /// How much hardwood the party should keep. Engine-free so it can be unit tested.
    ///
    /// A fixed number does not survive contact with a real smithing stockpile: hardwood becomes
    /// charcoal, and charcoal is what every refining and smelting step burns, so the amount you
    /// need follows the ore and ingots you are holding. Somebody sitting on 20k iron is not served
    /// by a hundred logs. The target therefore scales with that stock, with the configured number
    /// acting as the floor for a party that carries no materials at all.
    /// </summary>
    public static class HardwoodTarget
    {
        /// <param name="configuredMinimum">The floor from the settings.</param>
        /// <param name="refinableMaterials">Ore and ingots held - the things that consume charcoal.</param>
        /// <param name="percentPerMaterial">Hardwood to keep per 100 units of those materials.</param>
        public static int Effective(int configuredMinimum, int refinableMaterials, int percentPerMaterial)
        {
            int floor = Math.Max(0, configuredMinimum);
            if (refinableMaterials <= 0 || percentPerMaterial <= 0)
            {
                return floor;
            }

            // Multiply first so small stocks do not truncate the ratio away.
            int scaled = refinableMaterials * percentPerMaterial / 100;
            return Math.Max(floor, scaled);
        }
    }
}
