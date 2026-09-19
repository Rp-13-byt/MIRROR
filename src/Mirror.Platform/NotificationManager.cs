using Mirror.Core.Models;

namespace Mirror.Platform;

public interface INotificationManager
{
    bool CanSendNotification(UserSettings settings);
    void SendDailySummary(int activeSeconds, int appCount, UserSettings settings);
    void SendPatternNotice(string patternName, string neutralSummary, UserSettings settings);
}

public class NotificationManager : INotificationManager
{
    private DateTime _lastNotificationDate = DateTime.MinValue;

    public bool CanSendNotification(UserSettings settings)
    {
        if (!settings.NotificationsEnabled) return false;

        // Check quiet hours
        int currentHour = DateTime.Now.Hour;
        if (settings.QuietHoursStart > settings.QuietHoursEnd)
        {
            // E.g. 23:00 to 07:00
            if (currentHour >= settings.QuietHoursStart || currentHour < settings.QuietHoursEnd)
            {
                return false;
            }
        }
        else
        {
            if (currentHour >= settings.QuietHoursStart && currentHour < settings.QuietHoursEnd)
            {
                return false;
            }
        }

        // Hard rate limit: max 1 proactive notification per calendar day
        if (DateTime.Now.Date <= _lastNotificationDate.Date)
        {
            return false;
        }

        return true;
    }

    public void SendDailySummary(int activeSeconds, int appCount, UserSettings settings)
    {
        if (!settings.NotificationsEnabled || !settings.DailySummaryNotification) return;

        TimeSpan activeTime = TimeSpan.FromSeconds(activeSeconds);
        string timeStr = $"{(int)activeTime.TotalHours}h {activeTime.Minutes}m";

        string title = "Mirror — Daily Summary";
        string message = $"Today's summary: {timeStr} active across {appCount} applications.";

        EmitLocalNotification(title, message);
        _lastNotificationDate = DateTime.Now;
    }

    public void SendPatternNotice(string patternName, string neutralSummary, UserSettings settings)
    {
        if (!CanSendNotification(settings)) return;

        string title = $"Mirror — {patternName}";
        string message = neutralSummary;

        EmitLocalNotification(title, message);
        _lastNotificationDate = DateTime.Now;
    }

    private static void EmitLocalNotification(string title, string message)
    {
        // Safe local Windows notification emission
        // When running unpackaged or packaged without AppNotification registration, fallback gracefully
        try
        {
            // Windows App SDK notification or console/diagnostic trace
            System.Diagnostics.Debug.WriteLine($"[Notification] {title}: {message}");
        }
        catch
        {
            // Best effort
        }
    }
}
