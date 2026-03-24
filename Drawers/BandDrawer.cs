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
    public class BandDrawer
    {
        public static void Draw(IContainer container, ReportPage page, FastReport.BandBase band)
        {
            float widthPts = Helpers.ToPoints(band.Width);
            float heightPts = Helpers.ToPoints(band.Height);

            container
                .Width(widthPts)
                .Height(heightPts)
                .Layers(layers =>
                {
                    // Layer 1: The Border (SVG Overlay)
                    layers.Layer().Element(e => Helpers.DrawComplexBorder(e, band.Border, widthPts, heightPts));

                    // Layer 2: The Content
                    layers.PrimaryLayer().Column(col =>
                    {
                        // Draw your band objects here
                    });
                });
        }
    }
}
