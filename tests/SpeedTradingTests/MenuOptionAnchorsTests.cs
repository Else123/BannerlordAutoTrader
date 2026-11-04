using System.Linq;
using AutoTrader.Menu;
using Xunit;

namespace SpeedTradingTests
{
    public class MenuOptionAnchorsTests
    {
        [Fact]
        public void All_MenuIdsAreUnique()
        {
            Assert.Equal(MenuOptionAnchors.All.Count, MenuOptionAnchors.All.Select(a => a.MenuId).Distinct().Count());
        }

        [Fact]
        public void All_OptionIdsAreUnique()
        {
            Assert.Equal(MenuOptionAnchors.All.Count, MenuOptionAnchors.All.Select(a => a.OptionId).Distinct().Count());
        }

        [Fact]
        public void All_FallbackIsNotTheAnchor()
        {
            Assert.All(MenuOptionAnchors.All, a => Assert.NotEqual(a.AnchorOptionId, a.FallbackBeforeOptionId));
        }

        [Fact]
        public void All_ContainsTheNamedEntries()
        {
            Assert.Equal(
                new[] { MenuOptionAnchors.Town, MenuOptionAnchors.Village, MenuOptionAnchors.NavalVirtualPort, MenuOptionAnchors.Port },
                MenuOptionAnchors.All);
        }
    }
}
