using Microsoft.UI.Xaml.Controls;
using Microsoft.Extensions.DependencyInjection;
using Mirror.Core.Models;
using Mirror_App.Services;
using Mirror_App.ViewModels;

namespace Mirror_App.Views;

public sealed partial class PatternsPage : Page
{
    public PatternsViewModel ViewModel { get; }

    public PatternsPage()
    {
        InitializeComponent();
        ViewModel = AppServices.Services.GetRequiredService<PatternsViewModel>();
        DataContext = ViewModel;

        ListPatterns.ItemsSource = ViewModel.Patterns;
        UpdateEmptyBanner();

        ViewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(ViewModel.HasPatterns))
            {
                UpdateEmptyBanner();
            }
        };

        Unloaded += (s, e) => ViewModel.Dispose();
    }

    private void UpdateEmptyBanner()
    {
        EmptyPatternsBanner.Visibility = ViewModel.HasPatterns
            ? Microsoft.UI.Xaml.Visibility.Collapsed
            : Microsoft.UI.Xaml.Visibility.Visible;
    }
}
