namespace AutoTrader.Trading
{
    /// <summary>What the keep rules decided about one item.</summary>
    public enum SellVerdict
    {
        /// <summary>Keep it: a rule protects this item.</summary>
        Keep,

        /// <summary>Sell it without asking the price logic.</summary>
        Sell,

        /// <summary>No rule applies; let the price logic decide.</summary>
        CheckPrice
    }

    /// <summary>Which per-run budget a sale consumes.</summary>
    public enum SellBudget
    {
        None,
        RegularMount,
        WarMount,
        NobleMount,
        PackAnimal,
        Livestock
    }

    /// <summary>The outcome of <see cref="SellGate.Evaluate"/>, with the reason for the log.</summary>
    public readonly struct SellDecision
    {
        public readonly SellVerdict Verdict;
        public readonly SellBudget Budget;
        public readonly string Reason;

        public SellDecision(SellVerdict verdict, string reason, SellBudget budget = SellBudget.None)
        {
            Verdict = verdict;
            Reason = reason;
            Budget = budget;
        }
    }

    /// <summary>Everything the keep rules need to know about the item on offer.</summary>
    public readonly struct ItemView
    {
        public readonly int Price;
        public readonly int Amount;
        public readonly int Tier;
        public readonly bool IsArmor;
        public readonly bool IsWeapon;
        public readonly bool IsPlayerCrafted;
        public readonly int HardwoodSmeltYield;
        public readonly bool IsHardwood;
        public readonly bool IsHorse;
        public readonly bool IsPackAnimal;
        public readonly bool IsWarMount;
        public readonly bool IsNobleMount;
        public readonly bool IsConsumable;
        public readonly bool IsGrain;
        public readonly bool IsLivestock;

        public ItemView(int price, int amount, int tier, bool isArmor, bool isWeapon, bool isPlayerCrafted,
            int hardwoodSmeltYield, bool isHardwood, bool isHorse, bool isPackAnimal, bool isWarMount,
            bool isNobleMount, bool isConsumable, bool isGrain, bool isLivestock)
        {
            Price = price;
            Amount = amount;
            Tier = tier;
            IsArmor = isArmor;
            IsWeapon = isWeapon;
            IsPlayerCrafted = isPlayerCrafted;
            HardwoodSmeltYield = hardwoodSmeltYield;
            IsHardwood = isHardwood;
            IsHorse = isHorse;
            IsPackAnimal = isPackAnimal;
            IsWarMount = isWarMount;
            IsNobleMount = isNobleMount;
            IsConsumable = isConsumable;
            IsGrain = isGrain;
            IsLivestock = isLivestock;
        }
    }

    /// <summary>The settings and party state the keep rules read.</summary>
    public readonly struct SellSettings
    {
        public readonly int SellUpToTier;
        public readonly bool KeepPlayerCraftedWeapons;
        public readonly bool CollectSmeltFodder;
        public readonly int HardwoodCount;
        public readonly int HardwoodTarget;
        public readonly int HardwoodUnitValue;
        public readonly bool SpeedAwareMounts;
        public readonly bool ProtectPackAnimals;
        public readonly int KeepMountsAboveValue;
        public readonly int FoodDaysRemaining;
        public readonly int KeepFoodDays;
        public readonly int KeepGrainsMax;
        public readonly int KeepConsumablesMax;
        public readonly bool JunkCattle;

        public SellSettings(int sellUpToTier, bool keepPlayerCraftedWeapons, bool collectSmeltFodder,
            int hardwoodCount, int hardwoodTarget, int hardwoodUnitValue, bool speedAwareMounts,
            bool protectPackAnimals, int keepMountsAboveValue, int foodDaysRemaining, int keepFoodDays,
            int keepGrainsMax, int keepConsumablesMax, bool junkCattle)
        {
            SellUpToTier = sellUpToTier;
            KeepPlayerCraftedWeapons = keepPlayerCraftedWeapons;
            CollectSmeltFodder = collectSmeltFodder;
            HardwoodCount = hardwoodCount;
            HardwoodTarget = hardwoodTarget;
            HardwoodUnitValue = hardwoodUnitValue;
            SpeedAwareMounts = speedAwareMounts;
            ProtectPackAnimals = protectPackAnimals;
            KeepMountsAboveValue = keepMountsAboveValue;
            FoodDaysRemaining = foodDaysRemaining;
            KeepFoodDays = keepFoodDays;
            KeepGrainsMax = keepGrainsMax;
            KeepConsumablesMax = keepConsumablesMax;
            JunkCattle = junkCattle;
        }
    }

    /// <summary>Per-run allowances left for selling animals.</summary>
    public readonly struct SellBudgets
    {
        public readonly int Regular;
        public readonly int War;
        public readonly int Noble;
        public readonly int Pack;
        public readonly int Livestock;

        public SellBudgets(int regular, int war, int noble, int pack, int livestock)
        {
            Regular = regular;
            War = war;
            Noble = noble;
            Pack = pack;
            Livestock = livestock;
        }
    }

