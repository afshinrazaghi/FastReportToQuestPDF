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
    public class PolygonDrawer
    {
        public static void Draw(IContainer container, ReportPage page, FastReport.PolygonObject polygonObject)
        {
            if (polygonObject.Points == null || polygonObject.Points.Count == 0) return;

            // 1. CALCULATE BOUNDING BOX OF THE POINTS
            // This identifies the "real" internal offset of the shape
            float minX = polygonObject.Points.Min(p => p.X);
            float minY = polygonObject.Points.Min(p => p.Y);
            float maxX = polygonObject.Points.Max(p => p.X);
            float maxY = polygonObject.Points.Max(p => p.Y);

            float internalWidth = maxX - minX;
            float internalHeight = maxY - minY;

            // 2. PREPARE STROKE AND FILL
            float strokeWidth = Helpers.ToPoints(polygonObject.Border.Width);
            string strokeColor = Helpers.ConvertColor(polygonObject.Border.Color);
            string dashArray = Helpers.GetDashArray(polygonObject.Border.Style, strokeWidth);

            string fillColor = "none";
            if (polygonObject.Fill is FastReport.SolidFill solidFill)
            {
                fillColor = solidFill.Color.A == 0 ? "none" : Helpers.ConvertColor(solidFill.Color);
            }

            // 3. NORMALIZE POINTS (Subtract minX/minY)
            // We map the points so the top-left-most point is at 0,0 in SVG space
            var pointsBuilder = new StringBuilder();
            foreach (var pt in polygonObject.Points)
            {
                // We convert to Points units and subtract the internal offset
                float normalizedX = Helpers.ToPoints(pt.X - minX);
                float normalizedY = Helpers.ToPoints(pt.Y - minY);

                pointsBuilder.Append($"{normalizedX.ToString(CultureInfo.InvariantCulture)},{normalizedY.ToString(CultureInfo.InvariantCulture)} ");
            }

            float svgWidthPts = Helpers.ToPoints(internalWidth);
            float svgHeightPts = Helpers.ToPoints(internalHeight);

            // 4. GENERATE SVG
            // We use overflow="visible" to ensure the stroke isn't clipped at the 0,0 edge
            string svgContent = $@"
                <svg width=""100%"" height=""100%"" viewBox=""0 0 {svgWidthPts.ToString(CultureInfo.InvariantCulture)} {svgHeightPts.ToString(CultureInfo.InvariantCulture)}"" overflow=""visible"" xmlns=""http://www.w3.org/2000/svg"">
                    <polygon 
                        points=""{pointsBuilder.ToString().Trim()}"" 
                        fill=""{fillColor}"" 
                        stroke=""{strokeColor}"" 
                        stroke-width=""{strokeWidth.ToString(CultureInfo.InvariantCulture)}"" 
                        stroke-dasharray=""{dashArray}""
                        stroke-linejoin=""round"" />
                </svg>";

            // 5. RENDER WITH CALIBRATED TRANSLATION
            container
                // Move to the Object's Absolute position + the internal offset of the points
                .TranslateX(Helpers.ToPoints(polygonObject.AbsLeft))
                .TranslateY(Helpers.ToPoints(polygonObject.AbsTop))
                .Width(svgWidthPts)
                .Height(svgHeightPts)
                .Svg(svgContent);
        }
    }
}
