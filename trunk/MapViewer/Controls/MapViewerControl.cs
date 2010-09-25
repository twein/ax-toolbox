using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.ComponentModel;

namespace AXToolbox.MapViewer
{
    public class MapViewerControl : ContentControl, INotifyPropertyChanged
    {
        public double LeftX { get; private set; }
        public double TopY { get; private set; }
        public double RightX { get; private set; }
        public double BottomY { get; private set; }
        public double UnitsPerPixel { get; private set; }
        public double MaxZoom { get; private set; }
        public double MinZoom { get; private set; }
        public double DefaultZoomFactor { get; set; }
        private double zoomLevel;
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
        public string MapFileName { get; set; }

        private TranslateTransform translateTransform;
        private ScaleTransform zoomTransform;
        private TransformGroup mTransformGroup;
        private TransformGroup oTransformGroup;
        private Canvas mapCanvas;
        private Canvas overlaysCanvas;

        private Point startPosition;
        private Point startOffset;

        private List<MapOverlayControl> overlays;

        public MapViewerControl()
        {
            startPosition = new Point(0, 0);
            MapFileName = "";
            overlays = new List<MapOverlayControl>();

            LeftX = 0;
            TopY = 10E5;
            RightX = 10E5;
            BottomY = 0;

            UnitsPerPixel = 10;
            MinZoom = 1 / UnitsPerPixel;
            MaxZoom = UnitsPerPixel;

            DefaultZoomFactor = 1.1;

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

        void Setup(FrameworkElement control)
        {
            // add transforms
            translateTransform = new TranslateTransform();
            zoomTransform = new ScaleTransform();
            zoomLevel = zoomTransform.ScaleX; ;

            mTransformGroup = new TransformGroup();
            mTransformGroup.Children.Add(zoomTransform);
            mTransformGroup.Children.Add(translateTransform);
            mapCanvas.RenderTransform = mTransformGroup;

            oTransformGroup = new TransformGroup();
            oTransformGroup.Children.Add(translateTransform);
            //overlaysCanvas.RenderTransform = mTransformGroup;


            // events
            Focusable = true;
            MouseLeftButtonDown += new MouseButtonEventHandler(source_MouseLeftButtonDown);
            MouseMove += new MouseEventHandler(source_MouseMove);
            MouseLeftButtonUp += new MouseButtonEventHandler(source_MouseLeftButtonUp);
            MouseWheel += new MouseWheelEventHandler(source_MouseWheel);

            mapCanvas.Children.Add(new Grid() { Width = RightX / UnitsPerPixel, Height = TopY / UnitsPerPixel, Background = Brushes.White });
        }

        /// <summary>Load a calibrated image file as map</summary>
        /// <param name="mapFileName">
        /// Map calibration file name: 
        /// The file contains two lines of text
        /// Line 1: name of the image file (wpf supported types)
        /// Line 2: coordinate of the leftmost pixel, coordinate of the topmost pixel and units per pixel (space separated)
        /// Perfect grid orientation and square pixels of invariant size throughout the map are assumed
        /// Example for an UTM map, units in meters:
        ///   Europeans2010.jpg
        ///   282854.10332446 4634097.33585027 8.9505817351082
        /// </param>
        public void LoadMapImage(string mapFileName)
        {
            try
            {
                // Load the map file
                //throw new Exception();
                var lines = System.IO.File.ReadAllLines(mapFileName);
                var imgFilename = lines[0];
                var parms = lines[1].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                LeftX = double.Parse(parms[0], NumberFormatInfo.InvariantInfo);
                TopY = double.Parse(parms[1], NumberFormatInfo.InvariantInfo);
                UnitsPerPixel = double.Parse(parms[2], NumberFormatInfo.InvariantInfo);

                if (!System.IO.Path.IsPathRooted(imgFilename))
                {
                    //imgFilename = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(path), imgFilename);
                    imgFilename = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(mapFileName), imgFilename);
                }

                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.UriSource = new Uri(imgFilename);
                bmp.EndInit();

                var img = new Image();
                img.Source = bmp;
                mapCanvas.Children.Clear();
                mapCanvas.Children.Add(img);

                RightX = LeftX + img.Source.Width * UnitsPerPixel;
                BottomY = TopY - img.Source.Height * UnitsPerPixel;
                MinZoom = 1 / UnitsPerPixel;
                MaxZoom = 10 * UnitsPerPixel;
            }
            catch
            {
            }
        }

