using Fubkalkylator.Core;
using Xunit;

namespace Fubkalkylator.Core.Tests;

public class BarkTests
{
    [Fact]
    public void Fub_is_smaller_than_on_bark()
    {
        double fub = Bark.FubFromOnBark(WoodSpecies.Gran, 10.0);
        Assert.True(fub < 10.0);
        Assert.True(Bark.DoubleBarkThicknessInches(WoodSpecies.Gran, 10.0) > 0);
    }

    [Fact]
    public void Round_trip_fub_on_bark()
    {
        double onBark = Bark.OnBarkFromFub(WoodSpecies.Tall, 9.0);
        Assert.Equal(9.0, Bark.FubFromOnBark(WoodSpecies.Tall, onBark), 9);
    }

    [Fact]
    public void Pine_has_thicker_bark_than_birch()
    {
        double pine = Bark.DoubleBarkThicknessInches(WoodSpecies.Tall, 10.0);
        double birch = Bark.DoubleBarkThicknessInches(WoodSpecies.Bjork, 10.0);
        Assert.True(pine > birch);
    }

    [Fact]
    public void Unknown_matches_excel_five_percent()
    {
        Assert.Equal(1.05, Bark.OverBarkFactor(WoodSpecies.Okant), 9);
    }
}
