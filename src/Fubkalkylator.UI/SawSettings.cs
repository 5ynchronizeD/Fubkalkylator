using Fubkalkylator.Core;

namespace Fubkalkylator.UI;

/// <summary>
/// Delade såginställningar som gäller över hela appen (delas mellan sidor och plattformar).
/// </summary>
public class SawSettings
{
    /// <summary>Sågspår/klingtjocklek i tum. Standard 1/4" (kedjesåg); ~3 mm för bandsåg.</summary>
    public double KerfInches { get; set; } = SawConstants.KerfInches;
}
