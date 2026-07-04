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
    public void Side_cuts_lie_between_block_face_and_preblock_edge()
    {
        var r = PostningsMax.Compute(9.75);
        double bh = r.BlockWidth.Inches / 2.0;
        double fbh = r.PreBlockWidth.Inches / 2.0;
        foreach (var cut in SawSequence.Compute(r).Where(c => c.Phase == CutPhase.SideFace1))
            Assert.InRange(cut.DistanceFromCenterInches, bh - 1e-9, fbh + 1e-9);
    }

    [Fact]
    public void Opposite_faces_have_matching_cut_counts()
    {
        var cuts = SawSequence.Compute(PostningsMax.Compute(9.75));
        Assert.Equal(cuts.Count(c => c.Phase == CutPhase.SideFace1), cuts.Count(c => c.Phase == CutPhase.SideFace2));
        Assert.Equal(cuts.Count(c => c.Phase == CutPhase.EndFace1), cuts.Count(c => c.Phase == CutPhase.EndFace2));
    }

    [Fact]
    public void Block_split_cuts_are_within_block_height()
    {
        var r = PostningsMax.Compute(9.75);
        double hh = r.BlockHeight.Inches / 2.0;
        var split = SawSequence.Compute(r).Where(c => c.Phase == CutPhase.BlockSplit).ToList();
        Assert.All(split, c => Assert.InRange(c.DistanceFromCenterInches, 0, hh + 1e-9));
        Assert.All(split, c => Assert.NotNull(c.AboveCenter));
    }

    [Fact]
    public void First_cut_of_each_phase_is_from_center_rest_are_relative()
    {
        var cuts = SawSequence.Compute(PostningsMax.Compute(9.75));
        foreach (var phase in cuts.GroupBy(c => c.Phase))
        {
            var ordered = phase.OrderBy(c => c.Number).ToList();
            Assert.Null(ordered[0].StepFromPreviousInches);           // första = från centrum
            Assert.All(ordered.Skip(1), c => Assert.NotNull(c.StepFromPreviousInches));
            Assert.All(ordered.Skip(1), c => Assert.True(c.StepFromPreviousInches!.Value > 0));
        }
    }

    [Fact]
    public void First_cut_is_the_outermost_bak()
    {
        var r = PostningsMax.Compute(9.75);
        var cuts = SawSequence.Compute(r);
        double fbh = r.PreBlockWidth.Inches / 2.0;
        Assert.Equal("Bak (kanta av)", cuts[0].Label);
        // Yttersta snittet ligger vid förblockets kant (± en brädtjocklek/spår).
        Assert.True(cuts[0].DistanceFromCenterInches <= fbh + 1e-9);
        Assert.True(cuts[0].DistanceFromCenterInches > r.BlockWidth.Inches / 2.0);
    }
}
