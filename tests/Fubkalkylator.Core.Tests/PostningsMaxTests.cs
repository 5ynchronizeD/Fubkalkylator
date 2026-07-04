using Fubkalkylator.Core;
using Xunit;

namespace Fubkalkylator.Core.Tests;

public class PostningsMaxTests
{
    // Referensfallet från kalkylbladet: fub = 9.75" ger de dokumenterade cellvärdena.
    [Fact]
    public void ReferenceCase_9_75_inch_matches_spreadsheet()
    {
        var r = PostningsMax.Compute(9.75);

        Assert.Equal(6.0, r.BlockWidth.Inches);        // C9  = B
        Assert.Equal(7.75, r.BlockHeight.Inches);      // C10 = H
        Assert.Equal(1, r.BlockOneInchBoards);         // C11
        Assert.Equal(3, r.BlockTwoInchBoards);         // C12
        Assert.Equal(0, r.EndOneInchBoards);           // C13
        Assert.Equal(0, r.EndTwoInchBoards);           // C14
        Assert.Equal(2, r.SideOneInchBoards);          // C15
        Assert.Equal(0, r.SideTwoInchBoards);          // C16
        Assert.Equal(8.5, r.PreBlockWidth.Inches);     // C17 = FB
        Assert.Equal(7.75, r.PreBlockHeight.Inches);   // C18 = FH
    }

    [Fact]
    public void Millimeter_conversion_matches_spreadsheet()
    {
        var r = PostningsMax.Compute(9.75);

        Assert.Equal(247.65, r.DiameterUnderBark.Millimeters, 3);  // E3
        Assert.Equal(152.4, r.BlockWidth.Millimeters, 3);          // E9
        Assert.Equal(215.9, r.PreBlockWidth.Millimeters, 3);       // E17
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void NonPositive_diameter_throws(double fub)
        => Assert.Throws<ArgumentOutOfRangeException>(() => PostningsMax.Compute(fub));

    // Blockhöjden snäpps alltid nedåt till ett giltigt tabellvärde.
    [Fact]
    public void BlockHeight_snaps_down_to_valid_table_value()
    {
        var r = PostningsMax.Compute(12.0);
        Assert.Contains(SawTables.BlockDivisionTable(SawConstants.KerfInches), row => row.HeightInches == r.BlockHeight.Inches);
        Assert.True(r.BlockHeight.Inches <= 2.0 * (12.0 / 2.0) * System.Math.Sqrt(2) - r.BlockWidth.Inches);
    }
}

public class ApteringsMaxTests
{
    // Referensfallet från kalkylbladet: B = 7" ger fub = 9", pb = 9.45".
    [Fact]
    public void ReferenceCase_7_inch_matches_spreadsheet()
    {
        var r = ApteringsMax.Compute(7.0);

        Assert.Equal(9.0, r.TopDiameterUnderBark.Inches);          // C7
        Assert.Equal(9.45, r.TopDiameterOverBark.Inches, 5);       // C6
        Assert.Equal(22.86, r.TopDiameterUnderBark.Centimeters, 3);// E7
        Assert.Equal(24.003, r.TopDiameterOverBark.Centimeters, 3);// E6
    }

    [Fact]
    public void Aptering_is_not_an_exact_inverse_of_postning()
    {
        // Modellen är medvetet asymmetrisk: aptering använder floor(B·√2) medan
        // postning använder floor(fub/√2). Att mata apteringens fub tillbaka in i
        // postningen ger därför ett något mindre block — så fungerar kalkylbladet.
        var apt = ApteringsMax.Compute(6.0);
        var post = PostningsMax.Compute(apt.TopDiameterUnderBark.Inches);
        Assert.True(post.BlockWidth.Inches <= 6.0);
    }
}

public class SawTablesTests
{
    [Theory]
    [InlineData(0.5, 0)]
    [InlineData(1.375, 2)]
    [InlineData(1.0, 2)]
    [InlineData(3.75, 2)]
    public void OneInchBoards_matches_IFS(double thickness, int expected)
        => Assert.Equal(expected, SawTables.OneInchBoards(thickness));

    [Theory]
    [InlineData(0.5, 0)]
    [InlineData(2.0, 2)]
    [InlineData(4.5, 4)]
    [InlineData(6.75, 6)]
    public void TwoInchBoards_matches_IFS(double thickness, int expected)
        => Assert.Equal(expected, SawTables.TwoInchBoards(thickness));

    [Fact]
    public void SnapBlockHeight_picks_largest_not_exceeding()
    {
        Assert.Equal(7.75, SawTables.SnapBlockHeight(7.788, SawConstants.KerfInches).HeightInches);
        Assert.Equal(2.0, SawTables.SnapBlockHeight(2.0, SawConstants.KerfInches).HeightInches);
        Assert.Equal(2.0, SawTables.SnapBlockHeight(2.24, SawConstants.KerfInches).HeightInches);
    }
}
