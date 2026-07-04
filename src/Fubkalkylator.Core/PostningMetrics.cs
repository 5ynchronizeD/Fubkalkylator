namespace Fubkalkylator.Core;

/// <summary>
/// Enkla utbytesmått för en postning, baserat på tvärsnittsareor (tum²).
/// Sido-/ändbrädornas längd approximeras med blockets höjd resp. bredd, så
/// utbytet är ungefärligt (för planering, inte fakturering).
/// </summary>
public static class PostningMetrics
{
    /// <summary>Stockens tvärsnittsarea (under bark), tum².</summary>
    public static double LogArea(PostningResult r)
    {
        double radius = r.DiameterUnderBark.Inches / 2.0;
        return Math.PI * radius * radius;
    }

    /// <summary>Ungefärlig area för sido- och ändbrädor (biprodukter), tum².</summary>
    public static double ByproductArea(PostningResult r)
        => (r.SideOneInchBoards + 2.0 * r.SideTwoInchBoards) * r.BlockHeight.Inches
         + (r.EndOneInchBoards + 2.0 * r.EndTwoInchBoards) * r.BlockWidth.Inches;

    /// <summary>
    /// Ungefärlig area för brädorna som sågas ur själva blocket, tum².
    /// (1"- och 2"-brädor som spänner över blockbredden.)
    /// </summary>
    public static double BlockBoardArea(PostningResult r)
        => (r.BlockOneInchBoards + 2.0 * r.BlockTwoInchBoards) * r.BlockWidth.Inches;

    /// <summary>Total ungefärlig virkesarea (block + biprodukter), tum².</summary>
    public static double TimberArea(PostningResult r)
        => BlockBoardArea(r) + ByproductArea(r);

    /// <summary>
    /// Utbyte som andel (0–1) av stockens area: blockbrädornas area (anges via
    /// <paramref name="blockBoardArea"/>) plus biprodukter, delat med stockarean.
    /// </summary>
    public static double YieldFraction(PostningResult r, double blockBoardArea)
    {
        double log = LogArea(r);
        if (log <= 0) return 0;
        return Math.Min(1.0, (blockBoardArea + ByproductArea(r)) / log);
    }

    /// <summary>Utbyte i procent, avrundat till heltal.</summary>
    public static int YieldPercent(PostningResult r, double blockBoardArea)
        => (int)Math.Round(YieldFraction(r, blockBoardArea) * 100.0);

    /// <summary>Utbyte i procent utifrån den uppskattade blockarean.</summary>
    public static int YieldPercent(PostningResult r)
        => YieldPercent(r, BlockBoardArea(r));
}
