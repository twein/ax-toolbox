using System;
using System.Collections.Generic;
using System.IO;
using AXToolbox.Common;
using AXToolbox.MapViewer;
using System.Windows;

namespace AXToolbox.Scripting
{
    public class ScriptingMap : ScriptingObject
    {
        protected AXPoint topLeft;
        protected AXPoint bottomRight;
        protected double gridWidth = 0;

        internal ScriptingMap(ScriptingEngine engine, string name, string type, string[] parameters, string displayMode, string[] displayParameters)
            : base(engine, name, type, parameters, displayMode, displayParameters)
        { }

        public override void CheckConstructorSyntax()
        {
            switch (ObjectType)
            {
                default:
                    throw new ArgumentException("Unknown map type '" + ObjectType + "'");

                case "BITMAP":
                    AssertNumberOfParametersOrDie(ObjectParameters.Length == 1);

                    //load the georeferenced image to retrieve top-left and bottom-right corners
                    var map = new GeoreferencedImage(Path.Combine(Directory.GetCurrentDirectory(), ObjectParameters[0]));
                    topLeft = new AXPoint(DateTime.Now, map.TopLeft.X, map.TopLeft.Y, 0);
                    bottomRight = new AXPoint(DateTime.Now, map.BottomRight.X, map.BottomRight.Y, 0);

                    Engine.MapViewer.LoadBitmap(Path.Combine(Directory.GetCurrentDirectory(), ObjectParameters[0]));

                    break;

                case "BLANK":
                    AssertNumberOfParametersOrDie(ObjectParameters.Length == 2);
                    topLeft = ResolveOrDie<ScriptingPoint>(0).Point;
                    bottomRight = ResolveOrDie<ScriptingPoint>(1).Point;

                    Engine.MapViewer.LoadBlank(topLeft.ToWindowsPoint(), bottomRight.ToWindowsPoint());

                    break;
            }

            Engine.Settings.TopLeft = topLeft;
            Engine.Settings.BottomRight = bottomRight;
        }
        public override void CheckDisplayModeSyntax()
        {
            switch (DisplayMode)
            {
                default:
                    throw new ArgumentException("Unknown display mode '" + DisplayMode + "'");

                case "GRID":
                    if (DisplayParameters.Length != 1)
                        throw new ArgumentException("Syntax error");

                    gridWidth = ParseDouble(DisplayParameters[0]);
                    if (gridWidth < 0)
                        throw new ArgumentException("Incorrect grid width.");

                    break;
            }
        }
        public override void Display()
        {
            if (gridWidth > 0)
                Engine.MapViewer.AddOverlay(new CoordinateGridOverlay(gridWidth) { Layer = 0 });
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

