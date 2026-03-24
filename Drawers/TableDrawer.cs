using FastReport;
using FastReport.Table;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FastReportToQuestPDF.Drawers
{
    public class TableDrawer
    {
        public static void Draw(IContainer container, ReportPage page, TableObject frTable)
        {
            if (frTable == null || frTable.RowCount == 0 || frTable.ColumnCount == 0) return;

            float widthPts = Helpers.ToPoints(frTable.Width);
            float heightPts = Helpers.ToPoints(frTable.Height);
            // Position and size the container holding the table
            container
                .TranslateX(Helpers.ToPoints(frTable.AbsLeft))
                .TranslateY(Helpers.ToPoints(frTable.AbsTop))
                .Width(widthPts)
                .Height(heightPts)
                .Layers(layers =>
                {
                    // LAYER 1 (Bottom): Draw the Table Grid and Cells
                    layers.PrimaryLayer().Table(table =>
                    {
                        // 1. DEFINE COLUMNS
                        table.ColumnsDefinition(columns =>
                        {
                            for (int c = 0; c < frTable.ColumnCount; c++)
                            {
                                float colWidth = Helpers.ToPoints(frTable.Columns[c].Width);
                                columns.ConstantColumn(colWidth);
                            }
                        });

                        // 2. TRACK MERGED CELLS
                        bool[,] coveredCells = new bool[frTable.RowCount, frTable.ColumnCount];

                        // 3. ITERATE GRID
                        for (int r = 0; r < frTable.RowCount; r++)
                        {
                            for (int c = 0; c < frTable.ColumnCount; c++)
                            {
                                if (coveredCells[r, c]) continue;

                                TableCell frCell = frTable[c, r];
                                if (frCell == null) continue;

                                uint colSpan = (uint)Math.Max(1, frCell.ColSpan);
                                uint rowSpan = (uint)Math.Max(1, frCell.RowSpan);

                                // Mark merged slots as covered
                                for (int i = 0; i < rowSpan; i++)
                                {
                                    for (int j = 0; j < colSpan; j++)
                                    {
                                        if (r + i < frTable.RowCount && c + j < frTable.ColumnCount)
                                        {
                                            coveredCells[r + i, c + j] = true;
                                        }
                                    }
                                }

                                // 4. DEFINE THE QUESTPDF CELL
                                var qCell = table.Cell()
                                                 .Row((uint)(r + 1))
                                                 .Column((uint)(c + 1))
                                                 .RowSpan(rowSpan)
                                                 .ColumnSpan(colSpan)
                                                 .Height(Helpers.ToPoints(frCell.Height));

                                // 5. RENDER CELL (Using the RenderTableCell method from the previous step)
                                RenderTableCell(qCell, frCell);
                            }
                        }
                    });

                    // LAYER 2 (Top): Draw the Outer Border of the TableObject
                    // We draw this AFTER the PrimaryLayer so the outer border sits on top of cell backgrounds
                    layers.Layer().Element(e => Helpers.DrawComplexBorder(e, frTable.Border, widthPts, heightPts)

                    );
                });
        }

        private static void RenderTableCell(IContainer cellContainer, TableCell frCell)
        {
            // 1. Apply Background Fill
            if (frCell.Fill is FastReport.SolidFill solidFill && solidFill.Color.A > 0)
            {
                cellContainer = cellContainer.Background(Helpers.ConvertColor(solidFill.Color));
            }

            // 2. Apply Borders
            // For tables, QuestPDF's native solid borders usually look best and prevent pixel gaps.
            cellContainer = ApplyCellBorders(cellContainer, frCell.Border);

            // 3. Apply Padding
            cellContainer = cellContainer.PaddingLeft(Helpers.ToPoints(frCell.Padding.Left))
                                         .PaddingRight(Helpers.ToPoints(frCell.Padding.Right))
                                         .PaddingTop(Helpers.ToPoints(frCell.Padding.Top))
                                         .PaddingBottom(Helpers.ToPoints(frCell.Padding.Bottom));

            // 4. Apply Alignments
            cellContainer = ApplyCellAlignment(cellContainer, frCell);

            // 5. Render Text Content
            // We use TextBlock to handle Fonts, Colors, and LineHeights precisely
            var text = cellContainer.Text(frCell.Text)
                         .FontFamily(frCell.Font.Name)
                         .FontSize(Helpers.ToPoints(frCell.Font.Size))
                         .FontColor(Helpers.ConvertColor(frCell.TextColor))
                         // FastReport usually assumes standard line height, but we can enforce it
                         .LineHeight(1.2f);
            // Map Bold/Italic
            if (frCell.Font.Bold)
                text.Bold();
            if (frCell.Font.Italic)
                text.Italic();
        }

        // --- Helpers ---

        private static IContainer ApplyCellBorders(IContainer container, FastReport.Border border)
        {
            if (border == null || border.Lines == FastReport.BorderLines.None) return container;

            // We use native QuestPDF borders for tables to ensure perfect contiguous grid lines.
            // Note: If you absolutely need dashed borders inside cells, you could call 
            // the DrawComplexBorder SVG method we created earlier here instead.

            if (border.Lines.HasFlag(FastReport.BorderLines.Top))
                container = container.BorderTop(Helpers.ToPoints(border.TopLine.Width)).BorderColor(Helpers.ConvertColor(border.TopLine.Color));

            if (border.Lines.HasFlag(FastReport.BorderLines.Bottom))
                container = container.BorderBottom(Helpers.ToPoints(border.BottomLine.Width)).BorderColor(Helpers.ConvertColor(border.BottomLine.Color));

            if (border.Lines.HasFlag(FastReport.BorderLines.Left))
                container = container.BorderLeft(Helpers.ToPoints(border.LeftLine.Width)).BorderColor(Helpers.ConvertColor(border.LeftLine.Color));

            if (border.Lines.HasFlag(FastReport.BorderLines.Right))
                container = container.BorderRight(Helpers.ToPoints(border.RightLine.Width)).BorderColor(Helpers.ConvertColor(border.RightLine.Color));

            return container;
        }

        private static IContainer ApplyCellAlignment(IContainer container, TableCell frCell)
        {
            // Horizontal Alignment Mapping
            container = frCell.HorzAlign switch
            {
                FastReport.HorzAlign.Left => container.AlignLeft(),
                FastReport.HorzAlign.Center => container.AlignCenter(),
                FastReport.HorzAlign.Right => container.AlignRight(),
                FastReport.HorzAlign.Justify => container.AlignLeft(), // QuestPDF handles justify on the Text element, but Left is a safe container fallback
                _ => container.AlignLeft()
            };

            // Vertical Alignment Mapping
            container = frCell.VertAlign switch
            {
                FastReport.VertAlign.Top => container.AlignTop(),
                FastReport.VertAlign.Center => container.AlignMiddle(),
                FastReport.VertAlign.Bottom => container.AlignBottom(),
                _ => container.AlignTop()
            };

            return container;
        }

       
    }
}
