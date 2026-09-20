using System.Text.Json;
using AuxCodex.Configuration;
using AuxCodex.Models;

namespace AuxCodex.Services;

public sealed class ConfigurationService
{
    private const string MigratedSessionName = "Principal";
    private readonly ConfigurationPathProvider _pathProvider;
    private readonly SessionResumeSyncService _sessionResumeSyncService;
    private readonly BatResumeKeyService _resumeKeyService = new();
    private readonly IFileSystemService _fileSystem;
    private readonly ActivityLogService? _activityLog;
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true, PropertyNameCaseInsensitive = true };

    public ConfigurationService(ConfigurationPathProvider? pathProvider = null, SessionResumeSyncService? sessionResumeSyncService = null, IFileSystemService? fileSystem = null, ActivityLogService? activityLogService = null)
    {
        _pathProvider = pathProvider ?? new ConfigurationPathProvider();
        _sessionResumeSyncService = sessionResumeSyncService ?? new SessionResumeSyncService();
        _fileSystem = fileSystem ?? new PhysicalFileSystemService();
        _activityLog = activityLogService;
    }

    public string ConfigurationFilePath => _pathProvider.ConfigurationFilePath;
    public IFileSystemService FileSystem => _fileSystem;
    public ActivityLogService? ActivityLog => _activityLog;

    public AppConfiguration Load()
    {
        _fileSystem.CreateDirectory(_pathProvider.BaseDirectory);
        if (!_fileSystem.FileExists(ConfigurationFilePath))
        {
            var initial = new AppConfiguration();
            Normalize(initial, new List<ProjectSession>());
            Save(initial);
            _activityLog?.Info("Configuração inexistente; configuração inicial criada.");
            return initial;
        }

        try
        {
            var json = _fileSystem.ReadAllText(ConfigurationFilePath, System.Text.Encoding.UTF8);
            if (string.IsNullOrWhiteSpace(json)) throw new JsonException("Configuration is empty.");
            var configuration = JsonSerializer.Deserialize<AppConfiguration>(json, _jsonOptions) ?? throw new JsonException("Configuration is null.");
            var migratedSessions = new List<ProjectSession>();
            var changed = Normalize(configuration, migratedSessions) || !IsCurrentSchema(json);
            var errors = ConfigurationValidator.Validate(configuration);
            if (errors.Count > 0) throw new JsonException(string.Join(" ", errors));
            if (changed) Save(configuration);
            foreach (var session in migratedSessions)
            {
                try { _sessionResumeSyncService.SyncSession(session); }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException or PathTooLongException or System.Security.SecurityException) { }
            }
            _activityLog?.Info("Configuração lida e validada.");
            return configuration;
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            _activityLog?.Error("Falha ao carregar configuração; recuperação aplicada.", ex);
            PreserveInvalidConfiguration();
            var fallback = new AppConfiguration();
            Normalize(fallback, new List<ProjectSession>());
            Save(fallback);
            return fallback;
        }
    }

    public void Save(AppConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        _fileSystem.CreateDirectory(_pathProvider.BaseDirectory);
        var temporaryPath = $"{ConfigurationFilePath}.tmp-{Guid.NewGuid():N}";
        try
        {
            _fileSystem.WriteAllText(temporaryPath, JsonSerializer.Serialize(configuration, _jsonOptions), System.Text.Encoding.UTF8);
            if (_fileSystem.FileExists(ConfigurationFilePath)) _fileSystem.Replace(temporaryPath, ConfigurationFilePath);
            else _fileSystem.Move(temporaryPath, ConfigurationFilePath);
            _activityLog?.Info("Configuração salva.");
        }
        catch (Exception exception)
        {
            _activityLog?.Error("Falha ao salvar configuração.", exception);
            throw;
        }
        finally { if (_fileSystem.FileExists(temporaryPath)) _fileSystem.Delete(temporaryPath); }
    }

    private bool Normalize(AppConfiguration config, List<ProjectSession> migratedSessions)
    {
        var changed = false;
        if (config.LastCreatedOpenAiBatContent is null) { config.LastCreatedOpenAiBatContent = string.Empty; changed = true; }
        if (config.LastCreatedDeepSeekBatContent is null) { config.LastCreatedDeepSeekBatContent = string.Empty; changed = true; }
        if (config.LegacyLastCreatedBatContent is not null)
        {
            if (string.IsNullOrEmpty(config.LastCreatedOpenAiBatContent)) config.LastCreatedOpenAiBatContent = config.LegacyLastCreatedBatContent;
            config.LegacyLastCreatedBatContent = null; changed = true;
        }
        if (config.OpenAiBatTemplate is null) { config.OpenAiBatTemplate = string.Empty; changed = true; }
        if (string.IsNullOrWhiteSpace(config.OpenAiBatTemplate))
        {
            var template = string.IsNullOrWhiteSpace(config.LastCreatedOpenAiBatContent) ? BatTemplateDefaults.OpenAi : config.LastCreatedOpenAiBatContent;
            var normalizedTemplate = _resumeKeyService.RemoveResumeKey(template);
            if (config.OpenAiBatTemplate != normalizedTemplate) { config.OpenAiBatTemplate = normalizedTemplate; changed = true; }
        }
        if (config.Folders is null) { config.Folders = new(); changed = true; }
        if (config.Items is null) { config.Items = new(); changed = true; }
        if (config.SecondaryProviders is null) { config.SecondaryProviders = new(); changed = true; }

        var deepSeek = FindOrCreateBuiltIn(config, SecondaryProviderCatalogDefaults.DeepSeekId, SecondaryProviderCatalogDefaults.DeepSeekName,
            _resumeKeyService.RemoveResumeKey(string.IsNullOrWhiteSpace(config.LastCreatedDeepSeekBatContent) ? SecondaryProviderCatalogDefaults.DeepSeekTemplate : config.LastCreatedDeepSeekBatContent), ref changed);
        FindOrCreateBuiltIn(config, SecondaryProviderCatalogDefaults.GlmId, SecondaryProviderCatalogDefaults.GlmName,
            SecondaryProviderCatalogDefaults.GlmTemplate, ref changed);
        foreach (var definition in config.SecondaryProviders)
        {
            if (definition.Id == Guid.Empty) { definition.Id = Guid.NewGuid(); changed = true; }
            var name = definition.Name?.Trim() ?? string.Empty;
            if (name != definition.Name) { definition.Name = name; changed = true; }
            var normalizedTemplate = _resumeKeyService.RemoveResumeKey(definition.BatTemplate ?? string.Empty);
            if (normalizedTemplate != definition.BatTemplate) { definition.BatTemplate = normalizedTemplate; changed = true; }
        }

        foreach (var folder in config.Folders) changed |= NormalizeFolder(folder, config, deepSeek, migratedSessions);
        foreach (var item in config.Items) changed |= NormalizeItem(item, config, deepSeek, migratedSessions);
        return changed;
    }

    private static SecondaryProviderDefinition FindOrCreateBuiltIn(AppConfiguration config, Guid defaultId, string name, string template, ref bool changed)
    {
        var definition = config.SecondaryProviders.FirstOrDefault(item => item.Id == defaultId)
            ?? config.SecondaryProviders.FirstOrDefault(item => string.Equals(item.Name?.Trim(), name, StringComparison.OrdinalIgnoreCase));
        if (definition is not null)
        {
            if (!definition.IsBuiltIn) { definition.IsBuiltIn = true; changed = true; }
            return definition;
        }
        definition = new SecondaryProviderDefinition { Id = defaultId, Name = name, BatTemplate = template, IsBuiltIn = true };
        config.SecondaryProviders.Add(definition);
        changed = true;
        return definition;
    }

    private bool NormalizeFolder(MenuFolder? folder, AppConfiguration config, SecondaryProviderDefinition deepSeek, List<ProjectSession> migrated)
    {
        if (folder is null) return false;
        var changed = false;
        if (folder.Id == Guid.Empty) { folder.Id = Guid.NewGuid(); changed = true; }
        var name = folder.Name?.Trim() ?? string.Empty;
        if (name != folder.Name) { folder.Name = name; changed = true; }
        if (folder.Folders is null) { folder.Folders = new(); changed = true; }
        if (folder.Items is null) { folder.Items = new(); changed = true; }
        foreach (var child in folder.Folders) changed |= NormalizeFolder(child, config, deepSeek, migrated);
        foreach (var item in folder.Items) changed |= NormalizeItem(item, config, deepSeek, migrated);
        return changed;
    }

    private bool NormalizeItem(MenuItem? item, AppConfiguration config, SecondaryProviderDefinition deepSeek, List<ProjectSession> migrated)
    {
        if (item is null) return false;
        var changed = false;
        if (item.Id == Guid.Empty) { item.Id = Guid.NewGuid(); changed = true; }
        var name = item.Name?.Trim() ?? string.Empty;
        if (name != item.Name) { item.Name = name; changed = true; }
        if (item.GptUrl is null) { item.GptUrl = string.Empty; changed = true; }
        if (item.Sessions is null) { item.Sessions = new(); changed = true; }

        if (item.Sessions.Count == 0 && (item.LegacyOpenAi is not null || item.LegacyDeepSeek is not null))
        {
            item.Sessions.Add(CreateMigratedSession(item.LegacyOpenAi, item.LegacyDeepSeek, deepSeek));
            migrated.Add(item.Sessions[0]); changed = true;
        }
        if (item.LegacyOpenAi is not null || item.LegacyDeepSeek is not null)
        {
            item.LegacyOpenAi = null; item.LegacyDeepSeek = null; changed = true;
        }
        foreach (var session in item.Sessions) changed |= NormalizeSession(session, config, deepSeek, migrated);
        return changed;
    }

    private static ProjectSession CreateMigratedSession(ProviderLaunchConfiguration? oldOpenAi, ProviderLaunchConfiguration? oldDeepSeek, SecondaryProviderDefinition deepSeek)
    {
        var session = new ProjectSession
        {
            Name = MigratedSessionName,
            OpenAiResumeKey = NormalizeKey(oldOpenAi?.LegacyResumeKey),
            OpenAi = oldOpenAi?.Clone() ?? new ProviderLaunchConfiguration()
        };
        if (oldDeepSeek is not null)
        {
            session.SecondaryProviders.Add(new SessionSecondaryProvider
            {
                ProviderDefinitionId = deepSeek.Id,
                ResumeKey = NormalizeKey(oldDeepSeek.LegacyResumeKey),
                LaunchConfiguration = oldDeepSeek.Clone()
            });
        }
        return session;
    }

    private bool NormalizeSession(ProjectSession? session, AppConfiguration config, SecondaryProviderDefinition deepSeek, List<ProjectSession> migrated)
    {
        if (session is null) return false;
        var changed = false;
        if (session.Id == Guid.Empty) { session.Id = Guid.NewGuid(); changed = true; }
        var name = session.Name?.Trim() ?? string.Empty;
        if (name != session.Name) { session.Name = name; changed = true; }
        if (session.OpenAi is null) { session.OpenAi = new(); changed = true; }
        if (session.SecondaryProviders is null) { session.SecondaryProviders = new(); changed = true; }

        var oldAiKey = NormalizeKey(session.OpenAi.LegacyResumeKey);
        var sharedKey = NormalizeKey(session.LegacySharedResumeKey);
        var aiKey = NormalizeKey(session.OpenAiResumeKey);
        if (oldAiKey.Length > 0) aiKey = oldAiKey;
        else if (aiKey.Length == 0 && sharedKey.Length > 0) aiKey = sharedKey;
        if (aiKey != session.OpenAiResumeKey) { session.OpenAiResumeKey = aiKey; changed = true; }
        changed |= NormalizeProvider(session.OpenAi);

        var oldDeep = session.LegacyDeepSeek;
        var oldDeepKey = NormalizeKey(session.LegacyDeepSeekResumeKey);
        if (oldDeep is not null)
        {
            if (oldDeepKey.Length == 0) oldDeepKey = NormalizeKey(oldDeep.LegacyResumeKey);
            if (oldDeepKey.Length == 0) oldDeepKey = sharedKey;
            if (!session.SecondaryProviders.Any(provider => provider.ProviderDefinitionId == deepSeek.Id))
            {
                session.SecondaryProviders.Add(new SessionSecondaryProvider { ProviderDefinitionId = deepSeek.Id, ResumeKey = oldDeepKey, LaunchConfiguration = oldDeep.Clone() });
                changed = true;
            }
            session.LegacyDeepSeek = null; changed = true;
        }
        else if (oldDeepKey.Length > 0 && !session.SecondaryProviders.Any(provider => provider.ProviderDefinitionId == deepSeek.Id))
        {
            session.SecondaryProviders.Add(new SessionSecondaryProvider { ProviderDefinitionId = deepSeek.Id, ResumeKey = oldDeepKey });
            changed = true;
        }

        if (sharedKey.Length > 0 && !session.SecondaryProviders.Any(provider => provider.ProviderDefinitionId == deepSeek.Id))
        {
            // MCO:10 shared key becomes independent initial keys for both providers.
            session.SecondaryProviders.Add(new SessionSecondaryProvider { ProviderDefinitionId = deepSeek.Id, ResumeKey = sharedKey });
            changed = true;
        }

        var seen = new HashSet<Guid>();
        foreach (var provider in session.SecondaryProviders.ToArray())
        {
            var definition = config.SecondaryProviders.FirstOrDefault(candidate => candidate.Id == provider.ProviderDefinitionId);
            if (definition is null || !seen.Add(provider.ProviderDefinitionId))
            {
                session.SecondaryProviders.Remove(provider); changed = true; continue;
            }
            var key = NormalizeKey(provider.ResumeKey);
            if (key != provider.ResumeKey) { provider.ResumeKey = key; changed = true; }
            if (provider.LaunchConfiguration is null) { provider.LaunchConfiguration = new(); changed = true; }
            changed |= NormalizeProvider(provider.LaunchConfiguration);
        }

        if (session.LegacySharedResumeKey is not null || session.LegacyDeepSeekResumeKey is not null)
        {
            session.LegacySharedResumeKey = null; session.LegacyDeepSeekResumeKey = null; changed = true;
        }
        if (session.OpenAi.LegacyResumeKey is not null) { session.OpenAi.LegacyResumeKey = null; changed = true; }
        if (changed) migrated.Add(session);
        return changed;
    }

    private static bool NormalizeProvider(ProviderLaunchConfiguration? provider)
    {
        if (provider is null) return false;
        var changed = false;
        if (provider.BatFilePath is null) { provider.BatFilePath = string.Empty; changed = true; }
        if (provider.LegacyResumeKey is not null) { provider.LegacyResumeKey = null; changed = true; }
        return changed;
    }

    private static string NormalizeKey(string? value) => value?.Trim().Replace("\r", string.Empty).Replace("\n", string.Empty) ?? string.Empty;

    private bool IsCurrentSchema(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.TryGetProperty("SecondaryProviders", out _) &&
            ItemsCurrent(doc.RootElement.TryGetProperty("Items", out var items) ? items : default) &&
            FoldersCurrent(doc.RootElement.TryGetProperty("Folders", out var folders) ? folders : default);
    }

    private bool ItemsCurrent(JsonElement items)
    {
        if (items.ValueKind != JsonValueKind.Array) return false;
        foreach (var item in items.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object || !item.TryGetProperty("Sessions", out var sessions) || sessions.ValueKind != JsonValueKind.Array) return false;
            foreach (var session in sessions.EnumerateArray())
                if (session.ValueKind != JsonValueKind.Object || !session.TryGetProperty("SecondaryProviders", out _) || session.TryGetProperty("DeepSeek", out _) || session.TryGetProperty("DeepSeekResumeKey", out _) || session.TryGetProperty("ResumeKey", out _)) return false;
        }
        return true;
    }

    private bool FoldersCurrent(JsonElement folders)
    {
        if (folders.ValueKind != JsonValueKind.Array) return false;
        foreach (var folder in folders.EnumerateArray())
            if (folder.ValueKind != JsonValueKind.Object || !ItemsCurrent(folder.TryGetProperty("Items", out var items) ? items : default) || !FoldersCurrent(folder.TryGetProperty("Folders", out var children) ? children : default)) return false;
        return true;
    }

    private void PreserveInvalidConfiguration()
    {
        if (!_fileSystem.FileExists(ConfigurationFilePath)) return;
        var backup = Path.Combine(_pathProvider.BaseDirectory, $"config.invalid.{DateTime.Now:yyyyMMdd_HHmmss}.json");
        try
        {
            if (_fileSystem.FileExists(backup)) backup = Path.Combine(_pathProvider.BaseDirectory, $"config.invalid.{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}.json");
            _fileSystem.Copy(ConfigurationFilePath, backup);
            _activityLog?.Warning("Configuração inválida preservada em backup.");
        }
        catch (IOException exception) { _activityLog?.Error("Falha ao preservar backup da configuração inválida.", exception); }
        catch (UnauthorizedAccessException exception) { _activityLog?.Error("Falha ao preservar backup da configuração inválida.", exception); }
    }
}
