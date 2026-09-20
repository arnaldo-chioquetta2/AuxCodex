namespace AuxCodex.Models;

using System.Text.Json.Serialization;

/// <summary>A work session with one primary OpenAI launch and zero or more secondary providers.</summary>
public sealed class ProjectSession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string OpenAiResumeKey { get; set; } = string.Empty;
    public ProviderLaunchConfiguration OpenAi { get; set; } = new();
    public List<SessionSecondaryProvider> SecondaryProviders { get; set; } = new();

    [JsonPropertyName("ResumeKey")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? LegacySharedResumeKey { get; set; }

    [JsonPropertyName("DeepSeekResumeKey")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? LegacyDeepSeekResumeKey { get; set; }

    [JsonPropertyName("DeepSeek")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ProviderLaunchConfiguration? LegacyDeepSeek { get; set; }
}