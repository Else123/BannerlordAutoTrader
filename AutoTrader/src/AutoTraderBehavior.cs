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
            // The menu options are added from AutoTraderSubModule.OnAfterGameInitializationFinished, see AddMenuOptions.
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
                // Caravan
                campaignGameStarter.AddPlayerLine("caravan_buy_products", "caravan_talk",
                    "close_window", "{=ATCaravan}I'd like to inspect your wares. (Autotrade)", 
                    new ConversationSentence.OnConditionDelegate(this.AutoTradeGoodsCaravanCondition),
                    new ConversationSentence.OnConsequenceDelegate(this.AutoTradeGoodsCaravanConsequence), 100, null, null);
            }
        }

        /// <summary>
        /// Adds the AutoTrader option to the town, village, port and naval story port menus, each right
        /// behind vanilla's "trade" option (looked up, not a fixed index: an index is a raw List.Insert into
        /// whatever the menu holds at that moment). Menus that are not built yet are skipped, e.g. the
        /// port menus without the NavalDLC.
        /// Must run after every OnAfterSessionLaunched listener has built its menus (NavalDLC builds
        /// "port_menu" there, and mods listening on that event run in reverse load order). Campaign calls
        /// GameManager.OnAfterGameInitializationFinished once per game, right after OnSessionStart, which
        /// invokes both session events, so AutoTraderSubModule calls this from that hook.
        /// </summary>
        public void AddMenuOptions(CampaignGameStarter campaignGameStarter)
        {
            AddMenuOption(campaignGameStarter, MenuOptionAnchors.Town, this.AutoTradeGoodsCondition);
            AddMenuOption(campaignGameStarter, MenuOptionAnchors.Village, this.AutoTradeGoodsVillageCondition);
            AddMenuOption(campaignGameStarter, MenuOptionAnchors.NavalVirtualPort, this.AutoTradeNavalCampaignCondition);
            AddMenuOption(campaignGameStarter, MenuOptionAnchors.Port, this.AutoTradeNavalCampaignCondition);
        }

        private void AddMenuOption(CampaignGameStarter campaignGameStarter, MenuOptionAnchor anchor, GameMenuOption.OnConditionDelegate condition)
        {
            // GetPresumedGameMenu would create an empty placeholder for a menu nobody built; don't.
            GameMenu menu = Campaign.Current.GameMenuManager.GetGameMenu(anchor.MenuId);
            if (menu == null || !menu.IsReady)
            {
                return;
            }

            // If the anchor is missing the option goes in front of the menu's leave option (an index of -1
            // would append behind it), or at the end if that is missing too.
            int index = MenuPlacement.IndexAfter(menu.MenuOptions.Select(option => option.IdString), anchor.AnchorOptionId, anchor.FallbackBeforeOptionId);
            campaignGameStarter.AddGameMenuOption(anchor.MenuId, anchor.OptionId, new TextObject("{=ATTrade}Automatically trade wares", null).ToString(),
                condition, new GameMenuOption.OnConsequenceDelegate(this.AutoTradeGoodsConsequence), false, index, false);
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
