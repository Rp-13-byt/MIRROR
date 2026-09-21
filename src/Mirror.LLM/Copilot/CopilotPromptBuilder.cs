using System.Text;

namespace Mirror.LLM.Copilot;

public static class CopilotPromptBuilder
{
    public const string SystemPrompt =
        "You are Mirror Copilot, a privacy-first, on-device digital activity assistant running on Qualcomm Snapdragon Windows PC.\n" +
        "RULES:\n" +
        "1. All data is processed 100% locally on this device. Zero cloud, zero network requests, zero telemetry.\n" +
        "2. Only reference facts explicitly provided inside <LOCAL_FACTS>. Never invent or hallucinate metrics, applications, or durations.\n" +
        "3. Maintain a neutral, objective, and supportive tone.\n" +
        "4. STRICT ANTI-CLINICAL POLICY: Never diagnose or speculate on clinical or psychiatric states (such as burnout, depression, ADHD, anxiety, addiction, or mental fatigue). If asked, state that Mirror provides only objective behavioral observations.\n" +
        "5. Keep responses concise (2 to 4 sentences or bullet points).\n" +
        "6. Do not judge or moralize activities as 'wasted time' or 'bad habits'.";

    public static string BuildPrompt(string userQuery, MirrorCopilotContext context)
    {
        var sb = new StringBuilder();
        sb.AppendLine(SystemPrompt);
        sb.AppendLine();
        sb.AppendLine("<LOCAL_FACTS>");
        sb.AppendLine($"Date/Range: {context.DateOrRangeLabel}");
        sb.AppendLine($"Active Screen Time: {context.ActiveHours:F1} hours");
        sb.AppendLine($"Idle Time: {context.IdleHours:F1} hours");
        sb.AppendLine($"Application Switches: {context.SwitchCount}");
        sb.AppendLine($"Session Count: {context.SessionCount}");
        sb.AppendLine($"Unique Applications: {context.UniqueAppCount}");
        sb.AppendLine($"Peak Activity Period: {context.PeakRhythmPeriod}");
        sb.AppendLine($"Late Night Activity: {context.LateNightHours:F1} hours");
        sb.AppendLine($"Longest Single Session: {context.LongestSessionAppName} ({context.LongestSessionMinutes:F0} minutes)");
        sb.AppendLine($"Baseline Comparison: {context.BaselineComparison}");

        if (context.TopApps.Count > 0)
        {
            sb.Append("Top Applications: ");
            sb.AppendLine(string.Join(", ", context.TopApps.Select(a => $"{a.DisplayName} ({a.ActiveMinutes:F0}m, {a.PercentageOfTotal:F0}%)")));
        }

        if (context.TopCategories.Count > 0)
        {
            sb.Append("Top Categories: ");
            sb.AppendLine(string.Join(", ", context.TopCategories.Select(c => $"{c.CategoryName} ({c.ActiveMinutes:F0}m, {c.PercentageOfTotal:F0}%)")));
        }

        if (context.DetectedPatterns.Count > 0)
        {
            sb.Append("Detected Patterns: ");
            sb.AppendLine(string.Join(", ", context.DetectedPatterns));
        }
        else
        {
            sb.AppendLine("Detected Patterns: None");
        }

        sb.AppendLine("</LOCAL_FACTS>");
        sb.AppendLine();
        sb.AppendLine("<USER_QUERY>");
        sb.AppendLine(userQuery.Trim());
        sb.AppendLine("</USER_QUERY>");
        sb.AppendLine();
        sb.AppendLine("ASSISTANT RESPONSE:");

        return sb.ToString();
    }
}
