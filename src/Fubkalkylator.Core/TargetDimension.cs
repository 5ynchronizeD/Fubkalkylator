namespace Fubkalkylator.Core;

/// <summary>
/// Resultatet av en måldimensionsberäkning: minsta stock som ger en önskad
/// färdig dimension (bredd × tjocklek, valfria mått), samt full postning.
/// </summary>
public sealed record TargetResult
{
    /// <summary>Önskad färdig bredd (tum).</summary>
    public required Measure TargetWidth { get; init; }

    /// <summary>Önskad tjocklek (tum) — valfri, t.ex. 1,5".</summary>
    public required Measure TargetThickness { get; init; }

    /// <summary>Postningen för den minsta stock som ger dimensionen.</summary>
    public required PostningResult Postning { get; init; }

    /// <summary>Antal bitar av måldimension ur blocket.</summary>
    public required int TargetPieceCount { get; init; }

    /// <summary>Blockets uppdelning i måltjocka bitar (för ritningen).</summary>
    public required IReadOnlyList<Piece> BlockPieces { get; init; }

    /// <summary>Blockets faktiska bredd [B]. Kan vara större än målbredden.</summary>
    public Measure ActualBlockWidth => Postning.BlockWidth;

    /// <summary>Överbredd som kapas bort för att nå målbredden (tum).</summary>
    public double TrimWidthInches => Postning.BlockWidth.Inches - TargetWidth.Inches;

    /// <summary>Toppdiameter på bark [pb] för den valda stocken (fub × 1,05).</summary>
    public Measure TopDiameterOverBark => Postning.DiameterUnderBark.Inches * ApteringsMax.BarkFactor;
}

/// <summary>
/// Resultatet av "får det plats i min stock" — framlänges från en given stock.
/// </summary>
public sealed record FitResult
{
    /// <summary>Önskad bredd (tum).</summary>
    public required Measure TargetWidth { get; init; }
    /// <summary>Önskad tjocklek (tum).</summary>
    public required Measure TargetThickness { get; init; }
    /// <summary>Postningen för den angivna stocken.</summary>
    public required PostningResult Postning { get; init; }
    /// <summary>Ryms målbredden i blocket?</summary>
    public required bool WidthFits { get; init; }
    /// <summary>Antal bitar av måldimension (0 om bredden inte ryms).</summary>
    public required int PieceCount { get; init; }
    /// <summary>Blockets uppdelning i måltjocka bitar (för ritningen).</summary>
    public required IReadOnlyList<Piece> BlockPieces { get; init; }

    /// <summary>Största bredd stocken klarar (= blockbredd).</summary>
    public Measure MaxWidth => Postning.BlockWidth;
    /// <summary>Överbredd som kapas bort (tum), om bredden ryms.</summary>
    public double TrimWidthInches => Math.Max(0, Postning.BlockWidth.Inches - TargetWidth.Inches);
}

/// <summary>
/// Räknar baklänges: givet en önskad färdig dimension (bredd × tjocklek),
/// hitta den minsta stock som ger den. Blocket klyvs i likadana bitar av
/// måltjockleken; brädans bredd fås ur blockbredden (kapas vid behov).
/// Använder <see cref="PostningsMax"/> som facit och skannar diametern uppåt.
/// </summary>
public static class TargetDimension
{
    /// <summary>Steg vid diameterskanning (tum).</summary>
    public const double Step = 0.25;

    /// <summary>
    /// Hittar minsta stockdiameter vars block är minst <paramref name="widthInches"/>
    /// brett och rymmer minst en bit av <paramref name="thicknessInches"/> tjocklek.
    /// Returnerar null om dimensionen inte kan uppnås inom <paramref name="maxFubInches"/>.
    /// </summary>
    public static TargetResult? Compute(double widthInches, double thicknessInches,
        double kerfInches = SawConstants.KerfInches,
        double minFubInches = 2.0, double maxFubInches = 20.0)
    {
        if (widthInches <= 0)
            throw new ArgumentOutOfRangeException(nameof(widthInches), widthInches,
                "Bredden måste vara större än 0.");
        if (thicknessInches <= 0)
            throw new ArgumentOutOfRangeException(nameof(thicknessInches), thicknessInches,
                "Tjockleken måste vara större än 0.");

        for (double fub = minFubInches; fub <= maxFubInches + 1e-9; fub += Step)
        {
            PostningResult p;
            try
            {
                p = PostningsMax.Compute(fub, kerfInches);
            }
            catch (ArgumentOutOfRangeException)
            {
                continue; // för klen stock — blockhöjd under tabellminimum
            }

            // Blocket måste vara minst så brett som målbredden.
            if (p.BlockWidth.Inches + 1e-9 < widthInches)
                continue;

            int count = PostningLayout.CountByThickness(p.BlockHeight.Inches, thicknessInches, kerfInches);
            if (count < 1)
                continue; // blocket för lågt för en hel bit av måltjockleken

            return new TargetResult
            {
                TargetWidth = widthInches,
                TargetThickness = thicknessInches,
                Postning = p,
                TargetPieceCount = count,
                BlockPieces = PostningLayout.BlockPiecesOfThickness(p.BlockHeight.Inches, thicknessInches, kerfInches),
            };
        }
        return null;
    }

