using Mirror.Core.Domain;
using Mirror.Core.Interfaces;
using Mirror.Core.Models;

namespace Mirror.Analytics;

public class PatternFusionEngine : IPatternFusionEngine
{
    private int _totalEvaluations;
    private int _ruleAndModelAgreedCount;
    private int _ruleOnlyCount;
    private int _modelOnlyCount;
    private int _disagreementCount;
    private int _uncertainCount;
    private readonly object _lock = new();

    public PatternEvent? FuseSignals(
        PatternEvent? ruleCandidate,
        InferenceResult? mlInference,
        float minimumConfidenceThreshold = 0.65f)
    {
        lock (_lock)
        {
            _totalEvaluations++;

            if (mlInference != null && mlInference.IsUncertain)
            {
                _uncertainCount++;
            }

            // Case 1: Rule fired
            if (ruleCandidate != null)
            {
                // Subcase 1A: ML agrees with Rule above confidence threshold
                if (mlInference != null &&
                    !mlInference.IsUncertain &&
                    mlInference.DetectedPattern == ruleCandidate.PatternType &&
                    mlInference.Confidence >= minimumConfidenceThreshold)
                {
                    _ruleAndModelAgreedCount++;
                    float combinedConfidence = Math.Min(1.0f, (ruleCandidate.ModelConfidence + mlInference.Confidence) / 2.0f + 0.1f);
                    return ruleCandidate with
                    {
                        ModelSignal = true,
                        ModelConfidence = combinedConfidence,
                        Explanation = $"{ruleCandidate.Explanation} (Corroborated by local ONNX model with {mlInference.Confidence:P0} confidence)"
                    };
                }

                // Subcase 1B: ML is uncertain or did not run
                if (mlInference == null || mlInference.IsUncertain || mlInference.DetectedPattern == BehavioralPatternType.Normal)
                {
                    _ruleOnlyCount++;
                    string uncertaintyNote = mlInference != null && mlInference.IsUncertain
                        ? $" (Local model abstained: {mlInference.AbstentionReason ?? "Uncertain confidence"})"
                        : "";
                    return ruleCandidate with
                    {
                        ModelSignal = false,
                        ModelConfidence = ruleCandidate.ModelConfidence,
                        Explanation = $"{ruleCandidate.Explanation}{uncertaintyNote}"
                    };
                }

                // Subcase 1C: ML predicted a different pattern (Disagreement)
                _disagreementCount++;
                // Rule engine remains authoritative, but we log the disagreement
                return ruleCandidate with
                {
                    ModelSignal = false,
                    ModelConfidence = ruleCandidate.ModelConfidence
                };
            }

            // Case 2: Rule did not fire, but ML predicted a pattern
            if (mlInference != null &&
                !mlInference.IsUncertain &&
                mlInference.DetectedPattern != BehavioralPatternType.Normal &&
                mlInference.DetectedPattern != BehavioralPatternType.Uncertain)
            {
                // Exploratory ML signal tracked in diagnostics, but rule engine remains authoritative baseline
                _modelOnlyCount++;
                return null;
            }

            // Case 3: Neither fired or ML was uncertain
            return null;
        }
    }

    public PatternAgreementDiagnostics GetAgreementDiagnostics()
    {
        lock (_lock)
        {
            int total = Math.Max(1, _totalEvaluations);
            return new PatternAgreementDiagnostics(
                TotalEvaluations: _totalEvaluations,
                RuleAndModelAgreedCount: _ruleAndModelAgreedCount,
                RuleOnlyCount: _ruleOnlyCount,
                ModelOnlyCount: _modelOnlyCount,
                DisagreementCount: _disagreementCount,
                UncertainCount: _uncertainCount,
                RuleAndModelAgreementRate: (double)_ruleAndModelAgreedCount / total,
                RuleOnlyRate: (double)_ruleOnlyCount / total,
                ModelOnlyRate: (double)_modelOnlyCount / total,
                DisagreementRate: (double)_disagreementCount / total,
                UncertainRate: (double)_uncertainCount / total
            );
        }
    }
}
