using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using Microsoft.Extensions.DependencyInjection;
using Mirror.Core.Interfaces;
using Mirror_App.Services;

namespace Mirror_App;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : Application
{
    private Window? _window;
    
    /// <summary>
    /// Initializes the singleton application object.  This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        InitializeComponent();
        UnhandledException += (sender, e) =>
        {
            try
            {
                string mirrorDir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Mirror");
                System.IO.Directory.CreateDirectory(mirrorDir);
                string logFile = System.IO.Path.Combine(mirrorDir, "crash.log");
                System.IO.File.WriteAllText(logFile, $"{DateTime.UtcNow:O}\n{e.Exception}\n{e.Message}");
            }
            catch { }
            e.Handled = true;
        };
        AppServices.Initialize();
    }

    /// <summary>
    /// Invoked when the application is launched.
    /// </summary>
    /// <param name="args">Details about the launch request and process.</param>
    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        _window = new MainWindow();
        _window.Activate();

        try
        {
            var tracking = AppServices.Services.GetRequiredService<ITrackingCoordinator>();
            tracking.Start();
        }
        catch
        {
            // Tracking initialization error fallback handled safely
        }
    }
}
