using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace AXToolbox.MapViewer
{
    public class MapViewerControl : ContentControl, INotifyPropertyChanged
    {
        protected bool mapLoaded = false;
        
        //bitmap size
        protected double BitmapWidth { get; set; }
        protected double BitmapHeight { get; set; }

        //map center
        protected Point BitmapCenter { get; set; }
        protected Point MapCenter { get; set; }

        //zoom parameters
        public double MaxZoom { get; set; }
        public double MinZoom { get; set; }
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

        //bitmap transformation
        protected MapTransform mapTransform;

        //WPF transforms
        protected TranslateTransform translateTransform;
        protected ScaleTransform zoomTransform;
        protected TransformGroup mapTransformGroup;
        protected TransformGroup overlayTransformGroup;

        //mouse pan parameters
        protected Point startPosition;
        protected Point startOffset;

        public MapViewerControl()
        {
            UseLayoutRounding = true;
            ClipToBounds = true;

            startPosition = new Point(0, 0);
            overlays = new List<MapOverlay>();

            DefaultZoomFactor = 1.1;
            zoomLevel = 1;
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            // set up layout
            mapCanvas = new Canvas();
            overlaysCanvas = new Canvas();

            var grid = new Grid();
            grid.Children.Add(mapCanvas);
            grid.Children.Add(overlaysCanvas);

            this.AddChild(grid);

            // add transforms
            translateTransform = new TranslateTransform();
            zoomTransform = new ScaleTransform();

            mapTransformGroup = new TransformGroup();
            mapTransformGroup.Children.Add(zoomTransform);
            mapTransformGroup.Children.Add(translateTransform);
            mapCanvas.RenderTransform = mapTransformGroup;

            overlayTransformGroup = new TransformGroup();
            overlayTransformGroup.Children.Add(translateTransform);


            // events
            Focusable = true;
            MouseLeftButtonDown += new MouseButtonEventHandler(source_MouseLeftButtonDown);
            MouseMove += new MouseEventHandler(source_MouseMove);
            MouseLeftButtonUp += new MouseButtonEventHandler(source_MouseLeftButtonUp);
            MouseWheel += new MouseWheelEventHandler(source_MouseWheel);
            SizeChanged += new SizeChangedEventHandler(source_SizeChanged);
            KeyDown += new KeyEventHandler(source_KeyDown);
        }

        /// <summary>Load a calibrated image file as map</summary>
        /// <param name="bitmapFileName">
        /// Bitmap file name. An ESRI world file following the standard naming conventions must exist.
        /// http://en.wikipedia.org/wiki/World_file
        /// </param>
        public void LoadBitmap(string bitmapFileName)
        {
            try
            {
                //Load the bitmap file
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.UriSource = new Uri(bitmapFileName);
                bmp.EndInit();

                var img = new Image() { Source = bmp };

                BitmapWidth = bmp.Width;
                BitmapHeight = bmp.Height;
                mapCanvas.Children.Clear(); //delete blank map
                mapCanvas.Children.Add(img);
                mapLoaded = true;

                //Load and parse the world file
                //first naming convention
                var bitmapFileExtension = System.IO.Path.GetExtension(bitmapFileName);
                var worldFileExtension = "." + bitmapFileExtension[1] + bitmapFileExtension[3] + "w";
                var worldFileName = System.IO.Path.ChangeExtension(bitmapFileName, worldFileExtension);
                if (!System.IO.File.Exists(worldFileName))
                {
                    //second naming convention
                    worldFileExtension = bitmapFileExtension + "w";
                    worldFileName = System.IO.Path.ChangeExtension(bitmapFileName, worldFileExtension);
                }
                mapTransform = new MapTransform(worldFileName);
                ComputeMapConstants();
                Reset();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public void LoadBlank(Point center)
        {
            //load blank map
            BitmapWidth = 1e4;
            BitmapHeight = 1e4;
            mapCanvas.Children.Add(new Border() { Width = BitmapWidth, Height = BitmapHeight, Background = Brushes.White });
            mapTransform = new MapTransform(10, 0, 0, -10, center.X - 5e4, center.Y + 5e4);
            ComputeMapConstants();
            Reset();
        }

        /// <summary>Center the desired point</summary>
        /// <param name="mapPosition">Desired point in map coordinates</param>
        public void PanTo(Point mapPosition)
        {
            var localPosition = FromMapToLocal(mapPosition);
            var offset = new Point(translateTransform.X, translateTransform.Y);
            var mapViewerCenter = new Point(ActualWidth / 2, ActualHeight / 2);
            var displacement = new Vector(mapViewerCenter.X - localPosition.X + offset.X, mapViewerCenter.Y - localPosition.Y + offset.Y);

            LocalPanTo(displacement);
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

        ///<summary>Reset to default zoom level and centered content</summary>
        public void Reset()
        {
            ZoomTo(MinZoom, MapCenter);
        }

        /// <summary>Add a new overlay to MapViewer</summary>
        /// <param name="overlay"></param>
        public void AddOverlay(MapOverlay overlay)
        {
            //Load a blank map if adding the first overlay and no map is loaded
            if (overlays.Count == 0 && !mapLoaded)
                LoadBlank(overlay.Position);

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
        /// <summary>Removes all overlays from MapViewer</summary>
        public void ClearOverlays()
        {
            foreach (var overlay in overlays)
            {
                overlaysCanvas.Children.Remove(overlay);
                overlay.Map = null;
            }
            overlays.Clear();
        }

        /// <summary>Convert map coordinates to local (relative to mapviewer)</summary>
        /// <param name="mapPosition"></param>
        /// <returns></returns>
        public Point FromMapToLocal(Point mapPosition)
        {
            var bitmapPosition = mapTransform.FromMapToBitmap(mapPosition);
            var localPosition = mapTransformGroup.Transform(bitmapPosition);
            return localPosition;
        }
        /// <summary>Convert to local coordinates (relative to mapviewer) to map</summary>
        /// <param name="localPosition"></param>
        /// <returns></returns>
        public Point FromLocalToMap(Point localPosition)
        {
            var bitmapPosition = mapTransformGroup.Inverse.Transform(localPosition);
            var mapPosition = mapTransform.FromBitmapToMap(bitmapPosition);
            return mapPosition;
        }

        /// <summary>Pan the content (absolute)</summary>
        /// <param name="localDisplacement">Displacement from origin in local coords</param>
        private void LocalPanTo(Vector localDisplacement)
        {
            translateTransform.X = localDisplacement.X;
            translateTransform.Y = localDisplacement.Y;
            RefreshOverlays(false);
        }
        /// <summary>Pan the content (relative)</summary>
        /// <param name="localDisplacement">Displacement in local coords</param>
        private void LocalPan(Vector localDisplacement)
        {
            translateTransform.X += localDisplacement.X;
            translateTransform.Y += localDisplacement.Y;
            RefreshOverlays(false);
        }

        /// <summary>Absolute zoom into or out of the content relative to a point</summary>
        /// <param name="zoom">Desired zoom level</param>
        /// <param name="localPosition">Point in local coords to use as zoom center</param>
        private void DoZoom(double zoom, Point localPosition)
        {
            var deltaZoom = zoom / zoomLevel;
            DoIncZoom(deltaZoom, localPosition);
        }
        /// <summary>Incremental zoom into or out of the content relative to a point</summary>
        /// <param name="deltaZoom">Factor to mutliply the zoom level by</param>
        /// <param name="localPosition">Pointin local coords to use as zoom center</param>
        private void DoIncZoom(double deltaZoom, Point localPosition)
        {
            var currentZoom = zoomLevel;
            zoomLevel = Math.Max(MinZoom, Math.Min(MaxZoom, zoomLevel * deltaZoom));

            var bitmapPosition = mapTransformGroup.Inverse.Transform(localPosition);

            translateTransform.X = localPosition.X - bitmapPosition.X * zoomLevel;
            translateTransform.Y = localPosition.Y - bitmapPosition.Y * zoomLevel;

            zoomTransform.ScaleX = zoomLevel;
            zoomTransform.ScaleY = zoomLevel;

            RefreshOverlays(currentZoom != zoomLevel);

            if (zoomLevel != currentZoom)
                NotifyPropertyChanged("ZoomLevel");
        }

        /// <summary>Refresh the overlays position and size after a pan or zoom</summary>
        /// <param name="regenerateTrackShapes">For optimal performance must be true if zoom level changed, false otherwise</param>
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

        private void ComputeMapConstants()
        {
            BitmapCenter = new Point(BitmapWidth / 2, BitmapHeight / 2);
            MapCenter = mapTransform.FromBitmapToMap(BitmapCenter);

            MaxZoom = Math.Max(mapTransform.PixelWidth, mapTransform.PixelHeight);
            MinZoom = Math.Min(ActualWidth / BitmapWidth, ActualHeight / BitmapHeight); // fit to viewer
        }

        #region "Event handlers"
        protected void source_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            //Zoom to the mouse pointer position
            //if ((Keyboard.Modifiers & ModifierKeys.Control) > 0)
            {
                //Compute zoom delta according to wheel direction
                double zoomFactor = DefaultZoomFactor;
                if (e.Delta < 0)
                    zoomFactor = 1.0 / DefaultZoomFactor;

                //Perform the zoom
                var position = e.GetPosition(this);
                DoIncZoom(zoomFactor, position);
            }
        }
        protected void source_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            //Get keyboard focus
            Keyboard.Focus(this);

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
                LocalPanTo(displacement);
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
        protected void source_SizeChanged(object sender, SizeChangedEventArgs e)
        {

            if (!(e.PreviousSize.Height == 0 && e.PreviousSize.Width == 0))
            {
                var previousLocalCenter = new Point(e.PreviousSize.Width / 2, e.PreviousSize.Height / 2);
                var previousMapCenter = FromLocalToMap(previousLocalCenter);
                ComputeMapConstants();
                PanTo(previousMapCenter);
            }
        }
        protected void source_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Escape:
                    Reset();
                    break;
                case Key.OemPlus:
                case Key.Add:
                    ZoomLevel *= DefaultZoomFactor;
                    break;
                case Key.OemMinus:
                case Key.Subtract:
                    ZoomLevel /= DefaultZoomFactor;
                    break;
                case Key.OemPeriod:
                    ZoomLevel = 1;
                    break;
                case Key.Up:
                    LocalPan(new Vector(0, -50));
                    break;
                case Key.Down:
                    LocalPan(new Vector(0, 50));
                    break;
                case Key.Left:
                    LocalPan(new Vector(-50, 0));
                    break;
                case Key.Right:
                    LocalPan(new Vector(50, 0));
                    break;
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
