using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace AXToolbox.MapViewer
{
    public class MapViewerControl : Border, INotifyPropertyChanged
    {
        static Point ORIGIN = new Point(0, 0);

        public bool IsMapLoaded { get; private set; }

        public Point MapTopLeft { get { return geoImage.TopLeft; } }
        public Point MapBottomRight { get { return geoImage.BottomRight; } }

        protected Point pointerPosition;
        protected double zoomLevel;

        protected void SetPointerPosition(Point newPosition)
        {
            if (pointerPosition != newPosition)
            {
                pointerPosition = newPosition;
                NotifyPropertyChanged("PointerPosition");
            }
        }

        //begin INotifyPropertyChanged properties
        public Point PointerPosition
        {
            get { return pointerPosition; }
        }
        public double ZoomLevel
        {
            get { return zoomLevel; }
        }
        public double ZoomLevelPct
        {
            get { return Math.Round(zoomLevel * 100, 2); }
        }
        //end INotifyPropertyChanged properties

        public double MaxZoom { get; set; }
        public double MinZoom { get; set; }
        public double DefaultZoomFactor { get; set; }

        protected Grid mainGrid;
        protected Canvas mapCanvas;
        protected Canvas overlaysCanvas;
        protected List<MapOverlay> overlays;
        protected GeoreferencedImage geoImage;

        //WPF transforms
        protected TranslateTransform translateTransform;
        protected ScaleTransform zoomTransform;
        protected TransformGroup mapTransformGroup;

        //mouse pan parameters
        protected Point startPosition;
        protected Point startOffset;

        public MapViewerControl()
        {
            UseLayoutRounding = true;
            ClipToBounds = true;
            Focusable = true;

            startPosition = ORIGIN;
            overlays = new List<MapOverlay>();

            DefaultZoomFactor = 1.1;
            zoomLevel = 1;
            IsMapLoaded = false;

            // set up layout
            mapCanvas = new Canvas();
            overlaysCanvas = new Canvas();
            mainGrid = new Grid();
            mainGrid.Children.Add(mapCanvas);
            mainGrid.Children.Add(overlaysCanvas);
            Child = mainGrid;

            // add transforms
            translateTransform = new TranslateTransform();
            zoomTransform = new ScaleTransform();

            mapTransformGroup = new TransformGroup();
            mapTransformGroup.Children.Add(zoomTransform);
            mapTransformGroup.Children.Add(translateTransform);
            mapCanvas.RenderTransform = mapTransformGroup;

            // events
            KeyDown += new KeyEventHandler(control_KeyDown);
            MouseLeftButtonDown += new MouseButtonEventHandler(control_MouseLeftButtonDown);
            MouseLeftButtonUp += new MouseButtonEventHandler(control_MouseLeftButtonUp);
            MouseMove += new MouseEventHandler(control_MouseMove);
            MouseWheel += new MouseWheelEventHandler(control_MouseWheel);
            SizeChanged += new SizeChangedEventHandler(control_SizeChanged);
        }


        //bitmaps

        /// <summary>Load a calibrated image file as map</summary>
        /// <param name="bitmapFileName">
        /// Bitmap file name. An ESRI world file following the standard naming conventions must exist.
        /// http://en.wikipedia.org/wiki/World_file
        /// </param>
        public void LoadBitmap(string bitmapFileName)
        {
            try
            {
                Clear();
                geoImage = new GeoreferencedImage(bitmapFileName);
                mapCanvas.Children.Add(geoImage.RawImage);
                ComputeMapConstants();
                IsMapLoaded = true;

                ResetZoom();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        /// <summary>Use a white background as map
        /// </summary>
        /// <param name="topLeft">Top left corner coordinates</param>
        /// <param name="bottomRight">Bottom right corner coordinates</param>
        public void LoadBlank(Point topLeft, Point bottomRight)
        {
            //load blank map
            Clear();
            geoImage = new GeoreferencedImage(topLeft, bottomRight);
            mapCanvas.Children.Add(geoImage.RawImage);
            ComputeMapConstants();
            IsMapLoaded = true;

            ResetZoom();
        }
        /// <summary>Save the actual view to a graphics file.
        /// The extension determines the file type. Supported types are: .bmp, .gif, .jpg, .png and .tif
        /// </summary>
        /// <param name="fileName">desired file path</param>
        public void SaveSnapshot(string fileName)
        {
            var bitmap = new RenderTargetBitmap((int)mainGrid.ActualWidth, (int)mainGrid.ActualHeight, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(mainGrid);
            var frame = BitmapFrame.Create(bitmap);

            BitmapEncoder encoder;
            switch (Path.GetExtension(fileName).ToLower())
            {
                case ".bmp":
                    encoder = new BmpBitmapEncoder();
                    break;
                case ".gif":
                    encoder = new GifBitmapEncoder();
                    break;
                case ".jpg":
                    encoder = new JpegBitmapEncoder();
                    break;
                case ".png":
                    encoder = new PngBitmapEncoder();
                    break;
                case ".tif":
                    encoder = new TiffBitmapEncoder();
                    break;
                default:
                    throw new ArgumentException("Unsupported image type");
            }
            encoder.Frames.Add(frame);

            using (var stream = File.Create(fileName))
            {
                encoder.Save(stream);
            }
        }

        public void Clear()
        {
            IsMapLoaded = false;
            geoImage = null;
            overlays.Clear();
            overlaysCanvas.Children.Clear();
            mapCanvas.Children.Clear();
            zoomLevel = 1;
            NotifyPropertyChanged("ZoomLevel");
            NotifyPropertyChanged("ZoomLevelPct");
        }

        //overlays

        /// <summary>Add a new overlay to MapViewer</summary>
        /// <param name="overlay"></param>
        public void AddOverlay(MapOverlay overlay)
        {
            if (!IsMapLoaded)
                throw new InvalidOperationException("Must load a map before placing overlays");

            overlay.Map = this;

            //insert in the correct layer
            //at top by default
            var layer = -1;
            //find the layer if not default
            if (overlay.Layer < int.MaxValue)
                layer = overlays.FindIndex(o => o.Layer > overlay.Layer);

            if (layer >= 0)
            {
                overlaysCanvas.Children.Insert(layer, overlay);
                overlays.Insert(layer, overlay);
            }
            else
            {
                overlaysCanvas.Children.Add(overlay);
                overlays.Add(overlay);
            }
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


        //pan and zoom

        ///<summary>Reset to default zoom level and centered content</summary>
        public void ResetZoom()
        {
            ZoomTo(MinZoom, geoImage.Center);
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


        //conversions

        /// <summary>Convert map coordinates to local (relative to mapviewer)</summary>
        /// <param name="mapPosition"></param>
        /// <returns></returns>
        public Point FromMapToLocal(Point mapPosition)
        {
            var bitmapPosition = geoImage.FromMapToBitmap(mapPosition);
            var localPosition = mapTransformGroup.Transform(bitmapPosition);
            return localPosition;
        }
        /// <summary>Convert to local coordinates (relative to mapviewer) to map</summary>
        /// <param name="localPosition"></param>
        /// <returns></returns>
        public Point FromLocalToMap(Point localPosition)
        {
            var bitmapPosition = mapTransformGroup.Inverse.Transform(localPosition);
            var mapPosition = geoImage.FromBitmapToMap(bitmapPosition);
            return mapPosition;
        }


        #region "private"
        /// <summary>Pan the content (absolute)</summary>
        /// <param name="localDisplacement">Displacement from origin in local coords</param>
        protected void LocalPanTo(Vector localDisplacement)
        {
            translateTransform.X = localDisplacement.X;
            translateTransform.Y = localDisplacement.Y;
            RefreshOverlays(false);
        }
        /// <summary>Pan the content (relative)</summary>
        /// <param name="localDisplacement">Displacement in local coords</param>
        protected void LocalPan(Vector localDisplacement)
        {
            translateTransform.X += localDisplacement.X;
            translateTransform.Y += localDisplacement.Y;
            RefreshOverlays(false);
        }

        /// <summary>Absolute zoom into or out of the content relative to a point</summary>
        /// <param name="zoom">Desired zoom level</param>
        /// <param name="localPosition">Point in local coords to use as zoom center</param>
        protected void DoZoom(double zoom, Point localPosition)
        {
            var deltaZoom = zoom / zoomLevel;
            DoIncZoom(deltaZoom, localPosition);
        }
        /// <summary>Incremental zoom into or out of the content relative to a point</summary>
        /// <param name="deltaZoom">Factor to mutliply the zoom level by</param>
        /// <param name="localPosition">Pointin local coords to use as zoom center</param>
        protected void DoIncZoom(double deltaZoom, Point localPosition)
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
            {
                NotifyPropertyChanged("ZoomLevel");
                NotifyPropertyChanged("ZoomLevelPct");
            }
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

        protected void ComputeMapConstants()
        {
            MaxZoom = Math.Max(geoImage.PixelWidth, geoImage.PixelHeight);
            MinZoom = Math.Min(ActualWidth / geoImage.BitmapWidth, ActualHeight / geoImage.BitmapHeight); // fit to viewer
        }
        #endregion

        #region "Event handlers"
        protected void control_KeyDown(object sender, KeyEventArgs e)
        {
            if (IsMapLoaded && !IsMouseCaptured)
            {
                switch (e.Key)
                {
                    case Key.Escape:
                        ResetZoom();
                        break;
                    case Key.OemPlus:
                    case Key.Add:
                        Zoom(ZoomLevel * DefaultZoomFactor);
                        break;
                    case Key.OemMinus:
                    case Key.Subtract:
                        Zoom(ZoomLevel / DefaultZoomFactor);
                        break;
                    case Key.OemPeriod:
                        Zoom(1);
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
                    case Key.Multiply:
                        SaveSnapshot("snapshot.png");
                        break;
                }
            }
        }
        protected void control_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (IsMapLoaded)
            {
                //Get keyboard focus
                Keyboard.Focus(this);

                //Save starting point, used later when determining how much to scroll
                startPosition = e.GetPosition(this);

                startOffset = new Point(translateTransform.X, translateTransform.Y);
                CaptureMouse();
                Cursor = Cursors.ScrollAll;
            }
        }
        protected void control_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (IsMouseCaptured)
            {
                //Reset the cursor and release the mouse pointer
                Cursor = Cursors.Arrow;
                ReleaseMouseCapture();
            }
        }
        protected void control_MouseMove(object sender, MouseEventArgs e)
        {
            if (IsMapLoaded)
            {
                var position = e.GetPosition(this);

                if (IsMouseCaptured)
                {
                    //Move the content
                    var displacement = new Vector(position.X - startPosition.X + startOffset.X, position.Y - startPosition.Y + startOffset.Y);
                    LocalPanTo(displacement);
                }
                else
                    SetPointerPosition(FromLocalToMap(position));
            }
            else
                SetPointerPosition(ORIGIN);
        }
        protected void control_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (IsMapLoaded && !IsMouseCaptured)
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
        }
        protected void control_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (IsMapLoaded && !(e.PreviousSize.Height == 0 && e.PreviousSize.Width == 0))
            {
                var previousLocalCenter = new Point(e.PreviousSize.Width / 2, e.PreviousSize.Height / 2);
                var previousMapCenter = FromLocalToMap(previousLocalCenter);
                ComputeMapConstants();
                PanTo(previousMapCenter);
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
