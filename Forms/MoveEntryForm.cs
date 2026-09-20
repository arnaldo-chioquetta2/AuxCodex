using AuxCodex.Models;

namespace AuxCodex.Forms;

/// <summary>
/// Janela reutilizavel para mover pastas ou projetos na arvore do AuxCodex.
/// </summary>
public partial class MoveEntryForm : Form
{
    private readonly IReadOnlyList<MoveDestinationNode> _destinations;
    private readonly Guid? _initialDestinationFolderId;

    public MoveEntryForm(
        string title,
        string description,
        IReadOnlyList<MoveDestinationNode> destinations,
        Guid? initialDestinationFolderId)
    {
        _destinations = destinations ?? throw new ArgumentNullException(nameof(destinations));
        _initialDestinationFolderId = initialDestinationFolderId;

        InitializeComponent();
        Icon = Utils.ApplicationIconProvider.Icon;
        Text = title;
        formLabel.Text = description;

        BuildTree();
        SelectInitialDestination();
    }

    /// <summary>
    /// Identificador da pasta de destino escolhida, ou <c>null</c> para a raiz.
    /// </summary>
    public Guid? SelectedDestinationFolderId { get; private set; }

    /// <summary>
    /// Indica se o usuario escolheu um destino antes de confirmar.
    /// </summary>
    public bool HasSelection { get; private set; }

    private void BuildTree()
    {
        destinationTreeView.BeginUpdate();
        destinationTreeView.Nodes.Clear();

        foreach (var destination in _destinations)
        {
            destinationTreeView.Nodes.Add(CreateTreeNode(destination));
        }

        destinationTreeView.ExpandAll();
        destinationTreeView.EndUpdate();
    }

    private static TreeNode CreateTreeNode(MoveDestinationNode destination)
    {
        var node = new TreeNode(destination.Label) { Tag = destination };

        foreach (var child in destination.Children)
        {
            node.Nodes.Add(CreateTreeNode(child));
        }

        return node;
    }

    private void SelectInitialDestination()
    {
        var target = FindNodeByFolderId(_initialDestinationFolderId) ?? FirstNode();
        if (target is null)
        {
            return;
        }

        destinationTreeView.SelectedNode = target;
        target.EnsureVisible();
    }

    private TreeNode? FindNodeByFolderId(Guid? folderId)
    {
        foreach (TreeNode rootNode in destinationTreeView.Nodes)
        {
            var match = FindNode(rootNode, folderId);
            if (match is not null)
            {
                return match;
            }
        }

        return null;
    }

    private static TreeNode? FindNode(TreeNode node, Guid? folderId)
    {
        if (node.Tag is MoveDestinationNode destination && destination.FolderId == folderId)
        {
            return node;
        }

        foreach (TreeNode child in node.Nodes)
        {
            var match = FindNode(child, folderId);
            if (match is not null)
            {
                return match;
            }
        }

        return null;
    }

    private TreeNode? FirstNode() =>
        destinationTreeView.Nodes.Count > 0 ? destinationTreeView.Nodes[0] : null;

    private void OnMoveButtonClick(object? sender, EventArgs e)
    {
        if (destinationTreeView.SelectedNode?.Tag is not MoveDestinationNode destination)
        {
            MessageBox.Show(
                "Selecione a pasta de destino.",
                "Mover",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        SelectedDestinationFolderId = destination.FolderId;
        HasSelection = true;
        DialogResult = DialogResult.OK;
        Close();
    }
}
