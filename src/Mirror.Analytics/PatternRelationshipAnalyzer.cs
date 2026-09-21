using Mirror.Core.Domain;
using Mirror.Core.Models;

namespace Mirror.Analytics;

public record PatternCoOccurrence
{
    public BehavioralPatternType PatternA { get; init; }
    public BehavioralPatternType PatternB { get; init; }
    public int CoOccurrenceCount { get; init; }
    public double AverageIntervalMinutes { get; init; }
    public string Summary { get; init; } = string.Empty;
}

public class PatternRelationshipAnalyzer
{
    public IReadOnlyList<PatternCoOccurrence> AnalyzeRelationships(
        IReadOnlyList<PatternEvent> patterns,
        TimeSpan proximityWindow)
    {
        if (patterns.Count < 2)
            return Array.Empty<PatternCoOccurrence>();

        var sorted = patterns.OrderBy(p => p.StartUtc).ToList();
        var pairs = new Dictionary<(BehavioralPatternType, BehavioralPatternType), List<double>>();

        for (int i = 0; i < sorted.Count; i++)
        {
            for (int j = i + 1; j < sorted.Count; j++)
            {
                var p1 = sorted[i];
                var p2 = sorted[j];

                var interval = (p2.StartUtc - p1.EndUtc).TotalMinutes;
                if (interval > proximityWindow.TotalMinutes)
                    break; // Since sorted by StartUtc

                if (p1.PatternType == p2.PatternType)
                    continue;

                var key = p1.PatternType < p2.PatternType 
                    ? (p1.PatternType, p2.PatternType) 
                    : (p2.PatternType, p1.PatternType);

                if (!pairs.ContainsKey(key))
                    pairs[key] = new List<double>();

                pairs[key].Add(Math.Max(0.0, interval));
            }
        }

        var results = new List<PatternCoOccurrence>();
        foreach (var kvp in pairs)
        {
            double avgInterval = kvp.Value.Average();
            results.Add(new PatternCoOccurrence
            {
                PatternA = kvp.Key.Item1,
                PatternB = kvp.Key.Item2,
                CoOccurrenceCount = kvp.Value.Count,
                AverageIntervalMinutes = Math.Round(avgInterval, 1),
                Summary = $"Observed {kvp.Value.Count} instances where {kvp.Key.Item1} and {kvp.Key.Item2} occurred within {proximityWindow.TotalMinutes} minutes of each other (mean interval: {avgInterval:F1}m)."
            });
        }

        return results.OrderByDescending(r => r.CoOccurrenceCount).ToList();
    }
}
