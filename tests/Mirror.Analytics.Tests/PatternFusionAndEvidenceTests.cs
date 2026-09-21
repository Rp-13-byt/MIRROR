using Mirror.Analytics;
using Mirror.Core.Domain;
using Mirror.Core.Models;
using Xunit;

namespace Mirror.Analytics.Tests;

public class PatternFusionAndEvidenceTests
{
    [Fact]
    public void FuseSignals_RuleAndMlAgree_ElevatesConfidenceAndRecordsAgreement()
    {
        var fusion = new PatternFusionEngine();
        var ruleCandidate = new PatternEvent
        {
            PatternType = BehavioralPatternType.HighSwitchingBurst,
            StartUtc = DateTime.UtcNow.AddMinutes(-10),
            EndUtc = DateTime.UtcNow,
            SwitchCount = 12,
            UniqueApps = 4,
            DurationSeconds = 600,
            LateNightMinutes = 0,
            Explanation = "Rapid switching detected",
            RuleSignal = true,
            ModelSignal = false,
            ModelConfidence = 0.70f
        };

        var mlResult = new InferenceResult
        {
            BackendUsed = InferenceBackendKind.Cpu,
            DetectedPattern = BehavioralPatternType.HighSwitchingBurst,
            Confidence = 0.80f,
            ClassProbabilities = new[] { 0.1f, 0.8f, 0.05f, 0.05f, 0f },
            InferenceTimeMs = 5.2,
            ModelVersion = "1.0.0",
            IsUncertain = false
        };

        var fused = fusion.FuseSignals(ruleCandidate, mlResult);

        Assert.NotNull(fused);
        Assert.True(fused.ModelSignal);
        Assert.True(fused.ModelConfidence > 0.70f);
        Assert.Contains("Corroborated by local ONNX model", fused.Explanation);

        var diag = fusion.GetAgreementDiagnostics();
        Assert.Equal(1, diag.TotalEvaluations);
        Assert.Equal(1, diag.RuleAndModelAgreedCount);
        Assert.Equal(0, diag.DisagreementCount);
        Assert.Equal(1.0, diag.RuleAndModelAgreementRate);
    }

    [Fact]
    public void FuseSignals_RuleFires_MlUncertain_RuleRemainsAuthoritativeWithNote()
    {
        var fusion = new PatternFusionEngine();
        var ruleCandidate = new PatternEvent
        {
            PatternType = BehavioralPatternType.ExtendedSingleAppSession,
            StartUtc = DateTime.UtcNow.AddMinutes(-50),
            EndUtc = DateTime.UtcNow,
            SwitchCount = 1,
            UniqueApps = 1,
            DurationSeconds = 3000,
            LateNightMinutes = 0,
            Explanation = "Extended session",
            RuleSignal = true,
            ModelSignal = false,
            ModelConfidence = 0.85f
        };

        var mlResult = new InferenceResult
        {
            BackendUsed = InferenceBackendKind.Cpu,
            DetectedPattern = BehavioralPatternType.Uncertain,
            Confidence = 0.45f,
            ClassProbabilities = new[] { 0.3f, 0.2f, 0.45f, 0.05f, 0f },
            InferenceTimeMs = 4.1,
            ModelVersion = "1.0.0",
            IsUncertain = true,
            AbstentionReason = "Low confidence"
        };

        var fused = fusion.FuseSignals(ruleCandidate, mlResult);

        Assert.NotNull(fused);
        Assert.False(fused.ModelSignal);
        Assert.Contains("Local model abstained", fused.Explanation);

        var diag = fusion.GetAgreementDiagnostics();
        Assert.Equal(1, diag.TotalEvaluations);
        Assert.Equal(1, diag.RuleOnlyCount);
        Assert.Equal(1, diag.UncertainCount);
    }

    [Fact]
    public void FuseSignals_RuleFires_MlDisagrees_RecordsDisagreement()
    {
        var fusion = new PatternFusionEngine();
        var ruleCandidate = new PatternEvent
        {
            PatternType = BehavioralPatternType.HighSwitchingBurst,
            StartUtc = DateTime.UtcNow.AddMinutes(-10),
            EndUtc = DateTime.UtcNow,
            SwitchCount = 15,
            UniqueApps = 5,
            DurationSeconds = 600,
            LateNightMinutes = 0,
            Explanation = "Switch burst",
            RuleSignal = true,
            ModelSignal = false,
            ModelConfidence = 0.75f
        };

        var mlResult = new InferenceResult
        {
            BackendUsed = InferenceBackendKind.Cpu,
            DetectedPattern = BehavioralPatternType.RapidReopenPattern,
            Confidence = 0.78f,
            ClassProbabilities = new[] { 0.1f, 0.1f, 0.02f, 0.78f, 0f },
            InferenceTimeMs = 3.9,
            ModelVersion = "1.0.0",
            IsUncertain = false
        };

        var fused = fusion.FuseSignals(ruleCandidate, mlResult);

        Assert.NotNull(fused);
        // Rule remains authoritative
        Assert.Equal(BehavioralPatternType.HighSwitchingBurst, fused.PatternType);
        Assert.False(fused.ModelSignal);

        var diag = fusion.GetAgreementDiagnostics();
        Assert.Equal(1, diag.DisagreementCount);
    }

