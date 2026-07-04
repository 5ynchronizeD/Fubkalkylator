using Fubkalkylator.Core;
using Xunit;

namespace Fubkalkylator.Core.Tests;

public class TargetDimensionTests
{
    [Fact]
    public void Target_6x2_finds_smallest_stock_that_fits()
    {
        var r = TargetDimension.Compute(6.0, 2.0);

        Assert.NotNull(r);
        Assert.Equal(6.0, r!.Postning.BlockWidth.Inches);       // rätt blockbredd
        Assert.Equal(8.5, r.Postning.DiameterUnderBark.Inches); // minsta fub där B≥6
        Assert.True(r.TargetPieceCount >= 1);
        Assert.Equal(r.TargetPieceCount, r.BlockPieces.Count);  // ritbitar = antal
        Assert.All(r.BlockPieces, p => Assert.Equal(2.0, p.Thickness, 6));
    }

    [Fact]
    public void Custom_thickness_1_5_is_supported()
    {
        var r = TargetDimension.Compute(6.0, 1.5);

        Assert.NotNull(r);
        Assert.All(r!.BlockPieces, p => Assert.Equal(1.5, p.Thickness, 6));
        // 1,5"-bitar ryms fler än 2"-bitar i samma block.
        var two = TargetDimension.Compute(6.0, 2.0)!;
        Assert.True(r.TargetPieceCount >= two.TargetPieceCount);
    }

    [Fact]
    public void Custom_width_1_5_trims_from_wider_block()
    {
        var r = TargetDimension.Compute(1.5, 1.5);

        Assert.NotNull(r);
        Assert.True(r!.ActualBlockWidth.Inches >= 1.5);        // blocket minst så brett
        Assert.True(r.TrimWidthInches >= 0);                   // kapas ned till 1,5"
    }

    [Fact]
    public void PieceCount_matches_kerf_formula()
    {
        var r = TargetDimension.Compute(8.0, 1.5)!;
        double h = r.Postning.BlockHeight.Inches;
        int expected = (int)System.Math.Floor((h + SawConstants.KerfInches) / (1.5 + SawConstants.KerfInches) + 1e-9);
        Assert.Equal(expected, r.TargetPieceCount);
    }

    [Fact]
    public void ByWidth_lists_increasing_widths_each_with_pieces()
    {
        var rows = TargetDimension.ByWidth(2.0);

        Assert.NotEmpty(rows);
        // Bredderna ökar strikt och varje rad ger minst en bit.
        for (int i = 0; i < rows.Count; i++)
        {
            Assert.True(rows[i].TargetPieceCount >= 1);
            if (i > 0)
                Assert.True(rows[i].ActualBlockWidth.Inches > rows[i - 1].ActualBlockWidth.Inches);
        }
        // Bredare block kräver aldrig mindre stock.
        for (int i = 1; i < rows.Count; i++)
            Assert.True(rows[i].Postning.DiameterUnderBark.Inches
                        >= rows[i - 1].Postning.DiameterUnderBark.Inches);
    }

    [Fact]
    public void SmallestStockForWidth_gives_block_at_least_that_wide()
    {
        var p = TargetDimension.SmallestStockForWidth(6.0);
        Assert.NotNull(p);
        Assert.True(p!.BlockWidth.Inches >= 6.0);
        // Minsta: en kvarts tum mindre diameter ska ge smalare block.
        var smaller = PostningsMax.Compute(p.DiameterUnderBark.Inches - 0.25);
        Assert.True(smaller.BlockWidth.Inches < 6.0);
    }

    [Fact]
    public void FitInStock_reports_pieces_for_a_given_log()
    {
        var f = TargetDimension.FitInStock(9.75, 6.0, 2.0);
        Assert.NotNull(f);
        Assert.True(f!.WidthFits);                        // 6" ryms i blocket (B=6)
        Assert.Equal(6.0, f.MaxWidth.Inches);
        Assert.True(f.PieceCount >= 1);
        Assert.Equal(f.PieceCount, f.BlockPieces.Count);
    }

    [Fact]
    public void FitInStock_flags_too_wide_target()
    {
        var f = TargetDimension.FitInStock(9.75, 9.0, 2.0); // block bara 6" brett
        Assert.NotNull(f);
        Assert.False(f!.WidthFits);
        Assert.Equal(0, f.PieceCount);
        Assert.Equal(6.0, f.MaxWidth.Inches);              // visar största möjliga bredd
    }

    [Fact]
    public void FitInStock_returns_null_for_too_small_log()
        => Assert.Null(TargetDimension.FitInStock(2.0, 6.0, 2.0));

    [Fact]
    public void Unachievable_width_returns_null()
        => Assert.Null(TargetDimension.Compute(40.0, 2.0));

    [Theory]
    [InlineData(0, 2)]
    [InlineData(6, 0)]
    [InlineData(-1, 1.5)]
    public void NonPositive_input_throws(double w, double t)
        => Assert.Throws<ArgumentOutOfRangeException>(() => TargetDimension.Compute(w, t));
}
