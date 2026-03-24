using FastReport;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FastReportToQuestPDF.Drawers
{
    public class ShapeDrawer
    {
        public static void Draw(IContainer container, ReportPage page, ShapeObject shapeObject)
        {
            // 1. Convert dimensions to Points
            float widthPts = Helpers.ToPoints(shapeObject.Width);
            float heightPts = Helpers.ToPoints(shapeObject.Height);
            float strokeWidth = Helpers.ToPoints(shapeObject.Border.Width);

            // 2. Get Colors
            // Assuming you have a helper that returns "#RRGGBB" or "none"
            string borderColor = Helpers.ConvertToSvgColor(shapeObject.Border.Color);
            string fillColor = Helpers.ConvertToSvgColor(shapeObject.FillColor); // Helper needs to handle "Transparent" -> "none"
            if (shapeObject.FillColor.Name == "Transparent")
                fillColor = "none";
            // 3. Handle Border Style (Dash, Dot, etc.)
            string dashArray = Helpers.GetDashArray(shapeObject.Border.Style, strokeWidth);

            string svgContent = "";

            // 4. Generate SVG
            switch (shapeObject.Shape)
            {
                case ShapeKind.Rectangle:
                    svgContent = GenerateSvgRect(widthPts, heightPts, strokeWidth, borderColor, fillColor, dashArray, 0);
                    break;

                case ShapeKind.RoundRectangle:
                    float curveRadius = Math.Min(widthPts, heightPts) * 0.15f;
                    if (shapeObject.Curve > 0)
                        curveRadius = shapeObject.Curve + Math.Min(widthPts, heightPts) * 0.15f;
                    svgContent = GenerateSvgRect(widthPts, heightPts, strokeWidth, borderColor, fillColor, dashArray, curveRadius);
                    break;

                case ShapeKind.Ellipse:
                    svgContent = GenerateSvgEllipse(widthPts, heightPts, strokeWidth, borderColor, fillColor, dashArray);
                    break;

                case ShapeKind.Triangle:
                    svgContent = GenerateSvgTriangle(widthPts, heightPts, strokeWidth, borderColor, fillColor, dashArray);
                    break;

                case ShapeKind.Diamond:
                    svgContent = GenerateSvgDiamond(widthPts, heightPts, strokeWidth, borderColor, fillColor, dashArray);
                    break;

                default:
                    svgContent = "";
                    break;
            }

            if (string.IsNullOrEmpty(svgContent))
                return;

            // 5. Draw in QuestPDF
            container
                .TranslateX(Helpers.ToPoints(shapeObject.AbsLeft))
                .TranslateY(Helpers.ToPoints(shapeObject.AbsTop))
                // Rotate around center if needed (FastReport objects rotate around center usually)
                .Width(widthPts)
                .Height(heightPts)
                .Svg(svgContent);
        }

        #region Draw Rectanble
        private static string GenerateSvgRect(float width, float height, float strokeWidth,
                                      string strokeColor, string fillColor, string dashArray, float radius)
        {
            string F(float val) => val.ToString("0.###", CultureInfo.InvariantCulture);

            // MATH FIX FOR PRECISION:
            // In SVG, the stroke is drawn on the center of the line. 
            // If we draw a rect from 0 to 100, the border will spill outside (to -1 and 101).
            // We must inset the rectangle by half the stroke width so it fits perfectly inside the box.
            float halfStroke = strokeWidth / 2f;
            float rectX = halfStroke;
            float rectY = halfStroke;
            float rectW = width - strokeWidth;
            float rectH = height - strokeWidth;

            var sb = new StringBuilder();
            sb.AppendLine($"<svg viewBox=\"0 0 {F(width)} {F(height)}\" xmlns=\"http://www.w3.org/2000/svg\">");

            sb.Append($"<rect x=\"{F(rectX)}\" y=\"{F(rectY)}\" width=\"{F(rectW)}\" height=\"{F(rectH)}\" ");

            // Appearance
            sb.Append($"fill=\"{fillColor}\" ");
            sb.Append($"stroke=\"{strokeColor}\" ");
            sb.Append($"stroke-width=\"{F(strokeWidth)}\" ");

            // Dashed Borders
            if (!string.IsNullOrEmpty(dashArray))
            {
                sb.Append($"stroke-dasharray=\"{dashArray}\" ");
            }

            // Rounded Corners
            if (radius > 0)
            {
                sb.Append($"rx=\"{F(radius)}\" ry=\"{F(radius)}\" ");
            }

            sb.AppendLine("/>");
            sb.AppendLine("</svg>");

            return sb.ToString();
        }

        #endregion

        #region Draw Ellipse
        private static string GenerateSvgEllipse(float width, float height, float strokeWidth,
                                  string strokeColor, string fillColor, string dashArray)
        {
            string F(float val) => val.ToString("0.###", CultureInfo.InvariantCulture);

            // MATH FIX FOR PRECISION:
            // Center point is exactly in the middle of the bounding box.
            float cx = width / 2f;
            float cy = height / 2f;

            // The radius must be reduced by half the stroke width so the border doesn't bleed outside the SVG viewBox.
            // Max(0, ...) prevents negative radii if the stroke is thicker than the shape itself.
            float rx = Math.Max(0, (width - strokeWidth) / 2f);
            float ry = Math.Max(0, (height - strokeWidth) / 2f);

            var sb = new StringBuilder();
            sb.AppendLine($"<svg viewBox=\"0 0 {F(width)} {F(height)}\" xmlns=\"http://www.w3.org/2000/svg\">");

            sb.Append($"<ellipse cx=\"{F(cx)}\" cy=\"{F(cy)}\" rx=\"{F(rx)}\" ry=\"{F(ry)}\" ");

            sb.Append($"fill=\"{fillColor}\" ");
            sb.Append($"stroke=\"{strokeColor}\" ");
            sb.Append($"stroke-width=\"{F(strokeWidth)}\" ");

            if (!string.IsNullOrEmpty(dashArray))
            {
                sb.Append($"stroke-dasharray=\"{dashArray}\" ");
            }

            sb.AppendLine("/>");
            sb.AppendLine("</svg>");

            return sb.ToString();
        }
        #endregion

        #region Draw Triangle
        private static string GenerateSvgTriangle(float width, float height, float strokeWidth,
                                   string strokeColor, string fillColor, string dashArray)
        {
            string F(float val) => val.ToString("0.###", CultureInfo.InvariantCulture);

            // MATH FIX FOR PRECISION:
            // We must inset the vertices by half the stroke width so the border draws inside the bounding box.
            float halfStroke = strokeWidth / 2f;

            // Calculate the 3 vertices of the triangle (Top Center, Bottom Right, Bottom Left)
            float topX = width / 2f;
            float topY = halfStroke;

            float rightX = Math.Max(0, width - halfStroke);
            float bottomY = Math.Max(0, height - halfStroke);

            float leftX = halfStroke;

            var sb = new StringBuilder();
            sb.AppendLine($"<svg viewBox=\"0 0 {F(width)} {F(height)}\" xmlns=\"http://www.w3.org/2000/svg\">");

            // The points attribute defines the 3 corners of the polygon
            string points = $"{F(topX)},{F(topY)} {F(rightX)},{F(bottomY)} {F(leftX)},{F(bottomY)}";

            sb.Append($"<polygon points=\"{points}\" ");

            sb.Append($"fill=\"{fillColor}\" ");
            sb.Append($"stroke=\"{strokeColor}\" ");
            sb.Append($"stroke-width=\"{F(strokeWidth)}\" ");

            // CRUCIAL: 'round' join prevents the sharp apex from spiking outside the top bounding box
            sb.Append("stroke-linejoin=\"round\" ");

            if (!string.IsNullOrEmpty(dashArray))
            {
                sb.Append($"stroke-dasharray=\"{dashArray}\" ");
            }

            sb.AppendLine("/>");
            sb.AppendLine("</svg>");

            return sb.ToString();
        }
        #endregion

        #region Draw Diamond
        private static string GenerateSvgDiamond(float width, float height, float strokeWidth,
                                  string strokeColor, string fillColor, string dashArray)
        {
            string F(float val) => val.ToString("0.###", CultureInfo.InvariantCulture);

            // MATH FIX FOR PRECISION:
            // Inset the vertices by half the stroke width to prevent border clipping
            float halfStroke = strokeWidth / 2f;

            // Calculate the 4 vertices of the diamond (Top, Right, Bottom, Left midpoints)
            float topX = width / 2f;
            float topY = halfStroke;

            float rightX = Math.Max(0, width - halfStroke);
            float rightY = height / 2f;

            float bottomX = width / 2f;
            float bottomY = Math.Max(0, height - halfStroke);

            float leftX = halfStroke;
            float leftY = height / 2f;

            var sb = new StringBuilder();
            sb.AppendLine($"<svg viewBox=\"0 0 {F(width)} {F(height)}\" xmlns=\"http://www.w3.org/2000/svg\">");

            // The points attribute defines the 4 corners of the diamond
            string points = $"{F(topX)},{F(topY)} {F(rightX)},{F(rightY)} {F(bottomX)},{F(bottomY)} {F(leftX)},{F(leftY)}";

            sb.Append($"<polygon points=\"{points}\" ");

            sb.Append($"fill=\"{fillColor}\" ");
            sb.Append($"stroke=\"{strokeColor}\" ");
            sb.Append($"stroke-width=\"{F(strokeWidth)}\" ");

            // 'round' join prevents sharp corner spikes from exceeding the viewBox boundaries
            sb.Append("stroke-linejoin=\"round\" ");

            if (!string.IsNullOrEmpty(dashArray))
            {
                sb.Append($"stroke-dasharray=\"{dashArray}\" ");
            }

            sb.AppendLine("/>");
            sb.AppendLine("</svg>");

            return sb.ToString();
        }
        #endregion
    }
}
