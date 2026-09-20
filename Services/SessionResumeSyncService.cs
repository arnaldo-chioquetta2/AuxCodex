using AuxCodex.Models;

namespace AuxCodex.Services;

/// <summary>Synchronizes each provider key only with its own BAT.</summary>
public sealed class SessionResumeSyncService
{
    private readonly BatResumeKeyService _resumeKeyService;
    private readonly BatFileService _batFileService;

    public SessionResumeSyncService(BatResumeKeyService? resumeKeyService = null, BatFileService? batFileService = null)
    {
        _resumeKeyService = resumeKeyService ?? new BatResumeKeyService();
        _batFileService = batFileService ?? new BatFileService();
    }

    public bool SyncSession(ProjectSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        var updated = SyncProviderBat(session.OpenAi, session.OpenAiResumeKey);
        foreach (var provider in session.SecondaryProviders ?? new())
            updated |= SyncProviderBat(provider.LaunchConfiguration, provider.ResumeKey);
        return updated;
    }

    public bool SyncProviderBat(ProviderLaunchConfiguration? provider, string? resumeKey)
    {
        var path = provider?.BatFilePath;
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return false;
        var content = _batFileService.ReadContent(path);
        if (!_resumeKeyService.TryExtractResumeKey(content, out _)) return false;
        var updated = _resumeKeyService.ReplaceResumeKey(content, resumeKey);
        if (string.Equals(updated, content, StringComparison.Ordinal)) return false;
        _batFileService.WriteContent(path, updated);
        return true;
    }
}