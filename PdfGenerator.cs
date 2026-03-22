using FastReport;
using FastReport.Web;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using SkiaSharp;
using System.Drawing;
using System.Globalization;
using System.Net.NetworkInformation;
using System.Text;
using ZXing;
using ZXing.Common;

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
                            if (obj is FastReport.BandBase band)
                            {
                                inner.Layer().ScaleToFit().Element(layerContainer =>
                                {
                                    DrawBand(layerContainer, page, band);
                                });
                            }
                            else if (obj is FastReport.TextObject txt && obj is not FastReport.Table.TableCell)
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
                            else if (obj is FastReport.ShapeObject shape)
                            {
                                inner.Layer().ScaleToFit().Element(layerContainer =>
                                {
                                    DrawShape(layerContainer, page, shape);
                                });
                            }
                            else if (obj is FastReport.Barcode.BarcodeObject barcode)
                            {
                                inner.Layer().ScaleToFit().Element(layerContainer =>
                                {
                                    DrawBarcode(layerContainer, page, barcode);
                                });
                            }
                            else if (obj is FastReport.PolygonObject polygon)
                            {
                                inner.Layer().ScaleToFit().Element(layerContainer =>
                                {
                                    DrawPolygon(layerContainer, page, polygon);
                                });
                            }
                            else if (obj is FastReport.PolyLineObject polyLine)
                            {
                                inner.Layer().ScaleToFit().Element(layerContainer =>
                                {
                                    DrawPolyLine(layerContainer, page, polyLine);
                                });
                            }
                            else if (obj is FastReport.Table.TableObject table)
                            {
                                inner.Layer().ScaleToFit().Element(layerContainer =>
                                {
                                    Table.DrawTable(layerContainer, page, table);
                                });
                            }
                        }

                        // REQUIRED: exactly one PrimaryLayer inside inner Layers
                        inner.PrimaryLayer().Element(_ => { });
                    });
                });
            });
        }

        #region Draw Text
        private void DrawText(IContainer container, ReportPage page, TextObject txt)
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
        #endregion

        #region Draw Picture
        private void DrawPicture(IContainer container, ReportPage page, PictureObject pic)
        {
            var c = container
                 .TranslateX(Helpers.ToPoints(pic.AbsLeft))
                 .TranslateY(Helpers.ToPoints(pic.AbsTop))
                 .Width(Helpers.ToPoints(pic.Width))
                 .Height(Helpers.ToPoints(pic.Height));


            var border = pic.Border;
            c = c.Background(Helpers.ConvertColor(pic.FillColor));

            if (border.Lines.HasFlag(BorderLines.Left))
                c = c.BorderLeft(Helpers.ToPoints(border.LeftLine.Width));

            if (border.Lines.HasFlag(BorderLines.Right))
                c = c.BorderRight(Helpers.ToPoints(border.RightLine.Width));

            if (border.Lines.HasFlag(BorderLines.Top))
                c = c.BorderTop(Helpers.ToPoints(border.TopLine.Width));

            if (border.Lines.HasFlag(BorderLines.Bottom))
                c = c.BorderBottom(Helpers.ToPoints(border.BottomLine.Width));

            c = c.BorderColor(Helpers.ConvertColor(border.Color));

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
        #endregion

        #region Draw Line
        private void DrawLine(IContainer container, ReportPage page, LineObject lineObject)
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


        #endregion

        #region Draw Shape
        private void DrawShape(IContainer container, ReportPage page, ShapeObject shapeObject)
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
        #endregion

        #region Draw Barcode
        private void DrawBarcode(IContainer container, FastReport.ReportPage page, FastReport.Barcode.BarcodeObject barcodeObject)
        {
            float widthPts = Helpers.ToPoints(barcodeObject.Width);
            float heightPts = Helpers.ToPoints(barcodeObject.Height);

            // 1. Map FastReport Barcode Type to ZXing BarcodeFormat
            BarcodeFormat format = MapBarcodeFormat(barcodeObject.Barcode.Name);
            bool is2D = format == BarcodeFormat.QR_CODE ||
                        format == BarcodeFormat.DATA_MATRIX ||
                        format == BarcodeFormat.AZTEC ||
                        format == BarcodeFormat.PDF_417;

            // 2. Configure Encoder Options
            // CRUCIAL: Setting Width and Height to 0 forces ZXing to output the RAW, unscaled matrix.
            // This keeps our SVG incredibly small and allows QuestPDF to handle the scaling flawlessly.
            var options = new EncodingOptions
            {
                Width = 0,
                Height = 0,
                Margin = 0,
                PureBarcode = true
            };

            var writer = new ZXing.BarcodeWriterGeneric
            {
                Format = format,
                Options = options
            };

            BitMatrix matrix = writer.Encode(barcodeObject.Text);
            string svgContent = GenerateBarcodeSvg(matrix, is2D);

            // THE FIX: Explicitly calculate the height required for the text
            float textHeight = 0;
            if (barcodeObject.ShowText)
            {
                // Font size is in points. Multiply by 1.2 to give standard line-height breathing room
                textHeight = 12;
            }

            // The SVG gets whatever height is left over. Math.Max prevents layout crashes if drawn too small
            float svgHeight = Math.Max(0, heightPts - textHeight);

            // 5. Render in QuestPDF
            container
                .TranslateX(Helpers.ToPoints(barcodeObject.AbsLeft))
                .TranslateY(Helpers.ToPoints(barcodeObject.AbsTop))
                .Rotate(barcodeObject.Angle)
                .Width(widthPts)
                .Height(heightPts)
                .Layers(layer =>
                {
                    // The SVG Barcode takes up all available vertical space
                    layer.Layer().Svg(svgContent);

                    // If ShowText is true, append the text at the bottom
                    if (barcodeObject.ShowText)
                    {
                        layer.Layer()
                            .AlignBottom() // Standard barcode text is centered
                            .Text(barcodeObject.Text).LineHeight(1f);

                    }
                    layer.PrimaryLayer().Element(_ => { });
                });

            // Note: If the barcode has "ShowText = true", you should draw the text 
            // using standard QuestPDF .Text() capabilities positioned just below this container.
        }
        #endregion

        #region Draw Rectanble
        private string GenerateSvgRect(float width, float height, float strokeWidth,
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
        private string GenerateSvgEllipse(float width, float height, float strokeWidth,
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
        private string GenerateSvgTriangle(float width, float height, float strokeWidth,
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
        private string GenerateSvgDiamond(float width, float height, float strokeWidth,
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

        #region Draw Polyline
        private void DrawPolyLine(IContainer container, ReportPage page, FastReport.PolyLineObject polyLineObject)
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
        #endregion

        #region Draw Polygon

        private void DrawPolygon(IContainer container, ReportPage page, FastReport.PolygonObject polygonObject)
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
        #endregion

        #region Draw Band
        private void DrawBand(IContainer container, ReportPage page, FastReport.BandBase band)
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
        #endregion

        #region Helpers

        private string GenerateBarcodeSvg(BitMatrix matrix, bool is2D)
        {
            int width = matrix.Width;
            int height = matrix.Height;

            var sb = new StringBuilder();

            // 1D needs to stretch to fill the width/height entirely. 
            // 2D needs to keep its square aspect ratio (xMidYMid meet is the SVG default when omitted).
            string aspectAttr = is2D ? "" : "preserveAspectRatio=\"none\"";

            // THE CRITICAL FIX: width="100%" height="100%" 
            // This commands the SVG to abandon its native 100x1 matrix size and fully map 
            // itself to the widthPts and svgHeight provided by the QuestPDF layout container.
            sb.AppendLine($"<svg width=\"100%\" height=\"100%\" viewBox=\"0 0 {width} {height}\" {aspectAttr} xmlns=\"http://www.w3.org/2000/svg\">");

            sb.Append("<path d=\"");

            for (int y = 0; y < height; y++)
            {
                int startX = -1;
                for (int x = 0; x < width; x++)
                {
                    if (matrix[x, y])
                    {
                        if (startX == -1) startX = x;
                    }
                    else
                    {
                        if (startX != -1)
                        {
                            int rectWidth = x - startX;
                            sb.Append($"M {startX} {y} h {rectWidth} v 1 h -{rectWidth} Z ");
                            startX = -1;
                        }
                    }
                }
                if (startX != -1)
                {
                    int rectWidth = width - startX;
                    sb.Append($"M {startX} {y} h {rectWidth} v 1 h -{rectWidth} Z ");
                }
            }

            sb.AppendLine("\" fill=\"black\" />");
            sb.AppendLine("</svg>");

            return sb.ToString();
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

        private BarcodeFormat MapBarcodeFormat(string fastReportBarcodeName)
        {
            // Map FastReport's string identifier to ZXing's Enums
            return fastReportBarcodeName.Replace(" ", "").ToUpper() switch
            {
                "QRCODE" => BarcodeFormat.QR_CODE,
                "CODE128" => BarcodeFormat.CODE_128,
                "CODE39" => BarcodeFormat.CODE_39,
                "EAN13" => BarcodeFormat.EAN_13,
                "EAN8" => BarcodeFormat.EAN_8,
                "UPCA" => BarcodeFormat.UPC_A,
                "DATAMATRIX" => BarcodeFormat.DATA_MATRIX,
                "PDF417" => BarcodeFormat.PDF_417,
                _ => throw new NotSupportedException("Barcode format not supported!") // Fallback
            };
        }



        #endregion

        #endregion
    }
}
