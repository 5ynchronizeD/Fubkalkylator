namespace Fubkalkylator.Core;

/// <summary>En rad i en orderlista: önskad dimension och antal bitar.</summary>
public sealed record OrderItem
{
    public required double ThicknessInches { get; init; }
    public required double WidthInches { get; init; }
    public required int Quantity { get; init; }
}

/// <summary>Resultatet för en orderrad: minsta stock, bitar per stock, antal stockar.</summary>
public sealed record OrderLineResult
{
    public required OrderItem Item { get; init; }

    /// <summary>Minsta stock som ger dimensionen. Null om den inte går att uppnå.</summary>
    public required TargetResult? Target { get; init; }

    public int PiecesPerLog => Target?.TargetPieceCount ?? 0;

    public bool Achievable => Target is not null && PiecesPerLog > 0;

    /// <summary>Antal stockar som krävs (avrundat uppåt).</summary>
    public int LogsNeeded => !Achievable || Item.Quantity <= 0
        ? 0
        : (int)Math.Ceiling(Item.Quantity / (double)PiecesPerLog);
}

/// <summary>Hela orderplanen.</summary>
public sealed record OrderPlan
{
    public required IReadOnlyList<OrderLineResult> Lines { get; init; }

    /// <summary>Totalt antal stockar för hela ordern.</summary>
    public int TotalLogs => Lines.Sum(l => l.LogsNeeded);
}

/// <summary>
/// Planerar en orderlista: för varje önskad dimension hittas minsta stock
/// (via <see cref="TargetDimension"/>) och antal stockar som behövs.
/// </summary>
public static class OrderPlanner
{
    public static OrderPlan Plan(IEnumerable<OrderItem> items, double kerfInches = SawConstants.KerfInches)
    {
        ArgumentNullException.ThrowIfNull(items);
        var lines = new List<OrderLineResult>();
        foreach (var item in items)
        {
            TargetResult? target = null;
            if (item.ThicknessInches > 0 && item.WidthInches > 0)
            {
                try { target = TargetDimension.Compute(item.WidthInches, item.ThicknessInches, kerfInches); }
                catch (ArgumentOutOfRangeException) { /* ogiltig rad → ej uppnåbar */ }
            }
            lines.Add(new OrderLineResult { Item = item, Target = target });
        }
        return new OrderPlan { Lines = lines };
    }
}
