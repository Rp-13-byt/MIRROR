using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Extensions.DependencyInjection;
using Mirror_App.Services;
using Mirror_App.ViewModels;

namespace Mirror_App.Views;

public sealed partial class PrivacyPage : Page
{
    public PrivacyViewModel ViewModel { get; }

    public PrivacyPage()
    {
        InitializeComponent();
        ViewModel = AppServices.Services.GetRequiredService<PrivacyViewModel>();
        DataContext = ViewModel;
    }

    private async void OnExportCsvClicked(object sender, RoutedEventArgs e)
    {
        await ViewModel.ExportToCsvCommand.ExecuteAsync(null);
        TxtStatusMessage.Text = ViewModel.StatusMessage;
    }

    private async void OnExportJsonClicked(object sender, RoutedEventArgs e)
    {
        await ViewModel.ExportToJsonCommand.ExecuteAsync(null);
        TxtStatusMessage.Text = ViewModel.StatusMessage;
    }

    private void OnDeleteTodayClicked(object sender, RoutedEventArgs e)
    {
        ViewModel.DeleteTodayDataCommand.Execute(null);
        TxtStatusMessage.Text = ViewModel.StatusMessage;
    }

    private void OnPurgeAllClicked(object sender, RoutedEventArgs e)
    {
        ViewModel.PurgeAllDataCommand.Execute(null);
        TxtStatusMessage.Text = ViewModel.StatusMessage;
    }
}
