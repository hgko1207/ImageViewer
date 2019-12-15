using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ViewerProject.Domain;
using ViewerProject.Utils;

namespace ViewerProject
{
    public partial class MainWindow : Window
    {
        private Image mapImage;

        private Point origin;
        private Point start;

        private ImageInfo imageInfo;

        public double ScreenWidth { get; set; }
        public double ScreenHeight { get; set; }

        public Boundary CanvasBoundary { get; internal set; }

        public MainWindow()
        {
            InitializeComponent();
        }

        private void CommonCommandBindingCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void OpenCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            System.Windows.Forms.OpenFileDialog fileDialog = new System.Windows.Forms.OpenFileDialog();
            //dlg.InitialDirectory = "c:\\";
            fileDialog.Filter = "Image files (*.tif)|*.tif|All Files (*.*)|*.*";
            fileDialog.RestoreDirectory = true;
            if (fileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                ImageViewer.Children.Clear();

                string fileName = fileDialog.FileName;

                GDALReader gdalReader = new GDALReader();
                gdalReader.FileName = fileDialog.FileName;
                gdalReader.Open(fileName);
                imageInfo = gdalReader.GetInfo();

                CanvasBoundary = imageInfo.ImageBoundary;

                ScreenWidth = ImageViewer.ActualWidth;
                ScreenHeight = ImageViewer.ActualHeight;

                ImageFormat imageFormat = ImageFormat.Bmp;
                if (fileName.ToLower().Contains(".png") || fileName.ToLower().Contains(".tif"))
                    imageFormat = ImageFormat.Png;
                else if (fileName.ToLower().Contains(".jpg") || fileName.ToLower().Contains(".jpeg"))
                    imageFormat = ImageFormat.Jpeg;

                mapImage = new Image()
                {
                    Source = ImageControl.Bitmap2BitmapImage(gdalReader.GetBitmap(0, 0, imageInfo.ImageWidth, imageInfo.ImageHeight), imageFormat),
                    Stretch = Stretch.Fill
                };

                //image = new Border()
                //{
                //    Child = myImage
                //};
                //MyImages.Add(image);

                Fit2Frame();
            }
        }

        private bool SetImage()
        {
            try
            {
                ImageViewer.Children.Remove(mapImage);

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

                ImageViewer.Children.Add(mapImage);
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
                canvasWidth = (CanvasBoundary.MaxX - CanvasBoundary.MinX) * viewPixelPerDegreeX;
                canvasHeight = (CanvasBoundary.MaxY - CanvasBoundary.MinY) * viewPixelPerDegreeY;
            }

            var tt = myTranslateTransform;
            if (ScreenWidth > canvasWidth)
                tt.X = (ScreenWidth - canvasWidth) / 2;
            else
                tt.X = -(canvasWidth - ScreenWidth) / 2;

            if (ScreenHeight > canvasHeight)
                tt.Y = (ScreenHeight - canvasHeight) / 2;
            else
                tt.Y = -(canvasHeight - ScreenHeight) / 2;

            return true;
        }

        public bool Fit2Frame()
        {
            if (imageInfo == null)
                return false;

            if (imageInfo.ImageBoundary == null)
            {
                double rate = 1;
                if (ScreenWidth / imageInfo.ViewerWidth < ScreenHeight / imageInfo.ViewerHeight)
                {
                    rate = ScreenWidth / imageInfo.ViewerWidth;
                    imageInfo.ViewerHeight *= ScreenWidth / imageInfo.ViewerWidth;
                    imageInfo.ViewerWidth = ScreenWidth;
                }
                else
                {
                    rate = ScreenHeight / imageInfo.ViewerHeight;
                    imageInfo.ViewerWidth *= ScreenHeight / imageInfo.ViewerHeight;
                    imageInfo.ViewerHeight = ScreenHeight;
                }
            }
            else
            {
                double viewPixelPerDegreeX = imageInfo.ViewerWidth / (imageInfo.ImageBoundary.MaxX - imageInfo.ImageBoundary.MinX);
                double viewPixelPerDegreeY = imageInfo.ViewerHeight / (imageInfo.ImageBoundary.MaxY - imageInfo.ImageBoundary.MinY);
                double canvasWidth = (CanvasBoundary.MaxX - CanvasBoundary.MinX) * viewPixelPerDegreeX;
                double canvasHeight = (CanvasBoundary.MaxY - CanvasBoundary.MinY) * viewPixelPerDegreeY;

                double rate = 1;
                if (ScreenWidth / canvasWidth < ScreenHeight / canvasHeight)
                {
                    rate = ScreenWidth / canvasWidth;
                    canvasHeight *= ScreenWidth / canvasWidth;
                    canvasWidth = ScreenWidth;
                }
                else
                {
                    rate = ScreenHeight / canvasHeight;
                    canvasWidth *= ScreenHeight / canvasHeight;
                    canvasHeight = ScreenHeight;
                }

                viewPixelPerDegreeX = canvasWidth / (CanvasBoundary.MaxX - CanvasBoundary.MinX);
                viewPixelPerDegreeY = canvasHeight / (CanvasBoundary.MaxY - CanvasBoundary.MinY);

                imageInfo.ViewerWidth = (imageInfo.ImageBoundary.MaxX - imageInfo.ImageBoundary.MinX) * viewPixelPerDegreeX;
                imageInfo.ViewerHeight = (imageInfo.ImageBoundary.MaxY - imageInfo.ImageBoundary.MinY) * viewPixelPerDegreeY;
                imageInfo.ImageBoundary.CalculateMargin(CanvasBoundary, viewPixelPerDegreeX, viewPixelPerDegreeY);
            }

            SetImage();

            return SetCenter();
        }

