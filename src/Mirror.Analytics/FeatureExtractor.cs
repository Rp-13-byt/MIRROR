using Mirror.Core.Interfaces;
using Mirror.Core.Models;

namespace Mirror.Analytics;

public class FeatureExtractor : IFeatureExtractor
{
    public const int Timesteps = 60;
    public const int FeatureCount = 10;

    public FeatureSequence ExtractFeatureSequence(
        IReadOnlyList<ActivitySession> sessions,
        IReadOnlyList<AppSwitchEvent> switches,
        DateTime windowEndUtc,
        int windowMinutes = 60)
    {
        var sequence = new FeatureSequence(Timesteps, FeatureCount)
        {
            StartUtc = windowEndUtc.AddMinutes(-windowMinutes),
            EndUtc = windowEndUtc
        };

        DateTime windowStartUtc = sequence.StartUtc;

        for (int step = 0; step < Timesteps; step++)
        {
            DateTime minuteStart = windowStartUtc.AddMinutes(step);
            DateTime minuteEnd = minuteStart.AddMinutes(1);

            // Filter sessions overlapping with this minute
            var minuteSessions = sessions.Where(s => s.StartUtc < minuteEnd && s.EndUtc > minuteStart).ToList();
            var minuteSwitches = switches.Where(sw => sw.TimestampUtc >= minuteStart && sw.TimestampUtc < minuteEnd).ToList();

            float[] features = ExtractMinuteFeatures(minuteSessions, minuteSwitches, minuteStart);
            for (int f = 0; f < FeatureCount; f++)
            {
                sequence.Values[step, f] = features[f];
            }
        }

        return sequence;
    }

    public float[] ExtractMinuteFeatures(
        IReadOnlyList<ActivitySession> sessionsInMinute,
        IReadOnlyList<AppSwitchEvent> switchesInMinute,
        DateTime minuteStartUtc)
    {
        var features = new float[FeatureCount];
        if (sessionsInMinute.Count == 0 && switchesInMinute.Count == 0)
        {
            // Empty minute
            features[7] = IsLateNight(minuteStartUtc) ? 1.0f : 0.0f;
            return features;
        }

        DateTime minuteEndUtc = minuteStartUtc.AddMinutes(1);

        // 1. Active seconds in this minute (0..60) -> normalized to [0..1]
        double totalActiveSec = 0;
        var appDurations = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        var categories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var s in sessionsInMinute)
        {
            DateTime segStart = s.StartUtc > minuteStartUtc ? s.StartUtc : minuteStartUtc;
            DateTime segEnd = s.EndUtc < minuteEndUtc ? s.EndUtc : minuteEndUtc;
            double sec = Math.Max(0, (segEnd - segStart).TotalSeconds);

            totalActiveSec += sec;
            if (!appDurations.ContainsKey(s.AppKey))
                appDurations[s.AppKey] = 0;
            appDurations[s.AppKey] += sec;

            if (!string.IsNullOrEmpty(s.Category))
                categories.Add(s.Category);
        }

        totalActiveSec = Math.Min(60.0, totalActiveSec);
        features[0] = (float)(totalActiveSec / 60.0);

        // 2. Switch count in minute normalized [0..1] (capped at 10)
        int switchCount = switchesInMinute.Count;
        features[1] = Math.Min(1.0f, switchCount / 10.0f);

        // 3. Unique apps count in minute normalized [0..1] (capped at 5)
        int uniqueApps = appDurations.Count;
        features[2] = Math.Min(1.0f, uniqueApps / 5.0f);

        // 4. Reopen count: if multiple switches reference the same destination app
        int reopens = switchesInMinute
            .GroupBy(sw => sw.ToAppKey)
            .Where(g => g.Count() > 1)
            .Sum(g => g.Count() - 1);
        features[3] = Math.Min(1.0f, reopens / 4.0f);

        // 5. Longest session seconds in minute normalized [0..1]
        double longest = appDurations.Values.Count > 0 ? appDurations.Values.Max() : 0;
        features[4] = (float)Math.Min(1.0, longest / 60.0);

        // 6. Top app share [0..1]
        features[5] = totalActiveSec > 0 ? (float)(longest / totalActiveSec) : 0.0f;

        // 7. App entropy [0..1]
        features[6] = CalculateEntropy(appDurations.Values, totalActiveSec);

        // 8. Late night ratio (1.0 if between 23:00 and 04:00 local time)
        features[7] = IsLateNight(minuteStartUtc) ? 1.0f : 0.0f;

        // 9. Single app ratio [0..1]
        features[8] = uniqueApps == 1 && totalActiveSec > 30 ? 1.0f : features[5];

        // 10. Category diversity [0..1] (capped at 4 categories)
        features[9] = Math.Min(1.0f, categories.Count / 4.0f);

        return features;
    }

    private static bool IsLateNight(DateTime utcTime)
    {
        // Local clock conversion for late-night hour check
        DateTime local = utcTime.ToLocalTime();
        int hour = local.Hour;
        return hour >= 23 || hour < 4;
    }

    private static float CalculateEntropy(IEnumerable<double> durations, double totalDuration)
    {
        if (totalDuration <= 0) return 0f;

        double entropy = 0;
        int count = 0;
        foreach (var d in durations)
        {
            if (d <= 0) continue;
            double p = d / totalDuration;
            entropy -= p * Math.Log2(p);
            count++;
        }

        if (count <= 1) return 0f;
        double maxEntropy = Math.Log2(count);
        return (float)Math.Clamp(entropy / maxEntropy, 0.0, 1.0);
    }
}
