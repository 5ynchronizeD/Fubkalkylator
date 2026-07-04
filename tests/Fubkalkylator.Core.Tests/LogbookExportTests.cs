using Fubkalkylator.Core;
using Xunit;

namespace Fubkalkylator.Core.Tests;

public class LogbookExportTests
{
    [Fact]
    public void Empty_gives_header_only()
    {
        var csv = LogbookExport.ToCsv(Array.Empty<SawJob>());
        var lines = csv.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
        Assert.Single(lines);
        Assert.StartsWith("Datum;Trädslag;", lines[0]);
    }

    [Fact]
    public void Row_has_same_column_count_as_header()
    {
        var job = new SawJob
        {
            SavedAt = new DateTime(2026, 3, 1),
            Species = "Gran",
            StockFubInches = 9.75,
            KerfInches = 0.25,
            Drying = DryingStatus.Torkar,
            CalculatedOutcome = "block 6×7,75",
        };
        var csv = LogbookExport.ToCsv(new[] { job });
        var lines = csv.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);

        int headerCols = lines[0].Split(';').Length;
        int rowCols = lines[1].Split(';').Length;
        Assert.Equal(headerCols, rowCols);
        Assert.Contains("2026-03-01", lines[1]);
        Assert.Contains("Gran", lines[1]);
    }

    [Fact]
    public void Fields_with_separator_are_quoted()
    {
        var job = new SawJob
        {
            SavedAt = new DateTime(2026, 3, 1),
            Species = "Gran",
            Note = "kluven; fin kvalitet",
        };
        var csv = LogbookExport.ToCsv(new[] { job });
        Assert.Contains("\"kluven; fin kvalitet\"", csv);
    }
}
