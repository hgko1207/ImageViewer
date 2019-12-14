using System;
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

        private double viewerWidth; //화면에 보여지는 이미지 사이즈
        private double viewerHeight; //화면에 보여지는 이미지 사이즈

        private HeaderInfo headerInfo;

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
                headerInfo = gdalReader.GetInfo();

                ImageFormat imageFormat = ImageFormat.Bmp;
                if (fileName.ToLower().Contains(".png") || fileName.ToLower().Contains(".tif"))
                    imageFormat = ImageFormat.Png;
                else if (fileName.ToLower().Contains(".jpg") || fileName.ToLower().Contains(".jpeg"))
                    imageFormat = ImageFormat.Jpeg;

                image = new Image();
                //image.Source = ImageControl.Bitmap2BitmapImage(gdalReader.GetBitmap(0, 0, headerInfo.ImageWidth, headerInfo.ImageHeight), imageFormat);
                image.Source = new BitmapImage(new Uri(fileName));

                image.Width = ImageViewer.ActualWidth;
                image.Height = ImageViewer.ActualHeight;
                image.Stretch = Stretch.Uniform;

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
                ImageViewer.Children.Add(image);

                Console.WriteLine(image.Width + ", " + image.ActualWidth); 
                Console.WriteLine(myScaleTransform.ScaleX + ", " + myScaleTransform.ScaleY);
                Console.WriteLine(myTranslateTransform.X + ", " + myTranslateTransform.Y);

            }
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
                    Vector v = start - e.GetPosition(ImageViewer);
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
                var tt = myTranslateTransform;
                start = e.GetPosition(ImageViewer);
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
