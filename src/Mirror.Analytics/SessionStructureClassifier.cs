using Mirror.Core.Domain;
using Mirror.Core.Models;

namespace Mirror.Analytics;

public enum SessionStructureType
{
    Focused,
    Fragmented,
    LongForm,
    SwitchHeavy,
    ReopenHeavy,
    Mixed
}

public record ClassifiedSessionBlock
{
    public SessionStructureType StructureType { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public SessionFingerprint Fingerprint { get; init; } = null!;
    public DateTime StartUtc { get; init; }
    public DateTime EndUtc { get; init; }
}

public class SessionStructureClassifier
{
    public ClassifiedSessionBlock ClassifyBlock(
        DateTime startUtc,
        DateTime endUtc,
        IReadOnlyList<ActivitySession> sessions,
        IReadOnlyList<AppSwitchEvent> switches,
        IReadOnlyList<IdlePeriod> idlePeriods)
    {
        double durationMinutes = Math.Max(0.1, (endUtc - startUtc).TotalMinutes);
        int totalSwitches = switches.Count;
        int uniqueApps = sessions.Select(s => s.AppKey).Distinct().Count();
        
        // Count rapid reopens (same app switched to within 60s)
        int reopens = 0;
        for (int i = 1; i < switches.Count; i++)
        {
            if (switches[i].ToAppKey == switches[i - 1].FromAppKey &&
                (switches[i].TimestampUtc - switches[i - 1].TimestampUtc).TotalSeconds <= 60)
            {
                reopens++;
            }
        }

        int interruptions = idlePeriods.Count(i => i.DurationSeconds >= 120);
        long activeSeconds = sessions.Sum(s => s.ActiveSeconds);
        double activeRatio = (endUtc - startUtc).TotalSeconds > 0 
            ? Math.Min(1.0, activeSeconds / (endUtc - startUtc).TotalSeconds) 
            : 0.0;

        string dominantCategory = sessions
            .GroupBy(s => s.Category)
            .OrderByDescending(g => g.Sum(x => x.ActiveSeconds))
            .FirstOrDefault()?.Key ?? "General";

        var fingerprint = SessionFingerprint.Create(
            durationMinutes,
            totalSwitches,
            uniqueApps,
            reopens,
            interruptions,
            activeRatio,
            dominantCategory);

        var (type, title, description) = DetermineStructure(fingerprint, durationMinutes);

        return new ClassifiedSessionBlock
        {
            StructureType = type,
            Title = title,
            Description = description,
            Fingerprint = fingerprint,
            StartUtc = startUtc,
            EndUtc = endUtc
        };
    }

    private static (SessionStructureType Type, string Title, string Description) DetermineStructure(
        SessionFingerprint fp,
        double durationMinutes)
    {
        if (fp.ReopenCount >= 4 && fp.SwitchRatePerMinute >= 2.0)
        {
            return (
                SessionStructureType.ReopenHeavy,
                "Repetitive Context Reopen Block",
                $"Observed {fp.ReopenCount} rapid re-opens across {fp.UniqueAppCount} applications with a switch rate of {fp.SwitchRatePerMinute:F1}/min."
            );
        }

        if (fp.SwitchRatePerMinute >= 3.0)
        {
            return (
                SessionStructureType.SwitchHeavy,
                "High Transition Density Block",
                $"Frequent application toggling recorded ({fp.SwitchRatePerMinute:F1} switches/min across {fp.UniqueAppCount} apps)."
            );
        }

        if (fp.InterruptionCount >= 3 || (fp.ActiveRatio < 0.6 && durationMinutes >= 15))
        {
            return (
                SessionStructureType.Fragmented,
                "Intermittent Activity Block",
                $"Activity was dispersed with {fp.InterruptionCount} pauses exceeding 2 minutes, yielding an active ratio of {fp.ActiveRatio * 100:F0}%."
            );
        }

        if (durationMinutes >= 60 && fp.UniqueAppCount <= 2 && fp.SwitchRatePerMinute < 0.8)
        {
            return (
                SessionStructureType.LongForm,
                "Sustained Deep Engagement Block",
                $"Uninterrupted focus session of {durationMinutes:F0} minutes concentrated in {fp.DominantCategory} with minimal transitions."
            );
        }

        if (fp.ActiveRatio >= 0.8 && fp.SwitchRatePerMinute <= 1.5 && fp.UniqueAppCount <= 3)
        {
            return (
                SessionStructureType.Focused,
                "Cohesive Focused Block",
                $"Continuous engagement ({fp.ActiveRatio * 100:F0}% active) with low switching density."
            );
        }

        return (
            SessionStructureType.Mixed,
            "Standard Mixed Workflow Block",
            $"Varied activity involving {fp.UniqueAppCount} applications over {durationMinutes:F0} minutes."
        );
    }
}
