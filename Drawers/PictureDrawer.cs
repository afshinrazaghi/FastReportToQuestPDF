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
    public class PictureDrawer
    {
        public static void Draw(IContainer container, ReportPage page, PictureObject pic)
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
    }
}