    /// <summary>
    /// Bredden spelar ingen roll: för varje möjlig blockbredd (heltal tum) ges
    /// minsta stock och antal bitar av <paramref name="thicknessInches"/>.
    /// En rad per uppnåbar bredd inom <paramref name="maxFubInches"/>.
    /// </summary>
    public static IReadOnlyList<TargetResult> ByWidth(double thicknessInches,
        double kerfInches = SawConstants.KerfInches, double maxFubInches = 20.0)
    {
        if (thicknessInches <= 0)
            throw new ArgumentOutOfRangeException(nameof(thicknessInches), thicknessInches,
                "Tjockleken måste vara större än 0.");

        var rows = new List<TargetResult>();
        for (int w = 2; w <= (int)maxFubInches; w++)
        {
            var r = Compute(w, thicknessInches, kerfInches, maxFubInches: maxFubInches);
            if (r is null) break;               // bredare block ryms inte i stocken
            if (r.ActualBlockWidth.Inches == w)  // ta bara raden där blocket blir exakt bredden
                rows.Add(r);
        }
        return rows;
    }

    /// <summary>
    /// Framlänges: hur en given stock kan kapas till måldimensionen.
    /// Blocket för stocken klyvs i måltjocka bitar; bredden fås ur blockbredden.
    /// Returnerar null om stocken är för klen för att ge ett block alls.
    /// </summary>
    public static FitResult? FitInStock(double fubInches, double widthInches, double thicknessInches,
        double kerfInches = SawConstants.KerfInches)
    {
        if (widthInches <= 0)
            throw new ArgumentOutOfRangeException(nameof(widthInches), widthInches, "Bredden måste vara > 0.");
        if (thicknessInches <= 0)
            throw new ArgumentOutOfRangeException(nameof(thicknessInches), thicknessInches, "Tjockleken måste vara > 0.");

        PostningResult p;
        try { p = PostningsMax.Compute(fubInches, kerfInches); }
        catch (ArgumentOutOfRangeException) { return null; }

        bool widthFits = widthInches <= p.BlockWidth.Inches + 1e-9;
        int count = widthFits ? PostningLayout.CountByThickness(p.BlockHeight.Inches, thicknessInches, kerfInches) : 0;

        return new FitResult
        {
            TargetWidth = widthInches,
            TargetThickness = thicknessInches,
            Postning = p,
            WidthFits = widthFits,
            PieceCount = count,
            BlockPieces = PostningLayout.BlockPiecesOfThickness(p.BlockHeight.Inches, thicknessInches, kerfInches),
        };
    }

    /// <summary>
    /// Tjockleken spelar ingen roll: minsta stock vars block är minst
    /// <paramref name="widthInches"/> brett. Blocket lämnas odelat (klyvs fritt).
    /// </summary>
    public static PostningResult? SmallestStockForWidth(double widthInches,
        double kerfInches = SawConstants.KerfInches,
        double minFubInches = 2.0, double maxFubInches = 20.0)
    {
        if (widthInches <= 0)
            throw new ArgumentOutOfRangeException(nameof(widthInches), widthInches,
                "Bredden måste vara större än 0.");

        for (double fub = minFubInches; fub <= maxFubInches + 1e-9; fub += Step)
        {
            PostningResult p;
            try { p = PostningsMax.Compute(fub, kerfInches); }
            catch (ArgumentOutOfRangeException) { continue; }

            if (p.BlockWidth.Inches + 1e-9 >= widthInches)
                return p;
        }
        return null;
    }
}
