using System.Text;
using Mirror.Core.Domain;
using Mirror.Core.Models;

namespace Mirror.LLM;

public static class PromptBuilder
{
    public const string SystemPrompt =
        "You are an on-device digital wellbeing assistant for Mirror. " +
        "Mirror never sends data to any server and processes all telemetry strictly locally. " +
        "Explain why this behavioral pattern was detected using only the provided local activity log in 2 neutral, objective sentences. " +
        "Never use clinical terms (such as burnout, addiction, ADHD, depression, anxiety, stress). " +
        "Do not diagnose. Do not give unsolicited life advice.";

    public static string BuildRagPrompt(
        PatternEvent pattern,
        IReadOnlyList<ActivitySession> recentSessions,
        IReadOnlyList<AppSwitchEvent> recentSwitches,
        AdaptiveBaseline? baseline)
    {
        var sb = new StringBuilder();
        sb.AppendLine("LOCAL ACTIVITY CONTEXT (LAST 4 HOURS):");

        if (recentSessions.Count == 0)
        {
            sb.AppendLine("- No previous sessions recorded in window.");
        }
        else
        {
            foreach (var s in recentSessions.OrderBy(s => s.StartUtc))
            {
                sb.AppendLine($"- [{s.StartUtc:HH:mm} - {s.EndUtc:HH:mm}] {s.DisplayName} ({s.Category}): {s.ActiveSeconds / 60}m active");
            }
        }

        sb.AppendLine();
        sb.AppendLine($"SWITCH EVENT COUNT: {recentSwitches.Count}");
        sb.AppendLine();

        if (baseline != null)
        {
            sb.AppendLine($"PERSONAL BASELINE ({baseline.MetricKey}): Median = {baseline.MedianValue:F1}, StdDev = {baseline.StdDev:F1}, GatePassed = {!baseline.IsLearningPhase}");
            sb.AppendLine();
        }

        sb.AppendLine("FLAGGED PATTERN FOR EXPLANATION:");
        sb.AppendLine($"- Pattern Type: {pattern.PatternType}");
        sb.AppendLine($"- Window: {pattern.StartUtc:HH:mm:ss} to {pattern.EndUtc:HH:mm:ss}");
        sb.AppendLine($"- Explanation hint: {pattern.Explanation}");
        sb.AppendLine();
        sb.AppendLine("Provide a clear, neutral 2-sentence explanation of why this was flagged.");

        return sb.ToString();
    }
}

