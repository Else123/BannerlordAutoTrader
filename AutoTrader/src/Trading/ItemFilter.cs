namespace AutoTrader.Trading
{
    /// <summary>The category toggles, as they apply to one direction of trade.</summary>
    public readonly struct FilterSettings
    {
        public readonly bool SellSmithingMaterials;
        public readonly bool SpeedAwareMounts;
        public readonly bool AllowHorses;
        public readonly bool AllowArmor;
        public readonly bool AllowWeapons;
        public readonly bool CollectSmeltFodder;
        public readonly bool AllowLivestock;
        public readonly bool AllowTradeGoods;
        public readonly bool AllowConsumables;

        public FilterSettings(bool sellSmithingMaterials, bool speedAwareMounts, bool allowHorses,
            bool allowArmor, bool allowWeapons, bool collectSmeltFodder, bool allowLivestock,
            bool allowTradeGoods, bool allowConsumables)
        {
            SellSmithingMaterials = sellSmithingMaterials;
            SpeedAwareMounts = speedAwareMounts;
            AllowHorses = allowHorses;
            AllowArmor = allowArmor;
            AllowWeapons = allowWeapons;
            CollectSmeltFodder = collectSmeltFodder;
            AllowLivestock = allowLivestock;
            AllowTradeGoods = allowTradeGoods;
            AllowConsumables = allowConsumables;
        }
    }

    /// <summary>What the filter needs to know about the item, beyond the shared item view.</summary>
    public readonly struct FilterItem
    {
        public readonly int Amount;
        public readonly bool IsLocked;
        public readonly bool AlreadyTraded;
        public readonly bool IsSmithingMaterial;
        public readonly bool IsHorse;
        public readonly bool IsArmor;
        public readonly bool IsWeapon;
        public readonly bool IsLivestock;
        public readonly bool IsTradeGood;
        public readonly bool IsConsumable;

        public FilterItem(int amount, bool isLocked, bool alreadyTraded, bool isSmithingMaterial,
            bool isHorse, bool isArmor, bool isWeapon, bool isLivestock, bool isTradeGood, bool isConsumable)
        {
            Amount = amount;
            IsLocked = isLocked;
            AlreadyTraded = alreadyTraded;
            IsSmithingMaterial = isSmithingMaterial;
            IsHorse = isHorse;
            IsArmor = isArmor;
            IsWeapon = isWeapon;
            IsLivestock = isLivestock;
            IsTradeGood = isTradeGood;
            IsConsumable = isConsumable;
        }
    }

    /// <summary>
    /// The gate every item passes before any buy or sell rule sees it.
    ///
    /// It runs first and rejects silently, which has twice disabled a whole feature: horses were
    /// excluded from buying outright, so speed-aware mount purchases never happened, and weapons
    /// were excluded whenever weapon buying was off, so smelt fodder could never be collected.
    /// A gate that can cancel features deserves the same test coverage as the rules behind it.
    /// </summary>
    public static class ItemFilter
    {
        /// <returns>True when the item must not be considered at all.</returns>
        public static bool IsFiltered(in FilterItem item, in FilterSettings s, bool isBuying, out string reason)
        {
            if (item.Amount <= 0)
            {
                reason = "out of stock";
                return true;
            }

            if (item.IsLocked)
            {
                reason = "locked in the inventory";
                return true;
            }

            if (item.AlreadyTraded)
            {
                reason = "already traded this run";
                return true;
            }

            // Smithing materials are kept for the forge unless selling them is explicitly allowed.
            if (!isBuying && item.IsSmithingMaterial)
            {
                reason = "smithing material";
                return !s.SellSmithingMaterials;
            }

            if (item.IsHorse)
            {
                // With speed-aware trading on, every horse must reach the per-mount rules; the
                // plain toggles only apply in manual mode.
                if (!s.SpeedAwareMounts && !s.AllowHorses)
                {
                    reason = "horses not traded in manual mode";
                    return true;
                }
            }

            if (item.IsArmor && !s.AllowArmor)
            {
                reason = "armor not traded";
                return true;
            }

            if (item.IsWeapon)
            {
                // Weapons must still get through while collecting smelt fodder, even when weapon
                // buying is off - the buy rules decline the ones that are not fodder.
                bool allowed = isBuying ? (s.AllowWeapons || s.CollectSmeltFodder) : s.AllowWeapons;
                if (!allowed)
                {
                    reason = "weapons not traded";
                    return true;
                }
            }

            if (item.IsLivestock && !s.AllowLivestock)
            {
                reason = "livestock not traded";
                return true;
            }

            if (item.IsTradeGood && !s.AllowTradeGoods && !item.IsConsumable)
            {
                reason = "trade goods not traded";
                return true;
            }

            if (item.IsConsumable && !s.AllowConsumables)
            {
                reason = "food not traded";
                return true;
            }

            reason = "passes";
            return false;
        }
    }
}
