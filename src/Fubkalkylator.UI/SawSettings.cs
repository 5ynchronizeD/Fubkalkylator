using Fubkalkylator.Core;

namespace Fubkalkylator.UI;

/// <summary>
/// Delade såginställningar som gäller över hela appen (delas mellan sidor och plattformar).
/// </summary>
public class SawSettings
{
    /// <summary>Sågspår/klingtjocklek i tum. Standard 1/4" (kedjesåg); ~3 mm för bandsåg.</summary>
    public double KerfInches { get; set; } = SawConstants.KerfInches;

    /// <summary>Pris för virke ur blocket, kr/m³.</summary>
    public double BlockPricePerM3 { get; set; } = PriceList.Default.BlockPerCubicMeter;

    /// <summary>Pris för biprodukter (sido-/ändbrädor), kr/m³.</summary>
    public double ByproductPricePerM3 { get; set; } = PriceList.Default.ByproductPerCubicMeter;

    /// <summary>Målfukthalt (%) som torkprognosen räknar mot.</summary>
    public double TargetMoisturePercent { get; set; } = 15;

    /// <summary>Vald sågmetod (snittordning) för visualiseringen.</summary>
    public SawMethod SawMethod { get; set; } = SawMethod.Block180;

    /// <summary>Märgdelning: lägg ett snitt genom kärnan (block-/varvsågning).</summary>
    public bool CenterCutThroughPith { get; set; }

    /// <summary>Stockklämmans höjd i mm — sätter minsta möjliga bottenbräda. 0 = ingen gräns.</summary>
    public double ClampHeightMm { get; set; }

    /// <summary>Aktuell prislista utifrån inställningarna.</summary>
    public PriceList Prices => new()
    {
        BlockPerCubicMeter = BlockPricePerM3,
        ByproductPerCubicMeter = ByproductPricePerM3,
    };

    /// <summary>Triggas när en global inställning ändras (t.ex. från profilmenyn) så sidor kan räkna om.</summary>
    public event Action? Changed;

    /// <summary>Meddela att en inställning ändrats.</summary>
    public void RaiseChanged() => Changed?.Invoke();
}
