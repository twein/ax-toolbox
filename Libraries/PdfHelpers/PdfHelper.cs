using System;
using System.Diagnostics;
using System.IO;
using iTextSharp.text;
using iTextSharp.text.pdf;

namespace AXToolbox.PdfHelpers
{
    public class PdfHelper
    {
        public static float cm2pt = 72f / 2.54f;

        public PdfConfig Config{get;protected set;}

        public Document Document { get; protected set; }
        public PdfWriter Writer { get; protected set; }

        private string pdfFileName;

        public PdfHelper(string pdfFileName, PdfConfig pdfConfig)
        {
            this.pdfFileName = pdfFileName;
            Config = pdfConfig;

            Document = new Document();
            Document.SetPageSize(pdfConfig.PageLayout);
            Document.SetMargins(Config.MarginLeft, Config.MarginRight, Config.MarginTop, Config.MarginBottom); // in pt

            Writer = PdfWriter.GetInstance(Document, new FileStream(pdfFileName, FileMode.Create));
            Writer.PageEvent = new PageEvents(pdfConfig);

            Document.Open();
        }

        public void AddMetadata(string author, string title, string subject, string keywords)
        {
            Document.AddAuthor(author);
            Document.AddTitle(title);
            Document.AddSubject(subject);
            Document.AddKeywords(keywords);
        }

        public PdfPTable NewTable(string[] columnHeaders, float[] relativeColumnWidths, string title = null)
        {
            if (columnHeaders != null)
                Debug.Assert(columnHeaders.Length == relativeColumnWidths.Length, "columnHeaders and relativeColumnWidths must have the same number of elements");

            var table = new PdfPTable(relativeColumnWidths)
            {
                WidthPercentage = 100,
                SpacingBefore = 15,
                SpacingAfter = 10,
                HeaderRows = (string.IsNullOrEmpty(title) ? 0 : 1) + (columnHeaders == null ? 0 : 1)
            };

            var headerColor = new BaseColor(0xc0, 0xc0, 0xc0);

            if (!string.IsNullOrEmpty(title))
                table.AddCell(new PdfPCell(new Paragraph(title, Config.BoldFont)) { Colspan = relativeColumnWidths.Length, BackgroundColor = headerColor });

            if (columnHeaders != null)
                foreach (var ch in columnHeaders)
                    table.AddCell(new PdfPCell(new Paragraph(ch, Config.BoldFont)) { BackgroundColor = headerColor });

            return table;
        }

        public Paragraph NewParagraph(string content)
        {
            return new Paragraph(content, Config.NormalFont);
        }

        public PdfPCell NewLCell(string cellContent, int colSpan = 1, BaseColor bgColor = null)
        {
            var cell = new PdfPCell(new Phrase(cellContent, Config.NormalFont)) { HorizontalAlignment = Element.ALIGN_LEFT, Colspan = colSpan };
            if (bgColor != null)
                cell.BackgroundColor = bgColor;
            return cell;
        }
        public PdfPCell NewRCell(string cellContent, int colSpan = 1, BaseColor bgColor = null)
        {
            var cell = new PdfPCell(new Phrase(cellContent, Config.NormalFont)) { HorizontalAlignment = Element.ALIGN_RIGHT, Colspan = colSpan };
            if (bgColor != null)
                cell.BackgroundColor = bgColor;
            return cell;
        }
        public PdfPCell NewCCell(string cellContent, int colSpan = 1, BaseColor bgColor = null)
        {
            var cell = new PdfPCell(new Phrase(cellContent, Config.NormalFont)) { HorizontalAlignment = Element.ALIGN_CENTER, Colspan = colSpan };
            if (bgColor != null)
                cell.BackgroundColor = bgColor;
            return cell;
        }
        public PdfPCell NewRCellBold(string cellContent, int colSpan = 1, BaseColor bgColor = null)
        {
            var cell = new PdfPCell(new Phrase(cellContent, Config.BoldFont)) { HorizontalAlignment = Element.ALIGN_RIGHT, Colspan = colSpan };
            if (bgColor != null)
                cell.BackgroundColor = bgColor;
            return cell;
        }

        public void OpenPdf()
        {
            PdfHelper.OpenPdf(pdfFileName);
        }

        public static void OpenPdf(string pdfFileName)
        {
            try
            {
                var proc = new System.Diagnostics.Process();
                proc.EnableRaisingEvents = false;
                proc.StartInfo.FileName = pdfFileName;
                proc.Start();
            }
            catch { }
        }

        internal class PageEvents : IPdfPageEvent
        {
            protected PdfConfig config;

            public PageEvents(PdfConfig pdfConfig)
            {
                config = pdfConfig;
            }

