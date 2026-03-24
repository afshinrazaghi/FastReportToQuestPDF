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
    public class PolyLineDrawer
    {
        public static void Draw(IContainer container, ReportPage page, FastReport.PolyLineObject polyLineObject)
        {
            float widthPts = Helpers.ToPoints(polyLineObject.Width);
            float heightPts = Helpers.ToPoints(polyLineObject.Height);

            // Extract border properties
            float strokeWidth = Helpers.ToPoints(polyLineObject.Border.Width);
            string strokeColor = Helpers.ConvertColor(polyLineObject.Border.Color);
            string dashArray = Helpers.GetDashArray(polyLineObject.Border.Style, strokeWidth);

            // Build the SVG points string
            var pointsBuilder = new StringBuilder();
            foreach (var pt in polyLineObject.Points)
            {
                // Convert each FastReport point to QuestPDF points
                float ptX = Helpers.ToPoints(pt.X);
                float ptY = Helpers.ToPoints(pt.Y);

                // CultureInfo.InvariantCulture is CRITICAL here so decimals use dots (.) 
                // instead of commas (,) which would break the SVG standard.
                pointsBuilder.Append($"{ptX.ToString(CultureInfo.InvariantCulture)},{ptY.ToString(CultureInfo.InvariantCulture)} ");
            }

            // Generate the SVG with <polyline>
            // stroke-linejoin="round" and stroke-linecap="round" prevent sharp, protruding spikes at tight angles
            string svgContent = $@"
            <svg width=""100%"" height=""100%"" viewBox=""0 0 {widthPts.ToString(CultureInfo.InvariantCulture)} {heightPts.ToString(CultureInfo.InvariantCulture)}"" overflow=""visible"" xmlns=""http://www.w3.org/2000/svg"">
                <polyline 
                    points=""{pointsBuilder.ToString().Trim()}"" 
                    fill=""none"" 
                    stroke=""{strokeColor}"" 
                    stroke-width=""{strokeWidth.ToString(CultureInfo.InvariantCulture)}"" 
                    stroke-dasharray=""{dashArray}""
                    stroke-linejoin=""round"" 
                    stroke-linecap=""round"" />
            </svg>";

            // Render in QuestPDF
            container
                .TranslateX(Helpers.ToPoints(polyLineObject.AbsLeft))
                .TranslateY(Helpers.ToPoints(polyLineObject.AbsTop))
                .Width(widthPts)
                .Height(heightPts)
                .Svg(svgContent);
        }
    }
}
