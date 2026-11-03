using System;
using System.Collections.Generic;
using System.Linq;
using AutoTrader;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;
using TestRig.Harness.Fixtures;
using TestRig.Harness.Logic;

namespace AutoTraderSpeed.Scenarios
{
    /// <summary>
    /// autotrader.town-trade (AT-1): one AutoTrader run in a foreign town, started through the live
    /// "autotrader_town" menu option. While the trade screen is open the goods are already moved but
    /// no gold is booked; after Done the player's gold change mirrors the town's (town trade pays no
    /// tax). A missing or disabled option fails its own checks and the trade still runs through
    /// TradeBehavior directly, so "wiring broken" and "trading broken" stay apart. Changed MCM values,
    /// the hero's gold, the party's items and the town's gold, market and stash are restored at the end,
    /// and an open trade screen or encounter is closed; the campaign is never saved.
    /// </summary>
    public sealed class TownTradeScenario : IScenario
    {
        private const string TownOptionId = "autotrader_town";
        private const int PlayerGold = 50000;
        private const string SurplusItemId = "pottery";
        private const int SurplusCount = 30;
        private const int FixedThresholdsIndex = 1;
        private const int BuyThreshold = 90;

        // Lowest value of the MCM "Sell threshold (%)" range (0..300): every good with a positive price factor
        // passes the threshold, so the sale does not depend on how the chosen town prices the surplus
        // (a producer town pays far below the average price).
        private const int SellThreshold = 0;

        // Longer than AutoTrader's 15-tick inventory display refresh: closing earlier breaks Gauntlet.
        private const int ScreenTicks = 30;
        private const double OpenTimeoutSeconds = 10;

        // Negative control: set to true (Cancel); the gold checks must then show a delta of 0.
        private const bool CloseAsCancel = false;

        private const string RunLogMarker = "Performing AutoTrade";
        private const string CrashLogMarker = "Something terrible happened";

        public string Id => "autotrader.town-trade";

