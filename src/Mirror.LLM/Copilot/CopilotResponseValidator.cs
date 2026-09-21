namespace Mirror.LLM.Copilot;

public class ValidationResult
{
    public bool IsValid { get; init; }
    public string SanitizedOutput { get; init; } = string.Empty;
    public string? RejectionReason { get; init; }
}

public class CopilotResponseValidator
{
    private readonly CopilotPolicyGuard _policyGuard;
    private readonly CopilotFactChecker _factChecker;

    public CopilotResponseValidator()
    {
        _policyGuard = new CopilotPolicyGuard();
        _factChecker = new CopilotFactChecker();
    }

    public ValidationResult Validate(string generatedText, MirrorCopilotContext context)
    {
        // 1. Safety check
        if (_policyGuard.RequiresClinicalRefusal(generatedText))
        {
            return new ValidationResult
            {
                IsValid = false,
                SanitizedOutput = _policyGuard.GenerateNonClinicalRefusal(context),
                RejectionReason = "Response contained prohibited clinical terminology."
            };
        }

        // 2. Fact check
        var factResult = _factChecker.Verify(generatedText, context);
        if (!factResult.IsFactuallySound)
        {
            return new ValidationResult
            {
                IsValid = false,
                SanitizedOutput = GenerateDeterministicGroundedResponse(context),
                RejectionReason = $"Factual inconsistency: {string.Join("; ", factResult.Discrepancies)}"
            };
        }

        return new ValidationResult
        {
            IsValid = true,
            SanitizedOutput = generatedText.Trim(),
            RejectionReason = null
        };
    }

    public static string GenerateDeterministicGroundedResponse(MirrorCopilotContext context)
    {
        string topAppPart = context.TopApps.Count > 0
            ? $"Most active application was {context.TopApps[0].DisplayName} ({context.TopApps[0].ActiveMinutes:F0}m)."
            : "";

        return
            $"During {context.DateOrRangeLabel}, you recorded {context.ActiveHours:F1} hours of active screen time across {context.SessionCount} sessions and {context.SwitchCount} application transitions. " +
            $"{topAppPart} Your peak activity occurred in the {context.PeakRhythmPeriod}. Status: {context.BaselineComparison}.";
    }
}
