namespace Fubkalkylator.Core;

/// <summary>
/// Resultatet av en postningsberäkning för en stock — motsvarar fliken "PostningsMax".
/// Alla mått i tum (nås även i mm/cm via <see cref="Measure"/>).
/// </summary>
public sealed record PostningResult
{
    /// <summary>Indata: diameter under bark i toppänden [fub].</summary>
    public required Measure DiameterUnderBark { get; init; }

    /// <summary>Blockbredd [B].</summary>
    public required Measure BlockWidth { get; init; }

    /// <summary>Blockhöjd [H] (snäppt till tabellvärde).</summary>
    public required Measure BlockHeight { get; init; }

    /// <summary>Antal 1"-brädor ur själva blocket.</summary>
    public required int BlockOneInchBoards { get; init; }

    /// <summary>Antal 2"-reglar/plank ur själva blocket.</summary>
    public required int BlockTwoInchBoards { get; init; }

    /// <summary>Antal 1"-ändbrädor.</summary>
    public required int EndOneInchBoards { get; init; }

    /// <summary>Antal 2"-ändreglar/plank.</summary>
    public required int EndTwoInchBoards { get; init; }

    /// <summary>Antal 1"-sidobrädor.</summary>
    public required int SideOneInchBoards { get; init; }

    /// <summary>Antal 2"-sidoreglar/plank.</summary>
    public required int SideTwoInchBoards { get; init; }

    /// <summary>Förblockbredd [FB] = block + sidobrädor (med sågspår).</summary>
    public required Measure PreBlockWidth { get; init; }

    /// <summary>Förblockhöjd [FH] = block + ändbrädor (med sågspår).</summary>
    public required Measure PreBlockHeight { get; init; }

    /// <summary>Sågspåret (tum) som använts i beräkningen.</summary>
    public required double KerfInches { get; init; }

    /// <summary>
    /// Antal ändar (topp/botten) som ger ändbräder. 2 = symmetriskt (standard).
    /// 1 = bara toppen ger utbyte; botten är barkbädd för stockklämman.
    /// </summary>
    public int EndSides { get; init; } = 2;
}

/// <summary>
/// Beräknar optimal postning (block + sido-/ändbrädor) från stockens diameter.
/// Ren översättning av formlerna i fliken "PostningsMax" (+ hjälpceller i "Data").
/// </summary>
public static class PostningsMax
{
    private static readonly double Sqrt2 = Math.Sqrt(2.0);

    /// <summary>
    /// Beräknar postningen för en stock med angiven diameter under bark (tum).
    /// </summary>
    /// <param name="diameterUnderBarkInches">Toppdiameter under bark [fub] i tum.</param>
    /// <param name="kerfInches">Sågspår i tum (standard 1/4"; t.ex. ~0,12" för bandsåg).</param>
    public static PostningResult Compute(double diameterUnderBarkInches,
        double kerfInches = SawConstants.KerfInches, double clampInches = 0)
    {
        if (diameterUnderBarkInches <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(diameterUnderBarkInches), diameterUnderBarkInches,
                "Diametern måste vara större än 0.");
        if (kerfInches < 0)
            throw new ArgumentOutOfRangeException(
                nameof(kerfInches), kerfInches, "Sågspåret kan inte vara negativt.");

        double fub = diameterUnderBarkInches;
        double blockWidth = BlockWidthFor(fub);
        BlockRow block = SawTables.SnapBlockHeight(RawHeightFor(fub), kerfInches);
        return Build(fub, blockWidth, block, kerfInches, clampInches);
    }

    /// <summary>
    /// Alla postningsalternativ för stocken: samma blockbredd men olika blockhöjd
    /// (raderna i uttagstabellen ≤ största som ryms), störst (= optimalt) först. Låter
    /// användaren välja en mindre blockning för en annan brädmix.
    /// </summary>
    public static IReadOnlyList<PostningResult> Alternatives(double diameterUnderBarkInches,
        double kerfInches = SawConstants.KerfInches, double clampInches = 0)
    {
        if (diameterUnderBarkInches <= 0)
            throw new ArgumentOutOfRangeException(nameof(diameterUnderBarkInches),
                diameterUnderBarkInches, "Diametern måste vara större än 0.");

        double fub = diameterUnderBarkInches;
        double blockWidth = BlockWidthFor(fub);
        double maxHeight = SawTables.SnapBlockHeight(RawHeightFor(fub), kerfInches).HeightInches;

        var list = new List<PostningResult>();
        foreach (var row in SawTables.BlockDivisionTable(kerfInches))
            if (row.HeightInches <= maxHeight + 1e-9)
                list.Add(Build(fub, blockWidth, row, kerfInches, clampInches));
        list.Reverse();   // störst blockhöjd (optimalt) först

        // Bara de största (optimalt + några mindre) — de minsta blocken är sällan intressanta.
        const int max = 6;
        if (list.Count > max) list = list.GetRange(0, max);
        return list;
    }

