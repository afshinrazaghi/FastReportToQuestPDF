using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZXing;
using ZXing.Common;

namespace FastReportToQuestPDF.Drawers
{
    public class BarcodeDrawer
    {
        public static void Draw(IContainer container, FastReport.ReportPage page, FastReport.Barcode.BarcodeObject barcodeObject)
        {
            float widthPts = Helpers.ToPoints(barcodeObject.Width);
            float heightPts = Helpers.ToPoints(barcodeObject.Height);

            // 1. Map FastReport Barcode Type to ZXing BarcodeFormat
            //BarcodeFormat format = MapBarcodeFormat(barcodeObject.Barcode.Name);
            //bool is2D = format == BarcodeFormat.QR_CODE ||
            //            format == BarcodeFormat.DATA_MATRIX ||
            //            format == BarcodeFormat.AZTEC ||
            //            format == BarcodeFormat.PDF_417;

            // 2. Configure Encoder Options
            // CRUCIAL: Setting Width and Height to 0 forces ZXing to output the RAW, unscaled matrix.
            // This keeps our SVG incredibly small and allows QuestPDF to handle the scaling flawlessly.
            //var options = new EncodingOptions
            //{
            //    Width = 0,
            //    Height = 0,
            //    Margin = 0,
            //    PureBarcode = true
            //};

            //var writer = new ZXing.BarcodeWriterGeneric
            //{
            //    Format = format,
            //    Options = options
            //};

            //BitMatrix matrix = writer.Encode(barcodeObject.Text);
            //string svgContent = GenerateBarcodeSvg(matrix, is2D);

            // THE FIX: Explicitly calculate the height required for the text
            //float textHeight = 0;
            //if (barcodeObject.ShowText)
            //{
            //    // Font size is in points. Multiply by 1.2 to give standard line-height breathing room
            //    textHeight = 12;
            //}

            // The SVG gets whatever height is left over. Math.Max prevents layout crashes if drawn too small
            //float svgHeight = Math.Max(0, heightPts - textHeight);

            // 5. Render in QuestPDF
            var image = Helpers.RenderFastReportObjectToImage(barcodeObject);


            container
                .TranslateX(Helpers.ToPoints(barcodeObject.AbsLeft))
                .TranslateY(Helpers.ToPoints(barcodeObject.AbsTop))
                .Rotate(barcodeObject.Angle)
                .Width(widthPts)
                .Height(heightPts)
                .Image(image);
                //.Layers(layer =>
                //{
                //    // The SVG Barcode takes up all available vertical space
                //    layer.Layer().Svg(svgContent);

                //    // If ShowText is true, append the text at the bottom
                //    if (barcodeObject.ShowText)
                //    {
                //        layer.Layer()
                //            .AlignBottom() // Standard barcode text is centered
                //            .Text(barcodeObject.Text).LineHeight(1f);

                //    }
                //    layer.PrimaryLayer().Element(_ => { });
                //});

            // Note: If the barcode has "ShowText = true", you should draw the text 
            // using standard QuestPDF .Text() capabilities positioned just below this container.
        }


        private static BarcodeFormat MapBarcodeFormat(string fastReportBarcodeName)
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

        private static string GenerateBarcodeSvg(BitMatrix matrix, bool is2D)
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
    }
}