    [Fact]
    public void FuseSignals_NoRule_HighConfidenceMl_MaintainsRuleAuthoritativeBaselineAndTracksDiagnostics()
    {
        var fusion = new PatternFusionEngine();
        var mlResult = new InferenceResult
        {
            BackendUsed = InferenceBackendKind.DirectMl,
            DetectedPattern = BehavioralPatternType.HighSwitchingBurst,
            Confidence = 0.92f,
            ClassProbabilities = new[] { 0.05f, 0.92f, 0.01f, 0.01f, 0.01f },
            InferenceTimeMs = 2.4,
            ModelVersion = "1.0.0",
            IsUncertain = false
        };

        var fused = fusion.FuseSignals(null, mlResult);

        // Rule is authoritative baseline to prevent hallucinated warnings: must be null
        Assert.Null(fused);

        var diag = fusion.GetAgreementDiagnostics();
        Assert.Equal(1, diag.TotalEvaluations);
        Assert.Equal(1, diag.ModelOnlyCount);
    }

    [Fact]
    public void InsightExplorerService_BuildEvidence_ProducesDetailedEvidence()
    {
        var explorer = new InsightExplorerService();
        var patternEvent = new PatternEvent
        {
            PatternType = BehavioralPatternType.HighSwitchingBurst,
            StartUtc = DateTime.UtcNow.AddMinutes(-10),
            EndUtc = DateTime.UtcNow,
            SwitchCount = 14,
            UniqueApps = 4,
            DurationSeconds = 600,
            LateNightMinutes = 0,
            Explanation = "High switching",
            RuleSignal = true,
            ModelSignal = true,
            ModelConfidence = 0.88f
        };

        var evidence = explorer.BuildEvidence(patternEvent);

        Assert.NotNull(evidence);
        Assert.Equal(BehavioralPatternType.HighSwitchingBurst, evidence.PatternType);
        Assert.Equal(14, evidence.SwitchCount);
        Assert.Equal(4, evidence.DistinctApplicationCount);
        Assert.True(evidence.RuleTriggered);
        Assert.True(evidence.ModelSupported);
        Assert.Equal(ConfidenceCategory.High, evidence.ConfidenceCategory);
        Assert.Contains("14 app switches across 4 applications", evidence.RuleEvidence);
        Assert.Contains("without sustained dwell time", evidence.Explanation);
    }

    [Fact]
    public void EventReplayEngine_ReplaysTimeline_DeterministicOutput()
    {
        var replayEngine = new EventReplayEngine();
        var now = DateTime.UtcNow;

        var switches = new List<AppSwitchEvent>();
        var apps = new[] { "dev.vscode", "browser.edge", "comm.slack", "dev.terminal" };
        for (int i = 0; i < 15; i++)
        {
            switches.Add(new AppSwitchEvent
            {
                Id = i + 1,
                FromAppKey = apps[i % apps.Length],
                ToAppKey = apps[(i + 1) % apps.Length],
                TimestampUtc = now.AddMinutes(-9).AddSeconds(i * 30)
            });
        }

        var sessions = new List<ActivitySession>
        {
            new()
            {
                Id = 1,
                AppKey = "dev.vscode",
                DisplayName = "Visual Studio Code",
                Category = "Development",
                StartUtc = now.AddMinutes(-10),
                EndUtc = now,
                ActiveSeconds = 600,
                CloseReason = SessionCloseReason.AppSwitch
            }
        };

        var report = replayEngine.ReplayTimeline(sessions, switches);

        Assert.NotNull(report);
        Assert.Equal(16, report.TotalEventsReplayed);
        Assert.Equal(1, report.TotalSessionsConstructed);
        Assert.NotEmpty(report.DetectedPatterns);
        Assert.Contains(report.DetectedPatterns, p => p.PatternType == BehavioralPatternType.HighSwitchingBurst);
    }
}
