using System.Text.RegularExpressions;

namespace Mirror.LLM.Copilot;

public class FactCheckResult
{
    public bool IsFactuallySound { get; init; }
    public IReadOnlyList<string> Discrepancies { get; init; } = Array.Empty<string>();
}

public class CopilotFactChecker
{
    private static readonly Regex NumberRegex = new(@"\b\d+(\.\d+)?\b", RegexOptions.Compiled);

    public FactCheckResult Verify(string generatedText, MirrorCopilotContext context)
    {
        var discrepancies = new List<string>();

        // Collect all verified numeric values from context
        var allowedNumbers = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            context.ActiveHours.ToString("F1"),
            context.ActiveHours.ToString("F0"),
            Math.Round(context.ActiveHours).ToString(),
            context.IdleHours.ToString("F1"),
            context.IdleHours.ToString("F0"),
            context.SwitchCount.ToString(),
            context.SessionCount.ToString(),
            context.UniqueAppCount.ToString(),
            context.LongestSessionMinutes.ToString("F0"),
            Math.Round(context.LongestSessionMinutes).ToString(),
            context.LateNightHours.ToString("F1")
        };

        foreach (var app in context.TopApps)
        {
            allowedNumbers.Add(app.ActiveMinutes.ToString("F0"));
            allowedNumbers.Add(app.PercentageOfTotal.ToString("F0"));
        }

        foreach (var cat in context.TopCategories)
        {
            allowedNumbers.Add(cat.ActiveMinutes.ToString("F0"));
            allowedNumbers.Add(cat.PercentageOfTotal.ToString("F0"));
        }

        // Standard small numbers that might appear in prose (e.g., 2 sentences, 4 hours, 7 days, 100%, 24 hours, 14 days)
        var proseNumbers = new HashSet<string> { "0", "1", "2", "3", "4", "5", "6", "7", "14", "24", "60", "100" };

        var matches = NumberRegex.Matches(generatedText);
        foreach (Match m in matches)
        {
            string val = m.Value;
            if (!allowedNumbers.Contains(val) && !proseNumbers.Contains(val))
            {
                // Discrepancy or unexpected number
                discrepancies.Add($"Number '{val}' does not match any metric in the verified local context.");
            }
        }

        return new FactCheckResult
        {
            IsFactuallySound = discrepancies.Count <= 1, // Allow 1 margin for formatting / rounding variations
            Discrepancies = discrepancies
        };
    }
}
