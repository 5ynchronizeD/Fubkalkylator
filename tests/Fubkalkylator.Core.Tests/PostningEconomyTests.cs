using Fubkalkylator.Core;
using Xunit;

namespace Fubkalkylator.Core.Tests;

public class PostningEconomyTests
{
    // 4 m stock i tum.
    private const double FourMetersInInches = 4.0 / SawConstants.MetersPerInch;

    [Fact]
    public void Reference_case_gives_positive_volume_and_value()
    {
        var r = PostningsMax.Compute(9.75);
        var e = PostningEconomy.Compute(r, FourMetersInInches, PriceList.Default);

        Assert.True(e.TimberVolumeM3 > 0);
        Assert.True(e.LogVolumeM3 > e.TimberVolumeM3);   // virket ryms i stocken
        Assert.True(e.BoardFeet > 0);
        Assert.True(e.TotalValue > 0);
        Assert.InRange(e.YieldPercent, 1, 100);
    }

    [Fact]
    public void Volume_scales_linearly_with_length()
    {
        var r = PostningsMax.Compute(9.75);
        var single = PostningEconomy.Compute(r, 100, PriceList.Default);
        var doubled = PostningEconomy.Compute(r, 200, PriceList.Default);

        Assert.Equal(single.TimberVolumeM3 * 2, doubled.TimberVolumeM3, 9);
        Assert.Equal(single.TotalValue * 2, doubled.TotalValue, 6);
    }

    [Fact]
    public void Zero_length_gives_zero_volume_and_value()
    {
        var r = PostningsMax.Compute(9.75);
        var e = PostningEconomy.Compute(r, 0, PriceList.Default);

        Assert.Equal(0, e.TimberVolumeM3);
        Assert.Equal(0, e.TotalValue);
    }

    [Fact]
    public void Value_follows_price_list()
    {
        var r = PostningsMax.Compute(9.75);
        var prices = new PriceList { BlockPerCubicMeter = 2000, ByproductPerCubicMeter = 0 };
        var e = PostningEconomy.Compute(r, FourMetersInInches, prices);

        Assert.Equal(0, e.ByproductValue);
        Assert.Equal(e.BlockVolumeM3 * 2000, e.BlockValue, 6);
        Assert.Equal(e.BlockValue, e.TotalValue, 6);
    }

    [Fact]
    public void Negative_length_throws()
    {
        var r = PostningsMax.Compute(9.75);
        Assert.Throws<ArgumentOutOfRangeException>(
            () => PostningEconomy.Compute(r, -1, PriceList.Default));
    }
}
