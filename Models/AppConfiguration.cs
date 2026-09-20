namespace AuxCodex.Models;

using System.Text.Json.Serialization;

public sealed class AppConfiguration
{
    public List<MenuFolder> Folders { get; set; } = new();

    public List<MenuItem> Items { get; set; } = new();

    public List<SecondaryProviderDefinition> SecondaryProviders { get; set; } = new();

    public string LastCreatedOpenAiBatContent { get; set; } = string.Empty;

    public string OpenAiBatTemplate { get; set; } = string.Empty;

    public string LastCreatedDeepSeekBatContent { get; set; } = string.Empty;

    [JsonPropertyName("LastCreatedBatContent")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? LegacyLastCreatedBatContent { get; set; }

    public IEnumerable<MenuFolder> GetFoldersOrdered() =>
        (Folders ?? new List<MenuFolder>())
            .OrderBy(folder => folder.Name ?? string.Empty, StringComparer.CurrentCultureIgnoreCase);

    public IEnumerable<MenuItem> GetItemsOrdered() =>
        (Items ?? new List<MenuItem>())
            .OrderBy(item => item.Name ?? string.Empty, StringComparer.CurrentCultureIgnoreCase);

    public MenuFolder? FindFolder(Guid id) => FindFolder(Folders, id);

    public MenuItem? FindItem(Guid id)
    {
        var item = (Items ?? new List<MenuItem>()).FirstOrDefault(candidate => candidate.Id == id);
        return item ?? FindItem(Folders, id);
    }

    public MenuFolder? FindFolderParent(Guid childId) =>
        FindFolderParent(Folders, childId);

    public MenuFolder? FindItemParent(Guid itemId)
    {
        if ((Items ?? new List<MenuItem>()).Any(item => item.Id == itemId))
        {
            return null;
        }

        return FindItemParent(Folders, itemId);
    }

    private static MenuFolder? FindFolder(IEnumerable<MenuFolder>? folders, Guid id)
    {
        foreach (var folder in folders ?? Enumerable.Empty<MenuFolder>())
        {
            if (folder.Id == id)
            {
                return folder;
            }

            var match = FindFolder(folder.Folders, id);
            if (match is not null)
            {
                return match;
            }
        }

        return null;
    }

    private static MenuItem? FindItem(IEnumerable<MenuFolder>? folders, Guid id)
    {
        foreach (var folder in folders ?? Enumerable.Empty<MenuFolder>())
        {
            var item = (folder.Items ?? new List<MenuItem>()).FirstOrDefault(candidate => candidate.Id == id);
            if (item is not null)
            {
                return item;
            }

            var match = FindItem(folder.Folders, id);
            if (match is not null)
            {
                return match;
            }
        }

        return null;
    }

    private static MenuFolder? FindFolderParent(IEnumerable<MenuFolder>? folders, Guid childId)
    {
        foreach (var folder in folders ?? Enumerable.Empty<MenuFolder>())
        {
            if ((folder.Folders ?? new List<MenuFolder>()).Any(child => child.Id == childId))
            {
                return folder;
            }

            var parent = FindFolderParent(folder.Folders, childId);
            if (parent is not null)
            {
                return parent;
            }
        }

        return null;
    }

    private static MenuFolder? FindItemParent(IEnumerable<MenuFolder>? folders, Guid itemId)
    {
        foreach (var folder in folders ?? Enumerable.Empty<MenuFolder>())
        {
            if ((folder.Items ?? new List<MenuItem>()).Any(item => item.Id == itemId))
            {
                return folder;
            }

            var parent = FindItemParent(folder.Folders, itemId);
            if (parent is not null)
            {
                return parent;
            }
        }

        return null;
    }
}
