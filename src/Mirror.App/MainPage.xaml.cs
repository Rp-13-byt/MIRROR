using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Extensions.DependencyInjection;
using Mirror_App.Services;
using Mirror_App.ViewModels;
using Mirror_App.Views;

namespace Mirror_App;

public sealed partial class MainPage : Page
{
    private readonly MainViewModel _viewModel;

    public MainPage()
    {
        InitializeComponent();
        _viewModel = AppServices.Services.GetRequiredService<MainViewModel>();
        Loaded += MainPage_Loaded;
    }

    private void MainPage_Loaded(object sender, RoutedEventArgs e)
    {
        TxtHardwareBadge.Text = _viewModel.HardwareAccelerationName;
        ToggleDemoSwitch.IsOn = _viewModel.IsDemoMode;
        UpdateDemoBadge();

        if (NavView.MenuItems.Count > 0)
        {
            NavView.SelectedItem = NavView.MenuItems[0];
        }
    }

    private void OnNavSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (ContentFrame == null) return;

        if (args.IsSettingsSelected)
        {
            ContentFrame.Navigate(typeof(SettingsPage));
            return;
        }

        if (args.SelectedItem is NavigationViewItem item && item.Tag is string tag)
        {
            switch (tag)
            {
                case "overview":
                    ContentFrame.Navigate(typeof(OverviewPage));
                    break;
                case "copilot":
                    ContentFrame.Navigate(typeof(CopilotPage));
                    break;
                case "timeline":
                    ContentFrame.Navigate(typeof(TimelinePage));
                    break;
                case "patterns":
                    ContentFrame.Navigate(typeof(PatternsPage));
                    break;
                case "trends":
                    ContentFrame.Navigate(typeof(TrendsPage));
                    break;
                case "privacy":
                    ContentFrame.Navigate(typeof(PrivacyPage));
                    break;
                case "diagnostics":
                    ContentFrame.Navigate(typeof(DiagnosticsPage));
                    break;
            }
        }
    }

    private void OnDemoToggled(object sender, RoutedEventArgs e)
    {
        if (_viewModel == null || ToggleDemoSwitch == null) return;

        _viewModel.IsDemoMode = ToggleDemoSwitch.IsOn;
        AppServices.Services.GetRequiredService<IDemoDataService>().IsDemoModeActive = ToggleDemoSwitch.IsOn;
        UpdateDemoBadge();

        // Reload current page
        if (ContentFrame?.Content is Page currentPage)
        {
            var pageType = currentPage.GetType();
            ContentFrame.Navigate(pageType);
        }
    }

    private void UpdateDemoBadge()
    {
        DemoBadge.Visibility = ToggleDemoSwitch.IsOn ? Visibility.Visible : Visibility.Collapsed;
    }

    private void OnTrackingClicked(object sender, RoutedEventArgs e)
    {
        _viewModel.ToggleTrackingCommand.Execute(null);
        BtnTrackingToggle.Content = _viewModel.IsTrackingActive ? "Tracking: Active" : "Tracking: Paused";
    }
}
