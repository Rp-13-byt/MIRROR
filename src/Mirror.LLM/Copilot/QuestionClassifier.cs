using System.Text.RegularExpressions;

namespace Mirror.LLM.Copilot;

public enum CopilotIntent
{
    DailySummary,
    DayComparison,
    WeeklyTrends,
    PatternInquiry,
    LongestSession,
    LateNightActivity,
    AppSwitching,
    DataExplorer,
    PrivacyAudit,
    ClinicalInterception,
    GeneralQuery
}

public class QuestionClassifier
{
    private static readonly Regex ClinicalRegex = new(
        @"\b(adhd|depression|depressed|burnout|burned\s*out|anxiety|anxious|addict|addiction|mental\s*illness|diagnos(e|is)|dopamine|trauma|stress(ed)?\s*out)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public CopilotIntent Classify(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return CopilotIntent.GeneralQuery;

        string normalized = query.Trim().ToLowerInvariant();

        // 1. Check for clinical / psychiatric keywords immediately
        if (ClinicalRegex.IsMatch(normalized))
            return CopilotIntent.ClinicalInterception;

        // 2. Privacy and local data ownership
        if (normalized.Contains("privacy") || normalized.Contains("cloud") || normalized.Contains("telemetry") ||
            normalized.Contains("server") || normalized.Contains("send") || normalized.Contains("leak") ||
            normalized.Contains("where is my data") || normalized.Contains("who can see"))
        {
            return CopilotIntent.PrivacyAudit;
        }

        // 3. Day comparison
        if (normalized.Contains("yesterday") || normalized.Contains("compare") || normalized.Contains("versus") || normalized.Contains("vs"))
        {
            return CopilotIntent.DayComparison;
        }

        // 4. Weekly trends
        if (normalized.Contains("week") || normalized.Contains("trend") || normalized.Contains("past 7 days") || normalized.Contains("over time"))
        {
            return CopilotIntent.WeeklyTrends;
        }

        // 5. Longest session / deep focus
        if (normalized.Contains("longest") || normalized.Contains("deep work") || normalized.Contains("single app") || normalized.Contains("most time"))
        {
            return CopilotIntent.LongestSession;
        }

        // 6. Late night
        if (normalized.Contains("late night") || normalized.Contains("midnight") || normalized.Contains("sleep") || normalized.Contains("night"))
        {
            return CopilotIntent.LateNightActivity;
        }

        // 7. App switching / fragmentation
        if (normalized.Contains("switch") || normalized.Contains("fragment") || normalized.Contains("multitask") || normalized.Contains("interrupt"))
        {
            return CopilotIntent.AppSwitching;
        }

        // 8. Pattern inquiry
        if (normalized.Contains("pattern") || normalized.Contains("flagged") || normalized.Contains("detected") || normalized.Contains("why was"))
        {
            return CopilotIntent.PatternInquiry;
        }

        // 9. Daily summary
        if (normalized.Contains("today") || normalized.Contains("summary") || normalized.Contains("summarize") || normalized.Contains("overview") || normalized.Contains("what did i do"))
        {
            return CopilotIntent.DailySummary;
        }

        // 10. Data explorer
        if (normalized.Contains("store") || normalized.Contains("track") || normalized.Contains("database") || normalized.Contains("record"))
        {
            return CopilotIntent.DataExplorer;
        }

        return CopilotIntent.DailySummary; // Default to daily context if general
    }
}
