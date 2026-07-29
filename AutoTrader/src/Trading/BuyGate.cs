namespace AutoTrader.Trading
{
    /// <summary>What the buy rules decided about one item on a merchant's shelf.</summary>
    public enum BuyVerdict
    {
        /// <summary>Do not buy it: a rule rejects it outright.</summary>
        Skip,

        /// <summary>Buy it for a stated reason, without consulting the price logic.</summary>
        Buy,

        /// <summary>No rule applies; let the price logic decide.</summary>
        CheckPrice
    }

    /// <summary>Which per-run allowance a purchase consumes.</summary>
    public enum BuyBudget
    {
        None,
        RegularMount,
        PackAnimal
    }

    public readonly struct BuyDecision
    {
        public readonly BuyVerdict Verdict;
        public readonly BuyBudget Budget;
        public readonly string Reason;

        public BuyDecision(BuyVerdict verdict, string reason, BuyBudget budget = BuyBudget.None)
        {
            Verdict = verdict;
            Reason = reason;
            Budget = budget;
        }
    }

    /// <summary>The settings and party state the buy rules read.</summary>
    public readonly struct BuySettings
    {
        public readonly bool BuyPackAnimals;
        public readonly bool SpeedAwareMounts;
        public readonly bool FleetCargoMode;
        public readonly float AvailableCapacity;
        public readonly float ItemWeight;
        public readonly bool BuyWeapons;
        public readonly bool CollectSmeltFodder;
        public readonly bool BuyHardwoodDirectly;
        public readonly int HardwoodCount;
        public readonly int HardwoodTarget;
        public readonly int HardwoodUnitValue;
        public readonly bool RestockFood;
        public readonly int FoodDaysRemaining;
        public readonly int KeepFoodDays;
        public readonly int KeepGrainsMax;
        public readonly int KeepConsumablesMax;

        public BuySettings(bool buyPackAnimals, bool speedAwareMounts, bool fleetCargoMode,
            float availableCapacity, float itemWeight, bool buyWeapons, bool collectSmeltFodder,
            bool buyHardwoodDirectly, int hardwoodCount, int hardwoodTarget, int hardwoodUnitValue,
            bool restockFood, int foodDaysRemaining, int keepFoodDays, int keepGrainsMax, int keepConsumablesMax)
        {
            BuyPackAnimals = buyPackAnimals;
            SpeedAwareMounts = speedAwareMounts;
            FleetCargoMode = fleetCargoMode;
            AvailableCapacity = availableCapacity;
            ItemWeight = itemWeight;
            BuyWeapons = buyWeapons;
            CollectSmeltFodder = collectSmeltFodder;
            BuyHardwoodDirectly = buyHardwoodDirectly;
            HardwoodCount = hardwoodCount;
            HardwoodTarget = hardwoodTarget;
            HardwoodUnitValue = hardwoodUnitValue;
            RestockFood = restockFood;
            FoodDaysRemaining = foodDaysRemaining;
            KeepFoodDays = keepFoodDays;
            KeepGrainsMax = keepGrainsMax;
            KeepConsumablesMax = keepConsumablesMax;
        }
    }

    /// <summary>Per-run allowances left for buying animals.</summary>
    public readonly struct BuyBudgets
    {
        public readonly int RegularMounts;
        public readonly int PackAnimals;

        public BuyBudgets(int regularMounts, int packAnimals)
        {
            RegularMounts = regularMounts;
            PackAnimals = packAnimals;
        }
    }

    /// <summary>
    /// The rules that decide what is worth buying, before any price is considered.
    ///
    /// Engine-free for the same reason as <see cref="SellGate"/>: the buy side is where the
    /// unbounded pack-animal purchase lived, which bought 900 mules in a single run because an
    /// inherited rule compared the party size against the livestock count. Rules that only speak
    /// up during a playthrough are rules nobody can check.
    /// </summary>
    public static class BuyGate
    {
        public static BuyDecision Evaluate(in ItemView item, in BuySettings s, in BuyBudgets budgets,
            int ownAmount)
        {
            if (item.IsHorse)
            {
                return EvaluateHorse(in item, in s, in budgets);
            }

            // Cheap weapons that smelt into hardwood, while the forge is short of it.
            if (s.CollectSmeltFodder && item.IsWeapon && s.HardwoodCount < s.HardwoodTarget
                && item.HardwoodSmeltYield > 0
                && item.Price <= item.HardwoodSmeltYield * s.HardwoodUnitValue)
            {
                return new BuyDecision(BuyVerdict.Buy, "cheap smelt fodder for hardwood");
            }

            // A weapon only got this far to be considered as smelt fodder; it must not slip into
            // the generic price logic unless the player actually wants weapons bought.
            if (item.IsWeapon && !s.BuyWeapons)
            {
                return new BuyDecision(BuyVerdict.Skip, "not smelt fodder and weapon buying is off");
            }

            if (s.BuyHardwoodDirectly && item.IsHardwood && s.HardwoodCount < s.HardwoodTarget)
            {
                return new BuyDecision(BuyVerdict.Buy, "restocking hardwood");
            }

            if (item.IsConsumable)
            {
                int max = item.IsGrain ? s.KeepGrainsMax : s.KeepConsumablesMax;
                if (ownAmount >= max)
                {
                    return new BuyDecision(BuyVerdict.Skip, "already holding enough of this food");
                }
                if (s.RestockFood && s.FoodDaysRemaining < s.KeepFoodDays)
                {
                    return new BuyDecision(BuyVerdict.Buy, "restocking the food reserve");
                }
            }

            return new BuyDecision(BuyVerdict.CheckPrice, "no rule applies");
        }

        private static BuyDecision EvaluateHorse(in ItemView item, in BuySettings s, in BuyBudgets budgets)
        {
            if (item.IsPackAnimal)
            {
                if (!s.BuyPackAnimals)
                {
                    return new BuyDecision(BuyVerdict.Skip, "pack animal buying is off");
                }
                return budgets.PackAnimals > 0
                    ? new BuyDecision(BuyVerdict.Buy, "pack animal needed for the cargo", BuyBudget.PackAnimal)
                    : new BuyDecision(BuyVerdict.Skip, "cargo fits, or the herd has no room");
            }

            // Riding mounts are only bought to reach the speed target.
            if (!s.SpeedAwareMounts)
            {
                return new BuyDecision(BuyVerdict.Skip, "mount management is off");
            }

            if (item.IsWarMount || item.IsNobleMount)
            {
                return new BuyDecision(BuyVerdict.Skip, "war and noble mounts are upgrade material, not speed");
            }

            if (budgets.RegularMounts <= 0)
            {
                return new BuyDecision(BuyVerdict.Skip, "enough mounts for the foot soldiers");
            }

            // Animals weigh nothing on land, but in fleet mode they take up ship cargo.
            if (s.FleetCargoMode && s.ItemWeight > s.AvailableCapacity)
            {
                return new BuyDecision(BuyVerdict.Skip, "fleet capacity reached");
            }

            return new BuyDecision(BuyVerdict.Buy, "riding mount for the speed target", BuyBudget.RegularMount);
        }
    }
}
