using iTextSharp.text;

namespace PdfHelper
{
    public class PdfConfig
    {
        public string MetadataAuthor { get; set; }
        public string MetadataTitle { get; set; }
        public string MetadataSubject { get; set; }
        public string MetadataKeywords { get; set; }

        public Rectangle PageLayout { get; set; }

        public string HeaderTitle { get; set; }
        public string HeaderSubtitle { get; set; }
        public string Footer { get; set; }

        public Font TitleFont { get; set; }
        public Font SubtitleFont { get; set; }
        public Font BoldFont { get; set; }
        public Font NormalFont { get; set; }
        public Font FooterFont { get; set; }

        public PdfConfig()
        {
            //Default values

            PageLayout = PageSize.A4.Rotate(); //A4 landscape

            TitleFont = new Font(Font.FontFamily.HELVETICA, 12, Font.BOLD);
            SubtitleFont = new Font(Font.FontFamily.HELVETICA, 10, Font.BOLD);
            BoldFont = new Font(Font.FontFamily.HELVETICA, 7, Font.BOLD);
            NormalFont = new Font(Font.FontFamily.HELVETICA, 6.5f, Font.NORMAL);
            FooterFont = new Font(Font.FontFamily.HELVETICA, 8, Font.NORMAL);
        }
    }
}
