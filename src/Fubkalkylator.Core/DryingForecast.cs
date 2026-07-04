namespace Fubkalkylator.Core;

/// <summary>Torkprognos utifrån en serie fukthaltsmätningar.</summary>
public sealed record DryingEstimate
{
    /// <summary>Torktakt (procentenheter per dygn). Negativ = torkar.</summary>
    public required double SlopePercentPerDay { get; init; }

    /// <summary>Uppskattat datum då målfukthalten nås. Null om det inte går att uppskatta.</summary>
    public required DateTime? TargetDate { get; init; }

    /// <summary>Sant om senaste mätningen redan är vid eller under målet.</summary>
    public required bool AlreadyReached { get; init; }
}

/// <summary>
/// Uppskattar när en sågning nått en målfukthalt genom linjär regression på
/// mätningarna. Enkel modell (torkning är egentligen exponentiell) — för planering.
/// </summary>
public static class DryingForecast
{
    /// <summary>
    /// Uppskattar torktiden mot <paramref name="targetPercent"/>. Kräver minst två
    /// mätningar vid olika datum. Returnerar null om det inte går att uppskatta.
    /// </summary>
    public static DryingEstimate? Estimate(IReadOnlyList<MoistureReading> readings, double targetPercent)
    {
        if (readings is null || readings.Count < 2)
            return null;

        var ordered = readings.OrderBy(r => r.Date).ToList();
        var last = ordered[^1];

        if (last.Percent <= targetPercent)
            return new DryingEstimate { SlopePercentPerDay = 0, TargetDate = last.Date, AlreadyReached = true };

        DateTime start = ordered[0].Date;
        // Linjär regression: x = dygn sedan första mätning, y = fukthalt.
        double n = ordered.Count;
        double sx = 0, sy = 0, sxx = 0, sxy = 0;
        foreach (var r in ordered)
        {
            double x = (r.Date - start).TotalDays;
            double y = r.Percent;
            sx += x; sy += y; sxx += x * x; sxy += x * y;
        }
        double denom = n * sxx - sx * sx;
        if (Math.Abs(denom) < 1e-9)
            return null;

        double slope = (n * sxy - sx * sy) / denom;         // %/dygn
        double intercept = (sy - slope * sx) / n;

        if (slope >= 0)                                       // torkar inte
            return new DryingEstimate { SlopePercentPerDay = slope, TargetDate = null, AlreadyReached = false };

        double daysToTarget = (targetPercent - intercept) / slope;
        DateTime target = start.AddDays(daysToTarget);
        // Prognosen ska aldrig ligga före den senaste mätningen.
        if (target < last.Date) target = last.Date;

        return new DryingEstimate { SlopePercentPerDay = slope, TargetDate = target, AlreadyReached = false };
    }
}
