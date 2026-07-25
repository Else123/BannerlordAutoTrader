# Town Warehouse & Caravan Distribution (design)

Status: **design only, nothing implemented yet.**

## The problem

Late game the party accumulates far more goods than the world can absorb:

- A town merchant only has so much gold, so a big haul cannot be sold in one place.
- Prices drop as you dump quantity into one market.
- The workaround today is hauling the surplus from town to town and selling it in
  slices, which is pure busywork and slows the party down (weight, and the animals
  needed to carry it).

The bottleneck is **throughput**, not price: there is no way to keep selling while
doing something else.

## The idea

Use a **warehouse in a town you own** as a buffer, and two ways to drain it:

1. **Consignment** - the local market absorbs a slice of the warehouse every day, as
   the merchant's gold allows.
2. **Caravan distribution** - your own caravans pick cargo up from the warehouse and
   sell it at the towns they travel to, for slightly less than you would get yourself.

The player deposits once, then goods reach the market over time instead of being
carried around.

## Why this is sound, and where it could go wrong

It is a good fit because it turns a tedious manual loop into a slow automatic one
**without inventing new money**: every sale still needs a merchant with gold, still
moves the market price, and the caravan takes a cut.

The risk is that it becomes a money printer if unbounded. The design therefore keeps
every vanilla constraint and adds friction:

- Sales are capped by the buying merchant's gold (as today).
- Prices still react to quantity (vanilla market data).
- Consignment sells only a limited amount per day.
- Caravan sales take a commission (default 15 %).
- Nothing is instant: caravans have to travel.

The result is higher throughput, which is exactly the complaint, and not a better
price than trading by hand.

## Vanilla anchors (confirmed by decompiling v1.4.7)

| Need | Anchor |
|------|--------|
| The warehouse itself | `Settlement.Stash` - an `ItemRoster` on every settlement, save-persisted, with a vanilla UI in settlements you own |
| Owned towns | `Settlement.OwnerClan` / `Clan.PlayerClan` |
| Your caravans | `Hero.MainHero.OwnedCaravans` (`CaravanPartyComponent`, each with `MobileParty`) |
| Caravan purse | `MobileParty.PartyTradeGold` |
| Prices | `Town.MarketData.GetPrice(...)`, `Town.GetItemPrice(...)` |
| Merchant gold | the town's gold, as the trader already reads it today |
| Triggers | `CampaignEvents.DailyTickSettlementEvent`, `OnSettlementEnteredEvent` (fires for caravans too) |

Using the vanilla stash is the key simplification: no custom persistence, and the
player can already inspect and edit the warehouse with the normal stash screen.

## Phases

Each phase is useful on its own and can ship separately.

### Phase 1 - Deposit into the warehouse

- Menu option in a town you own: *Store surplus in the warehouse*.
- During auto-trade, when the merchant runs out of gold and goods remain unsold,
  offer/auto-deposit the remainder into that town's stash instead of hauling it on.
- Only in settlements owned by the player clan. Setting: designated town only, or any
  owned town.

Already solves the worst part: the surplus stops travelling with the party.

### Phase 2 - Consignment (local trickle sale)

On each settlement daily tick, for a town that owns warehouse goods:

- Sell from the stash to that town's market while the merchant has gold, capped by a
  daily allowance (setting, e.g. share of the merchant's gold, default 25 %).
- Use normal vanilla prices, minus an optional consignment fee.
- Skip items below a minimum price to avoid churning junk.

This alone may cover most of the pain: the warehouse empties itself over a few days
while the player is elsewhere.

### Phase 3 - Caravan distribution

- When a player-owned caravan enters a settlement whose warehouse holds goods, load
  cargo into the caravan up to its free capacity, best value-per-weight first.
- When that caravan later enters another town, sell the warehouse cargo there at the
  normal price minus a commission (default 15 %), capped by that merchant's gold.
- Cargo the caravan bought itself is left to the vanilla caravan logic; only
  warehouse-sourced cargo is handled here, tracked per caravan.

Deliberate choice: the caravan is used as a **vehicle and timer**, its route is not
touched. No Harmony patch, no fighting the caravan AI - just react to arrivals.

## Settings (new section, in the established style)

One owner per decision, mode selectors where behaviour is exclusive:

- **Warehouse**: `Off` | `Designated town` | `All owned towns`
- Designated town (dropdown of owned towns, when applicable)
- **Deposit unsold goods**: on/off (Phase 1)
- **Consignment**: `Off` | `Sell locally each day` (Phase 2)
- Daily consignment share of merchant gold (%) - default 25
- **Caravan distribution**: `Off` | `Load from warehouse` (Phase 3)
- Caravan commission (%) - default 15
- Minimum item price to consign - default 0

## Open questions to settle before coding

1. Is `Settlement.Stash` reachable for towns the player owns but is not currently in?
   The roster is per settlement, so reading it remotely should work, but the UI path
   and any vanilla assumptions need checking.
2. Do owned caravans reliably raise `OnSettlementEnteredEvent`, and are they inside
   long enough to react?
3. Should the caravan pay the player from `PartyTradeGold` on pickup (clean, feels
   like selling to your own caravan) or should the player be paid on the actual sale
   (more accurate, needs per-caravan bookkeeping)? Paying on the real sale is the
   honest model and is preferred, with the consignment fee as the caravan's cut.
4. Does the vanilla caravan AI ever dump cargo it did not buy? If so, per-caravan
   tracking must tolerate cargo disappearing.

## Verification plan

- Phase 2 and 3 pricing/allowance maths go into engine-free logic classes with unit
  tests, like `SpeedTrading` (the advisor pattern already proved its worth).
- The engine bridge stays in the connector, with debug logging per decision.
