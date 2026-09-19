using Microsoft.UI.Xaml.Controls;
using Microsoft.Extensions.DependencyInjection;
using Mirror_App.Services;
using Mirror_App.ViewModels;

namespace Mirror_App.Views;

public sealed partial class TimelinePage : Page
{
    public TimelineViewModel ViewModel { get; }

    public TimelinePage()
    {
        InitializeComponent();
        ViewModel = AppServices.Services.GetRequiredService<TimelineViewModel>();
        DataContext = ViewModel;

        TxtSessionCount.SetBinding(TextBlock.TextProperty, new Microsoft.UI.Xaml.Data.Binding { Path = new Microsoft.UI.Xaml.PropertyPath("SessionCountText"), Mode = Microsoft.UI.Xaml.Data.BindingMode.OneWay });
        ListSessions.ItemsSource = ViewModel.Sessions;

        Unloaded += (s, e) => ViewModel.Dispose();
    }
}
