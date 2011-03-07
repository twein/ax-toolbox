using System.IO;
using iTextSharp.text;
using iTextSharp.text.pdf;
using AXToolbox.Common;
using System;
using System.Globalization;
using System.Collections.Generic;

namespace AXToolbox.Tests
{
    public class PdfTest
    {
        public static float cm2pt = 72f / 2.54f;

        public static string loc = "Plaça dels Països Catalans, Girona";
        public static double lat = 41.950904;
        public static double lng = 3.225684;

        public static Font HeaderFont = new Font(Font.FontFamily.HELVETICA, 12, Font.BOLD);
        public static Font BoldFont = new Font(Font.FontFamily.HELVETICA, 7, Font.BOLD);
        public static Font NormalFont = new Font(Font.FontFamily.HELVETICA, 6.5f, Font.NORMAL);
        public static Font FooterFont = new Font(Font.FontFamily.HELVETICA, 8, Font.NORMAL);

        public PdfTest(string pdfFileName)
        {
            var document = new Document();
            //document.SetPageSize(PageSize.A4); //portrait
            document.SetPageSize(PageSize.A4.Rotate()); //landscape
            document.SetMargins(1f * cm2pt, 1f * cm2pt, 1.5f * cm2pt, 1.5f * cm2pt); // in pt

            PdfWriter.GetInstance(document, new FileStream(pdfFileName, FileMode.Create)).PageEvent = new PageEvents();
            document.Open();

            document.AddAuthor("AX-Toolbox Test program");
            document.AddTitle("Sun table for " + DateTime.Today.Year.ToString());
            document.AddSubject("Table with dawn and dusk times for " + DateTime.Today.Year.ToString() + ". It shows the start and end time for outdoor activities.");
            document.AddKeywords("AX-Toolbox, sun, dawn, dusk");

            //title
            //document.Add(new Paragraph("Sun table", HeaderFont) { Alignment = Element.ALIGN_CENTER });

            //paragraph
            //document.Add(new Paragraph(string.Format(NumberFormatInfo.InvariantInfo, "Location: {0} ({1:0.000000}, {2:0.000000})", loc, lat, lng), NormalFont)
            //    {
            //        Alignment = Element.ALIGN_CENTER
            //    });

            //page break
            //document.NewPage();

            //table
            var relWidths = new float[] { 8, 2, 2, 3 };
            var table = new PdfPTable(relWidths)
            {
                WidthPercentage = 100,
                SpacingBefore = 15,
                SpacingAfter = 10,
                HeaderRows = 1
            };
            table.DefaultCell.HorizontalAlignment = Element.ALIGN_LEFT;

            //table header
            table.AddCell(new PdfPCell(new Paragraph("Date", BoldFont))
            {
                HorizontalAlignment = Element.ALIGN_LEFT
            });
            table.AddCell(new PdfPCell(new Paragraph("Dawn", BoldFont)));
            table.AddCell(new PdfPCell(new Paragraph("Dusk", BoldFont)));
            table.AddCell(new PdfPCell(new Paragraph("Outdoor activity time", BoldFont)));

            //table body
            var cells = GetCells();
            foreach (var c in cells)
            {
                table.AddCell(new PdfPCell(new Paragraph(c, NormalFont)));
            }

            //normal layout
            //document.Add(table);

            //multicolumn layout
            MultiColumnText columns = new MultiColumnText();
            columns.AddRegularColumns(1 * cm2pt, document.PageSize.Width - 1 * cm2pt, 0.35f * cm2pt, 4); //(L-margin, R-margin, separation, # of cols) in pt
            columns.AddElement(table);
            document.Add(columns);


            document.Close();
        }

        private List<string> GetCells()
        {
            var cells = new List<string>();

            var sun = new Sun(lat, lng);

            var today = DateTime.Now;
            //var from = today - new TimeSpan(10, 0, 0, 0);
            //var to = today + new TimeSpan(30, 0, 0, 0);

            var from = new DateTime(today.Year, 1, 1);
            var to = new DateTime(today.Year, 12, 31);

            for (var date = from; date <= to; date += new TimeSpan(1, 0, 0, 0))
            {
                var sr = sun.Sunrise(date, Sun.ZenithTypes.Custom);
                var ss = sun.Sunset(date, Sun.ZenithTypes.Custom);
                var span = ss - sr;
                cells.Add(date.ToString(@"D", DateTimeFormatInfo.InvariantInfo));
                cells.Add(sr.ToString(@"HH:mm"));
                cells.Add(ss.ToString(@"HH:mm"));
                cells.Add(span.ToString());
            }

            return cells;
        }

        internal class PageEvents : IPdfPageEvent
        {
            public void OnEndPage(PdfWriter writer, Document document)
            {
                PdfContentByte cb = writer.DirectContent;
                if (document.PageNumber > 0)
                {
                    //insert header
                    ColumnText.ShowTextAligned(
                        cb,
                        Element.ALIGN_CENTER,
                        new Paragraph("Sun table", HeaderFont),
                        document.PageSize.Width / 2,
                        document.Top + 10 + HeaderFont.Size,
                        0);
                    ColumnText.ShowTextAligned(
                        cb,
                        Element.ALIGN_CENTER,
                        new Paragraph(string.Format(NumberFormatInfo.InvariantInfo, "Location: {0} ({1:0.000000}, {2:0.000000})", loc, lat, lng), FooterFont),
                        document.PageSize.Width / 2,
                        document.Top + 10,
                        0);

                    //insert footer
                    ColumnText.ShowTextAligned(
                        cb,
                        Element.ALIGN_CENTER,
                        new Paragraph(writer.PageNumber.ToString("Page 0"), PdfTest.FooterFont),
                        document.PageSize.Width / 2,
                        document.Bottom - 10,
                        0);
                }
            }

            public void OnChapter(PdfWriter writer, Document document, float paragraphPosition, Paragraph title) { }
            public void OnChapterEnd(PdfWriter writer, Document document, float paragraphPosition) { }
            public void OnCloseDocument(PdfWriter writer, Document document) { }
            public void OnGenericTag(PdfWriter writer, Document document, Rectangle rect, string text) { }
            public void OnOpenDocument(PdfWriter writer, Document document) { }
            public void OnParagraph(PdfWriter writer, Document document, float paragraphPosition) { }
            public void OnParagraphEnd(PdfWriter writer, Document document, float paragraphPosition) { }
            public void OnSection(PdfWriter writer, Document document, float paragraphPosition, int depth, Paragraph title) { }
            public void OnSectionEnd(PdfWriter writer, Document document, float paragraphPosition) { }
            public void OnStartPage(PdfWriter writer, Document document) { }
        }
    }
}

