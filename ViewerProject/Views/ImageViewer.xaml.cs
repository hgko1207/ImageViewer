using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ViewerProject.Domain;
using ViewerProject.Event;
using ViewerProject.Utils;

namespace ViewerProject.Views
{
    public partial class ImageViewer : UserControl
    {
        private Image mapImage;
        private ImageInfo imageInfo;
        private Boundary canvasBoundary;

        private GDALReader gdalReader;

        private double screenWidth;
        private double screenHeight;

        private Point origin;
        private Point start;

        public ImageViewer()
        {
            InitializeComponent();
            Init();
        }

        private void Init()
        {
            this.Loaded += (object sender, RoutedEventArgs e) =>
            {
                screenWidth = ((ImageViewer)sender).ActualWidth;
                screenHeight = ((ImageViewer)sender).ActualHeight;
            };

            this.SizeChanged += (object sender, SizeChangedEventArgs e) =>
            {
                screenWidth = ActualWidth;
                screenHeight = ActualHeight;
                FitToFrame();
            };
        }

        public void OpenExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            System.Windows.Forms.OpenFileDialog fileDialog = new System.Windows.Forms.OpenFileDialog();
            //dlg.InitialDirectory = "c:\\";
            fileDialog.Filter = "Image files (*.tif)|*.tif|All Files (*.*)|*.*";
            fileDialog.RestoreDirectory = true;
            if (fileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                ZoomFit();
                CanvasViewer.Children.Clear();

                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                string filePath = fileDialog.FileName;

                gdalReader = new GDALReader();
                gdalReader.FileName = filePath;
                gdalReader.Open(filePath);
                imageInfo = gdalReader.GetImageInfo();

                canvasBoundary = imageInfo.ImageBoundary;

                Debug.WriteLine("GDALReader : " + stopwatch.Elapsed.TotalMilliseconds + " msec");

                InitLoadImage();

                Debug.WriteLine("BitmapImage : " + stopwatch.Elapsed.TotalMilliseconds + " msec");

                FitToFrame();

                EventAggregator.ImageOpenEvent.Publish(imageInfo);

                stopwatch.Stop();
                Debug.WriteLine("Fit2Frame : " + stopwatch.Elapsed.TotalMilliseconds + " msec");
            }
        }

        private void InitLoadImage()
        {
            System.Drawing.Bitmap bitmap = gdalReader.GetBitmap(0, 0, imageInfo.ImageWidth, imageInfo.ImageHeight, 2);
            mapImage = new Image()
            {
                Source = ImageControl.Bitmap2BitmapImage(bitmap, imageInfo.ImageFormat),
                Stretch = Stretch.Fill
            };
        }

        private void ReloadImage(Point startPoint, Point endPoint, int overview)
        {
            System.Drawing.Bitmap bitmap = gdalReader.GetBitmap((int)startPoint.X, (int)startPoint.Y, (int)endPoint.X, (int)endPoint.Y, overview);
            mapImage.Source = ImageControl.Bitmap2BitmapImage(bitmap, imageInfo.ImageFormat);
        }

        private bool SetImage()
        {
            try
            {
                CanvasViewer.Children.Clear();

                mapImage.Width = imageInfo.ViewerWidth;
                mapImage.Height = imageInfo.ViewerHeight;
                mapImage.Stretch = Stretch.Uniform;

                if (imageInfo.ImageBoundary != null)
                {
                    Canvas.SetLeft(mapImage, imageInfo.ImageBoundary.Left);
                    Canvas.SetTop(mapImage, imageInfo.ImageBoundary.Top);
                }
                else
                {
                    Canvas.SetLeft(mapImage, 0);
                    Canvas.SetTop(mapImage, 0);
                }

                CanvasViewer.Children.Add(mapImage);
                return true;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
                return false;
            }
        }

        public void SetCenter()
        {
            if (imageInfo == null)
                return;

            double imageWidth = imageInfo.ViewerWidth;
            double imageHeight = imageInfo.ViewerHeight;
            if (imageInfo.ImageBoundary != null)
            {
                double viewPixelPerDegreeX = imageInfo.ViewerWidth / (imageInfo.ImageBoundary.MaxX - imageInfo.ImageBoundary.MinX);
                double viewPixelPerDegreeY = imageInfo.ViewerHeight / (imageInfo.ImageBoundary.MaxY - imageInfo.ImageBoundary.MinY);
                imageWidth = (canvasBoundary.MaxX - canvasBoundary.MinX) * viewPixelPerDegreeX;
                imageHeight = (canvasBoundary.MaxY - canvasBoundary.MinY) * viewPixelPerDegreeY;
            }

            var tt = TranslateTransform;
            if (screenWidth > imageWidth)
                tt.X = (screenWidth - imageWidth) / 2;
            else
                tt.X = -(imageWidth - screenWidth) / 2;

            if (screenHeight > imageHeight)
                tt.Y = (screenHeight - imageHeight) / 2;
            else
                tt.Y = -(imageHeight - screenHeight) / 2;
        }

