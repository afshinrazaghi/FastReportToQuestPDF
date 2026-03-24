using FastReport;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FastReportToQuestPDF.Drawers
{
    public class TextDrawer
    {
        public static void Draw(IContainer container, ReportPage page, TextObject txt)
        {
            float widthPts = Helpers.ToPoints(txt.Width);
            float heightPts = Helpers.ToPoints(txt.Height);

            // FIX 1: Use Min/Max to force STRICT sizing. 
            // This absolutely forbids QuestPDF from shrinking the box if it hits a page margin.
            container
                .TranslateX(Helpers.ToPoints(txt.AbsLeft))
                .TranslateY(Helpers.ToPoints(txt.AbsTop))
                .Width(widthPts)
                .Height(heightPts)
                .Background(Helpers.ConvertColor(txt.FillColor))
                .Layers(layers =>
                {
                    // BOTTOM LAYER: Text Content
                    layers.PrimaryLayer()
                        // FIX 2: Apply FastReport's native padding! 
                        // This ensures the text wraps exactly at the same character as FastReport.
                        .PaddingLeft(Helpers.ToPoints(txt.Padding.Left))
                        .PaddingRight(Helpers.ToPoints(txt.Padding.Right))
                        .PaddingTop(Helpers.ToPoints(txt.Padding.Top))
                        .PaddingBottom(Helpers.ToPoints(txt.Padding.Bottom))
                        .ScaleToFit()
                        // FIX 3: Apply Vertical Alignment (Missing in original code)
                        .Element(e =>
                        {
                            return txt.VertAlign switch
                            {
                                VertAlign.Center => e.AlignMiddle(),
                                VertAlign.Bottom => e.AlignBottom(),
                                _ => e.AlignTop()
                            };
                        })
                        // Cleaned up Text formatting block
                        .Text(text =>
                        {
                            // Apply Horizontal Alignment natively to the text block
                            switch (txt.HorzAlign)
                            {
                                case HorzAlign.Center: text.AlignCenter(); break;
                                case HorzAlign.Right: text.AlignRight(); break;
                                case HorzAlign.Left: text.AlignLeft(); break;
                                case HorzAlign.Justify: text.Justify(); break;
                            }

                            // Apply Text and Font Styles
                            var span = text.Span(txt.Text)
                                .FontFamily(txt.Font.Name)
                                .FontSize(Helpers.ToPoints(txt.Font.Size))
                                .FontColor(Helpers.ConvertColor(txt.TextColor));

                            if (txt.Font.Style.HasFlag(FontStyle.Bold)) span.Bold();
                            if (txt.Font.Style.HasFlag(FontStyle.Italic)) span.Italic();
                            if (txt.Font.Style.HasFlag(FontStyle.Underline)) span.Underline();
                            if (txt.Font.Style.HasFlag(FontStyle.Strikeout)) span.Strikethrough();
                        });

                    // TOP LAYER: The perfected exact Outer Border
                    if (txt.Border.Lines != FastReport.BorderLines.None || txt.Border.Shadow)
                    {
                        layers.Layer().Element(e => Helpers.DrawComplexBorder(e, txt.Border, widthPts, heightPts));
                    }
                });
        }
    }
}
