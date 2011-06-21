using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System.IO;
using System.Globalization;

namespace PdfHelper
{
    public class PdfHelper
    {
        public static float cm2pt = 72f / 2.54f;

        protected PdfConfig config;

        public PdfHelper(string pdfFileName, PdfConfig pdfConfig)
        {
            config = pdfConfig;

            var document = new Document();
            //document.SetPageSize(PageSize.A4); //portrait
            document.SetPageSize(PageSize.A4.Rotate()); //landscape
            document.SetMargins(1f * cm2pt, 1f * cm2pt, 1.5f * cm2pt, 1.5f * cm2pt); // in pt

            PdfWriter.GetInstance(document, new FileStream(pdfFileName, FileMode.Create)).PageEvent = new PageEvents(pdfConfig);

            document.Open();
            document.AddAuthor(pdfConfig.MetadataAuthor);
            document.AddTitle(pdfConfig.MetadataTitle);
            document.AddSubject(pdfConfig.MetadataSubject);
            document.AddKeywords(pdfConfig.MetadataKeywords);

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
            table.AddCell(new PdfPCell(new Paragraph("Date", config.BoldFont))
            {
                HorizontalAlignment = Element.ALIGN_LEFT
            });
            table.AddCell(new PdfPCell(new Paragraph("Dawn", config.BoldFont)));
            table.AddCell(new PdfPCell(new Paragraph("Dusk", config.BoldFont)));
            table.AddCell(new PdfPCell(new Paragraph("Outdoor activity time", config.BoldFont)));

            //table body
            var cells = new List<string>();
            foreach (var c in cells)
            {
                table.AddCell(new PdfPCell(new Paragraph(c, config.NormalFont)));
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


        internal class PageEvents : IPdfPageEvent
        {
            protected PdfConfig config;

            public PageEvents(PdfConfig pdfConfig)
            {
                config = pdfConfig;
            }

            public void OnEndPage(PdfWriter writer, Document document)
            {
                PdfContentByte cb = writer.DirectContent;
                if (document.PageNumber > 0)
                {
                    //insert header
                    //title
                    if (!string.IsNullOrEmpty(config.HeaderTitle))
                    {
                        ColumnText.ShowTextAligned(
                            cb,
                            Element.ALIGN_LEFT,
                            new Paragraph(config.HeaderTitle, config.TitleFont),
                            document.Left,
                            document.Top + 10 + config.TitleFont.Size,
                            0);
                    }
                    //subtitle
                    if (!string.IsNullOrEmpty(config.HeaderSubtitle))
                    {
                        ColumnText.ShowTextAligned(
                            cb,
                            Element.ALIGN_LEFT,
                            new Paragraph(config.HeaderSubtitle, config.SubtitleFont),
                            document.Left,
                            document.Top + 10,
                            0);
                    }

                    //insert footer
                    if (!string.IsNullOrEmpty(config.Footer))
                    {
                        ColumnText.ShowTextAligned(
                            cb,
                            Element.ALIGN_LEFT,
                            new Paragraph(config.Footer, config.FooterFont),
                            document.Left,
                            document.Bottom - 10,
                            0);
                    }
                    //page number
                    ColumnText.ShowTextAligned(
                        cb,
                        Element.ALIGN_RIGHT,
                        new Paragraph(writer.PageNumber.ToString("Page 0"), config.FooterFont),
                        document.Right,
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