        public IEnumerable<ScenarioStep> Run(IScenarioContext ctx)
        {
            AutoTraderMcmSettings settings = AutoTraderMcmSettings.Instance;
            ctx.Check("setup.mcm", settings != null, "AutoTraderMcmSettings.Instance != null", settings != null ? "ok" : "null");
            if (settings == null)
            {
                yield break;
            }

            TradeBehavior trade = Campaign.Current.GetCampaignBehavior<TradeBehavior>();
            ctx.Check("setup.trade-behavior", trade != null, "TradeBehavior registered", trade != null ? "ok" : "null");
            if (trade == null)
            {
                yield break;
            }

            ItemObject surplus = MBObjectManager.Instance.GetObject<ItemObject>(SurplusItemId);
            ctx.Check("setup.surplus", surplus != null, SurplusItemId + " exists", surplus != null ? "ok" : "not found");
            if (surplus == null)
            {
                yield break;
            }

            // Foreign and at peace, so the warehouse deposit stays out and the town grants trade access.
            Settlement town = TownFixture.FindTown(s => s.OwnerClan != Clan.PlayerClan && TownFixture.IsAtPeaceWithPlayer(s));
            if (town == null)
            {
                throw new InvalidOperationException("no foreign town at peace with the player");
            }

            ctx.Log("trade town " + town.StringId + " (gold " + town.Town.Gold + ")");
            ctx.Log("mod log " + ModLogFile.Path);

            TradeStateBackup backup = TradeStateBackup.Capture(town);

            int originalPricing = settings.PricingMode.SelectedIndex;
            int originalBuy = settings.BuyThreshold;
            int originalSell = settings.SellThreshold;
            bool originalDebug = settings.DebugMode;
            try
            {
                settings.PricingMode.SelectedIndex = FixedThresholdsIndex;
                settings.BuyThreshold = BuyThreshold;
                settings.SellThreshold = SellThreshold;
                settings.DebugMode = true;
                AutoTraderMcmSettings.Apply();
                ctx.Log("sell threshold " + SellThreshold + " (was " + originalSell + ")");
                ctx.Check("setup.sell-threshold", AutoTraderConfig.SellThresholdValue == SellThreshold && !AutoTraderConfig.SimpleTradingAI,
                    "fixed thresholds, effective sell threshold " + SellThreshold,
                    (AutoTraderConfig.SimpleTradingAI ? "rumours" : "thresholds") + ", sell " + AutoTraderConfig.SellThresholdValue);

                Hero.MainHero.ChangeHeroGold(PlayerGold - Hero.MainHero.Gold);
                MobileParty.MainParty.ItemRoster.AddToCounts(surplus, SurplusCount);

                foreach (ScenarioStep step in TownFixture.EnterTown(town))
                {
                    yield return step;
                }

                MenuContext menu = Campaign.Current.CurrentMenuContext;
                GameMenuOption option = menu.GameMenu.MenuOptions.FirstOrDefault(o => o.IdString == TownOptionId);
                ctx.Check("menu.option-present", option != null, TownOptionId + " in menu '" + menu.GameMenu.StringId + "'",
                    option != null ? "ok" : "missing");
                bool usable = option != null && option.GetConditionsHold(Game.Current, menu);
                if (option != null)
                {
                    ctx.Check("menu.option-condition", usable, "condition holds", usable ? "ok" : "false");
                }

                InventorySnapshot before = InventorySnapshot.Capture(town);
                long logOffset = ModLogFile.Length();
                if (usable)
                {
                    option.RunConsequence(menu);
                }
                else
                {
                    ctx.Log("menu option unusable, trading through TradeBehavior.AutoTradeGoodsConsequence");
                    trade.AutoTradeGoodsConsequence(null);
                }

                // A crash in PerformAutoTrade means the screen never opens; stop waiting as soon as it is logged.
                yield return ScenarioStep.Until(() => ScreenFixture.IsInventoryOpen || HasCrashSince(logOffset), OpenTimeoutSeconds,
                    "inventory screen open or '" + CrashLogMarker + "' logged");

                bool crashed = HasCrashSince(logOffset);
                ctx.Check("log.no-crash", !crashed, "no '" + CrashLogMarker + "'", crashed ? "found" : "none");
                bool opened = ScreenFixture.IsInventoryOpen;
                ctx.Check("screen.opened", opened, "inventory state active", opened ? "open" : "not open");
                if (!opened)
                {
                    yield break;
                }

                yield return ScenarioStep.Ticks(ScreenTicks);

                InventorySnapshot open = InventorySnapshot.Capture(town);
                CheckItemsConserved(ctx, "screen.items-conserved", before, open);
                ctx.Check("screen.gold-unbooked", open.HeroGold == before.HeroGold && open.TownGold == before.TownGold,
                    "hero " + before.HeroGold + ", town " + before.TownGold,
                    "hero " + open.HeroGold + ", town " + open.TownGold);

                foreach (ScenarioStep step in ScreenFixture.CloseInventory(CloseAsCancel))
                {
                    yield return step;
                }

                // CloseInventory only waits; DoneLogic may refuse and keep the screen open.
                bool closed = !ScreenFixture.IsInventoryOpen;
                ctx.Check("screen.closed", closed, "no inventory state", closed ? "closed" : "still open");
                if (!closed)
                {
                    yield break;
                }

                InventorySnapshot after = InventorySnapshot.Capture(town);
                CheckItemsConserved(ctx, "done.items-conserved", before, after);

                int heroDelta = after.HeroGold - before.HeroGold;
                int townDelta = after.TownGold - before.TownGold;
                ctx.Check("done.gold-mirrored", heroDelta == -townDelta, "hero delta = -(town delta), no tax",
                    "hero " + heroDelta + ", town " + townDelta);

                IReadOnlyDictionary<string, int> partyDelta = SnapshotDiff.CountDelta(before.PartyItems, after.PartyItems);
                ctx.Check("done.transactions", partyDelta.Count > 0, "at least one item type changed hands",
                    DeltaText.Describe(partyDelta));

                bool ran = ModLogFile.ReadSince(logOffset).IndexOf(RunLogMarker, StringComparison.Ordinal) >= 0;
                ctx.Check("log.run-section", ran, "'" + RunLogMarker + "' appended to " + ModLogFile.Path, ran ? "found" : "missing");

                foreach (ScenarioStep step in TownFixture.LeaveTown())
                {
                    yield return step;
                }

                // LeaveTown only waits; check the outcome itself.
                bool outside = PlayerEncounter.Current == null && MobileParty.MainParty.CurrentSettlement == null;
                ctx.Check("town.left", outside, "no encounter, party outside any settlement",
                    outside ? "outside" : "encounter " + (PlayerEncounter.Current != null ? "open" : "none")
                        + ", settlement " + (MobileParty.MainParty.CurrentSettlement != null ? MobileParty.MainParty.CurrentSettlement.StringId : "none"));
            }
            finally
            {
                settings.PricingMode.SelectedIndex = originalPricing;
                settings.BuyThreshold = originalBuy;
                settings.SellThreshold = originalSell;
                settings.DebugMode = originalDebug;
                AutoTraderMcmSettings.Apply();

                // Cancel an open screen first: Cancel resets the rosters to its own backup.
                TownFixture.LeaveTownNow();
                ctx.Log(backup.Restore());
            }
        }

        private static bool HasCrashSince(long logOffset)
        {
            return ModLogFile.ReadSince(logOffset).IndexOf(CrashLogMarker, StringComparison.Ordinal) >= 0;
        }

        // Trading only moves items between the party and the town, so per item key the sum over
        // party, town and stash must not change.
        private static void CheckItemsConserved(IScenarioContext ctx, string name, InventorySnapshot before, InventorySnapshot now)
        {
            IReadOnlyDictionary<string, int> party = SnapshotDiff.CountDelta(before.PartyItems, now.PartyItems);
            IReadOnlyDictionary<string, int> market = SnapshotDiff.CountDelta(before.TownItems, now.TownItems);
            IReadOnlyDictionary<string, int> stash = SnapshotDiff.CountDelta(before.StashItems, now.StashItems);

            var unbalanced = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (string key in party.Keys.Union(market.Keys).Union(stash.Keys))
            {
                int sum = ValueOrZero(party, key) + ValueOrZero(market, key) + ValueOrZero(stash, key);
                if (sum != 0)
                {
                    unbalanced[key] = sum;
                }
            }

            ctx.Check(name, unbalanced.Count == 0, "party + town + stash unchanged per item",
                unbalanced.Count == 0 ? "ok" : "off by " + DeltaText.Describe(unbalanced));
        }

        private static int ValueOrZero(IReadOnlyDictionary<string, int> counts, string key)
        {
            int value;
            return counts.TryGetValue(key, out value) ? value : 0;
        }
    }
}
