namespace Fubkalkylator.Core;

/// <summary>
/// Resultatet av en apteringsberäkning — motsvarar fliken "ApteringsMax".
/// Givet önskad blockbredd: vilken stock (toppdiameter) behöver du?
/// </summary>
public sealed record ApteringResult
{
    /// <summary>Indata: önskad blockbredd [B].</summary>
    public required Measure BlockWidth { get; init; }

    /// <summary>Toppdiameter under bark [fub] som krävs.</summary>
    public required Measure TopDiameterUnderBark { get; init; }

    /// <summary>Toppdiameter på bark [pb] som krävs (fub × 1,05).</summary>
    public required Measure TopDiameterOverBark { get; init; }
}

/// <summary>
/// Beräknar vilken toppdiameter en stock behöver för en önskad blockbredd.
/// Invers av <see cref="PostningsMax"/>; formler från fliken "ApteringsMax".
/// </summary>
public static class ApteringsMax
{
    private static readonly double Sqrt2 = Math.Sqrt(2.0);

    /// <summary>Andel extra diameter för bark ovanpå fub (ApteringsMax!C6 = ×1,05).</summary>
    public const double BarkFactor = 1.05;

    /// <summary>
    /// Beräknar nödvändig toppdiameter för en önskad blockbredd (tum).
    /// </summary>
    /// <param name="blockWidthInches">Önskad blockbredd [B] i tum.</param>
    public static ApteringResult Compute(double blockWidthInches)
    {
        if (blockWidthInches <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(blockWidthInches), blockWidthInches,
                "Blockbredden måste vara större än 0.");

        // fub = floor(√(B² + B²)) = floor(B·√2)   (ApteringsMax!C7)
        double fub = Math.Floor(blockWidthInches * Sqrt2);
        double pb = fub * BarkFactor;                            // ApteringsMax!C6

        return new ApteringResult
        {
            BlockWidth = blockWidthInches,
            TopDiameterUnderBark = fub,
            TopDiameterOverBark = pb,
        };
    }
}
