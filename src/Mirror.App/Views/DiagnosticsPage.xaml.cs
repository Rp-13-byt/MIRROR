using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Extensions.DependencyInjection;
using Mirror_App.Services;
using Mirror_App.ViewModels;

namespace Mirror_App.Views;

public sealed partial class DiagnosticsPage : Page
{
    public DiagnosticsViewModel ViewModel { get; }

    public DiagnosticsPage()
    {
        InitializeComponent();
        ViewModel = AppServices.Services.GetRequiredService<DiagnosticsViewModel>();
        DataContext = ViewModel;

        TxtBackend.Text = ViewModel.ActiveBackendName;
        TxtSoc.Text = ViewModel.SocArchitecture;
        TxtMemory.Text = ViewModel.MemoryFootprint;
        TxtLatency.Text = ViewModel.LastInferenceLatency;
    }

    private async void OnRunBenchmarkClicked(object sender, RoutedEventArgs e)
    {
        BtnRunBenchmark.IsEnabled = false;
        TxtBenchmarkLog.Text = "Benchmarking in progress...";
        await ViewModel.RunBenchmarkCommand.ExecuteAsync(null);
        TxtBenchmarkLog.Text = ViewModel.BenchmarkResults;
        TxtLatency.Text = ViewModel.LastInferenceLatency;
        TxtMemory.Text = ViewModel.MemoryFootprint;
        BtnRunBenchmark.IsEnabled = true;
    }
}
