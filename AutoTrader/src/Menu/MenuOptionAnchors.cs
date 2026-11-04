using System.Collections.Generic;

namespace AutoTrader.Menu
{
    /// <summary>Where one AutoTrader option goes in a vanilla (or DLC) menu.</summary>
    public sealed class MenuOptionAnchor
    {
        public MenuOptionAnchor(string menuId, string optionId, string anchorOptionId, string fallbackBeforeOptionId)
        {
            MenuId = menuId;
            OptionId = optionId;
            AnchorOptionId = anchorOptionId;
            FallbackBeforeOptionId = fallbackBeforeOptionId;
        }

        public string MenuId { get; }
        public string OptionId { get; }

        /// <summary>The option the new one is placed directly behind.</summary>
        public string AnchorOptionId { get; }

        /// <summary>Option to place the new one in front of if the anchor is missing (the menu's leave option).</summary>
        public string FallbackBeforeOptionId { get; }
    }

    /// <summary>All menus that get an AutoTrader option, each placed right behind the vanilla "trade" option.</summary>
    public static class MenuOptionAnchors
    {
        public static MenuOptionAnchor Town { get; } = new MenuOptionAnchor("town", "autotrader_town", "trade", "town_leave");
        public static MenuOptionAnchor Village { get; } = new MenuOptionAnchor("village", "autotrader_village", "trade", "leave");
        public static MenuOptionAnchor NavalVirtualPort { get; } = new MenuOptionAnchor("naval_storyline_virtualport", "autotrade_naval_campaign", "trade", "port_leave");
        public static MenuOptionAnchor Port { get; } = new MenuOptionAnchor("port_menu", "autotrade_port", "trade", "sail_option");

        public static IReadOnlyList<MenuOptionAnchor> All { get; } = new[] { Town, Village, NavalVirtualPort, Port };
    }
}
