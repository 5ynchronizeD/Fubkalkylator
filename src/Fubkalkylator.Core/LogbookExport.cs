using System.Globalization;
using System.Text;

namespace Fubkalkylator.Core;

/// <summary>Exporterar loggboken till CSV (semikolon-separerad, svenskt talformat).</summary>
public static class LogbookExport
{
    private static readonly CultureInfo Se = CultureInfo.GetCultureInfo("sv-SE");

    private static readonly string[] Header =
    {
        "Datum", "Trädslag", "Stock fub (tum)", "Sågspår (mm)", "Längd (m)",
        "Volym (m³)", "Värde (kr)", "Utbyte (%)", "Torkstatus", "Senaste fukthalt (%)",
        "Beräknat", "Faktiskt", "Anteckning",
    };

    /// <summary>Bygger en CSV-sträng av sågningarna.</summary>
    public static string ToCsv(IEnumerable<SawJob> jobs)
    {
        ArgumentNullException.ThrowIfNull(jobs);
        var sb = new StringBuilder();
        sb.Append(string.Join(';', Header)).Append("\r\n");

        foreach (var j in jobs)
        {
            double? lastMoisture = j.MoistureReadings.Count > 0 ? j.MoistureReadings[^1].Percent : null;
            string[] cells =
            {
                j.SavedAt.ToString("yyyy-MM-dd"),
                j.Species,
                Num(j.StockFubInches),
                Num(j.KerfInches * SawConstants.MmPerInch),
                j.StockLengthInches is double len ? Num(len * SawConstants.MetersPerInch) : "",
                j.TimberVolumeM3 is double v ? Num(v, 3) : "",
                j.EstimatedValue is double val ? Num(val, 0) : "",
                j.YieldPercent?.ToString(Se) ?? "",
                j.Drying == DryingStatus.Klar ? "Klar" : "Torkar",
                lastMoisture is double m ? Num(m, 1) : "",
                j.CalculatedOutcome,
                j.ActualOutcome,
                j.Note,
            };
            sb.Append(string.Join(';', cells.Select(Escape))).Append("\r\n");
        }
        return sb.ToString();
    }

    private static string Num(double value, int decimals = 2)
        => Math.Round(value, decimals).ToString(Se);

    private static string Escape(string? field)
    {
        field ??= "";
        if (field.Contains('"') || field.Contains(';') || field.Contains('\n') || field.Contains('\r'))
            return "\"" + field.Replace("\"", "\"\"") + "\"";
        return field;
    }
}
