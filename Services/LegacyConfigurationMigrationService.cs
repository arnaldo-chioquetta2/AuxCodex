using System.Text.Json;
using AuxCodex.Configuration;
using AuxCodex.Models;

namespace AuxCodex.Services;

/// <summary>
/// Recupera, de forma conservadora, configurações anteriores para a pasta da
/// instância em execução. Nunca é acionado para um --config-dir explícito.
/// </summary>
public sealed class LegacyConfigurationMigrationService
{
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public void MigrateIfNeeded(ConfigurationPathProvider destination, bool explicitConfigurationDirectory, ActivityLogService activityLog)
    {
        if (explicitConfigurationDirectory) return;

        var destinationPath = destination.ConfigurationFilePath;
        var destinationExists = File.Exists(destinationPath);
        var destinationHasRealData = false;
        if (destinationExists)
        {
            if (!TryReadConfiguration(destinationPath, out var current, out _))
            {
                activityLog.Warning("Configuração atual não pôde ser avaliada; migração legada ignorada.");
                return;
            }

            destinationHasRealData = HasRealUserData(current!);
        }

        var candidates = DiscoverCandidates(destinationPath)
            .Select(path => TryReadConfiguration(path, out var configuration, out var error)
                ? new Candidate(path, configuration!, HasRealUserData(configuration!))
                : null)
            .Where(candidate => candidate is not null)
            .Cast<Candidate>()
            .Where(candidate => candidate.HasRealData)
            .ToList();

        if (candidates.Count == 0) return;
        if (destinationHasRealData)
        {
            activityLog.Warning("Configuração atual já contém dados; migração automática legada ignorada.");
            return;
        }

        if (candidates.Count != 1)
        {
            activityLog.Warning("Mais de uma configuração legada válida foi encontrada; migração automática ignorada por ambiguidade.");
            return;
        }

        var source = candidates[0];
        activityLog.Info($"Configuração legada válida detectada: {source.Path}.");
        if (!CopyAtomically(source.Path, destinationPath, destinationExists, activityLog)) return;
        activityLog.Info("Migração legada concluída.");
    }

    private IEnumerable<string> DiscoverCandidates(string destinationPath)
    {
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AuxCodex", ConfigurationPathProvider.ConfigurationFileName),
            Path.Combine(Environment.CurrentDirectory, ConfigurationPathProvider.ConfigurationFileName)
        };

        return paths.Where(path => !string.Equals(path, destinationPath, StringComparison.OrdinalIgnoreCase) && File.Exists(path));
    }

    private bool TryReadConfiguration(string path, out AppConfiguration? configuration, out string? error)
    {
        configuration = null;
        error = null;
        try
        {
            var json = File.ReadAllText(path);
            if (string.IsNullOrWhiteSpace(json)) { error = "vazio"; return false; }
            configuration = JsonSerializer.Deserialize<AppConfiguration>(json, _jsonOptions);
            if (configuration is null) { error = "nulo"; return false; }
            var validationErrors = ConfigurationValidator.Validate(configuration);
            if (validationErrors.Count > 0) { error = "estrutura inválida"; return false; }
            return true;
        }
        catch (Exception exception) when (exception is JsonException or IOException or UnauthorizedAccessException or NotSupportedException)
        {
            error = exception.GetType().Name;
            return false;
        }
    }

    private static bool HasRealUserData(AppConfiguration configuration)
    {
        if (HasItems(configuration.Items) || HasFolders(configuration.Folders)) return true;
        if (!BatTemplateDefaults.IsFallbackOpenAiTemplate(configuration.OpenAiBatTemplate)) return true;
        if (!string.IsNullOrWhiteSpace(configuration.LastCreatedOpenAiBatContent) && !BatTemplateDefaults.IsFallbackOpenAiTemplate(configuration.LastCreatedOpenAiBatContent)) return true;
        if (!string.IsNullOrWhiteSpace(configuration.LastCreatedDeepSeekBatContent) && !string.Equals(configuration.LastCreatedDeepSeekBatContent.Trim(), SecondaryProviderCatalogDefaults.DeepSeekTemplate.Trim(), StringComparison.OrdinalIgnoreCase)) return true;
        return configuration.SecondaryProviders?.Any(IsCustomizedProvider) ?? false;
    }

    private static bool IsCustomizedProvider(SecondaryProviderDefinition definition)
    {
        if (!definition.IsBuiltIn) return true;
        var defaultTemplate = definition.Id == SecondaryProviderCatalogDefaults.DeepSeekId
            ? SecondaryProviderCatalogDefaults.DeepSeekTemplate
            : definition.Id == SecondaryProviderCatalogDefaults.GlmId
                ? SecondaryProviderCatalogDefaults.GlmTemplate
                : null;
        return defaultTemplate is null || !string.Equals(definition.BatTemplate?.Trim(), defaultTemplate.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasFolders(IEnumerable<MenuFolder>? folders) =>
        (folders ?? Enumerable.Empty<MenuFolder>()).Any(folder => folder is not null && (folder.Items?.Count > 0 || HasFolders(folder.Folders)));

    private static bool HasItems(IEnumerable<MenuItem>? items) =>
        (items ?? Enumerable.Empty<MenuItem>()).Any(item => item is not null);

    private static bool CopyAtomically(string sourcePath, string destinationPath, bool destinationExists, ActivityLogService activityLog)
    {
        string? temporaryPath = null;
        try
        {
            var destinationDirectory = Path.GetDirectoryName(destinationPath);
            if (string.IsNullOrWhiteSpace(destinationDirectory)) return false;
            Directory.CreateDirectory(destinationDirectory);

            if (destinationExists)
            {
                var backupPath = CreateBackupPath(destinationPath);
                Directory.CreateDirectory(Path.GetDirectoryName(backupPath)!);
                File.Copy(destinationPath, backupPath, overwrite: false);
                activityLog.Info("Backup da configuração atual criado.");
            }

            temporaryPath = Path.Combine(destinationDirectory, $"config.migration-{Guid.NewGuid():N}.tmp");
            File.Copy(sourcePath, temporaryPath, overwrite: false);
            if (!File.Exists(temporaryPath)) return false;

            if (destinationExists) File.Replace(temporaryPath, destinationPath, null);
            else File.Move(temporaryPath, destinationPath);
            temporaryPath = null;
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException or PathTooLongException or System.Security.SecurityException)
        {
            activityLog.Error("Falha na migração legada; configuração atual preservada.", exception);
            return false;
        }
        finally
        {
            if (temporaryPath is not null)
            {
                try { if (File.Exists(temporaryPath)) File.Delete(temporaryPath); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }
    }

    private static string CreateBackupPath(string destinationPath)
    {
        var destinationDirectory = Path.GetDirectoryName(destinationPath)!;
        var backupDirectory = Path.Combine(destinationDirectory, "backups");
        var basePath = Path.Combine(backupDirectory, $"config-pre-migration-{DateTime.Now:yyyyMMdd_HHmmss}.json");
        var path = basePath;
        var suffix = 1;
        while (File.Exists(path)) path = $"{basePath}.{suffix++}";
        return path;
    }

    private sealed record Candidate(string Path, AppConfiguration Configuration, bool HasRealData);
}