        private BitmapImage GetBitamImage(string fileName)
        {
            BitmapImage bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.UriSource = new Uri(fileName);
            bitmapImage.EndInit();
            return bitmapImage;
        }

        private void ComboBoxChanged(object sender, SelectionChangedEventArgs e)
        {
            if (mapImage != null)
            {
                ComboBox combo = sender as ComboBox;
                ComboBoxItem item = (ComboBoxItem)combo.SelectedItem;

                myRotateTransform.CenterX = ImageViewer.ActualWidth / 2;
                myRotateTransform.CenterY = ImageViewer.ActualHeight / 2;
                myRotateTransform.Angle = Int32.Parse(item.Content.ToString());
            }
        }

        private void CanvasMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (mapImage != null)
            {
                var st = myScaleTransform;
                var tt = myTranslateTransform;

                double zoom = e.Delta > 0 ? .2 : -.2;
                if (!(e.Delta > 0) && (st.ScaleX < .4 || st.ScaleY < .4))
                    return;

                Point relative = e.GetPosition(ImageViewer);
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
                if (ImageViewer.IsMouseCaptured)
                {
                    var tt = myTranslateTransform;
                    Vector v = start - e.GetPosition(this);
                    tt.X = origin.X - v.X;
                    tt.Y = origin.Y - v.Y;
                }

                var position = e.GetPosition(ImageViewer);
                Point imagePoint = ScreenToImage(position);

                PointText.Text = position.X + ", " + position.Y + " => Image Point : " + imagePoint.X + ", " + imagePoint.Y;
            }
        }

        public Point ScreenToImage(Point point)
        {
            return new Point()
            {
                X = point.X / imageInfo.ViewerWidth * imageInfo.ImageWidth,
                Y = point.Y / imageInfo.ViewerHeight * imageInfo.ImageHeight
            };
        }

        private void CanvasMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (mapImage != null)
            {
                Console.WriteLine(mapImage.ActualWidth + ", " + mapImage.ActualHeight);

                var tt = myTranslateTransform;
                start = e.GetPosition(this);
                origin = new Point(tt.X, tt.Y);
                this.Cursor = Cursors.Hand;
                ImageViewer.CaptureMouse();
            }
        }

        private void CanvasMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (mapImage != null)
            {
                this.Cursor = Cursors.Arrow;
                ImageViewer.ReleaseMouseCapture();
            }
        }

        private void ZoomFitClick(object sender, RoutedEventArgs e)
        {
            this.ZoomFit();
        }

        private void ZoomInClick(object sender, RoutedEventArgs e)
        {
            if (mapImage != null)
            {
                var st = myScaleTransform;
                var tt = myTranslateTransform;

                Point relative = new Point(this.ActualWidth / 2, this.ActualHeight / 2);
                double abosuluteX = relative.X * st.ScaleX + tt.X;
                double abosuluteY = relative.Y * st.ScaleY + tt.Y;

                st.ScaleX *= 1.1;
                st.ScaleY *= 1.1;

                tt.X = abosuluteX - relative.X * st.ScaleX;
                tt.Y = abosuluteY - relative.Y * st.ScaleY;
            }
        }

        private void ZoomOutClick(object sender, RoutedEventArgs e)
        {
            if (mapImage != null)
            {
                var st = myScaleTransform;
                var tt = myTranslateTransform;

                Point relative = new Point(this.ActualWidth / 2, this.ActualHeight / 2);
                double abosuluteX = relative.X * st.ScaleX + tt.X;
                double abosuluteY = relative.Y * st.ScaleY + tt.Y;

                st.ScaleX /= 1.1;
                st.ScaleY /= 1.1;

                tt.X = abosuluteX - relative.X * st.ScaleX;
                tt.Y = abosuluteY - relative.Y * st.ScaleY;
            }
        }

        public void ZoomFit()
        {
            if (mapImage != null)
            {
                // reset zoom
                var st = myScaleTransform;
                st.ScaleX = 1.0;
                st.ScaleY = 1.0;

                // reset pan
                //var tt = myTranslateTransform;
                //tt.X = 0.0;
                //tt.Y = 0.0;
                SetCenter();
            }
        }
    }
}
