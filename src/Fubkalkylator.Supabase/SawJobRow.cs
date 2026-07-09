using Fubkalkylator.Core;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace Fubkalkylator.Supabase;

/// <summary>Postgrest-modell mot tabellen <c>saw_jobs</c>. Mappar till/från <see cref="SawJob"/>.</summary>
[Table("saw_jobs")]
public sealed class SawJobRow : BaseModel
{
    // Identitetskolumn i DB → skickas inte vid insert (false).
    [PrimaryKey("id", false)] public int Id { get; set; }

    [Column("saved_at")] public DateTime SavedAt { get; set; }

    [Column("stock_fub_inches")] public double StockFubInches { get; set; }
    [Column("kerf_inches")] public double KerfInches { get; set; }
    [Column("target_thickness_inches")] public double? TargetThicknessInches { get; set; }
    [Column("target_width_inches")] public double? TargetWidthInches { get; set; }
    [Column("block_width_inches")] public double BlockWidthInches { get; set; }
    [Column("block_height_inches")] public double BlockHeightInches { get; set; }
    [Column("stock_length_inches")] public double? StockLengthInches { get; set; }
    [Column("timber_volume_m3")] public double? TimberVolumeM3 { get; set; }
    [Column("estimated_value")] public double? EstimatedValue { get; set; }
    [Column("yield_percent")] public int? YieldPercent { get; set; }
    [Column("calculated_outcome")] public string CalculatedOutcome { get; set; } = "";

    [Column("species")] public string Species { get; set; } = "";
    [Column("actual_outcome")] public string ActualOutcome { get; set; } = "";
    [Column("note")] public string Note { get; set; } = "";
    [Column("photo_data_url")] public string? PhotoDataUrl { get; set; }
    [Column("drying")] public int Drying { get; set; }
    [Column("drying_start")] public DateTime? DryingStart { get; set; }
    [Column("moisture_readings")] public List<MoistureReading> MoistureReadings { get; set; } = new();

    public static SawJobRow FromJob(SawJob j) => new()
    {
        Id = j.Id,
        SavedAt = j.SavedAt,
        StockFubInches = j.StockFubInches,
        KerfInches = j.KerfInches,
        TargetThicknessInches = j.TargetThicknessInches,
        TargetWidthInches = j.TargetWidthInches,
        BlockWidthInches = j.BlockWidthInches,
        BlockHeightInches = j.BlockHeightInches,
        StockLengthInches = j.StockLengthInches,
        TimberVolumeM3 = j.TimberVolumeM3,
        EstimatedValue = j.EstimatedValue,
        YieldPercent = j.YieldPercent,
        CalculatedOutcome = j.CalculatedOutcome,
        Species = j.Species,
        ActualOutcome = j.ActualOutcome,
        Note = j.Note,
        PhotoDataUrl = j.PhotoDataUrl,
        Drying = (int)j.Drying,
        DryingStart = j.DryingStart,
        MoistureReadings = j.MoistureReadings,
    };

    public SawJob ToJob() => new()
    {
        Id = Id,
        SavedAt = SavedAt,
        StockFubInches = StockFubInches,
        KerfInches = KerfInches,
        TargetThicknessInches = TargetThicknessInches,
        TargetWidthInches = TargetWidthInches,
        BlockWidthInches = BlockWidthInches,
        BlockHeightInches = BlockHeightInches,
        StockLengthInches = StockLengthInches,
        TimberVolumeM3 = TimberVolumeM3,
        EstimatedValue = EstimatedValue,
        YieldPercent = YieldPercent,
        CalculatedOutcome = CalculatedOutcome,
        Species = Species,
        ActualOutcome = ActualOutcome,
        Note = Note,
        PhotoDataUrl = PhotoDataUrl,
        Drying = (DryingStatus)Drying,
        DryingStart = DryingStart,
        MoistureReadings = MoistureReadings ?? new(),
    };
}
