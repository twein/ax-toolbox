using AXToolbox.Common;
using Microsoft.Win32;
using AXToolbox.GpsLoggers;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Collections.ObjectModel;
using AXToolbox.PdfHelpers;
using iTextSharp.text.pdf;
using iTextSharp.text;

namespace Tools
{
    public partial class WpfToPdf : TabWindow
    {
        public ObservableCollection<Waypoint> Waypoints { get; set; }

        public string DatumName { get; set; }
        public string UtmZone { get; set; }

        public string Competition { get; set; }
        public string Columns { get; set; }

        public WpfToPdf()
        {
            InitializeComponent();
            Waypoints = new ObservableCollection<Waypoint>();
            DataContext = this;

            DatumName = Datum.GetInstance("European 1950").ToString();
            UtmZone = "31T";
            Competition = "0th World Hot Air Balloon Championship";
            Columns = "3";
        }

        private void Load_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog();
            dlg.Filter = ".wpt files (*.wpt)|*.wpt";
            dlg.RestoreDirectory = true;

            string fileName = null;
            if (dlg.ShowDialog() == true)
                fileName = dlg.FileName;

            if (!string.IsNullOrEmpty(fileName))
            {
                try
                {
                    var gwplist = WPTFile.Load(fileName, DateTime.Now - DateTime.UtcNow);

                    Waypoints.Clear();
                    foreach (var gwp in gwplist)
                    {
                        var utmCoords = gwp.Coordinates.ToUtm(Datum.GetInstance(DatumName), UtmZone);
                        var altitude = utmCoords.Altitude;

                        var wp = new AXWaypoint(gwp.Name, gwp.Time, utmCoords.Easting, utmCoords.Northing, altitude);

                        Waypoints.Add(new Waypoint(wp, AXPointInfo.AltitudeInMeters));
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        private void Save_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var dlg = new SaveFileDialog()
            {
                Filter = ".pdf files (*.pdf)|*.pdf",
                RestoreDirectory = true
            };

            string fileName = null;
            if (dlg.ShowDialog() == true)
                fileName = dlg.FileName;

            if (!string.IsNullOrEmpty(fileName))
            {
                var config = new PdfConfig(PdfConfig.Application.Scorer)
                {
                    MarginTop = 1.5f * PdfHelper.cm2pt,
                    MarginBottom = 1.5f * PdfHelper.cm2pt,

                    HeaderLeft = Competition,
                    FooterLeft = "Datum " + DatumName + ", zone " + UtmZone
                };
                var helper = new PdfHelper(fileName, config);

                //title
                helper.Document.Add(new Paragraph("OFFICIAL WAYPOINT LIST", config.TitleFont)
                {
                    Alignment = Element.ALIGN_CENTER,
                    SpacingAfter = 10
                });

                //line separator
                //document.Add(new Paragraph(new Chunk(new LineSeparator())) { SpacingBefore = -10, SpacingAfter = 10 });

                //table
                var headers = new string[] { "Name", "Coordinates", "Altitude" };
                var relWidths = new float[] { 1, 2, 1 };
                var table = helper.NewTable(headers, relWidths);
                //var table = helper.NewTable(headers, relWidths, "Sun table"); //with table title

                //table body

                foreach (var wp in Waypoints)
                {
                    //place cells in table
                    table.AddCell(helper.NewLCell(wp.Name));
                    table.AddCell(helper.NewCCell(wp.CompetitionCoords));
                    table.AddCell(helper.NewRCell(wp.Altitude));
                }

                //Place table on document

                ////normal layout
                //helper.Document.Add(table);

                ////page break
                //document.NewPage();

                //multicolumn layout
                MultiColumnText columns = new MultiColumnText();
                columns.AddRegularColumns(
                    PdfHelper.cm2pt, //L-margin in pt
                    helper.Document.PageSize.Width - PdfHelper.cm2pt,  //R-margin in pt
                    0.35f * PdfHelper.cm2pt, //separation in pt
                    int.Parse(Columns)); //# of cols
                columns.AddElement(table);
                helper.Document.Add(columns);

                helper.Document.Close();

                PdfHelper.OpenPdf(fileName);
            }
        }
    }

    public class Waypoint
    {
        public string Name { get; set; }
        public string CompetitionCoords { get; set; }
        public string Altitude { get; set; }

        public Waypoint(AXWaypoint p, AXPointInfo altitudeInfo)
        {
            Name = p.Name;
            CompetitionCoords = p.ToString(AXPointInfo.CompetitionCoords8).Replace("/", " / ");
            Altitude = p.ToString(altitudeInfo);
        }
    }
}
