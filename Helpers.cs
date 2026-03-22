using FastReport;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FastReportToQuestPDF
{
    public  class Helpers
    {
        public static float ToPoints(float value)
        {
            return value * 0.749916457811947f;  // = 0.75f
        }

        public static QuestPDF.Infrastructure.Color ConvertColor(System.Drawing.Color c)
        {
            return QuestPDF.Infrastructure.Color.FromARGB(c.A, c.R, c.G, c.B);
        }

        public static string ConvertToSvgColor(System.Drawing.Color c)
        {
            if (c.Name == "Transparent")
                return "none";
            return ConvertColor(c);

        }


        public static void DrawComplexBorder(IContainer container, FastReport.Border border, float width, float height)
        {
            var svg = new StringBuilder();

            // We keep overflow="visible" just in case of shadows, but the lines will now be strictly inside the box.
            svg.Append($@"<svg width=""{width.ToString(CultureInfo.InvariantCulture)}"" height=""{height.ToString(CultureInfo.InvariantCulture)}"" viewBox=""0 0 {width.ToString(CultureInfo.InvariantCulture)} {height.ToString(CultureInfo.InvariantCulture)}"" overflow=""visible"" xmlns=""http://www.w3.org/2000/svg"">");

            // 1. Draw Shadow first (so it sits behind the border lines)
            if (border.Shadow)
            {
                float sW = ToPoints(border.ShadowWidth);
                string sC = ConvertColor(border.ShadowColor);
                // Shadow sits behind the box, shifted by sW
                svg.Append($@"<rect x=""{sW.ToString(CultureInfo.InvariantCulture)}"" y=""{sW.ToString(CultureInfo.InvariantCulture)}"" width=""{width.ToString(CultureInfo.InvariantCulture)}"" height=""{height.ToString(CultureInfo.InvariantCulture)}"" fill=""{sC}"" />");
            }

            // Helper to add a line to the SVG string
            void AddLine(float x1, float y1, float x2, float y2, float strokeWidth, FastReport.BorderLine line)
            {
                string color = ConvertColor(line.Color);
                string dash = GetDashArray(line.Style, strokeWidth);

                // Use stroke-linecap="butt" so lines don't protrude past the corners
                svg.Append($@"<line x1=""{x1.ToString(CultureInfo.InvariantCulture)}"" y1=""{y1.ToString(CultureInfo.InvariantCulture)}"" 
                           x2=""{x2.ToString(CultureInfo.InvariantCulture)}"" y2=""{y2.ToString(CultureInfo.InvariantCulture)}"" 
                           stroke=""{color}"" stroke-width=""{strokeWidth.ToString(CultureInfo.InvariantCulture)}"" 
                           stroke-dasharray=""{dash}"" stroke-linecap=""butt"" />");
            }

            // 2. Draw lines with INSET math (Width / 2f)

            if (border.Lines.HasFlag(FastReport.BorderLines.Top))
            {
                float sw = ToPoints(border.TopLine.Width);
                float inset = sw / 2f;
                // Shift Y down by half stroke
                AddLine(0, inset, width, inset, sw, border.TopLine);
            }

            if (border.Lines.HasFlag(FastReport.BorderLines.Bottom))
            {
                float sw = ToPoints(border.BottomLine.Width);
                float inset = sw / 2f;
                // Shift Y up by half stroke
                AddLine(0, height - inset, width, height - inset, sw, border.BottomLine);
            }

            if (border.Lines.HasFlag(FastReport.BorderLines.Left))
            {
                float sw = ToPoints(border.LeftLine.Width);
                float inset = sw / 2f;
                // Shift X right by half stroke
                AddLine(inset, 0, inset, height, sw, border.LeftLine);
            }

            if (border.Lines.HasFlag(FastReport.BorderLines.Right))
            {
                float sw = ToPoints(border.RightLine.Width);
                float inset = sw / 2f;
                // Shift X left by half stroke
                AddLine(width - inset, 0, width - inset, height, sw, border.RightLine);
            }

            svg.Append("</svg>");

            container.Svg(svg.ToString());
        }

        public static string GetDashArray(LineStyle style, float width)
        {
            string F(float val) => val.ToString("0.###", CultureInfo.InvariantCulture);

            // Dash patterns relative to stroke width usually look best
            return style switch
            {
                LineStyle.Solid => "",
                // "Dash" -> 5px line, 3px gap (scaled by width)
                LineStyle.Dash => $"{F(width * 4)},{F(width * 2)}",
                // "Dot" -> 1px line, 2px gap
                LineStyle.Dot => $"{F(width)},{F(width * 2)}",
                // "DashDot" -> Dash, Gap, Dot, Gap
                LineStyle.DashDot => $"{F(width * 4)},{F(width * 2)},{F(width)},{F(width * 2)}",
                // "Double" is not natively supported by SVG stroke, usually treated as Solid or two rects.
                LineStyle.Double => "",
                _ => ""
            };
        }

        public static bool IsLatin(char c)
        {
            return
                (c >= 'A' && c <= 'Z') ||
                (c >= 'a' && c <= 'z');
        }
    }
}
