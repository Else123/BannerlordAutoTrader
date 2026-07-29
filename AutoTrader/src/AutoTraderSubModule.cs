using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Localization;
using TaleWorlds.Library;

namespace AutoTrader
{
    public class AutoTraderSubModule : MBSubModuleBase
    {
		private AutoTraderLogic _autoTraderLogic;

		protected override void OnSubModuleLoad()
		{
			base.OnSubModuleLoad();
			AutoTraderHelpers.Initialize();
			_autoTraderLogic = new AutoTraderLogic(new AutoTraderLogicConnector());
		}

		public override void OnGameInitializationFinished(Game game)
		{
			// Pull the settings in once the game is up, so startup messages and the first trade
			// run already reflect what the player configured rather than the bare defaults.
			AutoTraderMcmSettings.Apply();

			AutoTraderHelpers.PrintMessage("Thanks for using AutoTrader! Configure it in Options -> Mod Options (MCM).");

			string fullVersion = ApplicationVersion.FromParametersFile(null).ToString();
			string[] versionParts = fullVersion.Split('.');
			string version = versionParts.Length >= 3
				? $"{versionParts[0]}.{versionParts[1]}.{versionParts[2]}"
				: fullVersion;

			if (!version.Equals(AutoTraderConfig.AutoTraderGameVersion))
			{
				AutoTraderHelpers.PrintMessage(new TextObject("{=ATVersionMismatch01}You are using AutoTrader for ", null).ToString()
					+ AutoTraderConfig.AutoTraderGameVersion
					+ new TextObject("{=ATVersionMismatch02} with Bannerlord ", null).ToString()
					+ version
					+ new TextObject("{=ATVersionMismatch03}. If you encounter issues please check the mod page for a fitting version.", null).ToString());
			}

			if (AutoTraderConfig.DebugMode)
			{
				AutoTraderHelpers.PrintMessage("WARNING: AutoTrader debug mode is active. This will make autotrading drastically slower.");
			}
		}

		protected override void OnApplicationTick(float dt)
		{
			if (Game.Current != null)
			{
				_autoTraderLogic.OnApplicationTick();
			}
		}

		protected override void InitializeGameStarter(Game game, IGameStarter gameStarterObject)
		{
			if (game.GameType is Campaign)
			{
				CampaignGameStarter gameInitializer = (CampaignGameStarter)gameStarterObject;
				this.AddBehaviors(gameInitializer);
			}
		}

		private void AddBehaviors(CampaignGameStarter gameStarterObject)
		{
			gameStarterObject.AddBehavior(new TradeBehavior(_autoTraderLogic));
			gameStarterObject.AddBehavior(new AutoTrader.Warehouse.WarehouseBehavior());
		}
	}
}
