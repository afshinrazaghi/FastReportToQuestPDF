using FastReport;
using FastReport.Web;
using FastReportToQuestPDF.Drawers;
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
                                    BandDrawer.Draw(layerContainer, page, band);
                                });
                            }
                            else if (obj is FastReport.TextObject txt && obj is not FastReport.Table.TableCell && obj is not FastReport.CellularTextObject)
                            {
                                inner.Layer().ScaleToFit().Element(layerContainer =>
                                {
                                    TextDrawer.Draw(layerContainer, page, txt);
                                });
                            }
                            else if (obj is FastReport.PictureObject pic)
                            {
                                inner.Layer().ScaleToFit().Element(layerContainer =>
                                {
                                    PictureDrawer.Draw(layerContainer, page, pic);
                                });
                            }
                            else if (obj is FastReport.LineObject line)
                            {
                                inner.Layer().ScaleToFit().Element(layerContainer =>
                                {
                                    LineDrawer.Draw(layerContainer, page, line);
                                });
                            }
                            else if (obj is FastReport.ShapeObject shape)
                            {
                                inner.Layer().ScaleToFit().Element(layerContainer =>
                                {
                                    ShapeDrawer.Draw(layerContainer, page, shape);
                                });
                            }
                            else if (obj is FastReport.Barcode.BarcodeObject barcode)
                            {
                                inner.Layer().ScaleToFit().Element(layerContainer =>
                                {
                                    BarcodeDrawer.Draw(layerContainer, page, barcode);
                                });
                            }
                            else if (obj is FastReport.PolygonObject polygon)
                            {
                                inner.Layer().ScaleToFit().Element(layerContainer =>
                                {
                                    PolygonDrawer.Draw(layerContainer, page, polygon);
                                });
                            }
                            else if (obj is FastReport.PolyLineObject polyLine)
                            {
                                inner.Layer().ScaleToFit().Element(layerContainer =>
                                {
                                    PolyLineDrawer.Draw(layerContainer, page, polyLine);
                                });
                            }
                            else if (obj is FastReport.Table.TableObject table)
                            {
                                inner.Layer().ScaleToFit().Element(layerContainer =>
                                {
                                    TableDrawer.Draw(layerContainer, page, table);
                                });
                            }
                            else if (obj is FastReport.Matrix.MatrixObject matrix)
                            {
                                inner.Layer().ScaleToFit().Element(layerContainer =>
                                {
                                    MatrixDrawer.Draw(layerContainer, page, matrix);
                                });
                            }
                            else if (obj is FastReport.ReportComponentBase component)
                            {
                                var image = Helpers.RenderFastReportObjectToImage(component);
                                inner.Layer().ScaleToFit().Element(layerContainer =>
                                {
                                    layerContainer
                                        .TranslateX(component.AbsLeft)
                                        .TranslateY(component.AbsTop)
                                        .Width(Helpers.ToPoints(component.Width))
                                        .Height(Helpers.ToPoints(component.Height))
                                        .Image(image);
                                });
                            }
                        }

                        // REQUIRED: exactly one PrimaryLayer inside inner Layers
                        inner.PrimaryLayer().Element(_ => { });
                    });
                });
            });
        }



        #endregion
    }
}
