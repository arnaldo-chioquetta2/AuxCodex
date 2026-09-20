using AuxCodex.Models;

namespace AuxCodex.Utils;

public static class BatPathConflictValidator
{
    public static bool TryNormalizePath(string? path, out string normalizedPath)
    {
        normalizedPath = string.Empty;
        if (string.IsNullOrWhiteSpace(path)) return false;
        var trimmed = path.Trim();
        try { normalizedPath = Path.GetFullPath(trimmed).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar); }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException or System.Security.SecurityException) { normalizedPath = trimmed; }
        if (normalizedPath.Length == 0) normalizedPath = trimmed;
        return true;
    }

    public static string? Validate(IEnumerable<SessionEditData>? sessions)
    {
        var usages = new List<(Guid Session, string SessionName, string Provider, string Path)>();
        foreach (var s in sessions ?? Enumerable.Empty<SessionEditData>())
        {
            Add(usages, s.Id, s.Name, "OpenAI", s.OpenAi?.BatFilePath);
            foreach (var secondary in s.SecondaryProviders ?? new()) Add(usages, s.Id, s.Name, secondary.ProviderName, secondary.LaunchConfiguration?.BatFilePath);
        }
        var conflict = usages.GroupBy(x => x.Path, StringComparer.OrdinalIgnoreCase).FirstOrDefault(g => g.Count() > 1);
        return conflict is null ? null : $"Mais de uma configuração usa o mesmo arquivo BAT: {string.Join(", ", conflict.Select(x => $"{x.Provider} ({x.SessionName})"))}. Escolha caminhos distintos.";
    }

    public static string? Validate(AppConfiguration configuration)
    {
        var all = new List<SessionEditData>();
        void AddProject(MenuItem item)
        {
            foreach (var session in item.Sessions ?? new())
                all.Add(new SessionEditData { Id = session.Id, Name = item.Name + " / " + session.Name, OpenAi = session.OpenAi,
                    SecondaryProviders = (session.SecondaryProviders ?? new()).Select(p => new SessionSecondaryProviderEditData
                    { ProviderDefinitionId = p.ProviderDefinitionId, ProviderName = configuration.SecondaryProviders.FirstOrDefault(d => d.Id == p.ProviderDefinitionId)?.Name ?? "Provedor", LaunchConfiguration = p.LaunchConfiguration }).ToList() });
        }
        void Walk(IEnumerable<MenuFolder>? folders, IEnumerable<MenuItem>? items)
        {
            foreach (var item in items ?? Enumerable.Empty<MenuItem>()) AddProject(item);
            foreach (var folder in folders ?? Enumerable.Empty<MenuFolder>()) Walk(folder.Folders, folder.Items);
        }
        Walk(configuration.Folders, configuration.Items);
        return Validate(all);
    }

    private static void Add(ICollection<(Guid Session, string SessionName, string Provider, string Path)> list, Guid id, string? sessionName, string provider, string? path)
    {
        if (TryNormalizePath(path, out var normalized)) list.Add((id, sessionName ?? "(sem nome)", provider, normalized));
    }
}