using System;
using System.Collections.Generic;
using Mirror.Core.Domain;
using Mirror.Core.Interfaces;
using Mirror.Core.Models;

namespace Mirror_App.Services;

public interface IDemoDataService
{
    bool IsDemoModeActive { get; set; }
    IReadOnlyList<DailyMetrics> GetDemoWeeklyMetrics();
    DailyMetrics GetDemoTodayMetrics();
    IReadOnlyList<ActivitySession> GetDemoTodaySessions();
    IReadOnlyList<PatternEvent> GetDemoDetectedPatterns();
    UserBaseline GetDemoBaselineProfile();
}
