using System.ComponentModel;
using System.Diagnostics;
using AuxCodex.Models;

namespace AuxCodex.Services;

/// <summary>
/// Coordena a execucao de um BAT de provedor e a abertura da URL do projeto.
/// </summary>
public sealed class ProviderLaunchService
{
    private readonly BrowserLaunchService _browserLaunchService;

    public ProviderLaunchService(BrowserLaunchService browserLaunchService)
    {
        _browserLaunchService = browserLaunchService ?? throw new ArgumentNullException(nameof(browserLaunchService));
    }

    public ProviderLaunchResult Launch(
        MenuItem project,
        ProviderLaunchConfiguration? providerConfiguration,
        string providerName,
        string? sessionName = null)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentException.ThrowIfNullOrWhiteSpace(providerName);

        var providerDescription = DescribeProvider(providerName, sessionName);
        var validationError = ValidateBat(providerConfiguration?.BatFilePath, providerDescription, out var batPath, out var workingDirectory);
        if (validationError is not null)
        {
            return ProviderLaunchResult.Failed(validationError);
        }

        var startResult = StartBat(batPath!, workingDirectory!, providerConfiguration!.RunAsAdministrator, providerDescription);
        if (!startResult.BatStarted)
        {
            return startResult;
        }

        if (string.IsNullOrWhiteSpace(project.GptUrl))
        {
            return ProviderLaunchResult.StartedWithoutUrl();
        }

        if (!Uri.TryCreate(project.GptUrl.Trim(), UriKind.Absolute, out var gptUri) ||
            (gptUri.Scheme != Uri.UriSchemeHttp && gptUri.Scheme != Uri.UriSchemeHttps))
        {
            return ProviderLaunchResult.StartedWithUrlFailure("O BAT foi iniciado, mas a URL do GPT é inválida.");
        }

        var browserResult = _browserLaunchService.Open(gptUri);
        return browserResult.Opened
            ? ProviderLaunchResult.StartedWithUrlOpened(browserResult.UsedFirefox)
            : ProviderLaunchResult.StartedWithUrlFailure("O BAT foi iniciado, mas não foi possível abrir a URL do GPT.");
    }

    private static string? ValidateBat(
        string? configuredPath,
        string providerDescription,
        out string? batPath,
        out string? workingDirectory)
    {
        batPath = null;
        workingDirectory = null;

        if (string.IsNullOrWhiteSpace(configuredPath))
        {
            return $"O BAT do {providerDescription} não está configurado.";
        }

        try
        {
            batPath = Path.GetFullPath(configuredPath.Trim());
            if (!string.Equals(Path.GetExtension(batPath), ".bat", StringComparison.OrdinalIgnoreCase))
            {
                return $"O arquivo configurado para {providerDescription} não possui extensão .bat.";
            }

            if (!File.Exists(batPath))
            {
                return $"O arquivo BAT configurado para {providerDescription} não foi encontrado.";
            }

            workingDirectory = Path.GetDirectoryName(batPath);
            if (string.IsNullOrWhiteSpace(workingDirectory) || !Directory.Exists(workingDirectory))
            {
                return $"O diretório do BAT configurado para {providerDescription} não é válido.";
            }

            return null;
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException or UnauthorizedAccessException)
        {
            return $"O caminho do BAT configurado para {providerDescription} é inválido.";
        }
    }

    private static ProviderLaunchResult StartBat(
        string batPath,
        string workingDirectory,
        bool runAsAdministrator,
        string providerDescription)
    {
        try
        {
            var startInfo = runAsAdministrator
                ? CreateElevatedStartInfo(batPath, workingDirectory)
                : CreateStandardStartInfo(batPath, workingDirectory);

            var process = Process.Start(startInfo);
            return process is null
                ? ProviderLaunchResult.Failed($"Não foi possível iniciar o {providerDescription}.")
                : ProviderLaunchResult.StartedWithoutUrl();
        }
        catch (Win32Exception exception) when (runAsAdministrator && exception.NativeErrorCode == 1223)
        {
            // Erro 1223: o usuário cancelou o prompt do UAC.
            return ProviderLaunchResult.UacCancelled();
        }
        catch (Exception exception) when (exception is Win32Exception or InvalidOperationException or System.Security.SecurityException)
        {
            return ProviderLaunchResult.Failed($"Não foi possível iniciar o {providerDescription}.");
        }
    }

    /// <summary>
    /// Descreve o provedor incluindo a sessao, para mensagens amigaveis.
    /// </summary>
    private static string DescribeProvider(string providerName, string? sessionName) =>
        string.IsNullOrWhiteSpace(sessionName)
            ? providerName
            : $"{providerName} da sessão \"{sessionName.Trim()}\"";

    /// <summary>
    /// O shell do Windows abre o arquivo .bat em uma janela de terminal visivel,
    /// tratando corretamente caminhos com espacos.
    /// </summary>
    internal static ProcessStartInfo CreateStandardStartInfo(string batPath, string workingDirectory) => new()
    {
        FileName = batPath,
        WorkingDirectory = workingDirectory,
        UseShellExecute = true,
        WindowStyle = ProcessWindowStyle.Normal
    };

    internal static ProcessStartInfo CreateElevatedStartInfo(string batPath, string workingDirectory) => new()
    {
        FileName = batPath,
        WorkingDirectory = workingDirectory,
        UseShellExecute = true,
        Verb = "runas",
        WindowStyle = ProcessWindowStyle.Normal
    };
}

public sealed record ProviderLaunchResult(
    bool BatStarted,
    bool UacWasCancelled,
    bool UrlWasAttempted,
    bool UrlOpened,
    bool UsedFirefox,
    string? ErrorMessage)
{
    public static ProviderLaunchResult Failed(string message) => new(false, false, false, false, false, message);

    public static ProviderLaunchResult UacCancelled() =>
        new(false, true, false, false, false, "A execução como administrador foi cancelada.");

    public static ProviderLaunchResult StartedWithoutUrl() => new(true, false, false, false, false, null);

    public static ProviderLaunchResult StartedWithUrlOpened(bool usedFirefox) =>
        new(true, false, true, true, usedFirefox, null);

    public static ProviderLaunchResult StartedWithUrlFailure(string message) =>
        new(true, false, true, false, false, message);
}

