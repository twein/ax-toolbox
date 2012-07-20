using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using AXToolbox.Common;
using AXToolbox.GpsLoggers;
using AXToolbox.PdfHelpers;
using iTextSharp.text;
using iTextSharp.text.pdf;

namespace Tools
{
    public partial class WptTools : TabWindow
    {
        public ObservableCollection<GeoWaypoint> Waypoints { get; set; }

        public string DatumName { get; set; }
        public string UtmZone { get; set; }
        public string Competition { get; set; }

        public string Columns { get; set; }

        public WptTools()
        {
            InitializeComponent();
            Waypoints = new ObservableCollection<GeoWaypoint>();
            DataContext = this;

            DatumName = Datum.GetInstance("European 1950").ToString();
            UtmZone = "31T";
            Competition = "0th World Hot Air Balloon Championship";
            Columns = "3";
        }

        private void Load_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var fileName = Helper.OpenWptFile();
            if (!string.IsNullOrEmpty(fileName))
            {
                Waypoints.Clear();
                foreach (var gwp in Helper.LoadWptFile(fileName))
                    Waypoints.Add(gwp);
            }
        }
        private void SavePdf_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var fileName = Helper.SaveFile(".pdf files (*.pdf)|*.pdf");
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

                foreach (var wp in Waypoints.Select(p => p.ToPdfWaypoint(DatumName, UtmZone, AltitudeUnits.Meters)))
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
        private void SaveAxs_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var fileName = Helper.SaveFile(".axs files (*.axs)|*.axs");

            if (!string.IsNullOrEmpty(fileName))
            {
                var lines = new List<string>();
                lines.Add("set title=" + Competition);
                lines.Add("set subtitle=put location and dates here");
                lines.Add(string.Format("set DateTime = {0:yyyy/MM/dd},AM", DateTime.Now));
                lines.Add(string.Format("set utcoffset={0:hh\\:mm}", DateTime.Now - DateTime.UtcNow));
                lines.Add("set Datum=" + DatumName);
                lines.Add("set UTMZone=" + UtmZone);
                lines.Add("set tasksinorder=true");
                lines.Add("set QNH = 1018");
                lines.Add("set altitudeunits = meters");
                lines.Add("// set altitudecorrectionsfile=insterr.cfg");
                lines.Add("map competitionMap = bitmap(competitionmap.jpg) grid(1000)");
                lines.AddRange(Waypoints.Select(wp => wp.ToAxsPoint(DatumName, UtmZone, AltitudeUnits.Meters)));

                File.WriteAllLines(fileName, lines);
            }
        }
    }
}
