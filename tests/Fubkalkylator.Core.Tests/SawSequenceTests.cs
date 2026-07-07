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
    public void Genomsagning_is_parallel_cuts_through_the_whole_log_without_rotation()
    {
        var r = PostningsMax.Compute(9.75);
        var cuts = SawSequence.Compute(r, SawMethod.Genomsagning);
        double woodR = r.DiameterUnderBark.Inches / 2.0;

        Assert.NotEmpty(cuts);
        Assert.All(cuts, c => Assert.Equal(0, c.RotationDegrees));   // ingen vändning
        Assert.All(cuts, c => Assert.Equal(SawFace.Block, c.Face));  // inget block/sidoutbyte — allt skivas
        Assert.All(cuts, c => Assert.InRange(c.DistanceFromCenterInches, 0, woodR + 1e-9));
        // Snitten ligger över hela diametern (minst ett en bit ut från centrum).
        Assert.Contains(cuts, c => c.DistanceFromCenterInches > woodR * 0.5);
    }

    [Fact]
    public void Center_cut_places_a_saw_cut_at_the_pith()
    {
        var r = PostningsMax.Compute(9.75);
        var centered = PostningLayout.CenteredBlockPieces(r.BlockHeight.Inches, 2.0, r.KerfInches);

        // En styckegräns ligger vid märgen (H/2 från blockets ovankant).
        double hc = r.BlockHeight.Inches / 2.0;
        Assert.Contains(centered, p => Math.Abs(p.End - hc) < 1e-6 || Math.Abs(p.Start - hc) < 1e-6);

        // Och ett delningssnitt hamnar då ~i centrum (0 från märgen).
        var cuts = SawSequence.Compute(r, SawMethod.Block180, centered);
        Assert.Contains(cuts.Where(c => c.Face == SawFace.Block), c => c.DistanceFromCenterInches < 0.05);
    }

    [Fact]
    public void Centered_block_shrinks_the_block_height_to_the_regel_band()
    {
        var r = PostningsMax.Compute(13.0);
        var adj = PostningLayout.CenteredBlock(r, 2.0);
        Assert.NotNull(adj);
        var (result, pieces) = adj!.Value;

        // Blocket har krympt till bandet (eller är lika stort om det redan gick jämnt ut).
        Assert.True(result.BlockHeight.Inches <= r.BlockHeight.Inches + 1e-9);
        // Bitarna är re-baserade 0..H' och fyller nya blockhöjden.
        Assert.Equal(0.0, pieces[0].Start, 6);
        Assert.Equal(result.BlockHeight.Inches, pieces[^1].End, 6);
        // Höjden som frigörs blir ändregion, inte spill: ändregionen krymper aldrig,
        // och sidoutbytet (blockbredden) är oförändrat.
        Assert.True(result.PreBlockHeight.Inches - result.BlockHeight.Inches
                    >= r.PreBlockHeight.Inches - r.BlockHeight.Inches - 1e-9);
        Assert.True(result.EndOneInchBoards + result.EndTwoInchBoards
                    >= r.EndOneInchBoards + r.EndTwoInchBoards);
        Assert.Equal(r.BlockWidth.Inches, result.BlockWidth.Inches, 6);
        Assert.Equal(r.SideTwoInchBoards, result.SideTwoInchBoards);
        // Ett snitt hamnar mitt i (nya) blocket = genom kärnan.
        double c = result.BlockHeight.Inches / 2.0;
        Assert.Contains(pieces, p => Math.Abs(p.End - c) < 1e-6 || Math.Abs(p.Start - c) < 1e-6);
    }

    [Fact]
    public void Centered_block_uses_only_whole_reglar_with_equal_margins()
    {
        var r = PostningsMax.Compute(9.75);
        double H = r.BlockHeight.Inches, t = 2.0;
        var centered = PostningLayout.CenteredBlockPieces(H, t, r.KerfInches);

        Assert.NotEmpty(centered);
        // Enbart hela reglar — inga tunna kantbitar.
        Assert.All(centered, p => Assert.Equal(t, p.Thickness, 6));
        // Bandet är centrerat: lika stor marginal upptill som nedtill.
        double topMargin = centered[0].Start;
        double bottomMargin = H - centered[^1].End;
        Assert.Equal(topMargin, bottomMargin, 6);
        // Marginalen ligger inuti blocket (kapas som bark), inte utanför.
        Assert.True(topMargin >= -1e-9);
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
    public void Whole_log_rests_on_bark()
    {
        var r = PostningsMax.Compute(9.75);
        Assert.True(SawSequence.IsBarkDown(r, 0, SawMethod.Block180));
    }

    [Fact]
    public void After_flipping_180_the_flat_side_rests_down_not_bark()
    {
        // Blocksågning: snitt 1 öppnar en sida (motstående runda sidan är nedåt = bark),
        // efter vändningen 180° ligger den nyss sågade platta sidan nedåt = ingen bark.
        var r = PostningsMax.Compute(9.75);
        var cuts = SawSequence.Compute(r, SawMethod.Block180);
        int firstSecondFace = cuts.TakeWhile(c => c.Face == cuts[0].Face).Count(); // steg där sida 2 börjar

        Assert.True(SawSequence.IsBarkDown(r, 1, SawMethod.Block180));                 // första sidan: bark nedåt
        Assert.False(SawSequence.IsBarkDown(r, firstSecondFace + 1, SawMethod.Block180)); // efter 180°: plan yta nedåt
    }

    [Fact]
    public void Clamp_drops_block_boards_that_cannot_be_sawn()
    {
        // 9.75" ger block 7,75" med 3×2" + 1×1" (understa = 1" = 25 mm).
        var full = PostningsMax.Compute(9.75);
        var clamped = PostningsMax.Compute(9.75, SawConstants.KerfInches, 1.6);   // ~40 mm klämma

        int fullBoards = full.BlockOneInchBoards + full.BlockTwoInchBoards;
        int clampedBoards = clamped.BlockOneInchBoards + clamped.BlockTwoInchBoards;
        Assert.True(clampedBoards < fullBoards);                       // minst en bräda blir spill
        Assert.Equal(full.BlockHeight.Inches, clamped.BlockHeight.Inches, 6);  // blockhöjden oförändrad
    }

    [Fact]
    public void Clamp_zero_leaves_the_postning_unchanged()
    {
        var a = PostningsMax.Compute(9.75);
        var b = PostningsMax.Compute(9.75, SawConstants.KerfInches, 0);
        Assert.Equal(a.BlockTwoInchBoards, b.BlockTwoInchBoards);
        Assert.Equal(a.BlockOneInchBoards, b.BlockOneInchBoards);
    }

    [Fact]
    public void Clamp_still_limits_the_bottom_board_in_genomsagning()
    {
        var r = PostningsMax.Compute(13.0);
        int none = SawSequence.Compute(r, SawMethod.Genomsagning, null, 0).Count(c => c.Face == SawFace.Block);
        int big = SawSequence.Compute(r, SawMethod.Genomsagning, null, 5.0).Count(c => c.Face == SawFace.Block);
        Assert.True(big <= none);
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
