using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Extensions.DependencyInjection;
using Mirror_App.Services;
using Mirror_App.ViewModels;

namespace Mirror_App.Views;

public sealed partial class SettingsPage : Page
{
    public SettingsViewModel ViewModel { get; }

    public SettingsPage()
    {
        InitializeComponent();
        ViewModel = AppServices.Services.GetRequiredService<SettingsViewModel>();
        DataContext = ViewModel;

        ToggleStartup.IsOn = ViewModel.LaunchOnStartup;
        ToggleTray.IsOn = ViewModel.MinimizeToTray;
        ToggleNpu.IsOn = ViewModel.EnableNpuAcceleration;
        ListExcluded.ItemsSource = ViewModel.ExcludedProcesses;
    }

    private void OnSettingsChanged(object sender, RoutedEventArgs e)
    {
        ViewModel.LaunchOnStartup = ToggleStartup.IsOn;
        ViewModel.MinimizeToTray = ToggleTray.IsOn;
        ViewModel.EnableNpuAcceleration = ToggleNpu.IsOn;
        ViewModel.SaveSettingsCommand.Execute(null);
        TxtSettingsStatus.Text = ViewModel.SaveMessage;
    }

    private void OnTrackingModeChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ComboTrackingMode != null && ComboTrackingMode.SelectedIndex >= 0)
        {
            ViewModel.SetTrackingModeCommand.Execute(ComboTrackingMode.SelectedIndex);
            TxtSettingsStatus.Text = ViewModel.SaveMessage;
        }
    }

    private void OnAddExclusionClicked(object sender, RoutedEventArgs e)
    {
        ViewModel.NewExcludedProcess = TxtNewProcess.Text;
        ViewModel.AddExcludedProcessCommand.Execute(null);
        TxtNewProcess.Text = string.Empty;
        TxtSettingsStatus.Text = ViewModel.SaveMessage;
    }

    private void OnRemoveExclusionClicked(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string proc)
        {
            ViewModel.RemoveExcludedProcessCommand.Execute(proc);
            TxtSettingsStatus.Text = ViewModel.SaveMessage;
        }
    }
}
