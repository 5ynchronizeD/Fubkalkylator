namespace Fubkalkylator.Core;

/// <summary>
/// Prislista i kronor per kubikmeter. Block/plank betingar oftast ett högre
/// pris än biprodukterna (sido-/ändbrädor).
/// </summary>
public sealed record PriceList
{
    /// <summary>Pris för virket ur blocket, kr/m³.</summary>
    public double BlockPerCubicMeter { get; init; }

    /// <summary>Pris för biprodukter (sido-/ändbrädor), kr/m³.</summary>
    public double ByproductPerCubicMeter { get; init; }

    /// <summary>Rimliga standardvärden (redigeras av användaren).</summary>
    public static PriceList Default => new()
    {
        BlockPerCubicMeter = 1500,
        ByproductPerCubicMeter = 500,
    };
}

/// <summary>Volym och värde för en postning, given stockens längd.</summary>
public sealed record PostningEconomyResult
{
    /// <summary>Stockens/brädornas längd som räkningen utgått från (tum).</summary>
    public required double LengthInches { get; init; }

    public required double BlockVolumeM3 { get; init; }
    public required double ByproductVolumeM3 { get; init; }

    /// <summary>Total virkesvolym (block + biprodukter), m³.</summary>
    public double TimberVolumeM3 => BlockVolumeM3 + ByproductVolumeM3;

    /// <summary>Stockens volym (cylinder under bark), m³.</summary>
    public required double LogVolumeM3 { get; init; }

    /// <summary>Total virkesvolym i board feet.</summary>
    public required double BoardFeet { get; init; }

    /// <summary>Volymutbyte i procent (virke / stock).</summary>
    public required int YieldPercent { get; init; }

    public required double BlockValue { get; init; }
    public required double ByproductValue { get; init; }

    /// <summary>Totalt uppskattat värde, kr.</summary>
    public double TotalValue => BlockValue + ByproductValue;
}

/// <summary>
/// Räknar volym och värde för en postning. Areor approximeras (se
/// <see cref="PostningMetrics"/>) så resultatet är för planering, inte fakturering.
/// </summary>
public static class PostningEconomy
{
    /// <summary>
    /// Beräknar volym och värde för postningen <paramref name="r"/> given
    /// längden <paramref name="lengthInches"/> (tum) och prislistan <paramref name="prices"/>.
    /// </summary>
    public static PostningEconomyResult Compute(PostningResult r, double lengthInches, PriceList prices)
    {
        ArgumentNullException.ThrowIfNull(prices);
        if (lengthInches < 0)
            throw new ArgumentOutOfRangeException(
                nameof(lengthInches), lengthInches, "Längden kan inte vara negativ.");

        double blockArea = PostningMetrics.BlockBoardArea(r);   // tum²
        double byArea = PostningMetrics.ByproductArea(r);       // tum²
        double logArea = PostningMetrics.LogArea(r);            // tum²

        static double ToCubicMeters(double areaSquareInches, double lengthInches)
            => areaSquareInches * lengthInches * SawConstants.CubicMetersPerCubicInch;

        double blockM3 = ToCubicMeters(blockArea, lengthInches);
        double byM3 = ToCubicMeters(byArea, lengthInches);
        double logM3 = ToCubicMeters(logArea, lengthInches);
        double boardFeet = (blockArea + byArea) * lengthInches / SawConstants.CubicInchesPerBoardFoot;

        return new PostningEconomyResult
        {
            LengthInches = lengthInches,
            BlockVolumeM3 = blockM3,
            ByproductVolumeM3 = byM3,
            LogVolumeM3 = logM3,
            BoardFeet = boardFeet,
            YieldPercent = PostningMetrics.YieldPercent(r, blockArea),
            BlockValue = blockM3 * prices.BlockPerCubicMeter,
            ByproductValue = byM3 * prices.ByproductPerCubicMeter,
        };
    }
}
