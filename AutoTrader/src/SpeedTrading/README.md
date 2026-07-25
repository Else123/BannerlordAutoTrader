# Speed- & Upgrade-Aware Animal Trading

Branch `feature/speed-aware-mounts`. Trades animals so the party stays **fast**, still has
the **mounts its troop upgrades need**, and keeps enough **carriers for its cargo**.

## Status: implemented and verified in-game

All of it is built, unit tested and confirmed working from a playthrough log
(`AutoTrader.log`, one run shed 1356 surplus animals for +83k gold).

## The rule it follows

Calibrated against the decompiled `DefaultPartySpeedCalculatingModel` (v1.4.7):

- Spare mounts **up to the number of foot soldiers** (`PartyBase.NumberOfMenWithoutHorse`)
  mount the infantry: a speed bonus, and they do **not** count toward the herd.
- Every ridable mount **beyond** that is pure herd penalty. The optimum is therefore
  exactly `ridable spare mounts == foot soldiers`.
- **Pack animals and livestock** cause the herd penalty independently, once
  `pack + livestock` exceeds the party size. Selling mounts cannot offset that.
- Capacity matters both ways: a pack animal carries 100, a spare mount 20
  (`DefaultInventoryCapacityModel` factors times `_itemAverageWeight`).

## What that means per category

| Category | Buy | Sell |
|----------|-----|------|
| Regular riding | up to the foot-soldier count | surplus above it |
| War mounts | not for speed | only above the pending-upgrade reserve |
| Noble mounts | not for speed | only above the reserve, and only if explicitly allowed |
| Pack animals | only when the cargo does not fit, capped by herd headroom | herd surplus, but never into overburden |
| Livestock | manual toggle | herd surplus first (carries nothing), keeping a food reserve |

Upgrade demand comes from `CharacterObject.UpgradeRequiresItemFromCategory`; categories
from `DefaultItemCategories` (`horse`, `war_horse`, `noble_horse`, `sumpter_horse`) plus
`HorseComponent.IsPackAnimal`, which covers mules and sumpters alike. Mounts priced at or
above "Keep mounts worth at least" are never sold, which protects unique and named mounts
whose category is just `horse`.

## Deliberate design decisions

- **Ridable surplus is never capacity-guarded.** It adds only 20 capacity but scales the
  herd penalty, and guarding it deadlocked an overburdened party into never recovering.
  Pack animals, the actual carriers, are guarded.
- **Pack buying is need-based**, not "buy whenever affordable". The inherited rule compared
  the party size against the *livestock* count and so bought hundreds of mules.
- When the herd is too big but the cargo needs its carriers, the log says exactly that
  instead of claiming everything is balanced.

## Structure

- `PartySnapshot`, `MountRecommendation`, `PartySpeedAdvisor` are engine-free and unit
  tested (`tests/SpeedTradingTests`, 25 tests) - no game install needed to run them.
- `AutoTraderLogicConnector` holds the TaleWorlds calls; `AutoTraderLogic` turns the
  advisor's plan into per-item buy/sell budgets.
- Settings live in MCM under "5. Animals"; `AutoTraderConfig` is the internal effective
  configuration the logic reads.

## Open

- Optional hysteresis (a deadband around the target) to avoid buying and re-selling around
  the optimum, which costs the merchant spread. Not built - the churn seen so far came
  from real party changes, not from the logic oscillating.
