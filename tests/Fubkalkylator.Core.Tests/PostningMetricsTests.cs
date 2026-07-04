using Fubkalkylator.Core;
using Xunit;

namespace Fubkalkylator.Core.Tests;

public class PostningMetricsTests
{
    [Fact]
    public void Yield_is_between_zero_and_one()
    {
        var r = TargetDimension.Compute(6.0, 2.0)!;
        double blockArea = r.TargetPieceCount * r.TargetThickness.Inches * r.ActualBlockWidth.Inches;
        double y = PostningMetrics.YieldFraction(r.Postning, blockArea);
        Assert.InRange(y, 0.0, 1.0);
        Assert.True(y > 0.3); // ett block fyller en rejäl del av stocken
    }

    [Fact]
    public void More_target_area_gives_higher_yield()
    {
        var r = PostningsMax.Compute(9.75);
        double small = PostningMetrics.YieldFraction(r, 2.0 * r.BlockWidth.Inches);   // 1 bit
        double big = PostningMetrics.YieldFraction(r, 6.0 * r.BlockWidth.Inches);     // 3 bitar
        Assert.True(big > small);
    }
}
