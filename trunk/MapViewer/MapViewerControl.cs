using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Linq;

namespace AXToolbox.MapViewer
{
    public class MapViewerControl : ContentControl, INotifyPropertyChanged
    {
        protected bool mapLoaded = false;

        //Transformation parameters 
        //transform matrix
        protected double TfwA { get; set; }
        protected double TfwD { get; set; }
        protected double TfwB { get; set; }
        protected double TfwE { get; set; }
        //inverse transform matrix
        protected double I11 { get; set; }
        protected double I12 { get; set; }
        protected double I21 { get; set; }
        protected double I22 { get; set; }
        //deltas
        protected double TfwC { get; set; }
        protected double TfwF { get; set; }

        //bitmap size
        protected double BitmapWidth { get; set; }
        protected double BitmapHeight { get; set; }

        //map center
        protected Point LocalCenter { get; set; }
        protected Point MapCenter { get; set; }

        //zoom parameters
        public double MaxZoom { get; protected set; }
        public double MinZoom { get; protected set; }
        public double DefaultZoomFactor { get; set; }
        protected double zoomLevel;
        public double ZoomLevel
        {
            get { return zoomLevel; }
            set
            {
                if (zoomLevel != value)
                {
                    Zoom(value);
                }
            }
        }

        protected Canvas mapCanvas;
        protected Canvas overlaysCanvas;
        protected List<MapOverlay> overlays;

        //WPF transforms
        protected TranslateTransform translateTransform;
        protected ScaleTransform zoomTransform;
        protected TransformGroup mTransformGroup;
        protected TransformGroup oTransformGroup;

        //pan parameters
        protected Point startPosition;
        protected Point startOffset;

        public MapViewerControl()
        {
            startPosition = new Point(0, 0);
            overlays = new List<MapOverlay>();

            DefaultZoomFactor = 1.1;
            zoomLevel = 1;


            // set up layout
            mapCanvas = new Canvas();
            overlaysCanvas = new Canvas();

            var grid = new Grid();
            grid.Children.Add(mapCanvas);
            grid.Children.Add(overlaysCanvas);

            this.AddChild(grid);
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            Setup(this);
        }

        protected void Setup(FrameworkElement control)
        {
            // add transforms
            translateTransform = new TranslateTransform();
            zoomTransform = new ScaleTransform();

            mTransformGroup = new TransformGroup();
            mTransformGroup.Children.Add(zoomTransform);
            mTransformGroup.Children.Add(translateTransform);
            mapCanvas.RenderTransform = mTransformGroup;

            oTransformGroup = new TransformGroup();
            oTransformGroup.Children.Add(translateTransform);


            // events
            Focusable = true;
            MouseLeftButtonDown += new MouseButtonEventHandler(source_MouseLeftButtonDown);
            MouseMove += new MouseEventHandler(source_MouseMove);
            MouseLeftButtonUp += new MouseButtonEventHandler(source_MouseLeftButtonUp);
            MouseWheel += new MouseWheelEventHandler(source_MouseWheel);


            //load blank map
            BitmapWidth = 1e4;
            BitmapHeight = 1e4;
            mapCanvas.Children.Add(new Border() { Width = BitmapWidth, Height = BitmapHeight, Background = Brushes.White });
        }

        /// <summary>Load a calibrated image file as map</summary>
        /// <param name="bitmapFileName">
        /// Bitmap file name. A .tfw (ESRI world file) with the same name must exist.
        /// http://en.wikipedia.org/wiki/World_file
        /// </param>
        public void Load(string bitmapFileName)
        {
            try
            {
                //throw new Exception();

                //Load and parse the world file
                //todo: honor world file naming convention: http://en.wikipedia.org/wiki/World_file#The_filename
                var tfwFileName = System.IO.Path.ChangeExtension(bitmapFileName, ".tfw");
                var lines = System.IO.File.ReadAllLines(tfwFileName);

                ComputeMapTransformParameters(double.Parse(lines[0]), double.Parse(lines[1]), double.Parse(lines[2]), double.Parse(lines[3]), double.Parse(lines[4]), double.Parse(lines[5]));

                //Load the bitmap file
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.UriSource = new Uri(bitmapFileName);
                bmp.EndInit();

                var img = new Image();
                img.Source = bmp;

                BitmapWidth = bmp.Width;
                BitmapHeight = bmp.Height;
                mapCanvas.Children.Clear(); //delete blank map
                mapCanvas.Children.Add(img);
                mapLoaded = true;

                Reset();
            }
            catch
            {
            }
        }


