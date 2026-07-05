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

    /// <summary>
    /// Renderar stockänden för ett visst steg i sågordningen (för vald metod):
    /// omkretsen klipps till den nuvarande formen (platt där man sågat), och det
    /// aktuella snittet markeras med linje + mått när <paramref name="showCutLine"/>.
    /// <paramref name="completedCuts"/> = antal gjorda snitt (0 = hel stock).
    /// </summary>
    public static string RenderStepped(PostningResult r, int completedCuts, SawMethod method, bool showCutLine,
        IReadOnlyList<Piece>? blockPieces = null)
    {
        var blockList = blockPieces ?? PostningLayout.BlockPieces(r);
        if (method == SawMethod.Genomsagning)
            return RenderGenom(r, completedCuts, showCutLine, blockList);
        double B = r.BlockWidth.Inches, H = r.BlockHeight.Inches;
        double bh = B / 2, hh = H / 2;
        double woodR = r.DiameterUnderBark.Inches / 2.0;
        double barkR = woodR * ApteringsMax.BarkFactor;
        double pad = barkR * 0.14, vb = (barkR + pad) * 2.0, c = vb / 2.0;

        var side = PostningLayout.SidePiecesPerSide(r);
        var end = PostningLayout.EndPiecesPerSide(r);
        var cuts = SawSequence.Compute(r, method, blockList);

        // Nuvarande platta yta per sida = innersta redan gjorda snittet på den sidan.
        double? FlatOf(SawFace f)
        {
            double? min = null;
            for (int i = 0; i < completedCuts && i < cuts.Count; i++)
                if (cuts[i].Face == f && (min is null || cuts[i].DistanceFromCenterInches < min))
                    min = cuts[i].DistanceFromCenterInches;
            return min;
        }
        double big = barkR + pad; // ingen platt sida än
        double rx = FlatOf(SawFace.Right) ?? big;
        double lx = FlatOf(SawFace.Left) ?? big;
        double by = FlatOf(SawFace.Bottom) ?? big;

        // Toppen: under delningen kapas blocket uppifrån, så toppen flyttas ned
        // förbi varje gjort delningssnitt (avsågade brädor försvinner).
        double? blockTopSigned = null;
        for (int i = 0; i < completedCuts && i < cuts.Count; i++)
            if (cuts[i].Face == SawFace.Block)
                blockTopSigned = cuts[i].AboveCenter == true ? -cuts[i].DistanceFromCenterInches : cuts[i].DistanceFromCenterInches;
        double ty = blockTopSigned is double bs ? -bs : (FlatOf(SawFace.Top) ?? big);

        var sb = new StringBuilder();
        sb.Append(CultureInfo.InvariantCulture, $"<svg class=\"log-svg\" viewBox=\"0 0 {F(vb)} {F(vb)}\" role=\"img\" aria-label=\"Stockände, steg {completedCuts}\">");
        sb.Append("<defs><radialGradient id=\"wood\" cx=\"42%\" cy=\"38%\" r=\"70%\">");
        sb.Append("<stop offset=\"0%\" stop-color=\"#faeecb\"/><stop offset=\"100%\" stop-color=\"#efd9a3\"/></radialGradient>");
        sb.Append(CultureInfo.InvariantCulture, $"<clipPath id=\"log\"><circle r=\"{F(woodR)}\"/></clipPath>");
        sb.Append(CultureInfo.InvariantCulture, $"<clipPath id=\"shape\"><rect x=\"{F(-lx)}\" y=\"{F(-ty)}\" width=\"{F(lx + rx)}\" height=\"{F(ty + by)}\"/></clipPath></defs>");
        sb.Append(CultureInfo.InvariantCulture, $"<g transform=\"translate({F(c)} {F(c)})\">");

        // Bark + splintved klipps mot den nuvarande formen (blir platt där man sågat).
        sb.Append("<g clip-path=\"url(#shape)\">");
        sb.Append(CultureInfo.InvariantCulture, $"<circle r=\"{F(barkR)}\" fill=\"#5b3a21\"/>");
        sb.Append(CultureInfo.InvariantCulture, $"<circle r=\"{F(woodR)}\" fill=\"url(#wood)\" stroke=\"#c9a86a\" stroke-width=\"1\" vector-effect=\"non-scaling-stroke\"/>");
        sb.Append("<g clip-path=\"url(#log)\">");
        sb.Append(CultureInfo.InvariantCulture, $"<rect x=\"{F(-bh)}\" y=\"{F(-hh)}\" width=\"{F(B)}\" height=\"{F(H)}\" fill=\"#e9cf95\"/>");
        foreach (var p in blockList)
            Rect(sb, -bh, -hh + p.Start, B, p.Thickness, Fill(p.Kind));
        for (int i = 0; i < side.Count; i++)
        {
            var p = side[i];
            Rect(sb, bh + p.Start, -hh, p.Thickness, H, Fill(p.Kind));
            Rect(sb, -bh - p.End, -hh, p.Thickness, H, Fill(p.Kind));
        }
        for (int i = 0; i < end.Count; i++)
        {
            var p = end[i];
            Rect(sb, -bh, -hh - p.End, B, p.Thickness, Fill(p.Kind));
            Rect(sb, -bh, hh + p.Start, B, p.Thickness, Fill(p.Kind));
        }
        sb.Append("</g></g>"); // klart: virke + formklipp

        // Aktuellt snitt: markeringslinje + mått i bilden (efter att bilden roterat).
        if (showCutLine && completedCuts >= 1 && completedCuts <= cuts.Count)
        {
            AppendCutLine(sb, cuts[completedCuts - 1], woodR);
            AppendDimension(sb, cuts, completedCuts, woodR);
        }

        sb.Append(CultureInfo.InvariantCulture, $"<circle r=\"{F(barkR * 0.02 + 0.05)}\" fill=\"{Stroke}\"/>");
        sb.Append("</g></svg>");
        return sb.ToString();
    }

    // Genomsågning: hela stocken skivas i parallella brädor (inget block), en orientering.
    private static string RenderGenom(PostningResult r, int completedCuts, bool showCutLine, IReadOnlyList<Piece> blockList)
    {
        double woodR = r.DiameterUnderBark.Inches / 2.0;
        double barkR = woodR * ApteringsMax.BarkFactor;
        double pad = barkR * 0.14, vb = (barkR + pad) * 2.0, c = vb / 2.0, big = barkR + pad;

        var cuts = SawSequence.Compute(r, SawMethod.Genomsagning, blockList);
        var ys = new List<double>();
        foreach (var cut in cuts)
            ys.Add(cut.AboveCenter == true ? -cut.DistanceFromCenterInches : cut.DistanceFromCenterInches);
        double topEdge = completedCuts >= 1 && completedCuts <= ys.Count ? ys[completedCuts - 1] : -big;

        double t = blockList.Count > 0 ? blockList[0].Thickness : 2.0;
        string boardFill = Math.Abs(t - 1.0) < 0.5 ? OneInchFill : (Math.Abs(t - 2.0) < 0.6 ? TwoInchFill : TargetFill);

        var sb = new StringBuilder();
        sb.Append(CultureInfo.InvariantCulture, $"<svg class=\"log-svg\" viewBox=\"0 0 {F(vb)} {F(vb)}\" role=\"img\" aria-label=\"Genomsågning, steg {completedCuts}\">");
        sb.Append("<defs><radialGradient id=\"wood\" cx=\"42%\" cy=\"38%\" r=\"70%\">");
        sb.Append("<stop offset=\"0%\" stop-color=\"#faeecb\"/><stop offset=\"100%\" stop-color=\"#efd9a3\"/></radialGradient>");
        sb.Append(CultureInfo.InvariantCulture, $"<clipPath id=\"log\"><circle r=\"{F(woodR)}\"/></clipPath>");
        sb.Append(CultureInfo.InvariantCulture, $"<clipPath id=\"shape\"><rect x=\"{F(-big)}\" y=\"{F(topEdge)}\" width=\"{F(2 * big)}\" height=\"{F(big - topEdge)}\"/></clipPath></defs>");
        sb.Append(CultureInfo.InvariantCulture, $"<g transform=\"translate({F(c)} {F(c)})\">");
        sb.Append("<g clip-path=\"url(#shape)\">");
        sb.Append(CultureInfo.InvariantCulture, $"<circle r=\"{F(barkR)}\" fill=\"#5b3a21\"/>");
        sb.Append(CultureInfo.InvariantCulture, $"<circle r=\"{F(woodR)}\" fill=\"url(#wood)\" stroke=\"#c9a86a\" stroke-width=\"1\" vector-effect=\"non-scaling-stroke\"/>");
        sb.Append("<g clip-path=\"url(#log)\">");
        for (int i = 0; i < ys.Count - 1; i++)
            Rect(sb, -big, ys[i], 2 * big, ys[i + 1] - ys[i], boardFill);
        sb.Append("</g></g>");

        if (showCutLine && completedCuts >= 1 && completedCuts <= cuts.Count)
        {
            AppendCutLine(sb, cuts[completedCuts - 1], woodR);
            AppendDimension(sb, cuts, completedCuts, woodR);
        }
        sb.Append(CultureInfo.InvariantCulture, $"<circle r=\"{F(barkR * 0.02 + 0.05)}\" fill=\"{Stroke}\"/>");
        sb.Append("</g></svg>");
        return sb.ToString();
    }

    // Signerad koordinat längs snittets axel (från centrum).
    private static double Coord(SawCut c) => c.Face switch
    {
        SawFace.Top => -c.DistanceFromCenterInches,
        SawFace.Bottom => c.DistanceFromCenterInches,
        SawFace.Left => -c.DistanceFromCenterInches,
        SawFace.Right => c.DistanceFromCenterInches,
        _ => c.AboveCenter == true ? -c.DistanceFromCenterInches : c.DistanceFromCenterInches,
    };

    // Snittet ritas som en horisontell linje (Top/Bottom/Block) eller vertikal (Left/Right).
    private static bool HorizontalCut(SawFace f) => f is SawFace.Top or SawFace.Bottom or SawFace.Block;

    private static void AppendCutLine(StringBuilder sb, SawCut cut, double woodR)
    {
        double d = Coord(cut);
        double L = woodR * 1.05;
        (double x1, double y1, double x2, double y2) = HorizontalCut(cut.Face)
            ? (-L, d, L, d)
            : (d, -L, d, L);
        sb.Append(CultureInfo.InvariantCulture,
            $"<line x1=\"{F(x1)}\" y1=\"{F(y1)}\" x2=\"{F(x2)}\" y2=\"{F(y2)}\" stroke=\"#d64545\" stroke-width=\"2.5\" stroke-dasharray=\"6 3\" vector-effect=\"non-scaling-stroke\"/>");
    }

    // Ritar måttet i bilden: från referenspunkt (centrum, eller förra snittet på samma sida) till snittlinjen.
    private static void AppendDimension(StringBuilder sb, IReadOnlyList<SawCut> cuts, int completedCuts, double woodR)
    {
        var cut = cuts[completedCuts - 1];
        SawCut? prev = null;
        for (int i = completedCuts - 2; i >= 0; i--)
            if (cuts[i].Face == cut.Face) { prev = cuts[i]; break; }

        double cutCoord = Coord(cut);
        double refCoord = cut.StepFromPreviousInches is null || prev is null ? 0 : Coord(prev);
        double valueMm = (cut.StepFromPreviousInches ?? cut.DistanceFromCenterInches) * SawConstants.MmPerInch;
        string txt = valueMm.ToString("0", CultureInfo.InvariantCulture) + " mm";
        double fs = woodR * 0.17;

        if (HorizontalCut(cut.Face))
        {
            double x = woodR * 0.18;
            DimLine(sb, x, refCoord, x, cutCoord);
            DimText(sb, x + fs * 0.4, (refCoord + cutCoord) / 2.0 + fs * 0.35, txt, fs, "start", cut.RotationDegrees);
        }
        else
        {
            double y = -woodR * 0.18;
            DimLine(sb, refCoord, y, cutCoord, y);
            DimText(sb, (refCoord + cutCoord) / 2.0, y - fs * 0.35, txt, fs, "middle", cut.RotationDegrees);
        }
    }

    private static void DimLine(StringBuilder sb, double x1, double y1, double x2, double y2)
    {
        sb.Append(CultureInfo.InvariantCulture,
            $"<line x1=\"{F(x1)}\" y1=\"{F(y1)}\" x2=\"{F(x2)}\" y2=\"{F(y2)}\" stroke=\"#2f5233\" stroke-width=\"1.4\" vector-effect=\"non-scaling-stroke\"/>");
        sb.Append(CultureInfo.InvariantCulture, $"<circle cx=\"{F(x1)}\" cy=\"{F(y1)}\" r=\"0.09\" fill=\"#2f5233\"/>");
        sb.Append(CultureInfo.InvariantCulture, $"<circle cx=\"{F(x2)}\" cy=\"{F(y2)}\" r=\"0.09\" fill=\"#2f5233\"/>");
    }

    // Texten motroteras med bildens rotation så den alltid står rätt (aldrig upp och ner).
    private static void DimText(StringBuilder sb, double x, double y, string text, double fontSize, string anchor, double imageRotationDeg)
        => sb.Append(CultureInfo.InvariantCulture,
            $"<text x=\"{F(x)}\" y=\"{F(y)}\" font-size=\"{F(fontSize)}\" text-anchor=\"{anchor}\" " +
            $"transform=\"rotate({F(-imageRotationDeg)} {F(x)} {F(y)})\" " +
            $"fill=\"#1e3a24\" stroke=\"#fbf7ec\" stroke-width=\"{F(fontSize * 0.2)}\" paint-order=\"stroke\" font-family=\"sans-serif\" font-weight=\"700\">{text}</text>");

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
