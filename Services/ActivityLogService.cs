using System.Text;

namespace AuxCodex.Services;

/// <summary>
/// Registra atividades da execução atual em um arquivo localizado junto ao executável.
/// Falhas do próprio log são absorvidas para não interromper a aplicação.
/// </summary>
public sealed class ActivityLogService
{
    private static readonly Encoding Utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    private readonly object _sync = new();
    private bool _disabled;

    public ActivityLogService(string? baseDirectory = null)
    {
        var directory = string.IsNullOrWhiteSpace(baseDirectory) ? AppContext.BaseDirectory : baseDirectory.Trim();
        LogFilePath = Path.Combine(directory, "AuxCodex.log");
    }

    public string LogFilePath { get; }

    public void StartNewExecution(string? version, string? configurationPath)
    {
        lock (_sync)
        {
            if (_disabled) return;
            try
            {
                var line = Format("INFO", "Aplicação iniciada.");
                File.WriteAllText(LogFilePath, line + Environment.NewLine, Utf8);
                WriteUnsafe("INFO", $"Versão: {Safe(version, "desconhecida")}.");
                WriteUnsafe("INFO", $"Diretório base: {AppContext.BaseDirectory}.");
                WriteUnsafe("INFO", $"Configuração: {Safe(configurationPath, "não informado")}.");
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException or PathTooLongException or System.Security.SecurityException)
            {
                _disabled = true;
            }
        }
    }

    public void Info(string message) => Write("INFO", message);
    public void Warning(string message) => Write("WARNING", message);
    public void Error(string message) => Write("ERROR", message);

    public void Error(string context, Exception exception) =>
        Write("ERROR", $"{context} Tipo: {exception.GetType().Name}.");

    private void Write(string level, string message)
    {
        lock (_sync)
        {
            if (_disabled) return;
            try { WriteUnsafe(level, message); }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException or PathTooLongException or System.Security.SecurityException)
            {
                _disabled = true;
            }
        }
    }

    private void WriteUnsafe(string level, string message) =>
        File.AppendAllText(LogFilePath, Format(level, message) + Environment.NewLine, Utf8);

    private static string Format(string level, string message) =>
        $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {message}";

    private static string Safe(string? value, string fallback) =>
        string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
}