        public void FitToFrame()
        {
            if (imageInfo == null)
                return;

            if (imageInfo.ImageBoundary == null)
            {
                if (screenWidth / imageInfo.ViewerWidth < screenHeight / imageInfo.ViewerHeight)
                {
                    imageInfo.ViewerWidth = screenWidth;
                    imageInfo.ViewerHeight *= screenWidth / imageInfo.ViewerWidth;
                }
                else
                {
                    imageInfo.ViewerWidth *= screenHeight / imageInfo.ViewerHeight;
                    imageInfo.ViewerHeight = screenHeight;
                }
            }
            else
            {
                double viewPixelPerDegreeX = imageInfo.ViewerWidth / (imageInfo.ImageBoundary.MaxX - imageInfo.ImageBoundary.MinX);
                double viewPixelPerDegreeY = imageInfo.ViewerHeight / (imageInfo.ImageBoundary.MaxY - imageInfo.ImageBoundary.MinY);
                double imageWidth = (canvasBoundary.MaxX - canvasBoundary.MinX) * viewPixelPerDegreeX;
                double imageHeight = (canvasBoundary.MaxY - canvasBoundary.MinY) * viewPixelPerDegreeY;

                if (screenWidth / imageWidth < screenHeight / imageHeight)
                {
                    imageWidth = screenWidth;
                    imageHeight *= screenWidth / imageWidth;
                }
                else
                {
                    imageWidth *= screenHeight / imageHeight;
                    imageHeight = screenHeight;
                }

                viewPixelPerDegreeX = imageWidth / (canvasBoundary.MaxX - canvasBoundary.MinX);
                viewPixelPerDegreeY = imageHeight / (canvasBoundary.MaxY - canvasBoundary.MinY);

                imageInfo.ViewerWidth = (imageInfo.ImageBoundary.MaxX - imageInfo.ImageBoundary.MinX) * viewPixelPerDegreeX;
                imageInfo.ViewerHeight = (imageInfo.ImageBoundary.MaxY - imageInfo.ImageBoundary.MinY) * viewPixelPerDegreeY;
                imageInfo.ImageBoundary.CalculateMargin(canvasBoundary, viewPixelPerDegreeX, viewPixelPerDegreeY);
            }

            SetImage();
            SetCenter();
        }

        public Point ScreenToImage(Point point)
        {
            return new Point()
            {
                X = point.X / imageInfo.ViewerWidth * imageInfo.ImageWidth,
                Y = point.Y / imageInfo.ViewerHeight * imageInfo.ImageHeight
            };
        }

        public void ComboBoxChanged(object sender, SelectionChangedEventArgs e)
        {
            if (mapImage != null)
            {
                ComboBox combo = sender as ComboBox;
                ComboBoxItem item = (ComboBoxItem)combo.SelectedItem;

                RotateTransform.CenterX = CanvasViewer.ActualWidth / 2;
                RotateTransform.CenterY = CanvasViewer.ActualHeight / 2;
                RotateTransform.Angle = Int32.Parse(item.Content.ToString());
            }
        }

