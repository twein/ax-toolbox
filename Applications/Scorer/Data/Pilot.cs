using System;
using System.Collections.Generic;
using AXToolbox.Common;
using AXToolbox.PdfHelpers;
using iTextSharp.text;
using System.Linq;
using System.Windows;

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
                if (isDisqualified != value)
                {
                    if (MessageBox.Show("This action will invalidate all current scores." + Environment.NewLine + "Are you sure?", "Alert", MessageBoxButton.YesNo, MessageBoxImage.Exclamation) == MessageBoxResult.Yes)
                    {
                        isDisqualified = value;
                        Event.Instance.IsDirty = true;

                        // void all scores
                        foreach (var t in Event.Instance.Tasks)
                            t.Phases |= CompletedPhases.Dirty;
                        RaisePropertyChanged("IsDisqualified");
                    }
                }
            }
        }

        public Pilot()
        {
            name = "enter pilot name";
        }

        public override void AfterPropertyChanged(string propertyName)
        {
            Event.Instance.IsDirty = true;
        }

        public override string ToString()
        {
            return string.Format("{0:000}: {1}", Number, Name);
        }

        public static void PdfList(string pdfFileName, string title, IEnumerable<Pilot> pilots)
        {
            var config = Event.Instance.GetDefaultPdfConfig();
            config.HeaderLeft = Event.Instance.Name;

            var helper = new PdfHelper(pdfFileName, config);
            var document = helper.Document;

            //title
            document.Add(new Paragraph(Event.Instance.Name, config.TitleFont));
            //subtitle
            document.Add(new Paragraph(title, config.SubtitleFont) { SpacingAfter = 10 });

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
            var config = Event.Instance.GetDefaultPdfConfig();
            config.HeaderLeft = Event.Instance.Name;

            var helper = new PdfHelper(pdfFileName, config);
            var document = helper.Document;

            //title
            document.Add(new Paragraph(Event.Instance.Name, config.TitleFont));
            //subtitle
            document.Add(new Paragraph(title, config.SubtitleFont) { SpacingAfter = 10 });

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
