using Fubkalkylator.Core;
using Xunit;

namespace Fubkalkylator.Core.Tests;

public class PostningLayoutTests
{
    private static readonly PostningResult Ref = PostningsMax.Compute(9.75);

    [Fact]
    public void BlockPieces_fill_the_block_height_with_kerfs()
    {
        var pieces = PostningLayout.BlockPieces(Ref);

        Assert.Equal(4, pieces.Count);                       // 3×2" + 1×1"
        Assert.Equal(3, System.Linq.Enumerable.Count(pieces, p => p.Kind == BoardKind.TwoInch));
        Assert.Equal(1, System.Linq.Enumerable.Count(pieces, p => p.Kind == BoardKind.OneInch));
        // Sista bitens ytterkant ska landa på blockhöjden.
        Assert.Equal(Ref.BlockHeight.Inches, pieces[^1].End, 6);
        Assert.Equal(0.0, pieces[0].Start, 6);
    }

    [Fact]
    public void SidePieces_per_side_stay_within_region_width()
    {
        var pieces = PostningLayout.SidePiecesPerSide(Ref);
        double region = PostningLayout.SideRegionWidth(Ref);

        Assert.Single(pieces);                               // 2 totalt → 1 per sida
        Assert.All(pieces, p => Assert.True(p.End <= region + 1e-9));
    }

    [Fact]
    public void Reference_case_has_no_end_boards()
        => Assert.Empty(PostningLayout.EndPiecesPerSide(Ref));

    [Fact]
    public void Side_board_butts_the_block_spill_goes_to_the_bark()
    {
        // Ett grövre fall som ger en sidobräda att titta på.
        var r = PostningsMax.Compute(13.0);
        var pieces = PostningLayout.SidePiecesPerSide(r);
        Assert.NotEmpty(pieces);

        // Innersta brädan ligger dikt an mot blocket (inget spill inåt).
        Assert.Equal(0.0, pieces[0].Start, 6);

        // Ev. rest ligger ytterst (mot barken): sista brädans ytterkant ≤ regionen.
        double region = PostningLayout.SideRegionWidth(r);
        Assert.True(pieces[^1].End <= region + 1e-9);
    }
}