        /// <summary>Center the desired point</summary>
        /// <param name="mapPosition">Desired point in map coordinates</param>
        public void PanTo(Point mapPosition)
        {
            var localPosition = FromMapToLocal(mapPosition);
            var offset = new Point(translateTransform.X, translateTransform.Y);
            var mapViewerCenter = new Point(ActualWidth / 2, ActualHeight / 2);
            var displacement = new Vector(mapViewerCenter.X - localPosition.X + offset.X, mapViewerCenter.Y - localPosition.Y + offset.Y);

            DoPan(displacement);
        }
        /// <summary>Zoom in/out and center to a desired point</summary>
        /// <param name="zoom">Absolute zoom level</param>
        /// <param name="mapPosition">Desired point in map coordinates</param>
        public void ZoomTo(double zoom, Point mapPosition)
        {
            var localPosition = FromMapToLocal(mapPosition);
            var offset = new Point(translateTransform.X, translateTransform.Y);
            var mapViewerCenter = new Point(ActualWidth / 2, ActualHeight / 2);
            var displacement = new Point(mapViewerCenter.X - localPosition.X + offset.X, mapViewerCenter.Y - localPosition.Y + offset.Y);

            translateTransform.X = displacement.X;
            translateTransform.Y = displacement.Y;

            DoIncZoom(zoom / zoomLevel, mapViewerCenter);
        }
        ///<summary>Absolute zoom into or out of the content relative to MapViewer center</summary>
        /// <param name="zoom">Desired zoom level</param>
        public void Zoom(double zoom)
        {
            var deltaZoom = zoom / zoomLevel;
            var mapViewerCenter = new Point(ActualWidth / 2, ActualHeight / 2);
            DoIncZoom(deltaZoom, mapViewerCenter);
        }
        ///<summary>Incremental zoom into or out of the content relative to MapViewer center</summary>
        /// <param name="deltaZoom">Factor to mutliply the zoom level by</param>
        public void IncZoom(double deltaZoom)
        {
            var mapViewerCenter = new Point(ActualWidth / 2, ActualHeight / 2);
            DoIncZoom(deltaZoom, mapViewerCenter);
        }

        /// <summary>Reset to default zoom level and centered content</summary>
        public void Reset()
        {
            //TODO: zoom to fit map
            ZoomTo(MinZoom, MapCenter);
        }

        /// <summary>Add a new overlay to MapViewer</summary>
        /// <param name="overlay"></param>
        public void AddOverlay(MapOverlay overlay)
        {
            //Load a blank map if adding the first overlay and no map is loaded
            if (overlays.Count == 0 && !mapLoaded)
            {
                var center = overlay.Position;
                ComputeMapTransformParameters(10, 0, 0, -10, center.X - 5e4, center.Y + 5e4);
                Reset();
            }

            overlay.Map = this;
            overlaysCanvas.Children.Add(overlay);
            overlays.Add(overlay);
        }
        /// <summary>Remove an overlay from MapViewer</summary>
        /// <param name="overlay"></param>
        public void RemoveOverlay(MapOverlay overlay)
        {
            overlays.Remove(overlay);
            overlaysCanvas.Children.Remove(overlay);
            overlay.Map = null;
        }


        /// <summary>Pan the content</summary>
        /// <param name="displacement">Displacement in local coords</param>
        protected void DoPan(Vector displacement)
        {
            translateTransform.X = displacement.X;
            translateTransform.Y = displacement.Y;
            RefreshOverlays(false);
        }
        /// <summary>Absolute zoom into or out of the content relative to a point</summary>
        /// <param name="zoom">Desired zoom level</param>
        /// <param name="position">Point in local coords to use as zoom center</param>
        protected void DoZoom(double zoom, Point position)
        {
            var deltaZoom = zoom / zoomLevel;
            DoIncZoom(deltaZoom, position);
        }
        /// <summary>Incremental zoom into or out of the content relative to a point</summary>
        /// <param name="deltaZoom">Factor to mutliply the zoom level by</param>
        /// <param name="position">Pointin local coords to use as zoom center</param>
        protected void DoIncZoom(double deltaZoom, Point position)
        {
            var currentZoom = zoomLevel;
            zoomLevel = Math.Max(MinZoom, Math.Min(MaxZoom, zoomLevel * deltaZoom));

            var untransformedPosition = mTransformGroup.Inverse.Transform(position);

            translateTransform.X = position.X - untransformedPosition.X * zoomLevel;
            translateTransform.Y = position.Y - untransformedPosition.Y * zoomLevel;

            zoomTransform.ScaleX = zoomLevel;
            zoomTransform.ScaleY = zoomLevel;

            RefreshOverlays(currentZoom != zoomLevel);

            if (zoomLevel != currentZoom)
                NotifyPropertyChanged("ZoomLevel");
        }

