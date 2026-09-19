using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Mirror.Core.Domain;
using Mirror.Core.Interfaces;
using Mirror.Core.Models;

namespace Mirror.Inference;

public class CpuInferenceBackend : IInferenceBackend
{
    private InferenceSession? _session;
    public string Name => "CPU (Portable Fallback)";
    public InferenceBackendKind Kind => InferenceBackendKind.Cpu;
    public bool IsAvailable { get; private set; }

    public Task<bool> InitializeAsync(string modelPath, CancellationToken ct = default)
    {
        try
        {
            if (!File.Exists(modelPath))
            {
                IsAvailable = false;
                return Task.FromResult(false);
            }

            var options = new SessionOptions
            {
                IntraOpNumThreads = 1,
                InterOpNumThreads = 1,
                GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL
            };

            _session = new InferenceSession(modelPath, options);

            // Execute warm-up tensor
            var dummyTensor = new DenseTensor<float>(new[] { 1, 60, 10 });
            var inputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor("behavioral_sequence", dummyTensor)
            };

            using var results = _session.Run(inputs);
            var output = results.First().AsTensor<float>();

            IsAvailable = output != null && output.Length == 5;
            return Task.FromResult(IsAvailable);
        }
        catch
        {
            _session?.Dispose();
            _session = null;
            IsAvailable = false;
            return Task.FromResult(false);
        }
    }

    public Task<InferenceResult> PredictAsync(FeatureSequence sequence, CancellationToken ct = default)
    {
        if (_session == null || !IsAvailable)
        {
            throw new InvalidOperationException("CPU backend is not initialized or not available.");
        }

        var sw = System.Diagnostics.Stopwatch.StartNew();

        var tensor = new DenseTensor<float>(new[] { 1, 60, 10 });
        for (int t = 0; t < 60; t++)
        {
            for (int f = 0; f < 10; f++)
            {
                tensor[0, t, f] = sequence.Values[t, f];
            }
        }

        var inputs = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor("behavioral_sequence", tensor)
        };

        using var results = _session.Run(inputs);
        sw.Stop();

        var logits = results.First().AsTensor<float>().ToArray();
        var (pattern, conf, probs) = ComputeProbabilities(logits);

        return Task.FromResult(new InferenceResult
        {
            BackendUsed = InferenceBackendKind.Cpu,
            DetectedPattern = pattern,
            Confidence = conf,
            ClassProbabilities = probs,
            InferenceTimeMs = sw.Elapsed.TotalMilliseconds,
            ModelVersion = "1.0.0"
        });
    }

    private static (BehavioralPatternType Pattern, float Confidence, float[] Probabilities) ComputeProbabilities(float[] logits)
    {
        float maxLogit = logits.Max();
        float sumExp = logits.Sum(l => (float)Math.Exp(l - maxLogit));
        float[] probs = logits.Select(l => (float)Math.Exp(l - maxLogit) / sumExp).ToArray();

        int argmax = 0;
        float maxProb = probs[0];
        for (int i = 1; i < probs.Length; i++)
        {
            if (probs[i] > maxProb)
            {
                maxProb = probs[i];
                argmax = i;
            }
        }

        var pattern = argmax switch
        {
            0 => BehavioralPatternType.Normal,
            1 => BehavioralPatternType.HighSwitchingBurst,
            2 => BehavioralPatternType.ExtendedSingleAppSession,
            3 => BehavioralPatternType.RapidReopenPattern,
            4 => BehavioralPatternType.CompositeScrollLike,
            _ => BehavioralPatternType.Normal
        };

        return (pattern, maxProb, probs);
    }

    public ValueTask DisposeAsync()
    {
        _session?.Dispose();
        _session = null;
        IsAvailable = false;
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }
}
