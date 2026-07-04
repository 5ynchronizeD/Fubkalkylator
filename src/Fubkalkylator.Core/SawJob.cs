namespace Fubkalkylator.Core;

/// <summary>Torkstatus för en sparad sågning.</summary>
public enum DryingStatus
{
    Torkar = 0,
    Klar = 1,
}

/// <summary>En fukthaltsmätning vid ett tillfälle (procent).</summary>
public sealed class MoistureReading
{
    public DateTime Date { get; set; }
    public double Percent { get; set; }
}

/// <summary>
/// En sparad sågning i loggboken: den teoretiska beräkningen plus det faktiska
/// utfallet du matar in. Alla mått i tum.
/// </summary>
public sealed class SawJob
{
    /// <summary>Unikt id (0 = ny, tilldelas vid sparande).</summary>
    public int Id { get; set; }

    /// <summary>När posten sparades.</summary>
    public DateTime SavedAt { get; set; }

    // --- Teoretiskt (från beräkningen) ---
    public double StockFubInches { get; set; }
    public double KerfInches { get; set; }
    public double? TargetThicknessInches { get; set; }
    public double? TargetWidthInches { get; set; }
    public double BlockWidthInches { get; set; }
    public double BlockHeightInches { get; set; }

    /// <summary>Kort sammanfattning av vad beräkningen förutsåg.</summary>
    public string CalculatedOutcome { get; set; } = "";

    // --- Faktiskt (du fyller i) ---
    public string Species { get; set; } = "";
    public string ActualOutcome { get; set; } = "";
    public string Note { get; set; } = "";
    public DryingStatus Drying { get; set; }
    public DateTime? DryingStart { get; set; }

    /// <summary>Fukthaltsmätningar över tid (torkkurva), äldst först.</summary>
    public List<MoistureReading> MoistureReadings { get; set; } = new();
}

/// <summary>
/// Lagring för loggboken. Plattformen avgör implementationen
/// (JSON-fil på Android, i minnet på webben).
/// </summary>
public interface ISawJobStore
{
    /// <summary>Alla poster, nyast först.</summary>
    Task<IReadOnlyList<SawJob>> GetAllAsync();

    /// <summary>Lägger till (Id=0) eller uppdaterar en post. Returnerar posten med Id.</summary>
    Task<SawJob> SaveAsync(SawJob job);

    /// <summary>Tar bort en post.</summary>
    Task DeleteAsync(int id);
}
