using System.IO;
using iTextSharp.text;
using iTextSharp.text.pdf;

namespace AXToolbox.Tests
{
    public class PdfTest
    {
        public static Font FontHeader = new Font(Font.FontFamily.HELVETICA, 18, Font.BOLD);
        public static Font FontBold = new Font(Font.FontFamily.HELVETICA, Font.DEFAULTSIZE, Font.BOLD);
        public static Font FontSmall = new Font(Font.FontFamily.HELVETICA, 10, Font.NORMAL);

        public PdfTest(string pdfFileName)
        {
            var document = new Document();
            document.SetPageSize(PageSize.A4.Rotate());
            document.SetMargins(30, 30, 30, 30); // in pt
            PdfWriter.GetInstance(document, new FileStream(pdfFileName, FileMode.Create)).PageEvent = new PageEvents();
            document.Open();

            //title
            document.Add(new Paragraph("iPDF test", FontHeader));

            //paragraph
            document.Add(new Paragraph("Special characters test: à è ò í ú ï ü ç À È Ò Í Ú Ï Ü Ç."));

            //page break
            document.NewPage();

            //table
            var relWidths = new float[] { 4, 1, 1, 1 };
            var table = new PdfPTable(relWidths)
            {
                WidthPercentage = 100,
                SpacingBefore = 15,
                SpacingAfter = 10,
                HeaderRows = 1
            };
            table.DefaultCell.HorizontalAlignment = Element.ALIGN_RIGHT;

            //table header
            table.AddCell(new PdfPCell(new Paragraph("Name", FontBold)) { HorizontalAlignment = Element.ALIGN_LEFT });
            table.AddCell(new PdfPCell(new Paragraph("Score 1", FontBold)) { HorizontalAlignment = Element.ALIGN_RIGHT });
            table.AddCell(new PdfPCell(new Paragraph("Score 2", FontBold)) { HorizontalAlignment = Element.ALIGN_RIGHT });
            table.AddCell(new PdfPCell(new Paragraph("Score 3", FontBold)) { HorizontalAlignment = Element.ALIGN_RIGHT });

            //table content
            for (int row = 1; row <= 90; row++)
            {
                table.AddCell(new PdfPCell(new Paragraph(row.ToString("0 1"))) { HorizontalAlignment = Element.ALIGN_LEFT });
                table.AddCell(new Paragraph(row.ToString("0 2")));
                table.AddCell(new Paragraph(row.ToString("0 3")));
                table.AddCell(new Paragraph(row.ToString("0 4")));
            }

            document.Add(table);


            document.Close();
        }

        internal class PageEvents : IPdfPageEvent
        {
            public void OnEndPage(PdfWriter writer, Document document)
            {
                PdfContentByte cb = writer.DirectContent;
                if (document.PageNumber > 0)
                {
                    //insert footer
                    ColumnText.ShowTextAligned(
                        cb,
                        Element.ALIGN_LEFT,
                        new Paragraph(writer.PageNumber.ToString("Page 0"), PdfTest.FontSmall),
                        document.LeftMargin,
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

