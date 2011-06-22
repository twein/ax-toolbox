using System;
using System.Collections.Generic;
using AXToolbox.Common;
using AXToolbox.PdfHelpers;
using iTextSharp.text;
using System.Linq;

namespace Scorer
{
    [Serializable]
    public class Pilot : BindableObject
    {
        private int number;
        public int Number
        {
            get { return number; }
            set
            {
                number = value;
                RaisePropertyChanged("Number");
            }
        }
        private string name;
        public string Name
        {
            get { return name; }
            set
            {
                name = value;
                RaisePropertyChanged("Name");
            }
        }
        private string country;
        public string Country
        {
            get { return country; }
            set
            {
                country = value;
                RaisePropertyChanged("Country");
            }
        }
        private string balloon;
        public string Balloon
        {
            get { return balloon; }
            set
            {
                balloon = value;
                RaisePropertyChanged("Balloon");
            }
        }
        private bool isDisqualified;
        public bool IsDisqualified
        {
            get { return isDisqualified; }
            set
            {
                isDisqualified = value;
                Database.Instance.IsDirty = true;
            }
        }

        public Pilot()
        {
            name = "enter pilot name";
        }

        protected override void AfterPropertyChanged(string propertyName)
        {
            Database.Instance.IsDirty = true;
        }

        public override string ToString()
        {
            return string.Format("{0:000}: {1}", Number, Name);
        }

        public static void PdfList(string pdfFileName, string title, IEnumerable<Pilot> pilots)
        {
            var config = new PdfConfig()
            {
                PageLayout = PageSize.A4,
                MarginTop = 1.5f * PdfHelper.cm2pt,
                MarginBottom = 1.5f * PdfHelper.cm2pt,

                HeaderLeft = title,
                FooterLeft = string.Format("Printed on {0:yyyy/MM/dd HH:mm}", DateTime.Now),
            };
            var helper = new PdfHelper(pdfFileName, config);
            var document = helper.PdfDocument;

            //title
            document.Add(new Paragraph(title, config.TitleFont)
            {
                Alignment = Element.ALIGN_LEFT,
                SpacingAfter = 10
            });


            //table
            var headers = new string[] { "Num.", "Name", "Country", "Balloon" };
            var relWidths = new float[] { 1, 6, 3, 3 };
            var table = helper.NewTable(headers, relWidths);
            foreach (var pilot in pilots.OrderBy(p => p.Number))
            {
                table.AddCell(helper.NewRCell(pilot.Number.ToString()));
                table.AddCell(helper.NewLCell(pilot.Name));
                table.AddCell(helper.NewLCell(pilot.Country));
                table.AddCell(helper.NewLCell(pilot.Balloon));
            }
            document.Add(table);

            document.Close();

            PdfHelper.OpenPdf(pdfFileName);
        }
        public static void PdfWorkList(string pdfFileName, string title, IEnumerable<Pilot> pilots)
        {
            var config = new PdfConfig()
            {
                PageLayout = PageSize.A4.Rotate(),
                MarginTop = 1.5f * PdfHelper.cm2pt,
                MarginBottom = 1.5f * PdfHelper.cm2pt,

                HeaderLeft = title,
                FooterLeft = string.Format("Printed on {0:yyyy/MM/dd HH:mm}", DateTime.Now),
            };
            var helper = new PdfHelper(pdfFileName, config);
            var document = helper.PdfDocument;

            //title
            document.Add(new Paragraph(title, config.TitleFont)
            {
                Alignment = Element.ALIGN_LEFT,
                SpacingAfter = 10
            });


            //table
            var headers = new string[] { "#", "Name", "", "", "", "", "", "", "", "" };
            var relWidths = new float[] { 1, 6, 3, 3, 3, 3, 3, 3, 3, 3 };
            var table = helper.NewTable(headers, relWidths);
            foreach (var pilot in pilots.OrderBy(p => p.Number))
            {
                table.AddCell(helper.NewRCell(pilot.Number.ToString()));
                table.AddCell(helper.NewLCell(pilot.Name));
                for (var i = 0; i < 8; i++)
                    table.AddCell(helper.NewLCell(""));
            }
            document.Add(table);

            document.Close();

            PdfHelper.OpenPdf(pdfFileName);
        }
    }
}
