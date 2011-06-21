﻿using iTextSharp.text;
using System;

namespace PdfHelpers
{
    public class PdfConfig
    {
        public string MetadataAuthor { get; set; }
        public string MetadataTitle { get; set; }
        public string MetadataSubject { get; set; }
        public string MetadataKeywords { get; set; }

        public Rectangle PageLayout { get; set; }
        public float MarginLeft { get; set; }
        public float MarginRight { get; set; }
        public float MarginTop { get; set; }
        public float MarginBottom { get; set; }

        public string HeaderLeft { get; set; }
        public string HeaderRight { get; set; }
        public string FooterLeft { get; set; }

        public Font TitleFont { get; set; }
        public Font SubtitleFont { get; set; }
        public Font BoldFont { get; set; }
        public Font NormalFont { get; set; }
        public Font HeaderFont { get; set; }
        public Font FooterFont { get; set; }

        public PdfConfig()
        {
            //Default values
            //PageLayout = PageSize.A4; //A4 portrait
            PageLayout = PageSize.A4.Rotate(); //A4 landscape
            MarginLeft = 1 * PdfHelper.cm2pt;
            MarginRight = 1 * PdfHelper.cm2pt;
            MarginTop = 1 * PdfHelper.cm2pt;
            MarginBottom = 1 * PdfHelper.cm2pt;

            TitleFont = new Font(Font.FontFamily.HELVETICA, 14f, Font.BOLD);
            SubtitleFont = new Font(Font.FontFamily.HELVETICA, 12f, Font.BOLD);
            BoldFont = new Font(Font.FontFamily.HELVETICA, 10f, Font.BOLD);
            NormalFont = new Font(Font.FontFamily.HELVETICA, 10f, Font.NORMAL);
            HeaderFont = new Font(Font.FontFamily.HELVETICA, 8f, Font.NORMAL);
            FooterFont = new Font(Font.FontFamily.HELVETICA, 8f, Font.NORMAL);
        }
    }
}
