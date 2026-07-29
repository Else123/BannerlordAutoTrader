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
    /// <summary>
    /// The single implementation of <see cref="ILogicConnector"/>: every call into TaleWorlds
    /// lives here, so the decision logic stays engine-free and testable.
    ///
    /// Split across partial files by role - Items, Party, Market, Warehouse - because one flat
    /// class of nearly eighty methods hid which of them were plain queries and which carried
    /// rules. This file keeps the shared state and the inventory session itself.
    /// </summary>
    partial class AutoTraderLogicConnector : ILogicConnector
    {

        public bool _isCaravan = false;
        public bool _isBuying = false;

        private ItemRosterElement _currentItemRosterElement;
        private InventoryLogic _inventoryLogic;
        private int _inventoryDisplayRefreshTicks;

        bool ILogicConnector.IsCaravan { get { return _isCaravan; } set { _isCaravan = value; } }
        bool ILogicConnector.IsBuying { get { return _isBuying; } set { _isBuying = value; } }

        public enum MerchantType
        {
            Town,
            Village,
            Caravan
        }

        public void SetCurrentElementById(int itemId)
        {
            _currentItemRosterElement = GetItemRoster()[itemId];
            AutoTraderHelpers.PrintDebugMessage(" - Current Item: " + _currentItemRosterElement.EquipmentElement.Item.Name.ToString());
        }




        // --- Speed-aware mount trading: per-category inventory counts + upgrade reserves ---

















        // --- Warehouse: store goods the local merchant could not afford ------




        // --- Smithing supply: cheap smeltable weapons as a hardwood source ---





        public bool InitInventory()
        {
            var merchantType = GetMerchantType();
            if (merchantType == MerchantType.Town)
            {
                InventoryScreenHelper.OpenScreenAsTrade(Settlement.CurrentSettlement.ItemRoster, Settlement.CurrentSettlement.Town,
                    InventoryScreenHelper.InventoryCategoryType.None, null);
            }
            else if (merchantType == MerchantType.Village)
            {
                InventoryScreenHelper.OpenScreenAsTrade(Settlement.CurrentSettlement.ItemRoster, Settlement.CurrentSettlement.Village, InventoryScreenHelper.InventoryCategoryType.None, null);
            }
            else if (merchantType == MerchantType.Caravan)
            {
                InventoryScreenHelper.OpenTradeWithCaravanOrAlleyParty(MobileParty.ConversationParty, InventoryScreenHelper.InventoryCategoryType.None);
            }
            else
            {
                return false;
            }

            return TryPrepareInventoryLogic();
        }

        /// <summary>
        /// In 1.4.6, TransactionDebt invokes TotalAmountChange without a null-check.
        /// Autotrading runs before the trade UI wires its handler, so we provide a temporary no-op.
        /// </summary>
        private bool TryPrepareInventoryLogic()
        {
            _inventoryLogic = InventoryScreenHelper.GetActiveInventoryState()?.InventoryLogic;
            if (_inventoryLogic == null)
            {
                AutoTraderHelpers.PrintDebugMessage("Failed to get active inventory logic!");
                return false;
            }

            if (_inventoryLogic.TotalAmountChange == null)
            {
                _inventoryLogic.TotalAmountChange = _ => { };
            }
            return true;
        }

        public void BeginInventoryDisplayRefresh()
        {
            _inventoryDisplayRefreshTicks = 15;
        }

        public void TickInventoryDisplayRefresh()
        {
            if (_inventoryDisplayRefreshTicks <= 0)
            {
                return;
            }

            if (InventoryScreenHelper.GetActiveInventoryState() == null)
            {
                _inventoryDisplayRefreshTicks = 0;
                return;
            }

            _inventoryLogic = InventoryScreenHelper.GetActiveInventoryState()?.InventoryLogic ?? _inventoryLogic;
            _inventoryLogic?.TotalAmountChange?.Invoke(_inventoryLogic.TotalAmount);
            _inventoryDisplayRefreshTicks--;
        }


        private ItemRoster GetItemRoster()
        {
            if (_isBuying)
            {
                if (GetMerchantType() == MerchantType.Caravan)
                    return MobileParty.ConversationParty.ItemRoster;
                return Settlement.CurrentSettlement.ItemRoster;
                
            } else
                return PartyBase.MainParty.ItemRoster;
        }



        public void TransferItem()
        {
            // Generate command
            TransferCommand transferCommand = TransferCommand.Transfer(1,
                _isBuying ? InventoryLogic.InventorySide.OtherInventory : InventoryLogic.InventorySide.PlayerInventory,
                _isBuying ? InventoryLogic.InventorySide.PlayerInventory : InventoryLogic.InventorySide.OtherInventory,
                _currentItemRosterElement, EquipmentIndex.None, EquipmentIndex.None, CharacterObject.PlayerCharacter);
            _inventoryLogic.AddTransferCommand(transferCommand);
            AutoTraderHelpers.PrintDebugMessage(" - Transfer of item " + GetItemName() + " complete! (" + (_isBuying? "Buy" : "Sell") + ")");
        }




        







        public void SetCurrentElementByName(string itemName)
        {
            bool found = false;
            foreach (ItemRosterElement element in GetItemRoster())
            {
                if (element.EquipmentElement.Item.Name.ToString().Equals(itemName))
                {
                    found = true;
                    AutoTraderHelpers.PrintDebugMessage("Found by name: " + element.EquipmentElement.Item.Name.ToString() + " == " + itemName);
                    _currentItemRosterElement = element;
                    break;
                }
            }
            if (!found)
            {
                AutoTraderHelpers.PrintDebugMessage("!!!!! Could not find element with name : " + itemName + " !!!!!");
            }
        }


    }
}
