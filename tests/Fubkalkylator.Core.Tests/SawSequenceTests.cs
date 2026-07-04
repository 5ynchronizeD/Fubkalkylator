using Fubkalkylator.Core;
using Xunit;

namespace Fubkalkylator.Core.Tests;

public class SawSequenceTests
{
    [Fact]
    public void Produces_cuts_numbered_sequentially()
    {
        var cuts = SawSequence.Compute(PostningsMax.Compute(9.75));
        Assert.NotEmpty(cuts);
        for (int i = 0; i < cuts.Count; i++)
            Assert.Equal(i + 1, cuts[i].Number);
    }

    [Fact]
    public void First_cut_starts_at_top_with_no_rotation()
    {
        var cuts = SawSequence.Compute(PostningsMax.Compute(9.75));
        Assert.Equal(SawFace.Top, cuts[0].Face);
        Assert.Equal(0, cuts[0].RotationDegrees);
        Assert.Null(cuts[0].StepFromPreviousInches);   // första på sidan = från centrum
    }

    [Fact]
    public void Side_cuts_lie_between_block_face_and_preblock_edge()
    {
        var r = PostningsMax.Compute(9.75);
        double bh = r.BlockWidth.Inches / 2.0;
        double fbh = r.PreBlockWidth.Inches / 2.0;
        foreach (var cut in SawSequence.Compute(r).Where(c => c.Face is SawFace.Left or SawFace.Right))
            Assert.InRange(cut.DistanceFromCenterInches, bh - 1e-9, fbh + 1e-9);
    }

    [Fact]
    public void Opposite_faces_have_matching_cut_counts()
    {
        var cuts = SawSequence.Compute(PostningsMax.Compute(9.75));
        Assert.Equal(cuts.Count(c => c.Face == SawFace.Left), cuts.Count(c => c.Face == SawFace.Right));
        Assert.Equal(cuts.Count(c => c.Face == SawFace.Top), cuts.Count(c => c.Face == SawFace.Bottom));
    }

    [Fact]
    public void First_cut_on_each_face_is_from_center_rest_are_relative()
    {
        var cuts = SawSequence.Compute(PostningsMax.Compute(9.75));
        foreach (var face in cuts.GroupBy(c => c.Face))
        {
            var ordered = face.OrderBy(c => c.Number).ToList();
            Assert.Null(ordered[0].StepFromPreviousInches);
            Assert.All(ordered.Skip(1), c => Assert.True(c.StepFromPreviousInches is > 0));
        }
    }

    [Fact]
    public void Block_split_cuts_are_within_block_height()
    {
        var r = PostningsMax.Compute(9.75);
        double hh = r.BlockHeight.Inches / 2.0;
        var split = SawSequence.Compute(r).Where(c => c.Face == SawFace.Block).ToList();
        Assert.All(split, c => Assert.InRange(c.DistanceFromCenterInches, 0, hh + 1e-9));
        Assert.All(split, c => Assert.NotNull(c.AboveCenter));
    }

    [Fact]
    public void Both_methods_produce_the_same_set_of_cuts_in_different_order()
    {
        var r = PostningsMax.Compute(9.75);
        var block = SawSequence.Compute(r, SawMethod.Block180);
        var varv = SawSequence.Compute(r, SawMethod.Varv90);

        Assert.Equal(block.Count, varv.Count);
        // Samma mängd distanser, olika ordning.
        Assert.Equal(
            block.Select(c => Math.Round(c.DistanceFromCenterInches, 4)).OrderBy(x => x),
            varv.Select(c => Math.Round(c.DistanceFromCenterInches, 4)).OrderBy(x => x));
    }

    [Fact]
    public void Varv90_turns_between_consecutive_faces()
    {
        var varv = SawSequence.Compute(PostningsMax.Compute(9.75), SawMethod.Varv90)
            .Where(c => c.Face != SawFace.Block).ToList();
        // Varvsågning byter sida mellan minst några på varandra följande snitt.
        bool switchesFaces = false;
        for (int i = 1; i < varv.Count; i++)
            if (varv[i].Face != varv[i - 1].Face) { switchesFaces = true; break; }
        Assert.True(switchesFaces);
    }
}
