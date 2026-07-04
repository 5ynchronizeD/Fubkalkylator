namespace Fubkalkylator.Core;

/// <summary>Vilken fas ett snitt tillhör (försågning per sida, sedan delning).</summary>
public enum CutPhase
{
    SideFace1,
    SideFace2,
    EndFace1,
    EndFace2,
    BlockSplit,
}

/// <summary>
/// Ett enskilt snitt i sågordningen, med avstånd från kärnan/centrum.
/// Positivt <see cref="DistanceFromCenterInches"/> = utåt (försågning) resp.
/// avstånd från centrum längs höjden (delning; se <see cref="AboveCenter"/>).
/// </summary>
public sealed record SawCut
{
    public required int Number { get; init; }
    public required CutPhase Phase { get; init; }
    public required string Label { get; init; }

    /// <summary>Avstånd från kärnan/centrum till snittet (tum). Används för första snittet på en sida.</summary>
    public required double DistanceFromCenterInches { get; init; }

    /// <summary>
    /// Hur långt såghuvudet flyttas från föregående snitt (tum). Null för det
    /// första snittet på varje ny sida — då gäller <see cref="DistanceFromCenterInches"/>.
    /// </summary>
    public double? StepFromPreviousInches { get; init; }

    /// <summary>För delningssnitt: sitter snittet ovanför centrum? (null för försågning).</summary>
    public bool? AboveCenter { get; init; }
}

/// <summary>
/// Räknar fram sågordningen för blockmetoden (Logosol-flöde): försågning av de
/// fyra sidorna med vändning emellan, sedan delning av blocket. Varje snitt får
/// ett mått från kärnan/centrum så man vet var såghuvudet ska ställas.
///
/// Modellen är ett hjälpschema — det exakta bak-/vankantsavfallet förenklas.
/// </summary>
public static class SawSequence
{
    public static IReadOnlyList<SawCut> Compute(PostningResult r)
    {
        var cuts = new List<SawCut>();
        int n = 1;
        double bh = r.BlockWidth.Inches / 2.0;   // halva blockbredden
        double hh = r.BlockHeight.Inches / 2.0;  // halva blockhöjden
        var side = PostningLayout.SidePiecesPerSide(r);
        var end = PostningLayout.EndPiecesPerSide(r);

        AddFace(cuts, ref n, CutPhase.SideFace1, "sidobräda", bh, side);
        AddFace(cuts, ref n, CutPhase.SideFace2, "sidobräda", bh, side);
        AddFace(cuts, ref n, CutPhase.EndFace1, "ändbräda", hh, end);
        AddFace(cuts, ref n, CutPhase.EndFace2, "ändbräda", hh, end);
        AddBlockSplit(cuts, ref n, r);

        return cuts;
    }

    private static void AddFace(List<SawCut> cuts, ref int n, CutPhase phase, string boardName,
        double blockHalf, IReadOnlyList<Piece> pieces)
    {
        if (pieces.Count == 0)
        {
            cuts.Add(new SawCut { Number = n++, Phase = phase, Label = "Kanta till blocket", DistanceFromCenterInches = blockHalf });
            return;
        }

        // Snittplan utåt→inåt: bak först, sedan varje brädas insida.
        var planes = new List<(string Label, double Dist)>
        {
            ("Bak (kanta av)", blockHalf + pieces[^1].End),
        };
        for (int i = pieces.Count - 1; i >= 0; i--)
        {
            int nr = pieces.Count - i; // 1 = ytterst
            planes.Add(($"{char.ToUpper(boardName[0]) + boardName[1..]} {nr}", blockHalf + pieces[i].Start));
        }

        double? prev = null;
        foreach (var (label, dist) in planes)
        {
            cuts.Add(new SawCut
            {
                Number = n++,
                Phase = phase,
                Label = label,
                DistanceFromCenterInches = dist,
                StepFromPreviousInches = prev is double p ? Math.Abs(p - dist) : null,
            });
            prev = dist;
        }
    }

    private static void AddBlockSplit(List<SawCut> cuts, ref int n, PostningResult r)
    {
        double hh = r.BlockHeight.Inches / 2.0;
        var pieces = PostningLayout.BlockPieces(r);
        // Delningssnitt = de inre gränserna (block-ytorna finns redan från försågningen).
        double? prevSigned = null;
        for (int i = 0; i < pieces.Count - 1; i++)
        {
            double signed = -hh + pieces[i].End;          // avstånd från centrum (neg = ovan)
            cuts.Add(new SawCut
            {
                Number = n++,
                Phase = CutPhase.BlockSplit,
                Label = $"Delning {i + 1}",
                DistanceFromCenterInches = Math.Abs(signed),
                StepFromPreviousInches = prevSigned is double p ? Math.Abs(signed - p) : null,
                AboveCenter = signed < 0,
            });
            prevSigned = signed;
        }
    }
}