            public void OnStartPage(PdfWriter writer, Document document) { }
            public void OnEndPage(PdfWriter writer, Document document)
            {
                PdfContentByte cOver = writer.DirectContent;
                PdfContentByte cUnder = writer.DirectContentUnder;

                if (document.PageNumber > 0)
                {
                    //insert watermark
                    if (!string.IsNullOrEmpty(config.Watermark))
                    {
                        ColumnText.ShowTextAligned(
                            cUnder,
                            Element.ALIGN_CENTER,
                            new Paragraph(config.Watermark, config.WatermarkFont) { },
                            (document.Left + document.Right) / 2,
                            (document.Top + document.Bottom) / 2,
                            (float)(Math.Atan2(document.Top - document.Bottom, document.Right - document.Left) * 180 / Math.PI));
                    }


                    //insert task number
                    if (!string.IsNullOrEmpty(config.TaskNumber))
                    {
                        ColumnText.ShowTextAligned(
                            cOver,
                            Element.ALIGN_RIGHT,
                            new Paragraph(config.TaskNumber, config.TaskNumberFont),
                            document.Right,
                            document.Top - config.TaskNumberFont.Size / 2,
                            0);
                    }


                    //insert header
                    //left
                    if (!string.IsNullOrEmpty(config.HeaderLeft))
                    {
                        ColumnText.ShowTextAligned(
                            cOver,
                            Element.ALIGN_LEFT,
                            new Paragraph(config.HeaderLeft, config.HeaderFont),
                            document.Left,
                            document.Top + config.MarginHeader + config.HeaderFont.Size / 2,
                            0);
                    }
                    //center
                    if (!string.IsNullOrEmpty(config.HeaderCenter))
                    {
                        ColumnText.ShowTextAligned(
                            cOver,
                            Element.ALIGN_CENTER,
                            new Paragraph(config.HeaderCenter, config.HeaderFont),
                            document.PageSize.Width / 2,
                            document.Top + config.MarginHeader + config.HeaderFont.Size / 2,
                            0);
                    }
                    //right
                    if (!string.IsNullOrEmpty(config.HeaderRight))
                    {
                        ColumnText.ShowTextAligned(
                            cOver,
                            Element.ALIGN_RIGHT,
                            new Paragraph(config.HeaderRight, config.HeaderFont),
                            document.Right,
                            document.Top + config.MarginHeader + config.HeaderFont.Size / 2,
                            0);
                    }

                    if (!string.IsNullOrEmpty(config.HeaderLeft)
                        || !string.IsNullOrEmpty(config.HeaderCenter)
                        || !string.IsNullOrEmpty(config.HeaderRight))
                    {
                        cOver.MoveTo(document.Left, document.Top + config.MarginHeader);
                        cOver.LineTo(document.Right, document.Top + config.MarginHeader);
                        cOver.Stroke();
                    }


                    //insert footer
                    //left
                    if (!string.IsNullOrEmpty(config.FooterLeft))
                    {
                        ColumnText.ShowTextAligned(
                            cOver,
                            Element.ALIGN_LEFT,
                            new Paragraph(config.FooterLeft, config.FooterFont),
                            document.Left,
                            document.Bottom - config.MarginFooter - config.FooterFont.Size,
                            0);
                    }
                    //center
                    if (!string.IsNullOrEmpty(config.FooterCenter))
                    {
                        ColumnText.ShowTextAligned(
                            cOver,
                            Element.ALIGN_CENTER,
                            new Paragraph(config.FooterCenter, config.FooterFont),
                            document.PageSize.Width / 2,
                            document.Bottom - config.MarginFooter - config.FooterFont.Size,
                            0);
                    }
                    else
                    {
                        //page number
                        ColumnText.ShowTextAligned(
                            cOver,
                            Element.ALIGN_CENTER,
                            new Paragraph(writer.PageNumber.ToString("Page 0"), config.FooterFont),
                            document.PageSize.Width / 2,
                            document.Bottom - config.MarginFooter - config.FooterFont.Size,
                            0);
                    }
                    //right
                    if (!string.IsNullOrEmpty(config.FooterRight))
                    {
                        ColumnText.ShowTextAligned(
                            cOver,
                            Element.ALIGN_RIGHT,
                            new Paragraph(config.FooterRight, config.FooterFont),
                            document.Right,
                            document.Bottom - config.MarginFooter - config.FooterFont.Size,
                            0);
                    }

                    cOver.MoveTo(document.Left, document.Bottom - config.MarginFooter);
                    cOver.LineTo(document.Right, document.Bottom - config.MarginFooter);
                    cOver.Stroke();
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
        }
    }
}
