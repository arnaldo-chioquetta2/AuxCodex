using AuxCodex.Models;

namespace AuxCodex.Services;

public static class BatTemplateDefaults
{
    // Fallback mínimo, sem modelo, provedor específico ou chave de sessão.
    public const string OpenAi = "codex resume";
    private const string PreviousOpenAiFallback = "@echo off\r\ncd /d \"{{PROJECT_DIRECTORY}}\"\r\ncodex resume";

    public static string ResolveOpenAi(AppConfiguration configuration, out bool fallbackUsed)
    {
        if (HasUsableExplicitTemplate(configuration.OpenAiBatTemplate))
        {
            fallbackUsed = false;
            return configuration.OpenAiBatTemplate;
        }

        if (HasUsableExplicitTemplate(configuration.LastCreatedOpenAiBatContent))
        {
            fallbackUsed = false;
            return configuration.LastCreatedOpenAiBatContent;
        }

        fallbackUsed = true;
        return OpenAi;
    }

    public static bool IsFallbackOpenAiTemplate(string? value) =>
        string.IsNullOrWhiteSpace(value) ||
        string.Equals(value.Trim(), OpenAi, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(value.Trim(), PreviousOpenAiFallback, StringComparison.OrdinalIgnoreCase);

    private static bool HasUsableExplicitTemplate(string? value) =>
        !string.IsNullOrWhiteSpace(value) &&
        !string.Equals(value.Trim(), PreviousOpenAiFallback, StringComparison.OrdinalIgnoreCase);
}
