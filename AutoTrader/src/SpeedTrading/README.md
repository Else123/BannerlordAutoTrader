# Speed- & Upgrade-Aware Mount Trading (Feature Plan v2)

Branch `feature/speed-aware-mounts`. Extends the auto trader so mounts are traded to
keep **party speed high** WHILE not blocking **troop upgrades**.

## Current state (v1, works in-game)

- Connector primitives `GetNumFootTroops`, `GetNumSpareRidingMounts`.
- `ComputeMountBudgets` (in `AutoTraderLogic`) + engine-free `PartySpeedAdvisor`.
- Buy gate: buy riding horses up to the speed target; fleet capacity cap.
- Sell gate: protect riding mounts, sell only the surplus above the herd threshold.
- Config `SpeedAwareMountsValue`, `HerdThresholdPercentValue` (XML only, no GUI).
- Builds cleanly (VS2026/MSBuild), deploys as its own module `AutoTraderSpeed`.

**Known gaps in v1 (motivation for v2):**
1. Only riding horses are considered - **pack animals / livestock missing** from the
   herd/speed maths.
2. **No mount categories** - regular/war/noble are treated identically.
3. **No upgrade reserve** - could sell war/noble mounts that are needed for troop
   upgrades.
4. Speed threshold simplified (not calibrated against `DefaultPartySpeedCalculatingModel`).
5. No GUI for the settings.

## Domain model v2

### Mounts have two competing purposes

- **Speed**: every free ridable horse lets a foot soldier mount up (speed bonus). Any
  category except pack animal counts for this.
- **Upgrade material**: troop upgrades require a specific mount category depending on the
  target (T1 "Mount" / T2-T3 "War Mount" / T4 "Noble Mount"). These horses are valuable
  and must NOT be sold as speed surplus while upgrades are pending.

### Categories (detection - verify against decompiled v1.4.7)

| Category | Detection (planned) | Purpose |
|----------|---------------------|---------|
| PackAnimal | `HorseComponent.IsPackAnimal` (Mule/Sumpter) | carry weight + herd penalty |
| Riding (regular) | Horse, !pack, Tier<=1 / ItemCategory Horse | speed + T1 upgrades |
| WarMount | Horse, !pack, Tier 2-3 / ItemCategory WarHorse | speed + war upgrades |
| NobleMount | Horse, !pack, Tier 4 / ItemCategory NobleHorse | speed + noble upgrades (rare, most valuable) |
| Livestock | `ItemType.Animal` / `IsAnimal` (cattle/sheep) | herd penalty, not ridable |

Anchors: `item.HorseComponent.IsPackAnimal`, `item.ItemCategory`, `item.Tier`.
Upgrade demand: `CharacterObject.UpgradeTargets` + `UpgradeRequiresItemFromCategory`
(count the required mount category per upgradable troop).

## Decision logic v2

### Herd/speed cap on TOTAL animal count (fixes gap 1)

`totalAnimals = pack + riding + war + noble + livestock`. Herd threshold ~
`MemberCount * factor` applies to `totalAnimals`, not just riding horses.

### Reserves before selling (fixes gaps 2+3)

Mounts to keep per category =
`max(SpeedReserve, UpgradeReserve[cat])`, where
- `SpeedReserve` = enough ridable mounts to mount the foot soldiers (category-agnostic).
- `UpgradeReserve[cat]` = sum of mounts of that category required by pending upgrades
  (+ configurable buffer).

### Sell order (true surplus only)

1. Sell **regular riding** above reserve first.
2. Then **PackAnimal** above the carry/herd need (new: manage pack animals).
3. **WarMount** only above the upgrade reserve, conservatively.
4. **NobleMount** protected by default (only on clear surplus, opt-in setting).

### Buy

- Buy cheap regular riding up to the speed target (as in v1).
- Optional (setting): buy war/noble to enable pending upgrades - a separate, cautious
  path, not for speed.
- Keep respecting the fleet capacity cap.

## Implementation phases

- **Phase A - categories + reserves** (core of this note):
  `MountKind` classification, upgrade-reserve calculation, snapshot v2 (per category),
  advisor v2 (sell true surplus only, keep buy target), pack animals in the herd maths.
  Extend the connector with category/upgrade getters.
- **Phase B - refactor + tests**: pull mount handling out of `CanBuy`/`CanSell` into a
  testable `MountTradingStrategy` (author's TODO "too many return statements"); unit tests
  for the advisor (uses the existing `AutoTraderTests` architecture).
- **Phase C - MCM UI**: move settings to MCM attributes (instead of the 87 KB prefab +
  39 KB view model + 200 lines of XML serialization); the new mount settings are exposed
  automatically. First re-check the native lock/filter usage so no feature is lost.
- **Phase D - cleanup/calibration**: remove dead code + unused config; calibrate the speed
  factors against the decompiled `DefaultPartySpeedCalculatingModel`.

## API anchors (confirmed so far)

- Trading: `TransferCommand.Transfer(...)` + `InventoryLogic`.
- Party/inventory: `PartyBase.MainParty.MemberRoster` (TroopRoster),
  `...MobileParty.ItemRoster` (ItemRoster), `ItemRoster.NumberOfLivestockAnimals`.
- Weight/capacity: `GetCurrentWeight`/`GetInventoryCapacity` (with Warsails fleet).
- Upgrade: `CharacterObject.UpgradeTargets`, `UpgradeRequiresItemFromCategory` (verify).
