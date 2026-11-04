using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using AutoTrader;
using AutoTrader.Menu;
using AutoTrader.Warehouse;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.Library;
using TestRig.Harness.Logic;

namespace AutoTraderSpeed.Scenarios
{
    /// <summary>
    /// autotrader.health: AutoTrader registered its behaviors and its town/village/port menu options, exactly
    /// once and directly after vanilla's "trade" option. Time stays stopped (no Days steps), so the scenario can also run as a health bracket.
    /// </summary>
    public sealed class HealthScenario : IScenario
    {
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

            // Town and village always exist. The port menus only exist with the NavalDLC (port_menu) and
            // in the naval story line (naval_storyline_virtualport); where nobody built the menu we add no
            // option, see CheckOptionPlacement.
            foreach (MenuOptionAnchor anchor in MenuOptionAnchors.All)
            {
                CheckOptionPlacement(ctx, anchor);
            }

            CheckLogGrowth(ctx);
            yield break;
        }

        private static void CheckOptionPlacement(IScenarioContext ctx, MenuOptionAnchor anchor)
        {
            // Check ids: the town keeps the historical "menu.option-after-anchor" (README), the others carry the menu id.
            bool isTown = anchor == MenuOptionAnchors.Town;
            string prefix = isTown ? "menu.town" : "menu." + anchor.MenuId;
            string afterAnchorCheck = isTown ? "menu.option-after-anchor" : prefix + "-after-anchor";

            // GetGameMenu can return an empty placeholder (any AddGameMenuOption on an unknown id creates
            // one), so "exists" means IsReady, i.e. someone initialized the menu.
            GameMenu menu = Campaign.Current.GameMenuManager.GetGameMenu(anchor.MenuId);
            bool built = menu != null && menu.IsReady;
            bool required = isTown || anchor == MenuOptionAnchors.Village;
            if (required)
            {
                // Reported on success too, so runs stay comparable by check id.
                ctx.Check(prefix + "-exists", built, "game menu '" + anchor.MenuId + "' exists", built ? "exists" : "missing");
            }

            if (!built)
            {
                if (!required)
                {
                    ctx.Log("menu '" + anchor.MenuId + "' not built (no NavalDLC / story line), placement not checked");
                }

                return;
            }

            List<GameMenuOption> options = menu.MenuOptions.ToList();
            int count = options.Count(o => o.IdString == anchor.OptionId);
            int ownIndex = options.FindIndex(o => o.IdString == anchor.OptionId);
            int anchorIndex = options.FindIndex(o => o.IdString == anchor.AnchorOptionId);
            ctx.Log(anchor.OptionId + " index " + ownIndex + " of " + options.Count + " options");

            ctx.Check(prefix + "-" + anchor.OptionId, count == 1, "occurs exactly once", count + " time(s)");
            ctx.Check(afterAnchorCheck, anchorIndex >= 0 && ownIndex == anchorIndex + 1,
                anchor.OptionId + " directly after " + anchor.AnchorOptionId,
                anchor.OptionId + " at " + ownIndex + ", " + anchor.AnchorOptionId + " at " + anchorIndex);
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
