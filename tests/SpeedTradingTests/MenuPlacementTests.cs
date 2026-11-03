using AutoTrader.Menu;
using Xunit;

namespace SpeedTradingTests
{
    public class MenuPlacementTests
    {
        [Fact]
        public void IndexAfter_ReturnsPositionDirectlyBehindAnchor()
        {
            string[] ids = { "town_keep", "town_arena", "trade", "town_smithy", "town_leave" };

            Assert.Equal(3, MenuPlacement.IndexAfter(ids, "trade"));
        }

        [Fact]
        public void IndexAfter_AnchorAtEnd_ReturnsCount()
        {
            string[] ids = { "a", "trade" };

            Assert.Equal(2, MenuPlacement.IndexAfter(ids, "trade"));
        }

        [Fact]
        public void IndexAfter_DuplicateAnchor_UsesFirst()
        {
            string[] ids = { "trade", "x", "trade" };

            Assert.Equal(1, MenuPlacement.IndexAfter(ids, "trade"));
        }

        [Fact]
        public void IndexAfter_MissingAnchor_ReturnsAppendValue()
        {
            Assert.Equal(-1, MenuPlacement.IndexAfter(new[] { "a", "b" }, "trade"));
        }

        [Fact]
        public void IndexAfter_MissingAnchor_ReturnsIndexOfFallbackOption()
        {
            string[] ids = { "town_keep", "town_arena", "town_leave" };

            Assert.Equal(2, MenuPlacement.IndexAfter(ids, "trade", "town_leave"));
        }

        [Fact]
        public void IndexAfter_AnchorPresent_IgnoresFallbackOption()
        {
            string[] ids = { "trade", "town_keep", "town_leave" };

            Assert.Equal(1, MenuPlacement.IndexAfter(ids, "trade", "town_leave"));
        }

        [Fact]
        public void IndexAfter_MissingAnchorAndFallback_ReturnsAppendValue()
        {
            Assert.Equal(-1, MenuPlacement.IndexAfter(new[] { "a", "b" }, "trade", "town_leave"));
        }

        [Fact]
        public void IndexAfter_EmptyMenu_ReturnsAppendValue()
        {
            Assert.Equal(-1, MenuPlacement.IndexAfter(new string[0], "trade"));
        }
    }
}
