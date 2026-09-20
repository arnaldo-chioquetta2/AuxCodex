using System.ComponentModel;
using System.Diagnostics;

namespace AuxCodex.Services;

/// <summary>
/// Abre a URL no Mozilla Firefox quando disponivel, recorrendo ao navegador
/// padrao do Windows caso contrario.
/// </summary>
public sealed class BrowserLaunchService
{
    private readonly Func<string?> _firefoxLocator;

    public BrowserLaunchService()
        : this(FindFirefoxExecutable)
    {
    }

    public BrowserLaunchService(Func<string?> firefoxLocator)
    {
        _firefoxLocator = firefoxLocator ?? throw new ArgumentNullException(nameof(firefoxLocator));
    }

    public BrowserLaunchResult Open(Uri url)
    {
        ArgumentNullException.ThrowIfNull(url);

        try
        {
            var firefoxPath = _firefoxLocator();
            if (!string.IsNullOrWhiteSpace(firefoxPath))
            {
                Process.Start(CreateFirefoxStartInfo(firefoxPath, url));
                return BrowserLaunchResult.OpenedInFirefox();
            }

            Process.Start(CreateDefaultBrowserStartInfo(url));
            return BrowserLaunchResult.OpenedInDefaultBrowser();
        }
        catch (Exception exception) when (exception is Win32Exception or InvalidOperationException or System.Security.SecurityException)
        {
            return BrowserLaunchResult.Failed();
        }
    }

    internal static ProcessStartInfo CreateFirefoxStartInfo(string firefoxPath, Uri url)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = firefoxPath,
            UseShellExecute = false
        };
        startInfo.ArgumentList.Add(url.AbsoluteUri);
        return startInfo;
    }

    internal static ProcessStartInfo CreateDefaultBrowserStartInfo(Uri url) => new()
    {
        FileName = url.AbsoluteUri,
        UseShellExecute = true
    };

    /// <summary>
    /// Localiza o firefox.exe nos diretorios usuais de instalacao, sem fixar
    /// caminhos absolutos da maquina do usuario.
    /// </summary>
    public static string? FindFirefoxExecutable()
    {
        var candidates = new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
            Environment.GetEnvironmentVariable("ProgramFiles"),
            Environment.GetEnvironmentVariable("ProgramFiles(x86)")
        };

        foreach (var basePath in candidates
                     .Where(path => !string.IsNullOrWhiteSpace(path))
                     .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var candidate = Path.Combine(basePath!, "Mozilla Firefox", "firefox.exe");
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        var fromPath = FindOnPath("firefox.exe");
        return fromPath;
    }

    private static string? FindOnPath(string fileName)
    {
        var pathVariable = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrWhiteSpace(pathVariable))
        {
            return null;
        }

        foreach (var directory in pathVariable.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            try
            {
                var candidate = Path.Combine(directory.Trim(), fileName);
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }
            catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
            {
                // Diretorio invalido no PATH: apenas ignora.
            }
        }

        return null;
    }
}

public sealed record BrowserLaunchResult(bool Opened, bool UsedFirefox)
{
    public static BrowserLaunchResult OpenedInFirefox() => new(true, true);

    public static BrowserLaunchResult OpenedInDefaultBrowser() => new(true, false);

    public static BrowserLaunchResult Failed() => new(false, false);
}