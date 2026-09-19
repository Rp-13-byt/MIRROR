using Mirror.Core.Interfaces;
using Mirror.Core.Models;

namespace Mirror.Analytics;

public class FlowStateDetector : IFlowStateDetector
{
    private readonly IMirrorRepository _repository;

    public FlowStateDetector(IMirrorRepository repository)
    {
        _repository = repository;
    }

    public async Task<FlowStateSession?> EvaluateFlowStateAsync(
        IReadOnlyList<ActivitySession> recentSessions,
        IReadOnlyList<AppSwitchEvent> recentSwitches,
        DateTime evaluationTimeUtc,
        CancellationToken ct = default)
    {
        var candidate = recentSessions
            .Where(s => s.ActiveSeconds >= 3600)
            .OrderByDescending(s => s.ActiveSeconds)
            .FirstOrDefault();

        if (candidate == null)
        {
            return null;
        }

        var switchesInSession = recentSwitches
            .Where(sw => sw.TimestampUtc >= candidate.StartUtc && sw.TimestampUtc <= candidate.EndUtc)
            .ToList();

        int distractionCount = switchesInSession.Count(s => !string.Equals(s.ToAppKey, candidate.AppKey, StringComparison.OrdinalIgnoreCase));
        if (distractionCount > 0)
        {
            return null;
        }

        var existingFlows = await _repository.GetFlowStateSessionsAsync(candidate.StartUtc.AddMinutes(-5), candidate.EndUtc.AddMinutes(5), ct);
        if (existingFlows.Any(f => string.Equals(f.AppKey, candidate.AppKey, StringComparison.OrdinalIgnoreCase)))
        {
            return null;
        }

        var flowSession = new FlowStateSession
        {
            AppKey = candidate.AppKey,
            DisplayName = candidate.DisplayName,
            StartUtc = candidate.StartUtc,
            EndUtc = candidate.EndUtc,
            DurationSeconds = candidate.ActiveSeconds,
            DistractionCount = 0,
            CreatedUtc = evaluationTimeUtc
        };

        await _repository.InsertFlowStateSessionAsync(flowSession, ct);
        return flowSession;
    }
}
