namespace AuxCodex.Models;

using System.Text.Json.Serialization;

/// <summary>
/// Projeto do AuxCodex. Cada projeto pode conter varias sessoes, e cada sessao
/// possui configuracao propria de OpenAI e de provedores secundarios dinamicos.
/// </summary>
public sealed class MenuItem
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    public string GptUrl { get; set; } = string.Empty;

    public string ProjectDirectory { get; set; } = string.Empty;

    public List<ProjectSession> Sessions { get; set; } = new();

    public IEnumerable<ProjectSession> GetSessionsOrdered() =>
        (Sessions ?? new List<ProjectSession>())
            .OrderBy(session => session.Name ?? string.Empty, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(session => session.Name ?? string.Empty, StringComparer.CurrentCulture);

    public ProjectSession? FindSession(Guid sessionId) =>
        (Sessions ?? new List<ProjectSession>()).FirstOrDefault(session => session.Id == sessionId);

    public bool HasSessionNameConflict(string name, Guid? ignoredSessionId = null)
    {
        var normalized = name?.Trim() ?? string.Empty;
        return (Sessions ?? new List<ProjectSession>()).Any(session =>
            session.Id != ignoredSessionId &&
            string.Equals(session.Name?.Trim(), normalized, StringComparison.OrdinalIgnoreCase));
    }

    // Compatibilidade de leitura com o schema anterior (projeto com um unico provedor).

    [JsonPropertyName("OpenAi")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ProviderLaunchConfiguration? LegacyOpenAi { get; set; }

    [JsonPropertyName("DeepSeek")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ProviderLaunchConfiguration? LegacyDeepSeek { get; set; }
}
