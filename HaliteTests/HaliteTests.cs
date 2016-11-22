using FluentAssertions;
using Xunit;

namespace HaliteTests
{
    public class DirectionIncrementing
    {
        [Fact]
        public void WhenMovingEastSiteLocationShouldIncrementX() { new Site(1, 1, 3, 3).East.X.Should().Be(2); }

        [Fact]
        public void WhenMovingWestSiteLocationShouldDecrementX() { new Site(1, 1, 3, 3).West.X.Should().Be(0); }

        [Fact]
        public void WhenMovingNorthSiteLocationShouldDecrementX() { new Site(1, 1, 3, 3).North.Y.Should().Be(0); }

        [Fact]
        public void WhenMovingSouthSiteLocationShouldIncrementX() { new Site(1, 1, 3, 3).South.Y.Should().Be(2); }

        [Fact]
        public void WhenWrappingEastSiteLocationShouldWrap() { new Site(2, 1, 3, 3).East.X.Should().Be(0); }

        [Fact]
        public void WhenWrappingWestSiteLocationShouldWrap() { new Site(0, 1, 3, 3).West.X.Should().Be(2); }

        [Fact]
        public void WhenWrappingNorthSiteLocationShouldWrap() { new Site(1, 0, 3, 3).North.Y.Should().Be(2); }

        [Fact]
        public void WhenWrappingSouthSiteLocationShouldWrap() { new Site(1, 2, 3, 3).South.Y.Should().Be(0); }
    }

    public class CheckBoundingSquare {}
}