    // Blockbredd [B] = floor(fub/2·√2); rå blockhöjd = 2·(fub/2·√2) − B (Data!E3/E4).
    private static double BlockWidthFor(double fub) => Math.Floor((fub / 2.0) * Sqrt2);
    private static double RawHeightFor(double fub) => 2.0 * ((fub / 2.0) * Sqrt2) - BlockWidthFor(fub);

    // Bygger postningen för en given blockbredd + blockhöjd (uttagsrad).
    private static PostningResult Build(double fub, double blockWidth, BlockRow block,
        double kerfInches, double clampInches)
    {
        double blockHeight = block.HeightInches;

        // Stockklämma: den sista biten mot bädden — INKLUSIVE den runda barken under
        // blocket — får inte vara lägre än klämman. Barken under blocket räcker ofta en
        // bit; bara om den inte når klämman offras de understa blockbrädorna (minsta möjliga).
        double barkRadius = (fub / 2.0) * ApteringsMax.BarkFactor;
        double barkBelowBlock = barkRadius - blockHeight / 2.0;   // rund bark ned till bädden
        int blockTwo = block.TwoInchCount, blockOne = block.OneInchCount;
        if (clampInches > 0)
            (blockTwo, blockOne) = ClampBlockBoards(blockTwo, blockOne, kerfInches, clampInches, barkBelowBlock);

        // Tjocklek på tillgängligt virke för änd-/sidobrädor (Data!B3 / B4).
        double endThickness = (fub - blockHeight - 1.0) / 2.0;   // Data!B3
        double sideThickness = (fub - blockWidth - 1.0) / 2.0;   // Data!B4

        int endOne = SawTables.OneInchBoards(endThickness);      // C13 (totalt båda ändar)
        int endTwo = SawTables.TwoInchBoards(endThickness);      // C14
        int sideOne = SawTables.OneInchBoards(sideThickness);    // C15
        int sideTwo = SawTables.TwoInchBoards(sideThickness);    // C16

        // Bottensidan är barkbädd för klämman (blocksågning): den ger ändutbyte bara om
        // en hel ändbräda ryms ovanför klämm-bädden (barken under ändregionen ≥ klämman).
        // Annars ger bara toppen ände-utbyte (halva). Klämma = 0 ⇒ symmetriskt som förut.
        int endSides = 2;
        if (clampInches > 0 && (endOne + endTwo) > 0)
        {
            bool bottomFits = barkBelowBlock - endThickness >= clampInches - 1e-9;
            if (!bottomFits) endSides = 1;
        }
        int perEndOne = endOne / 2, perEndTwo = endTwo / 2;
        endOne = perEndOne * endSides;
        endTwo = perEndTwo * endSides;

        // Förblock: block plus brädorna som sitter kvar utanpå (med sågspår).
        double oneSlot = SawConstants.Slot(1.0, kerfInches);
        double twoSlot = SawConstants.Slot(2.0, kerfInches);
        double preBlockWidth =                                   // C17
            blockWidth + oneSlot * sideOne + twoSlot * sideTwo;
        double preBlockHeight =                                  // C18
            blockHeight + oneSlot * endOne + twoSlot * endTwo;

        return new PostningResult
        {
            DiameterUnderBark = fub,
            BlockWidth = blockWidth,
            BlockHeight = blockHeight,
            BlockOneInchBoards = blockOne,
            BlockTwoInchBoards = blockTwo,
            EndOneInchBoards = endOne,
            EndTwoInchBoards = endTwo,
            SideOneInchBoards = sideOne,
            SideTwoInchBoards = sideTwo,
            PreBlockWidth = preBlockWidth,
            PreBlockHeight = preBlockHeight,
            KerfInches = kerfInches,
            EndSides = endSides,
        };
    }

    // Stockklämma, optimerat: den sista biten mot bädden (inkl. barken under blocket)
    // måste nå klämman. Barken bidrar med <paramref name="barkBelowBlock"/>; bara det
    // som saknas upp till klämman behöver tas från blockbrädorna. Vi offrar minsta möjliga
    // virke — den minsta kombination av brädor vars botten fyller resten — och lägger den
    // underst. Returnerar kvarvarande 2"/1".
    private static (int Two, int One) ClampBlockBoards(int n2, int n1, double kerf, double clamp, double barkBelowBlock)
    {
        double needed = clamp - barkBelowBlock;
        if (needed <= 1e-9) return (n2, n1);                    // barken räcker som bädd

        double bestSpill = double.PositiveInfinity;
        int bestTwo = -1, bestOne = -1;
        for (int b = 0; b <= n2; b++)
            for (int a = 0; a <= n1; a++)
            {
                int cnt = a + b;
                if (cnt == 0) continue;
                // Sammanhängande, osågad botten av blocket: brädorna + spåren mellan dem.
                double span = a * 1.0 + b * 2.0 + (cnt - 1) * kerf;
                if (span + 1e-9 < needed) continue;             // + barken räcker inte till klämman
                if (span < bestSpill - 1e-9) { bestSpill = span; bestTwo = b; bestOne = a; }
            }
        if (bestTwo < 0) return (0, 0);                         // ens hela blocket + bark < klämman
        return (n2 - bestTwo, n1 - bestOne);
    }
}
