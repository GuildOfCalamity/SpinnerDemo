using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Windows;

namespace SpinnerDemo;

public partial class App : Application
{
    void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        Debug.WriteLine($"[ERROR] DispatcherUnhandledException: {e.Exception}");
        e.Handled = true;
    }
}
