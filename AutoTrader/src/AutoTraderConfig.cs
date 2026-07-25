using System;
using System.IO;
using System.Text;
using System.Xml;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace AutoTrader
{
    public static class AutoTraderConfig
    {
        private static PlatformFilePath _configFile;

        // This fork is built and tested against v1.4.7 (War Sails).
        public static string AutoTraderGameVersion { get; } = "v1.4.7";

        public static int BuyThresholdValue { get; set; } = 90;
        public static int SellThresholdValue { get; set; } = 100;
        public static bool SimpleTradingAI { get; set; } = true;
        public static bool UseWeightedValue { get; set; } = false;
        public static int MaxCapacityValue { get; set; } = 15;
        // Days of marching the party must always have food for. Replaces the old fixed item
        // minimums, which starved a large party and overstocked a small one.
        public static int KeepFoodDaysValue { get; set; } = 14;
        public static int KeepGrainsMinValue { get; set; } = 10;
        public static int KeepGrainsMaxValue { get; set; } = 100;
        public static int KeepConsumablesMinValue { get; set; } = 4;
        public static int KeepConsumablesMaxValue { get; set; } = 20;
        public static int UseInventorySpaceValue { get; set; } = 90;
        public static bool UseMaxFleetCapacityValue { get; set; } = false;
        public static int KeepWagesValue { get; set; } = 3;
        public static int SearchRadiusValue { get; set; } = 300;
        public static int WeaponsArmorTierValue { get; set; } = 2;

        public static bool SellSmithingValue { get; set; } = false;
        public static bool KeepSmeltingValue { get; set; } = false;
        // Buy cheap smeltable weapons as a hardwood source for smithing (coupled to need).
        public static bool BuySmeltablesForHardwoodValue { get; set; } = false;
        public static int SmeltHardwoodTargetValue { get; set; } = 100;
        public static bool ResupplyHardwoodValue { get; set; } = false;
        public static bool ResupplyValue { get; set; } = true;
        public static bool JunkCattleValue { get; set; } = false;

        public static bool BuyHorsesValue { get; set; } = true;
        // Legacy toggle: allow selling horses at all. Only relevant when speed-aware mount trading
        // is off - with it on, the per-category mount plan governs selling instead.
        public static bool SellHorsesValue { get; set; } = false;
        // Speed-aware mode: never sell pack animals, not even as herd surplus.
        public static bool ProtectPackAnimalsValue { get; set; } = false;

        // Speed-aware mount trading: trade mounts so party speed stays optimal.
        public static bool SpeedAwareMountsValue { get; set; } = true;
        // Keep war/noble mounts needed for pending troop upgrades instead of selling them.
        public static bool ReserveUpgradeMountsValue { get; set; } = true;
        // Allow selling surplus noble mounts (off by default: noble mounts are the most valuable).
        public static bool SellNobleMountsValue { get; set; } = false;
        // Sell pack animals above the herd allowance (they slow the party down).
        public static bool ManagePackAnimalHerdValue { get; set; } = true;
        // Never sell a mount priced at or above this value (protects unique/named mounts whose
        // item category is not war_horse/noble_horse). 0 disables the guard.
        public static int KeepMountsAboveValueValue { get; set; } = 2000;
        // Sell livestock (cattle/sheep) above the herd allowance; they slow the party down and,
        // unlike pack animals, provide no cargo capacity.
        public static bool ManageLivestockHerdValue { get; set; } = true;
        // Livestock to keep regardless of the herd (food reserve).
        public static int KeepLivestockReserveValue { get; set; } = 5;
        public static bool BuyWeaponsValue { get; set; } = false;
        public static bool SellWeaponsValue { get; set; } = true;
        public static bool BuyArmorValue { get; set; } = false;
        public static bool SellArmorValue { get; set; } = true;
        public static bool BuyGoodsValue { get; set; } = true;
        public static bool SellGoodsValue { get; set; } = true;
        public static bool BuyConsumablesValue { get; set; } = true;
        public static bool SellConsumablesValue { get; set; } = true;
        public static bool BuyLivestockValue { get; set; } = false;
        public static bool SellLivestockValue { get; set; } = true;

        // Town warehouse (the vanilla settlement stash).
        public const int WarehouseOff = 0;
        public const int WarehouseDeposit = 1;
        public const int WarehouseConsign = 2;
        public const int WarehouseCaravans = 3;
        // The cut a player-owned caravan takes for hauling warehouse goods away.
        public static int CaravanCommissionPercentValue { get; set; } = 15;
        public static int WarehouseModeValue { get; set; } = WarehouseOff;
        // Share of the town's gold that may be spent on the warehouse per day.
        public static int ConsignmentSharePercentValue { get; set; } = 25;
        // Items below this unit price stay in the warehouse instead of flooding the market.
        public static int ConsignmentMinPriceValue { get; set; } = 0;

        public static int Version { get; set; } = 2;
        // On by default while this fork is in testing: the decision log is what makes in-game
        // behaviour diagnosable afterwards.
        public static bool DebugMode { get; set; } = true;

        public static void Initialize()
        {
            // Get the config file path
            AutoTraderConfig._configFile = new PlatformFilePath(EngineFilePaths.ConfigsPath, "AutoTraderConfig.xml");

            // If it does not exist, create it by an initial save
            if (!FileHelper.FileExists(AutoTraderConfig._configFile))
            {
                AutoTraderConfig.Save();
            }
            else
            {
                // Read it
                var content = FileHelper.GetFileContentString(AutoTraderConfig._configFile);
                var stringReader = new System.IO.StringReader(content);
                XmlTextReader textReader = new XmlTextReader(stringReader);

                bool found_version = false;
                while (textReader.Read())
                {
                    if (textReader.IsStartElement())
                    {
                        if (textReader.Name == "version")
                        {
                            found_version = true;
                        }
                    }
                }

                textReader.Close();

                if (!found_version)
                {
                    // Reset
                    AutoTraderConfig.Save();
                }

                content = FileHelper.GetFileContentString(AutoTraderConfig._configFile);
                stringReader = new System.IO.StringReader(content);
                textReader = new XmlTextReader(stringReader);

                while (textReader.Read())
                {
                    if (textReader.IsStartElement())
                    {
                        if (textReader.Name == "buyThresholdValue")
                        {
                            AutoTraderConfig.BuyThresholdValue = Int32.Parse(textReader.ReadString());
                        }
                        else if (textReader.Name == "simpleTradingAI")
                        {
                            AutoTraderConfig.SimpleTradingAI = Boolean.Parse(textReader.ReadString());
                        }
                        else if (textReader.Name == "useWeightedValue")
                        {
                            AutoTraderConfig.UseWeightedValue = Boolean.Parse(textReader.ReadString());
                        }
                        else if (textReader.Name == "sellThresholdValue")
                        {
                            AutoTraderConfig.SellThresholdValue = Int32.Parse(textReader.ReadString());
                        }
                        else if (textReader.Name == "maxCapacityValue")
                        {
                            AutoTraderConfig.MaxCapacityValue = Int32.Parse(textReader.ReadString());
                        }
                        else if (textReader.Name == "keepFoodDaysValue")
                        {
                            AutoTraderConfig.KeepFoodDaysValue = Int32.Parse(textReader.ReadString());
                        }
                        else if (textReader.Name == "keepGrainsMinValue")
                        {
                            AutoTraderConfig.KeepGrainsMinValue = Int32.Parse(textReader.ReadString());
                        }
                        else if (textReader.Name == "keepGrainsMaxValue")
                        {
                            AutoTraderConfig.KeepGrainsMaxValue = Int32.Parse(textReader.ReadString());
                        }
                        else if (textReader.Name == "keepConsumablesMinValue")
                        {
                            AutoTraderConfig.KeepConsumablesMinValue = Int32.Parse(textReader.ReadString());
                        }
                        else if (textReader.Name == "keepConsumablesMaxValue")
                        {
                            AutoTraderConfig.KeepConsumablesMaxValue = Int32.Parse(textReader.ReadString());
                        }
                        else if (textReader.Name == "useInventorySpaceValue")
                        {
                            AutoTraderConfig.UseInventorySpaceValue = Int32.Parse(textReader.ReadString());
                        }
                        else if (textReader.Name == "useMaxFleetCapacityValue")
                        {
                            AutoTraderConfig.UseMaxFleetCapacityValue = Boolean.Parse(textReader.ReadString());
                        }
                        else if (textReader.Name == "searchRadiusValue")
                        {
                            AutoTraderConfig.SearchRadiusValue = Int32.Parse(textReader.ReadString());
                        }
                        else if (textReader.Name == "weaponsArmorTierValue")
                        {
                            AutoTraderConfig.WeaponsArmorTierValue = Int32.Parse(textReader.ReadString());
                        }
                        else if (textReader.Name == "keepWagesValue")
                        {
                            AutoTraderConfig.KeepWagesValue = Int32.Parse(textReader.ReadString());
                        }
                        else if (textReader.Name == "resupplyValue")
                        {
                            AutoTraderConfig.ResupplyValue = Boolean.Parse(textReader.ReadString());
                        }
                        else if (textReader.Name == "junkCattleValue")
                        {
                            AutoTraderConfig.JunkCattleValue = Boolean.Parse(textReader.ReadString());
                        }
                        else if (textReader.Name == "sellSmithingValue")
                        {
                            AutoTraderConfig.SellSmithingValue = Boolean.Parse(textReader.ReadString());
                        }
                        else if (textReader.Name == "keepSmeltingValue")
                        {
                            AutoTraderConfig.KeepSmeltingValue = Boolean.Parse(textReader.ReadString());
                        }
                        else if (textReader.Name == "buySmeltablesForHardwoodValue")
                        {
                            AutoTraderConfig.BuySmeltablesForHardwoodValue = Boolean.Parse(textReader.ReadString());
                        }
                        else if (textReader.Name == "smeltHardwoodTargetValue")
                        {
                            AutoTraderConfig.SmeltHardwoodTargetValue = Int32.Parse(textReader.ReadString());
                        }
                        else if (textReader.Name == "resupplyHardwoodValue")
                        {
                            AutoTraderConfig.ResupplyHardwoodValue = Boolean.Parse(textReader.ReadString());
                        }
                        else if (textReader.Name == "buyHorsesValue")
                        {
                            AutoTraderConfig.BuyHorsesValue = Boolean.Parse(textReader.ReadString());
                        }
                        else if (textReader.Name == "sellHorsesValue")
                        {
                            AutoTraderConfig.SellHorsesValue = Boolean.Parse(textReader.ReadString());
                        }
                        else if (textReader.Name == "protectPackAnimalsValue")
                        {
                            AutoTraderConfig.ProtectPackAnimalsValue = Boolean.Parse(textReader.ReadString());
                        }
                        else if (textReader.Name == "speedAwareMountsValue")
                        {
                            AutoTraderConfig.SpeedAwareMountsValue = Boolean.Parse(textReader.ReadString());
                        }
                        else if (textReader.Name == "reserveUpgradeMountsValue")
                        {
                            AutoTraderConfig.ReserveUpgradeMountsValue = Boolean.Parse(textReader.ReadString());
                        }
                        else if (textReader.Name == "sellNobleMountsValue")
                        {
                            AutoTraderConfig.SellNobleMountsValue = Boolean.Parse(textReader.ReadString());
                        }
                        else if (textReader.Name == "managePackAnimalHerdValue")
                        {
                            AutoTraderConfig.ManagePackAnimalHerdValue = Boolean.Parse(textReader.ReadString());
                        }
                        else if (textReader.Name == "keepMountsAboveValueValue")
                        {
                            AutoTraderConfig.KeepMountsAboveValueValue = Int32.Parse(textReader.ReadString());
                        }
                        else if (textReader.Name == "warehouseModeValue")
                        {
                            AutoTraderConfig.WarehouseModeValue = Int32.Parse(textReader.ReadString());
                        }
                        else if (textReader.Name == "consignmentSharePercentValue")
                        {
                            AutoTraderConfig.ConsignmentSharePercentValue = Int32.Parse(textReader.ReadString());
                        }
                        else if (textReader.Name == "consignmentMinPriceValue")
                        {
                            AutoTraderConfig.ConsignmentMinPriceValue = Int32.Parse(textReader.ReadString());
                        }
                        else if (textReader.Name == "caravanCommissionPercentValue")
                        {
                            AutoTraderConfig.CaravanCommissionPercentValue = Int32.Parse(textReader.ReadString());
                        }
                        else if (textReader.Name == "manageLivestockHerdValue")
                        {
                            AutoTraderConfig.ManageLivestockHerdValue = Boolean.Parse(textReader.ReadString());
                        }
                        else if (textReader.Name == "keepLivestockReserveValue")
                        {
                            AutoTraderConfig.KeepLivestockReserveValue = Int32.Parse(textReader.ReadString());
                        }
                        else if (textReader.Name == "buyArmorValue")
                        {
                            AutoTraderConfig.BuyArmorValue = Boolean.Parse(textReader.ReadString());
                        }
                        else if (textReader.Name == "sellArmorValue")
                        {
                            AutoTraderConfig.SellArmorValue = Boolean.Parse(textReader.ReadString());
                        }
                        else if (textReader.Name == "buyWeaponsValue")
                        {
                            AutoTraderConfig.BuyWeaponsValue = Boolean.Parse(textReader.ReadString());
                        }
                        else if (textReader.Name == "sellWeaponsValue")
                        {
                            AutoTraderConfig.SellWeaponsValue = Boolean.Parse(textReader.ReadString());
                        }
                        else if (textReader.Name == "buyGoodsValue")
                        {
                            AutoTraderConfig.BuyGoodsValue = Boolean.Parse(textReader.ReadString());
                        }
                        else if (textReader.Name == "sellGoodsValue")
                        {
                            AutoTraderConfig.SellGoodsValue = Boolean.Parse(textReader.ReadString());
                        }
                        else if (textReader.Name == "buyConsumablesValue")
                        {
                            AutoTraderConfig.BuyConsumablesValue = Boolean.Parse(textReader.ReadString());
                        }
                        else if (textReader.Name == "sellConsumablesValue")
                        {
                            AutoTraderConfig.SellConsumablesValue = Boolean.Parse(textReader.ReadString());
                        }
                        else if (textReader.Name == "buyLivestockValue")
                        {
                            AutoTraderConfig.BuyLivestockValue = Boolean.Parse(textReader.ReadString());
                        }
                        else if (textReader.Name == "sellLivestockValue")
                        {
                            AutoTraderConfig.SellLivestockValue = Boolean.Parse(textReader.ReadString());
                        }
                        else if (textReader.Name == "debugMode")
                        {
                            AutoTraderConfig.DebugMode = Boolean.Parse(textReader.ReadString());
                        }
                    }
                }
            }
        }

        public static void Save()
        {
            // Open writer and write settings
            XmlDocument xmlDocument = new XmlDocument();
            XmlWriterSettings xmlWriterSettings = new XmlWriterSettings();
            xmlWriterSettings.Indent = true;
            xmlWriterSettings.IndentChars = "  ";
            xmlWriterSettings.NewLineChars = "\r\n";
            xmlWriterSettings.NewLineHandling = NewLineHandling.Replace;
            StringBuilder sb = new StringBuilder();

            //using (XmlTextWriter textWriter = XmlWriter.Create(xmlDocument.CreateNavigator().AppendChild(), xmlWriterSettings))
            using (XmlWriter textWriter = XmlWriter.Create(sb, xmlWriterSettings))
            {
                textWriter.WriteStartElement("config");

                textWriter.WriteElementString("version", AutoTraderConfig.Version.ToString());

                textWriter.WriteElementString("simpleTradingAI", AutoTraderConfig.SimpleTradingAI.ToString());
                textWriter.WriteElementString("useWeightedValue", AutoTraderConfig.UseWeightedValue.ToString());
                textWriter.WriteElementString("buyThresholdValue", AutoTraderConfig.BuyThresholdValue.ToString());
                textWriter.WriteElementString("sellThresholdValue", AutoTraderConfig.SellThresholdValue.ToString());
                textWriter.WriteElementString("maxCapacityValue", AutoTraderConfig.MaxCapacityValue.ToString());
                textWriter.WriteElementString("keepFoodDaysValue", AutoTraderConfig.KeepFoodDaysValue.ToString());
                textWriter.WriteElementString("keepGrainsMinValue", AutoTraderConfig.KeepGrainsMinValue.ToString());
                textWriter.WriteElementString("keepGrainsMaxValue", AutoTraderConfig.KeepGrainsMaxValue.ToString());
                textWriter.WriteElementString("keepConsumablesMinValue", AutoTraderConfig.KeepConsumablesMinValue.ToString());
                textWriter.WriteElementString("keepConsumablesMaxValue", AutoTraderConfig.KeepConsumablesMaxValue.ToString());
                textWriter.WriteElementString("useInventorySpaceValue", AutoTraderConfig.UseInventorySpaceValue.ToString());
                textWriter.WriteElementString("useMaxFleetCapacityValue", AutoTraderConfig.UseMaxFleetCapacityValue.ToString());
                textWriter.WriteElementString("keepWagesValue", AutoTraderConfig.KeepWagesValue.ToString());
                textWriter.WriteElementString("searchRadiusValue", AutoTraderConfig.SearchRadiusValue.ToString());
                textWriter.WriteElementString("weaponsArmorTierValue", AutoTraderConfig.WeaponsArmorTierValue.ToString());

                textWriter.WriteElementString("junkCattleValue", AutoTraderConfig.JunkCattleValue.ToString());
                textWriter.WriteElementString("resupplyValue", AutoTraderConfig.ResupplyValue.ToString());
                textWriter.WriteElementString("sellSmithingValue", AutoTraderConfig.SellSmithingValue.ToString());
                textWriter.WriteElementString("keepSmeltingValue", AutoTraderConfig.KeepSmeltingValue.ToString());
                textWriter.WriteElementString("buySmeltablesForHardwoodValue", AutoTraderConfig.BuySmeltablesForHardwoodValue.ToString());
                textWriter.WriteElementString("smeltHardwoodTargetValue", AutoTraderConfig.SmeltHardwoodTargetValue.ToString());
                textWriter.WriteElementString("resupplyHardwoodValue", AutoTraderConfig.ResupplyHardwoodValue.ToString());

                textWriter.WriteElementString("buyHorsesValue", AutoTraderConfig.BuyHorsesValue.ToString());
                textWriter.WriteElementString("sellHorsesValue", AutoTraderConfig.SellHorsesValue.ToString());
                textWriter.WriteElementString("protectPackAnimalsValue", AutoTraderConfig.ProtectPackAnimalsValue.ToString());
                textWriter.WriteElementString("speedAwareMountsValue", AutoTraderConfig.SpeedAwareMountsValue.ToString());
                textWriter.WriteElementString("reserveUpgradeMountsValue", AutoTraderConfig.ReserveUpgradeMountsValue.ToString());
                textWriter.WriteElementString("sellNobleMountsValue", AutoTraderConfig.SellNobleMountsValue.ToString());
                textWriter.WriteElementString("managePackAnimalHerdValue", AutoTraderConfig.ManagePackAnimalHerdValue.ToString());
                textWriter.WriteElementString("keepMountsAboveValueValue", AutoTraderConfig.KeepMountsAboveValueValue.ToString());
                textWriter.WriteElementString("warehouseModeValue", AutoTraderConfig.WarehouseModeValue.ToString());
                textWriter.WriteElementString("consignmentSharePercentValue", AutoTraderConfig.ConsignmentSharePercentValue.ToString());
                textWriter.WriteElementString("consignmentMinPriceValue", AutoTraderConfig.ConsignmentMinPriceValue.ToString());
                textWriter.WriteElementString("caravanCommissionPercentValue", AutoTraderConfig.CaravanCommissionPercentValue.ToString());
                textWriter.WriteElementString("manageLivestockHerdValue", AutoTraderConfig.ManageLivestockHerdValue.ToString());
                textWriter.WriteElementString("keepLivestockReserveValue", AutoTraderConfig.KeepLivestockReserveValue.ToString());
                textWriter.WriteElementString("buyArmorValue", AutoTraderConfig.BuyArmorValue.ToString());
                textWriter.WriteElementString("sellArmorValue", AutoTraderConfig.SellArmorValue.ToString());
                textWriter.WriteElementString("buyWeaponsValue", AutoTraderConfig.BuyWeaponsValue.ToString());
                textWriter.WriteElementString("sellWeaponsValue", AutoTraderConfig.SellWeaponsValue.ToString());
                textWriter.WriteElementString("buyGoodsValue", AutoTraderConfig.BuyGoodsValue.ToString());
                textWriter.WriteElementString("sellGoodsValue", AutoTraderConfig.SellGoodsValue.ToString());
                textWriter.WriteElementString("buyConsumablesValue", AutoTraderConfig.BuyConsumablesValue.ToString());
                textWriter.WriteElementString("sellConsumablesValue", AutoTraderConfig.SellConsumablesValue.ToString());
                textWriter.WriteElementString("buyLivestockValue", AutoTraderConfig.BuyLivestockValue.ToString());
                textWriter.WriteElementString("sellLivestockValue", AutoTraderConfig.SellLivestockValue.ToString());

                textWriter.WriteElementString("debugMode", AutoTraderConfig.DebugMode.ToString());

                textWriter.WriteEndElement();
            }

            FileHelper.SaveFileString(AutoTraderConfig._configFile, sb.ToString());
        }
    }
}
