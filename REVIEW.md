# Structure review and refactoring plan

State: `feature/speed-aware-mounts`, 53 unit tests, builds clean, runs in a v1.4.7 campaign.

## Where the code sits

| Lines | File | Role |
|------:|------|------|
| 935 | `AutoTraderLogic.cs` | the trade run and every decision in it |
| 811 | `AutoTraderLogicConnector.cs` | ~78 methods against TaleWorlds |
| 410 | `AutoTraderConfig.cs` | effective config plus XML persistence |
| 387 | `AutoTraderMcmSettings.cs` | the settings page and its mapping |
| 246 | `Trading/SellGate.cs` | sell rules, engine-free, tested |
| 197 | `Warehouse/WarehouseBehavior.cs` | deposit, consignment, caravan pickup |
| 135 | `SpeedTrading/PartySpeedAdvisor.cs` | animal plan, engine-free, tested |
|  ~90 | `Smithing/`, `Warehouse/ConsignmentPlanner` | engine-free, tested |

Two files hold about two thirds of the mod. That is not automatically wrong - the trade run
really is one process - but it is where every regression so far has come from.

## What is actually wrong

### 1. The buy side has the same problem the sell side just lost

`CanBuy` is 113 lines and untested, and it carries the upstream comment "too many return
statements". Every bug we have hit was a decision rule that only spoke up in a playthrough:
pack animals bought without limit, food sold down to four items, most of the armoury kept
because vanilla weapons carry a crafting design. The sell rules now live in `Trading.SellGate`
where a test can reach them; the buy rules do not.

**This is the single highest-value refactor left.**

### 2. `IsItemFiltered` is a decision hiding in the adapter

62 lines in the connector, run before every buy and sell decision, and it has silently
disabled whole features twice: horses were excluded from buying entirely, and weapons were
excluded whenever "buy weapons" was off, which made the smelt-fodder feature unreachable.
It is a rule, not an engine call, and it belongs beside `SellGate`.

### 3. The XML config layer is dead weight, and a trap

`AutoTraderConfig.Save()` is only ever called from inside `Initialize()`, to write a default
file. Nothing saves user changes any more - MCM owns the settings and `Apply()` overwrites
every field at the start of each trade run. What is left is roughly 200 lines where each
setting must be declared three times (property, XML read, XML write). Forgetting one is
silent: I did exactly that once and the setting simply had no effect.

Removing it means: `AutoTraderConfig` becomes a plain in-memory effective config, MCM applies
on session start as well as per run, and a new setting is touched in two places instead of
five.

### 4. The connector is a 71-member god interface

That is why a test double is impractical, and why the decision logic could not be tested for
so long. It also mixes three different things: plain queries (`GetItemName`), party state
(`GetNumPackAnimals`), and outright logic (`IsItemFiltered`, `GetBestEquippedTier`).

### 5. Smaller things

- `GetAveragePrice` (106 lines) scans towns and villages with two near-identical blocks.
- `AutoTraderSpecialRules` is now a thin leftover: two hardwood helpers and two cattle rules
  that belong with the buy rules once those are extracted.
- `DepositItemToStash` and `GetBestEquippedTier` are logic living in the adapter.

## Plan, in the order I would do it

**P1 - Extract the buy rules into `Trading.BuyGate`** *(highest value)*
Mirror of `SellGate`: item view plus settings plus budgets in, verdict out, engine-free and
tested. Covers pack animals, smelt fodder, food restocking, consumable caps, cattle. Closes
the last untested decision surface.

**P2 - Extract `IsItemFiltered` into `Trading.ItemFilter`**
Same shape, tested, with cases for exactly the two failures it caused. After P1 and P2 no
decision about an item is made outside a tested class.

**P3 - Delete the XML persistence**
Keep reading an existing file once for migration, then drop `Save()` and the read branches.
Apply MCM on session start so nothing depends on XML defaults.

**P4 - Split the connector by role**
`ItemQueries`, `PartyQueries`, `MarketQueries`, `InventoryOperations` - partial classes first
(no call-site churn), then the interface can be split along the same seams. After P1-P3 the
interface will already be smaller, because the decisions no longer need their raw inputs
exposed one by one.

**P5 - Deduplicate the price scan**
One routine for towns and villages instead of two near-identical blocks.

## What I would not change

- The `Logic` / `Connector` split. It is the reason any of this is testable at all, and it is
  the author's original design.
- `WarehouseBehavior` and the `SpeedTrading` / `Smithing` classes. They are already the shape
  the rest is being moved towards.
- The MCM page. The settings surface was reworked once and is coherent: one owner per
  decision, mode selectors instead of flags that cancel each other.
