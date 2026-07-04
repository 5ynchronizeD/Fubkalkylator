namespace Fubkalkylator.Core;

/// <summary>Sågmetod = i vilken ordning/åt vilket håll man vänder stocken.</summary>
public enum SawMethod
{
    /// <summary>Blockmetod: såga en sida klart, vänd 180° till motsatt sida, osv.</summary>
    Block180,
    /// <summary>Varvsågning: ett snitt, vänd 90°, ett snitt, vänd 90° … runt stocken.</summary>
    Varv90,
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

        var planes = new Dictionary<SawFace, List<(string Label, double Dist)>>
        {
            [SawFace.Top] = FacePlanes(end, hh, "Ändbräda"),
            [SawFace.Bottom] = FacePlanes(end, hh, "Ändbräda"),
            [SawFace.Left] = FacePlanes(side, bh, "Sidobräda"),
            [SawFace.Right] = FacePlanes(side, bh, "Sidobräda"),
        };

        // Sidordning: börja alltid uppåt (Top) så första snittet inte kräver rotation.
        var faceOrder = new[] { SawFace.Top, SawFace.Left, SawFace.Bottom, SawFace.Right };
        var order = method == SawMethod.Varv90
            ? RoundRobin(planes, faceOrder)      // vänd 90° varje snitt
            : Sequential(planes, faceOrder);     // en sida klart, sedan nästa

        var cuts = new List<SawCut>();
        int n = 1;
        var lastDist = new Dictionary<SawFace, double>();
        double rot = 0, prevAngle = FaceAngle(SawFace.Top);
        bool first = true;

        foreach (var (face, idx) in order)
        {
            var (label, dist) = planes[face][idx];
            double? step = lastDist.TryGetValue(face, out var l) ? Math.Abs(l - dist) : null;
            lastDist[face] = dist;

            double angle = FaceAngle(face);
            rot += first ? 0 : ShortestDelta(prevAngle, angle);
            prevAngle = angle;
            first = false;

            cuts.Add(new SawCut
            {
                Number = n++,
                Face = face,
                Label = label,
                DistanceFromCenterInches = dist,
                StepFromPreviousInches = step,
                RotationDegrees = rot,
            });
        }

        // Delning av blocket (block upprätt). Använd måldimensionens uppdelning om given.
        rot += ShortestDelta(prevAngle, FaceAngle(SawFace.Block));
        var block = blockPieces ?? PostningLayout.BlockPieces(r);
        double? prevSplit = null;
        for (int i = 0; i < block.Count - 1; i++)
        {
            double signed = -hh + block[i].End;
            cuts.Add(new SawCut
            {
                Number = n++,
                Face = SawFace.Block,
                Label = $"Delning {i + 1}",
                DistanceFromCenterInches = Math.Abs(signed),
                StepFromPreviousInches = prevSplit is double ps ? Math.Abs(signed - ps) : null,
                AboveCenter = signed < 0,
                RotationDegrees = rot,
            });
            prevSplit = signed;
        }

        return cuts;
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

    private static List<(SawFace, int)> Sequential(
        Dictionary<SawFace, List<(string, double)>> planes, SawFace[] faceOrder)
    {
        var result = new List<(SawFace, int)>();
        foreach (var f in faceOrder)
            for (int i = 0; i < planes[f].Count; i++)
                result.Add((f, i));
        return result;
    }

    private static List<(SawFace, int)> RoundRobin(
        Dictionary<SawFace, List<(string, double)>> planes, SawFace[] faceOrder)
    {
        var result = new List<(SawFace, int)>();
        var ptr = faceOrder.ToDictionary(f => f, _ => 0);
        bool any = true;
        while (any)
        {
            any = false;
            foreach (var f in faceOrder)
            {
                if (ptr[f] < planes[f].Count)
                {
                    result.Add((f, ptr[f]));
                    ptr[f]++;
                    any = true;
                }
            }
        }
        return result;
    }

    // Kortaste vinkeländring a→b, i intervallet (−180, 180].
    private static double ShortestDelta(double a, double b) => ((b - a + 540) % 360) - 180;
}
