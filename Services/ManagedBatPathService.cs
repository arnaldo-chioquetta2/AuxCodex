namespace AuxCodex.Services;

public sealed class ManagedBatPathService
{
    private readonly string _batDirectory;

    public ManagedBatPathService(string batDirectory) => _batDirectory = batDirectory;

    public string GetPath(string projectName, string sessionName, string providerName, bool isOpenAi, int sessionCount)
    {
        var project = Sanitize(projectName, "Projeto");
        var session = Sanitize(sessionName, "Sessao");
        var provider = Sanitize(providerName, "Provedor");
        var name = sessionCount == 1
            ? isOpenAi ? project : $"{project} - {provider}"
            : isOpenAi ? $"{project} - {session}" : $"{project} - {session} - {provider}";
        return Path.Combine(_batDirectory, $"{name}.bat");
    }

    private static string Sanitize(string? value, string fallback)
    {
        var invalid = Path.GetInvalidFileNameChars().Concat(new[] { '\\', '/', ':', '*', '?', '"', '<', '>', '|' }).ToHashSet();
        var text = new string((value ?? string.Empty).Trim().Select(character => invalid.Contains(character) ? '_' : character).ToArray());
        text = string.Join(" ", text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        return string.IsNullOrWhiteSpace(text) || text is "." or ".." ? fallback : text;
    }
}
