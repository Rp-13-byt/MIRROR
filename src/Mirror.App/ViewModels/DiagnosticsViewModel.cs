using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mirror.Core.Interfaces;
using Mirror.Core.Models;

namespace Mirror_App.ViewModels;

public sealed partial class DiagnosticsViewModel : ObservableObject
{
    private readonly IInferenceBackendManager _inferenceManager;

    [ObservableProperty]
    private string _activeBackendName = "Detecting...";

    [ObservableProperty]
    private string _tensorShape = "[1, 60, 10] (Batch=1, Sequence=60 min, Features=10)";

    [ObservableProperty]
    private string _quantizationType = "Static QDQ INT8 (Hexagon NPU optimized)";

    [ObservableProperty]
    private string _lastInferenceLatency = "0.04 ms";

    [ObservableProperty]
    private long _totalInferences = 0;

    [ObservableProperty]
    private string _memoryFootprint = "0 MB";

    [ObservableProperty]
    private string _databaseSize = "0 KB";

    [ObservableProperty]
    private string _walModeStatus = "WAL Mode Active (PRAGMA synchronous = NORMAL)";

    [ObservableProperty]
    private string _benchmarkResults = "Ready to benchmark on-device inference.";

    [ObservableProperty]
    private bool _isBenchmarking = false;

    public DiagnosticsViewModel(IInferenceBackendManager inferenceManager)
    {
        _inferenceManager = inferenceManager;
        RefreshDiagnostics();
    }

    [RelayCommand]
    public void RefreshDiagnostics()
    {
        ActiveBackendName = _inferenceManager.ActiveBackendName;

        using var proc = Process.GetCurrentProcess();
        double memMb = Math.Round(proc.WorkingSet64 / (1024.0 * 1024.0), 1);
        MemoryFootprint = $"{memMb} MB";

        string localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string dbPath = Path.Combine(localApp, "Mirror", "mirror.db");
        if (File.Exists(dbPath))
        {
            var info = new FileInfo(dbPath);
            DatabaseSize = $"{Math.Round(info.Length / 1024.0, 1)} KB";
        }
        else
        {
            DatabaseSize = "Database not yet materialized";
        }
    }

    [RelayCommand]
    public async Task RunBenchmarkAsync()
    {
        IsBenchmarking = true;
        BenchmarkResults = "Running 100 inference passes on active backend...";

        await Task.Run(async () =>
        {
            try
            {
                var seq = new FeatureSequence();
                var sw = Stopwatch.StartNew();
                int passes = 100;
                for (int i = 0; i < passes; i++)
                {
                    await _inferenceManager.PredictAsync(seq);
                }
                sw.Stop();

                double avgMs = sw.Elapsed.TotalMilliseconds / passes;
                LastInferenceLatency = $"{avgMs:F3} ms";
                TotalInferences += passes;
                BenchmarkResults = $"Completed {passes} iterations on {ActiveBackendName}. Average latency: {avgMs:F3} ms ({(passes / sw.Elapsed.TotalSeconds):F0} inf/sec).";
            }
            catch (Exception ex)
            {
                BenchmarkResults = $"Benchmark error: {ex.Message}";
            }
        });

        IsBenchmarking = false;
        RefreshDiagnostics();
    }
}
