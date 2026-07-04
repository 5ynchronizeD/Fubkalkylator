namespace Fubkalkylator.Core;

/// <summary>
/// Grundkonstanter för sågning. All intern räkning sker i tum (");
/// omvandling till mm/cm sker i <see cref="Measure"/>.
/// </summary>
public static class SawConstants
{
    /// <summary>
    /// Standardsågspår i tum (1/4" ≈ 6,35 mm) — kedjesågverk, som i Excel.
    /// Kan överstyras per beräkning (t.ex. ~3 mm för bandsågverk).
    /// </summary>
    public const double KerfInches = 0.25;

    /// <summary>Millimeter per tum.</summary>
    public const double MmPerInch = 25.4;

    /// <summary>Centimeter per tum.</summary>
    public const double CmPerInch = 2.54;

    /// <summary>Utrymme en bräda/regel tar: tjocklek + ett sågspår.</summary>
    public static double Slot(double boardThicknessInches, double kerfInches)
        => boardThicknessInches + kerfInches;
}
