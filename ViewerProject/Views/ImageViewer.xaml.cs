using System;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
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
                Fit2Frame();
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
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                CanvasViewer.Children.Clear();

                string fileName = fileDialog.FileName;

                gdalReader = new GDALReader();
                gdalReader.FileName = fileDialog.FileName;
                gdalReader.Open(fileName);
                imageInfo = gdalReader.GetInfo();

                canvasBoundary = imageInfo.ImageBoundary;

                Debug.WriteLine("GDALReader : " + stopwatch.Elapsed.TotalMilliseconds + " msec");

                ImageFormat imageFormat = ImageFormat.Bmp;
                if (fileName.ToLower().Contains(".png") || fileName.ToLower().Contains(".tif"))
                    imageFormat = ImageFormat.Png;
                else if (fileName.ToLower().Contains(".jpg") || fileName.ToLower().Contains(".jpeg"))
                    imageFormat = ImageFormat.Jpeg;

                System.Drawing.Bitmap bitmap = gdalReader.GetBitmap(0, 0, imageInfo.ImageWidth, imageInfo.ImageHeight);

                Debug.WriteLine("Bitmap : " + stopwatch.Elapsed.TotalMilliseconds + " msec");

                mapImage = new Image()
                {
                    Source = ImageControl.Bitmap2BitmapImage(bitmap, imageFormat),
                    Stretch = Stretch.Fill
                };

                Debug.WriteLine("BitmapImage : " + stopwatch.Elapsed.TotalMilliseconds + " msec");

                Fit2Frame();

                stopwatch.Stop();
                Debug.WriteLine("Fit2Frame : " + stopwatch.Elapsed.TotalMilliseconds + " msec");
            }
        }

        private bool SetImage()
        {
            try
            {
                CanvasViewer.Children.Remove(mapImage);

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

        public bool SetCenter()
        {
            if (imageInfo == null)
                return false;

            double canvasWidth = imageInfo.ViewerWidth;
            double canvasHeight = imageInfo.ViewerHeight;
            if (imageInfo.ImageBoundary != null)
            {
                double viewPixelPerDegreeX = imageInfo.ViewerWidth / (imageInfo.ImageBoundary.MaxX - imageInfo.ImageBoundary.MinX);
                double viewPixelPerDegreeY = imageInfo.ViewerHeight / (imageInfo.ImageBoundary.MaxY - imageInfo.ImageBoundary.MinY);
                canvasWidth = (canvasBoundary.MaxX - canvasBoundary.MinX) * viewPixelPerDegreeX;
                canvasHeight = (canvasBoundary.MaxY - canvasBoundary.MinY) * viewPixelPerDegreeY;
            }

            var tt = TranslateTransform;
            if (screenWidth > canvasWidth)
                tt.X = (screenWidth - canvasWidth) / 2;
            else
                tt.X = -(canvasWidth - screenWidth) / 2;

            if (screenHeight > canvasHeight)
                tt.Y = (screenHeight - canvasHeight) / 2;
            else
                tt.Y = -(canvasHeight - screenHeight) / 2;

            return true;
        }

        public bool Fit2Frame()
        {
            if (imageInfo == null)
                return false;

            if (imageInfo.ImageBoundary == null)
            {
                if (screenWidth / imageInfo.ViewerWidth < screenHeight / imageInfo.ViewerHeight)
                {
                    imageInfo.ViewerHeight *= screenWidth / imageInfo.ViewerWidth;
                    imageInfo.ViewerWidth = screenWidth;
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
                double canvasWidth = (canvasBoundary.MaxX - canvasBoundary.MinX) * viewPixelPerDegreeX;
                double canvasHeight = (canvasBoundary.MaxY - canvasBoundary.MinY) * viewPixelPerDegreeY;

                if (screenWidth / canvasWidth < screenHeight / canvasHeight)
                {
                    canvasHeight *= screenWidth / canvasWidth;
                    canvasWidth = screenWidth;
                }
                else
                {
                    canvasWidth *= screenHeight / canvasHeight;
                    canvasHeight = screenHeight;
                }

                viewPixelPerDegreeX = canvasWidth / (canvasBoundary.MaxX - canvasBoundary.MinX);
                viewPixelPerDegreeY = canvasHeight / (canvasBoundary.MaxY - canvasBoundary.MinY);

                imageInfo.ViewerWidth = (imageInfo.ImageBoundary.MaxX - imageInfo.ImageBoundary.MinX) * viewPixelPerDegreeX;
                imageInfo.ViewerHeight = (imageInfo.ImageBoundary.MaxY - imageInfo.ImageBoundary.MinY) * viewPixelPerDegreeY;
                imageInfo.ImageBoundary.CalculateMargin(canvasBoundary, viewPixelPerDegreeX, viewPixelPerDegreeY);
            }

            SetImage();

            return SetCenter();
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
                var st = ScaleTransform;
                var tt = TranslateTransform;

                double zoom = e.Delta > 0 ? .2 : -.2;
                if (!(e.Delta > 0) && (st.ScaleX < .4 || st.ScaleY < .4))
                    return;

                Point relative = e.GetPosition(CanvasViewer);
                double abosuluteX;
                double abosuluteY;

                abosuluteX = relative.X * st.ScaleX + tt.X;
                abosuluteY = relative.Y * st.ScaleY + tt.Y;

                st.ScaleX += zoom;
                st.ScaleY += zoom;

                tt.X = abosuluteX - relative.X * st.ScaleX;
                tt.Y = abosuluteY - relative.Y * st.ScaleY;
            }
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

                String statusLine = $"Map({lonlat.X}, {lonlat.Y}), Image({imagePoint.X}, {imagePoint.Y}), Display({position.X}, {position.Y})";
                EventAggregator.MouseMoveEvent.Publish(statusLine);
            }
        }

        private void CanvasMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (mapImage != null)
            {
                Console.WriteLine(mapImage.ActualWidth + ", " + mapImage.ActualHeight);

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
                //var tt = myTranslateTransform;
                //tt.X = 0.0;
                //tt.Y = 0.0;
                SetCenter();
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
