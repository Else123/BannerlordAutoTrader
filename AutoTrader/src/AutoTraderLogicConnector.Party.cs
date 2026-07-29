using AutoTrader.Trading;
using AutoTrader.Warsails;
using Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Inventory;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace AutoTrader
{
    // Part of AutoTraderLogicConnector: Party.
    partial class AutoTraderLogicConnector
    {
        public int GetInitialGold()
        {
            // TODO: Use "MobileParty" instead since 1.3?
            AutoTraderHelpers.PrintDebugMessage(" - InitialGold: " + PartyBase.MainParty.Owner.Gold.ToString());
            return PartyBase.MainParty.Owner.Gold;
        }

        public int GetTroopWage()
        {
            // ToDo: Whole daily wage
            AutoTraderHelpers.PrintDebugMessage(" - TroopWage: " + PartyBase.MainParty.MobileParty.TotalWage.ToString());
            return PartyBase.MainParty.MobileParty.TotalWage;
        }

        /// <summary>
        /// Decides once whether the fleet figures apply, and returns both together.
        ///
        /// Weight and capacity must come from the same source: an empty hold legitimately reports
        /// zero weight, so deciding per value (the previous "use it if it is above zero") could
        /// return a land weight while the capacity still came from the fleet.
        /// </summary>
        private static bool TryGetFleetFigures(out float weight, out float capacity)
        {
            weight = 0f;
            capacity = 0f;
            if (!AutoTraderConfig.UseMaxFleetCapacityValue || !WarsailsDetector.IsWarsailsDLCAvailable())
            {
                return false;
            }

            MobileParty party = MobileParty.MainParty;
            if (party == null || party.Ships == null || party.Ships.Count == 0)
            {
                return false;
            }

            int fleetCapacity = WarsailsHelper.GetFleetCargoCapacity(party);
            if (fleetCapacity <= 0)
            {
                return false;
            }

            weight = WarsailsHelper.GetFleetTotalWeightCarried(party);
            capacity = fleetCapacity;
            return true;
        }

        public float GetCurrentWeight()
        {
            float weight;
            float capacity;
            if (TryGetFleetFigures(out weight, out capacity))
            {
                AutoTraderHelpers.PrintDebugMessage(" - Using FleetTotalWeightCarried: " + weight.ToString());
                return weight;
            }

            AutoTraderHelpers.PrintDebugMessage(" - CurrentWeight: " + MobileParty.MainParty.TotalWeightCarried.ToString());
            return MobileParty.MainParty.TotalWeightCarried;
        }

        public float GetInventoryCapacity()
        {
            float weight;
            float capacity;
            if (TryGetFleetFigures(out weight, out capacity))
            {
                AutoTraderHelpers.PrintDebugMessage(" - Using FleetCargoCapacity: " + capacity.ToString());
                return capacity;
            }

            AutoTraderHelpers.PrintDebugMessage(" - InventoryCapacity: " + PartyBase.MainParty.MobileParty.InventoryCapacity.ToString());
            return PartyBase.MainParty.MobileParty.InventoryCapacity;
        }

        public int GetPlayerItemRosterSize()
        {
            AutoTraderHelpers.PrintDebugMessage(" - PartyItemRosterSize: " + PartyBase.MainParty.ItemRoster.Count.ToString());
            return PartyBase.MainParty.MobileParty.ItemRoster.Count;
        }

        public int GetNumPartyMembers()
        {
            // TODO: Use "MobileParty" instead since 1.3?
            AutoTraderHelpers.PrintDebugMessage(" - NumPartyMembers: " + PartyBase.MainParty.NumberOfAllMembers.ToString());
            return PartyBase.MainParty.NumberOfAllMembers;
        }

        public int GetNumLivestockAnimals()
        {
            AutoTraderHelpers.PrintDebugMessage(" - NumLivestockAnimals: " + PartyBase.MainParty.ItemRoster.NumberOfLivestockAnimals.ToString());
            return PartyBase.MainParty.MobileParty.ItemRoster.NumberOfLivestockAnimals;
        }

        // Foot soldiers a spare mount can mount - exactly what the vanilla speed model
        // (DefaultPartySpeedCalculatingModel) uses for the mounted-footmen bonus.
        public int GetNumFootTroops()
        {
            int count = PartyBase.MainParty.NumberOfMenWithoutHorse;
            AutoTraderHelpers.PrintDebugMessage(" - NumFootTroops (menWithoutHorse): " + count.ToString());
            return count;
        }

        private static int CountInventory(Func<ItemObject, bool> predicate)
        {
            int count = 0;
            ItemRoster roster = PartyBase.MainParty.MobileParty.ItemRoster;
            for (int i = 0; i < roster.Count; i++)
            {
                ItemRosterElement e = roster[i];
                ItemObject item = e.EquipmentElement.Item;
                if (item != null && predicate(item))
                {
                    count += e.Amount;
                }
            }
            return count;
        }

        private static bool IsRegularMount(ItemObject item)
        {
            return item.HorseComponent != null && item.HorseComponent.IsMount
                && item.ItemCategory != DefaultItemCategories.WarHorse
                && item.ItemCategory != DefaultItemCategories.NobleHorse;
        }

        public int GetNumRegularRidingMounts()
        {
            return CountInventory(IsRegularMount);
        }

        public int GetNumWarMounts()
        {
            return CountInventory(item => item.HorseComponent != null && item.HorseComponent.IsMount
                && item.ItemCategory == DefaultItemCategories.WarHorse);
        }

        public int GetNumNobleMounts()
        {
            return CountInventory(item => item.HorseComponent != null && item.HorseComponent.IsMount
                && item.ItemCategory == DefaultItemCategories.NobleHorse);
        }

        public int GetNumPackAnimals()
        {
            return CountInventory(item => item.HorseComponent != null && item.HorseComponent.IsPackAnimal);
        }

        // Mounts of the given category that pending troop upgrades would consume.
        private static int CountUpgradeDemand(ItemCategory category)
        {
            int count = 0;
            TroopRoster roster = PartyBase.MainParty.MemberRoster;
            for (int i = 0; i < roster.Count; i++)
            {
                CharacterObject c = roster.GetCharacterAtIndex(i);
                if (c == null || c.IsHero)
                {
                    continue;
                }
                if (c.UpgradeRequiresItemFromCategory == category)
                {
                    count += roster.GetElementNumber(i);
                }
            }
            return count;
        }

        public int GetWarMountUpgradeReserve()
        {
            return CountUpgradeDemand(DefaultItemCategories.WarHorse);
        }

        public int GetNobleMountUpgradeReserve()
        {
            return CountUpgradeDemand(DefaultItemCategories.NobleHorse);
        }

        /// <summary>
        /// The best weapon or armour tier the player and their companions are actually wearing.
        /// Used to decide what counts as loot: gear below this is surplus, gear at or above it
        /// might still be an upgrade for somebody and is kept.
        /// </summary>
        public int GetBestEquippedTier()
        {
            int best = 1;
            foreach (Hero hero in Clan.PlayerClan.Heroes)
            {
                if (hero == null || !hero.IsAlive || hero.PartyBelongedTo != MobileParty.MainParty)
                {
                    continue;
                }

                Equipment equipment = hero.BattleEquipment;
                if (equipment == null)
                {
                    continue;
                }

                for (int slot = 0; slot < (int)EquipmentIndex.NumEquipmentSetSlots; slot++)
                {
                    ItemObject item = equipment[(EquipmentIndex)slot].Item;
                    if (item == null)
                    {
                        continue;
                    }
                    if (!AutoTraderHelpers.IsWeapon(item) && !AutoTraderHelpers.IsArmor(item))
                    {
                        continue;
                    }
                    int tier = (int)item.Tier + 1;
                    if (tier > best)
                    {
                        best = tier;
                    }
                }
            }
            AutoTraderHelpers.PrintDebugMessage(" - BestEquippedTier: " + best.ToString());
            return best;
        }

        /// <summary>
        /// Days the party can keep marching on the food it carries - the same figure the game
        /// shows the player. Scales with party size, unlike a fixed item count.
        /// </summary>
        public int GetFoodDaysRemaining()
        {
            int result = MobileParty.MainParty.GetNumDaysForFoodToLast();
            AutoTraderHelpers.PrintDebugMessage(" - FoodDaysRemaining: " + result.ToString());
            return result;
        }

        public int GetHardwoodCount()
        {
            return PartyBase.MainParty.ItemRoster.GetItemNumber(DefaultItems.HardWood);
        }

        /// <summary>
        /// Ore and ingots the party carries - the materials whose refining and smelting burns
        /// charcoal, and therefore what the hardwood target should scale with.
        /// </summary>
        public int GetRefinableMaterialCount()
        {
            ItemRoster roster = PartyBase.MainParty.MobileParty.ItemRoster;
            int count = 0;
            for (int i = 0; i < roster.Count; i++)
            {
                ItemRosterElement e = roster[i];
                ItemObject item = e.EquipmentElement.Item;
                if (item == null)
                {
                    continue;
                }
                if (item == DefaultItems.IronOre
                    || item == DefaultItems.IronIngot1 || item == DefaultItems.IronIngot2
                    || item == DefaultItems.IronIngot3 || item == DefaultItems.IronIngot4
                    || item == DefaultItems.IronIngot5 || item == DefaultItems.IronIngot6)
                {
                    count += e.Amount;
                }
            }
            AutoTraderHelpers.PrintDebugMessage(" - RefinableMaterials: " + count.ToString());
            return count;
        }

        public List<string> GetPlayerItemRosterNames()
        {
            return PartyBase.MainParty.ItemRoster.Select(x => x.EquipmentElement.Item.Name.ToString()).ToList();
        }

        public int GetItemAmountInPlayerRoster()
        {
            int amount = 0;
            foreach (ItemRosterElement element in PartyBase.MainParty.ItemRoster)
            {
                if (element.EquipmentElement.Item.Name.ToString().Equals(GetItemName()))
                {
                    amount = element.Amount;
                    break;
                }
            }
            AutoTraderHelpers.PrintDebugMessage("- amount in own inventory: " + amount.ToString());
            return amount;
        }

    }
}