        private void CanvasMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (mapImage != null)
            {
                Point point = e.GetPosition(CanvasViewer);
                Zoom(e.Delta, point);

                //var st = ScaleTransform;
                //var tt = TranslateTransform;

                //double zoom = e.Delta > 0 ? .2 : -.2;
                //if (!(e.Delta > 0) && (st.ScaleX < .4 || st.ScaleY < .4))
                //    return;

                //double abosuluteX = point.X * st.ScaleX + tt.X;
                //double abosuluteY = point.Y * st.ScaleY + tt.Y;

                ////st.ScaleX += zoom;
                ////st.ScaleY += zoom;

                //tt.X = abosuluteX - point.X * st.ScaleX;
                //tt.Y = abosuluteY - point.Y * st.ScaleY;
            }
        }

        private void Zoom(int value, Point point)
        {
            double rate = 0.7;
            if (value > 0) //Zoom in, 확대
                rate = 1 / rate;

            double offsetX = Math.Abs(point.X * (1 - rate));
            double offsetY = Math.Abs(point.Y * (1 - rate));

            var tt = TranslateTransform;
            if (value > 0) //확대
            {
                tt.X -= offsetX;
                tt.Y -= offsetY;
            }
            else
            {
                tt.X += offsetX;
                tt.Y += offsetY;
            }

            imageInfo.ViewerWidth *= rate;
            imageInfo.ViewerHeight *= rate;

            double endWidth = imageInfo.ViewerWidth;
            double endHeight = imageInfo.ViewerHeight;

            double startX = 0;
            double startY = 0;
            if (tt.X < 0)
            {
                startX = Math.Abs(tt.X);
                if (imageInfo.ViewerWidth > screenWidth)
                {
                    endWidth = startX + screenWidth;
                }
            }

            if (tt.Y < 0)
            {
                startY = Math.Abs(tt.Y);
                if (imageInfo.ViewerHeight > screenHeight)
                {
                    endHeight = startY + screenHeight;
                }
            }

            Point startPoint = ScreenToImage(new Point(startX, startY));
            Point endPoint = ScreenToImage(new Point(endWidth, endHeight));

            //Console.WriteLine($"Screen : {screenWidth}, {screenHeight}");
            //Console.WriteLine($"Viewer : {imageInfo.ViewerWidth}, {imageInfo.ViewerHeight}");
            //Console.WriteLine($"TranslateTransform : {tt.X}, {tt.Y}");
            //Console.WriteLine($"Screen Start : {startX}, {startY}");
            //Console.WriteLine($"Screen end   : {endWidth}, {endHeight}");
            //Console.WriteLine($"start imagePoint: {startPoint.X}, {startPoint.Y}");
            //Console.WriteLine($"end imagePoint: {endPoint.X}, {endPoint.Y}");

            if (endWidth > screenWidth || endHeight > screenHeight)
            {
                ReloadImage(startPoint, endPoint, 1);
            }
            else if (endWidth <= screenWidth || endHeight <= screenHeight)
            {
                ReloadImage(startPoint, endPoint, 4);
            }

            CanvasViewer.Children.Clear();

            mapImage.Width = imageInfo.ViewerWidth;
            mapImage.Height = imageInfo.ViewerHeight;

            Canvas.SetLeft(mapImage, -((mapImage.Width - (endWidth + startX)) / 2));
            Canvas.SetTop(mapImage, -((mapImage.Height - (endHeight + startY)) / 2));

            CanvasViewer.Children.Add(mapImage);
        }

        private void CanvasMouseMove(object sender, MouseEventArgs e)
        {
            if (mapImage != null)
            {
                if (CanvasViewer.IsMouseCaptured)
                {
                    var tt = TranslateTransform;
                    Vector v = start - e.GetPosition(this);
                    tt.X = origin.X - v.X;
                    tt.Y = origin.Y - v.Y;
                }

                var position = e.GetPosition(CanvasViewer);
                Point imagePoint = ScreenToImage(position);
                Point lonlat = gdalReader.ImageToWorld(imagePoint.X, imagePoint.Y);

                string statusLine = $"Map({lonlat.X}, {lonlat.Y}), Image({imagePoint.X}, {imagePoint.Y}), Display({position.X}, {position.Y})";
                EventAggregator.MouseMoveEvent.Publish(statusLine);
            }
        }

        private void CanvasMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (mapImage != null)
            {
                var tt = TranslateTransform;
                start = e.GetPosition(this);
                origin = new Point(tt.X, tt.Y);
                this.Cursor = Cursors.Hand;
                CanvasViewer.CaptureMouse();
            }
        }

        private void CanvasMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (mapImage != null)
            {
                this.Cursor = Cursors.Arrow;
                CanvasViewer.ReleaseMouseCapture();
            }
        }

        public void ZoomFit()
        {
            if (mapImage != null)
            {
                // reset zoom
                var st = ScaleTransform;
                st.ScaleX = 1.0;
                st.ScaleY = 1.0;

                // reset pan
                //var tt = TranslateTransform;
                //tt.X = 0.0;
                //tt.Y = 0.0;

                InitLoadImage();
                FitToFrame();
            }
        }

        public void ZoomInClick(object sender, RoutedEventArgs e)
        {
            if (mapImage != null)
            {
                var st = ScaleTransform;
                var tt = TranslateTransform;

                Point relative = new Point(this.ActualWidth / 2, this.ActualHeight / 2);
                double abosuluteX = relative.X * st.ScaleX + tt.X;
                double abosuluteY = relative.Y * st.ScaleY + tt.Y;

                st.ScaleX *= 1.1;
                st.ScaleY *= 1.1;

                tt.X = abosuluteX - relative.X * st.ScaleX;
                tt.Y = abosuluteY - relative.Y * st.ScaleY;
            }
        }

        public void ZoomOutClick(object sender, RoutedEventArgs e)
        {
            if (mapImage != null)
            {
                var st = ScaleTransform;
                var tt = TranslateTransform;

                Point relative = new Point(this.ActualWidth / 2, this.ActualHeight / 2);
                double abosuluteX = relative.X * st.ScaleX + tt.X;
                double abosuluteY = relative.Y * st.ScaleY + tt.Y;

                st.ScaleX /= 1.1;
                st.ScaleY /= 1.1;

                tt.X = abosuluteX - relative.X * st.ScaleX;
                tt.Y = abosuluteY - relative.Y * st.ScaleY;
            }
        }
    }
}
