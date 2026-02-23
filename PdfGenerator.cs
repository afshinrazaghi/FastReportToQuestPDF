using FastReport;
using FastReport.Web;
using QuestPDF;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
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
            // ۱. محاسبه طول واقعی (وتر)
            float actualLength = MathF.Sqrt(MathF.Pow(lineObject.Width, 2) + MathF.Pow(lineObject.Height, 2));

            // ۲. محاسبه زاویه
            float angleDegrees = MathF.Atan2(lineObject.Height, lineObject.Width) * (180 / MathF.PI);

            // اگر خط Diagonal باشد (در فست‌ریپورت یعنی خط از پایین-چپ به بالا-راست)
            //if (lineObject.Diagonal)
            //{
            //    angleDegrees = -angleDegrees;
            //}

            container
                .TranslateX(ToPoints(lineObject.AbsLeft))
                .TranslateY(ToPoints(lineObject.AbsTop))
                // ما یک باکس با ارتفاع بسیار کم (ضخامت خط) و عرضی معادل طول کل خط می‌سازیم
                .Width(ToPoints(actualLength))
                .Height(ToPoints(lineObject.Border.Width))
                .Rotate(angleDegrees)
                .Background(ConvertColor(lineObject.Border.Color));
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


        #endregion
    }
}
