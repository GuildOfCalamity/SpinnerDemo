using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SpinnerDemo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        bool _frameCapture = false;
        System.Windows.Threading.DispatcherTimer? _popTimer = null;
        System.Windows.Threading.DispatcherTimer? _captureTimer = null;

        public MainWindow()
        {
            InitializeComponent();
        }

        void Window_Loaded(object sender, RoutedEventArgs e)
        {
            mainPopup.IsOpen = true;
            this.Icon = "pack://application:,,,/Assets/AppIcon.png".ReturnImageSource();
            if (_frameCapture && _captureTimer == null)
            {
                _captureTimer = new System.Windows.Threading.DispatcherTimer();
                _captureTimer.Interval = TimeSpan.FromSeconds(0.25);
                _captureTimer.Tick += captureTimer_Tick;
                _captureTimer.Start();
            }
        }

        void Window_Unloaded(object sender, RoutedEventArgs e)
        {
            _popTimer?.Stop();
            _captureTimer?.Stop();
        }

        void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.PrintScreen)
                this.Close();
        }

        void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();

        }

        void mainPopup_Closed(object sender, EventArgs e)
        {
            Debug.WriteLine($"[INFO] Popup close event.");
        }
       

        void mainPopup_Opened(object sender, EventArgs e)
        {
            if (_popTimer == null)
            {
                _popTimer = new System.Windows.Threading.DispatcherTimer();
                _popTimer.Interval = TimeSpan.FromSeconds(4.0);
                _popTimer.Tick += popTimer_Tick;
                _popTimer.Start();
            }
            else
            {
                if (!_popTimer.IsEnabled)
                    _popTimer?.Start();
            }
        }

        void popTimer_Tick(object? sender, EventArgs e)
        {
            Debug.WriteLine($"[INFO] Firing popup timer event.");
            mainPopup.IsOpen = false;
            _popTimer?.Stop();
        }

        int _captureCounter = 0;
        void captureTimer_Tick(object? sender, EventArgs e)
        {
            _captureCounter++;
            SaveElementAsPng(hostBorder, $"capture_{_captureCounter:D3}.png");
        }

        void SaveElementAsPng(FrameworkElement element, string filePath)
        {
            if (element == null) { return; }
            Size size = new Size(element.ActualWidth, element.ActualHeight);
            element.Measure(size);
            element.Arrange(new Rect(size));
            var rtb = new RenderTargetBitmap((int)size.Width, (int)size.Height, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(element);
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(rtb));
            try
            {
                using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                {
                    encoder.Save(fs);
                }
            }
            catch (Exception) { }
        }
    }
}