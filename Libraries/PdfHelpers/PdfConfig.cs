using iTextSharp.text;
using iTextSharp.text.pdf;

namespace AXToolbox.PdfHelpers
{
    public class PdfConfig
    {
        public enum Application { FlightAnalyzer, Scorer }

        public Rectangle PageLayout { get; set; }
        public float MarginLeft { get; set; }
        public float MarginRight { get; set; }
        public float MarginTop { get; set; }
        public float MarginBottom { get; set; }
        public float MarginHeader { get; set; }
        public float MarginFooter { get; set; }

        public string HeaderLeft { get; set; }
        public string HeaderCenter { get; set; }
        public string HeaderRight { get; set; }
        public string FooterLeft { get; set; }
        public string FooterCenter { get; set; }
        public string FooterRight { get; set; }
        public string Watermark { get; set; }
        public string TaskNumber { get; set; }

        public Font TitleFont { get; set; }
        public Font SubtitleFont { get; set; }
        public Font BoldFont { get; set; }
        public Font ItalicFont { get; set; }
        public Font NormalFont { get; set; }
        public Font FixedWidthFont { get; set; }
        public Font HeaderFont { get; set; }
        public Font FooterFont { get; set; }
        public Font WatermarkFont { get; set; }
        public Font TaskNumberFont { get; set; }

        public PdfConfig(Application application)
        {
            //Default values
            if (application == Application.FlightAnalyzer)
            {
                PageLayout = PageSize.A4; //A4 portrait

                MarginLeft = 1f * PdfHelper.cm2pt;
                MarginRight = 1f * PdfHelper.cm2pt;
                MarginTop = 1.5f * PdfHelper.cm2pt;
                MarginBottom = 1.5f * PdfHelper.cm2pt;
                MarginHeader = 0.3f * PdfHelper.cm2pt;
                MarginFooter = 0.3f * PdfHelper.cm2pt;

                TitleFont = new Font(Font.FontFamily.HELVETICA, 14f, Font.BOLD);
                SubtitleFont = new Font(Font.FontFamily.HELVETICA, 12f, Font.BOLD);
                NormalFont = new Font(Font.FontFamily.HELVETICA, 10f, Font.NORMAL);
                FixedWidthFont = new Font(Font.FontFamily.COURIER, 8f, Font.NORMAL);
                BoldFont = new Font(Font.FontFamily.HELVETICA, 10f, Font.BOLD);
                ItalicFont = new Font(Font.FontFamily.HELVETICA, 10f, Font.ITALIC);
                HeaderFont = new Font(Font.FontFamily.HELVETICA, 10f, Font.NORMAL);
                FooterFont = new Font(Font.FontFamily.HELVETICA, 8f, Font.NORMAL);
                WatermarkFont = new Font(Font.FontFamily.HELVETICA, 64f, Font.BOLD, BaseColor.LIGHT_GRAY);
                TaskNumberFont = new Font(Font.FontFamily.HELVETICA, 32f, Font.BOLD);
            }

            else
            {
                PageLayout = PageSize.A4.Rotate(); //A4 landscape

                MarginLeft = 1f * PdfHelper.cm2pt;
                MarginRight = 1f * PdfHelper.cm2pt;
                MarginTop = 1.5f * PdfHelper.cm2pt;
                MarginBottom = 1.5f * PdfHelper.cm2pt;
                MarginHeader = 0.3f * PdfHelper.cm2pt;
                MarginFooter = 0.3f * PdfHelper.cm2pt;

                TitleFont = new Font(Font.FontFamily.HELVETICA, 12f, Font.BOLD);
                SubtitleFont = new Font(Font.FontFamily.HELVETICA, 10f, Font.BOLD);
                NormalFont = new Font(Font.FontFamily.HELVETICA, 8f, Font.NORMAL);
                FixedWidthFont = new Font(Font.FontFamily.COURIER, 10f, Font.NORMAL);
                BoldFont = new Font(Font.FontFamily.HELVETICA, 8f, Font.BOLD);
                ItalicFont = new Font(Font.FontFamily.HELVETICA, 8f, Font.ITALIC);
                HeaderFont = new Font(Font.FontFamily.HELVETICA, 8f, Font.NORMAL);
                FooterFont = new Font(Font.FontFamily.HELVETICA, 8f, Font.NORMAL);
                WatermarkFont = new Font(Font.FontFamily.HELVETICA, 64f, Font.BOLD, BaseColor.LIGHT_GRAY);
                TaskNumberFont = new Font(Font.FontFamily.HELVETICA, 32f, Font.BOLD);
            }
        }
    }
}
