using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using ViewerProject.Domain;
using ViewerProject.Utils;

namespace ViewerProject
{
    public partial class MainWindow : Window
    {
        private Image image;

        private Point origin;
        private Point start;

        private HeaderInfo headerInfo;

        private List<Border> images;

        private GDALReader gdalReader;

        public double ScreenWidth { get; set; }
        public double ScreenHeight { get; set; }

        public Boundary CanvasBoundary { get; internal set; }

        public MainWindow()
        {
            InitializeComponent();

            images = new List<Border>();
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

                gdalReader = new GDALReader();
                gdalReader.FileName = fileDialog.FileName;
                gdalReader.Open(fileName);
                headerInfo = gdalReader.GetInfo();

                ImageFormat imageFormat = ImageFormat.Bmp;
                if (fileName.ToLower().Contains(".png") || fileName.ToLower().Contains(".tif"))
                    imageFormat = ImageFormat.Png;
                else if (fileName.ToLower().Contains(".jpg") || fileName.ToLower().Contains(".jpeg"))
                    imageFormat = ImageFormat.Jpeg;

                image = new Image()
                {
                    Source = ImageControl.Bitmap2BitmapImage(gdalReader.GetBitmap(0, 0, headerInfo.ImageWidth, headerInfo.ImageHeight), imageFormat),
                    //Source = bitmapImage,
                    Stretch = Stretch.Fill
                };

                CanvasBoundary = gdalReader.ImageBoundary;

                //image = new Image();
                //image.Source = ImageControl.Bitmap2BitmapImage(gdalReader.GetBitmap(0, 0, headerInfo.ImageWidth, headerInfo.ImageHeight), imageFormat);
                ////image.Source = GetBitamImage(fileName);

                //image.Width = ImageViewer.ActualWidth;
                //image.Height = ImageViewer.ActualHeight;
                //image.Stretch = Stretch.Uniform;

                //Console.WriteLine(gdalReader.ImageBoundary);

                //ImageBrush uniformBrush = new ImageBrush();
                //uniformBrush.ImageSource = new BitmapImage(new Uri(fileName));
                //uniformBrush.Stretch = Stretch.Uniform;

                //// Freeze the brush (make it unmodifiable) for performance benefits.
                //uniformBrush.Freeze();

                //Rectangle rectangle1 = new Rectangle();
                //rectangle1.Width = ImageViewer.ActualWidth;
                //rectangle1.Height = ImageViewer.ActualHeight;
                //rectangle1.Stroke = Brushes.MediumBlue;
                //rectangle1.StrokeThickness = 1.0;
                //rectangle1.Fill = uniformBrush;

                //Canvas.SetTop(image, 0);
                //Canvas.SetLeft(image, 0);
                //ImageViewer.Children.Add(image);

                //Fit2Frame();
                SetImage(gdalReader, image);
            }
        }

        private bool SetImage(GDALReader gdalReader, Image image)
        {
            try
            {
                image.Width = gdalReader.ViewerWidth;
                image.Height = gdalReader.ViewerHeight;
                image.Stretch = Stretch.Uniform;

                if (gdalReader.ImageBoundary != null)
                {
                    Canvas.SetLeft(image, gdalReader.ImageBoundary.Left);
                    Canvas.SetTop(image, gdalReader.ImageBoundary.Top);
                }
                else
                {
                    Canvas.SetLeft(image, 0);
                    Canvas.SetTop(image, 0);
                }

                ImageViewer.Children.Add(image);
                return true;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
                return false;
            }
        }

        public bool Fit2Frame()
        {
            if (gdalReader.ImageBoundary == null)
            {
                double rate = 1;
                if (ScreenWidth / gdalReader.ViewerWidth < ScreenHeight / gdalReader.ViewerHeight)
                {
                    rate = ScreenWidth / gdalReader.ViewerWidth;
                    gdalReader.ViewerHeight *= ScreenWidth / gdalReader.ViewerWidth;
                    gdalReader.ViewerWidth = ScreenWidth;
                }
                else
                {
                    rate = ScreenHeight / gdalReader.ViewerHeight;
                    gdalReader.ViewerWidth *= ScreenHeight / gdalReader.ViewerHeight;
                    gdalReader.ViewerHeight = ScreenHeight;
                }
                SetImage(gdalReader, image);
            }
            else
            {
                double viewPixelPerDegreeX = gdalReader.ViewerWidth / (gdalReader.ImageBoundary.MaxX - gdalReader.ImageBoundary.MinX);
                double viewPixelPerDegreeY = gdalReader.ViewerHeight / (gdalReader.ImageBoundary.MaxY - gdalReader.ImageBoundary.MinY);
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

                SetImage(gdalReader, image);
            }
            return SetCenter();
        }

