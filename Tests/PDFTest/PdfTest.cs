using System;
using System.Collections.Generic;
using System.Globalization;
using AXToolbox.GPSLoggers;
using iTextSharp.text;
using iTextSharp.text.pdf;
using PdfHelpers;

namespace PdfTest
{
    public class PdfTestClass
    {
        public string loc = "Plaça dels Països Catalans, Girona";
        public double lat = 41.950904;
        public double lng = 3.225684;


        public PdfTestClass(string pdfFileName)
        {
            var config = new PdfConfig()
                {
                    MarginTop = 1.5f * PdfHelper.cm2pt,
                    MarginBottom = 1.5f * PdfHelper.cm2pt,

                    HeaderLeft = "Sun table",
                    HeaderRight = loc,
                    FooterLeft = string.Format("Printed on {0:yyyy/MM/dd HH:mm}", DateTime.Now),

                    TitleFont = new Font(Font.FontFamily.HELVETICA, 12, Font.BOLD),
                    SubtitleFont = new Font(Font.FontFamily.HELVETICA, 10, Font.BOLD),
                    BoldFont = new Font(Font.FontFamily.HELVETICA, 7, Font.BOLD),
                    NormalFont = new Font(Font.FontFamily.HELVETICA, 6.5f, Font.NORMAL),
                    HeaderFont = new Font(Font.FontFamily.HELVETICA, 6.5f, Font.NORMAL),
                    FooterFont = new Font(Font.FontFamily.HELVETICA, 6.5f, Font.NORMAL)
                };
            var helper = new PdfHelper(pdfFileName, config);
            var document = helper.PdfDocument;

            //title
            //document.Add(new Paragraph("Sun table", config.TitleFont)
            //{
            //    Alignment = Element.ALIGN_LEFT,
            //    SpacingAfter = 10
            //});


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
            //table.DefaultCell.BackgroundColor = new BaseColor(192, 192, 192);

            //table header
            var headerColor = new BaseColor(192, 192, 192);
            table.AddCell(new PdfPCell(new Paragraph("Date", config.BoldFont)) { BackgroundColor = headerColor });
            table.AddCell(new PdfPCell(new Paragraph("Dawn", config.BoldFont)) { BackgroundColor = headerColor });
            table.AddCell(new PdfPCell(new Paragraph("Dusk", config.BoldFont)) { BackgroundColor = headerColor });
            table.AddCell(new PdfPCell(new Paragraph("Outdoor activity time", config.BoldFont)) { BackgroundColor = headerColor });

            //table body

            //compute cells
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
                cells.Add(date.ToString("D", DateTimeFormatInfo.InvariantInfo));
                cells.Add(sr.ToString("HH:mm"));
                cells.Add(ss.ToString("HH:mm"));
                cells.Add(span.ToString("hh\\:mm"));
            }

            //place cells in table
            foreach (var c in cells)
                table.AddCell(new PdfPCell(new Paragraph(c, config.NormalFont)));


            //Place table on document

            //normal layout
            //document.Add(table);

            //page break
            //document.NewPage();

            //multicolumn layout
            MultiColumnText columns = new MultiColumnText();
            columns.AddRegularColumns(
                PdfHelper.cm2pt, //L-margin in pt
                document.PageSize.Width - PdfHelper.cm2pt,  //R-margin in pt
                0.35f * PdfHelper.cm2pt, //separation in pt
                4); //# of cols
            columns.AddElement(table);
            document.Add(columns);

            document.Close();
        }
    }
}

