using Mirror.Core.Models;

namespace Mirror.LLM;

public interface ILocalLlmService
{
    Task<string> ExplainPatternAsync(PatternEvent patternEvent, CancellationToken ct = default);
    bool IsModelLoaded { get; }
    string ModelName { get; }
}

