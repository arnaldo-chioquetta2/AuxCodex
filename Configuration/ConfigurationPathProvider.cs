namespace AuxCodex.Configuration;

/// <summary>
/// Provides the application configuration location.
/// </summary>
public sealed class ConfigurationPathProvider
{
    /// <summary>
    /// Retorna a raiz de dados da instância atualmente executada.
    /// </summary>
    public static string DefaultBaseDirectory => AppContext.BaseDirectory;
    public const string ConfigurationFileName = "config.json";

    public ConfigurationPathProvider(string? baseDirectory = null)
    {
        BaseDirectory = string.IsNullOrWhiteSpace(baseDirectory)
            ? DefaultBaseDirectory
            : baseDirectory.Trim();
    }

    public string BaseDirectory { get; }

    public string ConfigurationFilePath =>
        Path.Combine(BaseDirectory, ConfigurationFileName);
}
