using Fubkalkylator.Core;
using Xunit;

namespace Fubkalkylator.Core.Tests;

public class KerfTests
{
    // Genererad tabell vid 1/4" ska matcha Excel-referensen exakt.
    [Fact]
    public void Default_kerf_reproduces_spreadsheet_reference()
    {
        var r = PostningsMax.Compute(9.75);
        Assert.Equal(6.0, r.BlockWidth.Inches);
        Assert.Equal(7.75, r.BlockHeight.Inches);
        Assert.Equal(3, r.BlockTwoInchBoards);
        Assert.Equal(1, r.BlockOneInchBoards);
        Assert.Equal(0.25, r.KerfInches);
    }

    // Tunnare sågspår (bandsåg ~3 mm) → mindre spill, fler bitar ryms.
    [Fact]
    public void Thinner_kerf_fits_more_pieces()
    {
        double band = 3.0 / SawConstants.MmPerInch;   // ~0,118"
        int chain = PostningLayout.CountByThickness(10.0, 2.0, SawConstants.KerfInches);
        int bandsaw = PostningLayout.CountByThickness(10.0, 2.0, band);
        Assert.True(bandsaw >= chain);
    }

    [Fact]
    public void Thinner_kerf_shrinks_preblock()
    {
        double band = 3.0 / SawConstants.MmPerInch;
        var chain = PostningsMax.Compute(9.75, SawConstants.KerfInches);
        var bandsaw = PostningsMax.Compute(9.75, band);
        // Med samma sido-/ändbrädor blir förblocket smalare med tunnare spår.
        if (chain.SideOneInchBoards == bandsaw.SideOneInchBoards
            && chain.SideTwoInchBoards == bandsaw.SideTwoInchBoards
            && chain.SideOneInchBoards + chain.SideTwoInchBoards > 0)
        {
            Assert.True(bandsaw.PreBlockWidth.Inches <= chain.PreBlockWidth.Inches);
        }
    }

    // Blockhöjdstabellen ska fortfarande vara giltig (heltalskombinationer, sorterad).
    [Fact]
    public void Generated_table_is_sorted_and_starts_at_two()
    {
        double band = 3.0 / SawConstants.MmPerInch;
        var table = SawTables.BlockDivisionTable(band);
        Assert.True(table[0].HeightInches >= 2.0 - 1e-9);
        for (int i = 1; i < table.Count; i++)
            Assert.True(table[i].HeightInches > table[i - 1].HeightInches);
    }
}
