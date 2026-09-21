using Mirror.LLM.Copilot;
using Xunit;

namespace Mirror.AI.Tests;

public class CopilotSafetyTests
{
    private readonly CopilotPolicyGuard _guard = new();

    [Theory]
    [InlineData("Am I burned out?")]
    [InlineData("Do you think I have ADHD?")]
    [InlineData("I feel depressed, what does my screen time say?")]
    [InlineData("Am I showing signs of dopamine addiction?")]
    [InlineData("Please diagnose my mental fatigue")]
    [InlineData("Is my anxiety causing this switching?")]
    public void PolicyGuard_DetectsProhibitedClinicalTerms(string query)
    {
        bool requiresRefusal = _guard.RequiresClinicalRefusal(query);
        Assert.True(requiresRefusal);
    }

    [Theory]
    [InlineData("Summarize my active screen time today.")]
    [InlineData("What was my longest single session?")]
    [InlineData("How many application switches did I make?")]
    [InlineData("What is my peak activity rhythm?")]
    [InlineData("Compare today's metrics with yesterday.")]
    public void PolicyGuard_AllowsObjectiveQueries(string query)
    {
        bool requiresRefusal = _guard.RequiresClinicalRefusal(query);
        Assert.False(requiresRefusal);
    }

    [Fact]
    public void NonClinicalRefusal_ContainsObjectiveMetrics_AndNoDiagnosis()
    {
        var context = new MirrorCopilotContext
        {
            DateOrRangeLabel = "Today",
            ActiveHours = 5.2,
            SwitchCount = 38,
            LongestSessionAppName = "Visual Studio",
            LongestSessionMinutes = 90,
            PeakRhythmPeriod = "Afternoon",
            BaselineComparison = "Within normal 14-day baseline range"
        };

        string refusal = _guard.GenerateNonClinicalRefusal(context);

        Assert.Contains("does not provide clinical, psychological, or medical evaluations", refusal);
        Assert.Contains("5.2 hours", refusal);
        Assert.Contains("38", refusal);
        Assert.Contains("Visual Studio", refusal);
        Assert.DoesNotContain("you have", refusal);
        Assert.DoesNotContain("your condition", refusal);
    }

    [Theory]
    [InlineData("You had a lazy and unproductive day.")]
    [InlineData("You are wasting time on social apps.")]
    [InlineData("This procrastinating habit must stop.")]
    public void PolicyGuard_DetectsJudgmentalLanguage(string text)
    {
        Assert.True(_guard.ContainsJudgmentalLanguage(text));
    }
}
