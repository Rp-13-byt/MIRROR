using Mirror.Core.Domain;
using Mirror.Core.Models;

namespace Mirror.Core.Interfaces;

public interface IInferenceBackend : IAsyncDisposable
{
    string Name { get; }
    InferenceBackendKind Kind { get; }
    bool IsAvailable { get; }
    Task<bool> InitializeAsync(string modelPath, CancellationToken ct = default);
    Task<InferenceResult> PredictAsync(FeatureSequence sequence, CancellationToken ct = default);
}

public interface IInferenceBackendManager : IAsyncDisposable
{
    InferenceBackendKind ActiveBackendKind { get; }
    string ActiveBackendName { get; }
    bool IsInitialized { get; }
    string? FallbackReason { get; }
    double WarmupLatencyMs { get; }
    double SteadyStateP50Ms { get; }
    double SteadyStateP95Ms { get; }
    ModelMetadata? CurrentModelMetadata { get; }

    Task InitializeAsync(string modelDirectory, CancellationToken ct = default);
    Task<InferenceResult> PredictAsync(FeatureSequence sequence, CancellationToken ct = default);
}
