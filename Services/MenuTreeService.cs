using AuxCodex.Models;

namespace AuxCodex.Services;

public sealed class MenuTreeService
{
    public MenuFolder? FindFolder(AppConfiguration configuration, Guid folderId) =>
        configuration.FindFolder(folderId);

    public MenuFolder? FindParentFolder(AppConfiguration configuration, Guid folderId) =>
        configuration.FindFolderParent(folderId);

    public IList<MenuFolder> GetFolderSiblings(AppConfiguration configuration, Guid folderId)
    {
        var parent = FindParentFolder(configuration, folderId);
        return parent?.Folders ?? configuration.Folders;
    }

    public IList<MenuItem> GetSiblingItems(AppConfiguration configuration, Guid folderId)
    {
        var parent = FindParentFolder(configuration, folderId);
        return parent?.Items ?? configuration.Items;
    }

    public MenuItem? FindItem(AppConfiguration configuration, Guid itemId) =>
        configuration.FindItem(itemId);

    public MenuFolder? FindItemParent(AppConfiguration configuration, Guid itemId) =>
        configuration.FindItemParent(itemId);

    public IList<MenuItem> GetItemCollection(AppConfiguration configuration, Guid itemId)
    {
        var parent = FindItemParent(configuration, itemId);
        return parent?.Items ?? configuration.Items;
    }

    public IList<MenuFolder> GetFolderCollection(AppConfiguration configuration, Guid? folderId)
    {
        if (folderId is null || folderId == Guid.Empty)
        {
            return configuration.Folders;
        }

        var parent = FindFolder(configuration, folderId.Value);
        return parent?.Folders ?? configuration.Folders;
    }

    public IList<MenuItem> GetItemCollection(AppConfiguration configuration, Guid? folderId)
    {
        if (folderId is null || folderId == Guid.Empty)
        {
            return configuration.Items;
        }

        var parent = FindFolder(configuration, folderId.Value);
        return parent?.Items ?? configuration.Items;
    }

    public bool HasNameConflict(
        AppConfiguration configuration,
        MenuFolder? parentFolder,
        string name,
        Guid? ignoredEntryId)
    {
        var normalizedName = name.Trim();
        var folders = parentFolder?.Folders ?? configuration.Folders;
        var items = parentFolder?.Items ?? configuration.Items;

        return folders.Any(folder =>
                   folder.Id != ignoredEntryId &&
                   string.Equals(folder.Name?.Trim(), normalizedName, StringComparison.OrdinalIgnoreCase)) ||
               items.Any(item =>
                   item.Id != ignoredEntryId &&
                   string.Equals(item.Name?.Trim(), normalizedName, StringComparison.OrdinalIgnoreCase));
    }

    public bool HasNameConflict(
        AppConfiguration configuration,
        Guid? parentFolderId,
        string name,
        Guid? ignoredEntryId)
    {
        var parentFolder = parentFolderId is null || parentFolderId == Guid.Empty
            ? null
            : FindFolder(configuration, parentFolderId.Value);

        return HasNameConflict(configuration, parentFolder, name, ignoredEntryId);
    }

    /// <summary>
    /// Indica se <paramref name="candidateId"/> esta contido na arvore de subpastas de
    /// <paramref name="ancestorId"/>, em qualquer profundidade.
    /// </summary>
    public bool IsDescendantFolder(AppConfiguration configuration, Guid ancestorId, Guid candidateId)
    {
        var ancestor = FindFolder(configuration, ancestorId);
        if (ancestor is null)
        {
            return false;
        }

        return ContainsFolder(ancestor.Folders, candidateId);
    }

    /// <summary>
    /// Monta a arvore de destinos exibida na janela de movimentacao.
    /// Retorna apenas a raiz e pastas; itens nunca aparecem como destino.
    /// Quando <paramref name="excludedFolderId"/> e informado, essa pasta e
    /// toda a sua descendencia sao omitidas.
    /// </summary>
    public IReadOnlyList<MoveDestinationNode> BuildFolderDestinationTree(
        AppConfiguration configuration,
        Guid? excludedFolderId = null)
    {
        var roots = new List<MoveDestinationNode> { CreateRootDestinationNode() };
        var rootNode = roots[0];

        foreach (var folder in configuration.GetFoldersOrdered())
        {
            if (folder is null || (excludedFolderId is not null && folder.Id == excludedFolderId.Value))
            {
                continue;
            }

            rootNode.Children.Add(BuildDestinationNode(folder, excludedFolderId));
        }

        return roots;
    }

    public static MoveDestinationNode CreateRootDestinationNode() => new(null, "Raiz");

    public static string DescribeFolderLocation(MenuFolder? parentFolder) =>
        parentFolder?.Name ?? "Raiz";

    public MoveOutcome ValidateMoveFolder(
        AppConfiguration configuration,
        MenuFolder sourceFolder,
        Guid? targetParentFolderId)
    {
        var effectiveTargetId = NormalizeTarget(targetParentFolderId);
        var currentParent = FindParentFolder(configuration, sourceFolder.Id);
        var currentParentId = currentParent?.Id;

        if (effectiveTargetId == currentParentId)
        {
            return MoveOutcome.SameLocation;
        }

        if (effectiveTargetId == sourceFolder.Id)
        {
            return MoveOutcome.Cycle;
        }

        if (effectiveTargetId is not null &&
            IsDescendantFolder(configuration, sourceFolder.Id, effectiveTargetId.Value))
        {
            return MoveOutcome.Cycle;
        }

        if (HasNameConflict(configuration, effectiveTargetId, sourceFolder.Name, sourceFolder.Id))
        {
            return MoveOutcome.DuplicateName;
        }

        return MoveOutcome.Ready;
    }

