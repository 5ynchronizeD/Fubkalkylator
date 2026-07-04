namespace Fubkalkylator.Core;

/// <summary>Trädslag med kända krympegenskaper.</summary>
public enum WoodSpecies
{
    Okant = 0,
    Gran,
    Tall,
    Bjork,
    Ek,
}

/// <summary>
/// Krympmån vid torkning. Virke krymper när fukthalten sjunker under
/// fibermättnadspunkten (~30 %). För att få ett visst <em>torrt</em> mått måste
/// man därför såga med ett <em>rått</em> övermått.
/// Modellen är linjär och ungefärlig — avsedd för planering.
/// </summary>
public static class Shrinkage
{
    /// <summary>Fibermättnadspunkt (%). Över denna sker ingen krympning.</summary>
    public const double FiberSaturationPercent = 30.0;

    /// <summary>
    /// Ungefärlig linjär krympning för en brädas mått från rått (fibermättat)
    /// till helt torrt (0 %), i procent. Blandat radiellt/tangentiellt.
    /// </summary>
    public static double GreenToOvenDryPercent(WoodSpecies species) => species switch
    {
        WoodSpecies.Gran => 6.0,
        WoodSpecies.Tall => 6.5,
        WoodSpecies.Bjork => 7.5,
        WoodSpecies.Ek => 8.0,
        _ => 6.5,
    };

    /// <summary>
    /// Krympandel (0–1) från rått till målfukthalten <paramref name="targetMoisturePercent"/>.
    /// </summary>
    public static double Fraction(WoodSpecies species, double targetMoisturePercent)
    {
        double below = Math.Clamp(FiberSaturationPercent - targetMoisturePercent, 0, FiberSaturationPercent);
        return GreenToOvenDryPercent(species) / 100.0 * (below / FiberSaturationPercent);
    }

    /// <summary>
    /// Rått sågmått (tum) som krävs för att nå <paramref name="dryDimensionInches"/>
    /// efter torkning till <paramref name="targetMoisturePercent"/>.
    /// </summary>
    public static double GreenDimensionInches(double dryDimensionInches, WoodSpecies species, double targetMoisturePercent)
    {
        if (dryDimensionInches < 0)
            throw new ArgumentOutOfRangeException(nameof(dryDimensionInches), dryDimensionInches, "Måttet kan inte vara negativt.");
        double f = Fraction(species, targetMoisturePercent);
        return dryDimensionInches / (1.0 - f);
    }

    /// <summary>Övermåttet (krympmån) i tum: rått mått minus torrt mått.</summary>
    public static double AllowanceInches(double dryDimensionInches, WoodSpecies species, double targetMoisturePercent)
        => GreenDimensionInches(dryDimensionInches, species, targetMoisturePercent) - dryDimensionInches;
}
