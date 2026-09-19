using System.Text;
using Mirror.Core.Models;

namespace Mirror.Voice;

public static class DailyScriptGenerator
{
    public static string GenerateScript(
        DailyMetrics? todayMetrics,
        IReadOnlyList<FlowStateSession>? flowSessions,
        IReadOnlyList<ActivitySession>? recentSessions,
        DateTime now)
    {
        var sb = new StringBuilder();

        // 1. Time-of-day greeting
        int hour = now.Hour;
        string greeting = hour switch
        {
            < 12 => "Good morning.",
            < 17 => "Good afternoon.",
            _ => "Good evening."
        };
        sb.Append(greeting).Append(" ");

        // 2. Screen time summary
        int activeSec = todayMetrics?.ActiveSeconds ?? 0;
        int hours = activeSec / 3600;
        int minutes = (activeSec % 3600) / 60;

        if (activeSec == 0 && (recentSessions == null || recentSessions.Count == 0))
        {
            sb.Append("No active screen time has been recorded yet today. Mirror is standing by silently in the background.");
            return sb.ToString();
        }

        if (hours > 0 && minutes > 0)
        {
            sb.Append($"Today you logged {hours} {(hours == 1 ? "hour" : "hours")} and {minutes} {(minutes == 1 ? "minute" : "minutes")} of screen time");
        }
        else if (hours > 0)
        {
            sb.Append($"Today you logged {hours} {(hours == 1 ? "hour" : "hours")} of screen time");
        }
        else
        {
            sb.Append($"Today you logged {minutes} {(minutes == 1 ? "minute" : "minutes")} of screen time");
        }

        // Top apps
        if (recentSessions != null && recentSessions.Count > 0)
        {
            var topApps = recentSessions
                .GroupBy(s => s.DisplayName)
                .OrderByDescending(g => g.Sum(s => s.ActiveSeconds))
                .Take(2)
                .Select(g => g.Key)
                .ToList();

            if (topApps.Count == 1)
            {
                sb.Append($", primarily in {topApps[0]}. ");
            }
            else if (topApps.Count >= 2)
            {
                sb.Append($", primarily in {topApps[0]} and {topApps[1]}. ");
            }
            else
            {
                sb.Append(". ");
            }
        }
        else if (!string.IsNullOrEmpty(todayMetrics?.TopAppKey))
        {
            sb.Append($", primarily in {todayMetrics.TopAppKey}. ");
        }
        else
        {
            sb.Append(". ");
        }

        // 3. Longest focus block
        int longestSec = todayMetrics?.LongestSessionSeconds ?? 0;
        if (longestSec >= 1200) // 20+ min
        {
            int focusMins = longestSec / 60;
            sb.Append($"Your longest continuous focus block was {focusMins} minutes. ");
        }

        // 4. Flow state moments
        if (flowSessions != null && flowSessions.Count > 0)
        {
            int flowCount = flowSessions.Count;
            int totalFlowMins = flowSessions.Sum(f => f.DurationSeconds) / 60;
            sb.Append($"You experienced {flowCount} {(flowCount == 1 ? "flow state session" : "flow state sessions")}, totaling {totalFlowMins} minutes with zero interruptions. ");
        }

        // 5. Late night check
        int lateNightSec = todayMetrics?.LateNightSeconds ?? 0;
        if (lateNightSec >= 1800) // 30+ min
        {
            int lateMins = lateNightSec / 60;
            sb.Append($"You were active for {lateMins} minutes during late hours. ");
        }

        // 6. Sign-off
        sb.Append("Have a restful time.");

        return sb.ToString();
    }
}