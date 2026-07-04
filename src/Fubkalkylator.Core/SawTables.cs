using System.Collections.Concurrent;

namespace Fubkalkylator.Core;

/// <summary>
/// Uppslag/regler för blockets uppdelning. Blockhöjdstabellen genereras utifrån
/// sågspåret (motsvarar Data!G3:I45 för 1/4" spår, men fungerar för valfritt spår).
/// </summary>
public static class SawTables
{
    /// <summary>Minsta blockhöjd som räknas (tum) — som i Excel.</summary>
    public const double MinBlockHeight = 2.0;

    /// <summary>Övre tak för genererade blockhöjder (tum).</summary>
    private const double MaxGeneratedHeight = 26.0;

    private static readonly ConcurrentDictionary<double, IReadOnlyList<BlockRow>> Cache = new();

    /// <summary>
    /// Alla uppdelningar (antal 2" + 1") sorterade efter blockhöjd, för ett givet
    /// sågspår. Blockhöjd = 2·n2 + n1 + (n2+n1−1)·spår. Motsvarar Data!G3:I45.
    /// </summary>
    public static IReadOnlyList<BlockRow> BlockDivisionTable(double kerfInches)
        => Cache.GetOrAdd(Math.Round(kerfInches, 6), Generate);

    private static IReadOnlyList<BlockRow> Generate(double kerf)
    {
        var rows = new List<BlockRow>();
        for (int pieces = 1; pieces <= 60; pieces++)
        {
            for (int n2 = 0; n2 <= pieces; n2++)
            {
                int n1 = pieces - n2;
                double height = 2.0 * n2 + n1 + (pieces - 1) * kerf;
                if (height >= MinBlockHeight - 1e-9 && height <= MaxGeneratedHeight)
                    rows.Add(new BlockRow(height, n2, n1));
            }
        }
        // Sortera på höjd; vid lika höjd, föredra fler 2"-bitar.
        rows.Sort((a, b) =>
        {
            int c = a.HeightInches.CompareTo(b.HeightInches);
            return c != 0 ? c : b.TwoInchCount.CompareTo(a.TwoInchCount);
        });
        // Ta bort dubbletter med praktiskt taget samma höjd.
        var result = new List<BlockRow>(rows.Count);
        foreach (var r in rows)
            if (result.Count == 0 || r.HeightInches - result[^1].HeightInches > 1e-6)
                result.Add(r);
        return result;
    }

    /// <summary>
    /// Snäpper en rå blockhöjd nedåt till närmaste giltiga tabellhöjd för sågspåret.
    /// Motsvarar IFS-formeln i PostningsMax!C10 (blockhöjd [H]).
    /// </summary>
    public static BlockRow SnapBlockHeight(double rawHeightInches, double kerfInches)
    {
        BlockRow? best = null;
        foreach (var row in BlockDivisionTable(kerfInches))
        {
            if (rawHeightInches + 1e-9 >= row.HeightInches)
                best = row;
            else
                break; // tabellen är sorterad stigande
        }
        return best
            ?? throw new ArgumentOutOfRangeException(
                nameof(rawHeightInches), rawHeightInches,
                $"Blockhöjden är mindre än minsta tabellvärde ({MinBlockHeight}\").");
    }

    /// <summary>
    /// Antal 1"-brädor som ryms i en sido-/ändbräda av given tjocklek.
    /// Motsvarar IFS-formlerna i PostningsMax!C13/C15 (oberoende av sågspår).
    /// </summary>
    public static int OneInchBoards(double thicknessInches) => thicknessInches switch
    {
        < 1.0 => 0,
        < 2.0 => 2,
        < 3.75 => 0,
        < 4.75 => 2,
        _ => 0,
    };

    /// <summary>
    /// Antal 2"-reglar/plank som ryms i en sido-/ändbräda av given tjocklek.
    /// Motsvarar IFS-formlerna i PostningsMax!C14/C16 (oberoende av sågspår).
    /// </summary>
    public static int TwoInchBoards(double thicknessInches) => thicknessInches switch
    {
        < 2.0 => 0,
        < 4.5 => 2,
        < 6.75 => 4,
        < 11.0 => 6,
        _ => 0,
    };
}

/// <summary>En rad i blockuppdelningstabellen.</summary>
/// <param name="HeightInches">Blockhöjd i tum (inkl. sågspår).</param>
/// <param name="TwoInchCount">Antal 2"-reglar/plank.</param>
/// <param name="OneInchCount">Antal 1"-brädor.</param>
public readonly record struct BlockRow(double HeightInches, int TwoInchCount, int OneInchCount);
