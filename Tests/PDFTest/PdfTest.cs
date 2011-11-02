using System;
using System.Collections.Generic;
using System.Globalization;
using AXToolbox.GpsLoggers;
using iTextSharp.text;
using iTextSharp.text.pdf;
using AXToolbox.PdfHelpers;
using iTextSharp.text.pdf.draw;

namespace PdfTest
{
    public class PdfTestClass
    {
        public string loc = "Plaça dels Països Catalans, Girona";
        public double lat = 41.950904;
        public double lng = 3.225684;

        public PdfTestClass(string pdfFileName)
        {
            var config = new PdfConfig(PdfConfig.Application.Scorer)
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
            var document = helper.Document;

            //title
            //document.Add(new Paragraph("Sun table", config.TitleFont)
            //{
            //    Alignment = Element.ALIGN_LEFT,
            //    SpacingAfter = 10
            //});

            //line separator
            //document.Add(new Paragraph(new Chunk(new LineSeparator())) { SpacingBefore = -10, SpacingAfter = 10 });

            //table
            var headers = new string[] { "Date", "Dawn", "Dusk", "Outdoor activity time" };
            var relWidths = new float[] { 8, 2, 2, 3 };
            var table = helper.NewTable(headers, relWidths);
            //var table = helper.NewTable(headers, relWidths, "Sun table"); //with table title

            //table body

            //compute cells
            var sun = new Sun(lat, lng);
            var today = DateTime.Now;
            //var from = today - new TimeSpan(10, 0, 0, 0);
            //var to = today + new TimeSpan(30, 0, 0, 0);

            var from = new DateTime(today.Year, 1, 1);
            var to = new DateTime(today.Year, 12, 31);

            for (var date = from; date <= to; date += new TimeSpan(1, 0, 0, 0))
            {
                var dawn = sun.Sunrise(date, Sun.ZenithTypes.Custom);
                var dusk = sun.Sunset(date, Sun.ZenithTypes.Custom);
                var span = dusk - dawn;

                //strings
                var strDate = date.ToString("D", DateTimeFormatInfo.InvariantInfo);
                var strDawn = dawn.ToString("HH:mm");
                var strDusk = dusk.ToString("HH:mm");
                var strSpan = span.ToString("hh\\:mm");

                //place cells in table
                table.AddCell(helper.NewLCell(strDate));
                table.AddCell(helper.NewRCell(strDawn));
                table.AddCell(helper.NewRCell(strDusk));
                table.AddCell(helper.NewRCell(strSpan));

                //var cell = helper.NewCell("Cell with colspan 2");
                //cell.Colspan = 2;
                //table.AddCell(cell);
            }

            //Place table on document

            ////normal layout
            //document.Add(table);

            ////page break
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

