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

    private async void OnRunAuditClicked(object sender, RoutedEventArgs e)
    {
        await ViewModel.RunPrivacyAuditCommand.ExecuteAsync(null);
    }

    private async void OnRefreshInventoryClicked(object sender, RoutedEventArgs e)
    {
        await ViewModel.RefreshDataInventoryCommand.ExecuteAsync(null);
    }

    private async void OnExportCsvClicked(object sender, RoutedEventArgs e)
    {
        await ViewModel.ExportToCsvCommand.ExecuteAsync(null);
    }

    private async void OnExportJsonClicked(object sender, RoutedEventArgs e)
    {
        await ViewModel.ExportToJsonCommand.ExecuteAsync(null);
    }

    private async void OnDeleteTodayClicked(object sender, RoutedEventArgs e)
    {
        await ViewModel.DeleteTodayDataCommand.ExecuteAsync(null);
    }

    private void OnPurgeAllClicked(object sender, RoutedEventArgs e)
    {
        ViewModel.RequestPurgePreviewCommand.Execute(null);
    }

    private async void OnConfirmPurgeClicked(object sender, RoutedEventArgs e)
    {
        await ViewModel.PurgeAllDataCommand.ExecuteAsync(null);
    }

    private void OnCancelPurgeClicked(object sender, RoutedEventArgs e)
    {
        ViewModel.CancelPurgeCommand.Execute(null);
    }
}
