using System;
using System.Collections.Generic;
using AXToolbox.GpsLoggers;
using Microsoft.Win32;
using System.Globalization;

namespace Tools
{
    public class Helper
    {
        public static string OpenWptFile()
        {
            var dlg = new OpenFileDialog();
            dlg.Filter = ".wpt files (*.wpt)|*.wpt";
            dlg.RestoreDirectory = true;

            string fileName = null;
            if (dlg.ShowDialog() == true)
                fileName = dlg.FileName;

            return fileName;
        }

        public static string SaveFile(string filter)
        {
            var dlg = new SaveFileDialog();
            dlg.Filter = filter;
            dlg.RestoreDirectory = true;

            string fileName = null;
            if (dlg.ShowDialog() == true)
                fileName = dlg.FileName;

            return fileName;
        }


        public static IEnumerable<GeoWaypoint> LoadWptFile(string fileName)
        {
            return WPTFile.Load(fileName, DateTime.Now - DateTime.UtcNow);
        }
    }

    public static class GeoWaypointExtensions
    {
        public static PdfWaypoint ToPdfWaypoint(this GeoWaypoint gwp, string datumName, string utmZone, AltitudeUnits units)
        {
            var utmCoords = gwp.Coordinates.ToUtm(Datum.GetInstance(datumName), utmZone);
            var altitude = utmCoords.Altitude;
            var wp = new AXWaypoint(gwp.Name, gwp.Time, utmCoords.Easting, utmCoords.Northing, altitude);

            return new PdfWaypoint()
            {
                Name = wp.Name,
                CompetitionCoords = wp.ToString(AXPointInfo.CompetitionCoords8).Replace("/", " / "),
                Altitude = wp.ToString(units == AltitudeUnits.Meters ? AXPointInfo.AltitudeInMeters : AXPointInfo.AltitudeInFeet)
            };
        }
        public static string ToAxsPoint(this GeoWaypoint gwp, string datumName, string utmZone, AltitudeUnits units)
        {
            var utmCoords = gwp.Coordinates.ToUtm(Datum.GetInstance(datumName), utmZone);
            return string.Format(NumberFormatInfo.InvariantInfo, "point {0}=sutm({1:0.0},{2:0.0},{3:0.0}{4}) waypoint(lime)",
                gwp.Name,
                utmCoords.Easting, utmCoords.Northing,
                units == AltitudeUnits.Meters ? utmCoords.Altitude : utmCoords.Altitude * Physics.METERS2FEET,
                units == AltitudeUnits.Meters ? "m" : "ft"
                );
        }
    }


    public struct PdfWaypoint
    {
        public string Name { get; set; }
        public string CompetitionCoords { get; set; }
        public string Altitude { get; set; }
    }
}
