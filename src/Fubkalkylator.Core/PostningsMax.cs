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

        // Blockbredd [B] = floor(fub/2 · √2)   (PostningsMax!C9)
        double halfDiagonal = (fub / 2.0) * Sqrt2;           // Data!E3
        double blockWidth = Math.Floor(halfDiagonal);

        // Rå blockhöjd (Data!E4) = 2·E3 − B, snäpps sedan till tabellhöjd (C10).
        double rawHeight = 2.0 * halfDiagonal - blockWidth;
        BlockRow block = SawTables.SnapBlockHeight(rawHeight, kerfInches);
        double blockHeight = block.HeightInches;

        // Stockklämma: den nedersta biten mot bädden kan inte sågas tunnare än klämman.
        // Brädorna staplas 2" (topp) → 1" (botten); de understa som skulle lämna mindre
        // än klämman kvar går inte att kapa och blir spill. Blockhöjden är oförändrad.
        int blockTwo = block.TwoInchCount, blockOne = block.OneInchCount;
        if (clampInches > 0)
            (blockTwo, blockOne) = ClampBlockBoards(blockTwo, blockOne, blockHeight, kerfInches, clampInches);

        // Tjocklek på tillgängligt virke för änd-/sidobrädor (Data!B3 / B4).
        double endThickness = (fub - blockHeight - 1.0) / 2.0;   // Data!B3
        double sideThickness = (fub - blockWidth - 1.0) / 2.0;   // Data!B4

        int endOne = SawTables.OneInchBoards(endThickness);      // C13
        int endTwo = SawTables.TwoInchBoards(endThickness);      // C14
        int sideOne = SawTables.OneInchBoards(sideThickness);    // C15
        int sideTwo = SawTables.TwoInchBoards(sideThickness);    // C16

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
        };
    }

    // Stockklämma, optimerat: det som ligger mot bädden (spill) måste vara minst
    // klämman och går inte att såga. Vi OFFRAR så lite virke som möjligt — den minsta
    // kombination av brädor (a st 1" + b st 2") vars sammanhängande botten når klämman —
    // och lägger den underst. Resten sågas som vanligt. Returnerar kvarvarande 2"/1".
    private static (int Two, int One) ClampBlockBoards(int n2, int n1, double height, double kerf, double clamp)
    {
        double bestSpill = double.PositiveInfinity;
        int bestTwo = -1, bestOne = -1;
        for (int b = 0; b <= n2; b++)
            for (int a = 0; a <= n1; a++)
            {
                int cnt = a + b;
                if (cnt == 0) continue;
                // Sammanhängande, osågad botten: brädorna + spåren mellan dem.
                double spill = a * 1.0 + b * 2.0 + (cnt - 1) * kerf;
                if (spill + 1e-9 < clamp) continue;             // når inte upp till klämman
                if (spill < bestSpill - 1e-9) { bestSpill = spill; bestTwo = b; bestOne = a; }
            }
        if (bestTwo < 0) return (0, 0);                         // hela blocket < klämman → allt spill
        return (n2 - bestTwo, n1 - bestOne);
    }
}
