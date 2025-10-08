using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace SpinnerDemo;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    bool _frameCapture = false;
    DispatcherTimer? _popTimer = null;
    DispatcherTimer? _captureTimer = null;

    public MainWindow()
    {
        InitializeComponent();
    }

    #region [Events]
    void Window_Loaded(object sender, RoutedEventArgs e)
    {
        this.Icon = "pack://application:,,,/Assets/AppIcon.png".ReturnImageSource();
        if (_frameCapture)
        {
            tbPopup.Text = "Press [SPACE] to save captured frames, press any other key to exit.";
            RemovePreviousCaptures();
        }
        mainPopup.IsOpen = true;
    }

    void Window_Unloaded(object sender, RoutedEventArgs e)
    {
        _popTimer?.Stop();
        _captureTimer?.Stop();
    }

    void Window_KeyUp(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Space)
        {
            if (_frameCapture)
            {
                _captureTimer?.Stop();
                _frameCapture = false;
                int count = 0;
                foreach (var enc in _encoder)
                {
                    count++;
                    var filePath = $"capture_{count:D3}.png";
                    try
                    {
                        using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                        {
                            enc.Save(fs);
                        }
                    }
                    catch (Exception) { }
                }
                _encoder.Clear();
            }
            else
            {
                RemovePreviousCaptures();
                _frameCapture = true;
                if (_captureTimer == null)
                {
                    _captureTimer = new System.Windows.Threading.DispatcherTimer();
                    // Adjust this time based on the desired framerate for the GIF.
                    _captureTimer.Interval = TimeSpan.FromMilliseconds(100);
                    _captureTimer.Tick += captureTimer_Tick;
                    _captureTimer.Start();
                }
                _captureTimer?.Start();
            }
        }
        else if (e.Key != Key.PrintScreen)
        {
            _popTimer?.Stop();
            _captureTimer?.Stop();
            this.Close();
        }
    }

    void Border_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            Cursor = Cursors.Hand;
            DragMove();
        }
        Cursor = Cursors.Arrow;
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
        mainPopup.IsOpen = false;
        _popTimer?.Stop();
    }

    void captureTimer_Tick(object? sender, EventArgs e)
    {
        SaveElementAsPng(hostBorder);
    }
    #endregion

    List<PngBitmapEncoder> _encoder = new List<PngBitmapEncoder>();
    void SaveElementAsPng(FrameworkElement element)
    {
        if (element == null) { return; }
        Size size = new Size(element.ActualWidth, element.ActualHeight);
        element.Measure(size);
        //element.Arrange(new Rect(size));
        var rtb = new RenderTargetBitmap((int)size.Width, (int)size.Height, 96, 96, PixelFormats.Pbgra32);
        rtb.Render(element);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(rtb));
        _encoder.Add(encoder);
    }

    /// <summary>
    /// Remove old capture images, if they exist.
    /// </summary>
    void RemovePreviousCaptures()
    {
        var pngFiles = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "cap*.png", SearchOption.TopDirectoryOnly);
        foreach (var fn in pngFiles) { try { File.Delete(fn); } catch { } }
    }
}