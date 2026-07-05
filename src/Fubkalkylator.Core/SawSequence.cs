namespace Fubkalkylator.Core;

/// <summary>Sågmetod = i vilken ordning/åt vilket håll man vänder stocken.</summary>
public enum SawMethod
{
    /// <summary>Blockmetod: såga en sida klart, vänd 180° till motsatt sida, osv.</summary>
    Block180,
    /// <summary>Varvsågning: ett snitt, vänd 90°, ett snitt, vänd 90° … runt stocken.</summary>
    Varv90,
    /// <summary>Genomsågning: parallella snitt rakt igenom hela stocken, ingen vändning.</summary>
    Genomsagning,
}

/// <summary>Vilken sida av stocken som sågas (styr rotation och snittlinje).</summary>
public enum SawFace { Top, Bottom, Left, Right, Block }

/// <summary>Ett snitt i sågordningen, med mått och den rotation bilden ska ha.</summary>
public sealed record SawCut
{
    public required int Number { get; init; }
    public required SawFace Face { get; init; }
    public required string Label { get; init; }

    /// <summary>Avstånd från kärnan/centrum till snittet (tum). För första snittet på en sida.</summary>
    public required double DistanceFromCenterInches { get; init; }

    /// <summary>Hur långt såghuvudet flyttas från förra snittet på SAMMA sida (tum). Null = första på sidan.</summary>
    public double? StepFromPreviousInches { get; init; }

    /// <summary>För delningssnitt: ovanför centrum? (null för försågning).</summary>
    public bool? AboveCenter { get; init; }

    /// <summary>Kumulativ rotation (grader) bilden ska ha när snittet görs — sidan hamnar uppåt.</summary>
    public required double RotationDegrees { get; init; }
}

/// <summary>
/// Räknar fram sågordningen för en vald <see cref="SawMethod"/>. Varje snitt får
/// mått (från centrum / relativt förra snittet på samma sida) och en rotation så
/// att sidan som sågas alltid hamnar uppåt.
/// </summary>
public static class SawSequence
{
    public static IReadOnlyList<SawCut> Compute(PostningResult r, SawMethod method = SawMethod.Block180,
        IReadOnlyList<Piece>? blockPieces = null)
    {
        double bh = r.BlockWidth.Inches / 2.0, hh = r.BlockHeight.Inches / 2.0;
        var side = PostningLayout.SidePiecesPerSide(r);
        var end = PostningLayout.EndPiecesPerSide(r);
        var block = blockPieces ?? PostningLayout.BlockPieces(r);

        // Genomsågning: parallella snitt rakt igenom hela stocken, en orientering,
        // inget block/sidoutbyte. Brädtjocklek = blockets (mål)regeltjocklek.
        if (method == SawMethod.Genomsagning)
        {
            double woodR = r.DiameterUnderBark.Inches / 2.0;
            double t = block.Count > 0 ? block[0].Thickness : 2.0;
            var gcuts = new List<SawCut>();
            int gn = 1;
            double? gPrev = null;
            foreach (var (label, y) in GenomPlanes(woodR, t, r.KerfInches))
            {
                gcuts.Add(new SawCut
                {
                    Number = gn++,
                    Face = SawFace.Block,
                    Label = label,
                    DistanceFromCenterInches = Math.Abs(y),
                    StepFromPreviousInches = gPrev is double gp ? Math.Abs(y - gp) : null,
                    AboveCenter = y < 0,
                    RotationDegrees = 0,
                });
                gPrev = y;
            }
            return gcuts;
        }

        // Blocksågning: ta bredd-sidorna (Left→Right = 180°), vänd sedan 90° och
        //   skiva HELA blocket uppifrån och ned i en orientering — ändbräder, reglar
        //   och botten tas från samma sida utan att vända (man vänder max ~4 ggr).
        // Varvsågning: gå runt stocken (90° per sida) och ta alla fyra sidor, dela
        //   sedan blocket i reglar.
        var faces = method == SawMethod.Varv90
            ? new[] { SawFace.Top, SawFace.Left, SawFace.Bottom, SawFace.Right }
            : new[] { SawFace.Left, SawFace.Right };

        var cuts = new List<SawCut>();
        int n = 1;
        double prevAngle = FaceAngle(faces[0]);
        double rot = prevAngle;   // första snittets sida vänds uppåt

        foreach (var face in faces)
        {
            bool isSide = face is SawFace.Left or SawFace.Right;
            var facePlanes = FacePlanes(isSide ? side : end, isSide ? bh : hh, isSide ? "Sidobräda" : "Ändbräda");
            rot += ShortestDelta(prevAngle, FaceAngle(face));
            prevAngle = FaceAngle(face);

            double? prev = null;
            foreach (var (label, dist) in facePlanes)
            {
                cuts.Add(new SawCut
                {
                    Number = n++,
                    Face = face,
                    Label = label,
                    DistanceFromCenterInches = dist,
                    StepFromPreviousInches = prev is double p ? Math.Abs(p - dist) : null,
                    RotationDegrees = rot,
                });
                prev = dist;
            }
        }

        // Skivfas: block upprätt, EN orientering, uppifrån och ned (ingen vändning).
        rot += ShortestDelta(prevAngle, FaceAngle(SawFace.Block));
        var slice = method == SawMethod.Varv90
            ? ReglarPlanes(block, hh)              // ändbräder redan tagna som sidor
            : FullSlicePlanes(block, end, hh);     // svälj ändbräder i skivningen
        double? prevY = null;
        foreach (var (label, y) in slice)
        {
            cuts.Add(new SawCut
            {
                Number = n++,
                Face = SawFace.Block,
                Label = label,
                DistanceFromCenterInches = Math.Abs(y),
                StepFromPreviousInches = prevY is double py ? Math.Abs(y - py) : null,
                AboveCenter = y < 0,
                RotationDegrees = rot,
            });
            prevY = y;
        }

        return cuts;
    }

