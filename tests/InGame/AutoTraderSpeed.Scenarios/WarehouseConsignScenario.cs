using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using AutoTrader;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;
using TestRig.Harness.Fixtures;
using TestRig.Harness.Logic;

namespace AutoTraderSpeed.Scenarios
{
    /// <summary>
    /// autotrader.warehouse-consign (AT-2): a player-owned town sells goods from its warehouse (the
    /// vanilla stash) into its market. The daily settlement event is raised for that one town while
    /// game time is stopped, first with the warehouse off as a control (no change expected), then in
    /// consign mode with exact deltas. A final run of at least 25 game hours only proves that the real
    /// daily tick reaches the handler. Changed MCM values, gold, the town's market and stash are
    /// restored at the end; the MCM file must stay untouched. Not restored: the town stays owned by the
    /// player and the game time passed (with vanilla's effects of it) stays passed; the campaign is never saved.
    /// </summary>
    public sealed class WarehouseConsignScenario : IScenario
    {
        private const string GoodsItemId = "pottery";
        private const int GoodsCount = 20;
        private const int SharePercent = 25;
        private const int MinPrice = 0;

        // Vanilla's CampaignPeriodicEventManager ticks the settlements round-robin, spread evenly over
        // 24 game hours and not aligned to the day change, so only a window longer than 24 hours is sure
        // to include this town's DailyTickSettlement. Days(1) ends at the next day change (0 to 24 hours);
        // one extra hour covers the tick debt of the last frame.
        private const double DayWindowHours = 25;

        // WarehouseMode dropdown indices (AutoTraderConfig.WarehouseOff / WarehouseConsign).
        // Negative control: set ConsignModeIndex to AutoTraderConfig.WarehouseDeposit; the consign
        // checks must then fail.
        private const int OffModeIndex = AutoTraderConfig.WarehouseOff;
        private const int ConsignModeIndex = AutoTraderConfig.WarehouseConsign;

        private const string ConsignLogMarker = "consigned";

        public string Id => "autotrader.warehouse-consign";

        public IEnumerable<ScenarioStep> Run(IScenarioContext ctx)
        {
            AutoTraderMcmSettings settings = AutoTraderMcmSettings.Instance;
            ctx.Check("setup.mcm", settings != null, "AutoTraderMcmSettings.Instance != null", settings != null ? "ok" : "null");
            if (settings == null)
            {
                yield break;
            }

            ItemObject goods = MBObjectManager.Instance.GetObject<ItemObject>(GoodsItemId);
            ctx.Check("setup.goods", goods != null, GoodsItemId + " exists", goods != null ? "ok" : "not found");
            if (goods == null)
            {
                yield break;
            }

            // MCM only writes the file once the user saves settings, so a missing file is a valid baseline.
            string mcmPath = McmFilePath(settings);
            string mcmStateAtStart = CaptureFileState(mcmPath);
            ctx.Log("mcm file " + mcmPath + (mcmStateAtStart == MissingState ? " missing" : " present: " + mcmStateAtStart));
            ctx.Log("mod log " + ModLogFile.Path);

            Settlement town = TownFixture.FindTown(null);
            if (town == null)
            {
                throw new InvalidOperationException("no town available for the warehouse");
            }

            TradeStateBackup backup = TradeStateBackup.Capture(town);
            TownFixture.GiveTownToPlayer(town);
            ctx.Log("warehouse town " + town.StringId + " (gold " + town.Town.Gold + ")");

            int originalMode = settings.WarehouseMode.SelectedIndex;
            int originalShare = settings.ConsignmentShare;
            int originalMinPrice = settings.ConsignmentMinPrice;
            try
            {
                settings.ConsignmentShare = SharePercent;
                settings.ConsignmentMinPrice = MinPrice;
                town.Stash.AddToCounts(goods, GoodsCount);

                // Let the ownership change settle for one tick before measuring.
                yield return ScenarioStep.Ticks(1);

                // Control: warehouse off, so any delta comes from vanilla listeners of the event.
                SetMode(settings, OffModeIndex);
                InventorySnapshot before = InventorySnapshot.Capture(town);
                CampaignEventDispatcher.Instance.DailyTickSettlement(town);
                InventorySnapshot after = InventorySnapshot.Capture(town);
                CheckNoChange(ctx, before, after);

                // Consign mode, exact deltas.
                SetMode(settings, ConsignModeIndex);
                before = InventorySnapshot.Capture(town);
                CampaignEventDispatcher.Instance.DailyTickSettlement(town);
                after = InventorySnapshot.Capture(town);
                int sold = CheckConsignment(ctx, before, after);

                // Wiring: refill the stash and let more than one game day pass.
                if (sold > 0)
                {
                    town.Stash.AddToCounts(goods, sold);
                }

                int stashBeforeDay = Total(InventorySnapshot.Capture(town).StashItems);
                long logOffset = ModLogFile.Length();
                double startHours = CampaignTime.Now.ToHours;
                while (CampaignTime.Now.ToHours - startHours < DayWindowHours)
                {
                    yield return ScenarioStep.Days(1);
                }

                ctx.Log("game time passed: " + (CampaignTime.Now.ToHours - startHours).ToString("0.0") + " h");
                int stashAfterDay = Total(InventorySnapshot.Capture(town).StashItems);
                ctx.Check("day.stash-drops", stashAfterDay < stashBeforeDay, "< " + stashBeforeDay, stashAfterDay.ToString());

                bool logged = ModLogFile.ReadSince(logOffset).IndexOf(ConsignLogMarker, StringComparison.Ordinal) >= 0;
                ctx.Check("day.log-consigned", logged, "'" + ConsignLogMarker + "' appended to " + ModLogFile.Path,
                    logged ? "found" : "missing");
            }
            finally
            {
                settings.WarehouseMode.SelectedIndex = originalMode;
                settings.ConsignmentShare = originalShare;
                settings.ConsignmentMinPrice = originalMinPrice;
                AutoTraderMcmSettings.Apply();
                TownFixture.LeaveTownNow();
                ctx.Log(backup.Restore());
            }

            string mcmStateAtEnd = CaptureFileState(mcmPath);
            string mcmDetail = mcmStateAtStart == MissingState && mcmStateAtEnd != MissingState ? "created during run" : mcmStateAtEnd;
            ctx.Check("mcm.file-unchanged", mcmStateAtEnd == mcmStateAtStart, mcmStateAtStart, mcmDetail);
        }

