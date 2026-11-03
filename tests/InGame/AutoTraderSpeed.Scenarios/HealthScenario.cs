using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using AutoTrader;
using AutoTrader.Warehouse;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.Library;
using TestRig.Harness.Logic;

namespace AutoTraderSpeed.Scenarios
{
    /// <summary>
    /// autotrader.health: AutoTrader registered its behaviors and its town menu option, exactly
    /// once and directly after vanilla's "trade" option. Time stays stopped (no Days steps), so the scenario can also run as a health bracket.
    /// </summary>
    public sealed class HealthScenario : IScenario
    {
        private const string TownMenuId = "town";
        private const string TownOptionId = "autotrader_town";
        private const string AnchorOptionId = "trade";
        private const string ModLogFileName = "AutoTrader.log";

        // Case-sensitive substrings of the error lines AutoTrader writes to its log:
        // "ERROR:" (AutoTraderLogic price lookup), "crashed:" (GetItemAmount), "Something terrible
        // happened" (PerformAutoTrade catch-all), ": Error " (Warsails helpers) and "Failed to"
        // (inventory/Warsails load failures).
        private static readonly string[] ErrorMarkers =
        {
            "ERROR:", "crashed:", "Something terrible happened", ": Error ", "Failed to",
        };

        // Set by the @0 call; the same instance is reused for the @N call.
        private long? _logLengthAtStart;

        public string Id => "autotrader.health";

        public IEnumerable<ScenarioStep> Run(IScenarioContext ctx)
        {
            // AutoTrader registers two behaviors (AutoTraderSubModule.AddBehaviors).
            // GetCampaignBehavior<T> returns the first registered behavior that is a T.
            TradeBehavior trade = Campaign.Current.GetCampaignBehavior<TradeBehavior>();
            ctx.Check("behavior.trade", trade != null, "TradeBehavior registered", trade != null ? "ok" : "null");

            WarehouseBehavior warehouse = Campaign.Current.GetCampaignBehavior<WarehouseBehavior>();
            ctx.Check("behavior.warehouse", warehouse != null, "WarehouseBehavior registered", warehouse != null ? "ok" : "null");

            // AutoTrader registers no GameModel, so there is no active-model check.

            GameMenu townMenu = Campaign.Current.GameMenuManager.GetGameMenu(TownMenuId);
            ctx.Check("menu.town-exists", townMenu != null, "game menu '" + TownMenuId + "' exists", townMenu != null ? "ok" : "null");
            if (townMenu != null)
            {
                List<GameMenuOption> options = townMenu.MenuOptions.ToList();
                int count = options.Count(o => o.IdString == TownOptionId);
                ctx.Check("menu.town-" + TownOptionId, count == 1, "occurs exactly once", count + " time(s)");

                int ownIndex = options.FindIndex(o => o.IdString == TownOptionId);
                int anchorIndex = options.FindIndex(o => o.IdString == AnchorOptionId);
                ctx.Log(TownOptionId + " index " + ownIndex + " of " + options.Count + " options");

                ctx.Check("menu.option-after-anchor", anchorIndex >= 0 && ownIndex == anchorIndex + 1,
                    TownOptionId + " directly after " + AnchorOptionId,
                    TownOptionId + " at " + ownIndex + ", " + AnchorOptionId + " at " + anchorIndex);
            }

            CheckLogGrowth(ctx);
            yield break;
        }

        // F-S6-6: the first call remembers the log length, a later call checks only the appended bytes.
        private void CheckLogGrowth(IScenarioContext ctx)
        {
            // Absolute path; EngineFilePaths.ConfigsPath.Path alone is the relative "Configs".
            string path = ModLogFile.Path;
            long length = GetLength(path);

            if (_logLengthAtStart == null)
            {
                _logLengthAtStart = length;
                ctx.Log("mod log baseline: " + length + " bytes at day " + ctx.Day + " (" + path + ")");
                return;
            }

            long start = _logLengthAtStart.Value;
            if (length < start)
            {
                // The mod restarts its log on launch; a shorter file means it was rewritten.
                ctx.Log("mod log shrank from " + start + " to " + length + " bytes; reading it from the start");
                start = 0;
            }

            string appended = ReadFrom(path, start);
            string found = ErrorMarkers.FirstOrDefault(m => appended.IndexOf(m, System.StringComparison.Ordinal) >= 0);
            ctx.Log("mod log grew by " + (length - start) + " bytes (day " + ctx.Day + ")");
            ctx.Check("log.no-errors", found == null, "no error marker in " + ModLogFileName + " since start",
                found == null ? "none" : "found '" + found + "'");
        }

        private static long GetLength(string path)
        {
            if (!File.Exists(path))
            {
                return 0;
            }

            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                return stream.Length;
            }
        }

        private static string ReadFrom(string path, long offset)
        {
            if (!File.Exists(path))
            {
                return string.Empty;
            }

            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                stream.Seek(offset, SeekOrigin.Begin);
                using (var reader = new StreamReader(stream, Encoding.UTF8))
                {
                    return reader.ReadToEnd();
                }
            }
        }
    }
}
