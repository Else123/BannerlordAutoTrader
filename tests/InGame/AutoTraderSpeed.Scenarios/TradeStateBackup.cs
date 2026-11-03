using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;

namespace AutoTraderSpeed.Scenarios
{
    /// <summary>
    /// Gold and item rosters a trade scenario may change: the player hero's gold, the main party's
    /// roster, and one town's gold, market roster and stash. <see cref="Restore"/> puts them back without
    /// game ticks, so a scenario can call it from its finally block. Town ownership and anything vanilla
    /// changed on its own (prosperity, food, AI) are not covered.
    /// </summary>
    internal sealed class TradeStateBackup
    {
        private readonly Settlement _town;
        private readonly int _heroGold;
        private readonly int _townGold;
        private readonly ItemRoster _partyItems;
        private readonly ItemRoster _townItems;
        private readonly ItemRoster _stashItems;

        private TradeStateBackup(Settlement town)
        {
            _town = town;
            _heroGold = Hero.MainHero.Gold;
            _townGold = town.Town.Gold;
            _partyItems = new ItemRoster(MobileParty.MainParty.ItemRoster);
            _townItems = new ItemRoster(town.ItemRoster);
            _stashItems = new ItemRoster(town.Stash);
        }

        public static TradeStateBackup Capture(Settlement town)
        {
            return new TradeStateBackup(town);
        }

        /// <summary>
        /// Restores the captured values and returns a line for the scenario log. Never throws: a cleanup
        /// error must not hide the scenario's own outcome, so it is reported in the returned line instead.
        /// Call it after any open inventory was cancelled, as Cancel resets the rosters to its own backup.
        /// </summary>
        public string Restore()
        {
            try
            {
                Hero.MainHero.ChangeHeroGold(_heroGold - Hero.MainHero.Gold);
                _town.Town.ChangeGold(_townGold - _town.Town.Gold);
                Replace(MobileParty.MainParty.ItemRoster, _partyItems);
                Replace(_town.ItemRoster, _townItems);
                Replace(_town.Stash, _stashItems);
                return "restored hero gold " + _heroGold + ", party items and " + _town.StringId
                    + " gold " + _townGold + ", market and stash";
            }
            catch (Exception e)
            {
                return "restoring gold and items failed: " + e.GetType().Name + ": " + e.Message;
            }
        }

        private static void Replace(ItemRoster target, ItemRoster saved)
        {
            target.Clear();
            target.Add(saved);
        }
    }
}