    // Bara de inre reglarna (block-ytorna finns redan) — för varvsågning.
    private static List<(string, double)> ReglarPlanes(IReadOnlyList<Piece> block, double hh)
    {
        var list = new List<(string, double)>();
        for (int i = 0; i < block.Count - 1; i++)
            list.Add(($"Delning {i + 1}", -hh + block[i].End));
        return list;
    }

    // Hela blocket skivas uppifrån och ned: bak, ev. ändbräder, alla reglar, ev. undre ändbräder.
    private static List<(string, double)> FullSlicePlanes(IReadOnlyList<Piece> block, IReadOnlyList<Piece> end, double hh)
    {
        var list = new List<(string, double)>();
        double topUpper = end.Count > 0 ? -(hh + end[^1].End) : -hh;
        list.Add(("Bak (kanta av)", topUpper));
        for (int i = end.Count - 1; i >= 0; i--)
            list.Add(("Ändbräda (topp)", -(hh + end[i].Start)));
        for (int i = 0; i < block.Count; i++)
            list.Add(($"Regel/bräda {i + 1}", -hh + block[i].End));
        for (int i = 0; i < end.Count; i++)
            list.Add(("Ändbräda (botten)", hh + end[i].End));
        return list;
    }

    /// <summary>Rotation (grader) som lägger en sida uppåt.</summary>
    public static double FaceAngle(SawFace face) => face switch
    {
        SawFace.Top => 0,
        SawFace.Bottom => 180,
        SawFace.Left => 90,
        SawFace.Right => -90,
        _ => 0,   // Block: upprätt
    };

    private static List<(string, double)> FacePlanes(IReadOnlyList<Piece> pieces, double blockHalf, string boardName)
    {
        var list = new List<(string, double)>();
        if (pieces.Count == 0)
        {
            list.Add(("Kanta till blocket", blockHalf));
            return list;
        }
        list.Add(("Bak (kanta av)", blockHalf + pieces[^1].End));
        for (int i = pieces.Count - 1; i >= 0; i--)
        {
            int nr = pieces.Count - i;
            list.Add(($"{char.ToUpper(boardName[0]) + boardName[1..]} {nr}", blockHalf + pieces[i].Start));
        }
        return list;
    }

    // Parallella snitt genom hela stocken, med en bräda centrerad på märgen.
    // Returnerar snittplan (signerad y) uppifrån och ned; första snittet kantar av toppen.
    private static List<(string, double)> GenomPlanes(double woodR, double t, double kerf)
    {
        if (t <= 0) t = 2.0;
        var boards = new List<(double Top, double Bot)> { (-t / 2, t / 2) };
        for (int j = 1; ; j++)
        {
            double top = t / 2 + kerf + (j - 1) * (t + kerf);
            if (top + t > woodR) break;       // bara brädor som ryms helt i stocken
            boards.Add((top, top + t));       // nedanför märgen
            boards.Add((-(top + t), -top));   // ovanför märgen
        }
        boards.Sort((a, b) => a.Top.CompareTo(b.Top));

        var list = new List<(string, double)> { ("Bak (kanta av)", boards[0].Top) };
        for (int i = 0; i < boards.Count; i++)
            list.Add(($"Bräda {i + 1}", boards[i].Bot));
        return list;
    }

    // Kortaste vinkeländring a→b, i intervallet (−180, 180].
    private static double ShortestDelta(double a, double b) => ((b - a + 540) % 360) - 180;
}
