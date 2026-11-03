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
| `autotrader.health` | `HealthScenario` | `TradeBehavior` and `WarehouseBehavior` registered; the `town` menu has the `autotrader_town` option exactly once (index is logged, not asserted); the lines appended to `Configs\AutoTrader.log` between `@0` and `@<days>` contain none of the error markers `ERROR:`, `crashed:`, `Something terrible happened`, `: Error `, `Failed to` |
