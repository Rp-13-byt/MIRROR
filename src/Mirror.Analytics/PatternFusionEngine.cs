using Mirror.Core.Domain;
using Mirror.Core.Interfaces;
using Mirror.Core.Models;

namespace Mirror.Analytics;

public class PatternFusionEngine : IPatternFusionEngine
{
    public PatternEvent? FuseSignals(
        PatternEvent? ruleCandidate,
        InferenceResult? mlInference,
        float minimumConfidenceThreshold = 0.65f)
    {
        // 1. If rule candidate exists:
        if (ruleCandidate != null)
        {
            bool modelCorroborates = mlInference != null &&
                                     mlInference.DetectedPattern == ruleCandidate.PatternType &&
                                     mlInference.Confidence >= minimumConfidenceThreshold;

            float combinedConfidence = modelCorroborates
                ? Math.Min(1.0f, (ruleCandidate.ModelConfidence + mlInference!.Confidence) / 2.0f + 0.1f)
                : ruleCandidate.ModelConfidence;

            return ruleCandidate with
            {
                ModelSignal = modelCorroborates,
                ModelConfidence = combinedConfidence
            };
        }

        // 2. If no rule candidate, ML alone is NOT sufficient to declare a behavioral event
        // The rule engine remains the authoritative baseline to prevent hallucinated pattern warnings.
        return null;
    }
}