        /// <summary>Refresh the overlays position and size after a pan or zoom</summary>
        /// <param name="regenerateTrackShapes">For optimal performance must be true if zoom level changed, false otherwise</param>
        protected void RefreshOverlays(bool regenerateTrackShapes)
        {
            foreach (var o in overlays)
            {
                if (regenerateTrackShapes)
                {
                    o.RefreshShape();
                }
                o.UpdateLocalPosition();
            }
        }

        /// <summary>Compute the transformation parameters to convert bitmap coordinates from/to map coordinates</summary>
        /// <param name="a">1st transform matrix coefficient</param>
        /// <param name="d">2nd transform matrix coefficient</param>
        /// <param name="b">3rd transform matrix coefficient</param>
        /// <param name="e">4th transform matrix coefficient</param>
        /// <param name="c">delta x</param>
        /// <param name="f">delta y</param>
        private void ComputeMapTransformParameters(double a, double d, double b, double e, double c, double f)
        {
            //transform matrix
            //http://en.wikipedia.org/wiki/World_file
            TfwA = a;
            TfwD = d;
            TfwB = b;
            TfwE = e;

            TfwC = c;
            TfwF = f;

            //inverse transform matrix
            //http://en.wikipedia.org/wiki/Invertible_matrix#Inversion_of_2.C3.972_matrices
            var rdet = 1 / (a * e - d * b);
            I11 = rdet * e;
            I12 = -rdet * d;
            I21 = -rdet * b;
            I22 = rdet * a;

            var map = mapCanvas.Children[0];
            LocalCenter = new Point(BitmapWidth / 2, BitmapHeight / 2);
            MapCenter = FromLocalToMap(LocalCenter);

            //TODO: compute according to map parameters and screen size
            MaxZoom = 100;
            MinZoom = .01;
        }

        /// <summary>Converts units from local (screen) coords to map coords</summary>
        /// <param name="localCoords"></param>
        /// <returns></returns>
        public Point FromLocalToMap(Point localCoords)
        {
            var bitmapCoords = mTransformGroup.Inverse.Transform(localCoords);

            var mapX = TfwA * bitmapCoords.X + TfwB * bitmapCoords.Y + TfwC;
            var mapY = TfwD * bitmapCoords.X + TfwE * bitmapCoords.Y + TfwF;

            return new Point(mapX, mapY);
        }
        /// <summary>Converts units from map coords to local (screen)</summary>
        /// <param name="mapCoords"></param>
        /// <returns></returns>
        public Point FromMapToLocal(Point mapCoords)
        {
            var tX = mapCoords.X - TfwC;
            var tY = mapCoords.Y - TfwF;

            var bitmapX = I11 * tX + I12 * tY;
            var bitmapY = I21 * tX + I22 * tY;

            var bitmapCoords = new Point(bitmapX, bitmapY);
            return mTransformGroup.Transform(bitmapCoords);
        }


        #region "Event handlers"
        protected void source_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            //Zoom to the mouse pointer position
            if ((Keyboard.Modifiers & ModifierKeys.Control) > 0)
            {
                //Compute zoom delta according to wheel direction
                double zoomFactor = DefaultZoomFactor;
                if (e.Delta > 0)
                    zoomFactor = 1.0 / DefaultZoomFactor;

                //Perform the zoom
                var position = e.GetPosition(this);
                DoIncZoom(zoomFactor, position);
            }
        }
        protected void source_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            //Save starting point, used later when determining how much to scroll
            startPosition = e.GetPosition(this);
            startOffset = new Point(translateTransform.X, translateTransform.Y);
            CaptureMouse();
            Cursor = Cursors.ScrollAll;
        }
        protected void source_MouseMove(object sender, MouseEventArgs e)
        {
            if (IsMouseCaptured)
            {
                //Move the content
                var position = e.GetPosition(this);
                var displacement = new Vector(position.X - startPosition.X + startOffset.X, position.Y - startPosition.Y + startOffset.Y);
                DoPan(displacement);
            }
        }
        protected void source_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (IsMouseCaptured)
            {
                //Reset the cursor and release the mouse pointer
                Cursor = Cursors.Arrow;
                ReleaseMouseCapture();
            }
        }
        #endregion

        #region INotifyPropertyChanged Members
        public event PropertyChangedEventHandler PropertyChanged;
        protected void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }
        #endregion
    }
}
