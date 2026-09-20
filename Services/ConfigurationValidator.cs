using AuxCodex.Models;
using AuxCodex.Utils;

namespace AuxCodex.Services;

public static class ConfigurationValidator
{
    public static IReadOnlyList<string> Validate(AppConfiguration configuration)
    {
        var errors = new List<string>();
        var definitions = configuration.SecondaryProviders ?? new();
        var ids = new HashSet<Guid>(); var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var d in definitions)
        {
            if (d.Id == Guid.Empty || !ids.Add(d.Id)) errors.Add("O catálogo possui identificadores de provedor inválidos ou duplicados.");
            if (string.IsNullOrWhiteSpace(d.Name) || !names.Add(d.Name.Trim())) errors.Add("Os nomes dos provedores secundários devem ser únicos e não vazios.");
        }
        ValidateLevel(configuration.Folders, configuration.Items, "raiz", errors, ids);
        var conflict = BatPathConflictValidator.Validate(configuration);
        if (conflict is not null) errors.Add(conflict);
        return errors;
    }

    private static void ValidateLevel(IEnumerable<MenuFolder>? folders, IEnumerable<MenuItem>? items, string level, ICollection<string> errors, HashSet<Guid> definitions)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var folder in folders ?? Enumerable.Empty<MenuFolder>())
        {
            if (folder is null) { errors.Add($"Há pasta inválida no nível {level}."); continue; }
            if (string.IsNullOrWhiteSpace(folder.Name) || !names.Add(folder.Name.Trim())) errors.Add($"Há nome inválido ou duplicado no nível {level}.");
        }
        foreach (var project in items ?? Enumerable.Empty<MenuItem>())
        {
            if (project is null) continue;
            if (string.IsNullOrWhiteSpace(project.Name) || !names.Add(project.Name.Trim())) errors.Add($"Há nome inválido ou duplicado no nível {level}.");
            var sessionNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var session in project.Sessions ?? new())
            {
                if (session is null || string.IsNullOrWhiteSpace(session.Name) || !sessionNames.Add(session.Name.Trim())) errors.Add($"Há sessão inválida ou duplicada no projeto {project.Name}.");
                if (session is null) continue;
                var providerIds = new HashSet<Guid>();
                foreach (var provider in session.SecondaryProviders ?? new())
                    if (!definitions.Contains(provider.ProviderDefinitionId) || !providerIds.Add(provider.ProviderDefinitionId)) errors.Add($"Há referência de provedor secundário órfã ou duplicada na sessão {session.Name}.");
            }
        }
        foreach (var folder in folders ?? Enumerable.Empty<MenuFolder>()) if (folder is not null) ValidateLevel(folder.Folders, folder.Items, "pasta " + folder.Name, errors, definitions);
    }
}