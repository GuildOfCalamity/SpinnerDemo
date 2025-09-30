using System.Diagnostics;
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
        System.Windows.Threading.DispatcherTimer? _popTimer = null;

        public MainWindow()
        {
            InitializeComponent();
        }

        void Window_Loaded(object sender, RoutedEventArgs e)
        {
            mainPopup.IsOpen = true;
            this.Icon = "pack://application:,,,/Assets/AppIcon.png".ReturnImageSource();
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
    }
}