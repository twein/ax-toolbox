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

        protected AXPoint topLeft;
        protected AXPoint bottomRight;
        protected double gridWidth = 0;

        internal ScriptingMap(ScriptingEngine engine, string name, string type, string[] parameters, string displayMode, string[] displayParameters)
            : base(engine, name, type, parameters, displayMode, displayParameters)
        { }


        public override void CheckConstructorSyntax()
        {
            if (!types.Contains(ObjectType))
                throw new ArgumentException("Unknown map type '" + ObjectType + "'");

            switch (ObjectType)
            {
                case "BITMAP":
                    if (ObjectParameters.Length != 1)
                        throw new ArgumentException("Syntax error in bitmap definition");

                    //load the georeferenced image to retrieve top-left and bottom-right corners
                    var map = new GeoreferencedImage(Path.Combine(Directory.GetCurrentDirectory(), ObjectParameters[0]));
                    topLeft = new AXPoint(DateTime.Now, map.TopLeft.X, map.TopLeft.Y, 0);
                    bottomRight = new AXPoint(DateTime.Now, map.BottomRight.X, map.BottomRight.Y, 0);

                    break;

                case "BLANK":
                    if (ObjectParameters.Length != 2)
                        throw new ArgumentException("Syntax error in blank map definition");

                    if (!Engine.Heap.ContainsKey(ObjectParameters[0]) || !Engine.Heap.ContainsKey(ObjectParameters[1]))
                        throw new ArgumentException("Undefined point '" + ObjectParameters[0] + "'");

                    var tl = (ScriptingPoint)Engine.Heap[ObjectParameters[0]];
                    var br = (ScriptingPoint)Engine.Heap[ObjectParameters[1]];
                    topLeft = tl.Point;
                    bottomRight = br.Point;

                    break;
            }

            Engine.Settings.TopLeft = topLeft;
            Engine.Settings.BottomRight = bottomRight;
        }

        public override void CheckDisplayModeSyntax()
        {
            if (!displayModes.Contains(DisplayMode))
                throw new ArgumentException("Unknown display mode '" + DisplayMode + "'");

            switch (DisplayMode)
            {
                case "GRID":
                    if (DisplayParameters.Length != 1)
                        throw new ArgumentException("Syntax error");

                    gridWidth = ParseDouble(DisplayParameters[0]);
                    break;
            }
        }

        public override MapOverlay GetOverlay()
        {
            return null;
        }

        public void InitializeMapViewer(MapViewerControl map)
        {
            if (ObjectType == "BITMAP")
                map.LoadBitmap(Path.Combine(Directory.GetCurrentDirectory(), ObjectParameters[0]));
            else
                map.LoadBlank(new System.Windows.Point(topLeft.Easting, topLeft.Northing), new System.Windows.Point(bottomRight.Easting, bottomRight.Northing));

            if (gridWidth > 0)
                map.AddOverlay(new CoordinateGridOverlay(gridWidth));
        }

        /// <summary>Checks if a point is inside the map boundaries
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public bool IsInside(AXPoint p)
        {
            return (p.Easting >= topLeft.Easting && p.Easting <= bottomRight.Easting && p.Northing >= bottomRight.Northing && p.Northing <= topLeft.Northing);
        }
    }
}

