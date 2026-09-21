using AuxCodex.Models;

namespace AuxCodex.Services;

public sealed class TrayMenuBuilder
{
    public IReadOnlyList<ToolStripItem> BuildRootItems(AppConfiguration configuration, MouseEventHandler? menuItemMouseUpHandler = null) =>
        BuildLevel(configuration, configuration.Folders, configuration.Items, menuItemMouseUpHandler);

    private static IReadOnlyList<ToolStripItem> BuildLevel(AppConfiguration config, IEnumerable<MenuFolder>? folders, IEnumerable<MenuItem>? projects, MouseEventHandler? rightClick)
    {
        var rows = new List<(string Name, MenuFolder? Folder, MenuItem? Project)>();
        rows.AddRange((folders ?? Enumerable.Empty<MenuFolder>()).Where(x => x is not null).Select(x => (x.Name ?? "", (MenuFolder?)x, (MenuItem?)null)));
        rows.AddRange((projects ?? Enumerable.Empty<MenuItem>()).Where(x => x is not null).Select(x => (x.Name ?? "", (MenuFolder?)null, (MenuItem?)x)));
        return rows.OrderBy(x => x.Name, StringComparer.CurrentCultureIgnoreCase).ThenBy(x => x.Name, StringComparer.CurrentCulture)
            .Select(x => (ToolStripItem)(x.Folder is not null ? CreateFolder(config, x.Folder, rightClick) : CreateProject(config, x.Project!, rightClick))).ToList();
    }

    private static ToolStripMenuItem CreateFolder(AppConfiguration config, MenuFolder folder, MouseEventHandler? rightClick)
    {
        var result = new ToolStripMenuItem(folder.Name) { Tag = new TrayMenuEntryTag(folder.Id, TrayMenuEntryType.Folder) };
        result.DropDownItems.AddRange(BuildLevel(config, folder.Folders, folder.Items, rightClick).ToArray());
        if (rightClick is not null) result.MouseUp += rightClick;
        return result;
    }

    private static ToolStripMenuItem CreateProject(AppConfiguration config, MenuItem project, MouseEventHandler? rightClick)
    {
        var result = new ToolStripMenuItem(project.Name) { Tag = new TrayMenuEntryTag(project.Id, TrayMenuEntryType.Item) };
        var sessions = project.GetSessionsOrdered().ToList();
        if (sessions.Count == 0)
        {
            result.DropDownItems.Add(new ToolStripMenuItem("(Nenhuma sessão configurada)") { Enabled = false });
        }
        else if (sessions.Count == 1)
        {
            var session = sessions[0];
            var providers = GetAvailableProviders(config, session);
            if (providers.Count == 1)
            {
                result.Tag = CreateExecutionTag(project, session, providers[0]);
                result.Enabled = providers[0].IsEnabled;
            }
            else if (providers.Count > 1)
            {
                foreach (var provider in providers) result.DropDownItems.Add(CreateProvider(project, session, provider, rightClick));
            }
            else
            {
                result.DropDownItems.Add(new ToolStripMenuItem("(Nenhum provedor configurado)") { Enabled = false });
            }
        }
        else
        {
            foreach (var session in sessions)
            {
                result.DropDownItems.Add(CreateSession(config, project, session, rightClick));
            }
        }
        if (rightClick is not null) result.MouseUp += rightClick;
        return result;
    }

    private static ToolStripMenuItem CreateSession(AppConfiguration config, MenuItem project, ProjectSession session, MouseEventHandler? rightClick)
    {
        var providers = GetAvailableProviders(config, session);
        var result = new ToolStripMenuItem(session.Name);
        if (providers.Count == 1)
        {
            result.Tag = CreateExecutionTag(project, session, providers[0], TrayMenuEntryType.Session);
            result.Enabled = providers[0].IsEnabled;
        }
        else
        {
            result.Tag = new TrayMenuEntryTag(project.Id, session.Id);
            foreach (var provider in providers) result.DropDownItems.Add(CreateProvider(project, session, provider, rightClick));
            if (providers.Count == 0) result.DropDownItems.Add(new ToolStripMenuItem("(Nenhum provedor configurado)") { Enabled = false });
        }
        if (rightClick is not null) result.MouseUp += rightClick;
        return result;
    }

    private static ToolStripMenuItem CreateProvider(MenuItem project, ProjectSession session, ProviderAction provider, MouseEventHandler? mouseUpHandler)
    {
        var entryType = provider.DefinitionId is null ? TrayMenuEntryType.OpenAiAction : TrayMenuEntryType.SecondaryProviderAction;
        var item = new ToolStripMenuItem(provider.Name) { Enabled = provider.IsEnabled, Tag = CreateExecutionTag(project, session, provider, entryType) };
        if (mouseUpHandler is not null) item.MouseUp += mouseUpHandler;
        return item;
    }

    private static TrayMenuEntryTag CreateExecutionTag(MenuItem project, ProjectSession session, ProviderAction provider, TrayMenuEntryType entryType = TrayMenuEntryType.Item) =>
        new(project.Id, session.Id, provider.DefinitionId, entryType);

    private static List<ProviderAction> GetAvailableProviders(AppConfiguration config, ProjectSession session)
    {
        var providers = new List<ProviderAction>();
        if (session.OpenAi is not null)
        {
            providers.Add(new ProviderAction("OpenAI", null, session.OpenAi, !string.IsNullOrWhiteSpace(session.OpenAi.BatFilePath)));
        }

        var secondary = (session.SecondaryProviders ?? new())
            .Select(usage => (Usage: usage, Definition: config.SecondaryProviders.FirstOrDefault(def => def.Id == usage.ProviderDefinitionId)))
            .Where(row => row.Definition is not null)
            .OrderBy(row => row.Definition!.Name, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(row => row.Definition!.Name, StringComparer.CurrentCulture);
        foreach (var row in secondary)
        {
            providers.Add(new ProviderAction(row.Definition!.Name, row.Definition.Id, row.Usage.LaunchConfiguration,
                !string.IsNullOrWhiteSpace(row.Usage.LaunchConfiguration?.BatFilePath)));
        }
        return providers;
    }

    private sealed record ProviderAction(string Name, Guid? DefinitionId, ProviderLaunchConfiguration? Launch, bool IsEnabled);
}
