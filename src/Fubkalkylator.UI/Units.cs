using Fubkalkylator.Core;

namespace Fubkalkylator.UI;

/// <summary>Vilken metrisk enhet ett fält använder när tum-läget är av.</summary>
public enum MetricUnit { Mm, Cm, M }

/// <summary>
/// Hjälpare för det globala enhetsvalet (<see cref="SawSettings.UseInches"/>). Internt
/// lagras allt i tum; detta konverterar till/från det som visas i fälten.
/// </summary>
public static class Units
{
    public static string Symbol(SawSettings s, MetricUnit m) =>
        s.UseInches ? "\"" : m switch { MetricUnit.Mm => "mm", MetricUnit.Cm => "cm", _ => "m" };

    private static double PerInch(MetricUnit m) => m switch
    {
        MetricUnit.Mm => SawConstants.MmPerInch,
        MetricUnit.Cm => SawConstants.CmPerInch,
        _ => SawConstants.MmPerInch / 1000.0,
    };

    /// <summary>Tum → visat värde (tum eller metriskt), avrundat lagom för fältet.</summary>
    public static double ToDisplay(double inches, SawSettings s, MetricUnit m)
        => s.UseInches ? System.Math.Round(inches, 2) : System.Math.Round(inches * PerInch(m), m == MetricUnit.Mm ? 0 : 2);

    /// <summary>Visat värde → tum.</summary>
    public static double FromDisplay(double display, SawSettings s, MetricUnit m)
        => s.UseInches ? display : display / PerInch(m);
}
