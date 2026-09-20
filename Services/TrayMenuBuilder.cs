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
        else if (CanCompactSingleOpenAiSession(sessions))
        {
            var session = sessions[0];
            result.DropDownItems.Add(CreateProvider(project, session, "OpenAI", null, session.OpenAi, rightClick));
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

    private static bool CanCompactSingleOpenAiSession(IReadOnlyList<ProjectSession> sessions) =>
        sessions.Count == 1 &&
        sessions[0].OpenAi is not null &&
        !(sessions[0].SecondaryProviders?.Any() ?? false);

    private static ToolStripMenuItem CreateSession(AppConfiguration config, MenuItem project, ProjectSession session, MouseEventHandler? rightClick)
    {
        var result = new ToolStripMenuItem(session.Name) { Tag = new TrayMenuEntryTag(project.Id, session.Id) };
        result.DropDownItems.Add(CreateProvider(project, session, "OpenAI", null, session.OpenAi, rightClick));
        var secondary = (session.SecondaryProviders ?? new()).Select(usage => (Usage: usage, Definition: config.SecondaryProviders.FirstOrDefault(def => def.Id == usage.ProviderDefinitionId)))
            .Where(row => row.Definition is not null).OrderBy(row => row.Definition!.Name, StringComparer.CurrentCultureIgnoreCase).ThenBy(row => row.Definition!.Name, StringComparer.CurrentCulture);
        foreach (var row in secondary) result.DropDownItems.Add(CreateProvider(project, session, row.Definition!.Name, row.Definition.Id, row.Usage.LaunchConfiguration, rightClick));
        return result;
    }

    private static ToolStripMenuItem CreateProvider(MenuItem project, ProjectSession session, string name, Guid? definitionId, ProviderLaunchConfiguration? launch, MouseEventHandler? mouseUpHandler)
    {
        var item = new ToolStripMenuItem(name) { Enabled = !string.IsNullOrWhiteSpace(launch?.BatFilePath), Tag = new TrayMenuEntryTag(project.Id, session.Id, definitionId) };
        if (mouseUpHandler is not null) item.MouseUp += mouseUpHandler;
        return item;
    }
}
