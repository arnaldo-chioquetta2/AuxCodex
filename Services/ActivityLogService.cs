using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using AuxCodex.Models;

namespace AuxCodex.Services;

/// <summary>
/// Registra atividades em um arquivo diário junto ao executável.
/// Falhas do próprio log são absorvidas para não interromper a aplicação.
/// </summary>
public sealed class ActivityLogService
{
    private static readonly Encoding Utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    private static readonly Regex ArchiveNamePattern = new(
        @"^AuxCodex-(?<date>\d{4}-\d{2}-\d{2})\.log$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
    private readonly object _sync = new();
    private readonly string _baseDirectory;
    private bool _disabled;

    public ActivityLogService(string? baseDirectory = null)
    {
        _baseDirectory = string.IsNullOrWhiteSpace(baseDirectory) ? AppContext.BaseDirectory : baseDirectory.Trim();
        LogFilePath = Path.Combine(_baseDirectory, "AuxCodex.log");
        ArchiveDirectoryPath = Path.Combine(_baseDirectory, "logs");
    }

    public string LogFilePath { get; }
    public string ArchiveDirectoryPath { get; }

    public void StartNewExecution(string? version, string? configurationPath, bool explicitConfigurationDirectory = false)
    {
        lock (_sync)
        {
            if (_disabled) return;
            try
            {
                var today = DateTime.Now.Date;
                var existingDate = ReadLogDate(LogFilePath);
                if (existingDate is not null && existingDate.Value != today)
                {
                    if (!RotateUnsafe(existingDate.Value)) return;
                    WriteUnsafe("INFO", "Novo dia de execução.");
                }
                else if (existingDate is null && File.Exists(LogFilePath))
                {
                    WriteUnsafe("WARNING", "Não foi possível identificar a data do log existente; conteúdo preservado.");
                    WriteSeparatorUnsafe("Nova execução");
                }
                else if (existingDate is null)
                {
                    EnsureBaseDirectoryUnsafe();
                }
                else
                {
                    WriteSeparatorUnsafe("Nova execução");
                }

                WriteUnsafe("INFO", "Aplicação iniciada.");
                WriteUnsafe("INFO", $"Versão: {Safe(version, "desconhecida")}.");
                WriteUnsafe("INFO", $"Diretório base: {AppContext.BaseDirectory}.");
                WriteUnsafe("INFO", $"Configuração: {Safe(configurationPath, "não informado")}. Origem: {(explicitConfigurationDirectory ? "--config-dir" : "pasta de execução")}.");
                CleanupArchivesUnsafe(today);
            }
            catch (Exception exception) when (IsLogFailure(exception))
            {
                _disabled = true;
            }
        }
    }

    public void Info(string message) => Write("INFO", message);
    public void Warning(string message) => Write("WARNING", message);
    public void Error(string message) => Write("ERROR", message);

    public void ConfigurationLoaded(AppConfiguration configuration) =>
        Info($"Configuração carregada: {CountFolders(configuration.Folders)} pastas, {CountItems(configuration)} projetos, {CountSessions(configuration)} sessões, {configuration.SecondaryProviders?.Count ?? 0} provedores adicionais.");

    public void ConfigurationSaved(AppConfiguration configuration) =>
        Info($"Configuração salva: {CountItems(configuration)} projetos, {CountSessions(configuration)} sessões.");

    public void TrayMenuRebuilt(AppConfiguration configuration) =>
        Info($"Menu da bandeja reconstruído: {CountItems(configuration)} projetos, {CountSessions(configuration)} sessões.");

    public void Error(string context, Exception exception) =>
        Write("ERROR", $"{context} Tipo: {exception.GetType().Name}.");

    private void Write(string level, string message)
    {
        lock (_sync)
        {
            if (_disabled) return;
            try
            {
                var today = DateTime.Now.Date;
                var existingDate = ReadLogDate(LogFilePath);
                if (existingDate is not null && existingDate.Value != today && !RotateUnsafe(existingDate.Value)) return;
                if (existingDate is null && !File.Exists(LogFilePath)) EnsureBaseDirectoryUnsafe();
                WriteUnsafe(level, message);
            }
            catch (Exception exception) when (IsLogFailure(exception))
            {
                _disabled = true;
            }
        }
    }

    private bool RotateUnsafe(DateTime previousDate)
    {
        try
        {
            EnsureBaseDirectoryUnsafe();
            Directory.CreateDirectory(ArchiveDirectoryPath);
            var archivePath = Path.Combine(ArchiveDirectoryPath, $"AuxCodex-{previousDate:yyyy-MM-dd}.log");
            File.Copy(LogFilePath, archivePath, overwrite: true);
            File.WriteAllText(LogFilePath, string.Empty, Utf8);
            return true;
        }
        catch (Exception exception) when (IsLogFailure(exception))
        {
            try { WriteUnsafe("WARNING", $"Falha ao arquivar o log de {previousDate:yyyy-MM-dd}; conteúdo anterior preservado."); }
            catch (Exception writeException) when (IsLogFailure(writeException)) { _disabled = true; }
            return false;
        }
    }

    private void CleanupArchivesUnsafe(DateTime today)
    {
        try
        {
            if (!Directory.Exists(ArchiveDirectoryPath)) return;
            var cutoff = today.AddDays(-30);
            foreach (var path in Directory.EnumerateFiles(ArchiveDirectoryPath, "AuxCodex-*.log", SearchOption.TopDirectoryOnly))
            {
                var match = ArchiveNamePattern.Match(Path.GetFileName(path));
                if (!match.Success || !DateTime.TryParseExact(match.Groups["date"].Value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) || date >= cutoff) continue;
                try { File.Delete(path); }
                catch (Exception exception) when (IsLogFailure(exception)) { WriteUnsafe("WARNING", $"Falha ao remover log arquivado de {date:yyyy-MM-dd}."); }
            }
        }
        catch (Exception exception) when (IsLogFailure(exception))
        {
            try { WriteUnsafe("WARNING", "Falha ao limpar logs arquivados antigos."); }
            catch (Exception writeException) when (IsLogFailure(writeException)) { _disabled = true; }
        }
    }

    private void EnsureBaseDirectoryUnsafe() => Directory.CreateDirectory(_baseDirectory);

    private void WriteSeparatorUnsafe(string title)
    {
        EnsureBaseDirectoryUnsafe();
        File.AppendAllText(LogFilePath, $"{new string('-', 60)}{Environment.NewLine}{title}{Environment.NewLine}", Utf8);
    }

    private void WriteUnsafe(string level, string message)
    {
        EnsureBaseDirectoryUnsafe();
        File.AppendAllText(LogFilePath, Format(level, message) + Environment.NewLine, Utf8);
    }

    private static DateTime? ReadLogDate(string path)
    {
        if (!File.Exists(path)) return null;
        try
        {
            foreach (var line in File.ReadLines(path, Utf8))
            {
                if (line.Length < 10) continue;
                if (DateTime.TryParseExact(line[..10], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)) return date.Date;
            }
            return File.GetLastWriteTime(path).Date;
        }
        catch (Exception exception) when (IsLogFailure(exception))
        {
            try { return File.GetLastWriteTime(path).Date; }
            catch (Exception fallbackException) when (IsLogFailure(fallbackException)) { return null; }
        }
    }

    private static string Format(string level, string message) =>
        $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {message}";

    private static string Safe(string? value, string fallback) =>
        string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();

    private static int CountFolders(IEnumerable<MenuFolder>? folders) =>
        (folders ?? Enumerable.Empty<MenuFolder>()).Where(folder => folder is not null).Sum(folder => 1 + CountFolders(folder.Folders));

    private static int CountItems(AppConfiguration configuration) =>
        CountItems(configuration.Items) + (configuration.Folders ?? new()).Sum(folder => CountItems(folder.Items) + CountItemsInFolders(folder.Folders));

    private static int CountItemsInFolders(IEnumerable<MenuFolder>? folders) =>
        (folders ?? Enumerable.Empty<MenuFolder>()).Where(folder => folder is not null).Sum(folder => CountItems(folder.Items) + CountItemsInFolders(folder.Folders));

    private static int CountItems(IEnumerable<MenuItem>? items) =>
        (items ?? Enumerable.Empty<MenuItem>()).Count(item => item is not null);

    private static int CountSessions(AppConfiguration configuration) =>
        CountSessions(configuration.Items) + (configuration.Folders ?? new()).Sum(folder => CountSessions(folder.Items) + CountSessionsInFolders(folder.Folders));

    private static int CountSessionsInFolders(IEnumerable<MenuFolder>? folders) =>
        (folders ?? Enumerable.Empty<MenuFolder>()).Where(folder => folder is not null).Sum(folder => CountSessions(folder.Items) + CountSessionsInFolders(folder.Folders));

    private static int CountSessions(IEnumerable<MenuItem>? items) =>
        (items ?? Enumerable.Empty<MenuItem>()).Where(item => item is not null).Sum(item => item.Sessions?.Count ?? 0);

    private static bool IsLogFailure(Exception exception) => exception is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException or PathTooLongException or System.Security.SecurityException;
}
