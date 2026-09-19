using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Mirror.Core.Domain;
using Mirror.Core.Models;
using Mirror.Inference;
using Xunit;

namespace Mirror.Inference.Tests;

public class InferenceBackendTests
{
    private readonly string _modelsDir;
    private readonly string _fp32ModelPath;

    public InferenceBackendTests()
    {
        // Check output directory or source directory
        string baseDir = AppContext.BaseDirectory;
        string candidate1 = Path.Combine(baseDir, "Models");
        string candidate2 = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "..", "src", "Mirror.Inference", "Models"));

        _modelsDir = Directory.Exists(candidate1) ? candidate1 : candidate2;
        _fp32ModelPath = Path.Combine(_modelsDir, "mirror_pattern_v1.onnx");
    }

    [Fact]
    public async Task CpuInferenceBackend_InitializesAndPredictsSuccessfully()
    {
        Assert.True(File.Exists(_fp32ModelPath), $"FP32 model file not found at {_fp32ModelPath}");

        var backend = new CpuInferenceBackend();
        bool initialized = await backend.InitializeAsync(_fp32ModelPath);

        Assert.True(initialized);
        Assert.True(backend.IsAvailable);
        Assert.Equal(InferenceBackendKind.Cpu, backend.Kind);

        // Test inference with sample sequence
        var sequence = new FeatureSequence();
        // Populate sample high switching burst pattern
        for (int t = 0; t < 60; t++)
        {
            sequence.Values[t, 0] = 0.9f; // Active
            sequence.Values[t, 1] = 0.8f; // High switches
            sequence.Values[t, 2] = 0.7f; // Multiple apps
        }

        var result = await backend.PredictAsync(sequence);

        Assert.NotNull(result);
        Assert.Equal(InferenceBackendKind.Cpu, result.BackendUsed);
        Assert.True(result.Confidence >= 0.0f && result.Confidence <= 1.0f);
        Assert.NotNull(result.ClassProbabilities);
        Assert.Equal(5, result.ClassProbabilities.Length);

        // Sum of probabilities should be approximately 1.0
        float sumProb = result.ClassProbabilities.Sum();
        Assert.InRange(sumProb, 0.99f, 1.01f);

        await backend.DisposeAsync();
        Assert.False(backend.IsAvailable);
    }

    [Fact]
    public async Task CpuInferenceBackend_MissingModel_FailsGracefully()
    {
        var backend = new CpuInferenceBackend();
        bool initialized = await backend.InitializeAsync(@"C:\non_existent_mirror_path\fake.onnx");

        Assert.False(initialized);
        Assert.False(backend.IsAvailable);
    }

    [Fact]
    public async Task InferenceBackendManager_FallsBackGracefullyAndTruthfullyReportsBackend()
    {
        var manager = new InferenceBackendManager();
        await manager.InitializeAsync(_modelsDir);

        Assert.True(manager.IsInitialized);
        Assert.NotNull(manager.ActiveBackendName);

        // Truth in Hardware: on non-Snapdragon x64 environment, it should NOT claim QNN
        if (manager.ActiveBackendKind != InferenceBackendKind.Qnn)
        {
            Assert.NotNull(manager.FallbackReason);
        }

        // Test prediction through manager
        var sequence = new FeatureSequence();
        var result = await manager.PredictAsync(sequence);

        Assert.NotNull(result);
        Assert.Equal(manager.ActiveBackendKind, result.BackendUsed);
        Assert.True(manager.WarmupLatencyMs > 0);

        // Verify metadata loaded
        if (manager.CurrentModelMetadata != null)
        {
            Assert.Equal("mirror_pattern_cnn", manager.CurrentModelMetadata.ModelName);
            Assert.Equal(1, manager.CurrentModelMetadata.FeatureSchemaVersion);
        }

        await manager.DisposeAsync();
        Assert.False(manager.IsInitialized);
    }
}
