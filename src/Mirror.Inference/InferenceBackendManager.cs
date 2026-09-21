using System.Diagnostics;
using System.Text.Json;
using Mirror.Core.Domain;
using Mirror.Core.Interfaces;
using Mirror.Core.Models;

namespace Mirror.Inference;

public class InferenceBackendManager : IInferenceBackendManager
{
    private readonly List<IInferenceBackend> _backends = new();
    private IInferenceBackend? _activeBackend;
    private readonly List<double> _latencyHistory = new();
    private readonly object _lock = new();

    public InferenceBackendKind ActiveBackendKind => _activeBackend?.Kind ?? InferenceBackendKind.Cpu;
    public string ActiveBackendName => _activeBackend?.Name ?? "None";
    public bool IsInitialized => _activeBackend != null && _activeBackend.IsAvailable;
    public string? FallbackReason { get; private set; }
    public double WarmupLatencyMs { get; private set; }
    public double SteadyStateP50Ms { get; private set; }
    public double SteadyStateP95Ms { get; private set; }
    public ModelMetadata? CurrentModelMetadata { get; private set; }
    public float AbstentionThreshold { get; set; } = 0.60f;

    public async Task InitializeAsync(string modelDirectory, CancellationToken ct = default)
    {
        string fp32ModelPath = Path.Combine(modelDirectory, "mirror_pattern_v1.onnx");
        string qdqModelPath = Path.Combine(modelDirectory, "mirror_pattern_v1.qdq.onnx");
        string metadataPath = Path.Combine(modelDirectory, "metadata", "mirror_pattern_v1.json");

        if (File.Exists(metadataPath))
        {
            try
            {
                string json = await File.ReadAllTextAsync(metadataPath, ct);
                CurrentModelMetadata = JsonSerializer.Deserialize<ModelMetadata>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch
            {
                // Fallback metadata
            }
        }

        // Validate model schema
        if (CurrentModelMetadata != null && CurrentModelMetadata.FeatureSchemaVersion > 1)
        {
            throw new InvalidOperationException($"Incompatible model feature schema: {CurrentModelMetadata.FeatureSchemaVersion}");
        }

        // 1. Try QNN with QDQ model first (on Snapdragon hardware)
        var qnn = new QnnInferenceBackend();
        _backends.Add(qnn);
        string qnnTarget = File.Exists(qdqModelPath) ? qdqModelPath : fp32ModelPath;

        var sw = Stopwatch.StartNew();
        bool qnnOk = await qnn.InitializeAsync(qnnTarget, ct).ConfigureAwait(false);
        sw.Stop();

        if (qnnOk)
        {
            _activeBackend = qnn;
            WarmupLatencyMs = sw.Elapsed.TotalMilliseconds;
            FallbackReason = null;
            return;
        }

        FallbackReason = "Snapdragon QNN execution provider unavailable; falling back to DirectML.";

        // 2. Try DirectML with FP32 model
        var dml = new DirectMlInferenceBackend();
        _backends.Add(dml);

        sw.Restart();
        bool dmlOk = await dml.InitializeAsync(fp32ModelPath, ct).ConfigureAwait(false);
        sw.Stop();

        if (dmlOk)
        {
            _activeBackend = dml;
            WarmupLatencyMs = sw.Elapsed.TotalMilliseconds;
            return;
        }

        FallbackReason = "DirectML execution provider unavailable; falling back to portable CPU execution provider.";

        // 3. Fallback to CPU
        var cpu = new CpuInferenceBackend();
        _backends.Add(cpu);

        sw.Restart();
        bool cpuOk = await cpu.InitializeAsync(fp32ModelPath, ct).ConfigureAwait(false);
        sw.Stop();

        if (cpuOk)
        {
            _activeBackend = cpu;
            WarmupLatencyMs = sw.Elapsed.TotalMilliseconds;
            return;
        }

        FallbackReason = "No supported ONNX Runtime execution provider could be initialized.";
    }

    public async Task<InferenceResult> PredictAsync(FeatureSequence sequence, CancellationToken ct = default)
    {
        if (_activeBackend == null || !_activeBackend.IsAvailable)
        {
            throw new InvalidOperationException("No inference backend is initialized and available.");
        }

        var result = await _activeBackend.PredictAsync(sequence, ct).ConfigureAwait(false);

        lock (_lock)
        {
            _latencyHistory.Add(result.InferenceTimeMs);
            if (_latencyHistory.Count > 100) _latencyHistory.RemoveAt(0);

            var sorted = _latencyHistory.OrderBy(x => x).ToList();
            SteadyStateP50Ms = sorted[sorted.Count / 2];
            int p95Idx = (int)Math.Floor(sorted.Count * 0.95);
            SteadyStateP95Ms = sorted[Math.Clamp(p95Idx, 0, sorted.Count - 1)];
        }

        if (result.Confidence < AbstentionThreshold)
        {
            result = result with
            {
                IsUncertain = true,
                DetectedPattern = BehavioralPatternType.Uncertain,
                AbstentionReason = $"Confidence ({result.Confidence:P0}) below certainty threshold ({AbstentionThreshold:P0})"
            };
        }

        return result;
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var backend in _backends)
        {
            await backend.DisposeAsync();
        }
        _backends.Clear();
        _activeBackend = null;
        GC.SuppressFinalize(this);
    }
}
