using System;
using System.Windows;
using AXToolbox.Common;
using System.Globalization;
using AXToolbox.Common.IO;

namespace FlightAnalyzer
{
    /// <summary>
    /// Interaction logic for Input.xaml
    /// </summary>
    public partial class Input : Window
    {
        private string type;
        private Waypoint value = null;
        public Waypoint Value
        {
            get { return value; }
            set { this.value = value; }
        }
        private FlightSettings settings;

        public Input(string type, FlightSettings settings)
        {
            InitializeComponent();
            this.type = type;
            value = new Waypoint("000", settings.ReferencePoint);
            this.settings = settings;

            textBoxValue.Text = value.ToString(PointInfo.Name | PointInfo.Time | PointInfo.CompetitionCoords | PointInfo.Altitude);
        }

        private void buttonOk_Click(object sender, RoutedEventArgs e)
        {
            var strValue = textBoxValue.Text;

            var fields = strValue.Split(new char[] { ' ', ':', '/', '(', ')' }, StringSplitOptions.RemoveEmptyEntries);

            if (fields.Length<6 || fields.Length>7)
            {
                textBlockError.Text = "Wrong number of parameters";
                return;
            }

            int tmpNumber;
            if (!int.TryParse(fields[0], out tmpNumber))
            {
                textBlockError.Text = "Wrong " + type + " number";
                return;
            }
            if (tmpNumber == 0)
            {
                textBlockError.Text = type + " number can not be zero";
                return;
            }
            var number = tmpNumber.ToString("000");

            TimeSpan tmpTime;
            if (!TimeSpan.TryParse(fields[1] + ":" + fields[2] + ":" + fields[3], out tmpTime))
            {
                textBlockError.Text = "Wrong time";
                return;
            }
            var time = (settings.Date + tmpTime).ToUniversalTime();

            int tmpEasting;
            if (!int.TryParse(fields[4], out tmpEasting))
            {
                textBlockError.Text = "Wrong easting";
                return;
            }
            var easting = IGCFile.ComputeCorrectCoordinate(tmpEasting, settings.ReferencePoint.Easting);

            int tmpNorthing;
            if (!int.TryParse(fields[5], out tmpNorthing))
            {
                textBlockError.Text = "Wrong northing";
                return;
            }
            var northing = IGCFile.ComputeCorrectCoordinate(tmpNorthing, settings.ReferencePoint.Northing);

            double altitude = settings.DefaultAltitude;
            if (fields.Length == 7 && !double.TryParse(fields[6], out altitude))
            {
                textBlockError.Text = "Wrong altitude";
                return;
            }

            value = new Waypoint(
                number,
                time,
                settings.ReferencePoint.Datum, settings.ReferencePoint.Zone, easting, northing, altitude,
                settings.ReferencePoint.Datum
                );
            DialogResult = true;
        }

        private void buttonCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
