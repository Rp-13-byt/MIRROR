using System.Text.RegularExpressions;

namespace Mirror.LLM.Copilot;

public class CopilotPolicyGuard
{
    private static readonly Regex ProhibitedClinicalTerms = new(
        @"\b(adhd|depression|depressed|burnout|burned\s*out|anxiety|anxious|addict|addiction|mental\s*illness|diagnos(e|is)|dopamine|trauma|stress(ed)?\s*out)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex JudgmentalPhrases = new(
        @"\b(wast(ed|ing)\s*time|procrastinat(ing|ion)|bad\s*habit|unproductive\s*day|lazy)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public bool RequiresClinicalRefusal(string text)
    {
        return ProhibitedClinicalTerms.IsMatch(text);
    }

    public bool ContainsJudgmentalLanguage(string text)
    {
        return JudgmentalPhrases.IsMatch(text);
    }

    public string GenerateNonClinicalRefusal(MirrorCopilotContext context)
    {
        return
            "Mirror is an on-device digital activity companion and does not provide clinical, psychological, or medical evaluations (such as assessing burnout, ADHD, anxiety, or depression).\n\n" +
            "Here are your objective activity metrics for the requested period:\n" +
            $"• Active screen time: {context.ActiveHours:F1} hours ({context.BaselineComparison})\n" +
            $"• Application switches: {context.SwitchCount}\n" +
            $"• Longest single focus block: {context.LongestSessionAppName} ({context.LongestSessionMinutes:F0} minutes)\n" +
            $"• Peak activity period: {context.PeakRhythmPeriod}\n\n" +
            "If you are feeling overwhelmed or fatigued, consider stepping away from the screen or speaking with a qualified healthcare professional.";
    }
}
