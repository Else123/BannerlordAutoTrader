# In-game test scenarios

`AutoTraderSpeed.Scenarios` is a plugin module for the Bannerlord test rig (the sibling
`BannerlordTestRig` repo). It holds `IScenario` implementations that the rig's harness
(`TestRig.Harness`) discovers and runs inside a real campaign.

It exists for the test rig only. It is not part of the shipped mod, not in the normal mod build
and not in any solution; never install it for normal play.

## Build

The rig runner builds it, not you. It builds `AutoTrader/src/AutoTrader.csproj`, then the harness,
then `AutoTraderSpeed.Scenarios/AutoTraderSpeed.Scenarios.csproj`, all with the same
`/p:BannerlordModulesPath=<stage folder>`. This project references the staged `AutoTrader.dll`
(from `<stage>\AutoTraderSpeed\`) and `TestRig.Harness.dll` and copies its own DLL, PDB and
`SubModule.xml` to `<stage folder>\AutoTraderSpeed.Scenarios\`.

## Scenarios

| Id | Class | Checks |
|----|-------|--------|
| `autotrader.health` | `HealthScenario` | `TradeBehavior` and `WarehouseBehavior` registered; the `town` menu has the `autotrader_town` option exactly once and directly after vanilla's `trade` option (`menu.option-after-anchor`; the index is logged too); the `village` option and, where the menu is built (`port_menu` with NavalDLC, `naval_storyline_virtualport` in the story line), the port options exactly once and directly after `trade` (`menu.<menu id>-after-anchor`); the lines appended to `Documents\Mount and Blade II Bannerlord\Configs\AutoTrader.log` (resolved with `FileHelper.GetFileFullPath`, as the mod writes it) between `@0` and `@<days>` contain none of the error markers `ERROR:`, `crashed:`, `Something terrible happened`, `: Error `, `Failed to` |
| `autotrader.warehouse-consign` | `WarehouseConsignScenario` | AT-2. Gives the nearest town to the player, puts 20 `pottery` into its stash and raises `CampaignEventDispatcher.Instance.DailyTickSettlement(town)` with time stopped: in warehouse mode Off nothing changes (`control.stash`, `control.town-items`, `control.hero-gold`, `control.town-gold`); in mode Consign the stash loses n > 0 units, the town roster gains exactly those (`consign.stash-drops`, `consign.town-items`), the hero earns 0 < G <= 25% of the town's gold and the town pays G (`consign.hero-gold`, `consign.town-gold`). After refilling the stash, at least 25 game hours (`Days(1)` steps until then; vanilla ticks settlements round-robin over 24 hours, not at the day change) lower it again and log `consigned` (`day.stash-drops`, `day.log-consigned`). The MCM file `Configs\ModSettings\Global\<FolderName>\<Id>.json` (from the settings instance, today `AutoTraderSpeed\AutoTraderSpeed_v3.json`) is captured as a baseline at start (MCM only writes it once the user saves settings, so it may be missing) and must be in the identical state at the end: missing stays missing, an existing file keeps its SHA-256 and last write time (`mcm.file-unchanged`) |
| `autotrader.town-trade` | `TownTradeScenario` | AT-1. 50k gold, 30 `pottery`, fixed thresholds buy 90 / sell 0 (the lowest sell threshold, so the sale does not depend on the town's price for the surplus; `setup.sell-threshold` checks the effective value) and debug logging; enters the nearest foreign town at peace with the player, finds the live `autotrader_town` option (`menu.option-present`, `menu.option-condition`) and runs it (else `TradeBehavior.AutoTradeGoodsConsequence` directly). It waits until the trade screen is open or `Something terrible happened` is logged, then checks `log.no-crash` and `screen.opened` and stops if the screen did not open. With the trade screen open for 30 ticks, items per type are conserved over party, town and stash and no gold is booked yet (`screen.items-conserved`, `screen.gold-unbooked`). After Done: screen closed (stops if not), items conserved, hero gold delta = -(town gold delta) (`screen.closed`, `done.items-conserved`, `done.gold-mirrored`), at least one item type changed hands (`done.transactions`), a new `Performing AutoTrade` section in the log (`log.run-section`); then it leaves the town without saving and checks that no encounter is left and the party is outside (`town.left`) |

Both scenarios restore in their `finally` block, also on timeout or error and without game ticks: every MCM value they change, the hero's gold, the party's items and the used town's gold, market and stash; they then close an open trade screen (Cancel) and encounter (`TownFixture.LeaveTownNow`). Not restored, so later scenarios of the same run see it: the AT-2 town stays owned by the player, the game time AT-2 lets pass (with vanilla's daily effects) stays passed, and vanilla listeners of the extra `DailyTickSettlement` calls keep their side effects. The campaign is never saved. Negative controls (one-off, edit and rebuild): `WarehouseConsignScenario.ConsignModeIndex` = `AutoTraderConfig.WarehouseDeposit` must fail the `consign.*` checks; `TownTradeScenario.CloseAsCancel` = `true` must fail `done.transactions` and leave both gold deltas at 0.
