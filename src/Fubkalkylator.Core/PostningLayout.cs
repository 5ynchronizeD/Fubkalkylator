namespace Fubkalkylator.Core;

/// <summary>Typ av utsågad bit (styr färgläggning i ritningen).</summary>
public enum BoardKind
{
    /// <summary>1"-bräda.</summary>
    OneInch,
    /// <summary>2"-regel/plank.</summary>
    TwoInch,
    /// <summary>Bit med valfri (mål-)tjocklek.</summary>
    Target,
}

/// <summary>
/// En utsågad bit placerad längs en axel, uttryckt som start/slut i tum
/// från regionens ena kant.
/// </summary>
public readonly record struct Piece(double Start, double End, BoardKind Kind)
{
    public double Thickness => End - Start;
}

/// <summary>
/// Räknar ut den geometriska placeringen av alla bitar i en postning, så att
/// en stockände kan ritas upp. Rena mått i tum; ingen UI-koppling.
/// </summary>
public static class PostningLayout
{
    private static double Size(BoardKind k) => k == BoardKind.TwoInch ? 2.0 : 1.0;

    /// <summary>Bredd (tum) på sidobrädsregionen per sida (block → cirkel).</summary>
    public static double SideRegionWidth(PostningResult r) => (r.PreBlockWidth.Inches - r.BlockWidth.Inches) / 2.0;

    /// <summary>Höjd (tum) på ändbrädsregionen per ände (block → cirkel).</summary>
    public static double EndRegionHeight(PostningResult r) => (r.PreBlockHeight.Inches - r.BlockHeight.Inches) / 2.0;

    /// <summary>
    /// Blockets bitar staplade längs höjden [H]. Reglar (2") först, sedan brädor (1"),
    /// med sågspår mellan varje bit (inget spår ytterst).
    /// </summary>
    public static IReadOnlyList<Piece> BlockPieces(PostningResult r)
    {
        var kinds = Repeat(BoardKind.TwoInch, r.BlockTwoInchBoards)
            .Concat(Repeat(BoardKind.OneInch, r.BlockOneInchBoards))
            .ToList();

        var pieces = new List<Piece>(kinds.Count);
        double pos = 0;
        for (int i = 0; i < kinds.Count; i++)
        {
            double t = Size(kinds[i]);
            pieces.Add(new Piece(pos, pos + t, kinds[i]));
            pos += t;
            if (i < kinds.Count - 1) pos += r.KerfInches; // spår mellan bitar
        }
        return pieces;
    }

    /// <summary>
    /// Sidobrädorna på EN sida (halva totala antalet), placerade utåt från blockkanten.
    /// Ett sågspår ligger närmast blocket och mellan bitarna.
    /// </summary>
    public static IReadOnlyList<Piece> SidePiecesPerSide(PostningResult r)
        => OuterRegionPieces(r.SideTwoInchBoards / 2, r.SideOneInchBoards / 2, r.KerfInches);

    /// <summary>Ändbrädorna på EN ände (halva totala antalet), placerade utåt från blockkanten.</summary>
    public static IReadOnlyList<Piece> EndPiecesPerSide(PostningResult r)
        => OuterRegionPieces(r.EndTwoInchBoards / 2, r.EndOneInchBoards / 2, r.KerfInches);

    private static IReadOnlyList<Piece> OuterRegionPieces(int twoInch, int oneInch, double kerf)
    {
        var kinds = Repeat(BoardKind.TwoInch, twoInch)
            .Concat(Repeat(BoardKind.OneInch, oneInch))
            .ToList();

        var pieces = new List<Piece>(kinds.Count);
        double pos = kerf; // sågspår närmast blocket
        foreach (var kind in kinds)
        {
            double t = Size(kind);
            pieces.Add(new Piece(pos, pos + t, kind));
            pos += t + kerf; // spår efter varje bit
        }
        return pieces;
    }

    private static IEnumerable<BoardKind> Repeat(BoardKind kind, int count)
        => Enumerable.Repeat(kind, Math.Max(0, count));

    /// <summary>
    /// Antal bitar av valfri tjocklek som ryms längs en given blockhöjd,
    /// med ett sågspår mellan varje bit.
    /// n · T + (n−1) · spår ≤ H  ⇒  n = ⌊(H + spår) / (T + spår)⌋.
    /// </summary>
    public static int CountByThickness(double blockHeightInches, double thicknessInches,
        double kerfInches = SawConstants.KerfInches)
    {
        if (thicknessInches <= 0) return 0;
        double n = Math.Floor((blockHeightInches + kerfInches)
                              / (thicknessInches + kerfInches) + 1e-9);
        return (int)Math.Max(0, n);
    }

    /// <summary>
    /// Delar blockhöjden i likadana bitar av <paramref name="thicknessInches"/>,
    /// staplade från toppen med sågspår mellan. Eventuell rest längst ned lämnas
    /// (för klen för en hel bit).
    /// </summary>
    public static IReadOnlyList<Piece> BlockPiecesOfThickness(double blockHeightInches, double thicknessInches,
        double kerfInches = SawConstants.KerfInches)
    {
        int n = CountByThickness(blockHeightInches, thicknessInches, kerfInches);
        var kind = KindForThickness(thicknessInches);
        var pieces = new List<Piece>(n);
        double pos = 0;
        for (int i = 0; i < n; i++)
        {
            pieces.Add(new Piece(pos, pos + thicknessInches, kind));
            pos += thicknessInches;
            if (i < n - 1) pos += kerfInches;
        }
        return pieces;
    }

    private static BoardKind KindForThickness(double t)
    {
        if (Math.Abs(t - 1.0) < 0.01) return BoardKind.OneInch;
        if (Math.Abs(t - 2.0) < 0.01) return BoardKind.TwoInch;
        return BoardKind.Target;
    }
}
