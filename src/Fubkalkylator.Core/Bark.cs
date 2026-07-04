namespace Fubkalkylator.Core;

/// <summary>
/// Barktjocklek per trädslag. ApteringsMax använder en platt barkfaktor (×1,05);
/// den här modellen förfinar den så att t.ex. tall (tjock bark) och björk (tunn)
/// hanteras olika. Barken uttrycks som ett påslag på fub.
/// </summary>
public static class Bark
{
    /// <summary>Faktor från fub till diameter på bark (t.ex. 1,05 = 5 % påslag).</summary>
    public static double OverBarkFactor(WoodSpecies species) => species switch
    {
        WoodSpecies.Gran => 1.06,
        WoodSpecies.Tall => 1.12,
        WoodSpecies.Bjork => 1.04,
        WoodSpecies.Ek => 1.10,
        _ => 1.05,
    };

    /// <summary>Diameter på bark (tum) för en given fub.</summary>
    public static double OnBarkFromFub(WoodSpecies species, double fubInches)
    {
        if (fubInches < 0)
            throw new ArgumentOutOfRangeException(nameof(fubInches), fubInches, "Diametern kan inte vara negativ.");
        return fubInches * OverBarkFactor(species);
    }

    /// <summary>Fub (tum) för en uppmätt diameter på bark.</summary>
    public static double FubFromOnBark(WoodSpecies species, double onBarkInches)
    {
        if (onBarkInches < 0)
            throw new ArgumentOutOfRangeException(nameof(onBarkInches), onBarkInches, "Diametern kan inte vara negativ.");
        return onBarkInches / OverBarkFactor(species);
    }

    /// <summary>Sammanlagd barktjocklek (båda sidor), tum, för en uppmätt diameter på bark.</summary>
    public static double DoubleBarkThicknessInches(WoodSpecies species, double onBarkInches)
        => onBarkInches - FubFromOnBark(species, onBarkInches);
}
