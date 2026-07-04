using Fubkalkylator.Core;
using Xunit;

namespace Fubkalkylator.Core.Tests;

public class DryingForecastTests
{
    private static MoistureReading R(int day, double pct) =>
        new() { Date = new DateTime(2026, 1, 1).AddDays(day), Percent = pct };

    [Fact]
    public void Too_few_readings_returns_null()
    {
        Assert.Null(DryingForecast.Estimate(new[] { R(0, 40) }, 15));
        Assert.Null(DryingForecast.Estimate(Array.Empty<MoistureReading>(), 15));
    }

    [Fact]
    public void Linear_drying_reaches_target_on_expected_day()
    {
        // 40 % dag 0, 30 % dag 10 → −1 %/dygn → 15 % nås dag 25.
        var readings = new[] { R(0, 40), R(10, 30) };
        var est = DryingForecast.Estimate(readings, 15)!;

        Assert.False(est.AlreadyReached);
        Assert.Equal(-1.0, est.SlopePercentPerDay, 6);
        Assert.Equal(new DateTime(2026, 1, 1).AddDays(25), est.TargetDate!.Value, TimeSpan.FromHours(1));
    }

    [Fact]
    public void Already_below_target_is_flagged()
    {
        var readings = new[] { R(0, 20), R(10, 12) };
        var est = DryingForecast.Estimate(readings, 15)!;
        Assert.True(est.AlreadyReached);
    }

    [Fact]
    public void Not_drying_gives_no_target_date()
    {
        var readings = new[] { R(0, 20), R(10, 25) };  // ökar
        var est = DryingForecast.Estimate(readings, 15)!;
        Assert.Null(est.TargetDate);
        Assert.False(est.AlreadyReached);
    }
}
