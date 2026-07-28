using AutoTrader.Smithing;
using Xunit;

namespace SpeedTradingTests
{
    public class HardwoodTargetTests
    {
        [Fact]
        public void WithoutMaterials_TheConfiguredMinimumApplies()
        {
            Assert.Equal(100, HardwoodTarget.Effective(configuredMinimum: 100, refinableMaterials: 0, percentPerMaterial: 50));
        }

        [Fact]
        public void ALargeStockRaisesTheTargetWellAboveTheMinimum()
        {
            // The case that prompted this: 20k iron must not leave the target sitting at 100.
            Assert.Equal(10000, HardwoodTarget.Effective(100, refinableMaterials: 20000, percentPerMaterial: 50));
        }

        [Fact]
        public void TheMinimumWinsWhileTheStockIsSmall()
        {
            Assert.Equal(100, HardwoodTarget.Effective(100, refinableMaterials: 50, percentPerMaterial: 50));
        }

        [Fact]
        public void ZeroPercentPinsTheTargetToTheMinimum()
        {
            Assert.Equal(100, HardwoodTarget.Effective(100, refinableMaterials: 20000, percentPerMaterial: 0));
        }

        [Fact]
        public void SmallStocksKeepTheirShare()
        {
            // Dividing before multiplying would truncate this to 0 and fall back to the minimum.
            Assert.Equal(125, HardwoodTarget.Effective(0, refinableMaterials: 250, percentPerMaterial: 50));
        }
    }
}
