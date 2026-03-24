using FastReport;
using FastReport.Matrix;
using FastReport.Table;
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
    public class MatrixDrawer
    {
        public static void Draw(IContainer container, FastReport.ReportPage page, MatrixObject matrix)
        {
            // 1. Position the entire Matrix on the page
            var tableContainer = container
                .TranslateX(Helpers.ToPoints(matrix.AbsLeft))
                .TranslateY(Helpers.ToPoints(matrix.AbsTop))
                .MinWidth(Helpers.ToPoints(matrix.Width))
                .MaxWidth(Helpers.ToPoints(matrix.Width));

            // 2. Use QuestPDF Table
            tableContainer.Table(table =>
            {
                // Define Columns based on Matrix Column widths
                table.ColumnsDefinition(columns =>
                {
                    for (int i = 0; i < matrix.ColumnCount; i++)
                    {
                        // Use your Helpers.ToPoints factor (0.7499...)
                        columns.ConstantColumn(Helpers.ToPoints(matrix.Columns[i].Width));
                    }
                });

                // Track occupied cells for Span handling (QuestPDF throws error if cells overlap)
                bool[,] occupied = new bool[matrix.RowCount, matrix.ColumnCount];

                for (int r = 0; r < matrix.RowCount; r++)
                {
                    for (int c = 0; c < matrix.ColumnCount; c++)
                    {
                        // Skip if this space is occupied by a RowSpan or ColSpan from a previous cell
                        if (occupied[r, c]) continue;

                        var frCell = matrix[c, r]; // FastReport uses (Col, Row)

                        int rowSpan = frCell.RowSpan;
                        int colSpan = frCell.ColSpan;

                        // Mark these coordinates as occupied
                        for (int rs = 0; rs < rowSpan; rs++)
                        {
                            for (int cs = 0; cs < colSpan; cs++)
                            {
                                if (r + rs < matrix.RowCount && c + cs < matrix.ColumnCount)
                                    occupied[r + rs, c + cs] = true;
                            }
                        }

                        // Create the QuestPDF cell
                        var cell = table.Cell().Row((uint)(r + 1)).Column((uint)(c + 1));

                        if (rowSpan > 1) cell.RowSpan((uint)rowSpan);
                        if (colSpan > 1) cell.ColumnSpan((uint)colSpan);

                        // Render the cell content
                        cell.Element(e => DrawTableCell(e, frCell));
                    }
                }
            });
        }

        private static void DrawTableCell(IContainer container, TableCell cell)
        {
            float w = Helpers.ToPoints(cell.Width);
            float h = Helpers.ToPoints(cell.Height);

            // Use Layers so we can draw the background, text, and then the SVG border on top
            container
                .Height(h) // Force row height
                .Background(Helpers.ConvertColor(cell.FillColor))
                .Layers(layers =>
                {
                    // Layer 1: The Text content
                    layers.PrimaryLayer()
                        .PaddingLeft(Helpers.ToPoints(cell.Padding.Left))
                        .PaddingRight(Helpers.ToPoints(cell.Padding.Right))
                        .PaddingTop(Helpers.ToPoints(cell.Padding.Top))
                        .PaddingBottom(Helpers.ToPoints(cell.Padding.Bottom))
                        .Element(e =>
                        {
                            // Vertical Alignment
                            var aligned = cell.VertAlign switch
                            {
                                VertAlign.Center => e.AlignMiddle(),
                                VertAlign.Bottom => e.AlignBottom(),
                                _ => e.AlignTop()
                            };
                            return aligned;
                        })
                        .Text(text =>
                        {
                            // Horizontal Alignment
                            switch (cell.HorzAlign)
                            {
                                case HorzAlign.Center: text.AlignCenter(); break;
                                case HorzAlign.Right: text.AlignRight(); break;
                                case HorzAlign.Justify: text.Justify(); break;
                                default: text.AlignLeft(); break;
                            }

                            var span = text.Span(cell.Text)
                                .FontFamily(cell.Font.Name)
                                .FontSize(Helpers.ToPoints(cell.Font.Size))
                                .FontColor(Helpers.ConvertColor(cell.TextColor));

                            if (cell.Font.Style.HasFlag(FontStyle.Bold)) span.Bold();
                            if (cell.Font.Style.HasFlag(FontStyle.Italic)) span.Italic();
                        });

                    // Layer 2: The Border (using our SVG DrawComplexBorder)
                    if (cell.Border.Lines != FastReport.BorderLines.None || cell.Border.Shadow)
                    {
                        layers.Layer().Element(e => Helpers.DrawComplexBorder(e, cell.Border, w, h));
                    }
                });
        }
    }
}
