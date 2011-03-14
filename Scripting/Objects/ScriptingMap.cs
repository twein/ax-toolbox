using System;
using System.Collections.Generic;
using System.IO;
using AXToolbox.Common;
using AXToolbox.MapViewer;

namespace AXToolbox.Scripting
{
    public class ScriptingMap : ScriptingObject
    {
        private static readonly List<string> types = new List<string>
        {
            "BITMAP","BLANK"
        };
        private static readonly List<string> displayModes = new List<string>
        {
            "", "GRID"
        };

        protected Point topLeft;
        protected Point bottomRight;
        protected double gridWidth = 0;

        internal ScriptingMap(ScriptingEngine engine, string name, string type, string[] parameters, string displayMode, string[] displayParameters)
            : base(engine, name, type, parameters, displayMode, displayParameters)
        { }

        public override void CheckConstructorSyntax()
        {
            if (!types.Contains(type))
                throw new ArgumentException("Unknown map type '" + type + "'");

            switch (type)
            {
                case "BITMAP":
                    if (parameters.Length != 1)
                        throw new ArgumentException("Syntax error in bitmap definition");

                    //load the georeferenced image to retrieve top-left and bottom-right corners
                    var map = new GeoreferencedImage(Path.Combine(Directory.GetCurrentDirectory(), parameters[0]));
                    topLeft = new Point(DateTime.Now, engine.Settings.Datum, engine.Settings.UtmZone, map.TopLeft.X, map.TopLeft.Y, 0, engine.Settings.Datum, engine.Settings.UtmZone);
                    bottomRight = new Point(DateTime.Now, engine.Settings.Datum, engine.Settings.UtmZone, map.BottomRight.X, map.BottomRight.Y, 0, engine.Settings.Datum, engine.Settings.UtmZone);

                    break;

                case "BLANK":
                    if (parameters.Length != 2)
                        throw new ArgumentException("Syntax error in blank map definition");

                    if (!engine.Heap.ContainsKey(parameters[0]) || !engine.Heap.ContainsKey(parameters[1]))
                        throw new ArgumentException("Undefined point '" + parameters[0] + "'");

                    var tl = (ScriptingPoint)engine.Heap[parameters[0]];
                    var br = (ScriptingPoint)engine.Heap[parameters[1]];
                    topLeft = tl.Point;
                    bottomRight = br.Point;

                    break;
            }

            engine.Settings.TopLeft = topLeft;
            engine.Settings.BottomRight = bottomRight;
        }

        public override void CheckDisplayModeSyntax()
        {
            if (!displayModes.Contains(displayMode))
                throw new ArgumentException("Unknown display mode '" + displayMode + "'");

            switch (displayMode)
            {
                case "GRID":
                    if (displayParameters.Length != 1)
                        throw new ArgumentException("Syntax error");

                    gridWidth = ParseDouble(displayParameters[0]);
                    break;
            }
        }

        public override MapOverlay GetOverlay()
        {
            return null;
        }

        public void InitializeMapViewer(MapViewerControl map)
        {
            map.ClearOverlays();

            if (type == "BITMAP")
                map.LoadBitmap(Path.Combine(Directory.GetCurrentDirectory(), parameters[0]));
            else
                map.LoadBlank(new System.Windows.Point(topLeft.Easting, topLeft.Northing), new System.Windows.Point(bottomRight.Easting, bottomRight.Northing));

            if (gridWidth > 0)
                map.AddOverlay(new CoordinateGridOverlay(gridWidth));
        }

        public override string ToString()
        {
            return "MAP " + base.ToString();
        }

        /// <summary>Checks if a point is inside the map boundaries
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public bool IsInside(Point p)
        {
            return (p.Easting >= topLeft.Easting && p.Easting <= bottomRight.Easting && p.Northing >= bottomRight.Northing && p.Northing <= topLeft.Northing);
        }
    }
}

