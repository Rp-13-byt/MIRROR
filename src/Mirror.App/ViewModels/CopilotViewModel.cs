using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using Mirror.LLM.Copilot;

namespace Mirror_App.ViewModels;

public sealed partial class CopilotMessageItem : ObservableObject
{
    public bool IsUser { get; init; }
    public string Text { get; init; } = string.Empty;
    public string FormattedTime { get; init; } = DateTime.Now.ToString("HH:mm");
    public string BackendDisplayName { get; init; } = string.Empty;
    public double LatencyMs { get; init; }
    public bool WasRefusedOrRedirected { get; init; }
    public MirrorCopilotContext? Context { get; init; }
    public IReadOnlyList<CopilotAction> SuggestedActions { get; init; } = Array.Empty<CopilotAction>();

    public Visibility UserVisibility => IsUser ? Visibility.Visible : Visibility.Collapsed;
    public Visibility CopilotVisibility => !IsUser ? Visibility.Visible : Visibility.Collapsed;
    public Visibility RefusalVisibility => WasRefusedOrRedirected ? Visibility.Visible : Visibility.Collapsed;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EvidenceVisibility))]
    private bool _isEvidenceVisible = false;

    public Visibility EvidenceVisibility => IsEvidenceVisible ? Visibility.Visible : Visibility.Collapsed;

    public string GroundedFactsSummary
    {
        get
        {
            if (Context == null) return "No raw facts attached.";
            return $"• Active Time: {Context.ActiveHours:F1}h\n" +
                   $"• App Switches: {Context.SwitchCount}\n" +
                   $"• Longest Focus Session: {Context.LongestSessionAppName} ({Context.LongestSessionMinutes:F0}m)\n" +
                   $"• Peak Digital Rhythm: {Context.PeakRhythmPeriod}\n" +
                   $"• Baseline Evaluation: {Context.BaselineComparison}";
        }
    }

    [RelayCommand]
    public void ToggleEvidence()
    {
        IsEvidenceVisible = !IsEvidenceVisible;
    }
}

public sealed partial class CopilotViewModel : ObservableObject
{
    private readonly IMirrorCopilotService _copilotService;

    [ObservableProperty]
    private string _inputQuestion = string.Empty;

    [ObservableProperty]
    private bool _isLoading = false;

    [ObservableProperty]
    private string _activeBackend = "Qualcomm Snapdragon NPU (QNN EP)";

    [ObservableProperty]
    private bool _isHistorySaved = false; // Privacy-by-default

    [ObservableProperty]
    private string _privacyNotice = "100% On-Device • Zero Network Requests • Chat Not Saved To Disk";

    public ObservableCollection<CopilotMessageItem> Messages { get; } = new();

    public CopilotViewModel(IMirrorCopilotService copilotService)
    {
        _copilotService = copilotService;
        AddGreeting();
    }

    private void AddGreeting()
    {
        Messages.Add(new CopilotMessageItem
        {
            IsUser = false,
            Text = "Hello! I am Mirror Copilot, your on-device digital wellbeing companion running locally on your PC. I can summarize your activity, explain detected patterns, analyze focus rhythms, and verify data privacy guarantees.\n\nWhat would you like to explore today?",
            BackendDisplayName = "Qualcomm AI Hub Runtime (Local)",
            LatencyMs = 0.5
        });
    }

    [RelayCommand]
    public async Task SendAsync()
    {
        if (string.IsNullOrWhiteSpace(InputQuestion) || IsLoading)
            return;

        string query = InputQuestion.Trim();
        InputQuestion = string.Empty;

        Messages.Add(new CopilotMessageItem
        {
            IsUser = true,
            Text = query
        });

        IsLoading = true;

        try
        {
            var answer = await _copilotService.AskAsync(query);

            ActiveBackend = answer.BackendDisplayName;

            Messages.Add(new CopilotMessageItem
            {
                IsUser = false,
                Text = answer.AnswerText,
                BackendDisplayName = answer.BackendDisplayName,
                LatencyMs = answer.LatencyMs,
                WasRefusedOrRedirected = answer.WasRefusedOrRedirected,
                Context = answer.Context,
                SuggestedActions = answer.SuggestedActions
            });
        }
        catch (Exception ex)
        {
            Messages.Add(new CopilotMessageItem
            {
                IsUser = false,
                Text = $"Inference error: {ex.Message}",
                BackendDisplayName = "Local Runtime Error",
                LatencyMs = 0
            });
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task QuickAskAsync(string prompt)
    {
        InputQuestion = prompt;
        await SendAsync();
    }

    [RelayCommand]
    public void ClearChat()
    {
        Messages.Clear();
        AddGreeting();
    }
}
