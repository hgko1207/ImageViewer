using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using ViewerProject.Domain;
using ViewerProject.Event;
using ViewerProject.Views;

namespace ViewerProject
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            EventAggregator.MouseMoveEvent.Subscribe(CanvasMouseMoveEvent);
            EventAggregator.ProgressEvent.Subscribe(ProgressEvent);
            EventAggregator.ImageOpenEvent.Subscribe(ImageOpenEvent);
        }

        private void CommonCommandBindingCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void OpenCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            ImageViewer.OpenExecuted(sender, e);
        }

        private void ComboBoxChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ImageViewer != null)
            {
                ImageViewer.ComboBoxChanged(sender, e);
            }
        }

        private void ZoomFitClick(object sender, RoutedEventArgs e)
        {
            ImageViewer.ZoomFit();
        }

        private void ZoomInClick(object sender, RoutedEventArgs e)
        {
            ImageViewer.ZoomInClick(sender, e);
        }

        private void ZoomOutClick(object sender, RoutedEventArgs e)
        {
            ImageViewer.ZoomOutClick(sender, e);
        }

        private void CanvasMouseMoveEvent(string value)
        {
            PointText.Text = value;
        }

        private void ProgressEvent(int value)
        {
            ImageProgress.Dispatcher.Invoke(() => ImageProgress.Value = value + 1, DispatcherPriority.Background);
        }

        private void ImageOpenEvent(ImageInfo imageInfo)
        {
            List<string> imageList = new List<string>();
            imageList.Add(imageInfo.FileName);

            ImageListBox.ItemsSource = imageList;

            string image = $"Size (X,Y) : ({imageInfo.ImageWidth}, {imageInfo.ImageHeight})\r\n" +
                $"Band : {imageInfo.Band}\r\n" +
                $"File Type : {imageInfo.FileType}\r\n" +
                $"Data Type : {imageInfo.DataType}\r\n" +
                $"Interleave : {imageInfo.Interleave}\r\n" +
                $"Proj : {imageInfo.MapInfo.Projcs}\r\n" +
                $"Unit : {imageInfo.MapInfo.Unit}\r\n";

            MapImageText.Text = image;
        }
    }
}