        // WarehouseBehavior reads only AutoTraderConfig, so the MCM value must be applied.
        private static void SetMode(AutoTraderMcmSettings settings, int modeIndex)
        {
            settings.WarehouseMode.SelectedIndex = modeIndex;
            AutoTraderMcmSettings.Apply();
        }

        private static void CheckNoChange(IScenarioContext ctx, InventorySnapshot before, InventorySnapshot after)
        {
            IReadOnlyDictionary<string, int> stash = SnapshotDiff.CountDelta(before.StashItems, after.StashItems);
            IReadOnlyDictionary<string, int> market = SnapshotDiff.CountDelta(before.TownItems, after.TownItems);
            ctx.Check("control.stash", stash.Count == 0, "no change", DeltaText.Describe(stash));
            ctx.Check("control.town-items", market.Count == 0, "no change", DeltaText.Describe(market));
            ctx.Check("control.hero-gold", after.HeroGold == before.HeroGold, before.HeroGold.ToString(), after.HeroGold.ToString());
            ctx.Check("control.town-gold", after.TownGold == before.TownGold, before.TownGold.ToString(), after.TownGold.ToString());
        }

        /// <summary>Checks one consign slice and returns the number of units it sold.</summary>
        private static int CheckConsignment(IScenarioContext ctx, InventorySnapshot before, InventorySnapshot after)
        {
            IReadOnlyDictionary<string, int> stash = SnapshotDiff.CountDelta(before.StashItems, after.StashItems);
            IReadOnlyDictionary<string, int> market = SnapshotDiff.CountDelta(before.TownItems, after.TownItems);

            int sold = -stash.Values.Sum();
            bool onlyRemoved = stash.Values.All(v => v < 0);
            ctx.Check("consign.stash-drops", sold > 0 && onlyRemoved, "stash only loses units, n > 0",
                "n = " + sold + ", delta " + DeltaText.Describe(stash));

            bool mirrored = market.Count == stash.Count
                && stash.All(pair => market.ContainsKey(pair.Key) && market[pair.Key] == -pair.Value);
            ctx.Check("consign.town-items", mirrored, "town gains exactly what the stash lost", "town delta " + DeltaText.Describe(market));

            int earned = after.HeroGold - before.HeroGold;
            // Same budget rule as the mod's AutoTrader.Warehouse.ConsignmentPlanner.DailyBudget
            // (town gold * ConsignmentShare / 100); keep both in step.
            int budget = before.TownGold * SharePercent / 100;
            ctx.Check("consign.hero-gold", earned > 0 && earned <= budget, "0 < G <= " + budget + " (" + SharePercent + "% of town gold)",
                "G = " + earned);

            int paid = before.TownGold - after.TownGold;
            ctx.Check("consign.town-gold", paid == earned, "town pays G = " + earned, paid.ToString());
            return sold;
        }

        private static int Total(IReadOnlyDictionary<string, int> counts)
        {
            return counts.Values.Sum();
        }

        // MCMv5 keeps global settings under Configs\ModSettings\Global\<FolderName>[\<SubFolder>]\<Id>.json
        // (BaseGlobalSettingsContainer + BaseJsonSettingsFormat, decompiled MCMv5 5.12.3), resolved
        // against the same absolute user folder as the mod log.
        private static string McmFilePath(AutoTraderMcmSettings settings)
        {
            string relative = System.IO.Path.Combine("ModSettings", "Global", settings.FolderName, settings.SubFolder ?? string.Empty, settings.Id + ".json");
            return FileHelper.GetFileFullPath(new PlatformFilePath(EngineFilePaths.ConfigsPath, relative));
        }

        private const string MissingState = "(missing)";

        /// <summary>File state as text: SHA-256 hex plus last write time (UTC); MissingState when it does not exist.</summary>
        private static string CaptureFileState(string path)
        {
            if (!File.Exists(path))
            {
                return MissingState;
            }

            string hash;
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (SHA256 sha = SHA256.Create())
            {
                hash = BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty);
            }

            return hash + " @ " + File.GetLastWriteTimeUtc(path).ToString("o");
        }
    }
}