        public bool SetCenter()
        {
            double canvasWidth = gdalReader.ViewerWidth;
            double canvasHeight = gdalReader.ViewerHeight;
            if (gdalReader.ImageBoundary != null)
            {
                double viewPixelPerDegreeX = gdalReader.ViewerWidth / (gdalReader.ImageBoundary.MaxX - gdalReader.ImageBoundary.MinX);
                double viewPixelPerDegreeY = gdalReader.ViewerHeight / (gdalReader.ImageBoundary.MaxY - gdalReader.ImageBoundary.MinY);
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
            if (image != null)
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
            if (image != null)
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
            if (image != null)
            {
                if (ImageViewer.IsMouseCaptured)
                {
                    var tt = myTranslateTransform;
                    Vector v = start - e.GetPosition(this);
                    tt.X = origin.X - v.X;
                    tt.Y = origin.Y - v.Y;
                }

                var position = e.GetPosition(ImageViewer);

                Point test = new Point()
                {
                    X = position.X / ImageViewer.ActualWidth * headerInfo.ImageWidth,
                    Y = position.Y / ImageViewer.ActualHeight * headerInfo.ImageHeight
                };

                PointText.Text = (position.X) + ", " + (position.Y) + " ============ " + test.X + ", " + test.Y;
            }
        }

        private void CanvasMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (image != null)
            {
                Console.WriteLine(image.ActualWidth + ", " + image.ActualHeight);

                var tt = myTranslateTransform;
                start = e.GetPosition(this);
                origin = new Point(tt.X, tt.Y);
                this.Cursor = Cursors.Hand;
                ImageViewer.CaptureMouse();
            }
        }

        private void CanvasMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (image != null)
            {
                this.Cursor = Cursors.Arrow;
                ImageViewer.ReleaseMouseCapture();
            }
        }

        private void PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.ZoomFit();
        }

        private void ZoomFitClick(object sender, RoutedEventArgs e)
        {
            this.ZoomFit();
        }

        private void ZoomInClick(object sender, RoutedEventArgs e)
        {
            if (image != null)
            {
                var st = myScaleTransform;
                var tt = myTranslateTransform;

                Point relative = new Point(this.ActualWidth / 2, this.ActualHeight / 2);
                double abosuluteX;
                double abosuluteY;

                abosuluteX = relative.X * st.ScaleX + tt.X;
                abosuluteY = relative.Y * st.ScaleY + tt.Y;

                st.ScaleX *= 1.1;
                st.ScaleY *= 1.1;

                tt.X = abosuluteX - relative.X * st.ScaleX;
                tt.Y = abosuluteY - relative.Y * st.ScaleY;
            }
        }

        private void ZoomOutClick(object sender, RoutedEventArgs e)
        {
            if (image != null)
            {
                var st = myScaleTransform;
                var tt = myTranslateTransform;

                Point relative = new Point(this.ActualWidth / 2, this.ActualHeight / 2);
                double abosuluteX;
                double abosuluteY;

                abosuluteX = relative.X * st.ScaleX + tt.X;
                abosuluteY = relative.Y * st.ScaleY + tt.Y;

                st.ScaleX /= 1.1;
                st.ScaleY /= 1.1;

                tt.X = abosuluteX - relative.X * st.ScaleX;
                tt.Y = abosuluteY - relative.Y * st.ScaleY;
            }
        }

        public void ZoomFit()
        {
            if (image != null)
            {
                // reset zoom
                var st = myScaleTransform;
                st.ScaleX = 1.0;
                st.ScaleY = 1.0;

                // reset pan
                var tt = myTranslateTransform;
                tt.X = 0.0;
                tt.Y = 0.0;
            }
        }
    }
}
