using FastReport;
using FastReport.Web;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using SkiaSharp;
using System.Text;

namespace FastReportToQuestPDF
{
    public class PdfGenerator : IPdfGenerator
    {
        private string? _unicodeFontName;
        private string? _emojiFontName;
        public PdfGenerator()
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }
        public byte[] GeneratePDF(WebReport report)
        {
            return GeneratePDF(report, null);
        }

        public byte[] GeneratePDF(WebReport report, string? unicodeFontName)
        {
            return GeneratePDF(report, null, null);
        }

        public byte[] GeneratePDF(WebReport report, string? unicodeFontName, string? emojiFontName)
        {
            _unicodeFontName = unicodeFontName;
            _emojiFontName = emojiFontName;
            int pagesCount = report.Report.PreparedPages.Count;
            var doc = Document.Create(container =>
            {
                for (int i = 0; i < pagesCount; i++)
                {
                    var reportPage = report.Report.PreparedPages.GetPage(i);

                    container.Page(questPage =>
                    {
                        BuildQuestPage(questPage, reportPage);
                    });
                }
            });

            var generatedPdf = doc.GeneratePdf();
            return generatedPdf;
        }

        public void RegisterFont(string fontPath)
        {
            QuestPDF.Drawing.FontManager.RegisterFont(File.OpenRead(fontPath));
        }

        public void RegisterCustomFont(string fontName, string fontPath)
        {
            QuestPDF.Drawing.FontManager.RegisterFontWithCustomName(fontName, File.OpenRead(fontPath));
        }

        #region Private functions
        private void BuildQuestPage(PageDescriptor questPage, ReportPage page)
        {
            questPage.Size(page.PaperWidth / 10f, page.PaperHeight / 10f, Unit.Centimetre);
            questPage.MarginLeft(page.LeftMargin / 10, Unit.Centimetre);
            questPage.MarginTop(page.TopMargin / 10, Unit.Centimetre);
            questPage.MarginRight(page.RightMargin / 10, Unit.Centimetre);
            questPage.MarginBottom(page.BottomMargin / 10, Unit.Centimetre);
            questPage.Content().Layers(layers =>
            {
                // Required: exactly one primary layer
                layers.PrimaryLayer().Element(container =>
                {
                    container.Layers(inner =>
                    {
                        foreach (var obj in page.AllObjects)
                        {
                            if (obj is FastReport.TextObject txt)
                            {
                                inner.Layer().ScaleToFit().Element(layerContainer =>
                                {
                                    DrawText(layerContainer, page, txt);
                                });
                            }
                            else if (obj is FastReport.PictureObject pic)
                            {
                                inner.Layer().ScaleToFit().Element(layerContainer =>
                                {
                                    DrawPicture(layerContainer, page, pic);
                                });
                            }
                            else if (obj is FastReport.LineObject line)
                            {
                                inner.Layer().ScaleToFit().Element(layerContainer =>
                                {
                                    DrawLine(layerContainer, page, line);
                                });
                            }
                        }

                        // REQUIRED: exactly one PrimaryLayer inside inner Layers
                        inner.PrimaryLayer().Element(_ => { });
                    });
                });
            });
        }

        private float ToPoints(float value)
        {
            return value * 0.75f;
        }

        private void DrawText(IContainer container, ReportPage page, TextObject txt)
        {

            var c = container
                 .TranslateX(ToPoints(txt.AbsLeft))
                 .TranslateY(ToPoints(txt.AbsTop))
                 .Width(ToPoints(txt.Width))
                 .Height(ToPoints(txt.Height));

            var border = txt.Border;
            c = c.Background(ConvertColor(txt.FillColor));

            if (border.Lines.HasFlag(BorderLines.Left))
                c = c.BorderLeft(ToPoints(border.LeftLine.Width));

            if (border.Lines.HasFlag(BorderLines.Right))
                c = c.BorderRight(ToPoints(border.RightLine.Width));

            if (border.Lines.HasFlag(BorderLines.Top))
                c = c.BorderTop(ToPoints(border.TopLine.Width));

            if (border.Lines.HasFlag(BorderLines.Bottom))
                c = c.BorderBottom(ToPoints(border.BottomLine.Width));

            c = c.BorderColor(ConvertColor(border.Color));

            var text = c.Text(txt.Text);
            text = text.FontFamily(string.IsNullOrEmpty(_unicodeFontName) ? txt.Font.Name : (!txt.Text.Any(c => IsLatin(c)) ? txt.Font.Name : _unicodeFontName));

            text = text.LineHeight(ToPoints(txt.Height) / txt.Font.Size);

            switch (txt.HorzAlign)
            {
                case HorzAlign.Center: text = text.AlignCenter(); break;
                case HorzAlign.Right: text = text.AlignStart(); break;
                case HorzAlign.Left: text = text.AlignEnd(); break;
            }

            text = text.FontSize(txt.Font.Size);

            text = text.FontColor(ConvertColor(txt.TextColor));

            if (txt.Font.Bold)
                text = text.Bold();

            if (txt.Font.Italic)
                text = text.Italic();
        }

