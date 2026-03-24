using FastReport;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FastReportToQuestPDF.Drawers
{
    public class LineDrawer
    {
        public static void Draw(IContainer container, ReportPage page, LineObject lineObject)
        {
            // 1. Precise Conversion to Points
            float widthPts = Helpers.ToPoints(lineObject.Width);
            float heightPts = Helpers.ToPoints(lineObject.Height);
            float strokeWidth = Helpers.ToPoints(lineObject.Border.Width);

            // 2. Calculate Hypotenuse (Actual length of the line)
            float actualLength = MathF.Sqrt(MathF.Pow(widthPts, 2) + MathF.Pow(heightPts, 2));

            // 3. Calculate Angle
            float angleDegrees = MathF.Atan2(heightPts, widthPts) * (180 / MathF.PI);

            // 4. Determine Start Coordinates based on FastReport logic
            float startX = Helpers.ToPoints(lineObject.AbsLeft);
            float startY = Helpers.ToPoints(lineObject.AbsTop);

            //if (lineObject.Diagonal)
            //{
            //    // "Diagonal" in FastReport usually means Bottom-Left to Top-Right
            //    startY += heightPts;
            //    angleDegrees = -angleDegrees;
            //}

            // 5. Arrow Configuration
            bool hasStartArrow = lineObject.StartCap.Style == CapStyle.Arrow;
            bool hasEndArrow = lineObject.EndCap.Style == CapStyle.Arrow;

            // Calculate Arrow Sizes
            float arrowLen = strokeWidth * 4f;
            float arrowWidth = strokeWidth * 3f;

            // 6. Calculate Container Height
            // We need enough height so the arrow "wings" aren't cut off.
            float svgHeight = MathF.Max(strokeWidth, arrowWidth) * 2.5f;
            float centerY = svgHeight / 2f;

            // 7. Adjust Line Segments to not overlap Arrow Heads
            float lineStartX = hasStartArrow ? arrowLen : 0;
            float lineEndX = hasEndArrow ? actualLength - arrowLen : actualLength;

            string hexColor = Helpers.ConvertToSvgColor(lineObject.Border.Color);

            // 8. Generate SVG
            string svgContent = GenerateSvgLine(actualLength, svgHeight, strokeWidth, hexColor,
                                                centerY, lineStartX, lineEndX,
                                                hasStartArrow, hasEndArrow, arrowLen, arrowWidth);

            container
                // Move to the exact Start Point
                .TranslateX(startX)
                .TranslateY(startY)
                // Rotate towards the End Point
                .Rotate(angleDegrees)
                // --- THE FIX: --- 
                // Shift UP by half the height. 
                // This ensures the middle of the SVG (where the line is) aligns with the pivot point.
                .TranslateY(-svgHeight / 2f)
                .Width(actualLength)
                .Height(svgHeight)
                // Render SVG
                .Svg(svgContent);
        }


        private static string GenerateSvgLine(float width, float height, float strokeWidth, string color,
                                      float centerY, float lineStartX, float lineEndX,
                                      bool hasStartArrow, bool hasEndArrow, float arrowLen, float arrowWidth)
        {
            // Use InvariantCulture to ensure we write "5.5" instead of "5/5" or "5,5"
            string F(float val) => val.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture);

            var sb = new StringBuilder();
            // viewBox defines the coordinate system inside the SVG
            sb.AppendLine($"<svg viewBox=\"0 0 {F(width)} {F(height)}\" xmlns=\"http://www.w3.org/2000/svg\">");

            // 1. Draw Start Arrow
            if (hasStartArrow)
            {
                // Tip is exactly at (0, centerY)
                string p1 = $"0,{F(centerY)}";
                string p2 = $"{F(arrowLen)},{F(centerY - arrowWidth / 2)}";
                string p3 = $"{F(arrowLen)},{F(centerY + arrowWidth / 2)}";
                sb.AppendLine($"<polygon points=\"{p1} {p2} {p3}\" fill=\"{color}\" />");
            }

            // 2. Draw End Arrow
            if (hasEndArrow)
            {
                // Tip is exactly at (width, centerY)
                string p1 = $"{F(width)},{F(centerY)}";
                string p2 = $"{F(width - arrowLen)},{F(centerY - arrowWidth / 2)}";
                string p3 = $"{F(width - arrowLen)},{F(centerY + arrowWidth / 2)}";
                sb.AppendLine($"<polygon points=\"{p1} {p2} {p3}\" fill=\"{color}\" />");
            }

            // 3. Draw Main Line
            // Only draw if there is space between arrows
            if (lineEndX > lineStartX)
            {
                // shape-rendering="geometricPrecision" ensures sharp edges
                sb.AppendLine($"<line x1=\"{F(lineStartX)}\" y1=\"{F(centerY)}\" x2=\"{F(lineEndX)}\" y2=\"{F(centerY)}\" stroke=\"{color}\" stroke-width=\"{F(strokeWidth)}\" shape-rendering=\"geometricPrecision\" />");
            }

            sb.AppendLine("</svg>");
            return sb.ToString();
        }
    }
}
