using System;
using System.IO;
using System.Threading;
using AXToolbox.Common;
using AXToolbox.GpsLoggers;
using AXToolbox.MapViewer;

namespace AXToolbox.Scripting
{
    internal class ScriptingMap : ScriptingObject
    {
        internal static ScriptingMap Create(ScriptingEngine engine, ObjectDefinition definition)
        {
            return new ScriptingMap(engine, definition);
        }

        protected ScriptingMap(ScriptingEngine engine, ObjectDefinition definition)
            : base(engine, definition)
        { }


        protected AXPoint topLeft;
        protected AXPoint bottomRight;
        protected double gridWidth = 0;


        public override void CheckConstructorSyntax()
        {
            base.CheckConstructorSyntax();

            switch (Definition.ObjectType)
            {
                default:
                    throw new ArgumentException("Unknown map type '" + Definition.ObjectType + "'");

                case "BITMAP":
                    AssertNumberOfParametersOrDie(Definition.ObjectParameters.Length == 1);

                    //load the georeferenced image to retrieve top-left and bottom-right corners
                    Exception exception = null;
                    var t = new Thread(() =>
                    {
                        try
                        {
                            var map = new GeoreferencedImage(Path.Combine(Directory.GetCurrentDirectory(), Definition.ObjectParameters[0]));
                            topLeft = new AXPoint(DateTime.Now, map.TopLeft.X, map.TopLeft.Y, 0);
                            bottomRight = new AXPoint(DateTime.Now, map.BottomRight.X, map.BottomRight.Y, 0);
                        }
                        catch (Exception ex)
                        {
                            exception = ex;
                        }
                    });
                    // ensure we are doing this in the main thread
                    t.SetApartmentState(ApartmentState.STA);
                    t.Start();
                    t.Join();

                    //throw any exception
                    if (exception != null)
                        throw exception;

                    break;

                case "BLANK":
                    AssertNumberOfParametersOrDie(Definition.ObjectParameters.Length == 2);
                    topLeft = ResolveOrDie<ScriptingPoint>(0).Point;
                    bottomRight = ResolveOrDie<ScriptingPoint>(1).Point;

                    break;
            }

            Engine.Settings.TopLeft = topLeft;
            Engine.Settings.BottomRight = bottomRight;
        }
        public override void CheckDisplayModeSyntax()
        {
            switch (Definition.DisplayMode)
            {
                default:
                    throw new ArgumentException("Unknown display mode '" + Definition.DisplayMode + "'");

                case "GRID":
                    if (Definition.DisplayParameters.Length != 1)
                        throw new ArgumentException("Syntax error");

                    gridWidth = Parsers.ParseDouble(Definition.DisplayParameters[0]);
                    if (gridWidth < 0)
                        throw new ArgumentException("Incorrect grid width.");

                    break;
            }
        }
        public override void Display()
        {
            if (!Engine.MapViewer.IsMapLoaded)
            {
                switch (Definition.ObjectType)
                {
                    case "BITMAP":
                        Engine.MapViewer.LoadBitmap(Path.Combine(Directory.GetCurrentDirectory(), Definition.ObjectParameters[0]));
                        break;

                    case "BLANK":
                        Engine.MapViewer.LoadBlank(topLeft.ToWindowsPoint(), bottomRight.ToWindowsPoint());
                        break;
                }
            }
            if (gridWidth > 0)
                Engine.MapViewer.AddOverlay(new CoordinateGridOverlay(gridWidth) { Layer = (uint)OverlayLayers.Grid });
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

