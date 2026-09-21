namespace AuxCodex.Models;

public sealed class MenuFolder
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    public string ProjectDirectory { get; set; } = string.Empty;

    public List<MenuFolder> Folders { get; set; } = new();

    public List<MenuItem> Items { get; set; } = new();

    public IEnumerable<MenuFolder> GetFoldersOrdered() =>
        (Folders ?? new List<MenuFolder>())
            .OrderBy(folder => folder.Name ?? string.Empty, StringComparer.CurrentCultureIgnoreCase);

    public IEnumerable<MenuItem> GetItemsOrdered() =>
        (Items ?? new List<MenuItem>())
            .OrderBy(item => item.Name ?? string.Empty, StringComparer.CurrentCultureIgnoreCase);
}
