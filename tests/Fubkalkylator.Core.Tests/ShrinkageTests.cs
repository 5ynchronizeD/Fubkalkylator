using Fubkalkylator.Core;
using Xunit;

namespace Fubkalkylator.Core.Tests;

public class ShrinkageTests
{
    [Fact]
    public void No_shrinkage_above_fiber_saturation()
    {
        double green = Shrinkage.GreenDimensionInches(6.0, WoodSpecies.Gran, 30.0);
        Assert.Equal(6.0, green, 9);
        Assert.Equal(0, Shrinkage.AllowanceInches(6.0, WoodSpecies.Gran, 30.0), 9);
    }

    [Fact]
    public void Green_dimension_is_larger_when_drying_below_fsp()
    {
        double green = Shrinkage.GreenDimensionInches(6.0, WoodSpecies.Gran, 12.0);
        Assert.True(green > 6.0);
        Assert.True(Shrinkage.AllowanceInches(6.0, WoodSpecies.Gran, 12.0) > 0);
    }

    [Fact]
    public void Drier_target_needs_more_allowance()
    {
        double dry15 = Shrinkage.AllowanceInches(6.0, WoodSpecies.Tall, 15.0);
        double dry8 = Shrinkage.AllowanceInches(6.0, WoodSpecies.Tall, 8.0);
        Assert.True(dry8 > dry15);
    }

    [Fact]
    public void Species_with_more_shrinkage_needs_more_allowance()
    {
        double gran = Shrinkage.AllowanceInches(6.0, WoodSpecies.Gran, 12.0);
        double ek = Shrinkage.AllowanceInches(6.0, WoodSpecies.Ek, 12.0);
        Assert.True(ek > gran);
    }
}
