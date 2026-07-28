using AutoTrader.Smithing;
using Xunit;

namespace SpeedTradingTests
{
    public class HardwoodTargetTests
    {
        private const int NoCeiling = 0;

        [Fact]
        public void WithoutMaterials_TheConfiguredMinimumApplies()
        {
            Assert.Equal(100, HardwoodTarget.Effective(configuredMinimum: 100, refinableMaterials: 0,
                percentPerMaterial: 50, maximum: NoCeiling));
        }

        [Fact]
        public void ALargeStockRaisesTheTargetWellAboveTheMinimum()
        {
            // The case that prompted this: 20k iron must not leave the target sitting at 100.
            Assert.Equal(10000, HardwoodTarget.Effective(100, refinableMaterials: 20000,
                percentPerMaterial: 50, maximum: NoCeiling));
        }

        [Fact]
        public void TheCeilingKeepsAHugeStockpileCarryable()
        {
            // Seen in a playthrough: 54772 ore and ingots scaled to a target of 27386 logs, which
            // the trader would have kept buying into an already overloaded party.
            Assert.Equal(2000, HardwoodTarget.Effective(100, refinableMaterials: 54772,
                percentPerMaterial: 50, maximum: 2000));
        }

        [Fact]
        public void TheMinimumWinsWhileTheStockIsSmall()
        {
            Assert.Equal(100, HardwoodTarget.Effective(100, refinableMaterials: 50,
                percentPerMaterial: 50, maximum: 2000));
        }

        [Fact]
        public void TheMinimumStillWinsOverALowerCeiling()
        {
            Assert.Equal(500, HardwoodTarget.Effective(configuredMinimum: 500, refinableMaterials: 20000,
                percentPerMaterial: 50, maximum: 200));
        }

        [Fact]
        public void ZeroPercentPinsTheTargetToTheMinimum()
        {
            Assert.Equal(100, HardwoodTarget.Effective(100, refinableMaterials: 20000,
                percentPerMaterial: 0, maximum: 2000));
        }

        [Fact]
        public void SmallStocksKeepTheirShare()
        {
            // Dividing before multiplying would truncate this to 0 and fall back to the minimum.
            Assert.Equal(125, HardwoodTarget.Effective(0, refinableMaterials: 250,
                percentPerMaterial: 50, maximum: NoCeiling));
        }
    }
}