    /// <summary>
    /// The rules that protect things from being sold, and the ones that release them, decided
    /// before any price is considered.
    ///
    /// This is engine-free on purpose. Every behaviour regression this mod has had so far lived in
    /// exactly these rules - keeping the whole armoury because a vanilla weapon has a crafting
    /// design, selling a large party's food down to a handful of items - and none of them could be
    /// caught by a test while the logic sat inside the trade loop with the game types.
    /// </summary>
    public static class SellGate
    {
        public static SellDecision Evaluate(in ItemView item, in SellSettings s, in SellBudgets budgets)
        {
            if (item.IsArmor)
            {
                return item.Tier <= s.SellUpToTier
                    ? new SellDecision(SellVerdict.Sell, "armor within the tier limit")
                    : new SellDecision(SellVerdict.Keep, "armor above the tier limit");
            }

            if (item.IsWeapon)
            {
                // Only weapons the player actually smithed are protected here. Testing for a
                // weapon design instead would match most vanilla weapons, which are themselves
                // defined from crafting pieces.
                if (s.KeepPlayerCraftedWeapons && item.IsPlayerCrafted)
                {
                    return new SellDecision(SellVerdict.Keep, "the player crafted it");
                }

                // Cheap smelt fodder is kept while the forge is short of hardwood; valuable
                // weapons that happen to yield some are not.
                if (s.CollectSmeltFodder && s.HardwoodCount < s.HardwoodTarget
                    && item.HardwoodSmeltYield > 0
                    && item.Price <= item.HardwoodSmeltYield * s.HardwoodUnitValue)
                {
                    return new SellDecision(SellVerdict.Keep, "cheap smelt fodder for hardwood");
                }

                return item.Tier <= s.SellUpToTier
                    ? new SellDecision(SellVerdict.Sell, "weapon within the tier limit")
                    : new SellDecision(SellVerdict.Keep, "weapon above the tier limit");
            }

            if (item.IsHorse)
            {
                return EvaluateHorse(in item, in s, in budgets);
            }

            if (item.IsConsumable)
            {
                if (item.Amount > 0 && s.FoodDaysRemaining <= s.KeepFoodDays)
                {
                    return new SellDecision(SellVerdict.Keep, "food reserve not covered");
                }

                int max = item.IsGrain ? s.KeepGrainsMax : s.KeepConsumablesMax;
                if (item.Amount > max)
                {
                    return new SellDecision(SellVerdict.Sell, "more food of this kind than we keep");
                }
            }

            if (item.IsLivestock)
            {
                if (s.JunkCattle)
                {
                    return new SellDecision(SellVerdict.Sell, "livestock treated as junk");
                }
                if (budgets.Livestock > 0)
                {
                    return new SellDecision(SellVerdict.Sell, "livestock herd surplus", SellBudget.Livestock);
                }
            }

            if (item.IsHardwood && s.CollectSmeltFodder && s.HardwoodCount < s.HardwoodTarget)
            {
                return new SellDecision(SellVerdict.Keep, "hardwood still below the target");
            }

            return new SellDecision(SellVerdict.CheckPrice, "no rule applies");
        }

        private static SellDecision EvaluateHorse(in ItemView item, in SellSettings s, in SellBudgets budgets)
        {
            if (item.IsPackAnimal)
            {
                if (s.ProtectPackAnimals)
                {
                    return new SellDecision(SellVerdict.Keep, "pack animals are protected");
                }
                if (budgets.Pack > 0)
                {
                    return new SellDecision(SellVerdict.Sell, "pack animal herd surplus", SellBudget.PackAnimal);
                }
                return new SellDecision(SellVerdict.Keep, "pack animal needed to carry the cargo");
            }

            if (!s.SpeedAwareMounts)
            {
                // Manual mode: the item filter already decided whether horses may be sold at all,
                // so the price logic takes it from here.
                return new SellDecision(SellVerdict.CheckPrice, "manual mount mode");
            }

            // Unique and named mounts are not necessarily in the war or noble item categories, so
            // the category alone would treat them as ordinary riding horses.
            if (s.KeepMountsAboveValue > 0 && item.Price >= s.KeepMountsAboveValue)
            {
                return new SellDecision(SellVerdict.Keep, "mount too valuable to sell");
            }

            if (item.IsNobleMount)
            {
                return budgets.Noble > 0
                    ? new SellDecision(SellVerdict.Sell, "noble mount surplus", SellBudget.NobleMount)
                    : new SellDecision(SellVerdict.Keep, "noble mount needed for speed or upgrades");
            }

            if (item.IsWarMount)
            {
                return budgets.War > 0
                    ? new SellDecision(SellVerdict.Sell, "war mount surplus", SellBudget.WarMount)
                    : new SellDecision(SellVerdict.Keep, "war mount needed for speed or upgrades");
            }

            return budgets.Regular > 0
                ? new SellDecision(SellVerdict.Sell, "riding mount surplus", SellBudget.RegularMount)
                : new SellDecision(SellVerdict.Keep, "riding mount needed for party speed");
        }
    }
}