        private void DrawPicture(IContainer container, ReportPage page, PictureObject pic)
        {
            var c = container
                 .TranslateX(ToPoints(pic.AbsLeft))
                 .TranslateY(ToPoints(pic.AbsTop))
                 .Width(ToPoints(pic.Width))
                 .Height(ToPoints(pic.Height));


            var border = pic.Border;
            c = c.Background(ConvertColor(pic.FillColor));

            if (border.Lines.HasFlag(BorderLines.Left))
                c = c.BorderLeft(ToPoints(border.LeftLine.Width));

            if (border.Lines.HasFlag(BorderLines.Right))
                c = c.BorderRight(ToPoints(border.RightLine.Width));

            if (border.Lines.HasFlag(BorderLines.Top))
                c = c.BorderTop(ToPoints(border.TopLine.Width));

            if (border.Lines.HasFlag(BorderLines.Bottom))
                c = c.BorderBottom(ToPoints(border.BottomLine.Width));

            c = c.BorderColor(ConvertColor(border.Color));

            // اگر هنوز Image لود نشده
            if (pic.Image == null)
            {
                pic.ForceLoadImage();
            }

            if (pic.Image == null)
            {
                // If there is no image (e.g., empty data), just return an empty container
                return;
            }

            byte[] imageBytes;
            using (var ms = new MemoryStream())
            {
                // Saving as PNG ensures that image transparency (alpha channel) is preserved
                pic.Image.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                imageBytes = ms.ToArray();
            }

            var imageDescriptor = c.Image(imageBytes);

            switch (pic.SizeMode)
            {
                case System.Windows.Forms.PictureBoxSizeMode.Zoom:
                    // Scales the image to fit inside the Width/Height while preserving aspect ratio
                    imageDescriptor.FitArea();
                    break;

                case System.Windows.Forms.PictureBoxSizeMode.CenterImage:
                    // Centers the image without scaling it
                    container.AlignCenter().AlignMiddle();
                    break;

                case System.Windows.Forms.PictureBoxSizeMode.StretchImage:
                    // Note: QuestPDF strictly preserves aspect ratios by default. 
                    // FitArea is the closest safe fallback for Stretch in standard QuestPDF layouts.
                    imageDescriptor.FitArea();
                    break;

                default:
                    imageDescriptor.FitArea();
                    break;
            }

        }

        private void DrawLine(IContainer container, ReportPage page, LineObject lineObject)
        {
            // 1. Precise Conversion to Points
            float widthPts = ToPoints(lineObject.Width);
            float heightPts = ToPoints(lineObject.Height);
            float strokeWidth = ToPoints(lineObject.Border.Width);

            // 2. Calculate Hypotenuse (Actual length of the line)
            float actualLength = MathF.Sqrt(MathF.Pow(widthPts, 2) + MathF.Pow(heightPts, 2));

            // 3. Calculate Angle
            float angleDegrees = MathF.Atan2(heightPts, widthPts) * (180 / MathF.PI);

            // 4. Determine Start Coordinates based on FastReport logic
            float startX = ToPoints(lineObject.AbsLeft);
            float startY = ToPoints(lineObject.AbsTop);

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

            string hexColor = ConvertColor(lineObject.Border.Color);

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

        private string GenerateSvgLine(float width, float height, float strokeWidth, string color,
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


        private QuestPDF.Infrastructure.Color ConvertColor(System.Drawing.Color c)
        {
            return QuestPDF.Infrastructure.Color.FromARGB(c.A, c.R, c.G, c.B);
        }

        private bool IsLatin(char c)
        {
            return
                (c >= 'A' && c <= 'Z') ||
                (c >= 'a' && c <= 'z');
        }

        private IEnumerable<(string Text, bool IsLatin)> SplitByScript(string input)
        {
            if (string.IsNullOrEmpty(input))
                yield break;

            var sb = new StringBuilder();
            bool currentIsLatin = IsLatin(input[0]);

            foreach (var ch in input)
            {
                bool isLatin = IsLatin(ch);

                if (isLatin != currentIsLatin)
                {
                    yield return (sb.ToString(), currentIsLatin);
                    sb.Clear();
                    currentIsLatin = isLatin;
                }

                sb.Append(ch);
            }

            yield return (sb.ToString(), currentIsLatin);
        }


        private void DrawArrowHead(SKCanvas canvas, float x1, float y1, float x2, float y2, SKPaint paint)
        {
            float arrowLength = 10f;          // طول بال فلش
            float arrowAngle = MathF.PI / 6;  // 30 درجه

            float angle = MathF.Atan2(y2 - y1, x2 - x1);

            float x3 = x2 - arrowLength * MathF.Cos(angle - arrowAngle);
            float y3 = y2 - arrowLength * MathF.Sin(angle - arrowAngle);

            float x4 = x2 - arrowLength * MathF.Cos(angle + arrowAngle);
            float y4 = y2 - arrowLength * MathF.Sin(angle + arrowAngle);

            canvas.DrawLine(x2, y2, x3, y3, paint);
            canvas.DrawLine(x2, y2, x4, y4, paint);
        }

        private void DrawFilledArrowHead(SKCanvas canvas, float x1, float y1, float x2, float y2, float size, SKColor color)
        {
            float angle = MathF.Atan2(y2 - y1, x2 - x1);

            float x3 = x2 - size * MathF.Cos(angle - MathF.PI / 6);
            float y3 = y2 - size * MathF.Sin(angle - MathF.PI / 6);

            float x4 = x2 - size * MathF.Cos(angle + MathF.PI / 6);
            float y4 = y2 - size * MathF.Sin(angle + MathF.PI / 6);

            using var path = new SKPath();
            path.MoveTo(x2, y2);
            path.LineTo(x3, y3);
            path.LineTo(x4, y4);
            path.Close();

            using var paint = new SKPaint
            {
                Color = color,
                Style = SKPaintStyle.Fill,
                IsAntialias = true
            };

            canvas.DrawPath(path, paint);
        }



        #endregion
    }
}
