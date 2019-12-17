using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
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
    }
}
