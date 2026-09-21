using Mirror.Core.Interfaces;

namespace Mirror.LLM.Copilot;

public record CopilotAction(string Label, string NavigationTarget);

public record CopilotAnswer
{
    public string Question { get; init; } = string.Empty;
    public string AnswerText { get; init; } = string.Empty;
    public CopilotIntent Intent { get; init; }
    public MirrorCopilotContext Context { get; init; } = null!;
    public string BackendDisplayName { get; init; } = string.Empty;
    public double LatencyMs { get; init; }
    public bool WasRefusedOrRedirected { get; init; }
    public string? RejectionReason { get; init; }
    public IReadOnlyList<CopilotAction> SuggestedActions { get; init; } = Array.Empty<CopilotAction>();
    public DateTime Timestamp { get; init; } = DateTime.Now;
}

public interface IMirrorCopilotService
{
    Task<CopilotAnswer> AskAsync(string question, DateTime? referenceTimeLocal = null, CancellationToken ct = default);
}

public class MirrorCopilotService : IMirrorCopilotService
{
    private readonly QuestionClassifier _classifier;
    private readonly LocalQueryPlanner _planner;
    private readonly CopilotPolicyGuard _policyGuard;
    private readonly CopilotResponseValidator _validator;
    private readonly IAiHubModelRuntime _runtime;

    public MirrorCopilotService(IMirrorRepository repository, IAiHubModelRuntime? runtime = null)
    {
        _classifier = new QuestionClassifier();
        _planner = new LocalQueryPlanner(repository);
        _policyGuard = new CopilotPolicyGuard();
        _validator = new CopilotResponseValidator();
        _runtime = runtime ?? new AiHubModelRuntime();
    }

    public async Task<CopilotAnswer> AskAsync(
        string question,
        DateTime? referenceTimeLocal = null,
        CancellationToken ct = default)
    {
        var refTime = referenceTimeLocal ?? DateTime.Now;
        var intent = _classifier.Classify(question);

        // Fetch grounded local context
        var context = await _planner.PlanAndFetchAsync(intent, refTime, ct);

        // Intercept clinical / diagnostic prompts immediately
        if (intent == CopilotIntent.ClinicalInterception || _policyGuard.RequiresClinicalRefusal(question))
        {
            string refusal = _policyGuard.GenerateNonClinicalRefusal(context);
            return new CopilotAnswer
            {
                Question = question,
                AnswerText = refusal,
                Intent = CopilotIntent.ClinicalInterception,
                Context = context,
                BackendDisplayName = _runtime.ActiveBackendName,
                LatencyMs = 2.0,
                WasRefusedOrRedirected = true,
                RejectionReason = "Query requested clinical/psychiatric evaluation.",
                SuggestedActions = new[]
                {
                    new CopilotAction("View Privacy Guarantees", "Privacy"),
                    new CopilotAction("Open Activity Timeline", "Timeline")
                }
            };
        }

        // Intercept privacy inquiries with direct factual assurance
        if (intent == CopilotIntent.PrivacyAudit)
        {
            string privacyAnswer =
                "Mirror is engineered with zero network communication. All activity monitoring, pattern inference, and copilot reasoning run 100% on your local device.\n\n" +
                $"• Database location: Local AppData (DPAPI encrypted)\n" +
                $"• Stored metrics: Process names, active seconds, switch counts\n" +
                $"• Never stored: URLs, window titles, document contents, keystrokes\n" +
                $"• Active AI Provider: {_runtime.ActiveBackendName}";

            return new CopilotAnswer
            {
                Question = question,
                AnswerText = privacyAnswer,
                Intent = CopilotIntent.PrivacyAudit,
                Context = context,
                BackendDisplayName = _runtime.ActiveBackendName,
                LatencyMs = 1.5,
                WasRefusedOrRedirected = false,
                SuggestedActions = new[]
                {
                    new CopilotAction("Inspect Privacy Dashboard", "Privacy"),
                    new CopilotAction("Data Inventory & Purge", "Privacy")
                }
            };
        }

        // Build prompt and run inference
        string prompt = CopilotPromptBuilder.BuildPrompt(question, context);
        var inferenceResult = await _runtime.GenerateAsync(prompt, context, ct);

        // Validate response
        var validation = _validator.Validate(inferenceResult.GeneratedText, context);

        var actions = new List<CopilotAction>
        {
            new CopilotAction("Open Timeline", "Timeline"),
            new CopilotAction("Show Insights", "Insights")
        };

        return new CopilotAnswer
        {
            Question = question,
            AnswerText = validation.SanitizedOutput,
            Intent = intent,
            Context = context,
            BackendDisplayName = inferenceResult.BackendDisplayName,
            LatencyMs = inferenceResult.LatencyMs,
            WasRefusedOrRedirected = !validation.IsValid,
            RejectionReason = validation.RejectionReason,
            SuggestedActions = actions
        };
    }
}
