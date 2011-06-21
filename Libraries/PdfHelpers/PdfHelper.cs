using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System.IO;
using System.Globalization;

namespace PdfHelpers
{
    public class PdfHelper
    {
        public static float cm2pt = 72f / 2.54f;

        protected PdfConfig config;

        public Document PdfDocument { get; protected set; }

        public PdfHelper(string pdfFileName, PdfConfig pdfConfig)
        {
            config = pdfConfig;

            PdfDocument = new Document();
            PdfDocument.SetPageSize(pdfConfig.PageLayout);
            PdfDocument.SetMargins(config.MarginLeft, config.MarginRight, config.MarginTop, config.MarginBottom); // in pt

            PdfWriter.GetInstance(PdfDocument, new FileStream(pdfFileName, FileMode.Create)).PageEvent = new PageEvents(pdfConfig);

            PdfDocument.Open();
            PdfDocument.AddAuthor(pdfConfig.MetadataAuthor);
            PdfDocument.AddTitle(pdfConfig.MetadataTitle);
            PdfDocument.AddSubject(pdfConfig.MetadataSubject);
            PdfDocument.AddKeywords(pdfConfig.MetadataKeywords);
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
                    if (!string.IsNullOrEmpty(config.HeaderLeft))
                    {
                        ColumnText.ShowTextAligned(
                            cb,
                            Element.ALIGN_LEFT,
                            new Paragraph(config.HeaderLeft, config.HeaderFont),
                            document.Left,
                            document.Top + config.HeaderFont.Size,
                            0);
                    }
                    //subtitle
                    if (!string.IsNullOrEmpty(config.HeaderRight))
                    {
                        ColumnText.ShowTextAligned(
                            cb,
                            Element.ALIGN_RIGHT,
                            new Paragraph(config.HeaderRight, config.HeaderFont),
                            document.Right,
                            document.Top + config.HeaderFont.Size,
                            0);
                    }

                    //insert footer
                    if (!string.IsNullOrEmpty(config.FooterLeft))
                    {
                        ColumnText.ShowTextAligned(
                            cb,
                            Element.ALIGN_LEFT,
                            new Paragraph(config.FooterLeft, config.FooterFont),
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