        /// <summary>Center the desired point</summary>
        /// <param name="position">Desired point</param>
        public void PanTo(Point position)
        {
            var offset = new Point(translateTransform.X, translateTransform.Y);
            var mapViewerCenter = new Point(ActualWidth / 2, ActualHeight / 2);
            var displacement = new Vector(mapViewerCenter.X - position.X + offset.X, mapViewerCenter.Y - position.Y + offset.Y);

            DoPan(displacement);
        }
        /// <summary>Zoom in/out and center to a desired point</summary>
        /// <param name="zoom">Absolute zoom level</param>
        /// <param name="position">Point to center to</param>
        public void ZoomTo(double zoom, Point position)
        {
            var offset = new Point(translateTransform.X, translateTransform.Y);
            var mapViewerCenter = new Point(ActualWidth / 2, ActualHeight / 2);
            var displacement = new Point(mapViewerCenter.X - position.X + offset.X, mapViewerCenter.Y - position.Y + offset.Y);

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
        /// <param name="zoomDelta">Factor to mutliply the zoom level by</param>
        public void IncZoom(double zoomDelta)
        {
            var mapViewerCenter = new Point(ActualWidth / 2, ActualHeight / 2);
            DoIncZoom(zoomDelta, mapViewerCenter);
        }

        /// <summary>Reset to default zoom level and centered content</summary>
        public void Reset()
        {
            var mapCenter = new Point(LeftX + (RightX - LeftX) / 2, BottomY + (TopY - BottomY) / 2);
            var center = FromMapToLocal(mapCenter);
            var zoom = Math.Min(ActualWidth * UnitsPerPixel / (RightX - LeftX), ActualHeight * UnitsPerPixel / (TopY - BottomY));

            ZoomTo(zoom, center);
        }

        /// <summary>Converts units from local (screen) coords to map coords</summary>
        /// <param name="localCoords"></param>
        /// <returns></returns>
        public Point FromLocalToMap(Point localCoords)
        {
            var imageCoords = mTransformGroup.Inverse.Transform(localCoords);
            var mapX = LeftX + imageCoords.X * UnitsPerPixel;
            var mapY = TopY - imageCoords.Y * UnitsPerPixel;
            return new Point(mapX, mapY);
        }
        /// <summary>Converts units from map coords to local (screen)</summary>
        /// <param name="mapCoords"></param>
        /// <returns></returns>
        public Point FromMapToLocal(Point mapCoords)
        {
            var imageX = (mapCoords.X - LeftX) / UnitsPerPixel;
            var imageY = (TopY - mapCoords.Y) / UnitsPerPixel;
            var imageCoords = new Point(imageX, imageY);
            return mTransformGroup.Transform(imageCoords);
        }


        /// <summary>Pan the content</summary>
        /// <param name="displacement"></param>
        private void DoPan(Vector displacement)
        {
            translateTransform.X = displacement.X;
            translateTransform.Y = displacement.Y;
            RefreshOverlays(false);
        }
        /// <summary>Absolute zoom into or out of the content relative to a point</summary>
        /// <param name="zoom">Desired zoom level</param>
        /// <param name="position">Point to use as zoom center</param>
        private void DoZoom(double zoom, Point position)
        {
            var deltaZoom = zoom / zoomLevel;
            DoIncZoom(deltaZoom, position);
        }
        /// <summary>Incremental zoom into or out of the content relative to a point</summary>
        /// <param name="zoomDelta">Factor to mutliply the zoom level by</param>
        /// <param name="position">Point to use as zoom center</param>
        private void DoIncZoom(double zoomDelta, Point position)
        {
            var currentZoom = zoomLevel;
            zoomLevel = Math.Max(MinZoom, Math.Min(MaxZoom, zoomLevel * zoomDelta));

            var untransformedPosition = mTransformGroup.Inverse.Transform(position);

            translateTransform.X = position.X - untransformedPosition.X * zoomLevel;
            translateTransform.Y = position.Y - untransformedPosition.Y * zoomLevel;

            zoomTransform.ScaleX = zoomLevel;
            zoomTransform.ScaleY = zoomLevel;

            RefreshOverlays(currentZoom != zoomLevel);

            if (zoomLevel != currentZoom)
                NotifyPropertyChanged("ZoomLevel");
        }

        private void RefreshOverlays(bool regenerateTrackShapes)
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

        public void AddOverlay(MapOverlayControl overlay)
        {
            if (LeftX == 0)
            {
                LeftX = overlay.Position.X - 5e4;
                TopY = overlay.Position.Y + 5e4;

            }

            overlay.Map = this;
            overlaysCanvas.Children.Add(overlay.Shape);
            overlays.Add(overlay);
        }

        #region "Event handlers"
        private void source_MouseWheel(object sender, MouseWheelEventArgs e)
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

        private void source_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            //Save starting point, used later when determining how much to scroll
            startPosition = e.GetPosition(this);
            startOffset = new Point(translateTransform.X, translateTransform.Y);
            CaptureMouse();
            Cursor = Cursors.ScrollAll;
        }
        private void source_MouseMove(object sender, MouseEventArgs e)
        {
            if (IsMouseCaptured)
            {
                //Move the content
                var position = e.GetPosition(this);
                var displacement = new Vector(position.X - startPosition.X + startOffset.X, position.Y - startPosition.Y + startOffset.Y);
                DoPan(displacement);
            }
        }
        private void source_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
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
        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }
        #endregion
    }
}
