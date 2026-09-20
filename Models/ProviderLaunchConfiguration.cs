namespace AuxCodex.Models;

using System.Text.Json.Serialization;

/// <summary>
/// Configuracao fisica de um provedor (BAT e elevacao).
/// A chave legada e migrada para a propriedade do respectivo provedor.
/// </summary>
public sealed class ProviderLaunchConfiguration
{
    public string BatFilePath { get; set; } = string.Empty;

    public bool RunAsAdministrator { get; set; }

    /// <summary>
    /// Compatibilidade de leitura com o schema anterior, no qual a chave de resume
    /// ficava no provedor. O valor e consumido pela migracao e nunca regravado.
    /// </summary>
    [JsonPropertyName("ResumeKey")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? LegacyResumeKey { get; set; }

    public ProviderLaunchConfiguration Clone() => new()
    {
        BatFilePath = BatFilePath,
        RunAsAdministrator = RunAsAdministrator
    };
}