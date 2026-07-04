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
    /// Renderar stockänden för ett visst steg i sågordningen: bitar som redan
    /// kapats bort tonas ned, och det aktuella snittet markeras med en linje.
    /// <paramref name="completedCuts"/> = antal gjorda snitt (0 = hel stock).
    /// Rotationen (så att rätt sida hamnar uppåt) sköts av anroparen.
    /// </summary>
    public static string RenderStepped(PostningResult r, int completedCuts)
    {
        double B = r.BlockWidth.Inches, H = r.BlockHeight.Inches;
        double bh = B / 2, hh = H / 2;
        double woodR = r.DiameterUnderBark.Inches / 2.0;
        double barkR = woodR * ApteringsMax.BarkFactor;
        double pad = barkR * 0.14, vb = (barkR + pad) * 2.0, c = vb / 2.0;

        var side = PostningLayout.SidePiecesPerSide(r);
        var end = PostningLayout.EndPiecesPerSide(r);

        // Tilldela snittnummer per bit, i exakt samma ordning som SawSequence.
        int rightStart = 1;
        int leftStart = rightStart + 1 + side.Count;
        int topStart = leftStart + 1 + side.Count;
        int botStart = topStart + 1 + end.Count;

        // Nuvarande form: en platt sida uppstår där man redan sågat.
        double big = barkR + pad; // ingen platt sida än
        double rx = FaceFlat(side, bh, rightStart, completedCuts) ?? big;
        double lx = FaceFlat(side, bh, leftStart, completedCuts) ?? big;
        double ty = FaceFlat(end, hh, topStart, completedCuts) ?? big;
        double by = FaceFlat(end, hh, botStart, completedCuts) ?? big;

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
        foreach (var p in PostningLayout.BlockPieces(r))
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

        // Aktuellt snitt: markeringslinje + mått i bilden.
        if (completedCuts >= 1)
        {
            AppendCutLine(sb, r, completedCuts, woodR);
            AppendDimension(sb, r, completedCuts, woodR);
        }

        sb.Append(CultureInfo.InvariantCulture, $"<circle r=\"{F(barkR * 0.02 + 0.05)}\" fill=\"{Stroke}\"/>");
        sb.Append("</g></svg>");
        return sb.ToString();
    }

    // Avstånd från centrum till en sidas nuvarande platta yta (null = sidan inte sågad än).
    private static double? FaceFlat(IReadOnlyList<Piece> pieces, double blockHalf, int startNum, int completedCuts)
    {
        if (completedCuts < startNum) return null;
        if (pieces.Count == 0) return blockHalf;
        double flat = blockHalf + pieces[^1].End; // efter bak-snittet
        int num = startNum;
        for (int i = pieces.Count - 1; i >= 0; i--)
        {
            num++;
            if (completedCuts >= num) flat = blockHalf + pieces[i].Start;
        }
        return flat;
    }

    // Ritar en röd markeringslinje för det aktuella snittet (i oroterade koordinater).
    private static void AppendCutLine(StringBuilder sb, PostningResult r, int completedCuts, double woodR)
    {
        var cuts = SawSequence.Compute(r);
        if (completedCuts > cuts.Count) return;
        var cut = cuts[completedCuts - 1];
        double d = cut.DistanceFromCenterInches;
        double L = woodR * 1.05;
        (double x1, double y1, double x2, double y2) = cut.Phase switch
        {
            CutPhase.SideFace1 => (d, -L, d, L),
            CutPhase.SideFace2 => (-d, -L, -d, L),
            CutPhase.EndFace1 => (-L, -d, L, -d),
            CutPhase.EndFace2 => (-L, d, L, d),
            _ => (-L, (cut.AboveCenter == true ? -d : d), L, (cut.AboveCenter == true ? -d : d)),
        };
        sb.Append(CultureInfo.InvariantCulture,
            $"<line x1=\"{F(x1)}\" y1=\"{F(y1)}\" x2=\"{F(x2)}\" y2=\"{F(y2)}\" stroke=\"#d64545\" stroke-width=\"2.5\" stroke-dasharray=\"6 3\" vector-effect=\"non-scaling-stroke\"/>");
    }

    // Ritar måttet i bilden: från referenspunkt (centrum, eller föregående snitt) till snittlinjen.
    private static void AppendDimension(StringBuilder sb, PostningResult r, int completedCuts, double woodR)
    {
        var cuts = SawSequence.Compute(r);
        if (completedCuts > cuts.Count) return;
        var cut = cuts[completedCuts - 1];
        var prev = completedCuts >= 2 && cuts[completedCuts - 2].Phase == cut.Phase ? cuts[completedCuts - 2] : null;

        static double Coord(SawCut c) => c.Phase switch
        {
            CutPhase.SideFace1 => c.DistanceFromCenterInches,
            CutPhase.SideFace2 => -c.DistanceFromCenterInches,
            CutPhase.EndFace1 => -c.DistanceFromCenterInches,
            CutPhase.EndFace2 => c.DistanceFromCenterInches,
            _ => c.AboveCenter == true ? -c.DistanceFromCenterInches : c.DistanceFromCenterInches,
        };

        double cutCoord = Coord(cut);
        double refCoord = prev is null ? 0 : Coord(prev);
        double valueMm = (cut.StepFromPreviousInches ?? cut.DistanceFromCenterInches) * SawConstants.MmPerInch;
        string txt = valueMm.ToString("0", CultureInfo.InvariantCulture) + " mm";
        double fs = woodR * 0.17;
        bool horizontalCut = cut.Phase is CutPhase.EndFace1 or CutPhase.EndFace2 or CutPhase.BlockSplit;

        if (!horizontalCut)
        {
            double y = -woodR * 0.18;
            DimLine(sb, refCoord, y, cutCoord, y);
            DimText(sb, (refCoord + cutCoord) / 2.0, y - fs * 0.35, txt, fs, "middle");
        }
        else
        {
            double x = woodR * 0.18;
            DimLine(sb, x, refCoord, x, cutCoord);
            DimText(sb, x + fs * 0.4, (refCoord + cutCoord) / 2.0 + fs * 0.35, txt, fs, "start");
        }
    }

    private static void DimLine(StringBuilder sb, double x1, double y1, double x2, double y2)
    {
        sb.Append(CultureInfo.InvariantCulture,
            $"<line x1=\"{F(x1)}\" y1=\"{F(y1)}\" x2=\"{F(x2)}\" y2=\"{F(y2)}\" stroke=\"#2f5233\" stroke-width=\"1.4\" vector-effect=\"non-scaling-stroke\"/>");
        sb.Append(CultureInfo.InvariantCulture, $"<circle cx=\"{F(x1)}\" cy=\"{F(y1)}\" r=\"0.09\" fill=\"#2f5233\"/>");
        sb.Append(CultureInfo.InvariantCulture, $"<circle cx=\"{F(x2)}\" cy=\"{F(y2)}\" r=\"0.09\" fill=\"#2f5233\"/>");
    }

    private static void DimText(StringBuilder sb, double x, double y, string text, double fontSize, string anchor)
        => sb.Append(CultureInfo.InvariantCulture,
            $"<text x=\"{F(x)}\" y=\"{F(y)}\" font-size=\"{F(fontSize)}\" text-anchor=\"{anchor}\" fill=\"#1e3a24\" stroke=\"#fbf7ec\" stroke-width=\"{F(fontSize * 0.2)}\" paint-order=\"stroke\" font-family=\"sans-serif\" font-weight=\"700\">{text}</text>");

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
