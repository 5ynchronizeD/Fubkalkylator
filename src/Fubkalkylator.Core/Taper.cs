namespace Fubkalkylator.Core;

/// <summary>
/// Avsmalning (taper): en stock är konisk och smalnar av mot toppen. Enkel
/// linjär modell — diametern ändras med en fast takt (cm per meter) längs stocken.
/// </summary>
public static class Taper
{
    /// <summary>Typisk avsmalning för sågtimmer (~1 cm per meter).</summary>
    public const double TypicalCmPerMeter = 1.0;

    /// <summary>Grovändens (rotändens) diameter given toppdiametern.</summary>
    public static double ButtDiameterCm(double topDiameterCm, double taperCmPerMeter, double lengthMeters)
    {
        if (lengthMeters < 0)
            throw new ArgumentOutOfRangeException(nameof(lengthMeters), lengthMeters, "Längden kan inte vara negativ.");
        return topDiameterCm + taperCmPerMeter * lengthMeters;
    }

    /// <summary>Toppdiametern given grovändens diameter.</summary>
    public static double TopDiameterCm(double buttDiameterCm, double taperCmPerMeter, double lengthMeters)
    {
        if (lengthMeters < 0)
            throw new ArgumentOutOfRangeException(nameof(lengthMeters), lengthMeters, "Längden kan inte vara negativ.");
        return buttDiameterCm - taperCmPerMeter * lengthMeters;
    }

    /// <summary>Mittdiametern (halvvägs längs stocken).</summary>
    public static double MidDiameterCm(double topDiameterCm, double taperCmPerMeter, double lengthMeters)
        => topDiameterCm + taperCmPerMeter * lengthMeters / 2.0;

    /// <summary>
    /// Topphöjning (toppningsbräda på t.ex. Logosol): för att såga parallellt med
    /// märgen läggs stocken med barken nedåt och den smalare toppänden lyfts så att
    /// märgen blir vågrät. Lyftet är halva diameterskillnaden mellan rot- och toppände
    /// (märgen sitter en radie över bädden i var ände). Returnerar 0 om toppen inte är
    /// smalare (inget att kompensera för). Enheten följer indata (cm in → cm ut).
    /// </summary>
    public static double ToeBoardRaiseCm(double buttDiameterCm, double topDiameterCm)
    {
        double raise = (buttDiameterCm - topDiameterCm) / 2.0;
        return raise > 0 ? raise : 0;
    }
}
