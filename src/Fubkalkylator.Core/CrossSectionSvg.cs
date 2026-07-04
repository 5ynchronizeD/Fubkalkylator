using System.Globalization;
using System.Text;

namespace Fubkalkylator.Core;

/// <summary>
/// Renderar en stockände som ett fristående SVG-fragment: cirkel med bark,
/// block uppdelat i reglar/brädor, sido- och ändbrädor samt förblockets
/// sågsekvens. Ren sträng utan UI-beroenden — används av både webb och MAUI.
/// </summary>
public static class CrossSectionSvg
{
    private const string TwoInchFill = "#b06a2c";
    private const string OneInchFill = "#e3b466";
    private const string TargetFill = "#c07d33";
    private const string Stroke = "#4a2f18";

    /// <summary>Renderar med blockets ordinarie 1"/2"-uppdelning.</summary>
    public static string Render(PostningResult r) => Render(r, PostningLayout.BlockPieces(r));

    /// <summary>
    /// Renderar med en egen blockuppdelning (t.ex. likadana bitar av en
    /// måltjocklek). Sido-/ändbrädor ritas som vanligt.
    /// </summary>
    public static string Render(PostningResult r, IReadOnlyList<Piece> blockPieces)
    {
        double B = r.BlockWidth.Inches;
        double H = r.BlockHeight.Inches;
        double FB = r.PreBlockWidth.Inches;
        double FH = r.PreBlockHeight.Inches;

        double woodR = r.DiameterUnderBark.Inches / 2.0;
        double barkR = woodR * ApteringsMax.BarkFactor;
        double pad = barkR * 0.14;
        double vb = (barkR + pad) * 2.0;
        double c = vb / 2.0;

        var sb = new StringBuilder();
        sb.Append(CultureInfo.InvariantCulture,
            $"<svg class=\"log-svg\" viewBox=\"0 0 {F(vb)} {F(vb)}\" role=\"img\" ");
        sb.Append("aria-label=\"Stockände med block, sido- och ändbrädor\">");
        sb.Append("<defs><radialGradient id=\"wood\" cx=\"42%\" cy=\"38%\" r=\"70%\">");
        sb.Append("<stop offset=\"0%\" stop-color=\"#faeecb\"/>");
        sb.Append("<stop offset=\"100%\" stop-color=\"#efd9a3\"/></radialGradient>");
        // Allt virke klipps mot stockens splintvedscirkel — inget virke kan finnas
        // utanför stocken, så brädor nära kanten kapas naturligt (rundade hörn).
        sb.Append(CultureInfo.InvariantCulture, $"<clipPath id=\"log\"><circle r=\"{F(woodR)}\"/></clipPath>");
        sb.Append("</defs>");
        sb.Append(CultureInfo.InvariantCulture, $"<g transform=\"translate({F(c)} {F(c)})\">");

        // Bark + splintved
        sb.Append(CultureInfo.InvariantCulture, $"<circle r=\"{F(barkR)}\" fill=\"#5b3a21\"/>");
        sb.Append(CultureInfo.InvariantCulture,
            $"<circle r=\"{F(woodR)}\" fill=\"url(#wood)\" stroke=\"#c9a86a\" stroke-width=\"1\" vector-effect=\"non-scaling-stroke\"/>");

        // Virkesgrupp: klippt mot stockcirkeln
        sb.Append("<g clip-path=\"url(#log)\">");

        // Block-bas
        sb.Append(CultureInfo.InvariantCulture,
            $"<rect x=\"{F(-B / 2)}\" y=\"{F(-H / 2)}\" width=\"{F(B)}\" height=\"{F(H)}\" fill=\"#e9cf95\"/>");

        // Blockets bitar (ordinarie eller egen uppdelning)
        foreach (var p in blockPieces)
            Rect(sb, -B / 2, -H / 2 + p.Start, B, p.Thickness, Fill(p.Kind));

        // Sidobrädor (vänster + höger)
        foreach (var p in PostningLayout.SidePiecesPerSide(r))
        {
            Rect(sb, -B / 2 - p.End, -H / 2, p.Thickness, H, Fill(p.Kind));
            Rect(sb, B / 2 + p.Start, -H / 2, p.Thickness, H, Fill(p.Kind));
        }

        // Ändbrädor (topp + botten)
        foreach (var p in PostningLayout.EndPiecesPerSide(r))
        {
            Rect(sb, -B / 2, -H / 2 - p.End, B, p.Thickness, Fill(p.Kind));
            Rect(sb, -B / 2, H / 2 + p.Start, B, p.Thickness, Fill(p.Kind));
        }

        // Förblockets kontur (streckad sågsekvens)
        DashedRect(sb, -FB / 2, -H / 2, FB, H);
        DashedRect(sb, -B / 2, -FH / 2, B, FH);

        sb.Append("</g>"); // slut virkesgrupp

        // Märg
        sb.Append(CultureInfo.InvariantCulture, $"<circle r=\"{F(barkR * 0.02 + 0.05)}\" fill=\"{Stroke}\"/>");

        sb.Append("</g></svg>");
        return sb.ToString();
    }

    private static void Rect(StringBuilder sb, double x, double y, double w, double h, string fill)
        => sb.Append(CultureInfo.InvariantCulture,
            $"<rect x=\"{F(x)}\" y=\"{F(y)}\" width=\"{F(w)}\" height=\"{F(h)}\" fill=\"{fill}\" stroke=\"{Stroke}\" stroke-width=\"1\" vector-effect=\"non-scaling-stroke\"/>");

    private static void DashedRect(StringBuilder sb, double x, double y, double w, double h)
        => sb.Append(CultureInfo.InvariantCulture,
            $"<rect x=\"{F(x)}\" y=\"{F(y)}\" width=\"{F(w)}\" height=\"{F(h)}\" fill=\"none\" stroke=\"#6b4f2a\" stroke-width=\"1.4\" stroke-dasharray=\"5 3\" vector-effect=\"non-scaling-stroke\"/>");

    private static string Fill(BoardKind kind) => kind switch
    {
        BoardKind.TwoInch => TwoInchFill,
        BoardKind.OneInch => OneInchFill,
        _ => TargetFill,
    };

    private static string F(double v) => v.ToString("0.###", CultureInfo.InvariantCulture);
}
