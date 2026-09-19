using Microsoft.UI.Xaml.Controls;
using Microsoft.Extensions.DependencyInjection;
using Mirror_App.Services;
using Mirror_App.ViewModels;

namespace Mirror_App.Views;

public sealed partial class TrendsPage : Page
{
    public TrendsViewModel ViewModel { get; }

    public TrendsPage()
    {
        InitializeComponent();
        ViewModel = AppServices.Services.GetRequiredService<TrendsViewModel>();
        DataContext = ViewModel;

        TxtBaselineSummary.Text = ViewModel.BaselineComparisonText;
        TxtWeeklyAvg.Text = ViewModel.WeeklyAverageActive;
        ItemsDayTrends.ItemsSource = ViewModel.DayTrends;
    }
}
