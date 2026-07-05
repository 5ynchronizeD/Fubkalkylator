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
    public void Block_flips_180_between_the_first_two_faces()
    {
        var cuts = SawSequence.Compute(PostningsMax.Compute(9.75), SawMethod.Block180)
            .Where(c => c.Face != SawFace.Block).ToList();
        var first = cuts[0].Face;
        var second = cuts.First(c => c.Face != first);
        // Första två sidorna ska vara motstående (180° isär).
        bool opposite = (first, second.Face) is
            (SawFace.Left, SawFace.Right) or (SawFace.Right, SawFace.Left) or
            (SawFace.Top, SawFace.Bottom) or (SawFace.Bottom, SawFace.Top);
        Assert.True(opposite);
        double delta = Math.Abs(second.RotationDegrees - cuts[0].RotationDegrees) % 360;
        Assert.Equal(180, delta, 3);
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
    public void Slice_cuts_are_within_the_preblock_height()
    {
        var r = PostningsMax.Compute(9.75);
        double fhh = r.PreBlockHeight.Inches / 2.0;   // skivfasen kan svälja ändbräder ut till förblockskanten
        var split = SawSequence.Compute(r).Where(c => c.Face == SawFace.Block).ToList();
        Assert.NotEmpty(split);
        Assert.All(split, c => Assert.InRange(c.DistanceFromCenterInches, 0, fhh + 1e-9));
        Assert.All(split, c => Assert.NotNull(c.AboveCenter));
    }

    [Fact]
    public void Slice_phase_never_rotates_between_cuts()
    {
        // "Man kapar alla från samma sida blocket är framme" — delningen/skivningen
        // ska ske i EN orientering, utan vändning mellan snitten.
        foreach (var method in new[] { SawMethod.Block180, SawMethod.Varv90 })
        {
            var slice = SawSequence.Compute(PostningsMax.Compute(9.75), method)
                .Where(c => c.Face == SawFace.Block).ToList();
            Assert.All(slice, c => Assert.Equal(slice[0].RotationDegrees, c.RotationDegrees, 3));
        }
    }

    [Fact]
    public void Rotation_stays_bounded_never_spins_wildly()
    {
        // Man vänder stocken max ~4 gånger — rotationen ska aldrig dra iväg
        // flera hela varv (t.ex. round-robin som vände 90° per bräda).
        foreach (var method in new[] { SawMethod.Block180, SawMethod.Varv90 })
        {
            var cuts = SawSequence.Compute(PostningsMax.Compute(9.75), method);
            Assert.All(cuts, c => Assert.True(Math.Abs(c.RotationDegrees) <= 360 + 1e-6,
                $"{method}: {c.RotationDegrees}° överskrider 4 kvartsvarv"));
        }
    }

    [Fact]
    public void Each_face_is_sawn_in_one_run_not_interleaved()
    {
        // Sekventiellt: när en sida lämnats återkommer man inte till den.
        foreach (var method in new[] { SawMethod.Block180, SawMethod.Varv90 })
        {
            var faces = SawSequence.Compute(PostningsMax.Compute(9.75), method)
                .Where(c => c.Face != SawFace.Block).Select(c => c.Face).ToList();
            var seen = new HashSet<SawFace>();
            for (int i = 0; i < faces.Count; i++)
                if (i == 0 || faces[i] != faces[i - 1])
                    Assert.True(seen.Add(faces[i]), $"{method}: återkom till {faces[i]}");
        }
    }
}
