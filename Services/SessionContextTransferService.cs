namespace AuxCodex.Services;

public enum ContextTransferStatus { MissingOpenAiKey, AlreadyCurrent, AppliedWithBatUpdate, AppliedWithoutBatUpdate }
public sealed record ContextTransferResult(ContextTransferStatus Status, string ResumeKey, string BatContent);

/// <summary>Prepares an explicit one-way OpenAI-to-secondary context transfer.</summary>
public sealed class SessionContextTransferService
{
    private readonly BatResumeKeyService _resumeKeyService;
    public SessionContextTransferService(BatResumeKeyService? resumeKeyService = null) => _resumeKeyService = resumeKeyService ?? new BatResumeKeyService();

    public ContextTransferResult Prepare(string? openAiResumeKey, string? currentResumeKey, string? batContent)
    {
        var source = Normalize(openAiResumeKey);
        var current = Normalize(currentResumeKey);
        var bat = batContent ?? string.Empty;
        if (source.Length == 0) return new(ContextTransferStatus.MissingOpenAiKey, current, bat);
        if (string.Equals(source, current, StringComparison.Ordinal)) return new(ContextTransferStatus.AlreadyCurrent, current, bat);
        if (!_resumeKeyService.TryExtractResumeKey(bat, out _)) return new(ContextTransferStatus.AppliedWithoutBatUpdate, source, bat);
        return new(ContextTransferStatus.AppliedWithBatUpdate, source, _resumeKeyService.ReplaceResumeKey(bat, source));
    }

    private static string Normalize(string? value)
    {
        var key = value?.Trim() ?? string.Empty;
        if (key.Length >= 2 && key[0] == key[^1] && (key[0] == '\'' || key[0] == '"')) key = key[1..^1].Trim();
        return key.Replace("\r", string.Empty).Replace("\n", string.Empty);
    }
}