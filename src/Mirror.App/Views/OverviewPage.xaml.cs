using Microsoft.UI.Xaml.Controls;
using Microsoft.Extensions.DependencyInjection;
using Mirror_App.Services;
using Mirror_App.ViewModels;

namespace Mirror_App.Views;

public sealed partial class OverviewPage : Page
{
    public OverviewViewModel ViewModel { get; }

    public OverviewPage()
    {
        InitializeComponent();
        ViewModel = AppServices.Services.GetRequiredService<OverviewViewModel>();
        DataContext = ViewModel;

        ListCategories.ItemsSource = ViewModel.Categories;
        ListTopApps.ItemsSource = ViewModel.TopApplications;
        ListRecentPatterns.ItemsSource = ViewModel.RecentPatterns;

        Unloaded += (s, e) => ViewModel.Dispose();
    }
}
