using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.Extensions.DependencyInjection;
using Mirror_App.Services;
using Mirror_App.ViewModels;

namespace Mirror_App.Views;

public sealed partial class CopilotPage : Page
{
    public CopilotViewModel ViewModel { get; }

    public CopilotPage()
    {
        InitializeComponent();
        ViewModel = AppServices.Services.GetRequiredService<CopilotViewModel>();
        DataContext = ViewModel;

        TxtBackendBadge.Text = ViewModel.ActiveBackend;
    }

    private async void OnSendClicked(object sender, RoutedEventArgs e)
    {
        ViewModel.InputQuestion = TxtQuery.Text;
        TxtQuery.Text = string.Empty;
        await ViewModel.SendCommand.ExecuteAsync(null);
        ScrollToBottom();
    }

    private async void OnQueryKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            e.Handled = true;
            ViewModel.InputQuestion = TxtQuery.Text;
            TxtQuery.Text = string.Empty;
            await ViewModel.SendCommand.ExecuteAsync(null);
            ScrollToBottom();
        }
    }

    private async void OnQuickChipClicked(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Content is string query)
        {
            await ViewModel.QuickAskCommand.ExecuteAsync(query);
            ScrollToBottom();
        }
    }

    private void OnClearChatClicked(object sender, RoutedEventArgs e)
    {
        ViewModel.ClearChatCommand.Execute(null);
    }

    private void ScrollToBottom()
    {
        ChatScrollViewer?.ChangeView(null, ChatScrollViewer.ScrollableHeight, null);
    }
}