    public MoveOutcome ValidateMoveItem(
        AppConfiguration configuration,
        MenuItem sourceItem,
        Guid? targetParentFolderId)
    {
        var effectiveTargetId = NormalizeTarget(targetParentFolderId);
        var currentParent = FindItemParent(configuration, sourceItem.Id);
        var currentParentId = currentParent?.Id;

        if (effectiveTargetId == currentParentId)
        {
            return MoveOutcome.SameLocation;
        }

        if (HasNameConflict(configuration, effectiveTargetId, sourceItem.Name, sourceItem.Id))
        {
            return MoveOutcome.DuplicateName;
        }

        return MoveOutcome.Ready;
    }

    public MoveResult MoveFolder(
        AppConfiguration configuration,
        MenuFolder sourceFolder,
        Guid? targetParentFolderId,
        Action saveConfiguration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(sourceFolder);
        ArgumentNullException.ThrowIfNull(saveConfiguration);

        var outcome = ValidateMoveFolder(configuration, sourceFolder, targetParentFolderId);
        if (outcome != MoveOutcome.Ready)
        {
            return MoveResult.FromOutcome(outcome);
        }

        var effectiveTargetId = NormalizeTarget(targetParentFolderId);
        var originalCollection = GetFolderCollection(configuration, FindParentFolder(configuration, sourceFolder.Id)?.Id);
        var destinationCollection = GetFolderCollection(configuration, effectiveTargetId);

        try
        {
            if (!originalCollection.Remove(sourceFolder))
            {
                return MoveResult.Invalid("Não foi possível localizar a pasta de origem.");
            }

            destinationCollection.Add(sourceFolder);
            saveConfiguration();
            return MoveResult.Moved();
        }
        catch
        {
            RestoreFolder(sourceFolder, originalCollection, destinationCollection);
            return MoveResult.Invalid("Não foi possível salvar a movimentação da pasta.");
        }
    }

    public MoveResult MoveItem(
        AppConfiguration configuration,
        MenuItem sourceItem,
        Guid? targetParentFolderId,
        Action saveConfiguration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(sourceItem);
        ArgumentNullException.ThrowIfNull(saveConfiguration);

        var outcome = ValidateMoveItem(configuration, sourceItem, targetParentFolderId);
        if (outcome != MoveOutcome.Ready)
        {
            return MoveResult.FromOutcome(outcome);
        }

        var effectiveTargetId = NormalizeTarget(targetParentFolderId);
        var originalCollection = GetItemCollection(configuration, FindItemParent(configuration, sourceItem.Id)?.Id);
        var destinationCollection = GetItemCollection(configuration, effectiveTargetId);

        try
        {
            if (!originalCollection.Remove(sourceItem))
            {
                return MoveResult.Invalid("Não foi possível localizar o projeto de origem.");
            }

            destinationCollection.Add(sourceItem);
            saveConfiguration();
            return MoveResult.Moved();
        }
        catch
        {
            RestoreItem(sourceItem, originalCollection, destinationCollection);
            return MoveResult.Invalid("Não foi possível salvar a movimentação do projeto.");
        }
    }

    private static Guid? NormalizeTarget(Guid? targetParentFolderId) =>
        targetParentFolderId is null || targetParentFolderId == Guid.Empty
            ? null
            : targetParentFolderId;

    private static bool ContainsFolder(IEnumerable<MenuFolder>? folders, Guid candidateId)
    {
        foreach (var folder in folders ?? Enumerable.Empty<MenuFolder>())
        {
            if (folder is null)
            {
                continue;
            }

            if (folder.Id == candidateId || ContainsFolder(folder.Folders, candidateId))
            {
                return true;
            }
        }

        return false;
    }

    private static MoveDestinationNode BuildDestinationNode(MenuFolder folder, Guid? excludedFolderId)
    {
        var node = new MoveDestinationNode(folder.Id, folder.Name ?? string.Empty);

        foreach (var childFolder in folder.GetFoldersOrdered())
        {
            if (childFolder is null || (excludedFolderId is not null && childFolder.Id == excludedFolderId.Value))
            {
                continue;
            }

            node.Children.Add(BuildDestinationNode(childFolder, excludedFolderId));
        }

        return node;
    }

    private static void RestoreFolder(
        MenuFolder folder,
        IList<MenuFolder> originalCollection,
        IList<MenuFolder> destinationCollection)
    {
        destinationCollection.Remove(folder);
        if (!originalCollection.Contains(folder))
        {
            originalCollection.Add(folder);
        }
    }

    private static void RestoreItem(
        MenuItem item,
        IList<MenuItem> originalCollection,
        IList<MenuItem> destinationCollection)
    {
        destinationCollection.Remove(item);
        if (!originalCollection.Contains(item))
        {
            originalCollection.Add(item);
        }
    }
}