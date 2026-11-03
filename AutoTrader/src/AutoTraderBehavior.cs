using Helpers;
using AutoTrader.Menu;
using System;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Localization;

namespace AutoTrader
{
    public class TradeBehavior : CampaignBehaviorBase
    {
        private AutoTraderLogic _autoTraderLogic;

        public TradeBehavior(AutoTraderLogic autoTraderLogic)
        {
            _autoTraderLogic = autoTraderLogic;
        }

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnGameLoaded));
            // MbEvent runs listeners in reverse registration order, so OnSessionLaunched listeners of a
            // mod run BEFORE vanilla builds the "town" menu. The town option needs that menu to exist.
            CampaignEvents.OnAfterSessionLaunchedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.AddTownMenuOption));
        }

        public override void SyncData(IDataStore dataStore)
        {
        }

        public void AutoTradeGoodsConsequence(MenuCallbackArgs args)
        {
            _autoTraderLogic.PerformAutoTrade(false);
        }


        private void OnGameLoaded(CampaignGameStarter campaignGameStarter)
        {
            if (campaignGameStarter != null)
                this.AddDialogAndGameMenus(campaignGameStarter);
        }

        private bool AutoTradeGoodsCondition(MenuCallbackArgs args)
        {
            bool shouldBeDisabled;
            TextObject disabledText;
            bool canPlayerDo = Campaign.Current.Models.SettlementAccessModel.CanMainHeroDoSettlementAction(Settlement.CurrentSettlement, SettlementAccessModel.SettlementAction.Trade, out shouldBeDisabled, out disabledText);
            args.optionLeaveType = GameMenuOption.LeaveType.Trade;
            return MenuHelper.SetOptionProperties(args, canPlayerDo, shouldBeDisabled, disabledText);
        }

        private bool AutoTradeGoodsVillageCondition(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Trade;
            return MenuHelper.SetOptionProperties(args, true, false, new TextObject("", null));
        }

        private bool AutoTradeGoodsCaravanCondition()
        {
            return true;
        }

        private void AutoTradeGoodsCaravanConsequence()
        {
            _autoTraderLogic.PerformAutoTrade(true);
            PlayerEncounter.LeaveEncounter = true;
        }

        private void AddDialogAndGameMenus(CampaignGameStarter campaignGameStarter)
        {
            if (campaignGameStarter != null)
            {
                campaignGameStarter.AddGameMenuOption("village", "autotrader_village", new TextObject("{=ATTrade}Automatically trade wares", null).ToString(),
                    new GameMenuOption.OnConditionDelegate(this.AutoTradeGoodsVillageCondition),
                    new GameMenuOption.OnConsequenceDelegate(this.AutoTradeGoodsConsequence), false, 4, false);

                // Caravan
                campaignGameStarter.AddPlayerLine("caravan_buy_products", "caravan_talk",
                    "close_window", "{=ATCaravan}I'd like to inspect your wares. (Autotrade)", 
                    new ConversationSentence.OnConditionDelegate(this.AutoTradeGoodsCaravanCondition),
                    new ConversationSentence.OnConsequenceDelegate(this.AutoTradeGoodsCaravanConsequence), 100, null, null);

                // Naval Campaign
                campaignGameStarter.AddGameMenuOption("naval_storyline_virtualport", "autotrade_naval_campaign", "{=ATTrade}Automatically trade wares", 
                    new GameMenuOption.OnConditionDelegate(this.AutoTradeNavalCampaignCondition), new GameMenuOption.OnConsequenceDelegate(this.AutoTradeGoodsConsequence), false, 4, false, null);

                // Ports
                campaignGameStarter.AddGameMenuOption("port_menu", "autotrade_port", "{=ATTrade}Automatically trade wares",
                    new GameMenuOption.OnConditionDelegate(this.AutoTradeNavalCampaignCondition), new GameMenuOption.OnConsequenceDelegate(this.AutoTradeGoodsConsequence), false, 4, false, null);
            }
        }

        private void AddTownMenuOption(CampaignGameStarter campaignGameStarter)
        {
            if (campaignGameStarter == null)
            {
                return;
            }

            // Placed right behind vanilla's "trade" option, found by lookup instead of a fixed index.
            // This must run after PlayerTownVisitCampaignBehavior built the "town" menu, which is why
            // it listens on OnAfterSessionLaunched (see RegisterEvents). A fixed index (the old 4) is
            // a raw List.Insert into whatever the menu holds at that moment, so it drifts with
            // registration order. If the anchor is missing the option goes in front of "town_leave"
            // (an index of -1 would append behind it), or at the end if that is missing too.
            // GetPresumedGameMenu never returns null (it creates an empty menu).
            GameMenu townMenu = campaignGameStarter.GetPresumedGameMenu("town");
            int index = MenuPlacement.IndexAfter(townMenu.MenuOptions.Select(option => option.IdString), "trade", "town_leave");
            campaignGameStarter.AddGameMenuOption("town", "autotrader_town", new TextObject("{=ATTrade}Automatically trade wares", null).ToString(),
                new GameMenuOption.OnConditionDelegate(this.AutoTradeGoodsCondition),
                new GameMenuOption.OnConsequenceDelegate(this.AutoTradeGoodsConsequence), false, index, false);
        }

        private bool AutoTradeNavalCampaignCondition(MenuCallbackArgs args)
        {
            Settlement currentSettlement = Settlement.CurrentSettlement;
            if (currentSettlement == null || currentSettlement.IsUnderSiege)
            {
                return false;
            }

            bool shouldBeDisabled;
            TextObject disabledText;
            bool canPlayerDo = Campaign.Current.Models.SettlementAccessModel.CanMainHeroDoSettlementAction(Settlement.CurrentSettlement, SettlementAccessModel.SettlementAction.Trade, out shouldBeDisabled, out disabledText);
            args.optionLeaveType = GameMenuOption.LeaveType.Trade;
            return MenuHelper.SetOptionProperties(args, canPlayerDo, shouldBeDisabled, disabledText);
        }

    }
}
