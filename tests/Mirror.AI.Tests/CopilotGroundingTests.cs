using Mirror.LLM.Copilot;
using Xunit;

namespace Mirror.AI.Tests;

public class CopilotGroundingTests
{
    [Fact]
    public void FactChecker_AllowsNumbers_PresentInContext()
    {
        var context = new MirrorCopilotContext
        {
            DateOrRangeLabel = "Today",
            ActiveHours = 4.5,
            IdleHours = 1.2,
            SwitchCount = 42,
            SessionCount = 8,
            UniqueAppCount = 5,
            LongestSessionAppName = "Visual Studio Code",
            LongestSessionMinutes = 75,
            TopApps = new[]
            {
                new AppUsageSummary("Visual Studio Code", 75, 45),
                new AppUsageSummary("Microsoft Edge", 30, 18)
            }
        };

        var checker = new CopilotFactChecker();
        string generatedText = "During Today, you recorded 4.5 hours of active time with 42 application switches. Your longest session was 75 minutes in Visual Studio Code.";

        var result = checker.Verify(generatedText, context);

        Assert.True(result.IsFactuallySound);
        Assert.Empty(result.Discrepancies);
    }

    [Fact]
    public void FactChecker_RejectsHallucinatedNumbers_NotInContext()
    {
        var context = new MirrorCopilotContext
        {
            DateOrRangeLabel = "Today",
            ActiveHours = 2.0,
            SwitchCount = 10,
            SessionCount = 2,
            UniqueAppCount = 2,
            LongestSessionAppName = "Notepad",
            LongestSessionMinutes = 30
        };

        var checker = new CopilotFactChecker();
        // Model hallucinates "9 hours" and "88 switches"
        string hallucinatedText = "You spent 9 hours on the screen today with 88 switches and opened 99 apps.";

        var result = checker.Verify(hallucinatedText, context);

        Assert.False(result.IsFactuallySound);
        Assert.NotEmpty(result.Discrepancies);
    }

    [Fact]
    public void Validator_FallsBackToDeterministic_WhenHallucinationDetected()
    {
        var context = new MirrorCopilotContext
        {
            DateOrRangeLabel = "September 21, 2026",
            ActiveHours = 3.0,
            SwitchCount = 15,
            SessionCount = 4,
            PeakRhythmPeriod = "Morning",
            BaselineComparison = "Within normal 14-day baseline range"
        };

        var validator = new CopilotResponseValidator();
        string hallucinated = "You were active for 15 hours and switched 500 times across 80 applications.";

        var valResult = validator.Validate(hallucinated, context);

        Assert.False(valResult.IsValid);
        Assert.NotNull(valResult.RejectionReason);
        Assert.Contains("3.0 hours", valResult.SanitizedOutput);
        Assert.Contains("15 application transitions", valResult.SanitizedOutput);
    }
}
